using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class SamuraiRecoveryPlayTests
{
    [MenuItem("Tools/Samurai Boy/Testar jogo em Play Mode %#F9")]
    public static void Start()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        File.WriteAllText("Library/RecoveryPlayTest.request", "explicit-test");
        EditorSceneManager.OpenScene("Assets/PrimeiraTela.unity");
        EditorApplication.isPlaying = true;
    }
}
