using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using System.Text;

namespace SBFLApp
{
    class Program
    {
        private static readonly string CoverageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Coverage");
        private static readonly string TemporaryCoverageFileName = Path.Combine(CoverageDirectory, "__sbfl_current_test.coverage.tmp");

        static void Main(string[] args)
        {
            ConsoleLogger.Info("Running the Spectrum Based Fault Localizer Application\n");

            // Process command line arguments.
            CommandLineArguments? arguments = CommandLineArguments.Parse(args);
            if (arguments == null)
            {
                return;
            }

            // Get the path to the test project.  If there isn't a .csproj file, then use the directory.
            string? testProjectFile = Directory.EnumerateFiles(arguments.TestProjectDirectory, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
            string testProjectPath = testProjectFile ?? arguments.TestProjectDirectory;

            // Get a list of the tests in the test project directory.
            var discoveredTests = GetListOfTests(arguments.TestProjectDirectory);

            // Get a list of files to add coverage statements to.
            var productionSourceFiles = DiscoverProductionSourceFiles(arguments.ProjectUnderTestDirectory);
            if (productionSourceFiles.Count == 0)
            {
                ConsoleLogger.Warning("No production source files discovered for instrumentation.");
                return;
            }

            EnsureProductionInstrumentation(productionSourceFiles, TemporaryCoverageFileName, arguments.ResetRequested);

            // Run the tests and collect the pass/fail data.
            var testPassFailData = RunTests(discoveredTests, testProjectPath, arguments.VerboseRequested);

            var testCoverage = BuildTestCoverage(discoveredTests);

            Rank rank = new(testCoverage, testPassFailData);
            rank.CalculateTarantula();
            rank.CalculateOchiai();
            rank.CalculateDStar();
            rank.CalculateOp2();
            rank.CalculateJaccard();

            if(arguments.CleanupRequested)
            {
                ConsoleLogger.Info("Cleaning up the instrumentation data.");
                Spectrum.ResetInstrumentation(productionSourceFiles);
                CleanupCoverageData(CoverageDirectory);
            }

            string csvOutputPath = Path.Combine(Directory.GetCurrentDirectory(), "suspiciousness_report.csv");
            rank.WriteSuspiciousnessReport(csvOutputPath);
            ConsoleLogger.Info($"Suspiciousness scores written to {csvOutputPath}.");
        }

        private sealed record DiscoveredTest(string FilePath, string TypeDisplayName, string MethodName, string FullyQualifiedName)
        {
            public string CoverageFileStem => $"{TypeDisplayName}.{MethodName}";
        }

        /// <summary>
        /// Get a list of <see cref="DiscoveredTest"/> in the given test project directory.
        /// </summary>
        /// <param name="testProjectDirectory">The directory to search.</param>
        /// <returns>A list of <see cref="DiscoveredTest"/> objects.</returns>
        private static List<DiscoveredTest> GetListOfTests(string testProjectDirectory)
        {
            // Get all the .cs files in the test project directory ignoreing the bin and obj directories.
            var allTestFiles = Directory.EnumerateFiles(testProjectDirectory, "*.cs", SearchOption.AllDirectories)
                .Where(file => !file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                            && !file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
                .ToList();

            // Get a list of Test methods.
            var discoveredTests = DiscoverTests(allTestFiles);

            if (discoveredTests.Count == 0)
            {
                ConsoleLogger.Error($"No tests discovered in {testProjectDirectory}.");
            }
            else
            {
                ConsoleLogger.Info("Discovered the following tests:");
                foreach (var test in discoveredTests)
                {
                    Console.WriteLine($" - {test.FullyQualifiedName}");
                }
            }

            return discoveredTests;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tests"></param>
        /// <param name="solutionDirectory"></param>
        /// <param name="testProjectDirectory"></param>
        /// <returns></returns>
        private static Dictionary<string, ISet<string>> BuildTestCoverage(IEnumerable<DiscoveredTest> tests)
        {
            var coverage = new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var test in tests)
            {
                string fileKey = test.CoverageFileStem;
                string coverageFileName = Path.Combine(CoverageDirectory, $"{fileKey}.coverage");
                var guidSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);


                if (!File.Exists(coverageFileName))
                {
                    continue;
                }

                // A coverage file was found, read all the guid values and add them to the guid set.
                foreach (var line in File.ReadAllLines(coverageFileName))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        guidSet.Add(line.Trim());
                    }
                }


                if (guidSet.Count == 0)
                {
                    ConsoleLogger.Error($"Coverage file not found or empty for test: {coverageFileName}");
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
        /// Runs each test and returns the pass/fail information.
        /// </summary>
        /// <param name="tests">The <see cref="DiscoveredTest"/> instances to execute.</param>
        /// <param name="testProjectPath">The path to the test csproj file.</param>
        /// <param name="verbose">If verbose is requested, the test output will be displayed.</param>
        private static Dictionary<string, bool> RunTests(
            in IReadOnlyList<DiscoveredTest> tests,
            in string testProjectPath,
            in bool verbose = false)
        {
            ConsoleLogger.Info("Testing in progress...");

            var testPassFailData = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            CleanupCoverageData(CoverageDirectory);
            Directory.CreateDirectory(CoverageDirectory);

            if (!BuildTestProject(testProjectPath, verbose))
            {
                ConsoleLogger.Error("Unable to build test project. Aborting test run.");
                return testPassFailData;
            }

            foreach (var test in tests)
            {
                // Get the coverage filename.
                string coverageFileName = Path.Combine(CoverageDirectory, $"{test.CoverageFileStem}.coverage");

                // Run the test silently
                bool passed = RunTest(testProjectPath, test.FullyQualifiedName, verbose);
                testPassFailData[test.CoverageFileStem] = passed;

                PromoteTemporaryCoverageFile(TemporaryCoverageFileName, coverageFileName);

                // Delete the temporary coverage file if there is one.
                DeleteCoverageFile(TemporaryCoverageFileName);
            }

            return testPassFailData;
        }

        /// <summary>
        /// Checks the files to see if they contain coverage statements.  If they don't,
        /// coverage statements are added to the file.  If they already contain coverage statements,
        /// Verify they are using the correct coverage file.
        /// </summary>
        /// <param name="sourceFiles">The files to ensure instrumentation is added to.</param>
        /// <returns>The list of files with instrumentation added.</returns>
        private static void EnsureProductionInstrumentation(IReadOnlyList<string> sourceFiles, string? coverageFileName = null, bool resetRequested = false)
        {
            // If a reset was requested, we want to erase all instrumentation and coverage data to start fresh.
            if (resetRequested)
            {
                Console.WriteLine("Resetting existing instrumentation artifacts.");
                Spectrum.ResetInstrumentation(sourceFiles);
            }

            foreach (var file in sourceFiles)
            {
                try
                {
                    string sourceCode = File.ReadAllText(file);

                    // Add instrumentation if not already there.
                    if (!ContainsCoverageInstrumentation(sourceCode))
                    {
                        Spectrum.SpectrumAll(file, coverageFileName);
                    }
                    else
                    {
                        // Make sure the correct coverage filename is being used in existing instrumentation.
                        UpdateCoverageFileTarget(file, TemporaryCoverageFileName);
                    }
                }
                catch (IOException ex)
                {
                    ConsoleLogger.Warning($"Failed to instrument '{file}': {ex.Message}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    ConsoleLogger.Warning($"Failed to instrument '{file}': {ex.Message}");
                }
            }
        }


        /// <summary>
        /// Update the coverage file path in the coverage instrumentation statements.
        /// </summary>
        /// <param name="filePath">The file path of the file to update.</param>
        /// <param name="coverageFileName">The path to the coverage file.</param>
        private static void UpdateCoverageFileTarget(string filePath, string coverageFileName)
        {
            string sourceCode = File.ReadAllText(filePath);

            if (!ContainsCoverageInstrumentation(sourceCode))
            {
                return;
            }

            var root = CSharpSyntaxTree.ParseText(sourceCode).GetRoot();
            RewriteCoverageStatements(filePath, root, coverageFileName);
        }

        private static bool ContainsCoverageInstrumentation(string sourceCode)
        {
            return sourceCode.Contains("System.IO.File.AppendAllText", StringComparison.Ordinal);
        }

        private static void DeleteCoverageFile(string coverageFile)
        {
            try
            {
                if (File.Exists(coverageFile))
                {
                    File.Delete(coverageFile);
                }
            }
            catch (Exception ex)
            {
                ConsoleLogger.Warning("Unable to delete existing coverage data.");
                ConsoleLogger.Warning(ex.Message);
            }
        }

        private static void CleanupCoverageData(string coverageDirectory)
        {
            try
            {
                if (Directory.Exists(coverageDirectory))
                {
                    foreach (string file in Directory.EnumerateFiles(coverageDirectory))
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleLogger.Warning("Unable to delete existing coverage data.");
                ConsoleLogger.Warning(ex.Message);
            }
        }

        /// <summary>
        /// Utilizes a <see cref="LogStatementRewriter"/> to direct instrumentation to the specified coverage file.
        /// </summary>
        /// <param name="filePath">The path to the file being rewritten.</param>
        /// <param name="root">The root <see cref="SyntaxNode"/> for the file.</param>
        /// <param name="coverageFileName">The name of the file containing the coverage information.</param>
        /// <returns>The root <see cref="SyntaxNode"/> that contains the modified instrumentation.</returns>
        private static SyntaxNode RewriteCoverageStatements(
            string filePath,
            SyntaxNode root,
            string coverageFileName)
        {
            // Create an LogStatementRewriter to modify the method
            var rewriter = new LogStatementRewriter(coverageFileName);
            var rewrittenRoot = rewriter.Visit(root);

            if (rewrittenRoot == null || SyntaxFactory.AreEquivalent(root, rewrittenRoot))
            {
                return root;
            }

            // Write the file back to save the changes.
            var rewrittenText = rewrittenRoot.NormalizeWhitespace().ToFullString();
            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                writer.Write(rewrittenText);
                writer.Flush();
                stream.Flush(true);
            }

            // Reload the code that was just written, and return the new root.
            var reloadedCode = File.ReadAllText(filePath);
            return CSharpSyntaxTree.ParseText(reloadedCode).GetRoot();
        }

        private static List<string> DiscoverProductionSourceFiles(string projectUnderTestDirectory)
        {
            var files = new List<string>();

            if (!Directory.Exists(projectUnderTestDirectory))
            {
                return files;
            }

            foreach (var file in Directory.EnumerateFiles(projectUnderTestDirectory, "*.cs", SearchOption.AllDirectories))
            {
                if (ShouldSkipProjectFile(file))
                {
                    continue;
                }

                files.Add(file);
            }

            return files;
        }

        private static void PromoteTemporaryCoverageFile(string temporaryCoverageFileName, string finalCoverageFileName)
        {
            try
            {
                if (File.Exists(finalCoverageFileName))
                {
                    File.Delete(finalCoverageFileName);
                }

                File.Move(temporaryCoverageFileName, finalCoverageFileName);
            }
            catch (IOException ex)
            {
                ConsoleLogger.Warning($"Failed to move coverage file '{temporaryCoverageFileName}' to '{finalCoverageFileName}': {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                ConsoleLogger.Warning($"Failed to move coverage file '{temporaryCoverageFileName}' to '{finalCoverageFileName}': {ex.Message}");
            }
        }

        private static bool ShouldSkipProjectFile(string filePath)
        {
            var normalized = Path.GetFullPath(filePath);

            if (normalized.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains($"{Path.DirectorySeparatorChar}coverage{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains($"{Path.DirectorySeparatorChar}.coverage{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var fileName = Path.GetFileName(normalized);
            if (fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
                fileName.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("GlobalUsings.g.cs", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private static bool BuildTestProject(string testProjectPath, bool verbose)
        {
            ConsoleLogger.Info($"Building {testProjectPath}...");

            try
            {
                var startInfo = new ProcessStartInfo("dotnet")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                startInfo.ArgumentList.Add("build");
                startInfo.ArgumentList.Add(testProjectPath);
                startInfo.ArgumentList.Add("--nologo");

                using Process? process = Process.Start(startInfo);
                if (process is null)
                {
                    ConsoleLogger.Error("Failed to start build process.");
                    return false;
                }

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (verbose && !string.IsNullOrEmpty(output))
                {
                    Console.WriteLine(output);
                }

                bool failed = process.ExitCode != 0;

                if (!string.IsNullOrEmpty(error) && (verbose || failed))
                {
                    ConsoleLogger.Warning(error);
                }

                if (failed && !string.IsNullOrEmpty(output) && !verbose)
                {
                    Console.WriteLine(output);
                }

                if (failed)
                {
                    ConsoleLogger.Error("dotnet build failed.");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                ConsoleLogger.Error($"Failed to build test project: {ex.Message}");
                return false;
            }
        }

        private static bool RunTest(string testProjectPath, string fullyQualifiedTestName, bool verbose = false)
        {
            ConsoleLogger.Info($"Running {fullyQualifiedTestName}...");
            try
            {
                string filter = $"FullyQualifiedName~{fullyQualifiedTestName}";

                var startInfo = new ProcessStartInfo("dotnet")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                startInfo.ArgumentList.Add("test");
                startInfo.ArgumentList.Add(testProjectPath);
                startInfo.ArgumentList.Add("--filter");
                startInfo.ArgumentList.Add(filter);
                startInfo.ArgumentList.Add("--no-build");
                startInfo.ArgumentList.Add("--nologo");

                using Process? process = Process.Start(startInfo);
                if (process is null)
                {
                    ConsoleLogger.Error("Failed to start test process.");
                    return false;
                }

                bool exited = process.WaitForExit(30 * 1000);
                if (!exited)
                {
                    ConsoleLogger.Warning("Process timed out. Killing the process...");
                    process.Kill();
                    return false;
                }

                // Show the test output if the user desires it.
                if (verbose)
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
                ConsoleLogger.Error($"Error running test: {ex.Message}");
                return false;
            }
        }

        private class CommandLineArguments
        {
            public string SolutionDirectory { get; set; }
            public string TestProjectDirectory { get; set; }
            public string ProjectUnderTestDirectory { get; set; }
            public bool ResetRequested { get; set; }
            public bool VerboseRequested { get; set; }
            public bool CleanupRequested {  get; set; }

            private CommandLineArguments()
            {
                SolutionDirectory = string.Empty;
                TestProjectDirectory = string.Empty;
                ProjectUnderTestDirectory = string.Empty;
                ResetRequested = false;
                VerboseRequested = false;
                CleanupRequested = false;
            }

            public static CommandLineArguments? Parse(string[] args)
            {
                if (args.Length < 3)
                {
                    ConsoleLogger.Warning("Usage: dotnet run <solutionDirectory> <testProjectName> <projectUnderTestName> [--reset (-r)] [--verbose (-v)] [--cleaup (-c) ");
                    return null;
                }


                // Parse the given commandline arguments.
                CommandLineArguments arguments = new()
                {
                    SolutionDirectory = args[0],
                    ResetRequested = args.Any(arg => string.Equals(arg, "--reset", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "-r", StringComparison.OrdinalIgnoreCase)),
                    VerboseRequested = args.Any(arg => string.Equals(arg, "--verbose", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "-v", StringComparison.OrdinalIgnoreCase)),
                    CleanupRequested = args.Any(arg => string.Equals(arg, "--cleanup", StringComparison.OrdinalIgnoreCase) || string.Equals(arg, "-c", StringComparison.OrdinalIgnoreCase))
                };

                string testProjectName = args[1];
                string projectUnderTestName = args[2];

                if (args.Length > 5)
                {
                    var extras = string.Join(", ", args.Skip(5));
                    ConsoleLogger.Warning($"Ignoring extra arguments: {extras}");
                }

                // Verify the solution directory to be tested exists.
                if (!Directory.Exists(arguments.SolutionDirectory))
                {
                    ConsoleLogger.Error($"Solution directory not found: {arguments.SolutionDirectory}");
                    return null;
                }

                // Verify the test project path exists.  The assumption is that the project name matches the project directory.
                arguments.TestProjectDirectory = Path.Combine(arguments.SolutionDirectory, testProjectName);
                if (!Directory.Exists(arguments.TestProjectDirectory))
                {
                    ConsoleLogger.Error($"Test project directory not found: {arguments.TestProjectDirectory}");
                    return null;
                }

                // Verify the project under test path exists.  The assumption is that the project name matches the project directory.
                arguments.ProjectUnderTestDirectory = Path.Combine(arguments.SolutionDirectory, projectUnderTestName);
                if (!Directory.Exists(arguments.ProjectUnderTestDirectory))
                {
                    ConsoleLogger.Error($"Project under test directory not found: {arguments.ProjectUnderTestDirectory}");
                    return null;
                }

                return arguments;
            }
        }
    }
}
