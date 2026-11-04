#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections;
using System.Collections.Generic;
public class SonsRoninTeste:MonoBehaviour {
 [System.Serializable]public class Resultado{public List<string> aprovados=new List<string>(),falhas=new List<string>();}
 Resultado r=new Resultado();float inicio;bool fim;
 public static void Testar(){EditorSceneManager.OpenScene("Assets/Bimestres34/TravessiaDoTemplo.unity");File.WriteAllText("Library/SonsRonin.request","1");EditorApplication.isPlaying=true;}
 [RuntimeInitializeOnLoadMethod]static void Inicializar(){if(!File.Exists("Library/SonsRonin.request"))return;File.Delete("Library/SonsRonin.request");var g=new GameObject("SOM_TEST_ONLY");DontDestroyOnLoad(g);g.AddComponent<SonsRoninTeste>();}
 void Check(string nome,bool ok){if(ok)r.aprovados.Add(nome);else r.falhas.Add(nome);}
 void Start(){inicio=Time.realtimeSinceStartup;StartCoroutine(Executar());}
 void Update(){if(!fim&&Time.realtimeSinceStartup-inicio>30){r.falhas.Add("Timeout");Finalizar();}}
 void Finalizar(){fim=true;File.WriteAllText(Path.GetFullPath(Application.dataPath+"/../../Diagnostico/Audio04Nov/testes.json"),JsonUtility.ToJson(r,true));EditorApplication.Exit(r.falhas.Count==0?0:1);}
 IEnumerator Executar(){
  yield return new WaitForSeconds(.3f);var audio=FindObjectOfType<SomRonin>();var s=FindObjectOfType<Samurai>();var boss=FindObjectOfType<OldSamurai>();boss.enabled=false;foreach(var g in FindObjectsOfType<GuardaTemplo>())g.enabled=false;
  foreach(string nome in new[]{"pulo","katana","tiro","magia","impacto","dano","coleta","portal","explosao","vitoria","derrota","dash","boss_rock"}){var c=Resources.Load<AudioClip>("SonsRonin/"+nome);Check("Audio importado: "+nome,c!=null&&c.samples>1000);}
  Check("Rock silencioso antes do chefe",!audio.RockAtivo);
  bool baixo=true;foreach(var f in FindObjectsOfType<AudioSource>())if(f.gameObject!=audio.gameObject)baixo&=f.volume<=.101f;Check("Musica ambiente reduzida",baixo);
  s.TentarPular(true);Check("Pulo dispara efeito",audio.UltimoEfeito=="pulo");yield return new WaitForSeconds(.1f);
  s.AtacarKatana();Check("Katana dispara efeito",audio.UltimoEfeito=="katana");yield return new WaitForSeconds(.65f);
  s.DispararMissao();Check("Projetil dispara efeito",audio.UltimoEfeito=="tiro");
  s.ReceberDanoMissao(1,false);Check("Dano dispara efeito",audio.UltimoEfeito=="dano");int n=audio.EfeitosTocados;s.ReceberDanoMissao(1,false);Check("Invulnerabilidade nao repete som",audio.EfeitosTocados==n);
  s.GanharBonusTemporario(1);Check("Bonus dispara efeito",audio.UltimoEfeito=="coleta");
  s.ponteAtivada=true;s.guardasDerrotados=3;s.Teletransportar(GameObject.FindGameObjectWithTag("portal02").transform);Check("Portal dispara efeito",audio.UltimoEfeito=="portal");
  s.transform.position=boss.transform.position+Vector3.left*3+Vector3.up;yield return new WaitForSeconds(1.4f);
  Check("Rock inicia perto do chefe",audio.RockAtivo);baixo=true;foreach(var f in FindObjectsOfType<AudioSource>())if(f.gameObject!=audio.gameObject)baixo&=f.volume<.01f;Check("Rock substitui musica ambiente",baixo);
  boss.GetComponent<AlvoTemplo>().ReceberDano(100,s);yield return new WaitForSeconds(1.5f);
  Check("Vitoria para rock",SceneManager.GetActiveScene().name=="Vitoria3"&&!audio.RockAtivo);Check("Vitoria toca fanfarra",audio.UltimoEfeito=="vitoria");Check("Um gerenciador entre cenas",FindObjectsOfType<SomRonin>().Length==1);
  Finalizar();
 }
}
#endif
