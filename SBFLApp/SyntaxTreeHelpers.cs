using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SBFLApp
{
    internal class SyntaxTreeHelpers
    {
        /// <summary>
        /// Look through the decendents of the root <see cref="SyntaxNode"/> for the node specified
        /// by the test parameter.
        /// </summary>
        /// <param name="root">The <see cref="SyntaxNode"/> to search.</param>
        /// <param name="methodName">The method name to search for.</param>
        /// <param name="typeDisplayName">The type display name to search for.</param>
        /// <returns><see cref="MethodDeclarationSyntax"/> if one is found, or null if no method is found.</returns>
        public static MethodDeclarationSyntax? FindMethod(SyntaxNode root, string methodName, string typeDisplayName)
        {
            return root
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(method =>
                    string.Equals(method.Identifier.Text, methodName, StringComparison.Ordinal) &&
                    string.Equals(GetTypeDisplayName(method), typeDisplayName, StringComparison.Ordinal));
        }

        /// <summary>
        /// Look through the decendents of the root <see cref="SyntaxNode"/> for the node specified
        /// by the test parameter.
        /// </summary>
        /// <param name="root">The <see cref="SyntaxNode"/> to search.</param>
        /// <param name="methodName">The method name to search for.</param>
        /// <returns><see cref="MethodDeclarationSyntax"/> if one is found, or null if no method is found.</returns>
        public static MethodDeclarationSyntax? FindMethod(SyntaxNode root, string methodName = "")
        {
            return root
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(method =>
                    string.Equals(method.Identifier.Text, methodName, StringComparison.Ordinal));
        }

        /// <summary>
        /// Get's the Identifier Text of the given <see cref="SyntaxNode"/>.  The value
        /// will be found by getting the <see cref="TypeDeclarationSyntax"/> ancestors of the
        /// given node and concatenating their Identifier.Text properties with the '.' delimeter.
        /// </summary>
        /// <param name="node">The <see cref="SyntaxNode"/> to get the display name of.</param>
        /// <returns>A string representing the display name of the given <see cref="SyntaxNode"/>.</returns>
        public static string GetTypeDisplayName(SyntaxNode node)
        {
            var typeNames = node
                .Ancestors()
                .OfType<TypeDeclarationSyntax>()
                .Select(type => type.Identifier.Text)
                .Reverse()
                .ToList();

            return typeNames.Count == 0 ? string.Empty : string.Join('.', typeNames);
        }

        /// <summary>
        /// Get the namespace of the given <see cref="SyntaxNode"/>.
        /// </summary>
        /// <param name="node">The <see cref="SyntaxNode"/> to get the namespace of.</param>
        /// <returns>The namespace of the given node or <see cref="string.Empty"/> if the node has no namespace declared.</returns>
        public static string GetNamespace(SyntaxNode node)
        {
            var namespaceNode = node
                .Ancestors()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .FirstOrDefault();

            return namespaceNode?.Name.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Get's the short name of the specified <see cref="AttributeSyntax"/>.
        /// </summary>
        /// <param name="attribute">The <see cref="AttributeSyntax"/> to shorten.</param>
        /// <returns>A string representing the given <see cref="AttributeSyntax"/>.</returns>
        public static string GetAttributeShortName(AttributeSyntax attribute)
        {
            return attribute.Name switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.Text,
                QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
                AliasQualifiedNameSyntax alias => alias.Name.Identifier.Text,
                _ => attribute.Name.ToString(),
            };
        }
    }
}
