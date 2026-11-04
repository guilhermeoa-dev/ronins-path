using UnityEngine;
using UnityEngine.SceneManagement;
// Navegacao por tecla como PrimeiraTela/SelecFases do backup.
public class EntradaTemplo : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
            ClickAsset.Acionar("botao_templo");
    }
}
