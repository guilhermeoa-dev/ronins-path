using UnityEngine;
// Aulas 03x02 (conteudo nos blocos), 03x04 e 04x04 (dano/destruicao).
public class AlvoTemplo : MonoBehaviour
{
    public int vida = 2;
    public GameObject premio;
    public float atrasoDestruicao;
    public bool Derrotado
    {
        get
        {
            return destruido;
        }
    }

    public event System.Action AoReceberDano;
    public event System.Action AoDerrotar;
    bool destruido;
    SpriteRenderer imagem;
    Color corOriginal;
    float fimFeedback, morteEm;
    // Aulas 03x02/04 e 04x04: a tag identifica o alvo antes de acessar sua vida.
    public static AlvoTemplo Identificar(Collider2D objeto)
    {
        if (objeto == null)
            return null;
        string tag = objeto.gameObject.tag;
        if (tag.Contains("inimigo") || tag.Contains("destrutivel") || tag == "Limbo")
            return objeto.GetComponentInParent<AlvoTemplo>();
        return null;
    }

    void Awake()
    {
        imagem = GetComponent<SpriteRenderer>();
        if (imagem != null)
            corOriginal = imagem.color;
    }

    void Update()
    {
        if (imagem == null)
            return;
        Color cor = Time.time < fimFeedback ? new Color(1, .35f, .35f, 1) : corOriginal;
        if (destruido && atrasoDestruicao > 0)
            cor.a = corOriginal.a * (1 - Mathf.Clamp01((Time.time - morteEm) / atrasoDestruicao));
        imagem.color = cor;
    }

    public void ReceberDano(int quantidade, Samurai autor)
    {
        if (destruido || quantidade <= 0 || (autor != null && autor.missaoEncerrada))
            return;
        SomRonin.Tocar("impacto", transform.position);
        vida = Mathf.Max(0, vida - quantidade);
        fimFeedback = Time.time + .12f;
        if (vida > 0)
        {
            if (AoReceberDano != null)
                AoReceberDano();
            return;
        }
        destruido = true;
        morteEm = Time.time;
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>())
            c.enabled = false;
        if (premio != null)
        {
            premio.transform.SetParent(null, true);
            premio.SetActive(true);
        }
        if (CompareTag("inimigo01") && autor != null && autor.modoMissao)
            autor.guardasDerrotados++;
        if (AoDerrotar != null)
            AoDerrotar();
        Destroy(gameObject, Mathf.Max(0, atrasoDestruicao));
    }
}
