using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Messaging;
using System;
using VisualEditor.Core;
using VisualEditor.Core.Messages; // 👈 استدعاء النواة اللي فيها المولد والمترجم

namespace VisualEditorApp
{
    public partial class WorkspaceView : UserControl
    {
        // 👈 السر اللي بيمنع اللوب اللانهائي بين المحرر واللوحة
        private bool _isUpdating = false;

        public static WorkspaceView? Instance { get; private set; }

        public WorkspaceView()
        {
            InitializeComponent();
            Instance = this;


            // 🎧 الرادار: تسجيل الشاشة للاستماع لرسائل التحديد من أي مكان
            WeakReferenceMessenger.Default.Register<ControlSelectedMessage>(this, (recipient, message) =>
            {
                // لو الرسالة جاية من الشجرة (Outline)، حدد الكنترول في اللوحة
                if (message.SenderName == "Outline" && message.SelectedControl != null)
                {
                    // MyDesignSurface هو اسم لوحة التصميم بتاعتك في الـ XAML
                    MyDesignSurface.SelectControl(message.SelectedControl);
                }
            });
            // 🎧 الرادار: الاستماع لتعديلات نافذة الخصائص
            WeakReferenceMessenger.Default.Register<DesignChangedMessage>(this, (recipient, message) =>
            {
                // بنادي على نفس الدالة اللي بتكتب كود الـ XAML لما كنا بنسحب الكنترول بالماوس
               // MyDesignSurface_DesignChanged(this, EventArgs.Empty);
            });
        }
        private void BtnViewMode_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button clickedBtn && clickedBtn.Tag is string mode)
            {
                // 1. تصفير شكل كل الزراير (إلغاء التظليل)
                BtnDesign.Background = Brushes.Transparent;
                BtnDesign.FontWeight = FontWeight.Normal;

                BtnSplit.Background = Brushes.Transparent;
                BtnSplit.FontWeight = FontWeight.Normal;

                BtnXaml.Background = Brushes.Transparent;
                BtnXaml.FontWeight = FontWeight.Normal;

                // 2. تظليل الزرار اللي انداس عليه دلوقتي
                clickedBtn.Background = Brush.Parse("#DDDDDD");
                clickedBtn.FontWeight = FontWeight.Bold;

                // 3. تغيير مساحات الشاشة (اللعب في الـ RowDefinitions)
                if (mode == "Design")
                {
                    // إخفاء الفاصل (الصف 2) والمحرر (الصف 3)
                    MainGrid.RowDefinitions = RowDefinitions.Parse("*, Auto, 0, 0");
                }
                else if (mode == "Split")
                {
                    // إرجاع كل حاجة لوضعها الطبيعي (النص بالنص)
                    MainGrid.RowDefinitions = RowDefinitions.Parse("*, Auto, 5, *");
                }
                else if (mode == "XAML")
                {
                    // إخفاء اللوحة (الصف 0) والفاصل (الصف 2)
                    MainGrid.RowDefinitions = RowDefinitions.Parse("0, Auto, 0, *");
                }
            }
        }
        // =================================================================
        // 1. حدث لوحة التصميم: (لما تسحب كنترول أو تغير حجمه بالماوس)
        // الاتجاه: من اللوحة ⬅️ إلى النواة ⬅️ إلى المحرر
        // =================================================================
        private void MyDesignSurface_DesignChanged(object? sender, EventArgs e)
        {
            if (_isUpdating) return;
            _isUpdating = true;

            try
            {
                // 1. هات الرسمة الحالية من اللوحة
                var rootControl = MyDesignSurface.RootDesign;
                if (rootControl != null)
                {
                    // 2. حول الرسمة لكود XAML (باستخدام النواة Core)
                    string generatedXaml = XamlGenerator.GenerateXaml(rootControl);

                    // 3. ابعت الكود للمحرر الذكي عشان يعرضه
                    MyCodeEditor.SetXamlText(generatedXaml);
                }
            }
            finally
            {
                _isUpdating = false;
            }
        }

        // =================================================================
        // 2. حدث المحرر الذكي: (لما تكتب كود بإيدك في المحرر اللي تحت)
        // الاتجاه: من المحرر ⬅️ إلى النواة ⬅️ إلى اللوحة
        // =================================================================
        private void MyCodeEditor_XamlTextChanged(object? sender, string newXamlText)
        {
            if (_isUpdating) return;
            _isUpdating = true;

            try
            {
                // 1. ابعت الكود اللي انكتب لـ LiveDesignerCompiler عشان ينضفه ويرسمه
                var newControl = LiveDesignerCompiler.RenderLiveXaml(newXamlText);

                // 2. اعرض الكنترول الجديد فوق في لوحة التصميم
                if (newControl != null)
                {
                    MyDesignSurface.LoadDesign(newControl);
                    // 2. 📢 السطر الجديد: ابعت التصميم كله في رسالة عشان الشجرة (Outline) تتغذى بيه
                  //  WeakReferenceMessenger.Default.Send(new DesignTreeUpdatedMessage(newControl));
                }
            }
            catch
            {
                // 🚫 بنتجاهل الأخطاء هنا لأن المستخدم ممكن يكون بيكتب ولسه مخلصش الكلمة
            }
            finally
            {
                _isUpdating = false;
            }
        }

        // =================================================================
        // 3. حدث التحديد: (لما تضغط كليك على كنترول في اللوحة)
        // =================================================================
        private void MyDesignSurface_SelectionChanged(object? sender, Control? selectedControl)
        {
            // الحدث ده هنستخدمه دلوقتي عشان نبعت الكنترول لشجرة العناصر (Outline)
            if (selectedControl != null)
            {
                // هنا هتنده على الدالة بتاعتك اللي بتبني شجرة العناصر
                // (لحد ما ننقل شجرة العناصر لمكتبة Toolbox الخاصة بيها)
                // 2. 📢 السطر الجديد: ابعت التصميم كله في رسالة عشان الشجرة (Outline) تتغذى بيه
              //  WeakReferenceMessenger.Default.Send(new DesignTreeUpdatedMessage(selectedControl));
            }
        }


        // دالة لاستقبال النص من الخارج (MainWindow أو Solution Explorer)
        public void SetXamlText(string xml)
        {
            // 1. ابعت النص للمحرر (المحرر هيحدث النص بصمت ومش هيبعت حدث)
            MyCodeEditor.SetXamlText(xml);

            // 2. خد نفس النص، ابعته للمترجم يرسمه، وازرعه في لوحة التصميم فوراً!
            try
            {
                var newControl = LiveDesignerCompiler.RenderLiveXaml(xml);

                if (newControl != null)
                {
                    MyDesignSurface.LoadDesign(newControl);
                    // 2. 📢 السطر الجديد: ابعت التصميم كله في رسالة عشان الشجرة (Outline) تتغذى بيه
                    WeakReferenceMessenger.Default.Send(new DesignTreeUpdatedMessage(newControl));
                }
            }
            catch (Exception ex)
            {
                // لو الملف اللي اتفتح فيه XAML غلط، ممكن نطبع الخطأ في الـ Output 
                System.Diagnostics.Debug.WriteLine($"Error Initial Rendering: {ex.Message}");
            }
        }

        // الدالة اللي هتاخد الرقم المختار وتبعته للوحة التصميم عشان تكبر
        private void ZoomComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (ZoomComboBox?.SelectedItem is ComboBoxItem item && item.Content != null)
            {
                string? zoomText = item.Content.ToString();
                if (zoomText != null)
                {
                    // بننادي على اللوحة ونقولها نفذي الزووم
                    MyDesignSurface?.SetZoomLevel(zoomText);
                }
            }
        }
    }
}