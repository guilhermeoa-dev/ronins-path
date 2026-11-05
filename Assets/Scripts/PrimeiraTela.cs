using UnityEngine;

public class PrimeiraTela : MonoBehaviour
{
    void Start()
    {
        Samurai.ReiniciarPartida();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
            ClickAsset.Acionar("botao_start");

        if (Input.GetKeyDown(KeyCode.L))
            ClickAsset.Acionar("fechar");
    }
}
