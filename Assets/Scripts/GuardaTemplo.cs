using UnityEngine;
// Patrulha e disparo usam o mesmo samurai de cabelo branco encontrado na caverna.
public class GuardaTemplo : MonoBehaviour
{
    public Samurai jogador;
    public GameObject projetil;
    public float distanciaPatrulha = 2, velocidade = 1.2f, alcance = 6;
    Vector3 inicio;
    float proximoTiro, dispararEm, fimFerido, TempoDecorrido;
    bool preparando;
    SpriteRenderer imagem;
    Animator animador;
    AlvoTemplo alvo;
    string animacao;
    void Awake()
    {
        imagem = GetComponent<SpriteRenderer>();
        animador = GetComponent<Animator>();
        alvo = GetComponent<AlvoTemplo>();
        if (alvo != null)
        {
            alvo.AoReceberDano += Ferir;
            alvo.AoDerrotar += Morrer;
        }
    }

    void Start()
    {
        inicio = transform.position;
        GameObject personagem = GameObject.FindGameObjectWithTag("Player");
        if (personagem != null)
            jogador = personagem.GetComponent<Samurai>();
    }

    void OnDestroy()
    {
        if (alvo != null)
        {
            alvo.AoReceberDano -= Ferir;
            alvo.AoDerrotar -= Morrer;
        }
    }

    void Animar(string nome)
    {
        if (animador == null || animacao == nome)
            return;
        animacao = nome;
        animador.SetBool("andando", nome == "Correndo");
        animador.SetBool("estaAtacando", nome == "Ataque");
        animador.SetBool("ferido", nome == "Ferido");
        animador.Update(0);
    }

    void Ferir()
    {
        preparando = false;
        fimFerido = Time.time + .3f;
        animacao = "";
        Animar("Ferido");
    }

    void Morrer()
    {
        preparando = false;
        Animar("Ferido");
    }

    void Update()
    {
        if (alvo != null && alvo.Derrotado)
            return;
        if (jogador == null || jogador.missaoEncerrada || Time.time < fimFerido)
            return;
        float distancia = Vector2.Distance(transform.position, jogador.transform.position);
        Vector3 anterior = transform.position;
        if (distancia < alcance && VisaoCombate.Livre(transform.position, jogador.transform.position, gameObject, jogador))
        {
            if (!preparando)
                imagem.flipX = jogador.transform.position.x < transform.position.x;
            if (distancia > 3 && !preparando)
                SeguirPlayer();
            if (!preparando && Time.time >= proximoTiro && projetil != null)
            {
                preparando = true;
                dispararEm = Time.time + .45f;
                animacao = "";
                Animar("Ataque");
            }
            if (preparando && Time.time >= dispararEm)
            {
                preparando = false;
                proximoTiro = Time.time + 1.8f;
                SomRonin.Tocar("tiro", transform.position);
                float direcao = imagem.flipX ? - 1 : 1;
                GameObject tiro = Instantiate(projetil, transform.position + Vector3.right * direcao, Quaternion.identity);
                tiro.GetComponent<Rigidbody2D>().velocity = new Vector2(direcao * 5, 0);
            }
        }
        else
        {
            preparando = false;
            MovimentoPingPong();
        }
        if (!preparando)
            Animar((transform.position - anterior).sqrMagnitude > .000001f ? "Correndo" : "Parado");
    }
    // Aula 04x03: perseguicao por distancia; fora do alcance, patrulha PingPong.
    void SeguirPlayer()
    {
        Vector3 destino = new Vector3(Mathf.Clamp(jogador.transform.position.x, inicio.x - 1, inicio.x + distanciaPatrulha), inicio.y, inicio.z);
        transform.position = Vector3.MoveTowards(transform.position, destino, velocidade * Time.deltaTime);
        // Conserva a fase da patrulha ao voltar, evitando um teleporte para o inicio.
        float deslocamento = Mathf.Clamp(transform.position.x - inicio.x, 0, distanciaPatrulha);
        TempoDecorrido = imagem.flipX ? 2 * distanciaPatrulha - deslocamento : deslocamento;
    }

    void MovimentoPingPong()
    {
        TempoDecorrido += Time.deltaTime * velocidade;
        float deslocamento = Mathf.PingPong(TempoDecorrido, distanciaPatrulha);
        Vector3 destino = inicio + Vector3.right * deslocamento;
        float anterior = transform.position.x;
        // MoveTowards tambem evita saltos se a perseguicao saiu do trecho original.
        transform.position = Vector3.MoveTowards(transform.position, destino, velocidade * Time.deltaTime);
        imagem.flipX = transform.position.x < anterior;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, alcance);
        Vector3 origem = Application.isPlaying ? inicio : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origem - Vector3.right, origem + Vector3.right * distanciaPatrulha);
        if (jogador != null)
        {
            bool visivel = Vector2.Distance(transform.position, jogador.transform.position) < alcance && VisaoCombate.Livre(transform.position, jogador.transform.position, gameObject, jogador);
            Gizmos.color = visivel ? Color.green : Color.gray;
            Gizmos.DrawLine(transform.position, jogador.transform.position);
        }
    }

    void Tocar(Collider2D c)
    {
        if (alvo != null && alvo.Derrotado)
            return;
        if (!c.CompareTag("Player"))
            return;
        Samurai s = c.GetComponent<Samurai>();
        if (s != null)
            s.ReceberDanoMissao(1, false);
    }

    void OnCollisionStay2D(Collision2D c)
    {
        Tocar(c.collider);
    }

    void OnTriggerStay2D(Collider2D c)
    {
        Tocar(c);
    }
}
