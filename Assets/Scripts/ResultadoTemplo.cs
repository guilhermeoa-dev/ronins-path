using UnityEngine;
using UnityEngine.SceneManagement;
// Reaproveita as telas originais de resultado, sem acrescentar texto sobre a fase.
public class ResultadoTemplo : MonoBehaviour
{
    Samurai jogador;
    float mostrarEm;
    void Awake()
    {
        jogador = GetComponent<Samurai>();
    }

    void Update()
    {
        if (jogador == null || !jogador.missaoEncerrada)
            return;
        if (mostrarEm == 0)
            mostrarEm = Time.time + 1f;
        if (Time.time >= mostrarEm)
            SceneManager.LoadScene(jogador.venceuMissao ? "Vitoria3" : "GameOver");
    }
}
