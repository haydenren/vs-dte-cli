using System;
using System.Globalization;
using System.IO;

namespace VsDteCli
{
    internal static partial class Program
    {
        private static string ResolveRoot(CommandOptions options)
        {
            string configured = options.Get("root");
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return Path.GetFullPath(configured);
            }

            string current = Directory.GetCurrentDirectory();
            if (File.Exists(Path.Combine(current, DefaultSolution)))
            {
                return current;
            }

            string nested = Path.Combine(current, "autotest.leyserplus");
            if (File.Exists(Path.Combine(nested, DefaultSolution)))
            {
                return nested;
            }

            return current;
        }

        private static string ResolvePath(string root, string path)
        {
            return Path.IsPathRooted(path) ? Path.GetFullPath(path) : Path.GetFullPath(Path.Combine(root, path));
        }

        private static string Require(CommandOptions options, string key)
        {
            string value = options.Get(key);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException("Missing required option --" + key);
            }

            return value;
        }

        private static string ModeName(int mode)
        {
            switch (mode)
            {
                case DesignMode:
                    return "design";
                case RunMode:
                    return "running";
                case BreakMode:
                    return "paused";
                default:
                    return "unknown";
            }
        }

        private static string SafeString(Func<object> getter)
        {
            try
            {
                object value = getter();
                return value == null ? null : Convert.ToString(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }

        private static int SafeInt(Func<object> getter, int defaultValue)
        {
            try
            {
                object value = getter();
                return value == null ? defaultValue : Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return defaultValue;
            }
        }

        private static bool SafeBool(Func<object> getter, bool defaultValue)
        {
            try
            {
                object value = getter();
                return value == null ? defaultValue : Convert.ToBoolean(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return defaultValue;
            }
        }

        private static void TryQuitDte(dynamic dte)
        {
            if (dte == null)
            {
                return;
            }

            try
            {
                dte.Quit();
            }
            catch
            {
            }
        }

        private static void TryActivateDteWindow(dynamic dte)
        {
            try
            {
                dte.MainWindow.Visible = true;
                dte.MainWindow.Activate();
                IntPtr hwnd = new IntPtr((int)dte.MainWindow.HWnd);
                ShowWindow(hwnd, ShowWindowRestore);
                SetForegroundWindow(hwnd);
            }
            catch
            {
            }
        }
    }
}
