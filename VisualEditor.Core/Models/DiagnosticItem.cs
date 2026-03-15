namespace VisualEditor.Core.Models
{
    public enum DiagnosticSeverity { Error, Warning, Info }
    
    public class DiagnosticItem
    {
        public DiagnosticSeverity Severity { get; set; }
        public string Code { get; set; } = "";
        public string Description { get; set; } = "";
        public string Project { get; set; } = "";
        public string File { get; set; } = "";
        public int Line { get; set; }
        public int Column { get; set; }
        public string ProjectPath { get; set; } = ""; 
    }
}
