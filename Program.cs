using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace ModOrganizerLinkGenerator
{
    public static class Program
    {
        private static Config _config;

        public static void Main() {
            string configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ModOrganizerLinkGenerator.cfg");
            _config = new Config(configFile);

            if (string.IsNullOrEmpty(_config.GamePath)) {
                Console.Write("GamePath: ");
                _config.GamePath = Console.ReadLine();
                _config.Save();
            }

            if (string.IsNullOrEmpty(_config.RootPath)) {
                Console.Write("RootPath: ");
                _config.RootPath = Console.ReadLine();
                _config.Save();
            }

            if (string.IsNullOrEmpty(_config.SelectedProfile)) {
                string path = Path.Combine(_config.ProfilesPath);
                string[] dirs = Directory.GetDirectories(path).Select(Path.GetFileName).ToArray();

                int index;
                if (dirs.Length > 1) {
                    Console.WriteLine("SelectedProfile: ");

                    for (int i = 0; i < dirs.Length; i++) {
                        Console.WriteLine(i + 1 + ": " + dirs[i]);
                    }

                    index = int.Parse(Console.ReadLine() ?? "1") - 1;
                } else {
                    index = 0;
                }

                _config.SelectedProfile = dirs[index];
                _config.Save();
                UpdateLinks();
            } else {
                DeleteAllLinks();
                _config.SelectedProfile = null;
                _config.Save();
            }

        }

        private static void DeleteAllLinks() {
            Dictionary<string, string> diff = Settings.Links.ToDictionary(o => o.Key, o => (string) null);
            UpdateLinks(diff);
            DeleteEmptyDirs();
            Settings.Links.Clear();
            Settings.Save();
        }

        private static void DeleteEmptyDirs() {
            Console.Write("Removing empty directories ...");
            string dataDir = Path.Combine(_config.GamePath, "Data");

            void ProcessDirectory(string path) {
                foreach (string directory in Directory.GetDirectories(path)) {
                    ProcessDirectory(directory);
                    if (Directory.GetFiles(directory).Length == 0 && Directory.GetDirectories(directory).Length == 0) {
                        ReplaceLine($"Removing empty directories ... ({directory.Substring(dataDir.Length + 1)})");
                        Directory.Delete(directory, false);
                    }
                }
            }

            ProcessDirectory(dataDir);
        }

        private static Dictionary<string, string> GetFileLinksDiff(Dictionary<string, string> fileLinks) {
            Console.WriteLine();
            Console.Write("Resolving file links ... ");
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            int actual = 0;
            int insert = 0;
            int update = 0;
            int delete = 0;

            foreach (KeyValuePair<string, string> link in Settings.Links) {
                if (fileLinks.TryGetValue(link.Key, out string current)) {
                    if (current == link.Value) {
                        ++actual;
                    } else {
                        ++update;
                        result.Add(link.Key, current);
                    }
                } else {
                    result.Add(link.Key, null);
                    ++delete;
                }
            }

            foreach (KeyValuePair<string, string> link in fileLinks) {
                if (!Settings.Links.ContainsKey(link.Key)) {
                    result.Add(link.Key, link.Value);
                    ++insert;
                }
            }

            ReplaceLine($"Resolving file links ... {actual} up to date, {insert} new, {update} changed and {delete} deleted links found.");
            return result;
        }

        private static void LinkIniFiles() {
            Console.WriteLine("Linking ini files ... ");
            string[] files = {"fallout4.ini", "Fallout4Custom.ini", "fallout4prefs.ini"};

            string documentsConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Fallout4");
            foreach (string file in files) {
                string filePath = Path.Combine(_config.SelectedProfilePath, file);
                string destPath = Path.Combine(documentsConfigPath, file);
                if (File.Exists(destPath)) {
                    File.SetAttributes(filePath, FileAttributes.Normal);
                    File.Delete(destPath);
                }

                if (File.Exists(filePath)) {
                    PInvoke.CreateHardLink(destPath, filePath, IntPtr.Zero);
                }
            }
        }

        private static void LinkSaveDirectory() {
            Console.WriteLine("Creating save folder link ... ");
            if (!Directory.Exists(_config.SavesPath)) {
                Console.WriteLine("  source directory not found, creating new one (new profile?) ... ");
                Directory.CreateDirectory(_config.SavesPath);
            }

            string documentsConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Fallout4");
            string destDir = Path.Combine(documentsConfigPath, "Saves");
            if (Directory.Exists(destDir)) {
                Directory.Delete(destDir);
            }

            PInvoke.CreateSymbolicLink(destDir, _config.SavesPath, PInvoke.SYMBOLIC_LINK_FLAG.DIRECTORY);
        }

        private static string[] LoadMods() {
            Console.WriteLine($"Loading mod list for profile {_config.SelectedProfile} ... ");

            string modList = Path.Combine(_config.SelectedProfilePath, "modlist.txt");

            if (!File.Exists(modList)) {
                throw new FileNotFoundException("File 'modlist.txt' was not found in profile directory!");
            }

            IEnumerable<string> lines = File.ReadLines(modList, Encoding.UTF8);
            int allMods = 0;
            IList<string> activeMods = new List<string>();
            foreach (string line in lines) {
                if (line[0] == '#') {
                    continue;
                }

                ++allMods;
                if (line[0] == '+') {
                    activeMods.Add(line.Substring(1));
                }
            }

            string[] result = activeMods.Reverse().ToArray();
            Console.WriteLine($"  {allMods} mods found out of which {result.Length} are active.");
            return result;
        }

        private static void ReplaceLine(string message) {
            int diff = Console.WindowWidth - message.Length - 1;
            if (diff < 0) {
                message = message.Substring(0, Console.WindowWidth - 4) + "...";
            } else if (diff > 0) {
                message += new string(' ', diff);
            }

            Console.Write("\r" + message);
        }

        private static Dictionary<string, string> ResolveFileLinks(string[] modList) {
            Console.Write("Listing files for mods ... ");
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            for (int i = 0; i < modList.Length; i++) {
                string modName = modList[i];
                string modPath = Path.Combine(_config.ModsPath, modName);
                IEnumerable<string> files = Directory.EnumerateFiles(modPath, "*.*", SearchOption.AllDirectories);
                int counter = 0;
                foreach (string file in files) {
                    string relativePath = file.Substring(modPath.Length + 1);

                    // meta.ini is MO file ...
                    if (relativePath == "meta.ini") {
                        continue;
                    }

                    result[relativePath] = modName;
                    ++counter;
                }

                ReplaceLine($"Listing files for mods ... {i + 1} of {modList.Length}: {counter} files found");
            }


            return result;
        }

        private static void UpdateLinks() {
            if (Settings.ProfileName != _config.SelectedProfile) {
                LinkSaveDirectory();
                LinkIniFiles();
                Settings.ProfileName = _config.SelectedProfile;
                Settings.Save();
            }

            UpdatePlugins();
            string[] modList = LoadMods();
            Dictionary<string, string> actualLinks = ResolveFileLinks(modList);
            Dictionary<string, string> diff = GetFileLinksDiff(actualLinks);
            UpdateLinks(diff);
            DeleteEmptyDirs();
            Settings.Links.Clear();
            foreach (KeyValuePair<string, string> pair in actualLinks) {
                Settings.Links.Add(pair.Key, pair.Value);
            }

            Settings.Save();
        }

        private static void UpdateLinks(Dictionary<string, string> diff) {
            string ShortKey(string key) {
                string keyStr = key;
                if (key.Length > 100) {
                    keyStr = $"{key.Substring(0, 50)}...{key.Substring(key.Length - 45)}";
                }

                return keyStr;
            }

            string[] keys = diff.Keys.ToArray();
            for (int i = 0; i < keys.Length; i++) {
                string key = keys[i];
                string value = diff[key];
                ReplaceLine($"{(value != null ? "Updating" : "Deleting")} file link ... {i + 1} of {keys.Length} ({ShortKey(key)}).");

                string destPath = Path.Combine(_config.GamePath, "Data", key);
                if (File.Exists(destPath)) {
                    // .BAK file is backup of vanila game file, that is overriden by a mod ...
                    if (value != null) {
                        File.Move(destPath, destPath + ".BAK");
                    } else {
                        File.Delete(destPath);
                        if (File.Exists(destPath + ".BAK")) {
                            File.Move(destPath + ".BAK", destPath);
                        }
                    }
                }

                if (value != null) {
                    string srcPath = Path.Combine(_config.ModsPath, value, key);
                    string dirPath = Path.GetDirectoryName(destPath);
                    Debug.Assert(dirPath != null, nameof(dirPath) + " != null");
                    if (!Directory.Exists(dirPath)) {
                        Directory.CreateDirectory(dirPath);
                    }

                    PInvoke.CreateHardLink(destPath, srcPath, IntPtr.Zero);
                }
            }

            Console.WriteLine();
        }

        private static void UpdatePlugins() {
            // older version of MO2 has a bug, where plugins.txt includes base game and DLC esm ...
            Console.WriteLine("Updating plugins.txt ... ");

            string srcPath = Path.Combine(_config.SelectedProfilePath, "plugins.txt");
            string destPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Fallout4", "plugins.txt");

            string[] dlc = {
                "*Fallout4.esm",
                "*DLCWorkshop01.esm",
                "*DLCWorkshop02.esm",
                "*DLCWorkshop03.esm",
                "*DLCCoast.esm",
                "*DLCRobot.esm",
                "*DLCNukaWorld.esm"
            };

            IEnumerable<string> plugins = File.ReadLines(srcPath).Where(o => o[0] != '#' && !dlc.Contains(o));
            File.WriteAllLines(destPath, plugins);
        }
    }
}