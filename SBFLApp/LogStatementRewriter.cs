using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SBFLApp
{
    internal class LogStatementRewriter : CSharpSyntaxRewriter
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
