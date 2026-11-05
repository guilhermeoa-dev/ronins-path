using UnityEngine;
// Guardiao adicional: usa as folhas originais IDLE/RUN/ATTACK 1/HURT, sem alterar escala.
[RequireComponent(typeof(Rigidbody2D), typeof(AlvoTemplo), typeof(Animator))] public class OldSamurai : MonoBehaviour
{
    public enum Estado
    {
        Patrulha, Perseguindo, Preparando, Atacando, Recuperando, Ferido, Derrotado
    }

    public bool modoMago;
    public int AtaqueExecutado
    {
        get;
        private set;
    }

    public Samurai jogador;
    public GameObject projetil;
    public TextMesh indicador;
    public float distanciaPatrulha = 2.5f, velocidade = 1.5f, deteccao = 5f;
    public float alcanceKatana = 1.55f, aviso = .5f, recuperacao = .8f;
    public Estado estado
    {
        get;
        private set;
    }
    Rigidbody2D corpo;
    Collider2D colisor;
    AlvoTemplo alvo;
    Animator animador;
    SpriteRenderer imagem;
    Vector2 inicio;
    float fimEstado, ladoPatrulha = 1, ladoAtaque = 1;
    bool ataqueDistante, golpeAplicado;
    void Awake()
    {
        corpo = GetComponent<Rigidbody2D>();
        colisor = GetComponent<Collider2D>();
        alvo = GetComponent<AlvoTemplo>();
        animador = GetComponent<Animator>();
        imagem = GetComponent<SpriteRenderer>();
        alvo.AoReceberDano += Ferir;
        alvo.AoDerrotar += Derrotar;
    }

    void Start()
    {
        inicio = corpo.position;
        GameObject personagem = GameObject.FindGameObjectWithTag("Player");
        if (personagem != null)
            jogador = personagem.GetComponent<Samurai>();
        Trocar(Estado.Patrulha, 0, "Parado");
    }

    void OnDestroy()
    {
        if (alvo != null)
        {
            alvo.AoReceberDano -= Ferir;
            alvo.AoDerrotar -= Derrotar;
        }
    }

    Vector2 Centro
    {
        get
        {
            return colisor != null ? (Vector2) colisor.bounds.center : corpo.position + Vector2.up;
        }
    }

    bool Enxerga()
    {
        if (jogador == null || !jogador.modoMissao || jogador.missaoEncerrada)
            return false;

        Vector2 posicaoJogador = jogador.transform.position;
        float distanciaDaPatrulha = Mathf.Abs(posicaoJogador.x - inicio.x);
        return distanciaDaPatrulha <= distanciaPatrulha + deteccao
            && Vector2.Distance(Centro, posicaoJogador) <= deteccao
            && VisaoCombate.Livre(Centro, posicaoJogador, gameObject, jogador);
    }

    void Trocar(Estado novo, float duracao, string animacao)
    {
        if (modoMago && animacao == "Ataque" && !ataqueDistante)
            animacao = "Ataque2";
        estado = novo;
        fimEstado = Time.time + duracao;
        if (animador != null)
        {
            animador.speed = 1;
            animador.SetBool("andando", animacao == "Correndo");
            animador.SetBool("estaAtacando", animacao == "Ataque");
            animador.SetBool("ferido", animacao == "Ferido");
            animador.Update(0);
            // A FSM conserva o aviso e o quadro exato de ataque solicitado para o chefe.
            animador.speed = novo == Estado.Preparando ? 0 : 1;
            animador.Play(animacao, 0, novo == Estado.Preparando ? 1f / 7f : 0);
            animador.Update(0);
        }
    }

    void FixedUpdate()
    {
        if (estado == Estado.Derrotado || jogador == null || jogador.missaoEncerrada)
            return;
        if (estado == Estado.Ferido || estado == Estado.Recuperando)
        {
            if (Time.time >= fimEstado)
                Trocar(Estado.Patrulha, 0, "Parado");
            return;
        }
        if (estado == Estado.Preparando)
        {
            if (Time.time >= fimEstado)
            {
                golpeAplicado = false;
                Trocar(Estado.Atacando, (modoMago ? 8f : 7f) / 12f, "Ataque");
            }
            return;
        }
        if (estado == Estado.Atacando)
        {
            // O golpe acontece no quarto quadro, depois do aviso e da preparacao da lamina.
            if (!golpeAplicado && Time.time >= fimEstado - 4f / 12f)
            {
                golpeAplicado = true;
                AplicarGolpe();
            }
            if (Time.time >= fimEstado)
                Trocar(Estado.Recuperando, recuperacao, "Parado");
            return;
        }
        if (Enxerga())
        {
            float dx = jogador.transform.position.x - corpo.position.x;
            imagem.flipX = dx < 0;
            float distancia = Vector2.Distance(Centro, jogador.transform.position);
            if (distancia <= alcanceKatana || distancia >= 3.2f)
            {
                ataqueDistante = distancia > alcanceKatana;
                ladoAtaque = imagem.flipX ? - 1 : 1;
                Trocar(Estado.Preparando, aviso, "Ataque");
            }
            else
            {
                if (estado != Estado.Perseguindo)
                    Trocar(Estado.Perseguindo, 0, "Correndo");
                Mover(Mathf.Clamp(jogador.transform.position.x, inicio.x - distanciaPatrulha, inicio.x + distanciaPatrulha));
            }
        }
        else
        {
            if (estado != Estado.Patrulha)
                Trocar(Estado.Patrulha, 0, "Correndo");
            float destino = inicio.x + ladoPatrulha * distanciaPatrulha;
            if (Mathf.Abs(corpo.position.x - destino) < .08f)
                ladoPatrulha *= - 1;
            Mover(inicio.x + ladoPatrulha * distanciaPatrulha);
        }
    }

    void Mover(float x)
    {
        Vector2 proxima = new Vector2(Mathf.MoveTowards(corpo.position.x, x, velocidade * Time.fixedDeltaTime), inicio.y);
        float dx = proxima.x - corpo.position.x;
        if (Mathf.Abs(dx) < .001f)
            return;
        imagem.flipX = dx < 0;
        // Colisor fisico cinemático: nunca teleporta ao retomar a patrulha.
        RaycastHit2D[] impactos = Physics2D.BoxCastAll(Centro, colisor.bounds.size, 0, new Vector2(dx, 0), Mathf.Abs(dx));
        foreach (RaycastHit2D hit in impactos)
            if (hit.collider != null && !hit.collider.isTrigger && hit.collider != colisor && hit.collider.bounds.max.y > colisor.bounds.min.y + .1f)
                return;
        corpo.MovePosition(proxima);
        if (estado == Estado.Patrulha)
        {
            animador.SetBool("andando", true);
            animador.SetBool("estaAtacando", false);
            animador.SetBool("ferido", false);
        }
    }

    void AplicarGolpe()
    {
        if (!Enxerga())
            return;
        AtaqueExecutado = ataqueDistante ? 1 : 2;
        SomRonin.Tocar(ataqueDistante ? "magia" : "katana", transform.position);
        if (ataqueDistante)
        {
            if (projetil == null)
                return;
            Vector2 posicao = Centro + Vector2.right * ladoAtaque * .7f;
            if (!VisaoCombate.Livre(Centro, posicao, gameObject, jogador))
                return;
            GameObject tiro = Instantiate(projetil, posicao, Quaternion.identity);
            Rigidbody2D rb = tiro.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.velocity = Vector2.right * ladoAtaque * 4.5f;
        }
        else
        {
            Collider2D corpoJogador = jogador.GetComponent<Collider2D>();
            Vector2 centroGolpe = Centro + Vector2.right * ladoAtaque * .85f;
            foreach (Collider2D c in Physics2D.OverlapBoxAll(centroGolpe, new Vector2(1.2f, 1.3f), 0))
                if (c.CompareTag("Player") && c == corpoJogador)
            {
                jogador.ReceberDanoMissao(1, false);
                break;
            }
        }
    }

    void Ferir()
    {
        Trocar(Estado.Ferido, .32f, "Ferido");
    }

    void Derrotar()
    {
        corpo.velocity = Vector2.zero;
        Trocar(Estado.Derrotado, 0, modoMago ? "Morte" : "Ferido");
        if (modoMago && jogador != null)
            jogador.VencerMago(alvo);
    }

    void OnDrawGizmosSelected()
    {
        Collider2D c = GetComponent<Collider2D>();
        Vector3 centro = c != null ? c.bounds.center : transform.position + Vector3.up;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(centro, deteccao);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(centro, alcanceKatana);
        Vector3 origem = Application.isPlaying ? (Vector3) inicio : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origem - Vector3.right * distanciaPatrulha, origem + Vector3.right * distanciaPatrulha);
        if (jogador != null)
        {
            Gizmos.color = estado == Estado.Perseguindo ? Color.green : Color.gray;
            Gizmos.DrawLine(centro, jogador.transform.position);
        }
    }
    // O aviso visual usa a preparacao do golpe, sem legendas sobre o personagem.
}
