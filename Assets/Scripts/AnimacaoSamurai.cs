using UnityEngine;
// Quadros completos com o mesmo pivo: o corpo e a katana nao saltam entre recortes.
[DefaultExecutionOrder(200)][RequireComponent(typeof(Samurai), typeof(Animator), typeof(Rigidbody2D))] public class AnimacaoSamurai : MonoBehaviour
{
    Samurai jogador;
    Animator animador;
    Rigidbody2D corpo;
    Collider2D colisor;
    string estado;
    int ultimoPulo;
    public bool NoChao
    {
        get;
        private set;
    }

    public bool PuloPausado
    {
        get;
        private set;
    }

    public string EstadoAtual
    {
        get
        {
            return estado;
        }
    }

    void Start()
    {
        jogador = GetComponent<Samurai>();
        animador = GetComponent<Animator>();
        corpo = GetComponent<Rigidbody2D>();
        colisor = GetComponent<Collider2D>();
    }

    public bool VerificarChao()
    {
        if (colisor == null || corpo.velocity.y > .15f)
            return false;
        Bounds b = colisor.bounds;
        Vector2 origem = new Vector2(b.center.x, b.min.y + .04f);
        foreach (RaycastHit2D hit in Physics2D.BoxCastAll(origem, new Vector2(b.size.x * .8f, .025f), 0, Vector2.down, .08f))
            if (hit.collider != null && !hit.collider.isTrigger && hit.collider.GetComponentInParent<Samurai>() != jogador && hit.normal.y > .5f)
                return true;
        return false;
    }

    void LateUpdate()
    {
        NoChao = VerificarChao();
        bool novoPulo = ultimoPulo != jogador.SequenciaPulo;
        ultimoPulo = jogador.SequenciaPulo;
        string proximo = jogador.ataqueAtivo ? "Atacando" : jogador.defesaAtiva ? "Defendendo" : jogador.dashAtivo ? "Dash" : !NoChao ? "Pulando" : Mathf.Abs(corpo.velocity.x) > .1f ? (jogador.correndo ? "Correndo" : "Andando") : "Parado";
        bool mudou = estado != proximo;
        // Aulas 03x05 e 04x04: os bools comandam transicoes sem Has Exit Time.
        animador.speed = 1;
        animador.SetBool("andando", proximo == "Andando");
        animador.SetBool("correndo", proximo == "Correndo");
        animador.SetBool("pulando", proximo == "Pulando");
        animador.SetBool("estaAtacando", proximo == "Atacando");
        animador.SetBool("defendendo", proximo == "Defendendo");
        animador.SetBool("dash", proximo == "Dash");
        if (mudou)
        {
            estado = proximo;
            animador.Update(0);
        }
        // Extensoes do jogo: reiniciar apenas um NOVO pulo, ou usar a pose de queda
        // ao sair de uma beirada. A descida de um pulo normal nunca o reinicia.
        if (proximo == "Pulando" && ((novoPulo && !mudou) || (mudou && corpo.velocity.y <= 0)))
        {
            animador.Play("Pulando", 0, novoPulo ? 0 : .9f);
            animador.Update(0);
        }
        PuloPausado = proximo == "Pulando" && corpo.velocity.y <= 0;
        animador.speed = PuloPausado ? 0 : (proximo == "Dash" ? 1.6f : 1);
    }

    void OnDisable()
    {
        if (animador != null)
            animador.speed = 1;
    }
}
