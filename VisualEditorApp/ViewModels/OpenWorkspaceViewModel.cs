using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace VisualEditorApp.ViewModels
{
    public partial class OpenWorkspaceViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _promptMessage;

        public event EventHandler<string> RequestClose;

        public OpenWorkspaceViewModel(string projectName)
        {
            // بناء الرسالة بناءً على اسم المشروع
            PromptMessage = $"Open '{projectName}' now?";
        }

        [RelayCommand]
        private void Open()
        {
            // إرسال إشارة للفتح في نفس النافذة
            RequestClose?.Invoke(this, "OpenCurrent");
        }

        [RelayCommand]
        private void OpenInNewWindow()
        {
            // إرسال إشارة للفتح في نافذة جديدة
            RequestClose?.Invoke(this, "OpenNew");
        }

        [RelayCommand]
        private void Cancel()
        {
            RequestClose?.Invoke(this, "Cancel");
        }
    }
}
