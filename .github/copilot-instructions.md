# Copilot Instructions

## Preferences
- 用户偏好中文（zh-CN）输出，添加注释时应使用中文。
- 使用 Visual Studio 2026 进行开发。
## Rules
- **Every Rules In The List Should Be Followed Strictly.**
- 如果用户给出的信息以及通过多种渠道获取的信息不足以完成任务，必须直接告诉用户：*我需要更多信息*。**不要编造任何信息**。
- 如果用户给出的信息模糊或存在歧义，必须直接告诉用户：*我需要更明确的信息*。**不要编造任何信息**。
## Agent Directions
- 编写代码时，所有函数都要添加XML注释，且注释内容必须使用中文。函数内部的重要部分也需要添加行内注释，注释内容同样使用中文。
- 使用 `if` 或 `try - catch` 判断异常时，必须在异常处理代码块中添加日志记录，记录异常信息。
- 日志的规范为 : `[组件名(类名)]:异常信息`
## ReShaper Preferences
- 优先采用较新的 C# 语言特性，如使用集合表达式，模式匹配等。
- 检测Unity对象是否为null时，不要使用诸如 `(pathLineRenderer == null)`，而使用 `(!pathLineRenderer)`
- 在查找，遍历时，优先使用LINQ表达式。