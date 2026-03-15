using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VisualEditorApp.Models;
using Microsoft.CodeAnalysis;
using Microsoft.Build.Construction;
using VisualEditor.Core.Messages;
using VisualEditor.Core.Models;

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

            // 🎯 التحقق: هل الملف المفتوح سوليوشن ولا مشروع؟
            bool isSolution = solution.FilePath != null && solution.FilePath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase);

            if (isSolution)
            {
                // --- 1. حالة الـ Solution (الكود القديم مع تنظيم) ---
                var slnFile = SolutionFile.Parse(solution.FilePath);
                var nodeLookup = new Dictionary<string, SolutionItemViewModel>();

                // إنشاء المجلدات الوهمية
                foreach (var p in slnFile.ProjectsInOrder.Where(x => x.ProjectType == SolutionProjectType.SolutionFolder))
                {
                    var folderNode = new SolutionItemViewModel(SolutionItemKind.Folder, p.ProjectName, null);
                    nodeLookup[p.ProjectGuid] = folderNode;
                }

                // إنشاء المشاريع
                foreach (var p in slnFile.ProjectsInOrder.Where(x => x.ProjectType != SolutionProjectType.SolutionFolder))
                {
                    var roslynProject = solution.Projects.FirstOrDefault(rp => string.Equals(rp.FilePath, p.AbsolutePath, StringComparison.OrdinalIgnoreCase));
                    if (roslynProject == null) continue;

                    var projectNode = CreateProjectNode(roslynProject); // دالة مساعدة لتقليل التكرار
                    nodeLookup[p.ProjectGuid] = projectNode;
                }

                // ربط الأبناء بالآباء (Hierarchy)
                foreach (var p in slnFile.ProjectsInOrder)
                {
                    if (!nodeLookup.TryGetValue(p.ProjectGuid, out var currentNode)) continue;

                    if (!string.IsNullOrEmpty(p.ParentProjectGuid) && nodeLookup.TryGetValue(p.ParentProjectGuid, out var parentNode))
                        parentNode.Children.Add(currentNode);
                    else
                        root.Children.Add(currentNode);
                }
            }
            else
            {
                // --- 2. 🌟 حالة المشروع المنفرد (.csproj) 🌟 ---
                foreach (var roslynProject in solution.Projects)
                {
                    var projectNode = CreateProjectNode(roslynProject);
                    root.Children.Add(projectNode);
                }
            }

            SortNodes(root);
            root.IsExpanded = true;
            return root;
        }

        // 🎯 دالة مساعدة لإنشاء نود المشروع وقراءة ملفاته (عشان منكررش الكود)
        private static SolutionItemViewModel CreateProjectNode(Project roslynProject)
        {
            var projectNode = new SolutionItemViewModel(SolutionItemKind.Project, roslynProject.Name, roslynProject.FilePath);
                
            // 🎯 استرجاع حالة مشروع التشغيل (Startup) من المدير
            if (WorkspaceService.Instance.CurrentStartupProject != null && 
                WorkspaceService.Instance.CurrentStartupProject.Path == roslynProject.FilePath)
            {
                projectNode.IsStartupProject = true;
                // تحديث المرجع في المدير للنسخة الجديدة
                WorkspaceService.Instance.SetCurrentStartupProject(projectNode);
            }
            var projectDirectory = Path.GetDirectoryName(roslynProject.FilePath);

            // سحب كل المستندات (CS, AXAML, إلخ)
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

            // تنظيف المسارات واستبعاد bin/obj
            var cleanPaths = allPaths
                .Where(pPath => projectDirectory != null && pPath.StartsWith(projectDirectory, StringComparison.OrdinalIgnoreCase))
                .Where(pPath => !pPath.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                             && !pPath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            AddDocumentsWithNesting(projectNode, projectDirectory, cleanPaths);

            // 🎯 إضافة عقدة "Dependencies" المحسنة (مقسمة لمجلدات)
            if (roslynProject.ProjectReferences.Any() || roslynProject.MetadataReferences.Any())
            {
                var depsNode = new SolutionItemViewModel(SolutionItemKind.Folder, "Dependencies", null);
                
                // 1. مجلد المشاريع (Project References)
                if (roslynProject.ProjectReferences.Any())
                {
                    var projectsFolder = new SolutionItemViewModel(SolutionItemKind.Folder, "Projects", null);
                    foreach (var pr in roslynProject.ProjectReferences)
                    {
                        var referencedProject = roslynProject.Solution.Projects.FirstOrDefault(p => p.Id == pr.ProjectId);
                        if (referencedProject != null)
                        {
                            projectsFolder.Children.Add(new SolutionItemViewModel(SolutionItemKind.Dependency, referencedProject.Name, referencedProject.FilePath));
                        }
                    }
                    if (projectsFolder.Children.Any()) depsNode.Children.Add(projectsFolder);
                }

                // 2. مجلد الحزم (NuGet Packages)
                var packagesFolder = new SolutionItemViewModel(SolutionItemKind.Folder, "Packages", null);
                foreach (var mr in roslynProject.MetadataReferences)
                {
                    if (mr is PortableExecutableReference per && !string.IsNullOrEmpty(per.FilePath))
                    {
                        string fileName = Path.GetFileName(per.FilePath);
                        if (per.FilePath.Contains(".nuget", StringComparison.OrdinalIgnoreCase) || 
                            per.FilePath.Contains("packages", StringComparison.OrdinalIgnoreCase))
                        {
                            string pkgName = Path.GetFileNameWithoutExtension(fileName);
                            if (!packagesFolder.Children.Any(c => c.Name == pkgName))
                            {
                                packagesFolder.Children.Add(new SolutionItemViewModel(SolutionItemKind.NuGet, pkgName, per.FilePath));
                            }
                        }
                    }
                }

                if (packagesFolder.Children.Any()) depsNode.Children.Add(packagesFolder);

                if (depsNode.Children.Any())
                {
                    projectNode.Children.Add(depsNode);
                }
            }

            SortNodes(projectNode);
            return projectNode;
        }

        // 💡 الترتيب (المجلدات أولاً ثم الملفات)
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
                SortNodes(child);
            }
        }

        // --- باقي الدوال المساعدة (GetExtraUiFiles, AddDocumentsWithNesting, إلخ) ---
        // كما هي في كودك الأصلي...
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
                foreach (var file in dir.GetFiles()) { if (extensions.Contains(file.Extension)) results.Add(file.FullName); }
                foreach (var subDir in dir.GetDirectories()) { SearchDirectory(subDir, extensions, excluded, results); }
            }
            catch (Exception ex)
            {
                MessageBus.Send(SystemDiagnosticMessage.Create(VisualEditor.Core.Models.DiagnosticSeverity.Warning, "TREE001", $"Error searching directory {dir.FullName}: {ex.Message}"));
            }
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
                if (child.Kind == SolutionItemKind.Folder || child.Kind == SolutionItemKind.Project || child.Kind == SolutionItemKind.Solution)
                    NestFileTypes(child, childExt, parentExt);

            var possibleParents = node.Children.Where(c => c.Kind == SolutionItemKind.Document && c.Name.EndsWith(parentExt, StringComparison.OrdinalIgnoreCase)).ToList();
            var possibleChildren = node.Children.Where(c => c.Kind == SolutionItemKind.Document && c.Name.EndsWith(childExt, StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var childDoc in possibleChildren)
            {
                var parentName = childDoc.Name.Substring(0, childDoc.Name.Length - childExt.Length) + parentExt;
                var parent = possibleParents.FirstOrDefault(p => string.Equals(p.Name, parentName, StringComparison.OrdinalIgnoreCase));
                if (parent != null) { node.Children.Remove(childDoc); parent.Children.Add(childDoc); }
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
                        current.Children.Add(new SolutionItemViewModel(SolutionItemKind.Document, segment, fullPath));
                    continue;
                }

                var folder = current.Children.FirstOrDefault(child => child.Kind == SolutionItemKind.Folder && string.Equals(child.Name, segment, StringComparison.OrdinalIgnoreCase));
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