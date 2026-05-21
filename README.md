# vs-dte-cli

Visual Studio DTE automation CLI for .NET Framework MSTest debugging.

`vsdte-cli` can open or reuse Visual Studio, attach to a started
`vstest.console.exe` process, stop at a source breakpoint, capture the current
scene, step, continue, set next statement, and manage temporary breakpoints.

## Status

This repository is a public-extraction workspace. The code builds and the
internal product-specific defaults have been removed, but a license and final
public examples still need owner approval before publishing.

## Build

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe' `
  .\src\VsDteCli\VsDteCli.csproj `
  /t:Build /p:Configuration=Debug /verbosity:minimal
```

## Basic Usage

Run from the root of the MSTest project or pass `--root`.

```powershell
.\src\VsDteCli\bin\Debug\vsdte-cli.exe preflight `
  --solution .\SampleTests.sln `
  --test-dll .\SampleTests\bin\Debug\SampleTests.dll
```

Start a debug run:

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
`--root` / the current directory. `--test-dll` is required for `start`.

## Live Debug Hook

When `start` launches `vstest.console.exe`, it sets these environment variables
for test code that wants a first-break hook:

- `VSDTECLI_LIVE_DEBUG`
- `VSDTECLI_FIRST_BREAK`
- `VSDTECLI_FIRST_BREAK_SCOPE`
- `VSDTECLI_ATTACH_WAIT_MS`
- `VSDTECLI_FIRST_BREAK_MODE`

The CLI does not require a hook, but a hook can make early attach more reliable
for tests that run past the requested breakpoint too quickly.
