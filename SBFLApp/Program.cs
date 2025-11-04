using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using MathApp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;

namespace SBFLApp
{
    class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine("Running the Spetrum Based Fault Localizer Application\n");

            if (args.Length < 2)
            {
                Console.WriteLine("Usage: dotnet run <solutionDirectory> <testProjectName> [--reset]");
                return;
            }

            string solutionDirectory = args[0];
            string testProjectName = args[1];
            bool resetRequested = args.Any(arg => string.Equals(arg, "--reset", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "-r", StringComparison.OrdinalIgnoreCase));

            if (!Directory.Exists(solutionDirectory))
            {
                Console.WriteLine($"Solution directory not found: {solutionDirectory}");
                return;
            }

            string testProjectDirectory = Path.Combine(solutionDirectory, testProjectName);
            if (!Directory.Exists(testProjectDirectory))
            {
                Console.WriteLine($"Test project directory not found: {testProjectDirectory}");
                return;
            }

            string? testProjectFile = Directory.EnumerateFiles(testProjectDirectory, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
            string testProjectPath = testProjectFile ?? testProjectDirectory;

            if (resetRequested)
            {
                Console.WriteLine("Resetting existing instrumentation artifacts...");
                Spectrum.ResetInstrumentation(solutionDirectory, AppDomain.CurrentDomain.BaseDirectory);
            }

            var allTestFiles = Directory.EnumerateFiles(testProjectDirectory, "*.cs", SearchOption.AllDirectories)
                .Where(file => !file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                            && !file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
                .ToList();

            var discoveredTests = DiscoverTests(allTestFiles);

            if (!discoveredTests.Any())
            {
                Console.WriteLine($"No tests discovered in {testProjectDirectory}.");
                return;
            }

            Console.WriteLine("Discovered the following tests:");
            foreach (var test in discoveredTests)
            {
                Console.WriteLine($" - {test.FullyQualifiedName}");
            }

            var testPassFail = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var group in discoveredTests.GroupBy(test => test.FilePath, StringComparer.OrdinalIgnoreCase))
            {
                SetInjection(group.Key, group.ToList(), testProjectPath, testPassFail);
            }

            var testCoverage = BuildTestCoverage(discoveredTests);

            Rank rank = new Rank(testCoverage, testPassFail);
            rank.calculateTarantula();
            rank.calculateOchiai();
            rank.calculateDStar();
            rank.calculateOp2();
            rank.calculateJaccard();

            string csvOutputPath = Path.Combine(solutionDirectory, "suspiciousness_report.csv");
            rank.WriteSuspiciousnessReport(csvOutputPath);
            Console.WriteLine($"Suspiciousness scores written to {csvOutputPath}.");
        }

        private sealed record DiscoveredTest(string FilePath, string TypeDisplayName, string MethodName, string FullyQualifiedName)
        {
            public string CoverageFileStem => FullyQualifiedName;
        }

        private static List<DiscoveredTest> DiscoverTests(IEnumerable<string> testFiles)
        {
            var discoveredTests = new List<DiscoveredTest>();

            foreach (var file in testFiles)
            {
                string sourceCode = File.ReadAllText(file);
                var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
                var root = syntaxTree.GetRoot();

                foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    if (!IsTestMethod(method))
                    {
                        continue;
                    }

                    string typeDisplayName = GetTypeDisplayName(method);
                    if (string.IsNullOrEmpty(typeDisplayName))
                    {
                        continue;
                    }

                    string namespaceName = GetNamespace(method);
                    string fullyQualifiedClass = string.IsNullOrEmpty(namespaceName)
                        ? typeDisplayName
                        : $"{namespaceName}.{typeDisplayName}";
                    string fullyQualifiedName = $"{fullyQualifiedClass}.{method.Identifier.Text}";

                    discoveredTests.Add(new DiscoveredTest(file, typeDisplayName, method.Identifier.Text, fullyQualifiedName));
                }
            }

            return discoveredTests
                .Distinct()
                .OrderBy(test => test.FullyQualifiedName, StringComparer.Ordinal)
                .ToList();
        }

        private static Dictionary<string, ISet<string>> BuildTestCoverage(IEnumerable<DiscoveredTest> tests)
        {
            var coverage = new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var test in tests)
            {
                string fileKey = test.CoverageFileStem;
                string coverageFileName = $"{fileKey}.coverage";
                var guidSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                var searchPaths = new[]
                {
                    coverageFileName,
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, coverageFileName)
                };

                foreach (var path in searchPaths)
                {
                    if (!File.Exists(path))
                    {
                        continue;
                    }

                    foreach (var line in File.ReadAllLines(path))
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            guidSet.Add(line.Trim());
                        }
                    }

                    break;
                }

                if (!guidSet.Any())
                {
                    Console.WriteLine($"Coverage file not found or empty for test: {coverageFileName}");
                }

                coverage[fileKey] = guidSet;
            }

            return coverage;
        }

        private static MethodDeclarationSyntax? FindMethod(SyntaxNode root, DiscoveredTest test)
        {
            return root
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(method =>
                    string.Equals(method.Identifier.Text, test.MethodName, StringComparison.Ordinal) &&
                    string.Equals(GetTypeDisplayName(method), test.TypeDisplayName, StringComparison.Ordinal));
        }

        private static bool IsTestMethod(MethodDeclarationSyntax method)
        {
            foreach (var attributeList in method.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    string attributeName = GetAttributeShortName(attribute);
                    if (IsRecognizedTestAttribute(attributeName))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsRecognizedTestAttribute(string attributeName)
        {
            return attributeName.Equals("Fact", StringComparison.OrdinalIgnoreCase)
                || attributeName.Equals("Theory", StringComparison.OrdinalIgnoreCase)
                || attributeName.Equals("TestMethod", StringComparison.OrdinalIgnoreCase)
                || attributeName.Equals("Test", StringComparison.OrdinalIgnoreCase)
                || attributeName.Equals("DataTestMethod", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetAttributeShortName(AttributeSyntax attribute)
        {
            return attribute.Name switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.Text,
                QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
                AliasQualifiedNameSyntax alias => alias.Name.Identifier.Text,
                _ => attribute.Name.ToString(),
            };
        }

        private static string GetTypeDisplayName(SyntaxNode node)
        {
            var typeNames = node
                .Ancestors()
                .OfType<TypeDeclarationSyntax>()
                .Select(type => type.Identifier.Text)
                .Reverse()
                .ToList();

            return typeNames.Count == 0 ? string.Empty : string.Join('.', typeNames);
        }

        private static string GetNamespace(SyntaxNode node)
        {
            var namespaceNode = node
                .Ancestors()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .FirstOrDefault();

            return namespaceNode?.Name.ToString() ?? string.Empty;
        }

        private static void SetInjection(
            string filePath,
            IReadOnlyList<DiscoveredTest> tests,
            string testProjectPath,
            Dictionary<string, bool> testPassFail)
        {
            Console.WriteLine("Injection and testing in progress...");

            string sourceCode = File.ReadAllText(filePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = syntaxTree.GetRoot();

            var updatedRoot = root;
            var rootDirty = false;

            foreach (var test in tests)
            {
                var methodNode = FindMethod(updatedRoot, test);

                if (methodNode == null)
                {
                    Console.WriteLine($"Skipping instrumentation for '{test.FullyQualifiedName}' because the method could not be located in '{filePath}'.");
                    bool fallbackResult = RunTest(testProjectPath, test.FullyQualifiedName);
                    testPassFail[test.CoverageFileStem] = fallbackResult;
                    continue;
                }

                bool alreadyInstrumented = methodNode.DescendantNodes()
                    .OfType<ExpressionStatementSyntax>()
                    .Any(stmt => stmt.ToString().Contains("System.IO.File.AppendAllText"));

                string newGuid = Guid.NewGuid().ToString();
                string logStatement = $"System.IO.File.AppendAllText(\"{test.CoverageFileStem}.coverage\", \"{newGuid}\");";

                if (alreadyInstrumented)
                {
                    var rewriter = new LogStatementRewriter(logStatement);
                    var newMethodNode = (MethodDeclarationSyntax)rewriter.Visit(methodNode);
                    updatedRoot = updatedRoot.ReplaceNode(methodNode, newMethodNode);
                    rootDirty = true;
                }
                else
                {
                    Spectrum.SpectrumMethod(filePath, test.MethodName);

                    string reloadedCode = File.ReadAllText(filePath);
                    updatedRoot = CSharpSyntaxTree.ParseText(reloadedCode).GetRoot();

                    var reloadedMethod = FindMethod(updatedRoot, test);

                    if (reloadedMethod != null)
                    {
                        var rewriter = new LogStatementRewriter(logStatement);
                        var newMethodNode = (MethodDeclarationSyntax)rewriter.Visit(reloadedMethod);
                        updatedRoot = updatedRoot.ReplaceNode(reloadedMethod, newMethodNode);
                        rootDirty = true;
                    }
                }

                if (rootDirty)
                {
                    File.WriteAllText(filePath, updatedRoot.NormalizeWhitespace().ToFullString());
                    Thread.Sleep(1000);
                    rootDirty = false;
                }

                string binPath = AppDomain.CurrentDomain.BaseDirectory;
                string coveragePath = Path.Combine(binPath, $"{test.CoverageFileStem}.coverage");
                File.AppendAllText(coveragePath, newGuid + Environment.NewLine);

                // Run the test silently
                bool passed = RunTest(testProjectPath, test.FullyQualifiedName);
                testPassFail[test.CoverageFileStem] = passed;
            }
        }

        private static bool RunTest(string testProjectPath, string fullyQualifiedTestName)
        {
            try
            {
                string filter = $"FullyQualifiedName~{fullyQualifiedTestName}";

                var startInfo = new ProcessStartInfo(
                    "dotnet",
                    $"test \"{testProjectPath}\" --no-build --filter \"{filter}\""
                )
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using (Process? process = Process.Start(startInfo))
                {
                    if (process is null)
                    {
                        Console.WriteLine("Failed to start test process.");
                        return false;
                    }

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

            return base.VisitExpressionStatement(node) ?? node;
        }
    }
}
