#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
// Instrumentacao de teste editorial; nao faz parte das mecanicas nem entra na build.
public class TemploPlayTests : MonoBehaviour {
 [Serializable] public class Verificacao { public string nome; public bool passou; }
 [Serializable] public class Resultado { public string unity; public bool completo; public List<Verificacao> testes=new List<Verificacao>(); public List<string> erros=new List<string>(); }
 Resultado resultado=new Resultado();Samurai s;string pasta;bool terminado;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void IniciarSePedido() {
  if(!File.Exists("Library/TestarTemplo.request"))return;
  File.Delete("Library/TestarTemplo.request");new GameObject("Teste do templo").AddComponent<TemploPlayTests>();
 }
 void Start() { pasta=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Diagnostico/EstruturaSlides/Missao")); Directory.CreateDirectory(pasta); resultado.unity=Application.unityVersion;Application.logMessageReceived+=Log;StartCoroutine(Executar()); }
 void Log(string m,string stack,LogType tipo){if(tipo==LogType.Error||tipo==LogType.Exception)resultado.erros.Add(m+"\n"+stack);}
 void Verificar(string nome,bool ok){resultado.testes.Add(new Verificacao{nome=nome,passou=ok});Salvar();}
 void Salvar(){File.WriteAllText(Path.Combine(pasta,"testes_unity.json"),JsonUtility.ToJson(resultado,true));}
 void Captura(string nome){
  Camera c=Camera.main;RenderTexture antigo=c.targetTexture,ativo=RenderTexture.active;
  RenderTexture rt=new RenderTexture(Screen.width,Screen.height,24);Texture2D foto=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
  c.targetTexture=rt;Canvas.ForceUpdateCanvases();c.Render();RenderTexture.active=rt;
  foto.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);foto.Apply();File.WriteAllBytes(Path.Combine(pasta,nome+".png"),foto.EncodeToPNG());
  c.targetTexture=antigo;RenderTexture.active=ativo;Destroy(rt);Destroy(foto);
 }
 IEnumerator Executar(){
  yield return new WaitForSeconds(.6f);
  s=FindObjectOfType<Samurai>();
  s.GetComponent<ResultadoTemplo>().enabled=false;
  Verificar("Samurai existente controla a missao",s!=null&&s.modoMissao);
  int faltando=0;foreach(Transform t in FindObjectsOfType<Transform>())foreach(Component c in t.GetComponents<Component>())if(c==null)faltando++;
  Verificar("Componentes resolvidos",faltando==0);
  Captura("inicio");
  foreach(GuardaTemplo guarda in FindObjectsOfType<GuardaTemplo>())guarda.enabled=false;
  Rigidbody2D rb=s.GetComponent<Rigidbody2D>();
  ObjetoTemplo caixa=null,selo=null,portal=null,meta=null;
  foreach(ObjetoTemplo o in FindObjectsOfType<ObjetoTemplo>()) {
   if(o.tipo==ObjetoTemplo.Tipo.CaixaMovel)caixa=o;
   if(o.tipo==ObjetoTemplo.Tipo.Selo)selo=o;
   if(o.tipo==ObjetoTemplo.Tipo.Portal)portal=o;
   if(o.tipo==ObjetoTemplo.Tipo.Pergaminho)meta=o;
  }
  s.transform.position=caixa.transform.position+Vector3.left;rb.velocity=Vector2.zero;
  s.CarregarObjeto(caixa);Verificar("E - carregar usa hierarquia",caixa.transform.parent==s.transform&&caixa.carregado);
  s.GetComponent<SpriteRenderer>().flipX=false;s.transform.position=new Vector3(selo.transform.position.x-1.4f,1.1f,0);s.SoltarObjeto();
  yield return new WaitForSeconds(1.2f);
  Verificar("Soltar restaura fisica",!caixa.carregado&&caixa.transform.parent==null&&!caixa.GetComponent<Rigidbody2D>().isKinematic);
  Verificar("Caixa no selo abre grade por toque indireto",s.ponteAtivada&&!selo.portao.activeSelf);
  Vector3 antes=selo.plataforma.transform.position;yield return new WaitForSeconds(.5f);
  Verificar("Plataforma se move apos ativacao",Vector3.Distance(antes,selo.plataforma.transform.position)>.2f);
  s.transform.position=selo.plataforma.transform.position+Vector3.up*1.3f;rb.velocity=Vector2.zero;
  yield return new WaitForSeconds(.4f);
  Verificar("Personagem acompanha plataforma fisicamente",s.transform.parent==selo.plataforma.transform);
  s.TentarPular(true);Verificar("Pulo sai da plataforma",rb.velocity.y>6);
  yield return new WaitForSeconds(.2f);s.transform.SetParent(null,true);
  s.transform.position=new Vector3(3.9f,1.1f,0);rb.velocity=Vector2.zero;s.GetComponent<SpriteRenderer>().flipX=false;
  Verificar("Katana iniciou",s.AtacarKatana());yield return new WaitForSeconds(.4f);
  bool bonusSolto=false;foreach(ObjetoTemplo o in FindObjectsOfType<ObjetoTemplo>())if(o.tipo==ObjetoTemplo.Tipo.BonusVento&&o.transform.parent==null){bonusSolto=true;s.transform.position=o.transform.position;break;}
  Verificar("Katana quebra bau e libera filho",bonusSolto||s.bonusRestante>0);yield return new WaitForSeconds(.2f);
  Verificar("Coletar bonus ativa temporizador",s.bonusRestante>7);
  s.transform.position=new Vector3(2,1.1f,0);rb.velocity=Vector2.zero;
  yield return new WaitForSeconds(8.2f);Verificar("Bonus expira",s.bonusRestante==0);
  s.DefinirDefesa(true);yield return null;
  // AtualizarMissao le teclado; durante a verificacao de defesa desabilita apenas Update do Samurai.
  s.enabled=false;s.DefinirDefesa(true);
  yield return new WaitForSeconds(.15f);
  Verificar("Animator possui animacao de defesa",s.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Defendendo"));
  int vidas=s.VidasMissao;
  GameObject tiroHostil=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Bimestres34/DisparoGuarda.prefab"),s.transform.position+Vector3.right*2,Quaternion.identity);
  tiroHostil.GetComponent<Rigidbody2D>().velocity=Vector2.left*5;yield return new WaitForSeconds(.5f);
  Verificar("Defesa bloqueia projetil real",s.VidasMissao==vidas&&tiroHostil==null);
  s.DefinirDefesa(false);
  tiroHostil=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Bimestres34/DisparoGuarda.prefab"),s.transform.position+Vector3.right*2,Quaternion.identity);
  tiroHostil.GetComponent<Rigidbody2D>().velocity=Vector2.left*5;yield return new WaitForSeconds(.5f);
  Verificar("Sem defesa perde uma vida",s.VidasMissao==vidas-1);
  s.enabled=true;
  for(int tipo=0;tipo<3;tipo++) {
   yield return new WaitForSeconds(1.3f);s.tipoProjetil=tipo;
   GameObject tiro=s.DispararMissao();Verificar("Disparo tipo "+tipo,tiro!=null&&tiro.GetComponent<ProjetilTemplo>().tipo==tipo);
   Verificar("Recarga bloqueia disparo repetido "+tipo,s.DispararMissao()==null);
   if(tiro!=null)Destroy(tiro);
  }
  GuardaTemplo primeiro=FindObjectOfType<GuardaTemplo>();s.transform.position=primeiro.transform.position+Vector3.left*2.5f;primeiro.enabled=true;
  s.enabled=false;s.DefinirDefesa(true);yield return new WaitForSeconds(.6f);
  bool encontrou=false;foreach(ProjetilTemplo p in FindObjectsOfType<ProjetilTemplo>())if(p.hostil)encontrou=true;
  Verificar("Guarda dispara ao aproximar",encontrou);primeiro.enabled=false;s.DefinirDefesa(false);s.enabled=true;
  foreach(ProjetilTemplo p in FindObjectsOfType<ProjetilTemplo>())if(!p.modelo)Destroy(p.gameObject);
  // Projeteis reais atingem os tres guardas. Posicoes de preparacao isolam combate de travessia.
  foreach(GuardaTemplo guarda in FindObjectsOfType<GuardaTemplo>()) {
   guarda.enabled=false;s.transform.position=guarda.transform.position+Vector3.left*3;s.GetComponent<SpriteRenderer>().flipX=false;rb.velocity=Vector2.zero;
   for(int n=0;n<3;n++){yield return new WaitForSeconds(1.3f);s.tipoProjetil=1;s.DispararMissao();yield return new WaitForSeconds(.5f);}
  }
  Verificar("Projeteis derrotam guardas e contam objetivo",s.guardasDerrotados==3);
  Verificar("Meta nao conclui antes do portal",!s.ConcluirMissao());
  s.transform.position=portal.transform.position;rb.velocity=Vector2.zero;yield return new WaitForSeconds(.15f);
  Verificar("Portal transporta sem retorno imediato",s.portalUsado&&Mathf.Abs(s.transform.position.x-(portal.destino.position.x+1.8f))<.2f);
  Captura("santuario");
  Verificar("Old Samurai bloqueia vitoria enquanto vivo",s.guardiaoFinal!=null&&!s.ConcluirMissao());
  OldSamurai chefe=FindObjectOfType<OldSamurai>();chefe.enabled=false;
  // Projeteis de combate reais vencem o novo guardiao. A IA tem uma suite propria.
  s.transform.position=chefe.transform.position+new Vector3(-3,1.1f,0);rb.velocity=Vector2.zero;s.GetComponent<SpriteRenderer>().flipX=false;
  for(int n=0;n<4;n++){yield return new WaitForSeconds(1.3f);s.tipoProjetil=1;s.DispararMissao();yield return new WaitForSeconds(.6f);}
  Verificar("Projeteis derrotam Old Samurai",s.guardiaoFinal==null||s.guardiaoFinal.Derrotado);
  s.transform.position=meta.transform.position;yield return new WaitForSeconds(.15f);
  Verificar("Objetivos levam a vitoria",s.missaoEncerrada);
  Captura("vitoria");
  // Prepara uma queda apos a verificacao de vitoria para conferir o painel de derrota.
  s.missaoEncerrada=false;s.transform.position=new Vector3(2,-6,0);rb.velocity=Vector2.zero;
  yield return new WaitForSeconds(.15f);
  Verificar("Queda encerra e preserva mensagem de derrota",s.missaoEncerrada&&s.VidasMissao==0&&s.painelMissao.text.Contains("O SAMURAI CAIU"));
  DontDestroyOnLoad(gameObject);s.SairDaMissao();yield return new WaitForSeconds(.2f);
  Verificar("Sair da missao abre selecao",SceneManager.GetActiveScene().name=="FAses");
  SceneManager.LoadScene("Cena1");yield return new WaitForSeconds(.6f);
  Samurai antigo=FindObjectOfType<Samurai>();Rigidbody2D corpoAntigo=antigo.GetComponent<Rigidbody2D>();
  float antesDoPulo=corpoAntigo.velocity.y;antigo.TentarPular(true);
  Verificar("Retorno preserva vidas e bloqueio original de pulo",!antigo.modoMissao&&antigo.UITextVidas.text=="5"&&Mathf.Abs(corpoAntigo.velocity.y-antesDoPulo)<.001f);
  resultado.completo=true;Salvar();terminado=true;
  Application.logMessageReceived-=Log;
  bool ok=resultado.erros.Count==0;foreach(Verificacao v in resultado.testes)ok &=v.passou;
  Debug.Log("TEMPLO_TESTES_FINAL ok="+ok+" total="+resultado.testes.Count);
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-batchmode")>=0)EditorApplication.Exit(ok?0:1);else EditorApplication.isPlaying=false;
 }
 void Update(){if(!terminado&&Time.timeSinceLevelLoad>90){resultado.erros.Add("Timeout da suite");Salvar();EditorApplication.Exit(2);}}
}
#endif


