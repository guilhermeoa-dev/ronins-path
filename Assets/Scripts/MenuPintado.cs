using UnityEngine;
using UnityEngine.UI;
// A imagem e a interface ficam serializadas. Tags e ClickAsset preservam a navegacao das aulas.
[DefaultExecutionOrder(300)] public class MenuPintado : MonoBehaviour
{
    [System.Serializable] public class Opcao
    {
        public RectTransform area;
        public BoxCollider2D colisor;
        public Text texto, marcador;
        public Image destaque;
    }

    public Opcao[] opcoes;
    public Camera cameraMenu;
    int selecionado;
    Vector3 ultimoMouse;
    readonly Vector3[] cantos = new Vector3[4];
    void Start()
    {
        ultimoMouse = Input.mousePosition;
        Atualizar();
    }

    void Update()
    {
        if (opcoes == null || opcoes.Length == 0)
            return;
        if (Input.GetKeyDown(KeyCode.DownArrow))
            selecionado = (selecionado + 1) % opcoes.Length;
        if (Input.GetKeyDown(KeyCode.UpArrow))
            selecionado = (selecionado + opcoes.Length - 1) % opcoes.Length;
        if (Input.mousePosition != ultimoMouse)
        {
            for (int i = 0; i < opcoes.Length; i++)
                if (RectTransformUtility.RectangleContainsScreenPoint(opcoes[i].area, Input.mousePosition, cameraMenu))
                    selecionado = i;
            ultimoMouse = Input.mousePosition;
        }
        if (Input.GetKeyDown(KeyCode.Return))
            ClickAsset.Acionar(opcoes[selecionado].colisor.tag);
    }

    void LateUpdate()
    {
        Atualizar();
    }

    public void Atualizar()
    {
        if (opcoes == null || cameraMenu == null)
            return;
        Canvas.ForceUpdateCanvases();
        for (int i = 0; i < opcoes.Length; i++)
        {
            Opcao o = opcoes[i];
            o.area.GetWorldCorners(cantos);
            o.colisor.transform.position = (cantos[0] + cantos[2]) * .5f;
            Vector3 escala = o.colisor.transform.lossyScale;
            o.colisor.size = new Vector2(Mathf.Abs((cantos[2].x - cantos[0].x) / escala.x), Mathf.Abs((cantos[2].y - cantos[0].y) / escala.y));
            o.colisor.offset = Vector2.zero;
            bool ativo = i == selecionado;
            o.texto.color = ativo ? new Color(1, .92f, .68f) : new Color(.95f, .95f, .89f);
            o.marcador.enabled = ativo;
            o.destaque.enabled = true;
            o.destaque.color = ativo ? new Color(.18f, .16f, .08f, .85f) : new Color(.03f, .05f, .04f, .45f);
        }
    }
}
