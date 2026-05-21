using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Management;

namespace VsDteCli
{
    internal static partial class Program
    {
        private static IEnumerable<Process> GetTargetTestProcesses(string marker)
        {
            foreach (Process process in Process.GetProcesses())
            {
                string name;
                try
                {
                    name = process.ProcessName;
                }
                catch
                {
                    continue;
                }

                if (!string.Equals(name, "vstest.console", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(name, "testhost", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string commandLine = GetCommandLine(process.Id);
                if (!string.IsNullOrWhiteSpace(commandLine) && commandLine.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    yield return process;
                }
            }
        }

        private static string GetCommandLine(int processId)
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + processId.ToString(CultureInfo.InvariantCulture)))
                using (ManagementObjectCollection results = searcher.Get())
                {
                    foreach (ManagementObject result in results)
                    {
                        return Convert.ToString(result["CommandLine"], CultureInfo.InvariantCulture);
                    }
                }
            }
            catch
            {
            }

            return null;
        }

        private static void StopProcessTree(int processId)
        {
            foreach (int childId in GetChildProcessIds(processId))
            {
                StopProcessTree(childId);
            }

            try
            {
                Process process = Process.GetProcessById(processId);
                SafeKill(process);
            }
            catch
            {
            }
        }

        private static List<int> GetChildProcessIds(int processId)
        {
            List<int> children = new List<int>();
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT ProcessId FROM Win32_Process WHERE ParentProcessId = " + processId.ToString(CultureInfo.InvariantCulture)))
                using (ManagementObjectCollection results = searcher.Get())
                {
                    foreach (ManagementObject result in results)
                    {
                        children.Add(Convert.ToInt32(result["ProcessId"], CultureInfo.InvariantCulture));
                    }
                }
            }
            catch
            {
            }

            return children;
        }

        private static ProcessInfo ToProcessInfo(Process process)
        {
            return new ProcessInfo
            {
                ProcessId = SafeInt(() => process.Id, 0),
                Name = SafeString(() => process.ProcessName),
                CommandLine = GetCommandLine(process.Id)
            };
        }

        private static void SafeKill(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch
            {
            }
        }

        private static void TryDeleteBreakpoint(dynamic breakpoint)
        {
            if (breakpoint == null)
            {
                return;
            }

            try
            {
                breakpoint.Delete();
            }
            catch
            {
            }
        }

        private static void TryDebuggerStop(dynamic dte)
        {
            try
            {
                dte.Debugger.Stop(false);
            }
            catch
            {
            }
        }
    }
}
