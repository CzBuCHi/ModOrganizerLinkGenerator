using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ModOrganizerLinkGenerator
{
    internal static class Settings
    {
        private static readonly Dictionary<string, string> _Links = new Dictionary<string, string>();
        private static readonly string _SaveFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.dat");
        private static bool _loaded;
        private static string _profileName;

        public static Dictionary<string, string> Links {
            get {
                if (!_loaded) {
                    Load();
                    _loaded = true;
                }

                return _Links;
            }
        }

        public static string ProfileName {
            get {
                if (!_loaded) {
                    Load();
                    _loaded = true;
                }

                return _profileName;
            }
            set => _profileName = value;
        }

        public static void Save() {
            using (FileStream stream = File.Open(_SaveFile, FileMode.Create, FileAccess.Write)) {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8)) {
                    writer.Write(_profileName);
                    writer.Write(_Links.Count);
                    foreach (KeyValuePair<string, string> pair in _Links) {
                        writer.Write(pair.Key);
                        writer.Write(pair.Value);
                    }
                }
            }
        }

        private static void Load() {
            if (!File.Exists(_SaveFile)) {
                return;
            }

            try {
                using (FileStream stream = File.OpenRead(_SaveFile)) {
                    using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8)) {
                        _profileName = reader.ReadString();
                        _Links.Clear();
                        int count = reader.ReadInt32();
                        for (int i = 0; i < count; i++) {
                            string key = reader.ReadString();
                            string value = reader.ReadString();
                            _Links.Add(key, value);
                        }
                    }
                }
            } catch (Exception e) {
                try {
                    File.Delete(_SaveFile);
                } catch {
                    // noop
                }

                Console.WriteLine("Config load Error: " + e.Message);
                _profileName = null;
                _Links.Clear();
            }
        }
    }
}