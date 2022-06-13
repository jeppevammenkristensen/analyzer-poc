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
            "using System.Collections.Generic; namespace Supra { public class Testing { public void Test(){ var dict = new Dictionary<string,string>(); var res = dict[\"hello\"]; }}}";
        ExpectedDiagnostics.Clear();
        await RunAsync();
    }

    [Fact]
    public async Task InitInCodeAndFromConstructor_AllPropertiesInitalized_NoDiagnosticEmitted()
    {
        TestCode =
            "public class Test { public void DoTest(){ var someClass = new Someclass(){ TheThird = 43}; someClass.Name = \"5\"; someClass.Other = 5; } } public class Someclass { public string Name {get;set;} public int Other {get;set; } public int TheThird {get;set;}}";
        ExpectedDiagnostics.Clear();
        await RunAsync();
    }
}