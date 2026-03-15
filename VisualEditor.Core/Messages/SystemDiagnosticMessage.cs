using VisualEditor.Core.Models;

namespace VisualEditor.Core.Messages
{
    public class SystemDiagnosticMessage
    {
        public DiagnosticItem Diagnostic { get; }

        public SystemDiagnosticMessage(DiagnosticItem diagnostic)
        {
            Diagnostic = diagnostic;
        }

        public static SystemDiagnosticMessage Create(DiagnosticSeverity severity, string code, string description, string? file = null, string? project = null)
        {
            return new SystemDiagnosticMessage(new DiagnosticItem
            {
                Severity = severity,
                Code = code,
                Description = description,
                File = file ?? "",
                Project = project ?? "System"
            });
        }
    }
}
