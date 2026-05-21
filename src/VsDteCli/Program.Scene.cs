using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace VsDteCli
{
    internal static partial class Program
    {
        private static SceneInfo CaptureScene(DteSession session, CommandOptions options)
        {
            dynamic dte = session.Dte;
            int context = options.GetIntOrDefault("context", 3);
            List<string> expressions = options.GetAll("expr").ToList();

            SceneInfo scene = new SceneInfo
            {
                CapturedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
                DteMoniker = session.Moniker,
                DevenvPid = session.ProcessId,
                Mode = SafeInt(() => dte.Debugger.CurrentMode, 0),
                Solution = SafeString(() => dte.Solution.FullName),
                Arguments = new List<VariableInfo>(),
                Locals = new List<VariableInfo>(),
                Watches = new List<WatchInfo>(),
                SourceContext = new List<SourceLineInfo>(),
                TargetProcesses = GetTargetTestProcesses(GetTargetMarker(options)).Select(ToProcessInfo).ToList(),
                Errors = new List<string>()
            };
            scene.ModeName = ModeName(scene.Mode);

            try
            {
                dynamic document = dte.ActiveDocument;
                if (document != null)
                {
                    scene.ActiveDocument = SafeString(() => document.FullName);
                    scene.Line = SafeInt(() => document.Selection.ActivePoint.Line, 0);
                    scene.Column = SafeInt(() => document.Selection.ActivePoint.LineCharOffset, 0);
                }
            }
            catch (Exception ex)
            {
                scene.Errors.Add("activeDocument: " + ex.Message);
            }

            try
            {
                dynamic frame = dte.Debugger.CurrentStackFrame;
                if (frame != null)
                {
                    scene.FunctionName = SafeString(() => frame.FunctionName);
                    scene.Module = SafeString(() => frame.Module);
                    scene.ReturnType = SafeString(() => frame.ReturnType);
                    scene.FrameFile = SafeString(() => frame.FileName);
                    scene.FrameLine = SafeInt(() => frame.LineNumber, 0);
                    scene.FrameColumn = SafeInt(() => frame.ColumnNumber, 0);
                    scene.Arguments = ReadExpressions(frame.Arguments);
                    scene.Locals = ReadExpressions(frame.Locals);
                    scene.HasStackFrame =
                        !string.IsNullOrWhiteSpace(scene.FunctionName) ||
                        !string.IsNullOrWhiteSpace(scene.FrameFile) ||
                        scene.FrameLine > 0 ||
                        scene.Arguments.Count > 0 ||
                        scene.Locals.Count > 0;
                }
            }
            catch (Exception ex)
            {
                scene.Errors.Add("stackFrame: " + ex.Message);
            }

            scene.State = ResolveLogicalState(scene);
            scene.LocationSource = ResolveLocationSource(scene);

            foreach (string expression in expressions)
            {
                scene.Watches.Add(EvaluateExpression(dte, expression, options.GetIntOrDefault("eval-timeout-ms", 3000)));
            }

            string sourcePath = GetSourceContextPath(scene);
            int sourceLine = GetSourceContextLine(scene);

            if (!string.IsNullOrWhiteSpace(sourcePath) && File.Exists(sourcePath) && context > 0 && sourceLine > 0)
            {
                scene.SourceContext = ReadSourceContext(sourcePath, sourceLine, context);
            }

            return scene;
        }

        private static string ResolveLogicalState(SceneInfo scene)
        {
            if (scene.HasStackFrame)
            {
                return "paused";
            }

            if (scene.Mode == BreakMode)
            {
                return "paused";
            }

            if (scene.Mode == RunMode)
            {
                return "running";
            }

            if (scene.Mode == DesignMode)
            {
                return "design";
            }

            return "unknown";
        }

        private static string ResolveLocationSource(SceneInfo scene)
        {
            if (scene.HasStackFrame && !string.IsNullOrWhiteSpace(scene.FrameFile) && scene.FrameLine > 0)
            {
                return "stack-frame";
            }

            if (scene.HasStackFrame && !string.IsNullOrWhiteSpace(scene.ActiveDocument) && scene.Line > 0)
            {
                return "stack-frame-caret";
            }

            if (!string.IsNullOrWhiteSpace(scene.ActiveDocument) && scene.Line > 0)
            {
                return "caret-only";
            }

            return "unknown";
        }

        private static string GetSourceContextPath(SceneInfo scene)
        {
            if (scene.HasStackFrame && !string.IsNullOrWhiteSpace(scene.FrameFile))
            {
                return scene.FrameFile;
            }

            if (scene.HasStackFrame && !string.IsNullOrWhiteSpace(scene.ActiveDocument))
            {
                return scene.ActiveDocument;
            }

            return scene.ActiveDocument;
        }

        private static int GetSourceContextLine(SceneInfo scene)
        {
            if (scene.HasStackFrame && scene.FrameLine > 0)
            {
                return scene.FrameLine;
            }

            if (scene.HasStackFrame && scene.Line > 0)
            {
                return scene.Line;
            }

            return scene.Line;
        }

        private static List<VariableInfo> ReadExpressions(dynamic expressions)
        {
            List<VariableInfo> variables = new List<VariableInfo>();
            if (expressions == null)
            {
                return variables;
            }

            foreach (dynamic expression in expressions)
            {
                VariableInfo info = new VariableInfo
                {
                    Name = SafeString(() => expression.Name),
                    Value = SafeString(() => expression.Value),
                    Type = SafeString(() => expression.Type),
                    IsValid = SafeBool(() => expression.IsValidValue, true)
                };

                if (!string.IsNullOrWhiteSpace(info.Name) || !string.IsNullOrWhiteSpace(info.Value))
                {
                    variables.Add(info);
                }
            }

            return variables;
        }

        private static WatchInfo EvaluateExpression(dynamic dte, string expression, int timeoutMs)
        {
            WatchInfo info = new WatchInfo { Expression = expression };
            try
            {
                dynamic result = dte.Debugger.GetExpression(expression, false, timeoutMs);
                info.Value = SafeString(() => result.Value);
                info.Type = SafeString(() => result.Type);
                info.IsValid = SafeBool(() => result.IsValidValue, false);
            }
            catch (Exception ex)
            {
                info.Error = ex.Message;
            }

            return info;
        }

        private static bool SceneMatches(SceneInfo scene, string file, int line)
        {
            LocationInfo executionLocation = GetExecutionLocation(scene);
            if (!string.IsNullOrWhiteSpace(executionLocation.File))
            {
                return string.Equals(Path.GetFullPath(executionLocation.File), Path.GetFullPath(file), StringComparison.OrdinalIgnoreCase)
                    && executionLocation.Line == line;
            }

            LocationInfo caretLocation = GetCaretLocation(scene);
            return string.Equals(scene.State, "paused", StringComparison.OrdinalIgnoreCase)
                && !scene.HasStackFrame
                && LocationsMatch(caretLocation, new LocationInfo { File = file, Line = line });
        }

        private static LocationInfo GetExecutionLocation(SceneInfo scene)
        {
            if (scene == null)
            {
                return new LocationInfo();
            }

            return new LocationInfo
            {
                File = scene.HasStackFrame ? (!string.IsNullOrWhiteSpace(scene.FrameFile) ? scene.FrameFile : scene.ActiveDocument) : null,
                Line = scene.HasStackFrame ? (scene.FrameLine > 0 ? scene.FrameLine : scene.Line) : 0,
                Function = scene.FunctionName
            };
        }

        private static LocationInfo GetCaretLocation(SceneInfo scene)
        {
            if (scene == null)
            {
                return new LocationInfo();
            }

            return new LocationInfo
            {
                File = scene.ActiveDocument,
                Line = scene.Line,
                Function = scene.FunctionName
            };
        }

        private static string FormatLocation(LocationInfo location)
        {
            if (location == null || string.IsNullOrWhiteSpace(location.File))
            {
                return "<unknown>";
            }

            string suffix = location.Line > 0 ? ":" + location.Line.ToString(CultureInfo.InvariantCulture) : string.Empty;
            if (string.IsNullOrWhiteSpace(location.Function))
            {
                return location.File + suffix;
            }

            return location.File + suffix + " " + location.Function;
        }

        private static bool LocationsMatch(LocationInfo left, LocationInfo right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(left.File) || string.IsNullOrWhiteSpace(right.File))
            {
                return false;
            }

            return string.Equals(Path.GetFullPath(left.File), Path.GetFullPath(right.File), StringComparison.OrdinalIgnoreCase)
                && left.Line == right.Line;
        }

        private static int ResolveBreakLine(string breakFile, CommandOptions options)
        {
            int line = options.GetIntOrDefault("break-line", 0);
            if (line > 0)
            {
                return line;
            }

            string text = options.Get("break-text");
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("Pass --break-line or --break-text.");
            }

            string[] lines = File.ReadAllLines(breakFile);
            string trimmed = text.Trim();
            for (int i = 0; i < lines.Length; i++)
            {
                if (string.Equals(lines[i].Trim(), trimmed, StringComparison.Ordinal))
                {
                    return i + 1;
                }
            }

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].IndexOf(text, StringComparison.Ordinal) >= 0)
                {
                    return i + 1;
                }
            }

            throw new InvalidOperationException("Breakpoint text not found: " + text);
        }

        private static List<SourceLineInfo> ReadSourceContext(string path, int line, int context)
        {
            string[] lines = File.ReadAllLines(path);
            int start = Math.Max(1, line - context);
            int end = Math.Min(lines.Length, line + context);
            List<SourceLineInfo> result = new List<SourceLineInfo>();
            for (int current = start; current <= end; current++)
            {
                result.Add(new SourceLineInfo
                {
                    Line = current,
                    Text = lines[current - 1],
                    IsCurrent = current == line
                });
            }

            return result;
        }
    }
}
