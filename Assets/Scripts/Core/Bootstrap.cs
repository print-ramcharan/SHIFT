using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// First script that runs when the app launches.
/// Initialises all singleton systems and then loads the Main Menu scene.
///
/// Attach to a single GameObject in the Bootstrap scene.
/// Bootstrap scene must be Scene Index 0 in Build Settings.
/// </summary>
public class Bootstrap : MonoBehaviour
{
    [Header("Startup Settings")]
    [Tooltip("Seconds to wait (e.g. for splash/logo) before loading Main Menu.")]
    public float splashDuration = 1.5f;

    [Tooltip("Name of the Main Menu scene to load after bootstrap.")]
    public string mainMenuScene = Constants.SCENE_MAIN_MENU;

    private void Awake()
    {
        // Keep this object alive across scenes
        DontDestroyOnLoad(gameObject);

        // Force 60 FPS target on mobile
        Application.targetFrameRate = 60;

        // Prevent screen from sleeping during gameplay
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        Debug.Log("[Bootstrap] App initialised.");
    }

    private IEnumerator Start()
    {
        // Give Firebase (Phase 4) and other async systems a frame to init
        yield return null;

        // Trigger daily seed computation before anything loads
        int seed = DailySeed.ComputeLocalSeed(
            System.DateTime.UtcNow.ToString(Constants.SEED_DATE_FORMAT));
        Debug.Log($"[Bootstrap] Daily seed: {seed}");

        yield return new WaitForSeconds(splashDuration);

        LoadMainMenu();
    }

    private void LoadMainMenu()
    {
        Debug.Log($"[Bootstrap] Loading {mainMenuScene}");
        SceneManager.LoadScene(mainMenuScene);
    }
}
