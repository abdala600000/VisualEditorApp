using System;
using System.IO;
using System.Text.Json;
using VisualEditor.Core.Models;

namespace VisualEditor.Core.Services
{
    public static class SettingsService
    {
        private static readonly string SettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

        // دالة الحفظ
        public static void Save(AppSettings settings)
        {
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }

        // دالة القراءة
        public static AppSettings Load()
        {
            if (!File.Exists(SettingsPath)) return new AppSettings();

            string json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
    }
}
