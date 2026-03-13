using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace VisualEditor.Toolbox.Controls
{
    public class ToolboxDragBehavior : Behavior<Control>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            // أول ما الكنترول يظهر، نراقب حركة الماوس عليه
            if (AssociatedObject != null)
            {
                AssociatedObject.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            // تنظيف الأحداث لما الكنترول يختفي
            if (AssociatedObject != null)
            {
                AssociatedObject.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
            }
        }

        private async void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // نتأكد إن الضغطة كليك شمال وإن الماوس ماسك أداة فعلاً
            if (!e.GetCurrentPoint(AssociatedObject).Properties.IsLeftButtonPressed) return;

            if (AssociatedObject?.DataContext is ToolboxItem toolboxItem)
            {
                // تجهيز "الشنطة" اللي هتشيل بيانات الكنترول في الهواء
                var dragData = new DataObject();

                // بنبعت اسم الكلاس (Type) عشان نعرف نبنيه هناك على الـ Canvas
                dragData.Set("ControlType", toolboxItem.ControlType?.AssemblyQualifiedName ?? "");
                dragData.Set("ControlName", toolboxItem.Name);

                // بدء السحب (والكود بيستنى لحد ما تسيب الماوس)
                var result = await DragDrop.DoDragDrop(e, dragData, DragDropEffects.Copy);
            }
        }
    }
}
