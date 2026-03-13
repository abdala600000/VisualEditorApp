using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace VisualEditorApp.Services
{
    public sealed record SolutionLoadResult(Solution? Solution, IReadOnlyList<WorkspaceDiagnostic> Diagnostics);

    public sealed class SolutionLoader
    {
        private MSBuildWorkspace? _workspace;

        // €Ì—‰« «”„ «·»«—«„Ì — ·‹ filePath ⁄‘«‰ »ﬁÏ »Ì” ﬁ»· «·‰Ê⁄Ì‰
        public async Task<SolutionLoadResult> LoadAsync(string filePath, CancellationToken cancellationToken)
        {
            if (!MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterDefaults();
            }

            _workspace?.Dispose();

            var diagnostics = new List<WorkspaceDiagnostic>();
            var workspace = MSBuildWorkspace.Create(new Dictionary<string, string>
            {
                ["UseSharedCompilation"] = "false"
            });

            workspace.RegisterWorkspaceFailedHandler(args => diagnostics.Add(args.Diagnostic));

            Solution? solution = null;

            try
            {
                // 1. ·Ê «·„·› Solution (.sln)
                if (filePath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)|| filePath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
                {
                    solution = await workspace.OpenSolutionAsync(filePath, progress: null, cancellationToken);
                }
                // 2. ·Ê «·„·› Project (.csproj)
                else if (filePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                {
                    var project = await workspace.OpenProjectAsync(filePath, progress: null, cancellationToken);

                    // «·”Õ— Â‰«: «·‹ Workspace »Ì⁄„· Solution  ·ﬁ«∆Ì ÌÕ ÊÌ ⁄·Ï Â–« «·„‘—Ê⁄
                    solution = project.Solution;
                }
                else
                {
                    // ·Ê «„ œ«œ €Ì— „œ⁄Ê„
                    throw new NotSupportedException("«·„·› €Ì— „œ⁄Ê„. Ì—ÃÏ «Œ Ì«— „·› .sln √Ê .csproj");
                }

                _workspace = workspace;
            }
            catch (Exception ex)
            {
                // Ì›÷· œ«Ì„« «’ÿÌ«œ «·√Œÿ«¡ Â‰« ⁄‘«‰ ·Ê „·› «·„‘—Ê⁄ ›ÌÂ „‘ﬂ·… √Ê ‰«ﬁ’
                System.Diagnostics.Debug.WriteLine($"Error loading workspace: {ex.Message}");
            }

            return new SolutionLoadResult(solution, diagnostics);
        }

        public void Clear()
        {
            _workspace?.Dispose();
            _workspace = null;
        }
    }
}