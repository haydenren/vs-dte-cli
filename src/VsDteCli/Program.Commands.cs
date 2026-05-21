using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace VsDteCli
{
    internal static partial class Program
    {
        private static int RunPreflight(CommandOptions options)
        {
            string root = ResolveRoot(options);
            string dteProgId = options.Get("dte-prog-id") ?? DefaultDteProgId;
            string devenvPath = options.Get("devenv") ?? DefaultDevenvPath;
            string vstestPath = options.Get("vstest") ?? DefaultVstestPath;
            string solutionPath = ResolvePath(root, options.Get("solution") ?? DefaultSolution);
            string testDllPath = ResolvePath(root, options.Get("test-dll") ?? DefaultTestDll);
            string startScriptPath = ResolvePath(root, @"tools\vs-dte\Invoke-MstestVsDte.ps1");
            string cliPath = typeof(Program).Assembly.Location;

            PreflightInfo info = new PreflightInfo
            {
                State = "READY",
                AutotestRoot = root,
                DteProgId = dteProgId,
                DevenvPath = devenvPath,
                VstestPath = vstestPath,
                SolutionPath = solutionPath,
                TestDllPath = testDllPath,
                StartScriptPath = startScriptPath,
                CliPath = cliPath,
                DteProgIdAvailable = Type.GetTypeFromProgID(dteProgId, false) != null,
                DevenvExists = File.Exists(devenvPath),
                VstestExists = File.Exists(vstestPath),
                SolutionExists = File.Exists(solutionPath),
                TestDllExists = File.Exists(testDllPath),
                StartScriptExists = File.Exists(startScriptPath)
            };

            if (!Directory.Exists(root))
            {
                info.State = "WORKSPACE_MISSING";
            }
            else if (!info.DteProgIdAvailable)
            {
                info.State = "DTE_UNAVAILABLE";
            }
            else if (!info.DevenvExists)
            {
                info.State = "DEVENV_MISSING";
            }
            else if (!info.VstestExists)
            {
                info.State = "VSTEST_MISSING";
            }
            else if (!info.SolutionExists)
            {
                info.State = "SOLUTION_MISSING";
            }
            else if (!info.TestDllExists)
            {
                info.State = "READY_TEST_DLL_MISSING";
            }

            if (options.GetBoolOrDefault("create-dte", false) && info.DteProgIdAvailable)
            {
                dynamic dte = null;
                try
                {
                    dte = CreateDte(dteProgId);
                    info.DteVersion = SafeString(() => dte.Version);
                    info.DteName = SafeString(() => dte.Name);
                }
                finally
                {
                    TryQuitDte(dte);
                }
            }

            WriteOutput(options, info);
            return info.State == "READY" || info.State == "READY_TEST_DLL_MISSING" ? 0 : 1;
        }

        private static int RunListInstances(CommandOptions options)
        {
            List<DteInstanceInfo> instances = ListDteInstances(options.Get("dte-prog-id") ?? DefaultDteProgId);
            WriteOutput(options, instances);
            return 0;
        }

        private static int RunScene(CommandOptions options)
        {
            DteSession session = ResolveDteSession(options);
            SceneInfo scene = CaptureScene(session, options);
            scene.Command = "scene";
            WriteOutput(options, scene);
            return 0;
        }

        private static int RunDebuggerCommand(CommandOptions options, string command)
        {
            DteSession session = ResolveDteSession(options);
            dynamic debugger = session.Dte.Debugger;
            DebuggerCommandResult result = new DebuggerCommandResult
            {
                Command = command,
                DteVersion = SafeString(() => session.Dte.Version),
                TargetPid = session.ProcessId,
                Attached = true,
                SkippedBreaks = new List<LocationInfo>(),
                Messages = new List<string>()
            };

            result.BeforeScene = CaptureScene(session, options);
            result.BeforeScene.Command = command;

            switch (command)
            {
                case "step-over":
                    InvokeDebuggerActionWithRetry(session.Dte, options, result, (Action)(() => debugger.StepOver(false)), "Debug.StepOver");
                    result.CompletionReason = WaitForBreakOrEnd(session, options);
                    result.WaitForBreak = true;
                    result.WaitedForBreak = IsUsableBreakCompletion(result.CompletionReason);
                    break;
                case "step-into":
                    InvokeDebuggerActionWithRetry(session.Dte, options, result, (Action)(() => debugger.StepInto(false)), "Debug.StepInto");
                    result.CompletionReason = WaitForBreakOrEnd(session, options);
                    result.WaitForBreak = true;
                    result.WaitedForBreak = IsUsableBreakCompletion(result.CompletionReason);
                    break;
                case "step-out":
                    InvokeDebuggerActionWithRetry(session.Dte, options, result, (Action)(() => debugger.StepOut(false)), "Debug.StepOut");
                    result.CompletionReason = WaitForBreakOrEnd(session, options);
                    result.WaitForBreak = true;
                    result.WaitedForBreak = IsUsableBreakCompletion(result.CompletionReason);
                    break;
                case "continue":
                    result.WaitForBreak = options.GetBoolOrDefault("wait", false);
                    if (!HasTargetTestProcess(options))
                    {
                        result.Messages.Add("Target test process is not running; continue command was not sent.");
                        result.CompletionReason = "target-ended";
                    }
                    else if (IsTrulyRunning(result.BeforeScene))
                    {
                        result.Messages.Add("DTE is already running without an available stack frame; continue command was not sent.");
                    }
                    else
                    {
                        InvokeDebuggerActionWithRetry(session.Dte, options, result, (Action)(() => debugger.Go(false)), "Debug.Start");
                    }

                    if (result.WaitForBreak)
                    {
                        result.CompletionReason = WaitForBreakOrEnd(session, options);
                        result.WaitedForBreak = IsUsableBreakCompletion(result.CompletionReason);
                    }
                    else if (string.IsNullOrWhiteSpace(result.CompletionReason))
                    {
                        result.CompletionReason = "command-sent";
                    }
                    break;
                case "break-all":
                    InvokeDebuggerActionWithRetry(session.Dte, options, result, (Action)(() => debugger.Break(true)), "Debug.BreakAll");
                    result.CompletionReason = WaitForBreakOrEnd(session, options);
                    result.WaitForBreak = true;
                    result.WaitedForBreak = IsUsableBreakCompletion(result.CompletionReason);
                    break;
                case "stop":
                    InvokeDebuggerActionWithRetry(session.Dte, options, result, (Action)(() => debugger.Stop(false)), "Debug.StopDebugging");
                    result.CompletionReason = "command-sent";
                    break;
            }

            try
            {
                result.AfterScene = CaptureScene(session, options);
                result.AfterScene.Command = command;
            }
            catch (Exception ex)
            {
                result.Messages.Add("After-scene capture failed: " + ex.Message);
            }
            WriteOutput(options, result);
            return 0;
        }

        private static void InvokeDebuggerActionWithRetry(object dteObject, CommandOptions options, DebuggerCommandResult result, Action action, string fallbackCommand)
        {
            dynamic dte = dteObject;
            int retryMs = options.GetIntOrDefault("command-retry-ms", 5000);
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(retryMs);
            Exception lastError = null;

            while (DateTime.UtcNow <= deadline)
            {
                try
                {
                    action();
                    return;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    if (!HasTargetTestProcess(options))
                    {
                        result.Messages.Add("Target test process ended while sending debugger command.");
                        result.CompletionReason = "target-ended";
                        return;
                    }

                    TryActivateDteWindow(dte);
                    if (!string.IsNullOrWhiteSpace(fallbackCommand) && TryExecuteDteCommand(dte, fallbackCommand))
                    {
                        result.Messages.Add("Debugger API command was rejected; fallback command used: " + fallbackCommand);
                        return;
                    }

                    Thread.Sleep(250);
                }
            }

            throw new InvalidOperationException(
                "Debugger command was not accepted by Visual Studio DTE. Last error: " +
                (lastError == null ? "<none>" : lastError.Message),
                lastError);
        }

        private static bool TryExecuteDteCommand(dynamic dte, string commandName)
        {
            try
            {
                dte.ExecuteCommand(commandName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string WaitForBreakOrEnd(DteSession session, CommandOptions options)
        {
            int timeoutMs = options.GetIntOrDefault("timeout-ms", 30000);
            int noFrameBreakMs = options.GetIntOrDefault("break-without-frame-ms", 5000);
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            DateTime? noFrameBreakStart = null;

            while (DateTime.UtcNow < deadline)
            {
                if (!HasTargetTestProcess(options))
                {
                    return "target-ended";
                }

                SceneInfo scene = CaptureScene(session, options);
                if (string.Equals(scene.State, "paused", StringComparison.OrdinalIgnoreCase) && scene.HasStackFrame)
                {
                    return "break";
                }

                if (scene.Mode == DesignMode)
                {
                    return "design";
                }

                if (string.Equals(scene.State, "paused", StringComparison.OrdinalIgnoreCase))
                {
                    if (!noFrameBreakStart.HasValue)
                    {
                        noFrameBreakStart = DateTime.UtcNow;
                    }

                    if ((DateTime.UtcNow - noFrameBreakStart.Value).TotalMilliseconds >= noFrameBreakMs)
                    {
                        return "break-without-frame";
                    }
                }
                else
                {
                    noFrameBreakStart = null;
                }

                Thread.Sleep(250);
            }

            throw new TimeoutException("Timed out waiting for debugger break or target process end.");
        }

        private static bool IsUsableBreakCompletion(string completionReason)
        {
            return string.Equals(completionReason, "break", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasTargetTestProcess(CommandOptions options)
        {
            string marker = options.Get("target-marker") ?? DefaultTargetMarker;
            return GetTargetTestProcesses(marker).Any();
        }

        private static bool IsTrulyRunning(SceneInfo scene)
        {
            return scene != null
                && scene.Mode == RunMode
                && !string.Equals(scene.State, "paused", StringComparison.OrdinalIgnoreCase)
                && !scene.HasStackFrame;
        }

        private static void ContinueDebugger(dynamic dte, CommandOptions options)
        {
            try
            {
                dte.Debugger.Go(false);
            }
            catch
            {
                TryActivateDteWindow(dte);
                dte.ExecuteCommand("Debug.Start");
            }
        }

        private static int RunCleanup(CommandOptions options)
        {
            string marker = options.Get("target-marker") ?? DefaultTargetMarker;
            bool includeDevenv = options.GetBoolOrDefault("devenv", false);
            CleanupInfo info = new CleanupInfo
            {
                TargetMarker = marker,
                Killed = new List<ProcessInfo>()
            };

            foreach (Process process in GetTargetTestProcesses(marker))
            {
                info.Killed.Add(ToProcessInfo(process));
                StopProcessTree(process.Id);
            }

            if (includeDevenv)
            {
                foreach (Process process in Process.GetProcessesByName("devenv"))
                {
                    info.Killed.Add(ToProcessInfo(process));
                    SafeKill(process);
                }
            }

            WriteOutput(options, info);
            return 0;
        }
    }
}
