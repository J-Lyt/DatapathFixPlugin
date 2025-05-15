using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Frosty.Core;
using FrostySdk;
using FrostySdk.Interfaces;
using Frosty.Core.Attributes;
using Newtonsoft.Json;
using Frosty.Controls;
using System.Media;

namespace DatapathFixPlugin.Actions {
    public class LaunchExecutionAction : ExecutionAction {
        public string FSBasePath {
            get {
                dynamic fileSystem = typeof(App).GetField("FileSystem")?.GetValue(this) ?? typeof(App).GetField("FileSystemManager")?.GetValue(this);
                return fileSystem.BasePath;
            }
        }

        public string Game => Path.Combine(FSBasePath, $"{ProfilesLibrary.ProfileName}.exe");
        public string Par => Path.Combine(FSBasePath, $"{ProfilesLibrary.ProfileName}.par");

        public static string DatapathFix {
            get {
                string fileName = "DatapathFix.exe";
                if (Config.Get("DatapathFixDebugMode", false)) {
                    fileName = "DatapathFix(DEBUG).exe";
                }
                return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DatapathFix", fileName);
            }
        }

        public Version CurrentVersion = new Version(Assembly.GetExecutingAssembly().GetCustomAttribute<PluginVersionAttribute>().Version);

        public override Action<ILogger, PluginManagerType, CancellationToken> PreLaunchAction => new Action<ILogger, PluginManagerType, CancellationToken>((ILogger logger, PluginManagerType type, CancellationToken cancelToken) => {
            bool FirstLaunch = Config.Get("DatapathFixFirstLaunch", true) && Config.Get("DatapathFixEnabled", true);

            if (FirstLaunch)
            {
                MessageBoxResult result = FrostyMessageBox.Show(
                    "DatapathFix fixes an issue with modding games on Epic Games Store where mods do not appear in the game.\n\r\n" +
                    "It can also be used to bypass the 'Launch Game with custom arguments' window on Steam when launching the game. If the game fails to launch or your mods do not appear in-game when this plugin is enabled, you can disable it by going to: Tools > Options > DatapathFix Options.\n\n" +
                    "Would you like to keep this plugin enabled?", "DatapathFixPlugin", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.No || result == MessageBoxResult.Cancel)
                {
                    FrostyMessageBox.Show("DatapathFix has been disabled", "DatapathFixPlugin", MessageBoxButton.OK);
                    Config.Add("DatapathFixEnabled", false);
                }

                Config.Add("DatapathFixFirstLaunch", false);
                Config.Save();
            }

            if (Config.Get("DatapathFixEnabled", true) && File.Exists(DatapathFix)) {
                ResetGameDirectory();

                Thread.Sleep(1000);

                string cmdArgs = $"-dataPath \"{Path.Combine(FSBasePath, $"ModData\\{App.SelectedPack}")}\" ";
                cmdArgs += Config.Get("CommandLineArgs", "", ConfigScope.Game);

                try {
                    File.WriteAllText(Path.Combine(FSBasePath, "tmp"), cmdArgs);
                    File.Move(Game, Game.Replace(".exe", ".orig.exe"));
                    if (File.Exists(Par))
                        File.Copy(Par, Par.Replace(".par", ".orig.par"), true);
                    File.Copy(DatapathFix, Game, true);
                }
                catch (Exception ex) {
                    App.Logger.LogError(ex.Message);
                }

                Thread.Sleep(1000);
            }
            else if (!File.Exists(DatapathFix)) {
                App.Logger.LogError($"Cannot find {DatapathFix}");

                Task.Run(() => {
                    SystemSounds.Exclamation.Play();
                    FrostyMessageBox.Show($"Cannot find \\Plugins\\DatapathFix\\{Path.GetFileName(DatapathFix)}", "DatapathFixPlugin", MessageBoxButton.OK);
                });
            }
        });

        public override Action<ILogger, PluginManagerType, CancellationToken> PostLaunchAction => new Action<ILogger, PluginManagerType, CancellationToken>((ILogger logger, PluginManagerType type, CancellationToken cancelToken) => { });

        private void ResetGameDirectory() {
            try {
                File.Delete(Path.Combine(FSBasePath, "tmp"));
                File.Delete(Par.Replace(".par", ".orig.par"));

                // only delete game.old if it is less than 1MB to ensure it does not delete the actual game
                string gameOld = Game.Replace(".exe", ".old");
                if (File.Exists(gameOld) && new FileInfo(gameOld).Length < 1000000)
                    File.Delete(gameOld);
            }
            catch (Exception ex) {
                App.Logger.LogWarning(ex.Message);
            }

            try {
                if (File.Exists(Game.Replace(".exe", ".orig.exe")) && new FileInfo(Game).Length < 1000000) {
                    File.Delete(Game);
                    File.Move(Game.Replace(".exe", ".orig.exe"), Game);
                }
            }
            catch (Exception ex) {
                App.Logger.LogWarning(ex.Message);
            }
        }

        public LaunchExecutionAction() {
            App.Logger.Log($"DatapathFix v{CurrentVersion} by Dyvinia");
            App.Logger.Log(@"Github: https://github.com/J-Lyt/DatapathFixPlugin");
            App.Logger.Log(@"Donate: https://ko-fi.com/Dyvinia");

            if (ProfilesLibrary.IsLoaded(ProfileVersion.DragonAgeTheVeilguard))
            {
                App.Logger.Log(@"Note: This plugin is only needed for Epic Games Store but can be used to bypass the 'Launch Game with custom arguments' window on Steam.");
            }
            else
            {
                App.Logger.Log(@"Note: This plugin is only needed for Steam or Epic Games Store; no longer needed when using only the EA App.");
            }

            ExtractDatapathFix();
            ResetGameDirectory();
        }

        public static void ExtractDatapathFix() {
            if (!Directory.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DatapathFix")))
                Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DatapathFix"));

            if (File.Exists(DatapathFix))
                File.Delete(DatapathFix);

            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("DatapathFixPlugin.DatapathFix.DatapathFix.exe")) {
                using (FileStream f = File.OpenWrite(DatapathFix)) {
                    s.CopyTo(f);
                    s.Close();
                    f.Close();
                }
            }
        }
    }
}
