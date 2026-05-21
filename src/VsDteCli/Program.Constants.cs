namespace VsDteCli
{
    internal static partial class Program
    {
        private const int DesignMode = 1;
        private const int RunMode = 2;
        private const int BreakMode = 3;
        private const int ShowWindowRestore = 9;
        private const string DefaultDteProgId = "VisualStudio.DTE.18.0";
        private const string DefaultDevenvPath = @"C:\Program Files\Microsoft Visual Studio\18\Enterprise\Common7\IDE\devenv.exe";
        private const string DefaultVstestPath = @"C:\Program Files\Microsoft Visual Studio\18\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe";
        private const string LiveDebugMarkerVariable = "VSDTECLI_LIVE_DEBUG";
        private const string LiveDebugFirstBreakVariable = "VSDTECLI_FIRST_BREAK";
        private const string LiveDebugFirstBreakScopeVariable = "VSDTECLI_FIRST_BREAK_SCOPE";
        private const string LiveDebugAttachWaitVariable = "VSDTECLI_ATTACH_WAIT_MS";
        private const string LiveDebugFirstBreakModeVariable = "VSDTECLI_FIRST_BREAK_MODE";
    }
}
