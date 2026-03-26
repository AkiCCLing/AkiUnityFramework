# UIFrame 自动绑定功能 - 实现总结

## 已完成的改动

### 1. 新增代码生成器

#### AutoBindCodeSnippetGenerator.cs
- **位置**: `Packages/com.feif.uiframe/Editor/Scripts/CodeSnippetGenerator/AutoBindCodeSnippetGenerator.cs`
- **功能**: 
  - 扫描Prefab中所有以`@`开头的GameObject
  - 根据命名前缀（txt_、img_、btn_、input_、raw_）生成相应的绑定代码
  - 支持TextMeshProUGUI、Image、Button、InputField、RawImage和通用GameObject绑定

#### TextMeshProCodeSnippetGenerator.cs
- **位置**: `Packages/com.feif.uiframe/Editor/Scripts/CodeSnippetGenerator/TextMeshProCodeSnippetGenerator.cs`
- **功能**: 为@txt_前缀的GameObject生成TextMeshProUGUI字段声明
- **优先级**: 125（在标准生成器之间）

### 2. 修改脚本创建器

#### UIScriptCreator.cs
新增功能：
- `GenerateBindingPartialScript()` - 生成绑定partial脚本
- `RegenerateBindingScript()` - 菜单项：重新生成绑定脚本

**新增菜单项**：
```
Assets/UIFrame/重新生成绑定脚本
```
右键点击Prefab后即可快速更新所有绑定

### 3. 修改所有模板文件

所有模板文件的主类已改为`partial class`：

- UIBaseTemplate.txt
- UIComponentTemplate.txt  
- UIPanelTemplate.txt
- UIWindowTemplate.txt

**示例**：
```csharp
// 修改前
public class #SCRIPTNAME# : UIComponent<#SCRIPTNAME#Data>

// 修改后
public partial class #SCRIPTNAME# : UIComponent<#SCRIPTNAME#Data>
```

### 4. 模板代码结构

所有模板现在统一使用 `#region` 分区：

```csharp
#region Override Functions
// OnCreate, OnRefresh, OnBind, OnUnbind, OnShow, OnHide, OnDied
#endregion

#region Private Functions
#FUNCTIONS#
#endregion
```

## 命名规范

| 前缀 | 类型 | 字段类型 | 示例 |
|------|------|---------|------|
| `@txt_` | TextMeshProUGUI | `TextMeshProUGUI` | `@txt_Title` → `title` |
| `@img_` | Image | `Image` | `@img_Icon` → `icon` |
| `@btn_` | Button | `Button` | `@btn_Submit` → `submit` |
| `@input_` | InputField | `InputField` | `@input_Name` → `name` |
| `@raw_` | RawImage | `RawImage` | `@raw_Background` → `background` |
| `@` | GameObject | `GameObject` | `@Content` → `content` |

## 使用工作流

### 步骤1：创建脚本
```
在Prefab上右键 → Create/UIFrame/UIPanel (或UIWindow/UIComponent)
```
**自动生成**：
- `MyPanel.cs` (partial class)
- `MyPanel.Binding.cs` (绑定代码)
- **脚本组件自动添加到Prefab根节点**

### 步骤2：添加UI组件
在Prefab中新增以`@`开头的GameObject，例如：
- `@txt_Title` (添加TextMeshProUGUI组件)
- `@img_Icon` (添加Image组件)
- `@btn_Close` (添加Button组件)

### 步骤3：更新绑定脚本
```
在Prefab上右键 → UIFrame/重新生成绑定脚本
```
**自动更新**：`MyPanel.Binding.cs` 中的绑定代码，并确保脚本组件已添加到Prefab

## 生成的脚本格式

### 主脚本 (MyPanel.cs)
```csharp
public partial class MyPanel : UIComponent<MyPanelData>
{
    #region Override Functions
    protected override Task OnCreate()
    {
        return Task.CompletedTask;
    }
    
    protected override void OnBind()
    {
        BindReferences();  // 自动调用绑定方法
    }
    #endregion

    #region Private Functions
    // 您的自定义方法
    #endregion
}
```

### 绑定脚本 (MyPanel.Binding.cs)
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

## 技术特点

✅ **Partial Class 模式**
- 主脚本和绑定脚本分离
- 便于维护和版本控制
- 支持多次重新生成而不丢失主脚本内容

✅ **自动化流程**
- 创建脚本时自动生成绑定脚本
- 菜单快速重新生成
- 支持增量更新

✅ **灵活的命名规范**
- 前缀明确指示组件类型
- 首字母自动小写处理
- 支持通用GameObject绑定

✅ **一致的代码风格**
- 统一的#region分区
- 标准的缩进格式
- 自动using语句管理

## 文件变更清单

| 文件 | 变更类型 | 说明 |
|------|---------|------|
| UIScriptCreator.cs | 修改 | 添加绑定脚本生成和菜单项 |
| AutoBindCodeSnippetGenerator.cs | 新建 | 核心绑定代码生成逻辑 |
| TextMeshProCodeSnippetGenerator.cs | 新建 | TextMeshPro支持 |
| UIBaseTemplate.txt | 修改 | 改为partial class |
| UIComponentTemplate.txt | 修改 | 改为partial class |
| UIWindowTemplate.txt | 修改 | 改为partial class |
| UIPanelTemplate.txt | 修改 | 改为partial class |
| AutoBindGuide.md | 新建 | 用户使用文档 |

## 注意事项

1. **Binding.cs 文件为自动生成**
   - 不应手动编辑 Auto Bind References 区域
   - 重新生成时会覆盖该区域内容

2. **字段命名**
   - 从 `@txt_MyTitle` 生成的字段为 `myTitle` (首字母小写)
   - 命名遵循camelCase规范

3. **Transform.Find 路径**
   - 绑定使用完整的GameObject名称（包括@符号）
   - 确保Prefab层级中的GameObject名称唯一

4. **兼容性**
   - 完全兼容现有的UIBase代码生成系统
   - 不影响其他代码片段生成器的功能
   - 支持所有UIComponent派生类

## 后续扩展可能性

- 支持更多UI组件类型（Slider、Toggle等）
- 支持自定义绑定规则
- 支持批量重新生成多个Prefab的绑定脚本
- 性能分析工具集成
