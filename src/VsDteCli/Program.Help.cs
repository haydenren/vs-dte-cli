using System;

namespace VsDteCli
{
    internal static partial class Program
    {
        private static bool IsHelp(string value)
        {
            string normalized = value.Trim().ToLowerInvariant();
            return normalized == "help" || normalized == "--help" || normalized == "-h" || normalized == "/?";
        }

        private static void PrintHelp()
        {
            Console.WriteLine("vsdte-cli");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  preflight [--root PATH] [--json] [--create-dte true|false]");
            Console.WriteLine("  list-instances [--json]");
            Console.WriteLine("  scene [--pid PID] [--context N] [--expr EXPR] [--json]");
            Console.WriteLine("  start --test NAME --break-file FILE (--break-line N|--break-text TEXT) [--after-stop terminate|keep-paused|continue] [--first-break-mode Break|WaitOnly] [--reuse-vs true|false] [--new-vs true|false]");
            Console.WriteLine("  step-over|step-into|step-out [--pid PID] [--context N] [--json] [--break-without-frame-ms N]");
            Console.WriteLine("  continue [--pid PID] [--wait true|false] [--break-without-frame-ms N]");
            Console.WriteLine("  break-all [--pid PID]");
            Console.WriteLine("  stop [--pid PID]");
            Console.WriteLine("  set-next --pid PID [--file FILE] --line N [--context N] [--json]");
            Console.WriteLine("  breakpoint-list [--pid PID] [--json]");
            Console.WriteLine("  breakpoint-add [--pid PID] --file FILE (--line N|--text TEXT) [--json]");
            Console.WriteLine("  breakpoint-remove [--pid PID] --file FILE (--line N|--text TEXT) [--json]");
            Console.WriteLine("  cleanup [--target-marker SchoolSiteScript.dll] [--devenv true|false]");
        }
    }
}
