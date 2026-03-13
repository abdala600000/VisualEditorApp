using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Messaging;
using System.Reflection;
using VisualEditor.Core.Messages;


namespace VisualEditor.Toolbox.Prop;
public partial class PropertiesView : UserControl
{
    
    private Control? _currentElement;
    private bool _isUpdatingFromCode = false;
    private bool _isAlphabetical = false;
    private List<PropertyItem> _rawProperties = new(); // المصدر الأصلي

    public PropertiesView()
    {
        InitializeComponent();

        // 🎧 الرادار: النافذة دي قاعدة بتسمع، أول ما حد يحدد كنترول، هتاخده وتعرض خصائصه
        WeakReferenceMessenger.Default.Register<ControlSelectedMessage>(this, (recipient, message) =>
        {
            // استدعاء دالتك الأصلية لعرض الخصائص
            SetSelectedElement(message.SelectedControl);
        });

    }

    public void SetSelectedElement(Control? element)
    {
        _currentElement = element;
        if (element == null) return;

        _isUpdatingFromCode = true;
        NameEditor.Text = element.Name ?? "Unnamed";
        SelectedTypeText.Text = $"({element.GetType().Name})";

        // 1. جلب الخصائص مرة واحدة فقط
        _rawProperties = GetFilteredProperties(element);

        // 2. تحديث الشاشة
        RefreshDisplay();
        _isUpdatingFromCode = false;
    }

    private void RefreshDisplay()
    {
        if (_currentElement == null || _rawProperties == null) return;

        // تطبيق الفلترة (البحث) أولاً
        var searchText = SearchBox?.Text?.ToLower() ?? "";
        var filtered = _rawProperties.Where(p => p.Name.ToLower().Contains(searchText)).ToList();

        if (_isAlphabetical)
        {
            GroupsControl.ItemsSource = new List<PropertyGroup> {
                new PropertyGroup { Key = "Properties A-Z", Items = filtered.OrderBy(p => p.Name).ToList() }
            };
        }
        else
        {
            // التجميع حسب الـ Category
            GroupsControl.ItemsSource = filtered.GroupBy(p => GetCategory(p.Info!))
                .Select(g => new PropertyGroup { Key = g.Key, Items = g.ToList() })
                .OrderBy(g => g.Key).ToList();
        }
    }

    // ربط البحث بالـ RefreshDisplay
    private void SearchBox_KeyUp(object? sender, KeyEventArgs e) => RefreshDisplay();

    private void SortAlphabetical_Click(object? sender, RoutedEventArgs e) { _isAlphabetical = true; RefreshDisplay(); }
    private void SortByCategory_Click(object? sender, RoutedEventArgs e) { _isAlphabetical = false; RefreshDisplay(); }

    private List<PropertyItem> GetFilteredProperties(Control element)
    {
        return element.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
            .Select(p => {
                try
                {
                    return new PropertyItem
                    {
                        Name = p.Name,
                        Value = p.GetValue(element),
                        PropertyType = p.PropertyType,
                        Info = p,
                        Target = element
                    };
                }
                catch { return null; }
            }).Where(p => p != null).Cast<PropertyItem>().ToList();
    }

    private string GetCategory(PropertyInfo prop)
    {
        string n = prop.Name.ToLower();
        if (n.Contains("width") || n.Contains("height") || n.Contains("margin") || n.Contains("canvas") || n.Contains("position")) return "Layout";
        if (n.Contains("color") || n.Contains("brush") || n.Contains("background") || n.Contains("opacity")) return "Appearance";
        if (n.Contains("text") || n.Contains("font") || n.Contains("content")) return "Content";
        return "Common";
    }
}