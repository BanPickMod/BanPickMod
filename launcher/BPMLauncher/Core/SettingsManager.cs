using System;
using System.IO;

namespace BPMLauncher.Core
{
    public class SettingsManager
    {
        public string AppDataPath { get; private set; }
        public string InstallPathFile { get; private set; }
        public string StateFile { get; private set; }

        public SettingsManager()
        {
            AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SC2BanPickModeLauncher");
            if (!Directory.Exists(AppDataPath))
            {
                Directory.CreateDirectory(AppDataPath);
            }

            InstallPathFile = Path.Combine(AppDataPath, "install-path.txt");
            StateFile = Path.Combine(AppDataPath, "state.txt");
        }

        public string GetSavedInstallPath()
        {
            if (File.Exists(InstallPathFile))
            {
                return File.ReadAllText(InstallPathFile).Trim();
            }
            return string.Empty;
        }

        public void SaveInstallPath(string path)
        {
            File.WriteAllText(InstallPathFile, path);
        }
    }
}
