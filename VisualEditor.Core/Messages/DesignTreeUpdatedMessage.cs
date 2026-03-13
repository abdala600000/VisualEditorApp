using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace VisualEditor.Core.Messages
{
    public class DesignTreeUpdatedMessage
    {
        public Control RootControl { get; set; }

        public DesignTreeUpdatedMessage(Control rootControl)
        {
            RootControl = rootControl;
        }
    }
}
