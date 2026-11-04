using UnityEngine;
// Nova implementacao, nao fonte original recuperada.
// Usa tags e OnTriggerEnter2D como nas aulas do Prof. Helio Esperidiao.
// A pontuacao continua centralizada no Samurai para nao contar duas vezes.
public class ColetavelRestaurado : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D outro)
    {
        if (!enabled || outro == null || !outro.CompareTag("Player"))
            return;
        Samurai jogador = outro.GetComponent<Samurai>();
        Collider2D meuColisor = GetComponent<Collider2D>();
        if (jogador != null && jogador.enabled && meuColisor != null)
            jogador.ReceberColetavel(meuColisor);
    }
}
