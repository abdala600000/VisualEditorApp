using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VisualEditorApp.ViewModels
{
    public partial class TemplateSelectionViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private TemplateItem _selectedTemplate;

        // القائمة الأساسية اللي فيها كل حاجة
        public ObservableCollection<TemplateItem> Templates { get; } = new();

        // القائمة اللي بتتعرض في الجدول
        public ObservableCollection<TemplateItem> FilteredTemplates { get; } = new();

        // 🌟 الإضافات الجديدة لقائمة أنواع المشاريع 🌟
        public ObservableCollection<string> ProjectTypes { get; } = new() { "All", "Avalonia", "WPF", "Console", "Library", "Web" };

        [ObservableProperty]
        private string _selectedProjectType = "All"; // الافتراضي يعرض كله

        // الإشارات (Events) للانتقال والقفل
        public event EventHandler RequestClose;
        public event EventHandler<TemplateItem> RequestNavigateNext;

        // زرار Next بيشتغل بس لو المستخدم اختار قالب
        public bool IsNextEnabled => SelectedTemplate != null;

        public TemplateSelectionViewModel()
        {
            // تشغيل تحميل القوالب في الخلفية
            Task.Run(() => LoadTemplates());
        }

        // 🌟 لما المستخدم يغير الفلتر، نشغل التصفية 🌟
        partial void OnSelectedProjectTypeChanged(string value) => FilterTemplates();
        partial void OnSearchTextChanged(string value) => FilterTemplates();

        // لما المستخدم يختار قالب من الجدول
        partial void OnSelectedTemplateChanged(TemplateItem value)
        {
            OnPropertyChanged(nameof(IsNextEnabled));
            NextCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void LoadTemplates()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = "new list",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                ParseDotnetTemplates(output);

                Dispatcher.UIThread.InvokeAsync(FilterTemplates);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"🔴 خطأ في جلب القوالب: {ex.Message}");
            }
        }

        private void ParseDotnetTemplates(string output)
        {
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            bool foundDashes = false;
            foreach (var line in lines)
            {
                if (line.StartsWith("---"))
                {
                    foundDashes = true;
                    continue;
                }

                if (!foundDashes) continue;

                var parts = Regex.Split(line, @"\s{2,}");

                if (parts.Length >= 4)
                {
                    string lang = parts[2].Replace("[", "").Replace("]", "").Split(',').FirstOrDefault() ?? parts[2];

                    Dispatcher.UIThread.InvokeAsync(() => {
                        Templates.Add(new TemplateItem
                        {
                            Name = parts[0].Trim(),
                            ShortName = parts[1].Trim(),
                            Language = lang.Trim(),
                            Type = "project",
                            Description = parts[3].Trim()
                        });
                    });
                }
            }
        }

        // 🌟 دالة الفلترة المحدثة 🌟
        // 🌟 دالة الفلترة المحدثة (بتقرأ جميع اللغات وكل التمبليتات) 🌟
        private void FilterTemplates()
        {
            FilteredTemplates.Clear();
            var query = Templates.AsEnumerable();

            // ❌ تم إزالة الفلتر الإجباري بتاع الـ C#.. دلوقتي هيعرض كل اللغات وكل الملفات

            // 1. فلترة بالبحث النصي (لو المستخدم بيبحث عن اسم معين)
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(t => t.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                         t.ShortName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            // 2. فلترة بنوع التمبليت (من الـ ComboBox زي Avalonia أو WPF)
            if (SelectedProjectType != "All")
            {
                query = query.Where(t => t.Description.Contains(SelectedProjectType, StringComparison.OrdinalIgnoreCase) ||
                                         t.Name.Contains(SelectedProjectType, StringComparison.OrdinalIgnoreCase) ||
                                         t.ShortName.Contains(SelectedProjectType, StringComparison.OrdinalIgnoreCase));
            }

            // 3. إضافة النتيجة النهائية للجدول
            foreach (var item in query)
            {
                FilteredTemplates.Add(item);
            }
        }

        // أمر زرار Cancel
        [RelayCommand]
        private void Cancel()
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        // أمر زرار Next 
        [RelayCommand(CanExecute = nameof(IsNextEnabled))]
        private void Next()
        {
            if (SelectedTemplate != null)
            {
                RequestNavigateNext?.Invoke(this, SelectedTemplate);
            }
        }
    }

    // كلاس القالب
    public class TemplateItem
    {
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string Language { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
    }
}