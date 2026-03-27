# UIFrame Auto Binding - Implementation Guide

## Overview

This update adds automatic UI component binding functionality to the UIFrame framework. When generating scripts from Prefabs, the system automatically creates a `partial` class for binding all UI components prefixed with `@` in the Prefab hierarchy.

## Key Features

✅ **Automatic Generation** - Binding scripts are created when you generate the main script  
✅ **Incremental Updates** - Quick regeneration menu for new `@` components  
✅ **Clean Separation** - Partial class pattern keeps main logic and bindings separate  
✅ **Smart Naming** - Automatic camelCase conversion (e.g., `@txt_MyTitle` → `myTitle`)  
✅ **Full Compatibility** - Works seamlessly with existing UIFrame code generators  

## Naming Convention

Use these prefixes for automatic component detection:

| Prefix | Component Type | Field Type | Example |
|--------|----------------|-----------|---------|
| `@txt_` | TextMeshPro Text | `TextMeshProUGUI` | `@txt_Title` → `title` |
| `@img_` | UI Image | `Image` | `@img_Icon` → `icon` |
| `@btn_` | UI Button | `Button` | `@btn_Submit` → `submit` |
| `@input_` | InputField | `InputField` | `@input_Name` → `name` |
| `@raw_` | RawImage | `RawImage` | `@raw_BG` → `bg` |
| `@` | Any GameObject | `GameObject` | `@Content` → `content` |

## Workflow

### Step 1: Create Script from Prefab
```
Right-click Prefab → Create/UIFrame/UIPanel (or Window/Component/Base)
```
**Auto-generates:**
- `MyPanel.cs` (partial class with field declarations)
- `MyPanel.Binding.cs` (binding implementation)
- **Script component automatically added to Prefab root**

### Step 2: Add UI Components
Name your Prefab children with `@` prefix:
- `@txt_Username` (add TextMeshProUGUI)
- `@btn_Login` (add Button)
- `@img_Icon` (add Image)

### Step 3: Update Binding Script
When you add new `@` components:
```
Right-click Prefab → UIFrame/Regenerate Binding Script
```
**Updates:** `MyPanel.Binding.cs` with new components and ensures script component is attached

## Generated Code Example

**Prefab Structure:**
```
MyPanel
├── @txt_Title
├── @img_Avatar
├── @btn_Close
└── @content
```

**Main Script (MyPanel.cs):**
```csharp
public partial class MyPanel : UIComponent<MyPanelData>
{
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private Image avatar;
    [SerializeField] private Button close;
    [SerializeField] private GameObject content;
    
    #region Override Functions
    protected override Task OnCreate()
    {
        return Task.CompletedTask;
    }
    
    protected override void OnBind()
    {
        BindReferences();  // Call binding method
    }
    #endregion

    #region Private Functions
    // Your custom methods here
    #endregion
}
```

**Binding Script (MyPanel.Binding.cs):**
```csharp
public partial class MyPanel
{
    #region Auto Bind References
    #region Field Declarations
    [SerializeField] private TextMeshProUGUI txtMainTitle;
    [SerializeField] private Button btnExit;
    [SerializeField] private TextMeshProUGUI txtContent;
    #endregion

    private void BindReferences()
    {
        txtMainTitle = transform.Find("@txt_MainTitle").GetComponent<TextMeshProUGUI>();
        btnExit = transform.Find("@btn_Exit").GetComponent<Button>();
        txtContent = transform.Find("@txt_Content").GetComponent<TextMeshProUGUI>();
    }
    #endregion
}
```

## Files Modified/Created

### New Files
- `AutoBindCodeSnippetGenerator.cs` - Core binding code generation logic
- `TextMeshProCodeSnippetGenerator.cs` - TextMeshPro component support
- `AutoBindGuide.md` - User guide (Chinese)
- `QUICK_REFERENCE.md` - Quick reference (Chinese)
- `IMPLEMENTATION_SUMMARY_CN.md` - Implementation details (Chinese)

### Modified Files
- `UIScriptCreator.cs` - Added binding generation and menu items
- `UIBaseTemplate.txt` - Changed to `partial class`
- `UIComponentTemplate.txt` - Changed to `partial class`
- `UIWindowTemplate.txt` - Changed to `partial class`
- `UIPanelTemplate.txt` - Changed to `partial class`

## Code Structure

All templates now use unified `#region` organization:

```csharp
public partial class MyClass : UIComponent<MyClassData>
{
    [SerializeField] private TextMeshProUGUI field1;
    // ... more fields
    
    #region Override Functions
    protected override Task OnCreate() { }
    protected override Task OnRefresh() { }
    protected override void OnBind() { }
    // ... etc
    #endregion

    #region Private Functions
    // Your methods here
    #endregion
}
```

## Technical Details

### Partial Class Pattern
- Main script contains field declarations and override methods
- Binding script contains the `BindReferences()` method
- Both are marked as `partial` to coexist
- Binding script can be safely regenerated without affecting main logic

### Naming Transformation
```
@txt_MyComponent → myComponent
@img_UserAvatar → userAvatar
@btn_SubmitForm → submitForm
@input_EmailField → emailField
@Content → content
```

### Field Declaration
Fields are automatically declared in the main script with `[SerializeField]`:
```csharp
[SerializeField] private TextMeshProUGUI myComponent;
```

### Binding Implementation
The `BindReferences()` method uses `transform.Find()` to locate components:
```csharp
myComponent = transform.Find("@txt_MyComponent").GetComponent<TextMeshProUGUI>();
```

## Context Sensitivity

The code generator is context-aware:
- Detects component types from GameObject children
- Skips already-processed GameObjects (based on priority)
- Only generates code for properly named `@` components
- Validates component existence before binding

## Best Practices

1. **Naming Consistency** - Always use consistent prefix patterns
2. **Component Naming** - Keep GameObject names descriptive but reasonable length
3. **Hierarchy Organization** - Group related components in folders
4. **Binding Timing** - Call `BindReferences()` in `OnBind()` or `OnCreate()`
5. **Version Control** - Consider excluding `.Binding.cs` from commits (auto-generated)

## Compatibility

- ✅ TextMeshPro/TMPro UI components
- ✅ Unity UGUI (Button, Image, InputField, RawImage)
- ✅ Generic GameObject references
- ✅ All UIBase-derived classes
- ✅ Nested Prefabs
- ✅ Multiple instances of same component type

## Menu Items

**Creation Menu:**
```
Assets/Create/UIFrame/
├── UIBase
├── UIComponent
├── UIPanel
└── UIWindow
```

**Regeneration Menu:**
```
Assets/UIFrame/
└── 重新生成绑定脚本 (Regenerate Binding Script)
```

## Common Use Cases

### Simple Panel with Text and Button
```
Prefab: @txt_Message, @btn_OK
Generated: Fields + BindReferences()
```

### List with Item Template
```
Prefab: @content (parent), multiple @item_X children
Generated: Binding for parent and all items
```

### Complex Dialog
```
Prefab: Multiple @txt_, @btn_, @img_ components
Generated: Complete binding for all UI elements
```

## Future Enhancements

- Support for additional UGUI components (Slider, Toggle)
- Custom binding rules configuration
- Batch regeneration for multiple Prefabs
- Performance metrics integration
- Asset validation tools

## Troubleshooting

**Issue: Component not found in binding**
> Solution: Ensure GameObject name exactly matches the reference in Binding.cs

**Issue: Field type mismatch**
> Solution: Verify the `@` prefix is correct for the component type

**Issue: Script won't compile**
> Solution: Check that main script and binding script are both partial classes in same namespace

---

**Version**: 1.0  
**Framework**: UIFrame with TextMeshPro Support  
**Last Updated**: 2024
