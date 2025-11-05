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
            LogMessage("Running the Spetrum Based Fault Localizer Application\n");

            if (args.Length < 2)
            {
                LogWarning("Usage: dotnet run <solutionDirectory> <testProjectName> [--reset]");
                return;
            }

            string solutionDirectory = args[0];
            string testProjectName = args[1];
            bool resetRequested = args.Any(arg => string.Equals(arg, "--reset", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "-r", StringComparison.OrdinalIgnoreCase));

            if (!Directory.Exists(solutionDirectory))
            {
                LogError($"Solution directory not found: {solutionDirectory}");
                return;
            }

            string testProjectDirectory = Path.Combine(solutionDirectory, testProjectName);
            if (!Directory.Exists(testProjectDirectory))
            {
                LogError($"Test project directory not found: {testProjectDirectory}");
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

            if (discoveredTests.Count == 0)
            {
                LogError($"No tests discovered in {testProjectDirectory}.");
                return;
            }

            LogMessage("Discovered the following tests:");
            foreach (var test in discoveredTests)
            {
                Console.WriteLine($" - {test.FullyQualifiedName}");
            }

            var testPassFail = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var group in discoveredTests.GroupBy(test => test.FilePath, StringComparer.OrdinalIgnoreCase))
            {
                SetInjection(group.Key, group.ToList(), testProjectPath, testPassFail);
            }

            var testCoverage = BuildTestCoverage(
                discoveredTests,
                solutionDirectory,
                testProjectDirectory);

            Rank rank = new(testCoverage, testPassFail);
            rank.CalculateTarantula();
            rank.CalculateOchiai();
            rank.CalculateDStar();
            rank.CalculateOp2();
            rank.CalculateJaccard();

            string csvOutputPath = Path.Combine(solutionDirectory, "suspiciousness_report.csv");
            rank.WriteSuspiciousnessReport(csvOutputPath);
            LogMessage($"Suspiciousness scores written to {csvOutputPath}.");
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

        private static Dictionary<string, ISet<string>> BuildTestCoverage(
            IEnumerable<DiscoveredTest> tests,
            string solutionDirectory,
            string testProjectDirectory)
        {
            var coverage = new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase);

            var candidateRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Directory.GetCurrentDirectory(),
                AppDomain.CurrentDomain.BaseDirectory,
                solutionDirectory,
                testProjectDirectory
            };

            var binDirectory = Path.Combine(testProjectDirectory, "bin");
            var binCoverageFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (Directory.Exists(binDirectory))
            {
                foreach (var path in Directory.EnumerateFiles(binDirectory, "*.coverage", SearchOption.AllDirectories))
                {
                    var fileName = Path.GetFileName(path);
                    binCoverageFiles[fileName] = path;
                }

                foreach (var directory in Directory.EnumerateDirectories(binDirectory, "*", SearchOption.AllDirectories))
                {
                    candidateRoots.Add(directory);
                }
            }

            foreach (var test in tests)
            {
                string fileKey = test.CoverageFileStem;
                string coverageFileName = $"{fileKey}.coverage";
                var guidSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                var searchPaths = new List<string>();

                foreach (var root in candidateRoots)
                {
                    if (!string.IsNullOrWhiteSpace(root))
                    {
                        searchPaths.Add(Path.Combine(root, coverageFileName));
                    }
                }

                if (binCoverageFiles.TryGetValue(coverageFileName, out var locatedPath))
                {
                    searchPaths.Add(locatedPath);
                }

                foreach (var path in searchPaths)
                {
                    if (!File.Exists(path))
                    {
                        continue;
                    }

                    // A coverage file was found, read all the guid values and add them to the guid set.
                    foreach (var line in File.ReadAllLines(path))
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            guidSet.Add(line.Trim());
                        }
                    }

                    break;
                }

                if (guidSet.Count == 0)
                {
                    LogError($"Coverage file not found or empty for test: {coverageFileName}");
                }

                // Assign the guid set from the file to the coverage data for the test function.
                coverage[fileKey] = guidSet;
            }

            return coverage;
        }

        /// <summary>
        /// Look through the decendents of the root node for the node specified by the test parameter
        /// </summary>
        /// <param name="root"></param>
        /// <param name="test"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Search the attribute name for those attributes related to test functions.
        /// </summary>
        /// <param name="attributeName">That attribute name to search.</param>
        /// <returns>True if this is a test attribute, false otherwise.</returns>
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
            LogMessage("Injection and testing in progress...");

            string sourceCode = File.ReadAllText(filePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = syntaxTree.GetRoot();

            var updatedRoot = root;

            foreach (var test in tests)
            {
                var methodNode = FindMethod(updatedRoot, test);

                if (methodNode == null)
                {
                    LogWarning($"Skipping instrumentation for '{test.FullyQualifiedName}' because the method could not be located in '{filePath}'.");
                    bool fallbackResult = RunTest(testProjectPath, test.FullyQualifiedName);
                    testPassFail[test.CoverageFileStem] = fallbackResult;
                    continue;
                }

                bool alreadyInstrumented = methodNode.DescendantNodes()
                    .OfType<ExpressionStatementSyntax>()
                    .Any(stmt => stmt.ToString().Contains("System.IO.File.AppendAllText"));

                string coverageFileName = $"{test.CoverageFileStem}.coverage";
                DeleteCoverageFile(coverageFileName);

                if (alreadyInstrumented)
                {
                    updatedRoot = RewriteCoverageStatements(filePath, updatedRoot, methodNode, coverageFileName);
                }
                else
                {
                    Spectrum.SpectrumMethod(filePath, test.MethodName, coverageFileName);

                    string reloadedCode = File.ReadAllText(filePath);
                    updatedRoot = CSharpSyntaxTree.ParseText(reloadedCode).GetRoot();

                    var reloadedMethod = FindMethod(updatedRoot, test);

                    if (reloadedMethod != null)
                    {
                        updatedRoot = RewriteCoverageStatements(filePath, updatedRoot, reloadedMethod, coverageFileName);
                    }
                }

                // Run the test silently
                bool passed = RunTest(testProjectPath, test.FullyQualifiedName);
                testPassFail[test.CoverageFileStem] = passed;
            }
        }

        private static void DeleteCoverageFile(string coverageFileName)
        {
            try
            {
                if (File.Exists(coverageFileName))
                {
                    File.Delete(coverageFileName);
                }

                var binPath = AppDomain.CurrentDomain.BaseDirectory;
                var fullPath = Path.Combine(binPath, coverageFileName);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Failed to delete coverage file '{coverageFileName}': {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Failed to delete coverage file '{coverageFileName}': {ex.Message}");
            }
        }

        private static SyntaxNode RewriteCoverageStatements(
            string filePath,
            SyntaxNode root,
            MethodDeclarationSyntax methodNode,
            string coverageFileName)
        {
            var rewriter = new LogStatementRewriter(coverageFileName);
            var newMethodNode = (MethodDeclarationSyntax)rewriter.Visit(methodNode);

            if (ReferenceEquals(newMethodNode, methodNode))
            {
                return root;
            }

            var updatedRoot = root.ReplaceNode(methodNode, newMethodNode);
            File.WriteAllText(filePath, updatedRoot.NormalizeWhitespace().ToFullString());
            Thread.Sleep(1000);

            var reloadedCode = File.ReadAllText(filePath);
            return CSharpSyntaxTree.ParseText(reloadedCode).GetRoot();
        }

        private static bool RunTest(string testProjectPath, string fullyQualifiedTestName, bool displayTestOutput = false)
        {
            try
            {
                string filter = $"FullyQualifiedName~{fullyQualifiedTestName}";

                var startInfo = new ProcessStartInfo(
                    "dotnet",
                    $"test \"{testProjectPath}\" --filter \"{filter}\""
                )
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using Process? process = Process.Start(startInfo);
                if (process is null)
                {
                    LogError("Failed to start test process.");
                    return false;
                }

                bool exited = process.WaitForExit(30 * 1000);
                if (!exited)
                { 
                    LogWarning("Process timed out. Killing the process...");
                    process.Kill();
                    return false;
                }

                if (displayTestOutput)
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    Console.WriteLine(output);
                    if (!string.IsNullOrEmpty(error))
                    {
                        Console.WriteLine(error);
                    }
                }

                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                LogError($"Error running test: {ex.Message}");
                return false;
            }
        }

        private static void LogMessage(string message)
{
            ConsoleWriteLine($"Info: {message}", ConsoleColor.Cyan);

}

        private static void LogWarning(string message)
        {
            ConsoleWriteLine($"Warning: {message}", ConsoleColor.DarkYellow);
        }

        private static void LogError(string message)
        {
            ConsoleWriteLine($"Error: {message}", ConsoleColor.Red);
        }

        private static void ConsoleWriteLine(string message, ConsoleColor color = ConsoleColor.Gray)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        static void ConsoleWrite(string message, ConsoleColor color = ConsoleColor.Gray)
        {
            Console.ForegroundColor = color;
            Console.Write(message);
            Console.ResetColor();
        }

    }
}
