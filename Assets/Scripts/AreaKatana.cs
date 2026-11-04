using UnityEngine;
// Aula 04x04: areas laterais de ataque com trigger.
public class AreaKatana : MonoBehaviour
{
    public Samurai jogador;
    void OnTriggerEnter2D(Collider2D c)
    {
        Tocar(c);
    }

    void OnTriggerStay2D(Collider2D c)
    {
        Tocar(c);
    }

    void Tocar(Collider2D c)
    {
        if (jogador != null)
            jogador.AcertarKatana(AlvoTemplo.Identificar(c));
    }
}
