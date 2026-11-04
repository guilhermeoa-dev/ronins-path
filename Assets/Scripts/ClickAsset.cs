using UnityEngine;
using UnityEngine.SceneManagement;
// Aula jogos02x09, pagina 19: script no asset, Collider2D e acao pela tag.
public class ClickAsset : MonoBehaviour
{
    static int quadroDaTroca = - 1;
    void OnMouseDown()
    {
        if (enabled)
            Acionar(gameObject.tag);
    }
    // Teclado e mouse chamam a mesma acao; cada cena continua usando LoadScene.
    public static bool Acionar(string tagObjeto)
    {
        if (quadroDaTroca == Time.frameCount)
            return false;
        string cena = "";
        if (tagObjeto == "botao_start")
            cena = "FAses";
        if (tagObjeto == "botao_fase1")
            cena = "Cena1";
        if (tagObjeto == "botao_fase2")
            cena = "Cena2";
        if (tagObjeto == "botao_templo")
            cena = "TravessiaDoTemplo";
        if (tagObjeto == "reiniciar" || tagObjeto == "botao_menu")
            cena = "PrimeiraTela";
        if (tagObjeto == "fechar")
        {
            quadroDaTroca = Time.frameCount;
            Application.Quit();
            // Aula 02x08: fecha somente o executavel.
            return true;
        }
        if (cena == "")
            return false;
        quadroDaTroca = Time.frameCount;
        Samurai.ReiniciarPartida();
        SceneManager.LoadScene(cena);
        return true;
    }
}
