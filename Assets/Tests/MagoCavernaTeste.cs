#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
public class MagoCavernaTeste:MonoBehaviour {
 [System.Serializable]public class Relatorio {public List<string> aprovados=new List<string>();public List<string> falhas=new List<string>();}
 Relatorio r=new Relatorio();string pasta;float inicio;bool fim;
 public static void Testar(){EditorSceneManager.OpenScene("Assets/Bimestres34/TravessiaDoTemplo.unity");File.WriteAllText("Library/MagoTeste.request","1");EditorApplication.isPlaying=true;}
 [RuntimeInitializeOnLoadMethod]static void Iniciar(){if(!File.Exists("Library/MagoTeste.request"))return;File.Delete("Library/MagoTeste.request");new GameObject("Mago_TEST_ONLY").AddComponent<MagoCavernaTeste>();}
 void Start(){pasta=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Diagnostico/MagoCaverna01Nov"));inicio=Time.realtimeSinceStartup;StartCoroutine(Executar());}
 void Check(string nome,bool ok){if(ok)r.aprovados.Add(nome);else r.falhas.Add(nome);}
 void Update(){if(!fim && Time.realtimeSinceStartup-inicio>40){r.falhas.Add("Timeout");Finalizar();}}
 void Finalizar(){fim=true;File.WriteAllText(pasta+"/mago_testes.json",JsonUtility.ToJson(r,true));EditorApplication.Exit(r.falhas.Count==0?0:1);}
 void Foto(string nome){Camera c=Camera.main;RenderTexture antes=c.targetTexture,ativa=RenderTexture.active;RenderTexture rt=new RenderTexture(1280,720,24);Texture2D t=new Texture2D(1280,720,TextureFormat.RGB24,false);c.targetTexture=rt;Canvas.ForceUpdateCanvases();c.Render();RenderTexture.active=rt;t.ReadPixels(new Rect(0,0,1280,720),0,0);t.Apply();File.WriteAllBytes(pasta+"/"+nome+".png",t.EncodeToPNG());c.targetTexture=antes;RenderTexture.active=ativa;Destroy(t);Destroy(rt);}
 IEnumerator Executar(){
 yield return new WaitForSeconds(.5f);Samurai s=FindObjectOfType<Samurai>();OldSamurai b=FindObjectOfType<OldSamurai>();
 foreach(GuardaTemplo g in FindObjectsOfType<GuardaTemplo>())g.enabled=false;
 Check("Chefe usa mago e controlador novo",b.modoMago&&b.GetComponent<Animator>().runtimeAnimatorController.name=="Mago");
 Check("Objetivo final preservado",s.guardiaoFinal==b.GetComponent<AlvoTemplo>());
 Foto("entrada_caverna");s.enabled=false;s.GetComponent<Rigidbody2D>().isKinematic=true;
 s.transform.position=new Vector3(14,1,0);yield return null;Foto("parede_grade");
 s.transform.position=new Vector3(47,1,0);yield return null;Foto("parede_santuario");
 s.transform.position=new Vector3(2,1,0);
 yield return EsperarAte(delegate { return b.estado == OldSamurai.Estado.Patrulha; }, b.aviso + b.recuperacao + 2);
 s.transform.position=new Vector3(b.transform.position.x-3.8f,1,0);
 yield return EsperarAte(delegate { return b.estado == OldSamurai.Estado.Preparando; }, 1);
 Foto("mago_rajada_aviso");
 int vidas=s.VidasMissao;s.DefinirDefesa(true);
 yield return EsperarAte(delegate { return b.AtaqueExecutado == 1; }, b.aviso + 1);
 Check("Ataque 1 executa rajada",b.AtaqueExecutado==1);
 Check("Rajada usa prefab magico",b.projetil.name=="RajadaMago");
 s.transform.position=new Vector3(2,1,0);
 yield return EsperarAte(delegate { return b.estado == OldSamurai.Estado.Patrulha; }, b.recuperacao + 1);
 s.DefinirDefesa(false);s.transform.position=new Vector3(b.transform.position.x-1.25f,1,0);
 yield return EsperarAte(delegate { return b.estado == OldSamurai.Estado.Preparando; }, 1);
 Check("Ataque proximo usa segunda animacao",b.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Ataque2"));Foto("mago_golpe_aviso");
 yield return EsperarAte(delegate { return b.AtaqueExecutado == 2; }, b.aviso + 1);
 Check("Ataque 2 executa golpe",b.AtaqueExecutado==2);Check("Golpe causa dano",s.VidasMissao<vidas);
 Check("Piso composto preservado",FindObjectOfType<CompositeCollider2D>().pathCount==2);
 b.GetComponent<AlvoTemplo>().ReceberDano(100,s);yield return new WaitForSeconds(.1f);Check("Morte usa animacao propria",b.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Morte"));
 yield return new WaitForSeconds(.7f);Check("Chefe desaparece",b==null);Finalizar();
 }

 IEnumerator EsperarAte(System.Func<bool> condicao, float limiteSegundos)
 {
     float prazo = Time.time + limiteSegundos;
     while (!condicao() && Time.time < prazo)
         yield return null;
 }
}
#endif
