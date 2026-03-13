using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Messaging;
using Dock.Model.Controls;
using Dock.Model.Mvvm.Controls;
using Microsoft.CodeAnalysis.Differencing;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using VisualEditor.Core;
using VisualEditorApp.Core;
using VisualEditorApp.Models;
using VisualEditorApp.ViewModels;
using static VisualEditorApp.ViewModels.SolutionExplorerTool;

namespace VisualEditorApp.Views
{
    public partial class MainWindow : Window, IRecipient<OpenFileMessage>
    {

        public MainWindow()
        {
            InitializeComponent();
           
            var vm = new MainWindowViewModel();
            DataContext = vm;  

            // 2. تمرير الـ VM للمصنع لضمان "وحدة النسخ"
            var factory = new EditorDockFactory(vm);
            var layout = factory.CreateLayout();
            factory.InitLayout(layout);

            MainDockControl.Layout = layout;
            // الحل السحري: إلغاء أي تسجيل سابق لهذا الكائن قبل التسجيل الجديد
            WeakReferenceMessenger.Default.UnregisterAll(this);
            WeakReferenceMessenger.Default.Register<OpenFileMessage>(this);
        }
        // --- دوال شريط القوائم ---

        private void NewProject_Click(object? sender, RoutedEventArgs e)
        {
            // هنا سنكتب لاحقاً كود تفريغ مساحة العمل أو تهيئة مشروع جديد
            Debug.WriteLine("تم اختيار: إنشاء مشروع جديد");
        }

        private async void OpenProject_Click(object? sender, RoutedEventArgs e)
        {
            // الوصول للـ Window الحالية بأحدث طريقة في Avalonia 11
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var topLevel = Avalonia.Controls.TopLevel.GetTopLevel(desktop.MainWindow);
                if (topLevel != null)
                {
                    // فتح الـ Picker الحديث
                    var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
                    {
                        Title = "Open Project Folder",
                        AllowMultiple = false
                    });

                    if (folders.Count > 0)
                    {
                        // إرسال رسالة لكل البرنامج إن فيه فولدر اتفتح
                        var path = folders[0].Path.LocalPath;
                        WeakReferenceMessenger.Default.Send(new FolderOpenedMessage(path));
                    }
                }
            }
        }

        private async void OpenFile_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select XML / XAML File",
                AllowMultiple = false
            });

            if (files.Count >= 1)
            {
                var filePath = files[0].Path.LocalPath;

                try
                {
                    // 1. قراءة النص الخام
                    string xmlText = await System.IO.File.ReadAllTextAsync(filePath);

                    // 2. 👈 السطر السحري الجديد: ابعت النص للورك سبيس
                    // الورك سبيس هيبعته للمحرر، والمحرر هيرسمه أوتوماتيك!
                    WorkspaceView.Instance?.SetXamlText(xmlText);
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading file: {ex.Message}");
                }
            }
        }
       
      

        private void RunProject_Click(object? sender, RoutedEventArgs e)
        {
            // هنا سنكتب كود تشغيل (Compile and Run) للمشروع المفتوح
            Debug.WriteLine("تم اختيار: تشغيل المشروع");
        }

        private void PreviewToggle_Checked(object? sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton tb)
            {
                // إرسال الحالة (True للمعاينة، False للتصميم)
              //  WorkspaceView.Instance?.SetPreviewMode(tb.IsChecked ?? false);

                // تغيير لون الزرار للتنبيه
                tb.Content = (tb.IsChecked ?? false) ? "RUNNING (Live)" : "Preview Mode";
            }
        }





        public async void Receive(OpenFileMessage message)
        {
            try
            {
                // 1. قراءة النص من الملف اللي جي من الـ Solution Explorer
                string xmlText = await System.IO.File.ReadAllTextAsync(message.FilePath);

                // 2. إرسال النص للورك سبيس ليقوم بالسحر
                WorkspaceView.Instance?.SetXamlText(xmlText);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error receiving file: {ex.Message}");
            }
        }
    }
}