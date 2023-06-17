using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class CommandLineRunner : MonoBehaviour
{
    public static void StartGame()
    {
        Time.fixedDeltaTime = 0.005f;
        // Set the desired time scale (1 for normal speed)
        Time.timeScale = 1;

        // Load the desired scene by name or index
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity"); // TODO replace with desired start scene
        EditorApplication.EnterPlaymode();
    }
}
