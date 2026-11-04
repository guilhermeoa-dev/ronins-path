using UnityEngine;
// Aula 04x01: modelo por tag, Instantiate, Rigidbody2D, colisao e tempo de vida.
public class ProjetilTemplo : MonoBehaviour
{
    public int tipo, dano = 1;
    public bool hostil;
    public bool modelo;
    public Samurai dono;
    bool consumido;
    void Start()
    {
        if (!modelo)
            Invoke("Expirar", 3.5f);
    }

    public void PrepararDisparo(Samurai jogador)
    {
        modelo = false;
        dono = jogador;
        // Apenas o modelo conserva projetil1/2/3; a busca nao seleciona copias em voo.
        gameObject.tag = "disparoJogador";
        GetComponent<Rigidbody2D>().simulated = true;
        foreach (Collider2D colisor in GetComponentsInChildren<Collider2D>())
            colisor.enabled = true;
        foreach (SpriteRenderer imagem in GetComponentsInChildren<SpriteRenderer>())
            imagem.enabled = true;
    }

    void Expirar()
    {
        if (tipo == 2 && !hostil)
            Explodir();
        else Destroy(gameObject);
    }
    // Prefabs atuais usam triggers; tambem aceita um collider fisico.
    void OnTriggerEnter2D(Collider2D objeto)
    {
        VerificarColisao(objeto);
    }

    void OnCollisionEnter2D(Collision2D objeto)
    {
        VerificarColisao(objeto.collider);
    }

    void VerificarColisao(Collider2D objeto)
    {
        if (modelo || consumido)
            return;
        string tag = objeto.gameObject.tag;
        if (tag == "areaAtaqueEsquerda" || tag == "areaAtaqueDireita")
            return;
        if (hostil)
        {
            if (tag == "Player")
            {
                Samurai jogador = objeto.GetComponent<Samurai>();
                if (jogador != null)
                    jogador.ReceberDanoMissao(dano, true);
                consumido = true;
                Destroy(gameObject);
            }
            else if (!objeto.isTrigger && !tag.Contains("inimigo"))
            {
                consumido = true;
                Destroy(gameObject);
            }
            return;
        }
        if (tag == "Player" || tag == "disparoJogador" || tag == "disparoInimigo" || tag.StartsWith("projetil"))
            return;
        AlvoTemplo alvo = AlvoTemplo.Identificar(objeto);
        if (alvo != null || !objeto.isTrigger)
        {
            if (tipo == 2)
                Explodir();
            else
            {
                consumido = true;
                if (alvo != null)
                    alvo.ReceberDano(dano, dono);
                Destroy(gameObject);
            }
        }
    }

    void Explodir()
    {
        if (modelo || consumido)
            return;
        consumido = true;
        SomRonin.Tocar("explosao", transform.position);
        System.Collections.Generic.HashSet<int> atingidos = new System.Collections.Generic.HashSet<int>();
        foreach (Collider2D colisor in Physics2D.OverlapCircleAll(transform.position, 1.8f))
        {
            AlvoTemplo alvo = AlvoTemplo.Identificar(colisor);
            if (alvo != null && atingidos.Add(alvo.GetInstanceID()))
                alvo.ReceberDano(dano, dono);
        }
        Destroy(gameObject);
    }
}
