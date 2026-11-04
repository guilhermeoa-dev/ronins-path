#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections;
using System.Collections.Generic;
public class PortaisAulaTeste:MonoBehaviour {
 [System.Serializable]public class Resultado{public List<string> aprovados=new List<string>(),falhas=new List<string>();}
 Resultado r=new Resultado();float inicio;bool fim;
 public static void Testar(){EditorSceneManager.OpenScene("Assets/Bimestres34/TravessiaDoTemplo.unity");File.WriteAllText("Library/PortaisAula.request","1");EditorApplication.isPlaying=true;}
 [RuntimeInitializeOnLoadMethod]static void Inicializar(){if(!File.Exists("Library/PortaisAula.request"))return;File.Delete("Library/PortaisAula.request");new GameObject("Portal_TEST_ONLY").AddComponent<PortaisAulaTeste>();}
 void Check(string nome,bool ok){if(ok)r.aprovados.Add(nome);else r.falhas.Add(nome);}
 void Start(){inicio=Time.realtimeSinceStartup;StartCoroutine(Executar());}
 void Update(){if(!fim&&Time.realtimeSinceStartup-inicio>20){r.falhas.Add("Timeout");Finalizar();}}
 void Finalizar(){fim=true;File.WriteAllText(Path.GetFullPath(Application.dataPath+"/../../Diagnostico/Aulas04Nov/portais.json"),JsonUtility.ToJson(r,true));EditorApplication.Exit(r.falhas.Count==0?0:1);}
 IEnumerator Executar(){
  yield return new WaitForSeconds(.2f);
  Samurai s=FindObjectOfType<Samurai>();foreach(GuardaTemplo g in FindObjectsOfType<GuardaTemplo>())g.enabled=false;FindObjectOfType<OldSamurai>().enabled=false;
  Rigidbody2D rb=s.GetComponent<Rigidbody2D>();rb.gravityScale=0;rb.velocity=Vector2.zero;
  Transform a=GameObject.FindGameObjectWithTag("portal01").transform,b=GameObject.FindGameObjectWithTag("portal02").transform;
  Check("Ambos os portais possuem trigger",a.GetComponent<Collider2D>().isTrigger&&b.GetComponent<Collider2D>().isTrigger);
  rb.position=a.position;yield return new WaitForSeconds(.1f);Check("Objetivos bloqueiam entrada antecipada",!s.portalUsado);
  s.ponteAtivada=true;s.guardasDerrotados=3;yield return new WaitForSeconds(.15f);
  Check("Contato real no portal01 leva a direita do portal02",Mathf.Abs(s.transform.position.x-(b.position.x+1.8f))<.1f);
  Check("Cooldown bloqueia retorno imediato",!s.Teletransportar(a));
  yield return new WaitForSeconds(1.1f);Check("Saida nao provoca loop",Mathf.Abs(s.transform.position.x-(b.position.x+1.8f))<.1f);
  rb.position=b.position;yield return new WaitForSeconds(.15f);
  Check("Contato real no portal02 leva a esquerda do portal01",Mathf.Abs(s.transform.position.x-(a.position.x-1.8f))<.1f);
  yield return new WaitForSeconds(1.1f);Check("Retorno nao provoca loop",Mathf.Abs(s.transform.position.x-(a.position.x-1.8f))<.1f);
  Finalizar();
 }
}
#endif
