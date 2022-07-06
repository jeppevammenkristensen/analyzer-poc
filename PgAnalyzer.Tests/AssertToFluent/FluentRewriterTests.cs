using System.Security.Cryptography.X509Certificates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;
using FluentAssertions;
using PgAnalyzer.TypeMultiplier;

namespace PgAnalyzer.Tests.AssertToFluent;
public class FluentRewriterTests
{
    [Fact]
    public void Visit_MemberAccessInsideAssertTrue_VerifyCorrectOutcome()
    {
        var testHarness = new TestHarness().WithAssert("Assert.True(something.Property);");
        var subject = testHarness.Subject;
        testHarness.Code.Should().BeEquivalentTo(String.Empty);
        var result = subject.Visit(testHarness.CompilationUnit);
    }

    public class TestHarness
    {
        public bool Yay { get; }

        public FluentRewriter Subject
        {
            get
            {
                if (Code is null)
                    throw new InvalidOperationException("Code should have been initialized");
                var compilationUnitSyntax = SyntaxFactory.ParseCompilationUnit(Code);
                if (compilationUnitSyntax.DescendantNodes().OfType<ExpressionStatementSyntax>().FirstOrDefault()is { } statement && statement.Expression is InvocationExpressionSyntax inv)
                {
                    Statement = statement.Expression;
                    CompilationUnit = compilationUnitSyntax;
                    return new FluentRewriter(inv);
                }

                throw new InvalidOperationException();
            }
        }

        public ExpressionSyntax Statement { get; set; }

        public CompilationUnitSyntax CompilationUnit { get; private set; }

        public TestHarness()
        {
        }

        public TestHarness WithAssert(string line)
        {
            Line = line;
            Code = @$"public class TestClass
{{
    public void Test() 
    {{
        {line}
    }}
}}";
            return this;
        }

        public string Code { get; private set; }

        public string Line { get; private set; }
    }
}