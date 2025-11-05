using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SBFLApp
{
    internal class LogStatementRewriter : CSharpSyntaxRewriter
    {
        private readonly string _coverageFileName;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coverageFileName"></param>
        public LogStatementRewriter(string coverageFileName)
        {
            _coverageFileName = coverageFileName;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (!IsCoverageLogInvocation(node))
            {
                return base.VisitInvocationExpression(node) ?? node;
            }

            var argumentList = node.ArgumentList;
            if (argumentList.Arguments.Count < 2)
            {
                return base.VisitInvocationExpression(node) ?? node;
            }

            var updatedArguments = argumentList.Arguments;

            var fileLiteral = SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(_coverageFileName));

            var firstArgument = updatedArguments[0]
                .WithExpression(fileLiteral);

            updatedArguments = updatedArguments.Replace(updatedArguments[0], firstArgument);

            var secondArgument = updatedArguments[1];
            if (!ContainsEnvironmentNewLine(secondArgument.Expression))
            {
                var newlineExpression = SyntaxFactory.ParseExpression("System.Environment.NewLine");
                var appendedExpression = SyntaxFactory.BinaryExpression(
                    SyntaxKind.AddExpression,
                    (ExpressionSyntax)secondArgument.Expression,
                    newlineExpression);

                var updatedSecondArgument = secondArgument.WithExpression(appendedExpression);
                updatedArguments = updatedArguments.Replace(secondArgument, updatedSecondArgument);
            }

            var updatedArgumentList = argumentList.WithArguments(updatedArguments);

            return node.WithArgumentList(updatedArgumentList);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static bool IsCoverageLogInvocation(InvocationExpressionSyntax node)
        {
            if (node.Expression is not MemberAccessExpressionSyntax memberAccess)
            {
                return false;
            }

            var target = memberAccess.ToString();
            return target == "System.IO.File.AppendAllText";
        }


        /// <summary>
        /// Checks to see if the given expression contains "Environment.NewLine"
        /// </summary>
        /// <param name="expression">The expression to search.</param>
        /// <returns>True if the expression contains "Environment.NewLine" and false otherwise.</returns>
        private static bool ContainsEnvironmentNewLine(ExpressionSyntax expression)
        {
            return expression.ToString().Contains("Environment.NewLine", StringComparison.Ordinal);
        }
    }
}
