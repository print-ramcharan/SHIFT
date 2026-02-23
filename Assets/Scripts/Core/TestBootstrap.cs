using UnityEngine;

/// <summary>
/// Auto-starts the game when the test scene loads.
/// Attach this to any GameObject in the test scene (SceneBuilder adds it automatically).
/// Remove this from production scenes — Bootstrap handles startup there.
/// </summary>
public class TestBootstrap : MonoBehaviour
{
    private void Start()
    {
        // Give managers one frame to initialise, then start
        Invoke(nameof(AutoStart), 0.1f);
    }

    private void AutoStart()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
            Debug.Log("[TestBootstrap] Game auto-started for testing.");
        }
        else
        {
            Debug.LogWarning("[TestBootstrap] GameManager not found — add it to the scene.");
        }
    }
}
