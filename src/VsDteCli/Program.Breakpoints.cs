using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace VsDteCli
{
    internal static partial class Program
    {
        private static int RunBreakpointList(CommandOptions options)
        {
            DteSession session = ResolveDteSession(options);
            BreakpointCommandResult result = CreateBreakpointResult("breakpoint-list", session);
            result.Breakpoints = ReadBreakpoints(session.Dte);
            WriteOutput(options, result);
            return 0;
        }

        private static int RunBreakpointAdd(CommandOptions options)
        {
            DteSession session = ResolveDteSession(options);
            string file = ResolveBreakpointFile(options);
            int line = ResolveFlexibleBreakLine(file, options);
            BreakpointCommandResult result = CreateBreakpointResult("breakpoint-add", session);
            result.BreakpointFile = file;
            result.BreakpointLine = line;

            dynamic breakpoint = session.Dte.Debugger.Breakpoints.Add("", file, line);
            result.AddedCount = 1;
            result.Breakpoints.Add(ToBreakpointInfo(breakpoint, 0));
            WriteOutput(options, result);
            return 0;
        }

        private static int RunBreakpointRemove(CommandOptions options)
        {
            DteSession session = ResolveDteSession(options);
            string file = ResolveBreakpointFile(options);
            int line = ResolveFlexibleBreakLine(file, options);
            BreakpointCommandResult result = CreateBreakpointResult("breakpoint-remove", session);
            result.BreakpointFile = file;
            result.BreakpointLine = line;

            dynamic breakpoints = session.Dte.Debugger.Breakpoints;
            int count = SafeInt(() => breakpoints.Count, 0);
            for (int index = count; index >= 1; index--)
            {
                dynamic breakpoint = null;
                try
                {
                    breakpoint = breakpoints.Item(index);
                }
                catch (Exception ex)
                {
                    result.Messages.Add("breakpoint[" + index.ToString(CultureInfo.InvariantCulture) + "] read failed: " + ex.Message);
                    continue;
                }

                BreakpointInfo info = ToBreakpointInfo(breakpoint, index);
                if (!BreakpointMatches(info, file, line))
                {
                    continue;
                }

                result.Breakpoints.Add(info);
                TryDeleteBreakpoint(breakpoint);
                result.RemovedCount++;
            }

            if (result.RemovedCount == 0)
            {
                result.Messages.Add("No matching breakpoint found.");
            }

            WriteOutput(options, result);
            return 0;
        }

        private static BreakpointCommandResult CreateBreakpointResult(string command, DteSession session)
        {
            return new BreakpointCommandResult
            {
                Command = command,
                DevenvPid = session.ProcessId,
                DteMoniker = session.Moniker,
                Breakpoints = new List<BreakpointInfo>(),
                Messages = new List<string>()
            };
        }

        private static string ResolveBreakpointFile(CommandOptions options)
        {
            string root = ResolveRoot(options);
            string file = options.Get("file") ?? options.Get("break-file");
            if (string.IsNullOrWhiteSpace(file))
            {
                throw new InvalidOperationException("Missing required option --file.");
            }

            string resolved = ResolvePath(root, file);
            if (!File.Exists(resolved))
            {
                throw new InvalidOperationException("Breakpoint file not found: " + resolved);
            }

            return resolved;
        }

        private static int ResolveFlexibleBreakLine(string breakFile, CommandOptions options)
        {
            string line = options.Get("line");
            if (!string.IsNullOrWhiteSpace(line))
            {
                options = options.With("break-line", line);
            }

            string text = options.Get("text");
            if (!string.IsNullOrWhiteSpace(text))
            {
                options = options.With("break-text", text);
            }

            return ResolveBreakLine(breakFile, options);
        }

        private static List<BreakpointInfo> ReadBreakpoints(dynamic dte)
        {
            List<BreakpointInfo> result = new List<BreakpointInfo>();
            dynamic breakpoints = dte.Debugger.Breakpoints;
            int count = SafeInt(() => breakpoints.Count, 0);
            for (int index = 1; index <= count; index++)
            {
                try
                {
                    result.Add(ToBreakpointInfo(breakpoints.Item(index), index));
                }
                catch
                {
                }
            }

            return result;
        }

        private static BreakpointInfo ToBreakpointInfo(dynamic breakpoint, int index)
        {
            return new BreakpointInfo
            {
                Index = index,
                File = SafeString(() => breakpoint.File),
                Line = SafeInt(() => breakpoint.FileLine, 0),
                Function = SafeString(() => breakpoint.FunctionName),
                Name = SafeString(() => breakpoint.Name),
                Enabled = SafeBool(() => breakpoint.Enabled, false),
                Condition = SafeString(() => breakpoint.Condition)
            };
        }

        private static bool BreakpointMatches(BreakpointInfo breakpoint, string file, int line)
        {
            if (breakpoint == null || string.IsNullOrWhiteSpace(breakpoint.File))
            {
                return false;
            }

            return string.Equals(Path.GetFullPath(breakpoint.File), Path.GetFullPath(file), StringComparison.OrdinalIgnoreCase)
                && breakpoint.Line == line;
        }
    }
}
