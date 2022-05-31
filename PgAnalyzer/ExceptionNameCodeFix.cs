using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PgAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ExceptionNameCodeFix)), Shared]
    public class ExceptionNameCodeFix : CodeFixProvider
    {


        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(Descriptors.ExceptionNameFormat.Id); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
            SyntaxNode node = root?.FindNode(context.Span); // the span reported by analzyer

            if (node is not ClassDeclarationSyntax classDeclaration) return;

            SyntaxToken identifier = classDeclaration.Identifier;

            Document document = context.Document;
            Solution solution = document.Project.Solution;
            SemanticModel documentSemanticModel = await document.GetSemanticModelAsync(context.CancellationToken);
            ISymbol classModel = documentSemanticModel.GetDeclaredSymbol(classDeclaration, context.CancellationToken);
            string suggestedName = $"{identifier.Text}Exception";

            // Since we reached here we register a CodeAction which consists of
            // name - to be displayed to a user
            // createChangedSolution delegate - which gets invoked when user decides to take an action
            //                                  the delegate should return new solution model
            string options;
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Rename to {suggestedName}",
                    // the solution is in immutable hierarchical model of everything (includes projects, documents and every syntax node)
                    // the delegate should return a new (modified) model based on the initial one,
                    // if you used react/redux - this should be very familair
                    // in most of cases you don't need to write a code for it, therefore there are utilities like Renamer (used below)
                    createChangedSolution: async cancellationToken => await Renamer.RenameSymbolAsync(
                        solution,
                        classModel,new SymbolRenameOptions(),suggestedName,
                        cancellationToken)),
                context.Diagnostics);

            await Task.CompletedTask;
        }
    }
}
