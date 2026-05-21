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

            return Directory.GetCurrentDirectory();
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

        private static string ResolveSolutionPath(string root, CommandOptions options, bool required)
        {
            string configured = options.Get("solution");
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return ResolvePath(root, configured);
            }

            string[] candidates = Directory.Exists(root)
                ? Directory.GetFiles(root, "*.sln", SearchOption.TopDirectoryOnly)
                : new string[0];

            if (candidates.Length == 1)
            {
                return Path.GetFullPath(candidates[0]);
            }

            if (candidates.Length > 1)
            {
                if (required)
                {
                    throw new InvalidOperationException("Multiple solution files found under root. Provide --solution.");
                }

                return null;
            }

            if (required)
            {
                throw new InvalidOperationException("Missing --solution and no .sln file was found under root.");
            }

            return null;
        }

        private static string ResolveOptionalPath(string root, string path)
        {
            return string.IsNullOrWhiteSpace(path) ? null : ResolvePath(root, path);
        }

        private static string GetTargetMarker(CommandOptions options)
        {
            string marker = options.Get("target-marker");
            if (!string.IsNullOrWhiteSpace(marker))
            {
                return marker;
            }

            string testDll = options.Get("test-dll");
            return string.IsNullOrWhiteSpace(testDll) ? null : Path.GetFileName(testDll);
        }

        private static string RequireTargetMarker(CommandOptions options)
        {
            string marker = GetTargetMarker(options);
            if (string.IsNullOrWhiteSpace(marker))
            {
                throw new InvalidOperationException("Missing --target-marker or --test-dll. Refusing to clean all test processes without a marker.");
            }

            return marker;
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
