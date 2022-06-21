using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using PgAnalyzer.AssertToFluent;
using Xunit;

namespace PgAnalyzer.Tests.AssertToFluent;

public class AssertToFluentAnalyzerTests : CSharpAnalyzerTest<AssertToFluentAnalyzer, XUnitVerifier>
{
    [Fact]
    public async Task Test()
    {
        TestCode =
            "using Xunit; namespace Supra { public class Testing { public void Test(){ Assert.True(\"hello\" == \"hello\");  }}}";
        TestState.AdditionalReferences.Add(typeof(Xunit.Assert).Assembly);
        TestState.AdditionalReferences.Add(typeof(FluentAssertions.AssertionExtensions).Assembly);
        ExpectedDiagnostics.Clear();
        await RunAsync();
    }
}