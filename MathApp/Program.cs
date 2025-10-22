using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MathApp
{
    class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine("Hello\n");

            MathOperations.Add(5, 2);
            MathOperations.Subtract(5, 2);

            // Spectrum.SpectrumMethod("D:/CS4850/MathApp-Main/MathApp/MathOperations.cs", "Add");
            // Spectrum.SpectrumMethod("D:/CS4850/MathApp-Main/MathApp/MathOperations.cs", "Subtract");

            // MathOperations.SpectrumAll("D:/CS4850/MathApp-Main/MathApp.Tests/Addition.cs");

            // MathApp.Spectrum.SpectrumAllForTest("D:/CS4850/MathApp-Main/MathApp/MathOperations.cs", "AdditionTests.coverage");

            // Spectrum.SpectrumProject("D:/CS4850/MathApp-Main/", "MathAppTests.AllFiles");

            // bool passed = RunTest("D:/CS4850/MathApp-Main/MathApp.Tests", "AdditionTests", "AdditionTest");

            // Console.WriteLine($"Test passed: {passed}");

            // Spectrum.SpectrumTests("D:/CS4850/MathApp-Main/");

            var testFiles = new List<string>
                {
                    "C:/repos/CSharpFL/MathApp.Tests/Addition.cs",
                    "C:/repos/CSharpFL/MathApp.Tests/Subtraction.cs"
                };

            var targets = new List<(string className, string methodName)>
                    {
                        ("AdditionTests", "AdditionTest"),
                        ("AdditionTests", "AdditionTest2"),
                        ("SubtractionTests", "SubtractionTest"),
                        ("SubtractionTests", "SubtractionTest2"),
                    };

            foreach (var file in testFiles)
            {
                SetInjection(file, targets);
            }

            Dictionary<string, ISet<string>> testCoverage = new Dictionary<string, ISet<string>>();
            Dictionary<string, bool> testPassFail = new Dictionary<string, bool>(); ;

            testPassFail.Add("AdditionTests.AdditionTest", true);
            testPassFail.Add("AdditionTests.AdditionTest2", true);
            testPassFail.Add("SubtractionTests.SubtractionTest", true);
            testPassFail.Add("SubtractionTests.SubtractionTest2", false);

            for (int i = 0; i < targets.Count; i++)
            {
                string className = targets[i].className;
                string testName = targets[i].methodName;
                string fileKey = $"{className}.{testName}";
                string fileName = $"{fileKey}.coverage";

                // Initialize a set for this test
                var guidSet = new HashSet<string>();

                if (File.Exists(fileName))
                {
                    // Read all GUIDs from the coverage file
                    foreach (var line in File.ReadAllLines(fileName))
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            guidSet.Add(line.Trim()); // store each GUID
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Coverage file not found: {fileName}");
                }

                // Add to dictionary
                testCoverage[fileKey] = guidSet;
            }


            Rank rank = new Rank(testCoverage, testPassFail);
            rank.calculateTarantula();


        }

        private static void SetInjection(string filePath, List<(string className, string methodName)> targets)
        {
            Console.WriteLine("Injection and testing in progress...");

            string sourceCode = File.ReadAllText(filePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = syntaxTree.GetRoot();

            var updatedRoot = root;
            var modified = false;

            foreach (var (className, methodName) in targets)
            {
                var methodNode = updatedRoot
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault(m => m.Identifier.Text == methodName);

                if (methodNode == null)
                {
                    // Silently skip methods not found
                    continue;
                }

                bool alreadyInstrumented = methodNode.DescendantNodes()
                    .OfType<ExpressionStatementSyntax>()
                    .Any(stmt => stmt.ToString().Contains("System.IO.File.AppendAllText"));

                string newGuid = Guid.NewGuid().ToString();
                string logStatement = $"System.IO.File.AppendAllText(\"{className}.{methodName}.coverage\", \"{newGuid}\");";

                if (alreadyInstrumented)
                {
                    var rewriter = new LogStatementRewriter(logStatement);
                    var newMethodNode = (MethodDeclarationSyntax)rewriter.Visit(methodNode);
                    updatedRoot = updatedRoot.ReplaceNode(methodNode, newMethodNode);
                    modified = true;
                }
                else
                {
                    Spectrum.SpectrumMethod(filePath, methodName);

                    string reloadedCode = File.ReadAllText(filePath);
                    updatedRoot = CSharpSyntaxTree.ParseText(reloadedCode).GetRoot();

                    var reloadedMethod = updatedRoot
                        .DescendantNodes()
                        .OfType<MethodDeclarationSyntax>()
                        .FirstOrDefault(m => m.Identifier.Text == methodName);

                    if (reloadedMethod != null)
                    {
                        var rewriter = new LogStatementRewriter(logStatement);
                        var newMethodNode = (MethodDeclarationSyntax)rewriter.Visit(reloadedMethod);
                        updatedRoot = updatedRoot.ReplaceNode(reloadedMethod, newMethodNode);
                        modified = true;
                    }
                }

                string binPath = AppDomain.CurrentDomain.BaseDirectory;
                string coveragePath = Path.Combine(binPath, $"{className}.{methodName}.coverage");
                File.AppendAllText(coveragePath, newGuid + Environment.NewLine);

                // Run the test silently
                RunTest(Path.GetDirectoryName(filePath), className, methodName);
            }

            if (modified)
            {
                File.WriteAllText(filePath, updatedRoot.NormalizeWhitespace().ToFullString());
                Thread.Sleep(1000); // Give compiler a moment to catch up
            }
        }

        private static bool RunTest(string solutionPath, string testName, string testMethodToRun)
        {
            try
            {
                string filter = $"FullyQualifiedName~{testName}.{testMethodToRun}";

                ProcessStartInfo startInfo = new ProcessStartInfo(
                    "dotnet",
                    $"test \"{solutionPath}\" --no-build --filter \"{filter}\""
                );
                //{
                //    RedirectStandardOutput = true,
                //    RedirectStandardError = true,
                //    UseShellExecute = false,
                //    CreateNoWindow = true,
                //};

                using (Process process = Process.Start(startInfo))
                {
                    bool exited = process.WaitForExit(30 * 1000);
                    if (!exited)
                    {
                        Console.WriteLine("Process timed out. Killing the process...");
                        process.Kill();
                        return false;
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    Console.WriteLine(output);
                    if (!string.IsNullOrEmpty(error))
                    {
                        Console.WriteLine(error);
                    }

                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running test: {ex.Message}");
                return false;
            }
        }
    }

    class LogStatementRewriter : CSharpSyntaxRewriter
    {
        private readonly string _newLogStatement;

        public LogStatementRewriter(string newLogStatement)
        {
            _newLogStatement = newLogStatement;
        }

        public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            var text = node.ToString();
            if (text.Contains("System.IO.File.AppendAllText"))
            {
                return SyntaxFactory.ParseStatement(_newLogStatement)
                    .WithLeadingTrivia(node.GetLeadingTrivia())
                    .WithTrailingTrivia(node.GetTrailingTrivia());
            }

            return base.VisitExpressionStatement(node);
        }
    }
}
