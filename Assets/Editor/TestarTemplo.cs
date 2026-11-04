using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
public static class TestarTemplo {
 [MenuItem("Tools/Samurai Boy/Testar missao do templo")]
 public static void Iniciar() {
  if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
  EditorSceneManager.OpenScene("Assets/Bimestres34/TravessiaDoTemplo.unity");
  File.WriteAllText("Library/TestarTemplo.request","testar");EditorApplication.isPlaying=true;
 }
}
