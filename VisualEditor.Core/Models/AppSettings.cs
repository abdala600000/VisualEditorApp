namespace VisualEditor.Core.Models
{
    public class AppSettings
    {
        // مسار مجلد الـ bin الخاص بمشروع التشغيل
        public string LastStartupProjectBin { get; set; } = string.Empty;

        // مسار ملف الـ .sln المفتوح حالياً
        public string LastOpenedSolution { get; set; } = string.Empty;
    }
}
