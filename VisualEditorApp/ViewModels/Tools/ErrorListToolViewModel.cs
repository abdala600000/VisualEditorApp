using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using VisualEditorApp.Models;
using VisualEditorApp.Services;
using VisualEditor.Core.Models;

namespace VisualEditorApp.ViewModels.Tools
{
    public partial class ErrorListToolViewModel : Tool
    {
        [ObservableProperty]
        private DiagnosticItem? _selectedError;
        
        private List<DiagnosticItem> _allDiagnostics = new();

        [ObservableProperty] private ObservableCollection<DiagnosticItem> _filteredDiagnostics = new();

        [ObservableProperty] private int _errorCount;
        [ObservableProperty] private int _warningCount;
        [ObservableProperty] private int _infoCount;

        [ObservableProperty] private bool _showErrors = true;
        [ObservableProperty] private bool _showWarnings = true;
        [ObservableProperty] private bool _showMessages = true;

        public ErrorListToolViewModel()
        {
            Id = "ErrorList";
            Title = "ErrorList";
            RefreshFilters();
        }

        public void LoadDiagnostics(IEnumerable<DiagnosticItem> items)
        {
            _allDiagnostics.Clear();
            _allDiagnostics.AddRange(items);
            RefreshFilters();
        }

        public void AddDiagnostic(DiagnosticItem item)
        {
            _allDiagnostics.Add(item);
            RefreshFilters();
        }

        public void Clear()
        {
            _allDiagnostics.Clear();
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

        [RelayCommand]
        public void OnErrorSelected(DiagnosticItem? error)
        {
            var targetError = error ?? SelectedError;

            if (targetError != null)
            {
                WorkspaceService.Instance.NavigateToFile(targetError.File, targetError.Line);
                System.Diagnostics.Debug.WriteLine($"Navigate to: {targetError.File} at line {targetError.Line}");
            }
        }
    }
}
