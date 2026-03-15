using Avalonia.Controls;
using System.Reflection;

namespace VisualEditor.Core.Messages
{
    public class PropertyChangedMessage
    {
        public PropertyInfo Property { get; }
        public object? Value { get; }
        public Control Target { get; }

        public PropertyChangedMessage(PropertyInfo property, object? value, Control target)
        {
            Property = property;
            Value = value;
            Target = target;
        }
    }
}
