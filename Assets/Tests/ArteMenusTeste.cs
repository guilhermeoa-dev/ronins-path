#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ArteMenusTeste : MonoBehaviour {
    [System.Serializable]public class Relatorio {public List<string> aprovados=new List<string>();public List<string> falhas=new List<string>();}
    Relatorio r=new Relatorio();string pasta;float inicio;bool fim;
    public static void Testar(){EditorSceneManager.OpenScene("Assets/PrimeiraTela.unity");File.WriteAllText("Library/ArteMenus.request","1");EditorApplication.isPlaying=true;}
    [RuntimeInitializeOnLoadMethod]static void Iniciar(){if(!File.Exists("Library/ArteMenus.request"))return;File.Delete("Library/ArteMenus.request");var go=new GameObject("ARTE_TEST_ONLY");DontDestroyOnLoad(go);go.AddComponent<ArteMenusTeste>();}
    void Start(){pasta=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Diagnostico/Arte03Nov"));inicio=Time.realtimeSinceStartup;Application.logMessageReceived+=Log;StartCoroutine(Executar());}
    void Log(string texto,string stack,LogType tipo){if(tipo==LogType.Error||tipo==LogType.Exception)r.falhas.Add(texto);}
    void Check(string nome,bool ok){if(ok)r.aprovados.Add(nome);else r.falhas.Add(nome);}
    void Update(){if(!fim && Time.realtimeSinceStartup-inicio>65){r.falhas.Add("Timeout");Finalizar();}}
    void Finalizar(){fim=true;Application.logMessageReceived-=Log;File.WriteAllText(pasta+"/testes_arte.json",JsonUtility.ToJson(r,true));EditorApplication.Exit(r.falhas.Count==0?0:1);}
    void Foto(string nome,int w,int h){
        Camera c=Camera.main;RenderTexture antes=c.targetTexture,ativa=RenderTexture.active;
        RenderTexture rt=new RenderTexture(w,h,24);Texture2D t=new Texture2D(w,h,TextureFormat.RGB24,false);c.targetTexture=rt;
        c.Render();Canvas.ForceUpdateCanvases();FindObjectOfType<MenuPintado>().Atualizar();c.Render();RenderTexture.active=rt;t.ReadPixels(new Rect(0,0,w,h),0,0);t.Apply();File.WriteAllBytes(pasta+"/"+nome+".png",t.EncodeToPNG());
        c.targetTexture=antes;RenderTexture.active=ativa;Destroy(t);Destroy(rt);
    }
    IEnumerator Executar(){
        foreach(string cena in new[]{"PrimeiraTela","FAses","Vitoria1","Vitoria2","Vitoria3","GameOver"}){
            SceneManager.LoadScene(cena);yield return new WaitForSeconds(.25f);
            MenuPintado m=FindObjectOfType<MenuPintado>();Check(cena+" usa pintura nova",m!=null&&m.GetComponentInChildren<RawImage>().texture!=null);
            foreach(Text texto in m.GetComponentsInChildren<Text>()) if(texto.name!="Selecao") Check(cena+" texto visivel: "+texto.name,texto.cachedTextGenerator.characterCountVisible>0);
            Check(cena+" sem artes antigas sobrepostas",FindObjectsOfType<SpriteRenderer>().All(v=>!v.enabled));
            foreach(var o in m.opcoes){
                Vector3 pos=m.cameraMenu.WorldToScreenPoint(o.colisor.bounds.center);var hit=Physics2D.GetRayIntersection(m.cameraMenu.ScreenPointToRay(pos));
                Check(cena+" clique alcança "+o.colisor.tag,hit.collider==o.colisor);
            }
            Foto(cena+"_1280x720",1280,720);yield return null;Foto(cena+"_640x480",640,480);
        }
        Finalizar();
    }
}
#endif

