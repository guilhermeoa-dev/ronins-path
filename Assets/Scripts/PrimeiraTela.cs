using UnityEngine;
using UnityEngine.SceneManagement;
public class PrimeiraTela : MonoBehaviour
{
    bool ApertoUK;
    bool ApertouL;
    void Start()
    {
        Samurai.ReiniciarPartida();
        ApertoUK = false;
        ApertouL = false;
    }

    void Update()
    {
        // Detecta as teclas
        ApertoUK = Input.GetKeyDown(KeyCode.K);
        ApertouL = Input.GetKeyDown(KeyCode.L);
        // Se apertar K, carrega a cena
        if (ApertoUK == true)
        {
            ClickAsset.Acionar("botao_start");
        }
        // Se apertar L, fecha o jogo
        if (ApertouL == true)
        {
            ClickAsset.Acionar("fechar");
        }
    }
}
