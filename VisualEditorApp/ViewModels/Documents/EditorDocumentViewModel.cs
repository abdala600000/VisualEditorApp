using System;
using System.IO;
using System.Text;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Mvvm.Controls;

namespace VisualEditorApp.ViewModels.Documents
{
    public partial class EditorDocumentViewModel : Document
    {
        private readonly Encoding _encoding;
        [ObservableProperty] private int _caretLine;
        [ObservableProperty] private int _caretColumn;

        public EditorDocumentViewModel(string filePath, TextDocument document, Encoding encoding)
        {
            FilePath = filePath;
            Title = Path.GetFileName(filePath);
            Document = document;
            _encoding = encoding;
            _caretLine = 1;
            _caretColumn = 1;

            Document.Changed += (_, _) => IsModified = true;
            IsModified = false;
        }

        public string FilePath { get; }

        public TextDocument Document { get; }

        public string Text => Document.Text;

        public string EncodingName => _encoding.WebName;

        public string Extension => Path.GetExtension(FilePath);

        public void Save()
        {
            File.WriteAllText(FilePath, Document.Text, _encoding);
            IsModified = false;
        }

        public static EditorDocumentViewModel LoadFromFile(string path)
        {
            var encoding = DetectEncoding(path);
            var text = File.ReadAllText(path, encoding);
            var document = new TextDocument(text);
            return new EditorDocumentViewModel(path, document, encoding);
        }

        private static Encoding DetectEncoding(string path)
        {
            using var reader = new StreamReader(path, Encoding.UTF8, true);
            if (reader.Peek() >= 0)
            {
                reader.Read();
            }

            return reader.CurrentEncoding;
        }
    }
}
