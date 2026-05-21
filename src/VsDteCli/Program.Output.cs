using System;
using System.Globalization;
using System.Web.Script.Serialization;

namespace VsDteCli
{
    internal static partial class Program
    {
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer
        {
            MaxJsonLength = int.MaxValue
        };

        private static void WriteOutput(CommandOptions options, object value)
        {
            if (options.JsonOutput)
            {
                Console.WriteLine(Json.Serialize(value));
                return;
            }

            if (value is StartResult)
            {
                WriteStartResult((StartResult)value);
                return;
            }

            if (value is DebuggerCommandResult)
            {
                WriteDebuggerCommandResult((DebuggerCommandResult)value);
                return;
            }

            if (value is BreakpointCommandResult)
            {
                WriteBreakpointCommandResult((BreakpointCommandResult)value);
                return;
            }

            if (value is SceneInfo)
            {
                WriteScene((SceneInfo)value);
                return;
            }

            Console.WriteLine(Json.Serialize(value));
        }

        private static void WriteScene(SceneInfo scene)
        {
            WriteSceneBlock(null, scene, string.Empty);
        }

        private static void WriteStartResult(StartResult result)
        {
            Console.WriteLine("Start:");
            Console.WriteLine("  Test: " + result.Test);
            Console.WriteLine("  DTE: " + result.DteVersion + " VS PID=" + result.DevenvPid.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("  VS reused: " + (result.VisualStudioReused ? "yes" : "no"));
            Console.WriteLine("  Target PID: " + result.TargetPid.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("  Attached: " + (result.Attached ? "yes" : "no"));
            Console.WriteLine("  Breakpoint: " + result.BreakpointFile + ":" + result.BreakpointLine.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("  Break scope: " + result.BreakScope);
            Console.WriteLine("  After stop: " + result.AfterStop);
            Console.WriteLine("  Skipped breaks: " + result.SkippedBreaks.Count.ToString(CultureInfo.InvariantCulture));
            if (result.SkippedBreaks.Count > 0)
            {
                Console.WriteLine("  Skipped break locations:");
                foreach (LocationInfo location in result.SkippedBreaks)
                {
                    Console.WriteLine("    - " + FormatLocation(location));
                }
            }

            if (result.Scene != null)
            {
                WriteSceneBlock("Matched scene", result.Scene, "  ");
            }

            if (!string.IsNullOrWhiteSpace(result.CleanupStatus))
            {
                Console.WriteLine("  Cleanup: " + result.CleanupStatus);
            }
        }

        private static void WriteDebuggerCommandResult(DebuggerCommandResult result)
        {
            Console.WriteLine("Command:");
            Console.WriteLine("  Name: " + result.Command);
            Console.WriteLine("  DTE: " + result.DteVersion + " PID=" + result.TargetPid);
            Console.WriteLine("  Attached: " + (result.Attached ? "yes" : "no"));
            Console.WriteLine("  Wait for break: " + (result.WaitForBreak ? "yes" : "no"));
            Console.WriteLine("  Waited for break: " + (result.WaitedForBreak ? "yes" : "no"));
            if (!string.IsNullOrWhiteSpace(result.CompletionReason))
            {
                Console.WriteLine("  Completion: " + result.CompletionReason);
            }
            Console.WriteLine("  Skipped breaks: " + result.SkippedBreaks.Count.ToString(CultureInfo.InvariantCulture));
            if (result.Messages != null && result.Messages.Count > 0)
            {
                Console.WriteLine("  Messages:");
                foreach (string message in result.Messages)
                {
                    Console.WriteLine("    - " + message);
                }
            }
            if (result.BeforeScene != null)
            {
                WriteSceneBlock("Before scene", result.BeforeScene, "  ");
            }

            if (result.AfterScene != null)
            {
                WriteSceneBlock("After scene", result.AfterScene, "  ");
            }

            if (result.SkippedBreaks.Count > 0)
            {
                Console.WriteLine("  Skipped break locations:");
                foreach (LocationInfo location in result.SkippedBreaks)
                {
                    Console.WriteLine("    - " + FormatLocation(location));
                }
            }
        }

        private static void WriteBreakpointCommandResult(BreakpointCommandResult result)
        {
            Console.WriteLine("Breakpoints:");
            Console.WriteLine("  Command: " + result.Command);
            Console.WriteLine("  DTE: " + result.DteMoniker + " PID=" + result.DevenvPid.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrWhiteSpace(result.BreakpointFile))
            {
                Console.WriteLine("  Target: " + result.BreakpointFile + ":" + result.BreakpointLine.ToString(CultureInfo.InvariantCulture));
            }

            if (result.AddedCount > 0)
            {
                Console.WriteLine("  Added: " + result.AddedCount.ToString(CultureInfo.InvariantCulture));
            }

            if (result.RemovedCount > 0 || string.Equals(result.Command, "breakpoint-remove", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("  Removed: " + result.RemovedCount.ToString(CultureInfo.InvariantCulture));
            }

            if (result.Breakpoints.Count > 0)
            {
                Console.WriteLine("  Items:");
                foreach (BreakpointInfo breakpoint in result.Breakpoints)
                {
                    Console.WriteLine("    - #" + breakpoint.Index.ToString(CultureInfo.InvariantCulture) + " " +
                        breakpoint.File + ":" + breakpoint.Line.ToString(CultureInfo.InvariantCulture) +
                        " enabled=" + breakpoint.Enabled.ToString(CultureInfo.InvariantCulture));
                }
            }

            if (result.Messages.Count > 0)
            {
                Console.WriteLine("  Messages:");
                foreach (string message in result.Messages)
                {
                    Console.WriteLine("    - " + message);
                }
            }
        }

        private static void WriteSceneBlock(string title, SceneInfo scene, string indent)
        {
            string prefix = indent ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(title))
            {
                Console.WriteLine(prefix + title + ":");
            }

            if (scene == null)
            {
                Console.WriteLine(prefix + "<none>");
                return;
            }

            Console.WriteLine(prefix + "State: " + scene.State + " (DTE mode: " + scene.ModeName + ")");
            Console.WriteLine(prefix + "DTE: " + scene.DteMoniker + " PID=" + scene.DevenvPid.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine(prefix + "Frame: " + (scene.HasStackFrame ? scene.FunctionName : "<unavailable>"));
            LocationInfo executionLocation = GetExecutionLocation(scene);
            LocationInfo caretLocation = GetCaretLocation(scene);
            Console.WriteLine(prefix + "Execution: " + FormatLocation(executionLocation));
            if (!LocationsMatch(executionLocation, caretLocation) && !string.IsNullOrWhiteSpace(caretLocation.File))
            {
                Console.WriteLine(prefix + "Caret: " + FormatLocation(caretLocation));
            }
            if (!string.IsNullOrWhiteSpace(scene.LocationSource))
            {
                Console.WriteLine(prefix + "Location source: " + scene.LocationSource);
            }

            if (scene.Arguments.Count > 0)
            {
                Console.WriteLine(prefix + "Arguments:");
                foreach (VariableInfo variable in scene.Arguments)
                {
                    Console.WriteLine(prefix + "  " + variable.Name + " = " + variable.Value + " [" + variable.Type + "]");
                }
            }

            if (scene.Locals.Count > 0)
            {
                Console.WriteLine(prefix + "Locals:");
                foreach (VariableInfo variable in scene.Locals)
                {
                    Console.WriteLine(prefix + "  " + variable.Name + " = " + variable.Value + " [" + variable.Type + "]");
                }
            }

            if (scene.SourceContext.Count > 0)
            {
                Console.WriteLine(prefix + "Source:");
                foreach (SourceLineInfo line in scene.SourceContext)
                {
                    Console.WriteLine(prefix + (line.IsCurrent ? "> " : "  ") + line.Line.ToString(CultureInfo.InvariantCulture).PadLeft(4) + ": " + line.Text);
                }
            }

            if (scene.Errors.Count > 0)
            {
                Console.WriteLine(prefix + "Errors:");
                foreach (string error in scene.Errors)
                {
                    Console.WriteLine(prefix + "  - " + error);
                }
            }
        }
    }
}
