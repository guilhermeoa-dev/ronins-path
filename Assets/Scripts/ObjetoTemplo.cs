using UnityEngine;
// Aulas 03x01/06: caixa transportavel e toque indireto identificado por tags.
public class ObjetoTemplo : MonoBehaviour
{
    // Campos serializados conservados para nao romper cenas, ferramentas e referencias antigas.
    public enum Tipo
    {
        CaixaMovel, BonusVento, Selo, Portal, Pergaminho
    }

    public Tipo tipo;
    public bool carregado;
    public GameObject portao;
    public Plataforma plataforma;
    public Transform destino;
    bool usado;
    Vector3 posicaoInicial;
    void Start()
    {
        posicaoInicial = transform.position;
    }

    void Update()
    {
        if (CompareTag("mover") && !carregado && transform.position.y < - 5)
        {
            transform.position = posicaoInicial;
            Rigidbody2D corpo = GetComponent<Rigidbody2D>();
            if (corpo != null)
            {
                corpo.velocity = Vector2.zero;
                corpo.angularVelocity = 0;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D objeto)
    {
        VerificarToqueIndireto(objeto);
    }

    void OnTriggerStay2D(Collider2D objeto)
    {
        VerificarToqueIndireto(objeto);
    }

    void VerificarToqueIndireto(Collider2D objeto)
    {
        if (usado || !CompareTag("armadilha01") || !objeto.CompareTag("mover"))
            return;
        ObjetoTemplo caixa = objeto.GetComponent<ObjetoTemplo>();
        if (caixa == null || caixa.carregado)
            return;
        usado = true;
        // O objeto tocado altera outros objetos encontrados pelas tags (03x06).
        GameObject grade = GameObject.FindGameObjectWithTag("gradeTemplo");
        if (grade != null)
            grade.SetActive(false);
        foreach (GameObject ponte in GameObject.FindGameObjectsWithTag("Plataforma"))
        {
            Plataforma movimento = ponte.GetComponent<Plataforma>();
            if (movimento != null)
                movimento.ativada = true;
        }
        GameObject personagem = GameObject.FindGameObjectWithTag("Player");
        Samurai jogador = personagem != null ? personagem.GetComponent<Samurai>() : null;
        if (jogador != null && jogador.modoMissao)
            jogador.AtivarPonte();
        SpriteRenderer imagem = GetComponent<SpriteRenderer>();
        if (imagem != null)
            imagem.color = Color.green;
    }
}
