using UnityEngine;

public class TryAgain : MonoBehaviour
{
    bool reinicioSolicitado;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
            reinicioSolicitado = true;

        if (reinicioSolicitado)
            ClickAsset.Acionar("reiniciar");
    }
}
