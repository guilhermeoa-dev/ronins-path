using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class TryAgain : MonoBehaviour
{
    bool TenteNovamente;
    bool ApertoUK;
    // Use this for initialization
    void Start()
    {
        TenteNovamente = false;
        ApertoUK = false;
    }
    // Update is called once per frame
    void Update()
    {
        ApertoUK = Input.GetKeyDown(KeyCode.K);
        if (ApertoUK == true)
        {
            TenteNovamente = true;
        }
        if (TenteNovamente == true)
        {
            ClickAsset.Acionar("reiniciar");
        }
    }
}
