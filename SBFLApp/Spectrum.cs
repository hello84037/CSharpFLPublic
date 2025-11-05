using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;

namespace SBFLApp
{
    internal class Spectrum
    {
        /// <summary>
        /// Reads the source into a syntax tree representation.  Searches for the method to modify.
        /// If the method is located, then coverage data is written to each statement in the method.
        /// The source file is re-written using the modified syntax tree representation.
        /// </summary>
        /// <param name="filePath">The path to the file to be modified.</param>
        /// <param name="methodName">The name of the method to modify.</param>
        /// <param name="coverageFileName">The name of the coverage file to use in the modified statement.</param>
        public static void SpectrumMethod(string filePath, string methodName, string? coverageFileName = null)
        {
            // Read in the source code that contains the method.
            var sourceCode = File.ReadAllText(filePath);
            
            // Create an abstract syntax tree representation of the source code.
            var tree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = tree.GetRoot();

            // Search for the method to modify.
            var method = SyntaxTreeHelpers.FindMethod(root, methodName);

            // Method wasn't found, return;
            if (method == null)
            {
                Console.WriteLine($"Method '{methodName}' not found.");
                return;
            }

            // Inject the coverage statements into the method.
            var rewriter = new CoverageInjector(methodName, coverageFileName, null, filePath);
            if (rewriter.Visit(root) is not CompilationUnitSyntax rewrittenRoot)
            {
                Console.WriteLine($"Failed to rewrite method '{methodName}' in '{filePath}'.");
                return;
            }

            // Get the method from the rewritten/modified root.
            var updatedMethod = rewrittenRoot
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.Text == methodName);

            if (updatedMethod is null)
            {
                Console.WriteLine($"Rewritten method '{methodName}' not found in '{filePath}'.");
                return;
            }

            // Write the modified file back to the file system.
            var newRoot = rewrittenRoot;
            File.WriteAllText(filePath, newRoot.NormalizeWhitespace().ToFullString());
            Console.WriteLine($"Injected logging into all statements in '{methodName}' in '{filePath}'.");
        }

        /// <summary>
        /// Inject coverage logging into all methods within the specified file.
        /// </summary>
        /// <param name="filePath"></param>
        public static void SpectrumAll(string filePath)
        {
            var sourceCode = File.ReadAllText(filePath);
            var tree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = tree.GetRoot();
            var rewriter = new CoverageInjector(sourceFilePath: filePath);
            var newRoot = rewriter.Visit(root);
            File.WriteAllText(filePath, newRoot.NormalizeWhitespace().ToFullString());
            Console.WriteLine($"Injected logging into all methods in '{filePath}'.");
        }

        // Inject coverage logging into all methods in the file using the provided testName; create coverage folder and file
        public static void SpectrumAllForTest(string filePath, string testName)
        {
            var sourceCode = File.ReadAllText(filePath);
            var tree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = tree.GetRoot();

            var guidCollector = new List<string>();
            var binDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var coverageFolder = Path.Combine(binDirectory, $"{testName}.coverage");
            Directory.CreateDirectory(coverageFolder);
            var coverageFilePath = Path.Combine(coverageFolder, $"{testName}.coverage");

            var rewriter = new CoverageInjector(null, coverageFilePath, guidCollector, filePath);
            var newRoot = rewriter.Visit(root);

            File.WriteAllText(filePath, newRoot.NormalizeWhitespace().ToFullString());
            Console.WriteLine($"Injected logging into all methods in '{filePath}' using coverage file '{coverageFilePath}'.");

            File.WriteAllLines(coverageFilePath, guidCollector);
            Console.WriteLine($"Written {guidCollector.Count} GUIDs to '{coverageFilePath}'.");
        }

        public static void ResetInstrumentation(string rootPath, params string[] additionalPaths)
        {
            var pathsToProcess = new List<string>();

            if (!string.IsNullOrWhiteSpace(rootPath))
            {
                pathsToProcess.Add(rootPath);
            }

            if (additionalPaths != null && additionalPaths.Length > 0)
            {
                pathsToProcess.AddRange(additionalPaths.Where(p => !string.IsNullOrWhiteSpace(p)));
            }

            if (pathsToProcess.Count == 0)
            {
                Console.WriteLine("No paths provided for instrumentation reset.");
                return;
            }

            var cleanupRewriter = new CoverageCleanupRewriter();
            var processedRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int sourceFilesCleaned = 0;
            int coverageFilesRemoved = 0;
            int coverageDirectoriesRemoved = 0;

            foreach (var path in pathsToProcess)
            {
                if (!Directory.Exists(path))
                {
                    Console.WriteLine($"Reset skipped missing directory: {path}");
                    continue;
                }

                var normalizedPath = Path.GetFullPath(path);
                if (!processedRoots.Add(normalizedPath))
                {
                    continue;
                }

                sourceFilesCleaned += RemoveInstrumentationFromSource(normalizedPath, cleanupRewriter);
                coverageFilesRemoved += DeleteCoverageFiles(normalizedPath);
                coverageDirectoriesRemoved += DeleteCoverageDirectories(normalizedPath);
            }

            GuidMappingStore.Clear();

            Console.WriteLine($"Instrumentation reset complete. Cleaned {sourceFilesCleaned} source files, removed {coverageFilesRemoved} coverage files, and deleted {coverageDirectoriesRemoved} coverage directories.");
        }

        // Instrument every C# file in the project for coverage and save modified copies to a coverage folder
        public static void SpectrumProject(string projectPath, string testName)
        {
            var coverageFolder = Path.Combine(projectPath, "coverage");
            Directory.CreateDirectory(coverageFolder);

            var coverageFolderFullPath = Path.GetFullPath(coverageFolder) + Path.DirectorySeparatorChar;

            var allCsFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories)
                .Where(f =>
                    !f.EndsWith("Program.cs", StringComparison.OrdinalIgnoreCase) &&
                    !f.Contains(@"\obj\") &&
                    !f.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) &&
                    !f.EndsWith("AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase) &&
                    !f.EndsWith("GlobalUsings.g.cs", StringComparison.OrdinalIgnoreCase) &&
                    !Path.GetFullPath(f).StartsWith(coverageFolderFullPath, StringComparison.OrdinalIgnoreCase)
                )
                .ToList();

            foreach (var file in allCsFiles)
            {
                Console.WriteLine($"Processing file: {file}");

                var sourceCode = File.ReadAllText(file);
                var tree = CSharpSyntaxTree.ParseText(sourceCode);
                var root = tree.GetRoot();

                var rewriter = new CoverageInjector(sourceFilePath: file);
                var newRoot = rewriter.Visit(root);

                var fileName = Path.GetFileName(file);
                var destFilePath = Path.Combine(coverageFolder, fileName);

                File.WriteAllText(destFilePath, newRoot.NormalizeWhitespace().ToFullString());

                // Removed SpectrumAll(destFilePath) here to avoid double instrumentation

                Console.WriteLine($"Written instrumented file to: {destFilePath}");
            }
        }


        // Detect and instrument test files within known test folders in the project path
        public static void SpectrumTests(string projectPath)
        {
            var testFolders = DetectTestFolders(projectPath);
            if (testFolders.Count == 0)
            {
                Console.WriteLine("No test folders detected.");
                return;
            }

            foreach (var testFolder in testFolders)
            {
                Console.WriteLine($"Processing detected test folder: {testFolder}");

                var testFiles = Directory.GetFiles(testFolder, "*.cs", SearchOption.AllDirectories);

                foreach (var file in testFiles)
                {
                    Console.WriteLine($"Processing test file: {file}");

                    var sourceCode = File.ReadAllText(file);
                    var tree = CSharpSyntaxTree.ParseText(sourceCode);
                    var root = tree.GetRoot();

                    var classNodes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();

                    bool fileHasTestClassAttribute = false;
                    var testMethodsInFile = new List<string>();

                    foreach (var classNode in classNodes)
                    {
                        var hasTestClassAttribute = classNode.AttributeLists
                            .SelectMany(a => a.Attributes)
                            .Any(attr => IsTestClassAttribute(attr));

                        if (hasTestClassAttribute)
                        {
                            fileHasTestClassAttribute = true;
                            break;
                        }

                        foreach (var method in classNode.Members.OfType<MethodDeclarationSyntax>())
                        {
                            bool isTestMethod = method.AttributeLists
                                .SelectMany(a => a.Attributes)
                                .Any(attr => IsTestMethodAttribute(attr));

                            if (isTestMethod)
                            {
                                testMethodsInFile.Add(method.Identifier.Text);
                            }
                        }
                    }

                    if (fileHasTestClassAttribute)
                    {
                        Console.WriteLine($"Injecting coverage for whole test class file: {file}");
                        SpectrumAll(file);
                    }
                    else if (testMethodsInFile.Count > 0)
                    {
                        foreach (var methodName in testMethodsInFile)
                        {
                            Console.WriteLine($"Injecting coverage for test method '{methodName}' in file: {file}");
                            SpectrumMethod(file, methodName);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"No test classes or test methods found in file: {file}");
                    }
                }
            }
        }

        // Search for common test folder names under the project path and return matching directories
        private static List<string> DetectTestFolders(string projectPath)
        {
            var commonTestFolderNames = new[]
            {
                "test", "tests", "unittest", "unittests", "spec", "specs", "integrationtests"
            };

            var foundTestFolders = new List<string>();

            var allDirs = Directory.GetDirectories(projectPath, "*", SearchOption.AllDirectories);

            foreach (var dir in allDirs)
            {
                var folderName = Path.GetFileName(dir).ToLowerInvariant();
                if (commonTestFolderNames.Contains(folderName))
                {
                    foundTestFolders.Add(dir);
                }
            }

            if (foundTestFolders.Count == 0)
            {
                var fallback = Path.Combine(projectPath, "Tests");
                if (Directory.Exists(fallback))
                    foundTestFolders.Add(fallback);
            }

            return foundTestFolders;
        }

        // Determine if an attribute indicates a test class (e.g., [TestClass], [TestFixture])
        private static bool IsTestClassAttribute(AttributeSyntax attr)
        {
            var name = attr.Name.ToString();
            return Regex.IsMatch(name, @"TestClass", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(name, @"TestFixture", RegexOptions.IgnoreCase);
        }

        // Determine if an attribute indicates a test method (e.g., [TestMethod], [Fact], [Test])
        private static bool IsTestMethodAttribute(AttributeSyntax attr)
        {
            var name = attr.Name.ToString();
            return Regex.IsMatch(name, @"TestMethod|Fact|Test", RegexOptions.IgnoreCase);
        }

        private static int RemoveInstrumentationFromSource(string rootPath, CoverageCleanupRewriter rewriter)
        {
            var allCsFiles = Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories)
                .Where(file => !IsInIgnoredDirectory(file))
                .ToList();

            int cleanedFiles = 0;

            foreach (var file in allCsFiles)
            {
                var originalText = File.ReadAllText(file);
                var tree = CSharpSyntaxTree.ParseText(originalText);
                var root = tree.GetRoot();
                SyntaxNode? cleanedRoot = rewriter.Visit(root);

                if (cleanedRoot == null || SyntaxFactory.AreEquivalent(root, cleanedRoot))
                {
                    continue;
                }

                var newText = cleanedRoot.ToFullString();
                if (!string.Equals(originalText, newText, StringComparison.Ordinal))
                {
                    File.WriteAllText(file, newText);
                }

                cleanedFiles++;
            }

            return cleanedFiles;
        }

        private static bool IsInIgnoredDirectory(string filePath)
        {
            var normalizedPath = filePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            return normalizedPath.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                   normalizedPath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                   normalizedPath.Contains($"{Path.DirectorySeparatorChar}coverage{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                   normalizedPath.Contains($".coverage{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
        }

        private static int DeleteCoverageFiles(string rootPath)
        {
            var coverageFiles = Directory.GetFiles(rootPath, "*.coverage", SearchOption.AllDirectories);
            int deletedCount = 0;

            foreach (var file in coverageFiles)
            {
                try
                {
                    File.Delete(file);
                    deletedCount++;
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"Failed to delete coverage file '{file}': {ex.Message}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"Failed to delete coverage file '{file}': {ex.Message}");
                }
            }

            return deletedCount;
        }

        private static int DeleteCoverageDirectories(string rootPath)
        {
            var directories = Directory.GetDirectories(rootPath, "*.coverage", SearchOption.AllDirectories)
                .Concat(Directory.GetDirectories(rootPath, "coverage", SearchOption.AllDirectories))
                .ToList();

            int deletedCount = 0;
            var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var dir in directories)
            {
                if (!processed.Add(dir))
                {
                    continue;
                }

                if (!Directory.Exists(dir))
                {
                    continue;
                }

                try
                {
                    Directory.Delete(dir, true);
                    deletedCount++;
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"Failed to delete coverage directory '{dir}': {ex.Message}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"Failed to delete coverage directory '{dir}': {ex.Message}");
                }
            }

            return deletedCount;
        }
    }
}
