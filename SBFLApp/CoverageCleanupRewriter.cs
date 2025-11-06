using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SBFLApp
{
    /// <summary>
    /// Used to remove coverage statements.
    /// </summary>
    internal class CoverageCleanupRewriter : CSharpSyntaxRewriter
    {
        /// <summary>
        /// Removes the statement if it is a coverage statement, or returns the original
        /// expression if it isn't.
        /// </summary>
        /// <param name="node">The <see cref="ExpressionStatementSyntax"/> to check.</param>
        /// <returns>Null if this is a coverage statement, the original statement if it isn't.</returns>
        public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            // If this is a coverage statements, 
            if (IsCoverageLoggingStatement(node))
            {
                // Remove the statement
                return null;
            }

            // Return the original node.
            return base.VisitExpressionStatement(node);
        }

        /// <summary>
        /// Checks to see if the given statement is a coverage statement.
        /// </summary>
        /// <param name="node">The <see cref="ExpressionStatementSyntax"/> to check.</param>
        /// <returns>True if this is a coverage statement.  False otherwise.</returns>
        private static bool IsCoverageLoggingStatement(ExpressionStatementSyntax node)
        {
            // Verify this is an actual statement of executable code.
            if (node.Expression is not InvocationExpressionSyntax invocation ||
                invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            {
                return false;
            }

            // If this statement isn't a coverage statement, return false.
            if (!string.Equals(memberAccess.ToString(), "System.IO.File.AppendAllText", StringComparison.Ordinal))
            {
                return false;
            }

            // Verify there are two arguments being passed to the AppendAllText method.
            var arguments = invocation.ArgumentList.Arguments;
            if (arguments.Count != 2)
            {
                return false;
            }

            // Verify the first argument, which should be the coverage file name, ends with ".coverage"
            if (arguments[0].Expression is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                var pathValue = literal.Token.ValueText;
                return pathValue.EndsWith("coverage.tmp", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}
