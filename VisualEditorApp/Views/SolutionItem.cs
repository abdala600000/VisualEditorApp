using System.Collections.ObjectModel;

namespace VisualEditorApp;

public class SolutionItem
{
    public string Name { get; set; } = "";
    public string FullPath { get; set; } = ""; // <--- ÇáãÓÇÑ ÇáßÇãá ááãáÝ
    public string IconType { get; set; } = "File";
    public ObservableCollection<SolutionItem> Children { get; set; } = new();
}