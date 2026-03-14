using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using VisualEditorApp.ViewModels;

namespace VisualEditorApp;

public partial class NewProjectWizardWindow : Window
{
    public NewProjectWizardWindow()
    {
        InitializeComponent();

        var vm = new NewProjectWizardViewModel();
        DataContext = vm;

        // áãÇ ÇáãÇíÓÊÑæ íØáÈ ÇáÞÝá (ÈÓÈÈ ÖÛØÉ Cancel ãä Ãí ÔÇÔÉ)¡ ÇáäÇÝÐÉ ÊÞÝá
        vm.RequestClose += (s, e) => this.Close();
    }
}