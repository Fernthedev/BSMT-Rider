using BeatSaberMarkupLanguage.Settings;
using UnityEngine;

namespace MenuPlugin.Menu;

internal static class SettingsMenuManager
{
#if (Nullable)
    private static ExampleSettingsMenu? Instance { get; set; }
#else
    private static ExampleSettingsMenu Instance { get; set; }  
#endif

    private const string MenuName = nameof(MenuPlugin);
    private const string ResourcePath = nameof(MenuPlugin) + ".Menu.example.bsml";
#if (EnableHints)
    /// <summary>
    /// Adds a custom menu in the Mod Settings section of the main menu.
    /// This should only be called when the main menu is active.
    /// </summary>
#endif
    public static void AddSettingsMenu()
    {
        if (Instance == null)
        {
            Instance = new GameObject(nameof(ExampleSettingsMenu)).AddComponent<ExampleSettingsMenu>();
            Object.DontDestroyOnLoad(Instance.gameObject);
        }
            
        RemoveSettingsMenu();
            
        BSMLSettings.Instance.AddSettingsMenu(MenuName, ResourcePath, Instance);
    }

    public static void RemoveSettingsMenu()
    {
        BSMLSettings.Instance.RemoveSettingsMenu(Instance);
    }
}