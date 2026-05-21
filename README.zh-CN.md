# vs-dte-cli

[English](README.md) | 简体中文

`vs-dte-cli` 是一个 Windows 命令行工具，用于在调试 .NET Framework
MSTest 时通过 Visual Studio DTE 控制 Visual Studio。

它可以启动 `vstest.console.exe`、打开或复用 Visual Studio、附加调试器、
在指定源码断点暂停、采集当前调试现场、单步执行、继续运行、设置下一条语句，
以及动态添加或移除断点。

## 环境要求

- Windows
- 支持 DTE 自动化的 Visual Studio
- .NET Framework 4.8 开发工具
- MSTest / `vstest.console.exe`

默认路径面向 Visual Studio 2026 Enterprise：

```text
VisualStudio.DTE.18.0
C:\Program Files\Microsoft Visual Studio\18\Enterprise\
```

如果你的 Visual Studio 版本或版本类型不同，可以使用 `--dte-prog-id`、
`--devenv` 和 `--vstest` 指定对应路径。

## 构建

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe' `
  .\src\VsDteCli\VsDteCli.csproj `
  /t:Build /p:Configuration=Debug /verbosity:minimal
```

Debug 构建产物：

```text
src\VsDteCli\bin\Debug\vsdte-cli.exe
```

## 快速开始

从 MSTest 项目根目录运行，或通过 `--root` 指定根目录。

```powershell
.\src\VsDteCli\bin\Debug\vsdte-cli.exe preflight `
  --solution .\SampleTests.sln `
  --test-dll .\SampleTests\bin\Debug\SampleTests.dll
```

启动一次调试，并在源码行暂停：

```powershell
.\src\VsDteCli\bin\Debug\vsdte-cli.exe start `
  --solution .\SampleTests.sln `
  --test-dll .\SampleTests\bin\Debug\SampleTests.dll `
  --test Test0001_Sample `
  --break-file .\SampleTests\SampleTest.cs `
  --break-text "page.Save();" `
  --after-stop keep-paused
```

如果省略 `--solution`，CLI 会使用 `--root` 或当前目录下唯一的 `.sln`
文件。`start` 命令必须提供 `--test-dll`。

## 命令

```text
preflight
list-instances
scene
start
step-over | step-into | step-out
continue
break-all
stop
set-next
breakpoint-list
breakpoint-add
breakpoint-remove
cleanup
```

运行 `vsdte-cli help` 查看当前完整参数列表。

## 复用 Visual Studio

当同一个 solution 已经在 Visual Studio 中打开时，`start` 会优先复用现有
Visual Studio 实例，避免迭代调试时反复打开多个 Visual Studio 窗口。

如果确实需要启动新的 Visual Studio 进程，可以使用 `--new-vs true`。

## 适合 AI Agent 的调试接口

`vs-dte-cli` 适合和 AI 编码助手、自动化 Agent 配合使用。

它把 Visual Studio 中的调试现场转换成稳定的 CLI 命令和 JSON 输出，让
Agent 可以读取暂停中的 MSTest 运行状态，而不需要依赖截图识别或人工描述
Visual Studio 当前画面。

常用能力包括：

- 采集当前文件、行号、栈帧、局部变量、参数、watch 表达式和源码上下文
- 通过 CLI 执行单步、继续、暂停、停止和设置下一条语句
- 动态添加、列出和移除断点
- 复用已有 Visual Studio 实例，避免反复打开多个窗口
- 启动新的聚焦调试时，只清理目标测试进程

这样 AI 助手可以基于真实运行时证据分析问题，把实际状态和预期行为进行
对比，再建议下一步调试动作，而不是根据截图或文字描述猜测。

## Live Debug Hook

`start` 启动 `vstest.console.exe` 时，会为测试进程设置以下环境变量，供测试
代码中的 first-break hook 使用：

- `VSDTECLI_LIVE_DEBUG`
- `VSDTECLI_FIRST_BREAK`
- `VSDTECLI_FIRST_BREAK_SCOPE`
- `VSDTECLI_ATTACH_WAIT_MS`
- `VSDTECLI_FIRST_BREAK_MODE`

CLI 本身不强制要求测试代码实现 hook。对于运行很快、可能在 Visual Studio
附加前就越过目标断点的测试，hook 可以让早期附加更可靠。

## License

MIT
