using BeatSaberMarkupLanguage.Attributes;
using HMUI;
using TMPro;
using UnityEngine;

namespace MenuPlugin.Menu;
#if (EnableHints)
// This class is for managing the BSML resource and interact with the UI object in game.
// To see more about components read the BSML docs. https://monkeymanboy.github.io/BSML-Docs/
#endif
internal class ExampleSettingsMenu : MonoBehaviour
{
#if (EnableHints)
    // [UIComponent] will get a specified component from the BSML object with the matching id
#endif
    [UIComponent("example-image")]
    private readonly ImageView exampleImage = null!;

    [UIComponent("example-text")] 
    private readonly TextMeshProUGUI exampleText = null!;

#if (EnableHints)
    // The #post-parse event is provided by BSML. This action is invoked after BSML has parsed this object and
    // all [UIComponent] and [UIObject] members have been populated. Any initialization logic for the menu should
    // be done in here, rather than in Unity's Awake or Start events.
#endif
    [UIAction("#post-parse")]
    private void PostParse()
    {
        Plugin.Log.Debug($"{name} parsed");
    }
#if (EnableHints)
    // [UIAction] will be used by UI elements like buttons to invoke methods such as the one here.
#endif
    [UIAction("example-action")]
    private void ExampleAction()
    {
        exampleImage.color = Color.white;
        exampleText.text = "Hello World!";
    }
}