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

            var targets = new List<(string className, string methodName)>
                    {
                        ("AdditionTests", "AdditionTest"),
                        ("AdditionTests", "AdditionTest2"),
                        ("SubtractionTests", "SubtractionTest"),
                        ("SubtractionTests", "SubtractionTest2"),
                    };

            var testFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in allTestFiles)
            {
                string fileContents = File.ReadAllText(file);
                if (targets.Any(target => fileContents.Contains($"class {target.className}")))
                {
                    testFiles.Add(file);
                }
            }

            if (testFiles.Count == 0)
            {
                Console.WriteLine($"No test files found in {testProjectDirectory} matching the provided targets.");
                return;
            }

            foreach (var file in testFiles)
            {
                SetInjection(file, targets, testProjectPath);
            }

            Dictionary<string, ISet<string>> testCoverage = [];
            Dictionary<string, bool> testPassFail = []; ;

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


            Rank rank = new(testCoverage, testPassFail);
            rank.CalculateTarantula();
            rank.CalculateOchiai();
            rank.CalculateDStar();
            rank.CalculateOp2();
            rank.CalculateJaccard();

            string csvOutputPath = Path.Combine(solutionDirectory, "suspiciousness_report.csv");
            rank.WriteSuspiciousnessReport(csvOutputPath);
            Console.WriteLine($"Suspiciousness scores written to {csvOutputPath}.");
        }

        private static void SetInjection(string filePath, List<(string className, string methodName)> targets, string testProjectPath)
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
                RunTest(testProjectPath, className, methodName);
            }

            if (modified)
            {
                File.WriteAllText(filePath, updatedRoot.NormalizeWhitespace().ToFullString());
                Thread.Sleep(1000); // Give compiler a moment to catch up
            }
        }

        private static bool RunTest(string testProjectPath, string testName, string testMethodToRun)
        {
            try
            {
                string filter = $"FullyQualifiedName~{testName}.{testMethodToRun}";

                ProcessStartInfo startInfo = new(
                    "dotnet",
                    $"test \"{testProjectPath}\" --no-build --filter \"{filter}\""
                );
                //{
                //    RedirectStandardOutput = true,
                //    RedirectStandardError = true,
                //    UseShellExecute = false,
                //    CreateNoWindow = true,
                //};

                using Process process = Process.Start(startInfo);
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
}
