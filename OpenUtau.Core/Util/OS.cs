using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace OpenUtau {
    public static class OS {
        public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsMacOS() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static bool IsLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static bool IsAndroid() => OperatingSystem.IsAndroid();
        public static bool IsIOS() => OperatingSystem.IsIOS();


        public static void OpenFolder(string path) {
            if (Directory.Exists(path)) {
                Process.Start(new ProcessStartInfo {
                    FileName = GetOpener(),
                    Arguments = GetWrappedPath(path),
                    UseShellExecute = IsWindows(),
                });
            }
        }

        public static void GotoFile(string path) {
            if (File.Exists(path)) {
                var wrappedPath = GetWrappedPath(path);
                if (IsWindows()) {
                    Process.Start(new ProcessStartInfo {
                        FileName = GetOpener(),
                        Arguments = $"/select, {wrappedPath}",
                        UseShellExecute = true,
                    });
                } else if (IsMacOS()) {
                    Process.Start(new ProcessStartInfo {
                        FileName = GetOpener(),
                        Arguments = $" -R {wrappedPath}",
                    });
                } else {
                    OpenFolder(Path.GetDirectoryName(path));
                }
            }
        }

        /// <summary>
        /// Open a URL in the default browser. Prefer shell-execute with the URL so
        /// self-contained Linux builds work even when PATH does not include xdg-open.
        /// </summary>
        public static void OpenWeb(string url) {
            if (string.IsNullOrWhiteSpace(url)) {
                return;
            }
            try {
                Process.Start(new ProcessStartInfo {
                    FileName = url,
                    UseShellExecute = true,
                });
                return;
            } catch {
                // Fall back to platform openers below.
            }
            Process.Start(new ProcessStartInfo {
                FileName = GetOpener(),
                Arguments = url,
                UseShellExecute = IsWindows(),
            });
        }

        public static bool AppExists(string path) {
            if (IsMacOS()) {
                return Directory.Exists(path) && path.EndsWith(".app");
            } else {
                return File.Exists(path);
            }
        }

        public static string GetUpdaterRid() {
            if (IsWindows()) {
                if (RuntimeInformation.ProcessArchitecture == Architecture.X86) {
                    return "win-x86";
                }
                if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64) {
                    return "win-arm64";
                }
                return "win-x64";
            } else if (IsMacOS()) {
                if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64) {
                    return "osx-arm64";
                }
                return "osx-x64";
            } else if (IsLinux()) {
                if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64) {
                    return "linux-arm64";
                }
                return "linux-x64";
            }
            throw new NotSupportedException();
        }

        public static string WhereIs(string filename) {
            if (File.Exists(filename)) {
                return Path.GetFullPath(filename);
            }
            var values = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (var path in values.Split(Path.PathSeparator)) {
                if (string.IsNullOrWhiteSpace(path)) {
                    continue;
                }
                var fullPath = Path.Combine(path, filename);
                if (File.Exists(fullPath)) {
                    return fullPath;
                }
            }
            // Self-contained apps often ship with a stripped PATH that omits /usr/bin.
            if (IsLinux() || IsMacOS()) {
                foreach (var dir in new[] { "/usr/bin", "/bin", "/usr/local/bin" }) {
                    var fullPath = Path.Combine(dir, filename);
                    if (File.Exists(fullPath)) {
                        return fullPath;
                    }
                }
            }
            return null;
        }

        private static readonly string[] linuxOpeners = { "xdg-open", "gio", "mimeopen", "gnome-open", "open" };
        private static string GetOpener() {
            if (IsWindows()) {
                return "explorer.exe";
            }
            if (IsMacOS()) {
                return "open";
            }
            foreach (var opener in linuxOpeners) {
                string fullPath = WhereIs(opener);
                if (!string.IsNullOrEmpty(fullPath)) {
                    return fullPath;
                }
            }
            throw new IOException($"None of {string.Join(", ", linuxOpeners)} found. Install xdg-utils (xdg-open) or open the link manually.");
        }
        private static string GetWrappedPath(string path) {
            if (IsWindows()) {
                return path;
            } else {
                return $"\"{path}\"";
            }
        }
    }
}
