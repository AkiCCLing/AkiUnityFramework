# UIFrame 自动绑定功能说明

## 功能概述

本更新为UIFrame框架添加了自动绑定UI组件的功能。在生成脚本时，系统会自动创建一个partial类用于绑定Prefab中所有以`@`开头的UI组件。

## 命名规范

在Prefab中使用以下前缀命名您的UI组件，系统会自动生成对应的字段声明和绑定代码：

| 前缀 | 组件类型 | 字段类型 | 示例 |
|------|--------|---------|------|
| `@txt_` | 文本 | `TextMeshProUGUI` | `@txt_Title` → `txtTitle` |
| `@img_` | 图片 | `Image` | `@img_Icon` → `imgIcon` |
| `@btn_` | 按钮 | `Button` | `@btn_Close` → `btnClose` |
| `@input_` | 输入框 | `InputField` | `@input_UserName` → `inputUserName` |
| `@raw_` | 原始图片 | `RawImage` | `@raw_Background` → `rawBackground` |
| `@` | 物体 | `GameObject` | `@Content` → `content` |

## 生成过程

### 1. 创建脚本时自动生成

当您通过右键菜单创建UIFrame脚本时（UIBase、UIComponent、UIPanel、UIWindow），系统会自动：

1. 生成主脚本（例如：`MyPanel.cs`）- 声明为`partial class`
2. 生成绑定脚本（例如：`MyPanel.Binding.cs`）- 包含`BindReferences()`方法
3. **自动将脚本组件添加到Prefab根节点** - 无需手动添加

### 2. 重新生成绑定脚本

如果您在Prefab中新增了以`@`开头的组件，可以通过右键菜单快速更新绑定脚本，无需重新创建整个脚本：

**操作步骤：**
1. 在Assets面板中右键点击您的Prefab
2. 选择 `UIFrame/重新生成绑定脚本`
3. 系统会自动扫描新增的`@`组件并更新`*.Binding.cs`文件
4. **脚本组件会自动添加到Prefab根节点**（如果还没有的话）

## 生成的代码示例

**MyPanel.cs (主脚本)**
```csharp
public partial class MyPanel : UIComponent<MyPanelData>
{
    #region Override Functions
    protected override Task OnCreate() { }
    protected override void OnBind() { BindReferences(); }  // 自动调用绑定方法
    #endregion

    #region Private Functions
    // 您的自定义方法
    #endregion
}
```

**MyPanel.Binding.cs (绑定脚本)**
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

## 注意事项

1. **字段声明**：系统自动为每个`@`组件生成对应的`[SerializeField]`字段声明在主脚本中
2. **命名规则**：前缀后的首字母会被转换为小写（例如：`@txt_Title` -> `title`）
3. **绑定方法**：`BindReferences()`方法在`Auto Bind References`区域中，您不应该手动修改此区域
4. **Partial类**：主类声明为`partial`，允许与绑定脚本共存
5. **自动刷新**：重新生成绑定脚本后，资源库会自动刷新，无需手动操作

## 工作流程示例

1. 从Prefab创建UIPanel脚本 → 自动生成`*.Binding.cs`
2. 在Prefab中新增`@txt_NewText`组件
3. 右键选择`重新生成绑定脚本` → 更新`*.Binding.cs`以包含新的TextMeshPro字段
4. 编译完成，新字段可用

## 兼容性

- 支持TextMeshPro (TMPro)
- 支持Unity UGUI (Button、Image、InputField、RawImage)
- 支持通用GameObject引用
- 兼容所有UIBase派生类
