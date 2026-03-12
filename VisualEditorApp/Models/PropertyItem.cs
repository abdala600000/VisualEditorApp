using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

 




public partial class PropertyDescriptor : ObservableObject
{
    private readonly object _target;
    private readonly PropertyInfo _propertyInfo;

    public string Name => _propertyInfo.Name;
    public string Category { get; set; }

    [ObservableProperty]
    private object? _value;

    public PropertyDescriptor(object target, PropertyInfo propInfo, string category)
    {
        _target = target;
        _propertyInfo = propInfo;
        Category = category;
        _value = propInfo.GetValue(target);
    }

    // عندما تتغير القيمة في الواجهة، يتم تحديث الكنترول الأصلي فوراً
    partial void OnValueChanged(object? value)
    {
        try
        {
            // تحويل القيمة لنوع الخاصية الأصلي (مثلاً من string لـ double)
            var convertedValue = Convert.ChangeType(value, _propertyInfo.PropertyType);
            _propertyInfo.SetValue(_target, convertedValue);
        }
        catch { /* تجاهل أخطاء التحويل */ }
    }
}
