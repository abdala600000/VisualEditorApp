# مهام تحسين Visual Editor - خطة التنفيذ

---

## 🔴 أولوية عالية

### 1. إصلاح مزامنة RenderTransform مع XAML
**الملف**: `VisualEditorApp/Views/Documents/WorkspaceView.axaml.cs`
**الدالة**: `MyDesignSurface_DesignChanged`

**المشكلة**: الكود يكتب فقط `RotateTransform` في الـ XAML، لكن العنصر قد يحمل `TransformGroup` (Skew + Rotate).

**خطوات التنفيذ**:
1. في `MyDesignSurface_DesignChanged`، استبدل الكود الحالي:
   ```csharp
   if (targetControl.RenderTransform is RotateTransform rt)
       xml = XamlDOMPatcher.PatchProperty(xml, targetControl, "RenderTransform", $"rotate({rt.Angle})");
   ```
   بكود يتعامل مع `TransformGroup`:
   ```csharp
   if (targetControl.RenderTransform is RotateTransform rt)
       xml = XamlDOMPatcher.PatchRenderTransform(xml, targetControl, rt.Angle, 0, 0);
   else if (targetControl.RenderTransform is TransformGroup tg)
   {
       var rotate = tg.Children.OfType<RotateTransform>().FirstOrDefault();
       var skew   = tg.Children.OfType<SkewTransform>().FirstOrDefault();
       xml = XamlDOMPatcher.PatchRenderTransform(xml, targetControl,
           rotate?.Angle ?? 0, skew?.AngleX ?? 0, skew?.AngleY ?? 0);
   }
   ```
2. إضافة `using System.Linq;` إن لم يكن موجوداً.

---

### 2. إصلاح XamlDOMPatcher - كتابة RenderTransform كـ property element
**الملف**: `VisualEditor.Core/XamlDOMPatcher.cs`

**المشكلة**: `PatchProperty` يكتب `RenderTransform="rotate(45)"` وهذا ليس XAML صحيحاً في Avalonia.

**خطوات التنفيذ**:
1. إضافة دالة جديدة `PatchRenderTransform` في `XamlDOMPatcher`:
   ```csharp
   public static string PatchRenderTransform(string xaml, Control target, double angle, double skewX, double skewY)
   ```
2. الدالة تبحث عن العنصر بالاسم وتستبدل أو تضيف `<Control.RenderTransform>` كـ property element.
3. إذا كانت كل القيم صفر، تحذف الـ `RenderTransform` بالكامل.
4. إذا كان `skewX == 0 && skewY == 0`، تكتب `<RotateTransform Angle="45"/>` فقط.
5. إذا كان هناك Skew، تكتب `<TransformGroup>` يحتوي على `<RotateTransform>` و`<SkewTransform>`.

---

### 3. إصلاح الـ Adorner مع SkewTransform
**الملف**: `VisualEditor.Designer/DesignerSurfaceView.axaml.cs`
**الدالة**: `UpdateAdornerPosition`

**المشكلة**: عند تطبيق `SkewTransform`، الـ Adorner لا يتبع شكل العنصر المائل.

**خطوات التنفيذ**:
1. في `UpdateAdornerPosition`، في حالة العنصر الواحد، بعد تطبيق `RotateTransform` على الـ Adorner:
   ```csharp
   else if (savedTransform is TransformGroup tg)
   {
       var rotate = tg.Children.OfType<RotateTransform>().FirstOrDefault();
       var skew   = tg.Children.OfType<SkewTransform>().FirstOrDefault();
       var group  = new TransformGroup();
       if (skew   != null) group.Children.Add(new SkewTransform(skew.AngleX, skew.AngleY));
       if (rotate != null) group.Children.Add(new RotateTransform(rotate.Angle));
       SelectionAdorner.RenderTransform = group.Children.Count > 0 ? group : null;
   }
   ```

---

### 4. حذف المتغير غير المستخدم `_dragStartControlPosition`
**الملف**: `VisualEditor.Designer/DesignerSurfaceView.axaml.cs`

**المشكلة**: `_dragStartControlPosition` معرّف لكن لا يُستخدم أبداً → تحذير compiler.

**خطوات التنفيذ**:
1. احذف السطر: `private Point _dragStartControlPosition;`
2. تأكد أنه غير مستخدم في أي مكان آخر بالبحث عنه.

---

### 5. حذف `write_axaml.py` من الـ git
**الملف**: `write_axaml.py`

**المشكلة**: ملف Python مؤقت تم commit-ه بالخطأ.

**خطوات التنفيذ**:
1. تشغيل: `git rm write_axaml.py`
2. إضافة `write_axaml.py` لملف `.gitignore`
3. Commit التغيير.

---

## 🟡 أولوية متوسطة

### 6. إضافة Undo/Redo للـ Rotation وSkew
**الملف**: `VisualEditor.Designer/DesignerSurfaceView.axaml.cs`
**الدوال**: `RotateHandle_PointerReleased`, `SkewHandle_PointerReleased`

**المشكلة**: `HistoryService.RegisterChange` لا يُستدعى بعد الدوران والإمالة.

**خطوات التنفيذ**:
1. في `RotateHandle_PointerPressed`، احفظ الـ transform الأصلي:
   ```csharp
   _rotateStartTransform = _selectedControl?.RenderTransform;
   ```
2. في `RotateHandle_PointerReleased`، سجّل العملية:
   ```csharp
   var ctrl = _selectedControl;
   var oldT = _rotateStartTransform;
   var newT = ctrl?.RenderTransform;
   HistoryService.Instance.RegisterChange(
       undo: () => { if (ctrl != null) ctrl.RenderTransform = oldT; UpdateAdornerPosition(); DesignChanged?.Invoke(this, EventArgs.Empty); },
       redo: () => { if (ctrl != null) ctrl.RenderTransform = newT; UpdateAdornerPosition(); DesignChanged?.Invoke(this, EventArgs.Empty); }
   );
   ```
3. نفس الخطوات لـ `SkewHandle_PointerPressed/Released`.

---

### 7. تحسين `XamlSanitizer` - دعم TransformGroup
**الملف**: `VisualEditor.Core/XamlSanitizer.cs`

**المشكلة**: لا يتعامل مع `TransformGroup` في الـ XAML عند التنظيف.

**خطوات التنفيذ**:
1. مراجعة الـ regex الحالية في `XamlSanitizer`.
2. إضافة قاعدة تسمح بـ `<TransformGroup>`, `<RotateTransform>`, `<SkewTransform>` كـ property elements آمنة.
3. التأكد أن `RenderTransform` كـ property element لا يُحذف بالخطأ.

---

### 8. إصلاح `PropertiesToolView` - عرض خصائص العنصر المحدد
**الملف**: `VisualEditorApp/Views/Tools/PropertiesToolView.axaml.cs`

**المشكلة**: `PropertiesToolView` لا تعرض أي خصائص للعنصر المحدد.

**خطوات التنفيذ**:
1. في `PropertiesToolView`, اشترك في `MessageBus.ControlSelected`.
2. عند استقبال `ControlSelectedMessage`, استخرج خصائص العنصر ديناميكياً باستخدام Reflection.
3. اعرض الخصائص في `ItemsControl` أو `DataGrid` بسيط.
4. الخصائص المطلوبة كحد أدنى: `Name`, `Width`, `Height`, `Margin`, `Canvas.Left`, `Canvas.Top`, `Background`, `Foreground`.

---

### 9. إضافة حد أقصى لـ HistoryService
**الملف**: `VisualEditor.Designer/Services/HistoryService.cs`

**المشكلة**: الـ Stack ينمو بلا حدود مما قد يسبب استهلاك ذاكرة.

**خطوات التنفيذ**:
1. تغيير `Stack<Action>` إلى `LinkedList<Action>` أو استخدام `Queue` بحد أقصى.
2. إضافة ثابت `const int MaxHistory = 50;`
3. في `RegisterChange`، إذا تجاوز عدد العمليات الحد الأقصى، احذف الأقدم.

---

### 10. إصلاح `LoadDesign` - إعادة تعيين الحجم قبل الضبط
**الملف**: `VisualEditor.Designer/DesignerSurfaceView.axaml.cs`
**الدالة**: `LoadDesign`

**المشكلة**: عند فتح ملف جديد، الحجم القديم يبقى حتى يُحسب الحجم الجديد.

**خطوات التنفيذ**:
1. في بداية `LoadDesign`، أضف:
   ```csharp
   DesignSurfaceWrapper.Width  = double.NaN;
   DesignSurfaceWrapper.Height = double.NaN;
   ```
2. ثم في `Dispatcher.UIThread.Post` يتم ضبط الحجم الصحيح كما هو الآن.

---

## 🟢 أولوية منخفضة

### 11. توحيد التعليقات بالعربية
**الملفات**: جميع ملفات `.cs`

**المشكلة**: بعض التعليقات بالإنجليزية وبعضها يحتوي على إيموجي.

**خطوات التنفيذ**:
1. في `DesignerSurfaceView.axaml.cs`: استبدل التعليقات الإنجليزية بعربية.
2. احذف الإيموجي من التعليقات (🎯، 👈، إلخ).
3. توحيد نمط `// تعليق` بدلاً من `// 🎯 تعليق`.

---

### 12. حذف الكود المعلق `DuplicateSelected`
**الملف**: `VisualEditor.Designer/DesignerSurfaceView.axaml.cs`

**المشكلة**: يوجد كود `DuplicateSelected` معلق لا يُستخدم.

**خطوات التنفيذ**:
1. ابحث عن `DuplicateSelected` في الملف.
2. إذا كان معلقاً بالكامل، احذفه.
3. إذا كان مستخدماً في `KeyDown`، تأكد من تنفيذه الصحيح أو احذف الاستدعاء.

---

### 13. تحسين `XamlGenerator` - دعم خصائص إضافية
**الملف**: `VisualEditor.Core/XamlGenerator.cs`

**المشكلة**: لا يولّد `Background`, `Foreground`, `FontSize`, `RenderTransform`.

**خطوات التنفيذ**:
1. في دالة التوليد، أضف فحص للخصائص التالية:
   - `Background` إذا لم يكن `null`
   - `Foreground` إذا لم يكن `null`
   - `FontSize` إذا لم يكن القيمة الافتراضية
   - `RenderTransform` باستخدام `PatchRenderTransform` الجديدة

---

### 14. إضافة `CancellationToken` لعمليات البناء
**الملف**: `VisualEditorApp/ViewModels/MainWindowViewModel.cs`

**المشكلة**: عمليات البناء لا يمكن إلغاؤها.

**خطوات التنفيذ**:
1. إضافة `CancellationTokenSource? _buildCts;` كـ field.
2. في دالة البناء، أنشئ `_buildCts = new CancellationTokenSource()` وأرسل الـ token.
3. إضافة زر "إلغاء" في الـ UI يستدعي `_buildCts?.Cancel()`.
4. في `LiveDesignerCompiler`، تمرير الـ token لعمليات `Task.Run`.

---

### 15. إصلاح `new DocumentOutlineView` بدون إضافة للـ UI
**الملف**: `VisualEditorApp/ViewModels/MainWindowViewModel.cs`

**المشكلة**: `new DocumentOutlineView { DataContext = ... }` ينشئ كنترول لكن لا يضيفه لأي مكان.

**خطوات التنفيذ**:
1. ابحث عن هذا الكود في `MainWindowViewModel.cs`.
2. إذا كان الـ View يُعرض عبر `ContentControl` في الـ XAML، احذف الإنشاء اليدوي.
3. إذا كان مطلوباً، أضفه للـ UI المناسب أو استخدم `DataTemplate` بدلاً منه.
