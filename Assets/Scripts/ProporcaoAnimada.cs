using UnityEngine;
// Recortes animados podem ter larguras diferentes; cada quadro mantem seus pixels quadrados.
[DefaultExecutionOrder(250)][RequireComponent(typeof(SpriteRenderer))] public class ProporcaoAnimada : MonoBehaviour
{
    public float escalaVisual = 1;
    SpriteRenderer imagem;
    void Awake()
    {
        imagem = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (imagem == null || imagem.sprite == null)
            return;
        Vector3 escala = transform.lossyScale;
        if (Mathf.Abs(escala.x * escala.y) < .00001f)
            return;
        Vector2 nativo = imagem.sprite.rect.size / imagem.sprite.pixelsPerUnit;
        imagem.size = new Vector2(nativo.x * escalaVisual / Mathf.Abs(escala.x), nativo.y * escalaVisual / Mathf.Abs(escala.y));
    }
}
