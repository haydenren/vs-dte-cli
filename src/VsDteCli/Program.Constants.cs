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
        private const string DefaultSolution = "SchoolSiteTest.sln";
        private const string DefaultTestDll = @"SchoolSiteScript\bin\Debug\SchoolSiteScript.dll";
        private const string DefaultTargetMarker = "SchoolSiteScript.dll";
    }
}
