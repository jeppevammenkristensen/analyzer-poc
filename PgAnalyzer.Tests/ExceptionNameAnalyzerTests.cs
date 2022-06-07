using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace PgAnalyzer.Tests;

public class ExceptionNameAnalyzerTests : CSharpAnalyzerTest<PgAnalyzer.ExceptionNameAnalyzer,XUnitVerifier>
{
    [Fact]
    public async Task CorrectName_Ignores()
    {
        TestCode = "public class CorrectException : System.Exception {}";
        ExpectedDiagnostics.Clear();
        await RunAsync();
    }

    [Fact]
    public async Task WhenInconsistentName_ShowsWarning()
    {
        TestCode = "public class CustomError : System.Exception { }";

        ExpectedDiagnostics.Add(
            new DiagnosticResult(Descriptors.ExceptionNameFormat.Id, DiagnosticSeverity.Warning)
                .WithMessage("CustomError should end with exception")
                .WithSpan(1, 14, 1, 25));  // Indexing here starts with one for both line and column,
        // the same way as you'd see it in IDE/editor.
        // Here we have 1st line, characters 14 to 25, what is CustomError text
        // this also is the area that becomes highlighted by IDE/editor
        await RunAsync();
    }
}