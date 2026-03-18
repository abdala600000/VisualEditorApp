using Avalonia.Controls;
using Avalonia.Threading;
using System.Collections.Generic;
using VisualEditor.Core.Messages;

namespace VisualEditorApp.Views.Tools
{
    public partial class PropertiesToolView : UserControl
    {
        public PropertiesToolView()
        {
            InitializeComponent();

            MessageBus.ControlSelected += (message) =>
            {
                if (message.SelectedControl == null) return;
                Dispatcher.UIThread.InvokeAsync(() => ShowProperties(message.SelectedControl));
            };
        }

        private void ShowProperties(Control control)
        {
            var items = new List<PropertyItem>();

            items.Add(new PropertyItem("Name", control.Name ?? ""));
            items.Add(new PropertyItem("Type", control.GetType().Name));
            items.Add(new PropertyItem("Width", double.IsNaN(control.Width) ? "Auto" : ((int)control.Width).ToString()));
            items.Add(new PropertyItem("Height", double.IsNaN(control.Height) ? "Auto" : ((int)control.Height).ToString()));
            items.Add(new PropertyItem("Margin", $"{(int)control.Margin.Left},{(int)control.Margin.Top},{(int)control.Margin.Right},{(int)control.Margin.Bottom}"));

            if (control.Parent is Canvas)
            {
                items.Add(new PropertyItem("Canvas.Left", ((int)Canvas.GetLeft(control)).ToString()));
                items.Add(new PropertyItem("Canvas.Top", ((int)Canvas.GetTop(control)).ToString()));
            }

            if (control is Avalonia.Controls.Primitives.TemplatedControl tc)
            {
                if (tc.Background != null)
                    items.Add(new PropertyItem("Background", tc.Background.ToString() ?? ""));
                if (tc.Foreground != null)
                    items.Add(new PropertyItem("Foreground", tc.Foreground.ToString() ?? ""));
            }

            if (control.RenderTransform != null)
                items.Add(new PropertyItem("RenderTransform", control.RenderTransform.GetType().Name));

            var itemsControl = this.FindControl<ItemsControl>("PropertiesItemsControl");
            if (itemsControl != null)
                itemsControl.ItemsSource = items;
        }
    }

    public class PropertyItem
    {
        public string Name { get; }
        public string Value { get; }
        public PropertyItem(string name, string value) { Name = name; Value = value; }
    }
}
