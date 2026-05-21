using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace VsDteCli
{
    internal static partial class Program
    {
        private sealed class DteSession
        {
            public dynamic Dte { get; set; }
            public string Moniker { get; set; }
            public int ProcessId { get; set; }
            public bool Reused { get; set; }
        }

        private sealed class DteInstanceInfo
        {
            public string Moniker { get; set; }
            public int ProcessId { get; set; }
            public string Version { get; set; }
            public string Name { get; set; }
            public int Mode { get; set; }
            public string ModeName { get; set; }
            public string Solution { get; set; }
            [ScriptIgnore]
            public object DteObject { get; set; }
        }

        private sealed class PreflightInfo
        {
            public string State { get; set; }
            public string AutotestRoot { get; set; }
            public string DteProgId { get; set; }
            public string DteVersion { get; set; }
            public string DteName { get; set; }
            public string DevenvPath { get; set; }
            public string VstestPath { get; set; }
            public string SolutionPath { get; set; }
            public string TestDllPath { get; set; }
            public string StartScriptPath { get; set; }
            public string CliPath { get; set; }
            public bool DteProgIdAvailable { get; set; }
            public bool DevenvExists { get; set; }
            public bool VstestExists { get; set; }
            public bool SolutionExists { get; set; }
            public bool TestDllExists { get; set; }
            public bool StartScriptExists { get; set; }
        }

        private sealed class SceneInfo
        {
            public string CapturedAt { get; set; }
            public string Command { get; set; }
            public string State { get; set; }
            public int Mode { get; set; }
            public string ModeName { get; set; }
            public bool HasStackFrame { get; set; }
            public string LocationSource { get; set; }
            public string DteMoniker { get; set; }
            public int DevenvPid { get; set; }
            public string Solution { get; set; }
            public string ActiveDocument { get; set; }
            public int Line { get; set; }
            public int Column { get; set; }
            public string FrameFile { get; set; }
            public int FrameLine { get; set; }
            public int FrameColumn { get; set; }
            public string FunctionName { get; set; }
            public string Module { get; set; }
            public string ReturnType { get; set; }
            public List<VariableInfo> Arguments { get; set; }
            public List<VariableInfo> Locals { get; set; }
            public List<WatchInfo> Watches { get; set; }
            public List<SourceLineInfo> SourceContext { get; set; }
            public List<ProcessInfo> TargetProcesses { get; set; }
            public List<string> Errors { get; set; }
        }

        private sealed class VariableInfo
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public string Type { get; set; }
            public bool IsValid { get; set; }
        }

        private sealed class WatchInfo
        {
            public string Expression { get; set; }
            public string Value { get; set; }
            public string Type { get; set; }
            public bool IsValid { get; set; }
            public string Error { get; set; }
        }

        private sealed class SourceLineInfo
        {
            public int Line { get; set; }
            public string Text { get; set; }
            public bool IsCurrent { get; set; }
        }

        private sealed class ProcessInfo
        {
            public int ProcessId { get; set; }
            public string Name { get; set; }
            public string CommandLine { get; set; }
        }

        private sealed class CleanupInfo
        {
            public string TargetMarker { get; set; }
            public List<ProcessInfo> Killed { get; set; }
        }

        private sealed class StartResult
        {
            public string Test { get; set; }
            public string DteVersion { get; set; }
            public string DteMoniker { get; set; }
            public int DevenvPid { get; set; }
            public bool VisualStudioReused { get; set; }
            public int TargetPid { get; set; }
            public bool Attached { get; set; }
            public string BreakpointFile { get; set; }
            public int BreakpointLine { get; set; }
            public string BreakScope { get; set; }
            public string AfterStop { get; set; }
            public string CleanupStatus { get; set; }
            public SceneInfo Scene { get; set; }
            public List<LocationInfo> SkippedBreaks { get; set; }
        }

        private sealed class DebuggerCommandResult
        {
            public string Command { get; set; }
            public string DteVersion { get; set; }
            public int TargetPid { get; set; }
            public bool Attached { get; set; }
            public bool WaitForBreak { get; set; }
            public bool WaitedForBreak { get; set; }
            public string CompletionReason { get; set; }
            public SceneInfo BeforeScene { get; set; }
            public SceneInfo AfterScene { get; set; }
            public List<LocationInfo> SkippedBreaks { get; set; }
            public List<string> Messages { get; set; }
        }

        private sealed class LocationInfo
        {
            public string File { get; set; }
            public int Line { get; set; }
            public string Function { get; set; }
        }

        private sealed class BreakpointInfo
        {
            public int Index { get; set; }
            public string File { get; set; }
            public int Line { get; set; }
            public string Function { get; set; }
            public string Name { get; set; }
            public bool Enabled { get; set; }
            public string Condition { get; set; }
        }

        private sealed class BreakpointCommandResult
        {
            public string Command { get; set; }
            public int DevenvPid { get; set; }
            public string DteMoniker { get; set; }
            public string BreakpointFile { get; set; }
            public int BreakpointLine { get; set; }
            public int AddedCount { get; set; }
            public int RemovedCount { get; set; }
            public List<BreakpointInfo> Breakpoints { get; set; }
            public List<string> Messages { get; set; }
        }

        private sealed class ErrorInfo
        {
            public string Type { get; set; }
            public string Message { get; set; }

            public static ErrorInfo FromException(System.Exception exception)
            {
                return new ErrorInfo
                {
                    Type = exception.GetType().FullName,
                    Message = exception.Message
                };
            }
        }
    }
}
