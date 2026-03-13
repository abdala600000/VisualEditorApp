using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using XamlToCSharpGenerator.LanguageService.Models;
using XamlToCSharpGenerator.LanguageService.Workspace;

namespace VisualEditorApp.Core
{
    internal sealed class InMemoryCompilationProvider : ICompilationProvider
    {
        private readonly Compilation _compilation;

        public InMemoryCompilationProvider(Compilation compilation)
        {
            _compilation = compilation;
        }

        public Task<CompilationSnapshot> GetCompilationAsync(
            string filePath,
            string? workspaceRoot,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new CompilationSnapshot(
                ProjectPath: workspaceRoot,
                Project: null,
                Compilation: _compilation,
                Diagnostics: ImmutableArray<LanguageServiceDiagnostic>.Empty));
        }

        public void Invalidate(string filePath)
        {
        }

        public void Dispose()
        {
        }
    }
}
