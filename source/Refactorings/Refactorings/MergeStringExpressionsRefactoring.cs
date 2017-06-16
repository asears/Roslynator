﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Roslynator.CSharp.Refactorings
{
    internal static class MergeStringExpressionsRefactoring
    {
        public static async Task ComputeRefactoringAsync(RefactoringContext context, BinaryExpressionSelection binaryExpressionSelection)
        {
            if (binaryExpressionSelection.BinaryExpression.IsKind(SyntaxKind.AddExpression))
            {
                SemanticModel semanticModel = await context.GetSemanticModelAsync().ConfigureAwait(false);

                StringExpressionChain chain;
                if (StringExpressionChain.TryCreate(binaryExpressionSelection, semanticModel, context.CancellationToken, out chain))
                {
                    ComputeRefactoring(context, chain);
                }
            }
        }

        public static async Task ComputeRefactoringAsync(RefactoringContext context, BinaryExpressionSyntax binaryExpression)
        {
            if (binaryExpression.IsKind(SyntaxKind.AddExpression))
            {
                SemanticModel semanticModel = await context.GetSemanticModelAsync().ConfigureAwait(false);

                StringExpressionChain chain;
                if (StringExpressionChain.TryCreate(binaryExpression, semanticModel, context.CancellationToken, out chain))
                {
                    ComputeRefactoring(context, chain);
                }
            }
        }

        private static void ComputeRefactoring(RefactoringContext context, StringExpressionChain chain)
        {
            if (chain.ContainsNonLiteralExpression)
            {
                if (chain.ContainsLiteralExpression || chain.ContainsInterpolatedStringExpression)
                {
                    context.RegisterRefactoring(
                        "Merge string expressions",
                        cancellationToken => ToInterpolatedStringAsync(context.Document, chain, cancellationToken));
                }
            }
            else if (chain.ContainsLiteralExpression)
            {
                context.RegisterRefactoring(
                    "Merge string literals",
                    cancellationToken => ToStringLiteralAsync(context.Document, chain, multiline: false, cancellationToken: cancellationToken));

                if (chain.OriginalExpression
                        .DescendantTrivia(chain.Span ?? chain.OriginalExpression.Span)
                        .Any(f => f.IsKind(SyntaxKind.EndOfLineTrivia)))
                {
                    context.RegisterRefactoring(
                        "Merge string literals into multiline string literal",
                        cancellationToken => ToStringLiteralAsync(context.Document, chain, multiline: true, cancellationToken: cancellationToken));
                }
            }
        }

        private static Task<Document> ToInterpolatedStringAsync(
            Document document,
            StringExpressionChain chain,
            CancellationToken cancellationToken)
        {
            InterpolatedStringExpressionSyntax newExpression = chain.ToInterpolatedString();

            return RefactorAsync(document, chain, newExpression, cancellationToken);
        }

        public static Task<Document> ToStringLiteralAsync(
            Document document,
            StringExpressionChain chain,
            bool multiline,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ExpressionSyntax newExpression = (multiline)
                ? chain.ToMultilineStringLiteral()
                : chain.ToStringLiteral();

            return RefactorAsync(document, chain, newExpression, cancellationToken);
        }

        private static Task<Document> RefactorAsync(
            Document document,
            StringExpressionChain chain,
            ExpressionSyntax expression,
            CancellationToken cancellationToken)
        {
            if (chain.Span.HasValue)
            {
                TextSpan span = chain.Span.Value;

                int start = chain.OriginalExpression.SpanStart;

                string s = chain.OriginalExpression.ToString();

                s = s.Remove(span.Start - start)
                    + expression
                    + s.Substring(span.End - start);

                expression = SyntaxFactory.ParseExpression(s);
            }

            expression = expression
                .WithTriviaFrom(chain.OriginalExpression)
                .WithFormatterAnnotation();

            return document.ReplaceNodeAsync(chain.OriginalExpression, expression, cancellationToken);
        }
    }
}
