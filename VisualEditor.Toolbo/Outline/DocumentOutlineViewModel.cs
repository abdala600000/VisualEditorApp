using Avalonia.Controls;
using Avalonia.LogicalTree;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using VisualEditor.Core.Messages;

namespace VisualEditor.Toolbox.Outline
{
    public partial class DocumentOutlineViewModel : ObservableObject
    {
        
        // أضف هذا السطر ليكون هو المرجع الوحيد في البرنامج كله
        public static DocumentOutlineViewModel Instance { get; } = new DocumentOutlineViewModel();

        [ObservableProperty]
        private ObservableCollection<ElementNode> nodes = new();

        public DocumentOutlineViewModel()
        {
            // 🎧 تسجيل الشجرة للاستماع لأي تحديث في التصميم
            WeakReferenceMessenger.Default.Register<DesignTreeUpdatedMessage>(this, (recipient, message) =>
            {
                UpdateTree(message.RootControl);
            });


        }

        // كود البناء القديم بتاعك (رجعناه لمكانه الصح)
        // =======================================================
        private void UpdateTree(Control root)
        {
            // 1. مسح الشجرة القديمة
            Nodes.Clear();

            if (root == null) return;

            // 2. بناء العقدة الأساسية وكل اللي جواها
            var rootNode = BuildNode(root);

            // 3. إضافتها للشاشة
            Nodes.Add(rootNode);
        }

        private ElementNode BuildNode(Control control)
        {
            var node = new ElementNode { Header = control.GetType().Name, RelatedControl = control };

            // استخدام الـ LogicalChildren للحصول على العناصر المتداخلة
            foreach (var child in control.GetLogicalChildren())
            {
                if (child is Control childControl)
                {
                    node.Children.Add(BuildNode(childControl));
                }
            }
            return node;
        }
    }
}
