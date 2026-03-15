using CommunityToolkit.Mvvm.ComponentModel;

namespace VisualEditorApp.Models
{
    public partial class ProblemItemViewModel : ObservableObject
    {
        public ProblemItemViewModel(string severity, string message, string file = "", int line = 0, string projectPath = "")
        {
            Severity = severity;
            Message = message;
            File = file;
            Line = line;
            ProjectPath = projectPath;
        }

        public string Severity { get; }
        public string Message { get; }
        public string File { get; }
        public int Line { get; }
        public string ProjectPath { get; }
    }
}
