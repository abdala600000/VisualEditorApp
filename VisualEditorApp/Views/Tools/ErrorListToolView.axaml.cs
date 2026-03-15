using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using VisualEditorApp.ViewModels.Tools;

namespace VisualEditorApp.Views.Tools;

public partial class ErrorListToolView : UserControl
{
    public ErrorListToolView()
    {
        InitializeComponent();
    }

    private void OnDataGridDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is ErrorListToolViewModel vm && vm.SelectedError != null)
        {
            vm.ErrorSelectedCommand.Execute(vm.SelectedError);
        }
    }
}