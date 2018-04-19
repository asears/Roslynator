﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Roslynator.CSharp.Refactorings;
using Xunit;
using static Roslynator.Tests.CSharpCodeRefactoringVerifier;

namespace Roslynator.Refactorings.Tests
{
    public static class RRTests
    {
        private const string RefactoringId = RefactoringIdentifiers.AddBraces;

        private const string SourceTemplate = @"
using System.Collections.Generic;
using System.Linq;

class C
{
    void M()
    {
    }
}
";

        //[Fact]
        public static void TestCodeRefactoring()
        {
            VerifyCodeRefactoring(
@"
",
@"
",
                span: default,
                codeRefactoringProvider: default,
                equivalenceKey: RefactoringId);
        }

        //[Theory]
        //[InlineData("", "")]
        public static void TestCodeRefactoring2(string fixableCode, string fixedCode)
        {
            VerifyCodeRefactoring(
                SourceTemplate,
                fixableCode,
                fixedCode,
                codeRefactoringProvider: default,
                equivalenceKey: RefactoringId);
        }

        //[Fact]
        public static void TestNoCodeRefactoring()
        {
            VerifyNoCodeRefactoring(
@"
",
                span: default,
                codeRefactoringProvider: default,
                equivalenceKey: RefactoringId);
        }
    }
}