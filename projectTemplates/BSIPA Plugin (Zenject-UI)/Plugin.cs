using IPA;
using IPA.Config.Stores;
using IPA.Loader;
using SiraUtil.Zenject;
using ZenjectMenuPlugin.Installers;
using IpaLogger = IPA.Logging.Logger;
using IpaConfig = IPA.Config.Config;

namespace ZenjectMenuPlugin;

[Plugin(RuntimeOptions.DynamicInit), NoEnableDisable]
internal class Plugin
{
    internal static IpaLogger Log { get; private set; } = null!;
#if (EnableHints)
    // Methods with [Init] are called when the plugin is first loaded by IPA.
    // All the parameters are provided by IPA and are optional.
    // The constructor is called before any method with [Init]. Only use [Init] with one constructor.
#endif
    [Init]
    public Plugin(IpaLogger ipaLogger, IpaConfig ipaConfig, Zenjector zenjector, PluginMetadata pluginMetadata)
    {
        Log = ipaLogger;
        zenjector.UseLogger(Log);

#if (EnableHints)
        // Creates an instance of PluginConfig used by IPA to load and store config values
#endif
        var pluginConfig = ipaConfig.Generated<PluginConfig>();

#if (EnableHints)
        // Instructs SiraUtil to use this installer during Beat Saber's initialization
        // The PluginConfig is used as a constructor parameter for AppInstaller, so pass it to zenjector.Install()
#endif
        zenjector.Install<AppInstaller>(Location.App, pluginConfig);
#if (EnableHints)

        // Instructs SiraUtil to use this installer when the main menu initializes
#endif
        zenjector.Install<MenuInstaller>(Location.Menu);
            
        Log.Info($"{pluginMetadata.Name} {pluginMetadata.HVersion} initialized.");
    }
}