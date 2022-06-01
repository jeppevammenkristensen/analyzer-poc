using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;

namespace PgAnalyzer.Tests
{
    public class ExceptionNameCodeFixTests : CSharpCodeFixTest<ExceptionNameAnalyzer,ExceptionNameCodeFix, XUnitVerifier>
    {
        [Fact]
        public async Task WhenInconsistenName_AddsExceptionPostfix()
        {
            TestCode = "public class CustomError : System.Exception { }";

            ExpectedDiagnostics.Add(
                new DiagnosticResult(Descriptors.ExceptionNameFormat.Id, DiagnosticSeverity.Warning)
                    .WithMessage("CustomError should end with exception")
                    .WithSpan(1, 14, 1, 25));

            FixedCode = "public class CustomErrorException : System.Exception { }";

            await RunAsync();
        }
    }
}