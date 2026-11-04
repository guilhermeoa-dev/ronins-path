using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;

public static class AuditoriaColisoes
{
    // Unifica somente as pecas do piso do templo; conserva os BoxCollider2D,
    // objetos, arte e escalas. As ilhas e o desfiladeiro continuam separados.
    public static void Aplicar()
    {
        EditorSceneManager.OpenScene("Assets/Bimestres34/TravessiaDoTemplo.unity");
        GameObject raiz = GameObject.Find("Colisao continua do piso");
        if (raiz == null) raiz = new GameObject("Colisao continua do piso");
        Rigidbody2D corpo = raiz.GetComponent<Rigidbody2D>();
        if (corpo == null) corpo = raiz.AddComponent<Rigidbody2D>();
        corpo.bodyType = RigidbodyType2D.Static;
        CompositeCollider2D composto = raiz.GetComponent<CompositeCollider2D>();
        if (composto == null) composto = raiz.AddComponent<CompositeCollider2D>();
        composto.geometryType = CompositeCollider2D.GeometryType.Polygons;
        raiz.tag = "chao";
        foreach (BoxCollider2D piso in Object.FindObjectsOfType<BoxCollider2D>().Where(c => c.name.StartsWith("Piso ") && c.CompareTag("chao") && !c.isTrigger))
        {
            piso.transform.SetParent(raiz.transform, true);
            piso.usedByComposite = true;
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes(); AssetDatabase.SaveAssets();
        AuditoriaCompleta.Validar();
    }
}
