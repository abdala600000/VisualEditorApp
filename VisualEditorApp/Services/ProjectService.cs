using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace VisualEditorApp.Services
{

    public class ProjectService
    {
        private readonly HashSet<string> _ignoredFolders = new(StringComparer.OrdinalIgnoreCase) { "bin", "obj", ".git", ".vs" };
        private readonly HashSet<string> _allowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".cs", ".axaml", ".xaml", ".csproj", ".json" };

        public SolutionItem LoadProject(string rootPath)
        {
            var rootInfo = new DirectoryInfo(rootPath);
            var rootItem = new SolutionItem
            {
                Name = rootInfo.Name,
                FullPath = rootInfo.FullName, // <--- تحديد مسار الفولدر الرئيسي
                IconType = "project"
            };

            LoadSubItems(rootInfo, rootItem.Children);
            return rootItem;
        }

        private void LoadSubItems(DirectoryInfo directory, ObservableCollection<SolutionItem> children)
        {
            // 1. معالجة الفولدرات
            var directories = directory.GetDirectories()
                .Where(d => !_ignoredFolders.Contains(d.Name))
                .OrderBy(d => d.Name);

            foreach (var dir in directories)
            {
                var folderItem = new SolutionItem
                {
                    Name = dir.Name,
                    FullPath = dir.FullName, // <--- تخزين مسار الفولدر الكامل
                    IconType = "folder"
                };
                children.Add(folderItem);
                LoadSubItems(dir, folderItem.Children);
            }

            // 2. معالجة الملفات
            var files = directory.GetFiles()
                .Where(f => _allowedExtensions.Contains(f.Extension))
                .OrderBy(f => f.Name);

            foreach (var file in files)
            {
                children.Add(new SolutionItem
                {
                    Name = file.Name,
                    FullPath = file.FullName, // <--- الزتونة هنا: مسار الملف الكامل
                    IconType = file.Extension.ToLower()
                });
            }
        }
    }
}
