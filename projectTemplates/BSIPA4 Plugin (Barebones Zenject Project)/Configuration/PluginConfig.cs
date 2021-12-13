using System.Runtime.CompilerServices;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace $safeprojectname$.Configuration
{
    internal class PluginConfig
    {
        public static PluginConfig Instance { get; set; }
        
        /// A value for the config has to be virtual if you want BSIPA
        /// to detect a value change and save the config automatically
        // public virtual int MeaningofLife = 42 { get; set; } 
        
    }
}
