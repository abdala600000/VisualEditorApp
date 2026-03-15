using System.Collections.Generic;
using VisualEditor.Core.Models;

namespace VisualEditor.Core.Messages
{
    public class BuildFinishedMessage
    {
        public bool Success { get; }
        public List<DiagnosticItem> Diagnostics { get; }

        public BuildFinishedMessage(bool success, List<DiagnosticItem> diagnostics)
        {
            Success = success;
            Diagnostics = diagnostics;
        }
    }
}
