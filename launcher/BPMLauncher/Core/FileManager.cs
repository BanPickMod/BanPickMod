using System.IO;

namespace BPMLauncher.Core
{
    public class FileManager
    {
        private string sc2Path;

        public FileManager(string sc2Path)
        {
            this.sc2Path = sc2Path;
        }

        public bool CheckCoreModExists()
        {
            string modPath = Path.Combine(sc2Path, @"Mods\BPM_Core.SC2Mod");
            return Directory.Exists(modPath) || File.Exists(modPath);
        }

        public bool CheckTestMapExists()
        {
            string mapPath = Path.Combine(sc2Path, @"Maps\BanPickMode\BPM_1v1_TestMap.SC2Map");
            return File.Exists(mapPath);
        }
    }
}
