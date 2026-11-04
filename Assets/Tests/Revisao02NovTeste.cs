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
using System.Reflection;
public class Revisao02NovTeste:MonoBehaviour {
 [System.Serializable]public class Relatorio {public List<string> aprovados=new List<string>();public List<string> falhas=new List<string>();}
 Relatorio r=new Relatorio();string pasta;float inicio;bool fim;
 public static void Testar(){EditorSceneManager.OpenScene("Assets/Bimestres34/TravessiaDoTemplo.unity");File.WriteAllText("Library/Revisao02.request","1");EditorApplication.isPlaying=true;}
 [RuntimeInitializeOnLoadMethod]static void Iniciar(){if(!File.Exists("Library/Revisao02.request"))return;File.Delete("Library/Revisao02.request");var go=new GameObject("REVISAO_TEST_ONLY");DontDestroyOnLoad(go);go.AddComponent<Revisao02NovTeste>();}
 void Start(){pasta=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Diagnostico/Revisao02Nov"));inicio=Time.realtimeSinceStartup;StartCoroutine(Executar());}
 void Check(string nome,bool ok){if(ok)r.aprovados.Add(nome);else r.falhas.Add(nome);}
 void Update(){if(!fim && Time.realtimeSinceStartup-inicio>50){r.falhas.Add("Timeout");Finalizar();}}
 void Finalizar(){fim=true;File.WriteAllText(pasta+"/testes.json",JsonUtility.ToJson(r,true));EditorApplication.Exit(r.falhas.Count==0?0:1);}
 void Foto(string nome){Camera c=Camera.main;RenderTexture antes=c.targetTexture,ativa=RenderTexture.active;RenderTexture rt=new RenderTexture(1280,720,24);Texture2D t=new Texture2D(1280,720,TextureFormat.RGB24,false);c.targetTexture=rt;Canvas.ForceUpdateCanvases();c.Render();RenderTexture.active=rt;t.ReadPixels(new Rect(0,0,1280,720),0,0);t.Apply();File.WriteAllBytes(pasta+"/"+nome+".png",t.EncodeToPNG());c.targetTexture=antes;RenderTexture.active=ativa;Destroy(t);Destroy(rt);}
 IEnumerator Executar(){
 yield return new WaitForSeconds(1);Samurai s=FindObjectOfType<Samurai>();OldSamurai b=FindObjectOfType<OldSamurai>();
 foreach(GuardaTemplo g in FindObjectsOfType<GuardaTemplo>()){Check("Guarda tem 2 vidas: "+g.name,g.GetComponent<AlvoTemplo>().vida==2);g.enabled=false;}
 Check("Samurai inicia com cinco vidas",s.VidasMissao==5);Check("Mago inicia com cinco vidas",b.GetComponent<AlvoTemplo>().vida==5);
 HudRestaurado h=FindObjectOfType<HudRestaurado>();Check("HUD tem cinco coracoes",h.coracoes.Length==5&&h.coracoes.All(t=>t!=null));
 Check("Coracoes alinhados",h.coracoes.All(t=>Mathf.Abs(t.rectTransform.anchoredPosition.y+46)<.01f));
 foreach(var sr in FindObjectsOfType<SpriteRenderer>().Where(v=>v.name.Contains("Caixa do mecanismo")||v.name.Contains("Bau de vento"))){Check("Caixa apoiada: "+sr.name,Mathf.Abs(sr.bounds.min.y)<.06f);Check("Colisor acompanha base: "+sr.name,Mathf.Abs(sr.bounds.min.y-sr.GetComponent<BoxCollider2D>().bounds.min.y)<.01f);}
 Check("Fundo possui as duas camadas da Cena2",GameObject.Find("Fundo igual a Cena2").GetComponentsInChildren<SpriteRenderer>().Select(v=>v.sprite.name).Distinct().Count()==2);
 Check("Gizmos no guarda",typeof(GuardaTemplo).GetMethod("OnDrawGizmosSelected",BindingFlags.NonPublic|BindingFlags.Instance)!=null);
 Check("Gizmos no mago",typeof(OldSamurai).GetMethod("OnDrawGizmosSelected",BindingFlags.NonPublic|BindingFlags.Instance)!=null);
 Foto("hud_templo");
 s.ReceberDanoMissao(1,false);yield return null;Check("Dano apaga um coracao",s.VidasMissao==4&&h.coracoes[4].color.r<.5f&&h.coracoes[3].color.r>.9f);
 s.transform.position=new Vector3(7,1,0);yield return new WaitForSeconds(.1f);Foto("caixas_chao");
 ObjetoTemplo caixa=GameObject.FindGameObjectWithTag("mover").GetComponent<ObjetoTemplo>();
 s.CarregarObjeto(caixa);Check("Caixa pode ser carregada",caixa.carregado);s.SoltarObjeto();yield return new WaitForSeconds(1);
 Check("Caixa volta ao chao ao soltar",!caixa.carregado&&Mathf.Abs(caixa.GetComponent<SpriteRenderer>().bounds.min.y)<.06f);
 s.transform.position=new Vector3(47,1,0);yield return new WaitForSeconds(.1f);Foto("cenario_templo");
 b.GetComponent<AlvoTemplo>().ReceberDano(4,s);Check("Mago sobrevive a quatro danos",b.GetComponent<AlvoTemplo>().vida==1&&!s.missaoEncerrada);
 b.GetComponent<AlvoTemplo>().ReceberDano(1,s);Check("Derrotar mago encerra com vitoria",s.missaoEncerrada&&s.venceuMissao);
 yield return new WaitForSeconds(1.5f);Check("Tela de vitoria carregada",SceneManager.GetActiveScene().name=="Vitoria3");Foto("vitoria");
 foreach(string cena in new[]{"Cena1","Cena2"}){Samurai.ReiniciarPartida();SceneManager.LoadScene(cena);yield return new WaitForSeconds(.5f);s=FindObjectOfType<Samurai>();h=FindObjectOfType<HudRestaurado>();Check(cena+" reinicia com cinco vidas",s.VidasMissao==5);Check(cena+" possui cinco coracoes",h.coracoes.Length==5);Foto("hud_"+cena);}
 Finalizar();
 }
}
#endif

