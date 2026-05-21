using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;

namespace VsDteCli
{
    internal static partial class Program
    {
        private static DteSession ResolveDteSession(CommandOptions options)
        {
            string dteProgId = options.Get("dte-prog-id") ?? DefaultDteProgId;
            int pid = options.GetIntOrDefault("pid", 0);
            List<DteInstanceInfo> instances = ListDteInstances(dteProgId);

            DteInstanceInfo selected = null;
            if (pid > 0)
            {
                selected = instances.FirstOrDefault(instance => instance.ProcessId == pid);
                if (selected == null)
                {
                    throw new InvalidOperationException("No Visual Studio DTE instance found for PID " + pid.ToString(CultureInfo.InvariantCulture));
                }
            }
            else if (instances.Count == 1)
            {
                selected = instances[0];
            }
            else if (instances.Count > 1)
            {
                selected = instances.OrderByDescending(instance => instance.ProcessId).First();
            }

            if (selected != null && selected.DteObject != null)
            {
                return new DteSession
                {
                    Dte = selected.DteObject,
                    Moniker = selected.Moniker,
                    ProcessId = selected.ProcessId
                };
            }

            try
            {
                return new DteSession
                {
                    Dte = Marshal.GetActiveObject(dteProgId),
                    Moniker = dteProgId
                };
            }
            catch (COMException ex)
            {
                throw new InvalidOperationException("No active Visual Studio DTE instance found for " + dteProgId + ": " + ex.Message, ex);
            }
        }

        private static List<DteInstanceInfo> ListDteInstances(string dteProgId)
        {
            List<DteInstanceInfo> result = new List<DteInstanceInfo>();
            IRunningObjectTable rot;
            IBindCtx bindCtx;
            int rotResult = GetRunningObjectTable(0, out rot);
            int bindResult = CreateBindCtx(0, out bindCtx);
            if (rotResult != 0 || bindResult != 0 || rot == null || bindCtx == null)
            {
                return result;
            }

            IEnumMoniker enumMoniker;
            rot.EnumRunning(out enumMoniker);
            IMoniker[] monikers = new IMoniker[1];
            while (enumMoniker.Next(1, monikers, IntPtr.Zero) == 0)
            {
                string name;
                try
                {
                    monikers[0].GetDisplayName(bindCtx, null, out name);
                }
                catch
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(name) || name.IndexOf(dteProgId, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                object dteObject;
                try
                {
                    rot.GetObject(monikers[0], out dteObject);
                }
                catch
                {
                    continue;
                }

                DteInstanceInfo info = new DteInstanceInfo
                {
                    Moniker = name,
                    ProcessId = ParsePidFromMoniker(name),
                    Version = SafeString(() => ((dynamic)dteObject).Version),
                    Name = SafeString(() => ((dynamic)dteObject).Name),
                    Mode = SafeInt(() => ((dynamic)dteObject).Debugger.CurrentMode, 0),
                    Solution = SafeString(() => ((dynamic)dteObject).Solution.FullName),
                    DteObject = dteObject
                };
                info.ModeName = ModeName(info.Mode);
                result.Add(info);
            }

            return result.OrderBy(item => item.ProcessId).ToList();
        }

        private static DteSession ResolveStartDteSession(string dteProgId, string solutionPath, bool reuseVs, bool newVs, HashSet<int> existingDevenvPids)
        {
            if (reuseVs && !newVs)
            {
                DteInstanceInfo reusable = ListDteInstances(dteProgId)
                    .Where(instance => IsSamePath(instance.Solution, solutionPath))
                    .OrderByDescending(instance => instance.ProcessId)
                    .FirstOrDefault();

                if (reusable != null && reusable.DteObject != null)
                {
                    return new DteSession
                    {
                        Dte = reusable.DteObject,
                        Moniker = reusable.Moniker,
                        ProcessId = reusable.ProcessId,
                        Reused = true
                    };
                }
            }

            dynamic dte = CreateDte(dteProgId);
            return new DteSession
            {
                Dte = dte,
                Moniker = dteProgId,
                ProcessId = FindNewDevenvProcessId(existingDevenvPids),
                Reused = false
            };
        }

        private static int FindNewDevenvProcessId(HashSet<int> existingDevenvPids)
        {
            return Process.GetProcessesByName("devenv")
                .Where(process => !existingDevenvPids.Contains(process.Id))
                .Select(process => process.Id)
                .OrderByDescending(processId => processId)
                .FirstOrDefault();
        }

        private static bool IsSamePath(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            {
                return false;
            }

            try
            {
                return string.Equals(
                    Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static int ParsePidFromMoniker(string moniker)
        {
            int colonIndex = moniker.LastIndexOf(':');
            if (colonIndex < 0 || colonIndex == moniker.Length - 1)
            {
                return 0;
            }

            int pid;
            return int.TryParse(moniker.Substring(colonIndex + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out pid) ? pid : 0;
        }

        private static dynamic CreateDte(string dteProgId)
        {
            Type type = Type.GetTypeFromProgID(dteProgId, true);
            return Activator.CreateInstance(type);
        }

        private static void AttachDteToProcess(dynamic dte, int processId, int timeoutMs)
        {
            WaitUntil(() =>
            {
                foreach (dynamic process in dte.Debugger.LocalProcesses)
                {
                    if ((int)process.ProcessID == processId)
                    {
                        process.Attach();
                        return true;
                    }
                }

                return false;
            }, timeoutMs, "Target process was not visible to Visual Studio DTE.");
        }

        private static void WaitForMode(dynamic dte, int expectedMode, int timeoutMs)
        {
            WaitUntil(() => SafeInt(() => dte.Debugger.CurrentMode, 0) == expectedMode, timeoutMs, "Timed out waiting for DTE mode " + expectedMode.ToString(CultureInfo.InvariantCulture));
        }

        private static void WaitUntil(Func<bool> predicate, int timeoutMs, string errorMessage)
        {
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            Exception lastError = null;
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    if (predicate())
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex;
                }

                Thread.Sleep(250);
            }

            if (lastError != null)
            {
                throw new TimeoutException(errorMessage + " Last error: " + lastError.Message, lastError);
            }

            throw new TimeoutException(errorMessage);
        }
    }
}
