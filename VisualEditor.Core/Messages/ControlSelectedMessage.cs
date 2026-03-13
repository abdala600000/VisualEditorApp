using Avalonia.Controls;

namespace VisualEditor.Core.Messages
{
    public class ControlSelectedMessage
    {
        public Control? SelectedControl { get; set; }
        public string SenderName { get; set; } // عشان نعرف مين اللي بعت (الشجرة ولا اللوحة)

        public ControlSelectedMessage(Control? control, string senderName)
        {
            SelectedControl = control;
            SenderName = senderName;
        }
    }
}
