﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Roslynator.CSharp;
using Roslynator.CSharp.Analysis;
using Roslynator.CSharp.CodeFixes;
using Xunit;
using static Roslynator.Tests.CSharpDiagnosticVerifier;

namespace Roslynator.Analyzers.Tests
{
    public static class RCSTests
    {
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

        ////[Fact]
        public static void TestDiagnosticWithCodeFix()
        {
            VerifyDiagnosticAndCodeFix(
@"
",
@"
",
                descriptor: default,
                analyzer: default,
                codeFixProvider: default);
        }

        //[Theory]
        //[InlineData("", "")]
        public static void TestDiagnosticWithCodeFix2(string fixableCode, string fixedCode)
        {
            VerifyDiagnosticAndCodeFix(
                SourceTemplate,
                fixableCode,
                fixedCode,
                descriptor: default,
                analyzer: default,
                codeFixProvider: default);
        }

        //[Fact]
        public static void TestNoDiagnostic()
        {
            VerifyNoDiagnostic(
@"
",
                descriptor: default,
                analyzer: default);
        }

        //[Theory]
        //[InlineData("")]
        public static void TestNoDiagnostic2(string fixableCode)
        {
            VerifyNoDiagnostic(
                SourceTemplate,
                fixableCode,
                descriptor: default,
                analyzer: default);
        }
    }
}