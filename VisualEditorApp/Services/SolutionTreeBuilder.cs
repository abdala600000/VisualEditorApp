using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VisualEditorApp.Models;
using Microsoft.CodeAnalysis;
// 💡 السطر ده مهم جداً عشان نقرأ هيكل الـ Solution الحقيقي
using Microsoft.Build.Construction;

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

            if (solution.FilePath == null) return root;

            var solutionDirectory = Path.GetDirectoryName(solution.FilePath);

            // 1. قراءة الهيكل الحقيقي للمجلدات من ملف الـ .sln باستخدام MSBuild
            var slnFile = SolutionFile.Parse(solution.FilePath);

            // قاموس لربط كل عنصر (مجلد أو مشروع) بالـ ID بتاعه عشان نبني الشجرة صح
            var nodeLookup = new Dictionary<string, SolutionItemViewModel>();

            // 2. إنشاء المجلدات الوهمية (Solution Folders)
            foreach (var p in slnFile.ProjectsInOrder.Where(x => x.ProjectType == SolutionProjectType.SolutionFolder))
            {
                var folderNode = new SolutionItemViewModel(SolutionItemKind.Folder, p.ProjectName, null);
                nodeLookup[p.ProjectGuid] = folderNode;
            }

            // 3. إنشاء المشاريع وربط ملفاتها
            foreach (var p in slnFile.ProjectsInOrder.Where(x => x.ProjectType != SolutionProjectType.SolutionFolder))
            {
                // بندور على المشروع جوه Roslyn عشان نجيب ملفات الأكواد بتاعته
                var roslynProject = solution.Projects.FirstOrDefault(rp => string.Equals(rp.FilePath, p.AbsolutePath, StringComparison.OrdinalIgnoreCase));

                if (roslynProject == null) continue;

                var projectNode = new SolutionItemViewModel(SolutionItemKind.Project, roslynProject.Name, roslynProject.FilePath);
                nodeLookup[p.ProjectGuid] = projectNode;

                var projectDirectory = Path.GetDirectoryName(roslynProject.FilePath);

                // سحب الملفات (الكود القديم بتاعنا اللي بيجيب الـ axaml والـ cs)
                var allPaths = roslynProject.Documents
                    .Concat(roslynProject.AdditionalDocuments)
                    .Where(d => d.FilePath != null)
                    .Select(d => d.FilePath!)
                    .ToList();

                if (projectDirectory != null)
                {
                    var extraUiFiles = GetExtraUiFiles(projectDirectory);
                    allPaths.AddRange(extraUiFiles);
                }

                var cleanPaths = allPaths
                    .Where(pPath => projectDirectory != null && pPath.StartsWith(projectDirectory, StringComparison.OrdinalIgnoreCase))
                    .Where(pPath => !pPath.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                                 && !pPath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                                 && !pPath.Contains($"{Path.AltDirectorySeparatorChar}bin{Path.AltDirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                                 && !pPath.Contains($"{Path.AltDirectorySeparatorChar}obj{Path.AltDirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                AddDocumentsWithNesting(projectNode, projectDirectory, cleanPaths);

                SortNodes(projectNode); // ترتيب الملفات جوه المشروع
            }

            // 4. السحر هنا: بناء هيكل الشجرة النهائي بناءً على الـ Parent ID
            foreach (var p in slnFile.ProjectsInOrder)
            {
                if (!nodeLookup.TryGetValue(p.ProjectGuid, out var currentNode)) continue;

                // لو العنصر ليه أب (موجود جوه Solution Folder)
                if (!string.IsNullOrEmpty(p.ParentProjectGuid) && nodeLookup.TryGetValue(p.ParentProjectGuid, out var parentNode))
                {
                    parentNode.Children.Add(currentNode);
                }
                else
                {
                    // لو مالوش أب، يبقى على الروت الرئيسي للـ Solution
                    root.Children.Add(currentNode);
                }
            }

            SortNodes(root); // ترتيب الروت الرئيسي عشان المجلدات تطلع فوق
            root.IsExpanded = true;
            return root;
        }

        // 💡 تعديل الترتيب عشان المجلدات الوهمية تظهر فوق والمشاريع تحتها
        private static void SortNodes(SolutionItemViewModel node)
        {
            var sortedChildren = node.Children
                .OrderByDescending(c => c.Kind == SolutionItemKind.Folder ? 2 : (c.Kind == SolutionItemKind.Project ? 1 : 0))
                .ThenBy(c => c.Name)
                .ToList();

            node.Children.Clear();

            foreach (var child in sortedChildren)
            {
                node.Children.Add(child);
                SortNodes(child); // ترتيب متكرر للأبناء
            }
        }

        // =========================================================
        // باقي الدوال زي ما هي بدون تغيير (GetExtraUiFiles, AddDocumentsWithNesting, إلخ)
        // =========================================================

        private static IEnumerable<string> GetExtraUiFiles(string projectDirectory)
        {
            var extraFiles = new List<string>();
            if (!Directory.Exists(projectDirectory)) return extraFiles;

            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".xml", ".axaml", ".xaml", ".json", ".md" };
            var excludedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bin", "obj", ".vs", "packages", ".git" };

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
                    if (extensions.Contains(file.Extension)) results.Add(file.FullName);
                }
                foreach (var subDir in dir.GetDirectories())
                {
                    SearchDirectory(subDir, extensions, excluded, results);
                }
            }
            catch { }
        }

        private static void AddDocumentsWithNesting(SolutionItemViewModel projectNode, string? projectDirectory, IEnumerable<string> filePaths)
        {
            foreach (var path in filePaths)
            {
                var relativePath = GetRelativePath(projectDirectory, path);
                AddPath(projectNode, relativePath, path);
            }
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
            return relativePath.StartsWith("..", StringComparison.Ordinal) ? Path.GetFileName(fullPath) : relativePath;
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