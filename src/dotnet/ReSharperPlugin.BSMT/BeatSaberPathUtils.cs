using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
#pragma warning disable CS8604


// ReSharper disable All
#pragma warning disable CS8603
#pragma warning disable CS8600

namespace ReSharperPlugin.BSMT_Rider
{
    /// <summary>
    /// IT'S ALIVE! IT'S ALIVE!!!! FRANKENSTEIN LIVES!
    /// THE FRANKENSTEIN CODE OF
    /// https://github.com/Zingabopp/BeatSaberModdingTools/blob/master/BeatSaberModdingTools/Utilities/BeatSaberTools.cs
    /// AND
    /// https://github.com/Umbranoxio/beatsaber-gc-fixer/blob/42d67a967209f8a8eed9ef8ef77c33c587218b51/GCFixer/Utils.cs#L12
    /// </summary>
    public class BeatSaberPathUtils
    {
        private static readonly string OCULUS_LM_KEY = Path.Combine("SOFTWARE", "WOW6432Node", "Oculus VR, LLC", "Oculus", "Config");
        private static readonly string OCULUS_CU_KEY = Path.Combine("SOFTWARE", "Oculus VR, LLC", "Oculus", "Libraries");

        /// <summary>
        /// Path to the Steam Beat Saber uninstall registry entry, not always present.
        /// </summary>
        private static readonly string STEAM_BS_UNINSTALL_REG_KEY = Path.Combine("SOFTWARE", "Microsoft", "Windows", "CurrentVersion", "Uninstall", "Steam App 620980");
        /// <summary>
        /// Path to config.vdf from the Steam install folder.
        /// </summary>
        private static readonly string STEAM_CONFIG_PATH = Path.Combine("config", "config.vdf");

        private static readonly string STEAM_PATH_KEY = Path.Combine("SOFTWARE", "WOW6432Node", "Valve", "Steam");


        public static List<string> GetInstallDir()
        {
            List<String> paths = new List<string>();

            var InstallDir = "";

            if (!string.IsNullOrEmpty(InstallDir)
                && Directory.Exists(InstallDir)
                && Directory.Exists(Path.Combine(InstallDir, "Beat Saber_Data", "Plugins"))
                && File.Exists(Path.Combine(InstallDir, "Beat Saber.exe")))
            {
                paths.Add(InstallDir);
            }

            try
            {
                InstallDir = GetSteamDir();
            }
            catch
            {
                // ignored
            }

            if (!string.IsNullOrEmpty(InstallDir))
            {
                paths.Add(InstallDir);
            }

            try
            {
                InstallDir = GetOculusDir();
            }
            catch
            {
                // ignored
            }

            if (!string.IsNullOrEmpty(InstallDir))
            {
                paths.Add(InstallDir);
            }

            try
            {
                foreach (var path in GetOculusLibraryPaths())
                {
                    if (!string.IsNullOrEmpty(InstallDir))
                    {
                        paths.Add(InstallDir);
                    }
                    InstallDir = FindBeatSaberInOculusLibrary(path);
                }
            }
            catch
            {
                // ignored
            }

            if (!string.IsNullOrEmpty(InstallDir))
            {
                paths.Add(InstallDir);
            }

            return paths;
        }

        // Umbra's code
        public static string GetSteamDir()
        {

            var SteamInstall = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                ?.OpenSubKey("SOFTWARE")?.OpenSubKey("WOW6432Node")?.OpenSubKey("Valve")?.OpenSubKey("Steam")
                ?.GetValue("InstallPath").ToString();
            if (string.IsNullOrEmpty(SteamInstall))
            {
                SteamInstall = Registry.LocalMachine.OpenSubKey("SOFTWARE")?.OpenSubKey("WOW6432Node")
                    ?.OpenSubKey("Valve")?.OpenSubKey("Steam")?.GetValue("InstallPath").ToString();
            }

            if (string.IsNullOrEmpty(SteamInstall)) return null;

            var vdf = Path.Combine(SteamInstall, @"steamapps\libraryfolders.vdf");
            if (!File.Exists(vdf)) return null;

            var regex = new Regex("\\s\"\\d\"\\s+\"(.+)\"");
            var SteamPaths = new List<string>
            {
                Path.Combine(SteamInstall, @"steamapps")
            };

            using (var reader = new StreamReader(@vdf))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var match = regex.Match(line);
                    if (match.Success)
                    {
                        SteamPaths.Add(Path.Combine(match.Groups[1].Value.Replace(@"\\", @"\"), @"steamapps"));
                    }
                }
            }

            regex = new Regex("\\s\"installdir\"\\s+\"(.+)\"");
            foreach (var path in SteamPaths.Where(path => File.Exists(Path.Combine(@path, @"appmanifest_620980.acf"))))
            {
                using (var reader = new StreamReader(Path.Combine(@path, @"appmanifest_620980.acf")))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var match = regex.Match(line);
                        if (!match.Success) continue;
                        if (File.Exists(Path.Combine(path, @"common", match.Groups[1].Value,
                            "Beat Saber.exe")))
                        {
                            return Path.Combine(path, @"common", match.Groups[1].Value);
                        }
                    }
                }
            }

            return null;
        }

        // Umbra's code
        public static string GetOculusDir()
        {
            try
            {
                var OculusInstall = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                    ?.OpenSubKey("SOFTWARE")?.OpenSubKey("Wow6432Node")?.OpenSubKey("Oculus VR, LLC")
                    ?.OpenSubKey("Oculus")?.OpenSubKey("Config")?.GetValue("InitialAppLibrary").ToString();
                if (string.IsNullOrEmpty(OculusInstall)) return null;

                if (string.IsNullOrEmpty(OculusInstall)) return null;
                return File.Exists(Path.Combine(OculusInstall, "Software", "hyperbolic-magnetism-beat-saber",
                    "Beat Saber.exe")) ? Path.Combine(OculusInstall, "Software", "hyperbolic-magnetism-beat-saber") : null;
            }
            catch (Exception)
            {
                return null;
            }

        }

        // Zingabopp's Code
        public static string FindBeatSaberInOculusLibrary(string oculusLibraryPath)
        {
            var possibleLocation = Path.Combine(oculusLibraryPath, "hyperbolic-magnetism-beat-saber");
            string matchedLocation = null;
            if (Directory.Exists(possibleLocation))
            {
                if (IsBeatSaberDirectory(possibleLocation))
                    return possibleLocation;
            }
            else
            {
                var softwareFolder = Path.Combine(oculusLibraryPath, "Software");
                if (Directory.Exists(softwareFolder))
                    matchedLocation = FindBeatSaberInOculusLibrary(softwareFolder);
            }

            return matchedLocation;
        }

        // Zingabopp's Code
        public static string FindBeatSaberInSteamLibrary(string steamLibraryPath)
        {
            var possibleLocation = Path.Combine(steamLibraryPath, "SteamApps", "common", "Beat Saber");
            if (!Directory.Exists(possibleLocation)) return null;
            return IsBeatSaberDirectory(possibleLocation) ? possibleLocation : null;
        }

        // Zingabopp's Code
        public static string[] GetOculusLibraryPaths()
        {
            List<string> paths = new List<string>();
            using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)) // Doesn't work in 32 bit mode without this
            {
                using (RegistryKey oculusKey = hklm?.OpenSubKey(OCULUS_LM_KEY))
                {
                    string path = (string)oculusKey?.GetValue("InitialAppLibrary", string.Empty);
                    if (!string.IsNullOrEmpty(path))
                    {
                        paths.Add(path);
                    }
                }
            }
            using (RegistryKey hkcu = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64)) // Doesn't work in 32 bit mode without this
            {
                using (RegistryKey oculusKey = hkcu?.OpenSubKey(OCULUS_CU_KEY))
                {
                    if (oculusKey != null && oculusKey.SubKeyCount > 0)
                    {
                        foreach (string libraryKeyName in oculusKey.GetSubKeyNames())
                        {
                            using (RegistryKey library = oculusKey.OpenSubKey(libraryKeyName))
                            {
                                string path = (string)library?.GetValue("OriginalPath", string.Empty);
                                if (!string.IsNullOrEmpty(path) && !paths.Contains(path))
                                    paths.Add(path);
                            }
                        }
                    }
                }
            }
            return paths.ToArray();
        }


        public static bool IsBeatSaberDirectory(string path)
        {
            if (string.IsNullOrEmpty(path?.Trim()))
                return false;
            DirectoryInfo bsDir;
            try
            {
                bsDir = new DirectoryInfo(path);
            }
            catch
            {
                return false;
            }

            if (!bsDir.Exists) return false;
            var files = bsDir.GetFiles("Beat Saber.exe");
            return files.Any();

        }
    }
}