using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using VisualEditorApp.Models;
using VisualEditorApp.Services;

namespace VisualEditorApp.ViewModels.Tools
{
    public partial class ErrorListToolViewModel : Tool
    {
        // 🎯 1. الخاصية اللي بتمسك الخطأ المختار (بتحل إيرور SelectedError)
        [ObservableProperty]
        private DiagnosticItem? _selectedError;
        // القائمة الكاملة
        private List<DiagnosticItem> _allDiagnostics = new();

        // القائمة المفلترة اللي بتظهر في الشاشة
        [ObservableProperty] private ObservableCollection<DiagnosticItem> _filteredDiagnostics = new();

        // عدادات الأرقام اللي بتظهر في الزراير فوق
        [ObservableProperty] private int _errorCount;
        [ObservableProperty] private int _warningCount;
        [ObservableProperty] private int _infoCount;

        // حالات الزراير (مضغوطة ولا لأ)
        [ObservableProperty] private bool _showErrors = true;
        [ObservableProperty] private bool _showWarnings = true;
        [ObservableProperty] private bool _showMessages = true;

        public ErrorListToolViewModel()
        {
            Id = "ErrorList";
            Title = "ErrorList";
            // تجربة وهمية (Dummy Data)
            _allDiagnostics.Add(new DiagnosticItem { Severity = DiagnosticSeverity.Error, Code = "CS0103", Description = "The name 'RequestClose' does not exist...", Project = "VisualEditorApp", File = "MainViewModel.cs", Line = 45 });
            RefreshFilters();
        }

        [RelayCommand]
        private void RefreshFilters()
        {
            var filtered = _allDiagnostics.Where(x =>
                (ShowErrors && x.Severity == DiagnosticSeverity.Error) ||
                (ShowWarnings && x.Severity == DiagnosticSeverity.Warning) ||
                (ShowMessages && x.Severity == DiagnosticSeverity.Info)
            ).ToList();

            FilteredDiagnostics = new ObservableCollection<DiagnosticItem>(filtered);

            // تحديث العدادات
            ErrorCount = _allDiagnostics.Count(x => x.Severity == DiagnosticSeverity.Error);
            WarningCount = _allDiagnostics.Count(x => x.Severity == DiagnosticSeverity.Warning);
            InfoCount = _allDiagnostics.Count(x => x.Severity == DiagnosticSeverity.Info);
        }

        // 🎯 2. الأمر اللي بيتنفذ عند الضغط مرتين (بتحل إيرور OnErrorSelectedCommand)
        [RelayCommand]
        public void OnErrorSelected(DiagnosticItem? error)
        {
            // لو مبعتليش خطأ، خد اللي ممسوك في الـ SelectedError
            var targetError = error ?? SelectedError;

            if (targetError != null && !string.IsNullOrEmpty(targetError.ProjectPath))
            {
                // 🚀 بنكلم السيرفر اللي إنت لسه معدله عشان يفتح الملف
                WorkspaceService.Instance.NavigateToFile(targetError.ProjectPath, targetError.Line);
                // 🚀 نداء مباشر للسيرفر
                WorkspaceService.Instance.GoToError(SelectedError);
                System.Diagnostics.Debug.WriteLine($"Navigate to: {targetError.File} at line {targetError.Line}");
            }
        }

        
    }
}
