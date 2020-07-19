using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ModOrganizerLinkGenerator
{
    internal class Config
    {
        private readonly string _FilePath;
        private readonly Dictionary<string, string> _Values;

        public Config(string filePath) {
            _FilePath = filePath;
            if (File.Exists(filePath)) {
                _Values = File.ReadAllLines(filePath).Select(o => o.Split('=')).ToDictionary(o => o[0], o => o[1], StringComparer.InvariantCultureIgnoreCase);
            } else {
                _Values = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) {
                    {"GamePath", ""},
                    {"RootPath", ""},
                    {"SelectedProfile", ""}
                };
            }
        }

        public string GamePath {
            get => _Values["GamePath"];
            set => _Values["GamePath"] = value;
        }

        public string RootPath {
            get => _Values["RootPath"];
            set => _Values["RootPath"] = value;
        }

        public string ModsPath => Path.Combine(RootPath, "mods");

        public string SelectedProfilePath => Path.Combine(ProfilesPath, SelectedProfile);

        public string ProfilesPath => Path.Combine(RootPath, "profiles");

        public string SavesPath => Path.Combine(RootPath, "saves", SelectedProfile);

        public string SelectedProfile {
            get => _Values["SelectedProfile"];
            set => _Values["SelectedProfile"] = value;
        }

        public void Save() {
            File.WriteAllLines(_FilePath, _Values.Select(o => o.Key + "=" + o.Value));
        }
    }
}