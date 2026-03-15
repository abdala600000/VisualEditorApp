using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using VisualEditor.Core.Messages;
using VisualEditor.Core.Models;

namespace VisualEditorApp.Services
{
    public class RecentProjectsService
    {
        private static readonly string RecentFilesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VisualEditorApp", "recent_projects.json");

        public static List<string> GetRecentProjects()
        {
            try
            {
                if (!File.Exists(RecentFilesPath)) return new List<string>();
                var json = File.ReadAllText(RecentFilesPath);
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch (Exception ex) 
            {
                MessageBus.Send(SystemDiagnosticMessage.Create(DiagnosticSeverity.Warning, "REC001", $"Failed to load recent projects: {ex.Message}"));
                return new List<string>(); 
            }
        }

        public static void AddRecentProject(string path)
        {
            try
            {
                var list = GetRecentProjects();
                list.Remove(path);
                list.Insert(0, path);
                if (list.Count > 10) list = list.Take(10).ToList();

                var dir = Path.GetDirectoryName(RecentFilesPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);

                var json = JsonSerializer.Serialize(list);
                File.WriteAllText(RecentFilesPath, json);
            }
            catch (Exception ex)
            {
                MessageBus.Send(SystemDiagnosticMessage.Create(DiagnosticSeverity.Warning, "REC002", $"Failed to add recent project: {ex.Message}"));
            }
        }

        public static void Clear()
        {
            try { if (File.Exists(RecentFilesPath)) File.Delete(RecentFilesPath); }
            catch (Exception ex)
            {
                MessageBus.Send(SystemDiagnosticMessage.Create(VisualEditor.Core.Models.DiagnosticSeverity.Warning, "REC003", $"Failed to clear recent projects: {ex.Message}"));
            }
        }
    }
}
