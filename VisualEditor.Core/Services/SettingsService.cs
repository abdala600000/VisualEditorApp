using System;
using System.IO;
using System.Text.Json;
using VisualEditor.Core.Models;
using VisualEditor.Core.Messages;

namespace VisualEditor.Core.Services
{
    public static class SettingsService
    {
        private static readonly string SettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

        // دالة الحفظ
        public static void Save(AppSettings settings)
        {
            try
            {
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                MessageBus.Send(SystemDiagnosticMessage.Create(DiagnosticSeverity.Warning, "SET001", $"Failed to save settings: {ex.Message}"));
            }
        }

        // دالة القراءة
        public static AppSettings Load()
        {
            if (!File.Exists(SettingsPath)) return new AppSettings();

            try
            {
                string json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch (Exception ex)
            {
                MessageBus.Send(SystemDiagnosticMessage.Create(DiagnosticSeverity.Warning, "SET002", $"Failed to load settings: {ex.Message}"));
                return new AppSettings();
            }
        }
    }
}
