using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using VisualEditor.Toolbox.Controls;

namespace VisualEditor.Toolbox.Controls
{
    public partial class ToolboxViewModels : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<ToolboxItem> _items = new();

        public ToolboxViewModels()
        {
           

            LoadAllAvaloniaControls();
        }

        private void LoadAllAvaloniaControls()
        {
            // 1. تحديد الـ Assembly اللي جواه الكنترولات بتاعة أفلونيا
            var controlAssembly = typeof(Control).Assembly;

            // 2. البحث عن كل الكلاسات اللي بتورث من Control ومش Abstract
            var allControls = controlAssembly.GetTypes()
                .Where(t => t.IsPublic && !t.IsAbstract && typeof(Control).IsAssignableFrom(t))
                .OrderBy(t => t.Name)
                .ToList();

            var tempList = new ObservableCollection<ToolboxItem>();

            foreach (var type in allControls)
            {
                tempList.Add(new ToolboxItem
                {
                    Name = type.Name,
                    ControlType = type,
                    // لو حبيت ممكن تحط أيقونة افتراضية لكل الكنترولات أو تعمل سويتش لبعضهم
                    IconPath = "M4,6H20V16H4V6M20,4H4C2.9,4 2,4.9 2,6V16C2,17.1 2.9,18 4,18H20C21.1,18 22,17.1 22,16V6C22,4.9 21.1,4 20,4M18,14H6V12H18V14Z"
                });
            }

            Items = tempList;
        }
    }
}
