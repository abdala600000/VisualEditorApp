using System;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Mvvm.Controls;

namespace VisualEditorApp.ViewModels.Documents
{
    public partial class WorkspaceViewModel : Document
    {
        [ObservableProperty] private string _text = string.Empty;

        public WorkspaceViewModel(string filePath, string text)
        {
            Id = filePath;
            FilePath = filePath;
            Title = Path.GetFileName(filePath);
            _text = text;
            IsModified = false;
        }

        public string FilePath { get; }

        public string Extension => Path.GetExtension(FilePath);

        partial void OnTextChanged(string value) => IsModified = true;

        public void Save()
        {
            File.WriteAllText(FilePath, Text);
            IsModified = false;
        }

        public static WorkspaceViewModel LoadFromFile(string path)
        {
            var text = File.ReadAllText(path);
            return new WorkspaceViewModel(path, text);
        }
    }
}
