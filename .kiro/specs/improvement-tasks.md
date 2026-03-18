# مهام تحسين وإصلاح Visual Editor

## 🔴 أولوية عالية - مشاكل وظيفية

### 1. إصلاح مزامنة RenderTransform مع XAML
**الملف**: `WorkspaceView.axaml.cs` → `MyDesignSurface_DesignChanged`
**المشكلة**: الكود يكتب فقط `RotateTransform` في الـ XAML، لكن الآن العنصر قد يحمل `TransformGroup` (Skew + Rotate) أو `SkewTransform` فقط.
**الحل**: تعديل `MyDesignSurface_DesignChanged` ليكتب `TransformGroup` كاملاً في الـ XAML.

### 2. إصلاح XamlDOMPatcher.PatchProperty للـ RenderTransform
**الملف**: `XamlDOMPatcher.cs`
**المشكلة**: `PatchProperty` يكتب `RenderTransform="rotate(45)"` وهذا ليس XAML صحيحاً.
**الحل**: كتابة `RenderTransform` كـ property element وليس attribute.

### 3. إصلاح مشكلة الـ Adorner مع الـ SkewTransform
**الملف**: `DesignerSurfaceView.axaml.cs` → `UpdateAdornerPosition`
**المشكلة**: عند تطبيق `SkewTransform`، الـ Adorner لا يتبع شكل العنصر المائل.
**الحل**: تطبيق نفس الـ SkewTransform على `SelectionAdorner.RenderTransform`.

### 4. إصلاح `_dragStartControlPosition` غير المستخدم
**الملف**: `DesignerSurfaceView.axaml.cs`
**المشكلة**: المتغير `_dragStartControlPosition` معرّف لكن لا يُستخدم أبداً.
**الحل**: حذفه.

### 5. إصلاح `write_axaml.py` في الـ git
**الملف**: `write_axaml.py`
**المشكلة**: ملف Python مؤقت تم commit-ه بالخطأ.
**الحل**: حذفه وإضافته لـ `.gitignore`.

---

## 🟡 أولوية متوسطة - تحسينات وظيفية

### 6. إضافة Undo/Redo للـ Rotation وSkew
**الملف**: `DesignerSurfaceView.axaml.cs`
**المشكلة**: `HistoryService.RegisterChange` لا يُستدعى بعد الدوران والإمالة.
**الحل**: تسجيل الحالة قبل وبعد في `RotateHandle_PointerReleased` و`SkewHandle_PointerReleased`.

### 7. تحسين `XamlSanitizer` - إضافة تنظيف SkewTransform
**الملف**: `XamlSanitizer.cs`
**المشكلة**: لا يتعامل مع `TransformGroup` في الـ XAML.
**الحل**: إضافة regex لتنظيف الـ transforms غير الآمنة.

### 8. إصلاح `PropertiesToolView` - فارغة تماماً
**الملف**: `PropertiesToolView.axaml.cs`
**المشكلة**: `PropertiesToolView` لا تعرض أي خصائص للعنصر المحدد.
**الحل**: ربط الـ View بـ `ControlSelectedMessage` وعرض الخصائص ديناميكياً.

### 9. إضافة حد أقصى لـ HistoryService
**الملف**: `HistoryService.cs`
**المشكلة**: الـ Stack ينمو بلا حدود مما قد يسبب استهلاك ذاكرة.
**الحل**: تحديد حد أقصى (مثلاً 50 عملية).

### 10. إصلاح `LoadDesign` - حجم الـ DesignSurfaceWrapper
**الملف**: `DesignerSurfaceView.axaml.cs`
**المشكلة**: عند فتح ملف جديد، الحجم القديم يبقى حتى يُحسب الحجم الجديد.
**الحل**: إعادة تعيين الحجم لـ `NaN` أولاً ثم ضبطه بعد الـ layout.

---

## 🟢 أولوية منخفضة - تحسينات جودة الكود

### 11. توحيد نمط التعليقات
**الملفات**: جميع الملفات
**المشكلة**: بعض التعليقات بالعربية وبعضها بالإنجليزية، وبعضها يحتوي على إيموجي.
**الحل**: توحيد التعليقات بالعربية بدون إيموجي.

### 12. إزالة الكود المعلق (Commented Code)
**الملف**: `DesignerSurfaceView.axaml.cs`
**المشكلة**: يوجد كود `DuplicateSelected` معلق.
**الحل**: حذفه نهائياً.

### 13. تحسين `XamlGenerator` - دعم خصائص أكثر
**الملف**: `XamlGenerator.cs`
**المشكلة**: لا يولّد `Background`, `Foreground`, `FontSize`, `RenderTransform`.
**الحل**: إضافة هذه الخصائص للتوليد.

### 14. إضافة `CancellationToken` لعمليات البناء
**الملف**: `MainWindowViewModel.cs`
**المشكلة**: عمليات البناء لا يمكن إلغاؤها.
**الحل**: تمرير `CancellationToken` حقيقي مع زر إلغاء.

### 15. إصلاح `new DocumentOutlineView` بدون إضافة للـ UI
**الملف**: `MainWindowViewModel.cs`
**المشكلة**: `new DocumentOutlineView { DataContext = ... }` ينشئ كنترول لكن لا يضيفه لأي مكان.
**الحل**: مراجعة هذا الكود وإصلاحه أو حذفه.
