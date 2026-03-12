using Avalonia.Controls.Shapes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Dock.Model.Mvvm.Controls;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using VisualEditorApp.Models.Tools;
using VisualEditorApp.Services;
using static VisualEditorApp.Services.FileService;

namespace VisualEditorApp.ViewModels;
// رسالة بسيطة شايلة مسار الفولدر المختار
public record FolderOpenedMessage(string Path);
public partial class SolutionExplorerTool : Tool, IRecipient<FolderOpenedMessage>
{
    private readonly ProjectService _projectService = new();
    public static SolutionExplorerTool Instance { get; } = new SolutionExplorerTool();

    [ObservableProperty]
    private ObservableCollection<SolutionItem> _items = new();

    public SolutionExplorerTool()
    {
        Id = "SolutionExplorer";
        Title = "Solution Explorer";
        // الحل السحري: إلغاء أي تسجيل سابق لهذا الكائن قبل التسجيل الجديد
        WeakReferenceMessenger.Default.UnregisterAll(this);
        WeakReferenceMessenger.Default.Register<FolderOpenedMessage>(this);
    }

    // الدالة اللي هتتنفذ أول ما الرسالة توصل
    public void Receive(FolderOpenedMessage message)
    {
        // تفريغ الشجرة القديمة وتحميل الجديدة
        Items.Clear();
        var projectLoader = new ProjectService(); // الخدمة اللي عملناها
        var root = projectLoader.LoadProject(message.Path);
        Items.Add(root);
    }

    [RelayCommand]
    private void OpenProjectFolder(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;

        Items.Clear();
        var projectTree = _projectService.LoadProject(path);
        Items.Add(projectTree);
    }

    // نفترض إن الخدمة تم حقنها أو تعريفها
    private readonly IFileService _fileService;

    [RelayCommand]
    private async Task OpenProject()
    {
        var folder = await _fileService.OpenFolderAsync();

        if (folder != null)
        {
            // المميز هنا إننا بنتعامل مع IStorageFolder مش مجرد نص (Path)
            string localPath = folder.Path.LocalPath; // الحصول على المسار الحقيقي

            Items.Clear();
            var projectTree = _projectService.LoadProject(localPath);
            Items.Add(projectTree);
        }
    }


    // تعريف الرسالة
    public record OpenFileMessage(string FilePath, string FileName);

    // داخل كلاس SolutionExplorerTool
    [RelayCommand]
    private void OpenFile(SolutionItem item)
    {
        // لو هو ملف فعلاً مش فولدر
        if (item != null && item.IconType != "Folder")
        {
            WeakReferenceMessenger.Default.Send(new OpenFileMessage(item.FullPath, item.Name));
        }
    }
}