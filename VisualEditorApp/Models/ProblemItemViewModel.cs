using CommunityToolkit.Mvvm.ComponentModel;

namespace VisualEditorApp.Models
{
    public partial class ProblemItemViewModel : ObservableObject
    {
        public ProblemItemViewModel(string severity, string message)
        {
            Severity = severity;
            Message = message;
        }

        public string Severity { get; }
        public string Message { get; }
    }
}
