using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace VisualEditorApp.ViewModels
{
    public partial class NewProjectWizardViewModel : ObservableObject
    {
        // المتغير ده بيشيل الشاشة الحالية (سواء كانت Template أو Configuration)
        [ObservableProperty]
        private object _currentPage;

        // إشارة عشان نقفل النافذة الكبيرة كلها لما نخلص أو نلغي
        public event EventHandler RequestClose;

        public NewProjectWizardViewModel()
        {
            // 1. أول ما نفتح، نجهز شاشة التمبليت
            var templateVM = new TemplateSelectionViewModel();

            // 2. لو المستخدم داس Cancel جوه التمبليت، نقفل النافذة كلها
            templateVM.RequestClose += (sender, e) => RequestClose?.Invoke(this, EventArgs.Empty);

            templateVM.RequestNavigateNext += (sender, selectedTemplate) =>
            {
                // 🎯 هنا بنبعت الـ ShortName الحقيقي (زي console أو wpf أو avalonia.app) للشاشة التانية
                var configVM = new ProjectConfigurationViewModel(selectedTemplate.ShortName);

                configVM.RequestClose += (s, args) => RequestClose?.Invoke(this, EventArgs.Empty);

                CurrentPage = configVM;
            };

            // تعيين شاشة البداية
            CurrentPage = templateVM;
        }
    }
}
