using System;
using System.IO;

namespace VsDteCli
{
    internal static partial class Program
    {
        private static int RunSetNextStatement(CommandOptions options)
        {
            DteSession session = ResolveDteSession(options);
            string root = ResolveRoot(options);
            int line = options.GetIntOrDefault("line", 0);
            if (line <= 0)
            {
                throw new InvalidOperationException("--line is required.");
            }

            string file = options.Get("file");
            if (!string.IsNullOrWhiteSpace(file))
            {
                string fullPath = Path.IsPathRooted(file) ? file : ResolvePath(root, file);
                session.Dte.ItemOperations.OpenFile(fullPath);
            }

            dynamic selection = session.Dte.ActiveDocument.Selection;
            selection.GotoLine(line, true);
            session.Dte.ExecuteCommand("Debug.SetNextStatement");

            SceneInfo scene = CaptureScene(session, options);
            scene.Command = "set-next";
            WriteOutput(options, scene);
            return 0;
        }
    }
}
