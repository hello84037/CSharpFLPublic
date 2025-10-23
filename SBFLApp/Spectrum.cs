using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SBFLApp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

namespace MathApp
{
    internal class Spectrum
    {


        // Inject coverage logging into a specific method in a file
        public static void SpectrumMethod(string filePath, string methodName)
        {
            var sourceCode = File.ReadAllText(filePath);
            var tree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = tree.GetRoot();
            var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault(m => m.Identifier.Text == methodName);
            if (method == null)
            {
                Console.WriteLine($"Method '{methodName}' not found.");
                return;
            }
            var rewriter = new CoverageInjector(methodName, null, null, filePath);
            var newMethod = (MethodDeclarationSyntax)rewriter.Visit(method);
            var newRoot = root.ReplaceNode(method, newMethod);
            File.WriteAllText(filePath, newRoot.NormalizeWhitespace().ToFullString());
            Console.WriteLine($"Injected logging into all statements in '{methodName}' in '{filePath}'.");
        }

        // Inject coverage logging into all methods within the specified file
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

            var rewriter = new CoverageInjector(null, testName, guidCollector, filePath);
            var newRoot = rewriter.Visit(root);

            File.WriteAllText(filePath, newRoot.NormalizeWhitespace().ToFullString());
            Console.WriteLine($"Injected logging into all methods in '{filePath}' using coverage file '{coverageFilePath}'.");

            File.WriteAllLines(coverageFilePath, guidCollector);
            Console.WriteLine($"Written {guidCollector.Count} GUIDs to '{coverageFilePath}'.");
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

        // Rewrites C# syntax tree nodes to inject coverage logging statements
        private class CoverageInjector : CSharpSyntaxRewriter
        {
            private readonly string? _targetMethodName;
            private readonly string? _testName;
            private readonly List<string>? _guidCollector;
            private readonly string? _sourceFilePath;
            private readonly Stack<string> _namespaceStack = new();
            private readonly Stack<string> _typeStack = new();
            private string _currentMethodName = "";

            public CoverageInjector(string? methodName = null, string? testName = null, List<string>? guidCollector = null, string? sourceFilePath = null)
            {
                _targetMethodName = methodName;
                _testName = testName;
                _guidCollector = guidCollector;
                _sourceFilePath = sourceFilePath;
            }

            public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
            {
                _namespaceStack.Push(node.Name.ToString());
                var result = base.VisitNamespaceDeclaration(node);
                _namespaceStack.Pop();
                return result ?? node;
            }

            public override SyntaxNode VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
            {
                _namespaceStack.Push(node.Name.ToString());
                var result = base.VisitFileScopedNamespaceDeclaration(node);
                _namespaceStack.Pop();
                return result ?? node;
            }

            public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                _typeStack.Push(node.Identifier.Text);
                var result = base.VisitClassDeclaration(node);
                _typeStack.Pop();
                return result ?? node;
            }

            public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
            {
                _typeStack.Push(node.Identifier.Text);
                var result = base.VisitStructDeclaration(node);
                _typeStack.Pop();
                return result ?? node;
            }

            // Visit method declarations, process only the target method if specified
            public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (_targetMethodName != null && node.Identifier.Text != _targetMethodName)
                    return node;

                _currentMethodName = node.Identifier.Text;
                return base.VisitMethodDeclaration(node) ?? node;
            }

            // Visit blocks and inject logging statements before each original statement
            public override SyntaxNode VisitBlock(BlockSyntax node)
            {
                var newStatements = new List<StatementSyntax>();

                foreach (var statement in node.Statements)
                {
                    // Skip instrumentation if this statement is already a logging statement
                    var statementText = statement.ToString();
                    if (statementText.Contains("System.IO.File.AppendAllText"))
                    {
                        newStatements.Add(statement);
                        continue;
                    }

                    var guid = Guid.NewGuid().ToString();
                    var coverageFilePath = _testName ?? $"{_currentMethodName}.coverage";

                    var logStatement = SyntaxFactory.ParseStatement(
                        $"System.IO.File.AppendAllText(\"{coverageFilePath}\", \"{guid}\");"
                    );

                    _guidCollector?.Add(guid);
                    var qualifiedName = GetQualifiedMethodName();
                    if (!string.IsNullOrEmpty(qualifiedName))
                    {
                        var sourceFileName = string.IsNullOrWhiteSpace(_sourceFilePath)
                            ? null
                            : Path.GetFileName(_sourceFilePath);
                        GuidMappingStore.AddMapping(guid, qualifiedName, sourceFileName);
                    }
                    var visitedStatement = (StatementSyntax)Visit(statement);
                    newStatements.Add(logStatement);
                    newStatements.Add(visitedStatement);
                }

                return node.WithStatements(SyntaxFactory.List(newStatements));
            }

            private string GetQualifiedMethodName()
            {
                if (string.IsNullOrEmpty(_currentMethodName))
                {
                    return string.Empty;
                }

                var namespacePrefix = _namespaceStack.Count > 0
                    ? string.Join('.', _namespaceStack.Reverse()) + "."
                    : string.Empty;

                var typePrefix = _typeStack.Count > 0
                    ? string.Join('.', _typeStack.Reverse()) + "."
                    : string.Empty;

                return $"{namespacePrefix}{typePrefix}{_currentMethodName}";
            }


        }
    }
}
