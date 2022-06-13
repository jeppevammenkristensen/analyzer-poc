using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace PgAnalyzer.Tests.CheckKey;

public class CheckKeyAnalyzerTest : CSharpAnalyzerTest<CheckKeyAnalyzer, XUnitVerifier>
{
    [Fact]
    public async Task CallsContainsKeyDoesntEmit()
    {
        TestCode =
            "using System.Collections.Generic; namespace Supra { public class Testing { public void Test(){ var dict = new Dictionary<string,string>(); var st = dict.ContainsKey(\"hello\"); var res = dict[\"hello\"]; }}}";
        ExpectedDiagnostics.Clear();
        await RunAsync();
    }
    
    [Fact]
    public async Task Init()
    {
        TestCode =
            "using System.Collections.Generic; namespace Supra { public class Testing { public void Test(){ IDictionary<string,string> dict = new Dictionary<string,string>(); var res = dict[\"hello\"]; }}}";
        ExpectedDiagnostics.Clear();
        await RunAsync();
    }
    
    [Fact]
    public async Task Init2()
    {
        TestCode =
            "using System.Collections.Generic; namespace Supra { public class Testing { public void Test(){ IDictionary<string,string> dict = new Dictionary<string,string>(); if (dict.ContainsKey(\"hello\")) { var res = dict[\"hello\"];} }}}";
        ExpectedDiagnostics.Clear();
        await RunAsync();
    }

    
}

public class CheckKeyCodeFixTest : CSharpCodeFixTest<CheckKeyAnalyzer, CheckKeyCodeFix, XUnitVerifier>
{
    [Fact]
    public async Task Poc()
    {
        TestCode =
            "using System.Collections.Generic; namespace Supra { public class Testing { public void Test(){ IDictionary<string,string> dict = new Dictionary<string,string>(); var res = dict[\"hello\"]; }}}";

        ExpectedDiagnostics.Add(new DiagnosticResult(Descriptors.CheckKey.Id, DiagnosticSeverity.Warning)
            .WithMessage("Did not check if the key was present.")
            .WithSpan(1, 173, 1, 186));

        FixedCode = "public class CustomErrorException : System.Exception { }";

        await RunAsync();
    }
}

