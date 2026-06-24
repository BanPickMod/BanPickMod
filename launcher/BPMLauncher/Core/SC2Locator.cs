using System.IO;
using Microsoft.Win32;

namespace BPMLauncher.Core
{
    public class SC2Locator
    {
        public static string FindSC2InstallPath()
        {
            // Try Registry
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Blizzard Entertainment\Starcraft II"))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("InstallPath");
                        if (val != null)
                        {
                            string path = val.ToString();
                            if (ValidateSC2Path(path))
                            {
                                return path;
                            }
                        }
                    }
                }
            }
            catch { }

            // Default Paths
            string[] defaultPaths = new string[]
            {
                @"C:\Program Files (x86)\StarCraft II",
                @"C:\Program Files\StarCraft II"
            };

            foreach (var path in defaultPaths)
            {
                if (ValidateSC2Path(path))
                {
                    return path;
                }
            }

            return string.Empty;
        }

        public static bool ValidateSC2Path(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            return Directory.Exists(path) && File.Exists(Path.Combine(path, @"Support64\SC2Switcher_x64.exe"));
        }
    }
}
