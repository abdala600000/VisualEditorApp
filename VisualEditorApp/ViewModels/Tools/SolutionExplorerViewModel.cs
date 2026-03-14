using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Mvvm.Controls;
using Microsoft.CodeAnalysis;
using VisualEditorApp.Models;
using VisualEditorApp.Services;

namespace VisualEditorApp.ViewModels.Tools
{
    public partial class SolutionExplorerViewModel : Tool
    {
        [ObservableProperty] private SolutionItemViewModel? _selectedItem;

        public SolutionExplorerViewModel(Action<string> openDocument)
        {
            Id = "SolutionExplorer";
            Title = "Solution Explorer";
            Items = new ObservableCollection<SolutionItemViewModel>();
            OpenDocument = openDocument;
          
        }

        public ObservableCollection<SolutionItemViewModel> Items { get; }

        public Action<string> OpenDocument { get; }

        public void LoadSolution(Solution solution)
        {
            Items.Clear();
            Items.Add(SolutionTreeBuilder.Build(solution));
        }

        public void Clear()
        {
            Items.Clear();
        }



    }
}
