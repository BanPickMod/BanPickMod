using System.Diagnostics;
using System.IO;

namespace BPMLauncher.Core
{
    public class GameRunner
    {
        private string sc2Path;

        public GameRunner(string sc2Path)
        {
            this.sc2Path = sc2Path;
        }

        public void RunTestMap()
        {
            string switcherPath = Path.Combine(sc2Path, @"Support64\SC2Switcher_x64.exe");
            string mapPath = Path.Combine(sc2Path, @"Maps\BanPickMode\BPM_1v1_TestMap.SC2Map");

            if (File.Exists(switcherPath) && File.Exists(mapPath))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = switcherPath,
                    Arguments = $"\"{mapPath}\"",
                    WorkingDirectory = sc2Path
                };
                Process.Start(startInfo);
            }
        }
    }
}
