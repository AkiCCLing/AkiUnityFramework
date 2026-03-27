# 🚀 5分钟快速开始指南

## 第1步: 准备 Prefab

在你的 Prefab 中添加 UI 组件，**使用特定的前缀命名**：

```
你的Prefab名称
├── @txt_Username (TextMeshProUGUI)
├── @img_Avatar (Image)
├── @btn_Login (Button)
└── @Content (GameObject)
```

**前缀速查：**
- 文本框 → `@txt_`
- 图片 → `@img_`
- 按钮 → `@btn_`
- 输入框 → `@input_`
- 原始图 → `@raw_`
- 通用物体 → `@`

---

## 第2步: 生成脚本

在 Prefab 上 **右键** 选择：

```
Create/UIFrame/UIPanel
（或 UIWindow / UIComponent / UIBase）
```

✨ **自动生成两个文件：**
- `MyPanel.cs` - 主脚本（partial class）
- `MyPanel.Binding.cs` - 绑定脚本（自动生成）

**同时自动添加：**
- ✅ 脚本组件自动添加到 Prefab 根节点
- ✅ 所有字段自动绑定到对应的UI组件

---

## 第3步: 在代码中使用

### 生成的代码

**MyPanel.cs 中已自动声明字段：**
```csharp
[SerializeField] private TextMeshProUGUI txtMainTitle;
[SerializeField] private Button btnExit;
[SerializeField] private TextMeshProUGUI txtContent;
```

**MyPanel.Binding.cs 中自动生成的绑定代码：**
```csharp
private void BindReferences()
{
    txtMainTitle = transform.Find("@txt_MainTitle").GetComponent<TextMeshProUGUI>();
    btnExit = transform.Find("@btn_Exit").GetComponent<Button>();
    txtContent = transform.Find("@txt_Content").GetComponent<TextMeshProUGUI>();
}
```

---

## 第4步: 新增组件？快速更新！

如果你在 Prefab 中新增了 `@txt_NewField`，无需重新创建脚本！

**右键 Prefab** 选择：
```
UIFrame/重新生成绑定脚本
```

✨ **自动更新 MyPanel.Binding.cs**，包含新的字段绑定

---

## 完成！🎉

你现在可以在代码中直接使用这些字段了：

```csharp
public override void OnCreate()
{
    // 现在这些字段已经被自动绑定了
    username.text = "游客";
    avatar.sprite = Resources.Load<Sprite>("avatar");
    login.onClick.AddListener(OnLoginClick);
}

private void OnLoginClick()
{
    // 处理登录逻辑
}
```

---

## 🤔 常见问题

**Q: Binding.cs 文件能删除吗？**
> 不能。它包含关键的绑定代码。但可以在 .gitignore 中排除它（因为是自动生成）。

**Q: 我手动改了 Binding.cs，重新生成会丢失吗？**
> 是的。重新生成时会覆盖整个 Binding.cs。请在主脚本的 Private Functions 区域添加自定义逻辑。

**Q: 前缀不匹配会怎样？**
> 不会自动绑定。例如 `@MyField` 会被识别为通用 GameObject，`@xxx_MyField` 中的 `xxx` 不是标准前缀也会当作通用物体处理。

**Q: 支持嵌套 Prefab 吗？**
> 支持！BreadthTraversal 会遍历所有子层级的 `@` 组件。

---

## 📖 需要更多帮助？

- **详细文档**: 查看 `AutoBindGuide.md`
- **快速查询**: 参考 `QUICK_REFERENCE.md`
- **技术实现**: 阅读 `IMPLEMENTATION_GUIDE_EN.md`

---

**就这样！你现在已经掌握了 UIFrame 自动绑定的全部核心功能！** 🎯
