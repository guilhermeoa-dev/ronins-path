// Ferramenta adicionada durante a recuperacao. Nao altera nem salva cenas.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class SamuraiBackupValidation
{
    [Serializable] public class Finding
    {
        public string scene;
        public string objectPath;
        public string issue;
        public string detail;
    }
    [Serializable] public class Result
    {
        public string unityVersion;
        public int sceneCount;
        public int gameObjectCount;
        public List<Finding> findings = new List<Finding>();
        public List<string> externalGuidResolution = new List<string>();
    }

    [MenuItem("Tools/Samurai Boy/Validar backup sem salvar cenas")]
    public static void Run()
    {
        bool batchMode = Array.IndexOf(Environment.GetCommandLineArgs(), "-batchmode") >= 0;
        if (!batchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Result result = new Result();
        result.unityVersion = Application.unityVersion;
        SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
            {
                if (!buildScene.enabled) continue;
                if (!File.Exists(buildScene.path))
                {
                    result.findings.Add(new Finding { scene = buildScene.path, issue = "scene-file-missing" });
                    continue;
                }
                Scene scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
                result.sceneCount++;
                foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform tr in root.GetComponentsInChildren<Transform>(true))
                {
                    result.gameObjectCount++;
                    string path = tr.name;
                    for (Transform parent = tr.parent; parent != null; parent = parent.parent) path = parent.name + "/" + path;
                    foreach (Component component in tr.GetComponents<Component>())
                    {
                        if (component == null)
                        {
                            result.findings.Add(new Finding { scene = buildScene.path, objectPath = path, issue = "missing-script" });
                            continue;
                        }
                        Samurai samurai = component as Samurai;
                        if (samurai != null && samurai.enabled &&
                            (samurai.GetComponent<Rigidbody2D>() == null || samurai.GetComponent<Animator>() == null ||
                             samurai.GetComponent<Collider2D>() == null || samurai.GetComponent<SpriteRenderer>() == null ||
                             samurai.UITextMoedas == null || samurai.UITextEspadas == null ||
                             samurai.UITextSkates == null || samurai.UITextVidas == null))
                            result.findings.Add(new Finding { scene = buildScene.path, objectPath = path,
                                issue = "samurai-dependencies-missing", detail = "Componente ativo sem fisica, animacao ou campos do HUD." });
                        SpriteRenderer sprite = component as SpriteRenderer;
                        if (sprite != null && sprite.sprite == null)
                            result.findings.Add(new Finding { scene = buildScene.path, objectPath = path, issue = "sprite-null" });
                    }
                }
            }
            foreach (string guid in new string[] { "f5f67c52d1564df4a8936ccd202a3bd8", "f70555f144d8491a825f0804e09c671c",
                "795851dc9df113b4fbdd0b4b1964138c", "53fee11b0c2a7b04fb9eeda9cc74b790",
                "7e102455d0ebca3438c5669c812f1327", "d9331ba2cb2bd3b42b5bb5b4bd29cac1", "dd0f1a32852df4442b359fb0895fc984" })
                result.externalGuidResolution.Add(guid + " -> " + AssetDatabase.GUIDToAssetPath(guid));
        }
        finally
        {
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Diagnostico/unity_backup_validacao.json"));
            File.WriteAllText(output, JsonUtility.ToJson(result, true));
            Debug.Log("SAMURAI_BACKUP_AUDIT " + output + " scenes=" + result.sceneCount + " findings=" + result.findings.Count);
            if (!batchMode) EditorSceneManager.RestoreSceneManagerSetup(previous);
        }
    }
}
