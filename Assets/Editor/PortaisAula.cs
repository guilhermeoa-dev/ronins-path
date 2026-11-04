using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class PortaisAula {
    public static void Aplicar() {
        EditorSceneManager.OpenScene("Assets/Bimestres34/TravessiaDoTemplo.unity");
        GameObject volta=GameObject.FindGameObjectWithTag("portal02");
        if(volta==null)throw new System.Exception("Portal de retorno ausente");
        BoxCollider2D c=volta.GetComponent<BoxCollider2D>();
        if(c==null)c=volta.AddComponent<BoxCollider2D>();
        c.isTrigger=true;c.enabled=true;c.size=new Vector2(.8f/Mathf.Abs(volta.transform.lossyScale.x),1.6f/Mathf.Abs(volta.transform.lossyScale.y));c.offset=Vector2.zero;
                // O mago pode ter sido reposicionado/redimensionado no editor. Seu corpo nao deve entrar no piso.
        OldSamurai mago=UnityEngine.Object.FindObjectOfType<OldSamurai>();
        BoxCollider2D corpo=mago.GetComponent<BoxCollider2D>();
        CompositeCollider2D piso=UnityEngine.Object.FindObjectOfType<CompositeCollider2D>();
        if(piso!=null && corpo.bounds.min.y<piso.bounds.max.y && corpo.bounds.center.y>piso.bounds.max.y) {
            float corte=(piso.bounds.max.y-corpo.bounds.min.y+.01f)/Mathf.Abs(mago.transform.lossyScale.y);
            corpo.size=new Vector2(corpo.size.x,corpo.size.y-corte);
            corpo.offset+=Vector2.up*(corte*.5f);
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
    }
}


