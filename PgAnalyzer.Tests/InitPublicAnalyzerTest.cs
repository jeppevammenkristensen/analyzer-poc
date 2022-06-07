using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace PgAnalyzer.Tests;

public class InitPublicCodeFixTest : CSharpCodeFixTest<InitPublicAnalyzer, InitPublicCodeFix, XUnitVerifier>
{
    [Fact]
    public async Task Poc()
    {
        TestCode =
            "public class Test { public void DoTest(){ var someClass = new Someclass(); someClass.Name = \"5\"; someClass.Other = 5; } } public class Someclass { public string Name {get;set;} public int Other {get;set; } public int TheThird {get;set;}}";

        ExpectedDiagnostics.Add(new DiagnosticResult(Descriptors.InitPublic.Id, DiagnosticSeverity.Info)
            .WithMessage("Not all properties has been set. Missing are TheThird.")
            .WithSpan(1, 63, 1, 72));

        FixedCode = "public class CustomErrorException : System.Exception { }";

        await RunAsync();
    }
}

public class InitPublicAnalyzerTest : CSharpAnalyzerTest<InitPublicAnalyzer, XUnitVerifier>
{
    [Fact]
    public async Task Init()
    {
        TestCode =
            "public class Test { public void DoTest(){ var someClass = new Someclass(); someClass.Name = \"5\"; someClass.Other = 5; } } public class Someclass { public string Name {get;set;} public int Other {get;set; } public int TheThird {get;set;}}";
        ExpectedDiagnostics.Add(new DiagnosticResult(Descriptors.InitPublic.Id, DiagnosticSeverity.Info)
            .WithMessage("Not all properties has been set. Missing are TheThird.")
            .WithSpan(1, 63, 1, 72));
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