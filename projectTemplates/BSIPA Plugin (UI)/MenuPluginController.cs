using BeatSaberMarkupLanguage.Util;
using MenuPlugin.Menu;
using UnityEngine;

namespace MenuPlugin;
#if (EnableHints)
// MonoBehaviours are scripts added to in-game GameObjects which execute code during runtime.
// For a full list of Messages a MonoBehaviour can receive from the game, refer to the Unity documentation.
// https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
#endif
internal class MenuPluginController : MonoBehaviour
{
#if (Nullable)
    private static MenuPluginController? Instance { get; set; }
#else
    private static MenuPluginController Instance { get; set; }
#endif
#if (EnableHints)
    /// <summary>
    /// Called a single time by Unity when this script is created.
    /// </summary>
#endif
    private void Awake()
    {
#if (EnableHints)
        // For this particular MonoBehaviour, we only want one instance to exist at any time.
        // Store a reference to it in a static property and destroy any that are created while one already exists.
#endif
        if (Instance != null)
        {
            DestroyImmediate(this);
            return;
        }

        DontDestroyOnLoad(this); // Don't destroy this object on scene changes
        Instance = this;
#if (EnableHints)
        // Invoked when the main menu scene loads, or loads fresh after applying settings.
        // This is important for initializing objects in the menu, namely UI objects.
#endif
        MainMenuAwaiter.MainMenuInitializing += SettingsMenuManager.AddSettingsMenu;
    }
#if (EnableHints)
    /// <summary>
    /// Called a single time by Unity on the first frame the script is enabled.
    /// </summary>
#endif
    private void Start()
    {
        Plugin.Log.Debug($"{name} started");
    }
#if (EnableHints)
    /// <summary>
    /// Called a single time by Unity when the script is being destroyed.
    /// </summary>
#endif
    private void OnDestroy()
    {
        Plugin.Log.Debug($"{name} destroyed");
        MainMenuAwaiter.MainMenuInitializing -= SettingsMenuManager.AddSettingsMenu;
            
        if (Instance == this)
        {
#if (EnableHints)
            // This MonoBehaviour is being destroyed, so set the static instance property to null.
#endif
            Instance = null;
        }
    }
}