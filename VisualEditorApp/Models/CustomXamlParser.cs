using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace VisualEditorApp.Models
{
    public class CustomXamlParser
    {
        // الدالة الرئيسية: تستلم النص وترجع العنصر الجذري (Root) بكامل تفاصيله المتداخلة
        public Control? ParseDocument(string xmlText)
        {
            XDocument doc = XDocument.Parse(xmlText);
            if (doc.Root == null) return null;

            // نبدأ ببناء الشجرة من الجذر
            return ParseElement(doc.Root) as Control;
        }

        // --- المحرك الشجري (Recursive Parser) ---
        private object? ParseElement(XElement element)
        {
            string typeName = element.Name.LocalName;

            // 1. إنشاء العنصر الحالي (مثلاً Border أو LinearGradientBrush)
            object? instance = CreateInstanceDynamically(typeName);
            if (instance == null) return null;

            // 2. حقن الخصائص المكتوبة كـ Attributes (مثل Width, Height, StartPoint)
            ApplyAttributes(instance, element);

            // 3. معالجة العناصر المتداخلة (الأبناء)
            foreach (var childElement in element.Elements())
            {
                string childName = childElement.Name.LocalName;

                // الحالة الأولى: خاصية فرعية (Property Element) مثل <Border.Background>
                if (childName.Contains("."))
                {
                    string propName = childName.Split('.')[1]; // استخراج كلمة Background
                    PropertyInfo? propInfo = instance.GetType().GetProperty(propName);

                    if (propInfo != null && childElement.HasElements)
                    {
                        // نقرأ العنصر اللي جوه الخاصية (مثلاً LinearGradientBrush)
                        object? propValue = ParseElement(childElement.Elements().First());
                        if (propValue != null)
                        {
                            propInfo.SetValue(instance, propValue);
                        }
                    }
                }
                // الحالة الثانية: عنصر محتوى عادي (Content أو Collection) مثل <GradientStop> أو <TextBlock>
                else
                {
                    object? childInstance = ParseElement(childElement);
                    if (childInstance != null)
                    {
                        AddChildToParent(instance, childInstance);
                    }
                }
            }

            return instance;
        }

        // --- دالة ذكية لإضافة الأبناء للآباء بناءً على نوعهم ---
        private void AddChildToParent(object parent, object child)
        {
            if (parent is Panel panel && child is Control control)
            {
                panel.Children.Add(control); // مثل إضافة Border داخل StackPanel
            }
            else if (parent is ContentControl contentControl && child is Control childCtrl1)
            {
                contentControl.Content = childCtrl1; // مثل إضافة StackPanel داخل Window
            }
            else if (parent is Decorator decorator && child is Control childCtrl2)
            {
                decorator.Child = childCtrl2; // مثل إضافة TextBlock داخل Border
            }
            else if (parent is GradientBrush brush && child is GradientStop stop)
            {
                brush.GradientStops.Add(stop); // تجميع نقاط التدرج اللوني داخل الـ Brush
            }
        }

        // --- إنشاء العناصر ديناميكياً ---
        private object? CreateInstanceDynamically(string typeName)
        {
            // خريطة سريعة للعناصر الشائعة لتسريع الأداء (خصوصاً الفرش)
            switch (typeName)
            {
                case "Window": return new Window();
                case "StackPanel": return new StackPanel();
                case "Border": return new Border();
                case "TextBlock": return new TextBlock();
                case "LinearGradientBrush": return new LinearGradientBrush();
                case "RadialGradientBrush": return new RadialGradientBrush();
                case "GradientStop": return new GradientStop();
            }

            // محرك بحث ديناميكي لأي كنترول آخر
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName != null && (assembly.FullName.StartsWith("System") || assembly.FullName.StartsWith("Microsoft"))) continue;
                var type = assembly.GetTypes().FirstOrDefault(t => t.Name == typeName);
                if (type != null) return Activator.CreateInstance(type);
            }
            return null;
        }

        // --- حقن الخصائص وتحويل الأنواع ---
        private void ApplyAttributes(object instance, XElement element)
        {
            var instanceType = instance.GetType();
            foreach (var attribute in element.Attributes())
            {
                string propName = attribute.Name.LocalName;
                string xmlValue = attribute.Value;

                // تجاهل مساحات الأسماء الخاصة بـ XAML 
                if (propName.StartsWith("xmlns") || propName.Contains(":")) continue;

                PropertyInfo? propInfo = instanceType.GetProperty(propName);
                if (propInfo != null && propInfo.CanWrite)
                {
                    try
                    {
                        object? convertedValue = ConvertToAvaloniaType(xmlValue, propInfo.PropertyType);
                        if (convertedValue != null) propInfo.SetValue(instance, convertedValue);
                    }
                    catch { }
                }
            }
        }

        private object? ConvertToAvaloniaType(string value, Type targetType)
        {
            if (targetType == typeof(IBrush)) return Brush.Parse(value);
            if (targetType == typeof(Color)) return Color.Parse(value);
            if (targetType == typeof(Thickness)) return Thickness.Parse(value);
            if (targetType == typeof(CornerRadius)) return CornerRadius.Parse(value);
            if (targetType == typeof(RelativePoint)) return RelativePoint.Parse(value); // مهمة جداً لـ StartPoint و EndPoint
            if (targetType.IsEnum) return Enum.Parse(targetType, value, true);
            if (targetType == typeof(double) && double.TryParse(value, out double d)) return d;
            if (targetType == typeof(int) && int.TryParse(value, out int i)) return i;
            if (targetType == typeof(bool) && bool.TryParse(value, out bool b)) return b;
            if (targetType == typeof(string) || targetType == typeof(object)) return value;
            return null;
        }
    }
}
