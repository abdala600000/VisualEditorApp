using Avalonia.Controls;
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
    }
}