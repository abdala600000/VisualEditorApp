using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.ComponentModel;
using System.Reflection;
using VisualEditor.Core.Messages;

namespace VisualEditor.Toolbox.Prop
{
    public class PropertyItem : INotifyPropertyChanged
    {
        public string Name { get; set; } = string.Empty;
        public Type? PropertyType { get; set; }
        public PropertyInfo? Info { get; set; }
        public Control? Target { get; set; }

        private object? _value;
        public object? Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));

                    // 👈 أول ما المستخدم يكتب قيمة جديدة، نطبقها فوراً
                    ApplyValueToControl(value);
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void ApplyValueToControl(object? newValue)
        {
            if (Target == null || Info == null || newValue == null) return;

            try
            {
                // 1. تحويل النص المكتوب للنوع اللي بيقبله أفلونيا
                object? convertedValue = ConvertValue(newValue.ToString(), PropertyType);

                if (convertedValue != null)
                {
                    // 2. تطبيق التعديل الفعلي على الكنترول (تغيير العرض، اللون، النص..)
                    Info.SetValue(Target, convertedValue);

                    // 3. 📢 نضرب جرس الإنذار: "يا ديزاينر، حدث الكود!"
                    WeakReferenceMessenger.Default.Send(new DesignChangedMessage());
                }
            }
            catch
            {
                // 🚫 بنتجاهل الأخطاء لو المستخدم كتب كلام غلط في مكان أرقام
            }
        }

        // دالة المترجم: بتحول الكلام المكتوب لأرقام أو ألوان أو مسافات
        private object? ConvertValue(string? stringValue, Type? targetType)
        {
            if (string.IsNullOrWhiteSpace(stringValue) || targetType == null) return null;

            if (targetType == typeof(double)) return double.Parse(stringValue);
            if (targetType == typeof(int)) return int.Parse(stringValue);
            if (targetType == typeof(bool)) return bool.Parse(stringValue);
            if (targetType == typeof(string)) return stringValue;

            // محول الألوان بتاع أفلونيا (لو كتب Red أو #FF0000)
            if (targetType == typeof(IBrush)) return Brush.Parse(stringValue);

            // محول المسافات Margin / Padding (لو كتب 10,5,10,5)
            if (targetType == typeof(Avalonia.Thickness)) return Avalonia.Thickness.Parse(stringValue);

            return null;
        }
    }

 
}