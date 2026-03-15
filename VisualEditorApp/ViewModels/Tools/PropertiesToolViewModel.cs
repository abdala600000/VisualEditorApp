using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Mvvm.Controls;
using VisualEditorApp.Models;

namespace VisualEditorApp.ViewModels.Tools
{
    public partial class PropertiesToolViewModel : Tool
    {
        [ObservableProperty] private string _itemName = "No selection";
        [ObservableProperty] private string _itemKind = string.Empty;
        [ObservableProperty] private string _itemPath = string.Empty;

        public event System.EventHandler<(System.Reflection.PropertyInfo Prop, object? Value, Avalonia.Controls.Control Target)>? PropertyChangedByUser;

        public PropertiesToolViewModel()
        {
            Id = "Properties";
            Title = "Properties";
        }

        public void RaisePropertyChangedByUser(System.Reflection.PropertyInfo prop, object? value, Avalonia.Controls.Control target)
        {
            PropertyChangedByUser?.Invoke(this, (prop, value, target));
        }

        public void UpdateSelection(SolutionItemViewModel? item)
        {
            if (item is null)
            {
                ItemName = "No selection";
                ItemKind = string.Empty;
                ItemPath = string.Empty;
                return;
            }

            ItemName = item.Name;
            ItemKind = item.Kind.ToString();
            ItemPath = item.Path ?? string.Empty;
        }
    }
}
