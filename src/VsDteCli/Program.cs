using System;
using System.Linq;

namespace VsDteCli
{
    internal static partial class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            try
            {
                if (args.Length == 0 || IsHelp(args[0]))
                {
                    PrintHelp();
                    return 0;
                }

                string command = args[0].Trim().ToLowerInvariant();
                CommandOptions options = CommandOptions.Parse(args.Skip(1).ToArray());

                switch (command)
                {
                    case "preflight":
                        return RunPreflight(options);
                    case "list-instances":
                    case "instances":
                        return RunListInstances(options);
                    case "scene":
                    case "current":
                        return RunScene(options);
                    case "start":
                        return RunStart(options);
                    case "step-over":
                        return RunDebuggerCommand(options, "step-over");
                    case "step-into":
                        return RunDebuggerCommand(options, "step-into");
                    case "step-out":
                        return RunDebuggerCommand(options, "step-out");
                    case "continue":
                    case "go":
                        return RunDebuggerCommand(options, "continue");
                    case "break-all":
                    case "break":
                        return RunDebuggerCommand(options, "break-all");
                    case "stop":
                        return RunDebuggerCommand(options, "stop");
                    case "set-next":
                    case "set-next-statement":
                        return RunSetNextStatement(options);
                    case "breakpoint-list":
                    case "breakpoints":
                    case "bp-list":
                        return RunBreakpointList(options);
                    case "breakpoint-add":
                    case "bp-add":
                        return RunBreakpointAdd(options);
                    case "breakpoint-remove":
                    case "breakpoint-delete":
                    case "breakpoint-clear":
                    case "bp-remove":
                    case "bp-delete":
                    case "bp-clear":
                        return RunBreakpointRemove(options);
                    case "cleanup":
                        return RunCleanup(options);
                    default:
                        Console.Error.WriteLine("Unknown command: " + command);
                        PrintHelp();
                        return 2;
                }
            }
            catch (Exception ex)
            {
                ErrorInfo error = ErrorInfo.FromException(ex);
                Console.Error.WriteLine(error.Message);
                return 1;
            }
        }
    }
}
