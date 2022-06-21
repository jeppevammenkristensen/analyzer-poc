using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using PgAnalyzer.AssertToFluent;
using Xunit;
using FluentAssertions;

namespace PgAnalyzer.Tests.AssertToFluent;
public class AssertToFluentCodeFixTests : CSharpCodeFixTest<AssertToFluentAnalyzer, AssertToFluentCodeFix, XUnitVerifier>
{
    [Fact]
    public async Task Poc()
    {
        TestCode = "using Xunit; namespace Supra { public class Something { public string Name {get;set;}}  public class Testing { public void Test(){ Something st = new(); Assert.True(st.Name == \"hello\");  }}}";
        TestState.AdditionalReferences.Add(typeof(Xunit.Assert).Assembly);
        TestState.AdditionalReferences.Add(typeof(FluentAssertions.AssertionExtensions).Assembly);
        ExpectedDiagnostics.Add(new DiagnosticResult(Descriptors.AssertToFluent.Id, DiagnosticSeverity.Warning).WithMessage("Use fluent instead").WithSpan(1, 154, 1, 185));
        FixedCode = "public class CustomErrorException : System.Exception { }";

        await RunAsync();
    }
}