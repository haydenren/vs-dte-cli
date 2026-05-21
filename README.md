# vs-dte-cli

`vs-dte-cli` is a small Windows command-line tool for controlling Visual Studio
through DTE while debugging .NET Framework MSTest runs.

It can start `vstest.console.exe`, open or reuse Visual Studio, attach the
debugger, stop at a requested source breakpoint, capture the current scene,
step, continue, set the next statement, and add or remove breakpoints.

## Requirements

- Windows
- Visual Studio with DTE automation support
- .NET Framework 4.8 developer tooling
- MSTest / `vstest.console.exe`

The default paths target Visual Studio 2026 Enterprise:

```text
VisualStudio.DTE.18.0
C:\Program Files\Microsoft Visual Studio\18\Enterprise\
```

Use `--dte-prog-id`, `--devenv`, and `--vstest` when your installation uses a
different version or edition.

## Build

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe' `
  .\src\VsDteCli\VsDteCli.csproj `
  /t:Build /p:Configuration=Debug /verbosity:minimal
```

The debug build creates:

```text
src\VsDteCli\bin\Debug\vsdte-cli.exe
```

## Quick Start

Run from the root of the MSTest project, or pass `--root`.

```powershell
.\src\VsDteCli\bin\Debug\vsdte-cli.exe preflight `
  --solution .\SampleTests.sln `
  --test-dll .\SampleTests\bin\Debug\SampleTests.dll
```

Start a debug run and pause at a source line:

```powershell
.\src\VsDteCli\bin\Debug\vsdte-cli.exe start `
  --solution .\SampleTests.sln `
  --test-dll .\SampleTests\bin\Debug\SampleTests.dll `
  --test Test0001_Sample `
  --break-file .\SampleTests\SampleTest.cs `
  --break-text "page.Save();" `
  --after-stop keep-paused
```

If `--solution` is omitted, the CLI uses the only `.sln` file directly under
`--root` or the current directory. `--test-dll` is required for `start`.

## Commands

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

Run `vsdte-cli help` for the current option list.

## Visual Studio Reuse

`start` reuses an existing Visual Studio instance when the same solution is
already open. This avoids creating multiple Visual Studio windows during
iterative debugging.

Use `--new-vs true` when a fresh Visual Studio process is required.

## Live Debug Hook

When `start` launches `vstest.console.exe`, it sets these environment variables
for test code that wants a first-break hook:

- `VSDTECLI_LIVE_DEBUG`
- `VSDTECLI_FIRST_BREAK`
- `VSDTECLI_FIRST_BREAK_SCOPE`
- `VSDTECLI_ATTACH_WAIT_MS`
- `VSDTECLI_FIRST_BREAK_MODE`

The CLI does not require a hook. A hook is useful when a test can run past the
requested breakpoint before Visual Studio has enough time to attach.

## License

MIT
