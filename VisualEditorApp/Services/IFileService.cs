using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VisualEditorApp.Services
{
    public interface IFileService
    {
        // فتح فولدر واحد (للمشروع)
        Task<IStorageFolder?> OpenFolderAsync();
        // فتح ملف واحد أو أكتر
        Task<IReadOnlyList<IStorageFile>> OpenFileAsync();
    }
}
