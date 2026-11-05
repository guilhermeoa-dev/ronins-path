using UnityEngine;

public class SelecFases : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
            ClickAsset.Acionar("botao_fase1");

        if (Input.GetKeyDown(KeyCode.J))
            ClickAsset.Acionar("botao_fase2");

        if (Input.GetKeyDown(KeyCode.L))
            ClickAsset.Acionar("botao_menu");
    }
}
