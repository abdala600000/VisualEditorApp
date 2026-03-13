using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VisualEditorApp.Models;
using Microsoft.CodeAnalysis;

namespace VisualEditorApp.Services
{
    public static class SolutionTreeBuilder
    {
        public static SolutionItemViewModel Build(Solution solution)
        {
            var solutionName = solution.FilePath is null
                ? "Solution"
                : Path.GetFileNameWithoutExtension(solution.FilePath);
            var root = new SolutionItemViewModel(SolutionItemKind.Solution, solutionName, solution.FilePath);
            var solutionDirectory = solution.FilePath is null
                ? null
                : Path.GetDirectoryName(solution.FilePath);

            foreach (var project in solution.Projects.OrderBy(p => p.Name))
            {
                var projectNode = new SolutionItemViewModel(SolutionItemKind.Project, project.Name, project.FilePath);
                root.Children.Add(projectNode);

                var projectDirectory = project.FilePath is null
                    ? solutionDirectory
                    : Path.GetDirectoryName(project.FilePath);

                // 1. تجميع الملفات (وشيلنا AnalyzerConfigDocuments لأنها بتجيب ملفات من الويندوز)
                var allPaths = project.Documents
                    .Concat(project.AdditionalDocuments)
                    .Where(d => d.FilePath != null)
                    .Select(d => d.FilePath!)
                    .ToList();

                if (projectDirectory != null)
                {
                    var extraUiFiles = GetExtraUiFiles(projectDirectory);
                    allPaths.AddRange(extraUiFiles);
                }

                // 3. الفلتر الحديدي الجديد (بيمنع أي حاجة بره فولدر المشروع + بيمنع bin و obj)
                var cleanPaths = allPaths
                    .Where(p => projectDirectory != null && p.StartsWith(projectDirectory, StringComparison.OrdinalIgnoreCase)) // 👈 السطر ده اللي هيخفي الـ C تماماً
                    .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                             && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                             && !p.Contains($"{Path.AltDirectorySeparatorChar}bin{Path.AltDirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                             && !p.Contains($"{Path.AltDirectorySeparatorChar}obj{Path.AltDirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                AddDocumentsWithNesting(projectNode, projectDirectory, cleanPaths);

                 
            }

            root.IsExpanded = true;
            return root;
        }

        private static IEnumerable<string> GetExtraUiFiles(string projectDirectory)
        {
            var extraFiles = new List<string>();
            if (!Directory.Exists(projectDirectory)) return extraFiles;

            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".xml", ".axaml", ".xaml", ".json" };
            var excludedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bin", "obj", ".vs", "packages" };

            var dirInfo = new DirectoryInfo(projectDirectory);
            SearchDirectory(dirInfo, allowedExtensions, excludedFolders, extraFiles);

            return extraFiles;
        }

        private static void SearchDirectory(DirectoryInfo dir, HashSet<string> extensions, HashSet<string> excluded, List<string> results)
        {
            if (excluded.Contains(dir.Name)) return;

            try
            {
                foreach (var file in dir.GetFiles())
                {
                    if (extensions.Contains(file.Extension))
                    {
                        results.Add(file.FullName);
                    }
                }

                foreach (var subDir in dir.GetDirectories())
                {
                    SearchDirectory(subDir, extensions, excluded, results);
                }
            }
            catch { }
        }

        private static void AddDocumentsWithNesting(
            SolutionItemViewModel projectNode,
            string? projectDirectory,
            IEnumerable<string> filePaths)
        {
            foreach (var path in filePaths)
            {
                var relativePath = GetRelativePath(projectDirectory, path);
                AddPath(projectNode, relativePath, path);
            }

            // 4. النيستنج الصحيح: ملفات الأكواد تكون تحت ملفات التصميم
            NestFileTypes(projectNode, ".xml.cs", ".xml");
            NestFileTypes(projectNode, ".axaml.cs", ".axaml");
            NestFileTypes(projectNode, ".xaml.cs", ".xaml");
        }

        private static void NestFileTypes(SolutionItemViewModel node, string childExt, string parentExt)
        {
            var children = node.Children.ToList();
            foreach (var child in children)
            {
                if (child.Kind == SolutionItemKind.Folder || child.Kind == SolutionItemKind.Project || child.Kind == SolutionItemKind.Solution)
                {
                    NestFileTypes(child, childExt, parentExt);
                }
            }

            var possibleParents = node.Children.Where(c => c.Kind == SolutionItemKind.Document && c.Name.EndsWith(parentExt, StringComparison.OrdinalIgnoreCase)).ToList();
            var possibleChildren = node.Children.Where(c => c.Kind == SolutionItemKind.Document && c.Name.EndsWith(childExt, StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var childDoc in possibleChildren)
            {
                var parentName = childDoc.Name.Substring(0, childDoc.Name.Length - childExt.Length) + parentExt;
                var parent = possibleParents.FirstOrDefault(p => string.Equals(p.Name, parentName, StringComparison.OrdinalIgnoreCase));

                if (parent != null)
                {
                    node.Children.Remove(childDoc);
                    parent.Children.Add(childDoc);
                }
            }
        }

        private static string GetRelativePath(string? basePath, string fullPath)
        {
            if (string.IsNullOrWhiteSpace(basePath)) return Path.GetFileName(fullPath);

            var relativePath = Path.GetRelativePath(basePath, fullPath);
            return relativePath.StartsWith("..", StringComparison.Ordinal)
                ? Path.GetFileName(fullPath)
                : relativePath;
        }

        private static void AddPath(SolutionItemViewModel root, string relativePath, string fullPath)
        {
            var separators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
            var segments = relativePath.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            var current = root;

            for (var i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                var isLeaf = i == segments.Length - 1;

                if (isLeaf)
                {
                    if (!current.Children.Any(c => string.Equals(c.Path, fullPath, StringComparison.OrdinalIgnoreCase)))
                    {
                        current.Children.Add(new SolutionItemViewModel(SolutionItemKind.Document, segment, fullPath));
                    }
                    continue;
                }

                var folder = current.Children.FirstOrDefault(child =>
                    child.Kind == SolutionItemKind.Folder &&
                    string.Equals(child.Name, segment, StringComparison.OrdinalIgnoreCase));

                if (folder is null)
                {
                    folder = new SolutionItemViewModel(SolutionItemKind.Folder, segment, null);
                    current.Children.Add(folder);
                }
                current = folder;
            }
        }
    }
}