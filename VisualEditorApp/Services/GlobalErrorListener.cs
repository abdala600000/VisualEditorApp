using System;
using System.Diagnostics;
using VisualEditor.Core.Messages;
using VisualEditor.Core.Models;

namespace VisualEditorApp.Services
{
    /// <summary>
    /// صائد الأخطاء العام (Global Trace Listener)
    /// يقوم بالتقاط أي تنبيهات أو أخطاء تخرج من البرامج أو المكتبات أو حتى أفالونيا (Binding Errors)
    /// ويقوم بتحويلها لرسائل تظهر في الـ ErrorList
    /// </summary>
    public class GlobalErrorListener : TraceListener
    {
        public override void Write(string? message) { }

        public override void WriteLine(string? message)
        {
            if (string.IsNullOrEmpty(message)) return;

            // تحديد ما إذا كانت الرسالة تشير لخطأ أو تنبيه
            bool isBindingError = message.Contains("Binding") || message.Contains("Property");
            bool isGenericError = message.ToLower().Contains("error") || message.ToLower().Contains("fail");
            bool isWarning = message.ToLower().Contains("warning");

            if (isBindingError || isGenericError || isWarning)
            {
                var severity = (isGenericError || isBindingError) ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning;
                
                MessageBus.Send(SystemDiagnosticMessage.Create(
                    severity, 
                    isBindingError ? "BIND001" : "SYS002", 
                    message, 
                    "System/UI",
                    "Global"
                ));
            }
        }

        public static void Register()
        {
            // إضافة الصائد لجميع المخرجات البرمجية (Trace & Debug)
            Trace.Listeners.Add(new GlobalErrorListener());
        }
    }
}
