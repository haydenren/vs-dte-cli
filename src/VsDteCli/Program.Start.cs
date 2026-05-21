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
        private static int RunStart(CommandOptions options)
        {
            string root = ResolveRoot(options);
            string dteProgId = options.Get("dte-prog-id") ?? DefaultDteProgId;
            string solutionPath = ResolvePath(root, options.Get("solution") ?? DefaultSolution);
            string vstestPath = options.Get("vstest") ?? DefaultVstestPath;
            string testDll = options.Get("test-dll") ?? DefaultTestDll;
            string testDllFullPath = ResolvePath(root, testDll);
            string testName = Require(options, "test");
            string breakFile = ResolvePath(root, Require(options, "break-file"));
            int breakLine = ResolveBreakLine(breakFile, options);
            string breakScope = options.Get("break-scope") ?? testName;
            string afterStop = (options.Get("after-stop") ?? "terminate").Trim().ToLowerInvariant();
            int timeoutMs = options.GetIntOrDefault("timeout-ms", 180000);
            bool visible = options.GetBoolOrDefault("visible", true);
            bool forceNewVs = options.GetBoolOrDefault("new-vs", false);
            bool reuseVs = options.GetBoolOrDefault("reuse-vs", !forceNewVs);
            string marker = Path.GetFileName(testDllFullPath);

            if (!File.Exists(solutionPath))
            {
                throw new InvalidOperationException("Solution not found: " + solutionPath);
            }

            if (!File.Exists(vstestPath))
            {
                throw new InvalidOperationException("vstest.console.exe not found: " + vstestPath);
            }

            if (!File.Exists(testDllFullPath))
            {
                throw new InvalidOperationException("Test DLL not found: " + testDllFullPath);
            }

            if (!options.GetBoolOrDefault("no-clean-start", false))
            {
                foreach (Process process in GetTargetTestProcesses(marker))
                {
                    StopProcessTree(process.Id);
                }
            }

            HashSet<int> existingDevenvPids = new HashSet<int>(Process.GetProcessesByName("devenv").Select(p => p.Id));
            DteSession startSession = null;
            dynamic dte = null;
            dynamic temporaryBreakpoint = null;
            Process targetProcess = null;
            bool terminateTarget = afterStop == "terminate";
            StartResult result = new StartResult
            {
                Test = testName,
                BreakpointFile = breakFile,
                BreakpointLine = breakLine,
                BreakScope = breakScope,
                AfterStop = afterStop,
                SkippedBreaks = new List<LocationInfo>()
            };

            try
            {
                startSession = ResolveStartDteSession(dteProgId, solutionPath, reuseVs, forceNewVs, existingDevenvPids);
                dte = startSession.Dte;
                bool keepVs = options.GetBoolOrDefault("keep-vs", afterStop == "keep-paused" || startSession.Reused);
                dte.UserControl = keepVs;
                if (visible)
                {
                    dte.MainWindow.Visible = true;
                    TryActivateDteWindow(dte);
                }

                WaitUntil(() => SafeInt(() => dte.Debugger.CurrentMode, 0) > 0, 60000, "Visual Studio DTE did not become ready.");
                if (startSession.ProcessId == 0)
                {
                    startSession.ProcessId = FindNewDevenvProcessId(existingDevenvPids);
                }

                result.DteVersion = SafeString(() => dte.Version);
                result.DteMoniker = startSession.Moniker;
                result.DevenvPid = startSession.ProcessId;
                result.VisualStudioReused = startSession.Reused;

                if (!IsSamePath(SafeString(() => dte.Solution.FullName), solutionPath))
                {
                    dte.Solution.Open(solutionPath);
                    WaitUntil(() => SafeBool(() => dte.Solution.IsOpen, false), 120000, "Solution did not open.");
                }

                if (!options.GetBoolOrDefault("no-clean-start", false))
                {
                    if (SafeInt(() => dte.Debugger.CurrentMode, 0) != DesignMode)
                    {
                        TryDebuggerStop(dte);
                        WaitForMode(dte, DesignMode, 30000);
                    }
                }
                else if (SafeInt(() => dte.Debugger.CurrentMode, 0) != DesignMode)
                {
                    throw new InvalidOperationException("Visual Studio is already debugging. Run without --no-clean-start, continue the existing session, or use --new-vs true.");
                }

                if (visible)
                {
                    TryActivateDteWindow(dte);
                }

                temporaryBreakpoint = dte.Debugger.Breakpoints.Add("", breakFile, breakLine);

                targetProcess = StartVstest(root, vstestPath, testDll, testName, breakScope, options.GetIntOrDefault("attach-wait-ms", 180000), options);
                result.TargetPid = targetProcess.Id;

                AttachDteToProcess(dte, targetProcess.Id, 60000);
                result.Attached = true;

                bool matched = false;
                for (int attempt = 0; attempt < options.GetIntOrDefault("max-breaks", 6); attempt++)
                {
                    WaitForMode(dte, BreakMode, timeoutMs);
                    SceneInfo scene = CaptureScene(new DteSession { Dte = dte }, options);
                    if (SceneMatches(scene, breakFile, breakLine))
                    {
                        scene.Command = "start";
                        result.Scene = scene;
                        matched = true;
                        break;
                    }

                    LocationInfo executionLocation = GetExecutionLocation(scene);
                    result.SkippedBreaks.Add(new LocationInfo
                    {
                        File = executionLocation.File,
                        Line = executionLocation.Line,
                        Function = scene.FunctionName
                    });
                    ContinueDebugger(dte, options);
                }

                if (!matched)
                {
                    throw new InvalidOperationException("Visual Studio did not stop at requested breakpoint.");
                }

                if (afterStop == "continue")
                {
                    dte.Debugger.Go(false);
                    terminateTarget = false;
                    result.CleanupStatus = "target left running by request";
                }
                else if (afterStop == "keep-paused")
                {
                    terminateTarget = false;
                    result.CleanupStatus = "target left paused by request";
                }
                else if (afterStop == "terminate")
                {
                    TryDebuggerStop(dte);
                    result.CleanupStatus = "debugger stop requested; process tree cleanup pending";
                }
                else
                {
                    throw new InvalidOperationException("Unsupported --after-stop value: " + afterStop);
                }
            }
            finally
            {
                TryDeleteBreakpoint(temporaryBreakpoint);

                if (terminateTarget && targetProcess != null && !targetProcess.HasExited)
                {
                    StopProcessTree(targetProcess.Id);
                    result.CleanupStatus = "target process tree stopped";
                }
                else if (terminateTarget && targetProcess != null && targetProcess.HasExited && string.IsNullOrWhiteSpace(result.CleanupStatus))
                {
                    result.CleanupStatus = "target already exited";
                }

                if (!options.GetBoolOrDefault("keep-vs", afterStop == "keep-paused" || (startSession != null && startSession.Reused)) && startSession != null && !startSession.Reused)
                {
                    TryQuitDte(dte);
                    Thread.Sleep(1000);
                    foreach (Process process in Process.GetProcessesByName("devenv").Where(p => !existingDevenvPids.Contains(p.Id)))
                    {
                        SafeKill(process);
                    }
                }
            }

            WriteOutput(options, result);
            return 0;
        }

        private static Process StartVstest(string root, string vstestPath, string testDll, string testName, string breakScope, int attachWaitMs, CommandOptions options)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = vstestPath,
                WorkingDirectory = root,
                Arguments = "\"" + testDll + "\" /Tests:" + testName,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.EnvironmentVariables["AUTOTEST_DEBUGMCP_LIVE_DEBUG"] = "1";
            startInfo.EnvironmentVariables["AUTOTEST_LIVE_DEBUG_FIRST_BREAK"] = "1";
            startInfo.EnvironmentVariables["AUTOTEST_LIVE_DEBUG_FIRST_BREAK_SCOPE"] = breakScope;
            startInfo.EnvironmentVariables["AUTOTEST_LIVE_DEBUG_ATTACH_WAIT_MS"] = attachWaitMs.ToString(CultureInfo.InvariantCulture);
            startInfo.EnvironmentVariables["AUTOTEST_LIVE_DEBUG_FIRST_BREAK_MODE"] =
                options.Get("first-break-mode") ?? "Break";

            Process process = Process.Start(startInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start vstest.console.exe.");
            }

            return process;
        }
    }
}
