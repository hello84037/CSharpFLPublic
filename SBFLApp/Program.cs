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

            // Parse the given commandline arguments.
            string solutionDirectory = args[0];
            string testProjectName = args[1];
            bool resetRequested = args.Any(arg => string.Equals(arg, "--reset", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "-r", StringComparison.OrdinalIgnoreCase));

            // Verify the solution directory to be tested exists.
            if (!Directory.Exists(solutionDirectory))
            {
                LogError($"Solution directory not found: {solutionDirectory}");
                return;
            }
            
            // Verify the test project path exists.  The assumption is that the project name matches the project directory.
            string testProjectDirectory = Path.Combine(solutionDirectory, testProjectName);
            if (!Directory.Exists(testProjectDirectory))
            {
                LogError($"Test project directory not found: {testProjectDirectory}");
                return;
            }

            // Get the path to the test project.  If there isn't a .csproj file, then use the directory.
            string? testProjectFile = Directory.EnumerateFiles(testProjectDirectory, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
            string testProjectPath = testProjectFile ?? testProjectDirectory;

            // If a reset was requested, we want to erase all instrumentation and coverage data to start fresh.
            if (resetRequested)
            {
                Console.WriteLine("Resetting existing instrumentation artifacts...");
                Spectrum.ResetInstrumentation(solutionDirectory, AppDomain.CurrentDomain.BaseDirectory);
            }

            // Get all the .cs files in the test project directory ignoreing the bin and obj directories.
            var allTestFiles = Directory.EnumerateFiles(testProjectDirectory, "*.cs", SearchOption.AllDirectories)
                .Where(file => !file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                            && !file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
                .ToList();

            // Get a list of Test methods.
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

            // Create a dictionary for storing the test and the pass/fail status
            var testPassFail = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var group in discoveredTests.GroupBy(test => test.FilePath, StringComparer.OrdinalIgnoreCase))
            {
                SetInjection(group.Key, group.ToList(), testProjectPath, ref testPassFail);
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

        /// <summary>
        /// Goes through the list of included files to search for test signatures to find test
        /// methods.  
        /// </summary>
        /// <param name="testFiles">A list of .cs files in the test project.</param>
        /// <returns>A list of <see cref="DiscoveredTest"/> objects.</returns>
        private static List<DiscoveredTest> DiscoverTests(IEnumerable<string> testFiles)
        {
            var discoveredTests = new List<DiscoveredTest>();

            foreach (var file in testFiles)
            {
                // Read the source code for the current file.
                string sourceCode = File.ReadAllText(file);
                // Create the abstract syntax tree based off of the code.
                var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
                var root = syntaxTree.GetRoot();

                // Go through each decsendant of the root node that is a method.
                foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    // If the current method isn't a test method, move to the next.
                    if (!IsTestMethod(method))
                    {
                        continue;
                    }

                    // Get the display name for the function.  If there isn't one, then skip it.
                    string typeDisplayName = SyntaxTreeHelpers.GetTypeDisplayName(method);
                    if (string.IsNullOrEmpty(typeDisplayName))
                    {
                        continue;
                    }

                    // Get the namespace for the method to create a fully qualified name in the form
                    // {namespace}.{Identifier
                    string namespaceName = SyntaxTreeHelpers.GetNamespace(method);
                    string fullyQualifiedClass = string.IsNullOrEmpty(namespaceName)
                        ? typeDisplayName
                        : $"{namespaceName}.{typeDisplayName}";
                    string fullyQualifiedName = $"{fullyQualifiedClass}.{method.Identifier.Text}";

                    // Add the discovered test object to the list of discovered tests.
                    discoveredTests.Add(new DiscoveredTest(file, typeDisplayName, method.Identifier.Text, fullyQualifiedName));
                }
            }

            // return the discovered tests sorted in alphabetical order.
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
        /// Checks the <see cref="MethodDeclarationSyntax"/> to see if it has unit test attributes
        /// associated with it.
        /// </summary>
        /// <param name="method">The method to verify</param>
        /// <returns>True if the specified method is a test method. False otherwise.</returns>
        private static bool IsTestMethod(MethodDeclarationSyntax method)
        {
            foreach (var attributeList in method.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    string attributeName = SyntaxTreeHelpers.GetAttributeShortName(attribute);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath">The path to the test file.</param>
        /// <param name="tests">The <see cref="DiscoveredTest"/> objects associated with the file.</param>
        /// <param name="testProjectPath">The path to the test csproj file.</param>
        /// <param name="testPassFail">A dictionary to store the test and test result.</param>
        private static void SetInjection(
            in string filePath,
            in IReadOnlyList<DiscoveredTest> tests,
            in string testProjectPath,
            ref Dictionary<string, bool> testPassFail)
        {
            LogMessage("Injection and testing in progress...");

            // Read the source code for the given file.
            string sourceCode = File.ReadAllText(filePath);

            // Create an abstract syntax tree to represent the file.
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = syntaxTree.GetRoot();

            // Create a copy of the tree to modify with instrumentation data.
            var updatedRoot = root;

            // Process each test.
            foreach (var test in tests)
            {
                // Search the root for the specific test method.
                var methodNode = SyntaxTreeHelpers.FindMethod(updatedRoot, test.MethodName, test.TypeDisplayName);

                // If the method wasn't located, don't modify the source code, just run the test.
                if (methodNode == null)
                {
                    LogWarning($"Skipping instrumentation for '{test.FullyQualifiedName}' because the method could not be located in '{filePath}'.");

                    // Run the test anyway to get the pass/fail result.
                    bool fallbackResult = RunTest(testProjectPath, test.FullyQualifiedName);

                    // Save the pass/fail results in the referenced dictionary then skip the instrumentation.
                    testPassFail[test.CoverageFileStem] = fallbackResult;
                    continue;
                }

                // Check the method for existing instrumentation.
                bool alreadyInstrumented = methodNode.DescendantNodes()
                    .OfType<ExpressionStatementSyntax>()
                    .Any(stmt => stmt.ToString().Contains("System.IO.File.AppendAllText"));

                // Get the coverage filename and delete if there is an existing one.
                string coverageFileName = $"{test.CoverageFileStem}.coverage";
                DeleteCoverageFile(coverageFileName);

                //If the files were already instrumented, we are going to update them to make sure they are correct. 
                if (alreadyInstrumented)
                {
                    updatedRoot = RewriteCoverageStatements(filePath, updatedRoot, methodNode, coverageFileName);
                }
                else
                {
                    Spectrum.SpectrumMethod(filePath, test.MethodName, coverageFileName);

                    string reloadedCode = File.ReadAllText(filePath);
                    updatedRoot = CSharpSyntaxTree.ParseText(reloadedCode).GetRoot();

                    var reloadedMethod = SyntaxTreeHelpers.FindMethod(updatedRoot, test.MethodName, test.TypeDisplayName);

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

        /// <summary>
        /// Utilizes a <see cref="LogStatementRewriter"/> to add instrumentation to the specified method.
        /// </summary>
        /// <param name="filePath">The path to the file that contains the method to be modified.</param>
        /// <param name="root">The root <see cref="SyntaxNode"/> that contains the method.</param>
        /// <param name="methodNode">The <see cref="MethodDeclarationSyntax"/> node to modify.</param>
        /// <param name="coverageFileName">The name of the file containing the coverage information.</param>
        /// <returns>The root <see cref="SyntaxNode"/> that contains the modified method.</returns>
        private static SyntaxNode RewriteCoverageStatements(
            string filePath,
            SyntaxNode root,
            MethodDeclarationSyntax methodNode,
            string coverageFileName)
        {
            // Create an LogStatementRewriter to modify the method
            var rewriter = new LogStatementRewriter(coverageFileName);
            var newMethodNode = (MethodDeclarationSyntax)rewriter.Visit(methodNode);

            // If the methods are the same, then just return the root. 
            if (ReferenceEquals(newMethodNode, methodNode))
            {
                return root;
            }

            // Create a copy of the root with the method replaced by the instrumented method.
            var updatedRoot = root.ReplaceNode(methodNode, newMethodNode);

            // Write the file back to save the changes.
            File.WriteAllText(filePath, updatedRoot.NormalizeWhitespace().ToFullString());
            Thread.Sleep(1000);

            // Reload the code that was just written, and return the new root.
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

        /// <summary>
        /// Write to the console using the <see cref="ConsoleColor.Cyan"/>.
        /// </summary>
        /// <param name="message">The message to write to the console.</param>
        private static void LogMessage(string message)
        {
            ConsoleWriteLine($"Info: {message}", ConsoleColor.Cyan);

        }

        /// <summary>
        /// Write to the console using the <see cref="ConsoleColor.DarkYellow"/>.
        /// </summary>
        /// <param name="message">The message to write to the console.</param>
        private static void LogWarning(string message)
        {
            ConsoleWriteLine($"Warning: {message}", ConsoleColor.DarkYellow);
        }

        /// <summary>
        /// Write to the console using the <see cref="ConsoleColor.Red"/>.
        /// </summary>
        /// <param name="message">The message to write to the console.</param>
        private static void LogError(string message)
        {
            ConsoleWriteLine($"Error: {message}", ConsoleColor.Red);
        }

        /// <summary>
        /// Write a message with an appended newline to the console using the specified <see cref="ConsoleColor"/>.
        /// </summary>
        /// <param name="message">The message to write to the console.</param>
        /// <param name="color">The color to use.</param>
        private static void ConsoleWriteLine(string message, ConsoleColor color = ConsoleColor.Gray)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        /// <summary>
        /// Write a message to the console using the specified <see cref="ConsoleColor"/>.
        /// </summary>
        /// <param name="message">The message to write to the console.</param>
        /// <param name="color">The color to use.</param>
        static void ConsoleWrite(string message, ConsoleColor color = ConsoleColor.Gray)
        {
            Console.ForegroundColor = color;
            Console.Write(message);
            Console.ResetColor();
        }

    }
}
