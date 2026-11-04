using UnityEngine;
// Ajuste novo de apresentacao: preserva e enquadra os sprites existentes.
[RequireComponent(typeof(Camera))] public class EnquadrarMenu : MonoBehaviour
{
    Camera cameraMenu;
    SpriteRenderer[] imagens;
    void Awake()
    {
        cameraMenu = GetComponent<Camera>();
        imagens = FindObjectsOfType<SpriteRenderer>();
    }

    void OnPreCull()
    {
        bool encontrou = false;
        Bounds area = new Bounds();
        foreach (SpriteRenderer imagem in imagens)
        {
            if (imagem == null || !imagem.enabled || !imagem.gameObject.activeInHierarchy || imagem.sprite == null)
                continue;
            if (!encontrou)
            {
                area = imagem.bounds;
                encontrou = true;
            }
            else area.Encapsulate(imagem.bounds);
        }
        if (!encontrou)
            return;
        cameraMenu.orthographicSize = Mathf.Max(area.extents.y, area.extents.x / cameraMenu.aspect) * 1.02f;
        transform.position = new Vector3(area.center.x, area.center.y, transform.position.z);
    }
}
