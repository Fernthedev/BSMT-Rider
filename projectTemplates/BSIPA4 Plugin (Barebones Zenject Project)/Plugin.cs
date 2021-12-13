using System.Linq;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using SiraUtil.Zenject;
using $safeprojectname$.Configuration;
using $safeprojectname$.Installers;
using IPALogger = IPA.Logging.Logger;

namespace $safeprojectname$
{
    [Plugin(RuntimeOptions.DynamicInit), NoEnableDisable]  // NoEnableDisable supresses the warnings of not having a OnEnable/OnStart
                                                           // and OnDisable/OnExit methods
    public class Plugin
    {
        internal static Plugin Instance { get; private set; }
        internal static IPALogger Log { get; private set; }
        internal PluginConfig _config;

        [Init]
        public void Init(Zenjector zenjector, IPALogger logger, Config config)
        {
            Instance = this;
            Log = logger;

            zenjector.UseLogger(logger);
            zenjector.UseMetadataBinder<Plugin>();
            
            // This logic also goes for installing to Menu and Game. "Location." will give you a list of places to install to.
            zenjector.Install<AppInstaller>(Location.App, config.Generated<PluginConfig>());
            // zenjector.Install<{Menu|Game}Installer>(Location.{Menu|Game}>());
        }
    }
}
