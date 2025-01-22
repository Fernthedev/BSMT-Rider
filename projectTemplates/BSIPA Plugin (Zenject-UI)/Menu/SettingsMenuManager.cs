using System;
using BeatSaberMarkupLanguage.Settings;
using Zenject;

namespace ZenjectMenuPlugin.Menu;

internal class SettingsMenuManager : IInitializable, IDisposable
{
    private readonly ExampleSettingsMenu exampleSettingsMenu;
    private readonly BSMLSettings bsmlSettings;
        
    private const string MenuName = nameof(ZenjectMenuPlugin);
    private const string ResourcePath = nameof(ZenjectMenuPlugin) + ".Menu.example.bsml";
#if (EnableHints)
    // Zenject will inject our ExampleSettingsMenu instance on this object's creation.
    // BSMLSettings is bound by BSML. SiraUtil also lets us inject services from other mods.
#endif
    public SettingsMenuManager(ExampleSettingsMenu exampleSettingsMenu, BSMLSettings bsmlSettings)
    {
        this.exampleSettingsMenu = exampleSettingsMenu;
        this.bsmlSettings = bsmlSettings;
    }
#if (EnableHints)
    // Zenject will call IInitializable.Initialize for any menu bindings when the main menu loads for the first
    // time or when the game restarts internally, such as when settings are applied.
#endif
    public void Initialize()
    {
#if (EnableHints)
        // Adds a custom menu in the Mod Settings section of the main menu.
#endif
        bsmlSettings.AddSettingsMenu(MenuName, ResourcePath, exampleSettingsMenu);
    }
#if (EnableHints)
    // Zenject will call IDisposable.Dispose for any menu bindings when the menu scene unloads.
#endif
    public void Dispose()
    {
        bsmlSettings.RemoveSettingsMenu(exampleSettingsMenu);
    }
}