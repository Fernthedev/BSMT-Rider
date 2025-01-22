using IPA;
using IPA.Config.Stores;
using IPA.Loader;
using UnityEngine;
using IpaLogger = IPA.Logging.Logger;
using IpaConfig = IPA.Config.Config;

namespace MenuPlugin;

[Plugin(RuntimeOptions.DynamicInit)]
internal class Plugin
{
    internal static IpaLogger Log { get; private set; } = null!;
    internal static PluginConfig Config { get; private set; } = null!;

#if (EnableHints)
    // Methods with [Init] are called when the plugin is first loaded by IPA.
    // All the parameters are provided by IPA and are optional.
    // The constructor is called before any method with [Init]. Only use [Init] with one constructor.
#endif
    [Init]
    public Plugin(IpaLogger ipaLogger, IpaConfig ipaConfig, PluginMetadata pluginMetadata)
    {
        Log = ipaLogger;
#if (EnableHints)
        // Creates an instance of PluginConfig used by IPA to load and store config values
#endif
        Config = ipaConfig.Generated<PluginConfig>();
            
        Log.Info($"{pluginMetadata.Name} {pluginMetadata.HVersion} initialized.");
    }
        
    [OnStart]
    public void OnApplicationStart()
    {
        Log.Debug("OnApplicationStart");
        new GameObject("MenuPluginController").AddComponent<MenuPluginController>();
    }

    [OnExit]
    public void OnApplicationQuit()
    {
        Log.Debug("OnApplicationQuit");
    }
}