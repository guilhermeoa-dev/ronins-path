using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class SelecFases : MonoBehaviour
{
    bool ApertoK;
    bool ApertoJ;
    bool ApertoL;
    // Use this for initialization
    void Start()
    {
        ApertoK = false;
        ApertoJ = false;
        ApertoL = false;
    }
    // Update is called once per frame
    void Update()
    {
        ApertoK = Input.GetKeyDown(KeyCode.K);
        if (ApertoK == true)
        {
            ClickAsset.Acionar("botao_fase1");
        }
        ApertoJ = Input.GetKeyDown(KeyCode.J);
        if (ApertoJ == true)
        {
            ClickAsset.Acionar("botao_fase2");
        }
        ApertoL = Input.GetKeyDown(KeyCode.L);
        if (ApertoL == true)
        {
            ClickAsset.Acionar("botao_menu");
        }
    }
}
