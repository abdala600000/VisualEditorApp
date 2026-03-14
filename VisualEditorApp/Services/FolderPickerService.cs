using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;

namespace VisualEditorApp.Services
{
    public static class FolderPickerService
    {
        public static async Task<string?> SelectFolderAsync(Visual visual)
        {
            var topLevel = TopLevel.GetTopLevel(visual);
            if (topLevel == null) return null;

            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Project Location",
                AllowMultiple = false
            });

            // 🎯 التعديل السحري هنا:
            if (folders.Count > 0)
            {
                // TryGetLocalPath أضمن بكتير وبتحل مشكلة الـ Relative URI
                return folders[0].TryGetLocalPath();
            }

            return null;
        }
    }
}
