using UnityEngine;
using UnityEngine.UI;
// Mantem a moldura, o retrato e os tres coletaveis da interface original.
public class HudRestaurado : MonoBehaviour
{
    public Samurai jogador;
    public Sprite retrato, coletaveis;
    public Sprite[] projeteis;
    public Image[] IconesProjeteis
    {
        get
        {
            return iconesProjeteis;
        }
    }
    // Aula 02x07: Canvas e Images ficam montados na cena e ligados pelo Inspector.
    public Image[] iconesProjeteis;
    public Text[] coracoes;
    void Start()
    {
        if (jogador == null)
        {
            GameObject personagem = GameObject.FindGameObjectWithTag("Player");
            if (personagem != null)
                jogador = personagem.GetComponent<Samurai>();
        }
    }

    void LateUpdate()
    {
        if (jogador == null || IconesProjeteis == null)
            return;
        if (coracoes != null)
            for (int i = 0; i < coracoes.Length; i++)
                if (coracoes[i] != null)
                    coracoes[i].color = i < jogador.VidasMissao ? new Color(1, .15f, .2f) : new Color(.25f, .25f, .25f, .8f);
        for (int i = 0; i < IconesProjeteis.Length; i++)
            if (IconesProjeteis[i] != null)
                IconesProjeteis[i].color = i == jogador.tipoProjetil ? new Color(.7f, .7f, .7f, 1) : Color.white;
    }
}
