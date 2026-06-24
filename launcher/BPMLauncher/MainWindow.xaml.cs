using System.Windows;
using BPMLauncher.Core;
using Microsoft.Win32;

namespace BPMLauncher
{
    public partial class MainWindow : Window
    {
        private SettingsManager settings;
        private string currentSc2Path = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
            settings = new SettingsManager();
            InitializeLauncher();
        }

        private void InitializeLauncher()
        {
            // 1. Load saved path
            string savedPath = settings.GetSavedInstallPath();
            if (SC2Locator.ValidateSC2Path(savedPath))
            {
                currentSc2Path = savedPath;
            }
            else
            {
                // 2. Auto locate
                currentSc2Path = SC2Locator.FindSC2InstallPath();
                if (!string.IsNullOrEmpty(currentSc2Path))
                {
                    settings.SaveInstallPath(currentSc2Path);
                }
            }

            UpdateUI();
        }

        private void UpdateUI()
        {
            if (string.IsNullOrEmpty(currentSc2Path))
            {
                TxtSc2Path.Text = "Not Found. Please browse manually.";
                TxtModStatus.Text = "Mod File: N/A";
                TxtMapStatus.Text = "Map File: N/A";
                BtnRunTestMap.IsEnabled = false;
                return;
            }

            TxtSc2Path.Text = currentSc2Path;

            FileManager fileManager = new FileManager(currentSc2Path);
            bool modExists = fileManager.CheckCoreModExists();
            bool mapExists = fileManager.CheckTestMapExists();

            TxtModStatus.Text = modExists ? "Mod File: OK (BPM_Core.SC2Mod)" : "Mod File: Missing (BPM_Core.SC2Mod)";
            TxtMapStatus.Text = mapExists ? "Map File: OK (BPM_1v1_TestMap.SC2Map)" : "Map File: Missing (BPM_1v1_TestMap.SC2Map)";

            BtnRunTestMap.IsEnabled = mapExists;
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "StarCraft II (SC2Switcher_x64.exe)|SC2Switcher_x64.exe";
            dialog.Title = "Select SC2Switcher_x64.exe in Support64 folder";

            if (dialog.ShowDialog() == true)
            {
                string path = System.IO.Path.GetDirectoryName(dialog.FileName); // Support64
                if (path != null)
                {
                    path = System.IO.Path.GetDirectoryName(path); // SC2 Root
                }
                
                if (path != null && SC2Locator.ValidateSC2Path(path))
                {
                    currentSc2Path = path;
                    settings.SaveInstallPath(path);
                    UpdateUI();
                }
                else
                {
                    MessageBox.Show("Invalid StarCraft II path.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnRunTestMap_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(currentSc2Path))
            {
                GameRunner runner = new GameRunner(currentSc2Path);
                runner.RunTestMap();
            }
        }
    }
}
