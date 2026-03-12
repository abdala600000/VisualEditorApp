using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VisualEditorApp.Services
{
    public class FileService : IFileService
    {
        private readonly Window _target;

        public FileService(Window target) => _target = target;

        public async Task<IStorageFolder?> OpenFolderAsync()
        {
            // أحدث طريقة لفتح الفولدرات في أفلونيا 11
            var folders = await _target.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "اختر مجلد المشروع",
                AllowMultiple = false
            });

            return folders.Count >= 1 ? folders[0] : null;
        }

        public async Task<IReadOnlyList<IStorageFile>> OpenFileAsync()
        {
            // أحدث طريقة لفتح الملفات مع فلاتر (File Types)
            return await _target.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "فتح ملفات XAML",
                AllowMultiple = true,
                FileTypeFilter = new[] {
                new FilePickerFileType("Avalonia Files") { Patterns = new[] { "*.axaml", "*.xaml" } },
                new FilePickerFileType("C# Files") { Patterns = new[] { "*.cs" } }
            }
            });
        }
    }
}
