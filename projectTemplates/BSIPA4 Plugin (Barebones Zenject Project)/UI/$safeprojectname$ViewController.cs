namespace $safeprojectname$.UI
{
    internal class $safeprojectname$SettingsViewController : IInitializable, IDisposable, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged; // Use this to notify BSML of a UI Value change;
                                                                  // PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name of the Method)));
        private readonly PluginConfig _config;

        public $safeprojectname$ViewController(PluginConfig config)
        {
            _config = config;
        }

        public void Initialize()
        {
            BSMLSettings.instance.AddSettingsMenu("Name of your Mod", "Path.To.BSML.File.bsml", this);
        }

        public void Dispose()
        {
            if (BSMLSettings.instance != null) BSMLSettings.instance.RemoveSettingsMenu("Name of your Mod");
        }
    }
}
