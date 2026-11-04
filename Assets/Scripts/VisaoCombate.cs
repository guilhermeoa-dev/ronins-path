using UnityEngine;
// Obstaculos solidos bloqueiam a deteccao e o ataque; areas de ataque nao sao corpos.
public static class VisaoCombate
{
    public static bool Livre(Vector2 origem, Vector2 destino, GameObject emissor, Samurai jogador)
    {
        foreach (RaycastHit2D hit in Physics2D.LinecastAll(origem, destino))
        {
            Collider2D c = hit.collider;
            if (c == null || c.isTrigger || c.transform.IsChildOf(emissor.transform))
                continue;
            if (jogador != null && c.GetComponentInParent<Samurai>() == jogador)
                continue;
            return false;
        }
        return true;
    }
}
