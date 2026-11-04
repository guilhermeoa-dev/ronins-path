#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

// Sonda opt-in, excluida de builds. Testa eventos fisicos e estados reais em Play Mode.
public class RevisaoPlayTests : MonoBehaviour
{
    [Serializable] public class Check { public string nome; public bool passou; }
    [Serializable] public class Resultado { public string unity; public bool completo; public List<Check> testes=new List<Check>(); public List<string> erros=new List<string>(); }
    Resultado resultado=new Resultado(); string pasta; float inicio; bool terminou;
    Samurai s; Rigidbody2D rb; OldSamurai old;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar()
    {
        if(!File.Exists("Library/TestarRevisao.request"))return;
        File.Delete("Library/TestarRevisao.request");new GameObject("Revisao_TEST_ONLY").AddComponent<RevisaoPlayTests>();
    }
    void Start()
    {
        inicio=Time.realtimeSinceStartup;pasta=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Diagnostico/EstruturaSlides/Combate"));Directory.CreateDirectory(pasta);
        resultado.unity=Application.unityVersion;Application.logMessageReceived+=Log;StartCoroutine(Protegido());
    }
    void Log(string m,string stack,LogType t) { if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)resultado.erros.Add(m+"\n"+stack); }
    void Verificar(string nome,bool ok) { resultado.testes.Add(new Check{nome=nome,passou=ok});Salvar(); }
    void Salvar() { File.WriteAllText(Path.Combine(pasta,"resultados.json"),JsonUtility.ToJson(resultado,true)); }
    IEnumerator Protegido()
    {
        IEnumerator teste=Executar();
        while(true) { bool mais=false;object passo=null;try{mais=teste.MoveNext();if(mais)passo=teste.Current;}catch(Exception e){resultado.erros.Add(e.ToString());Finalizar(false);yield break;}if(!mais)break;yield return passo; }
        Finalizar(true);
    }
    void Finalizar(bool completo)
    {
        terminou=true;resultado.completo=completo;Salvar();Application.logMessageReceived-=Log;
        bool ok=completo&&resultado.erros.Count==0;foreach(Check c in resultado.testes)ok &=c.passou;
        Debug.Log("REVISAO_TESTES_FINAL ok="+ok+" total="+resultado.testes.Count);
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-batchmode")>=0)EditorApplication.Exit(ok?0:1);else EditorApplication.isPlaying=false;
    }
    void Update() { if(!terminou&&Time.realtimeSinceStartup-inicio>85){resultado.erros.Add("Timeout");Finalizar(false);} }
    void Posicionar(float x,float y) { s.transform.SetParent(null,true);rb.position=new Vector2(x,y);rb.velocity=Vector2.zero; }
    void LimparTiros() { foreach(ProjetilTemplo p in FindObjectsOfType<ProjetilTemplo>())if(!p.modelo)Destroy(p.gameObject); }
    void Captura(string nome)
    {
        Camera c=Camera.main;RenderTexture anterior=c.targetTexture,ativa=RenderTexture.active;
        RenderTexture rt=new RenderTexture(1280,720,24);Texture2D foto=new Texture2D(1280,720,TextureFormat.RGB24,false);
        c.targetTexture=rt;c.Render();RenderTexture.active=rt;foto.ReadPixels(new Rect(0,0,1280,720),0,0);foto.Apply();
        File.WriteAllBytes(Path.Combine(pasta,nome+".png"),foto.EncodeToPNG());c.targetTexture=anterior;RenderTexture.active=ativa;Destroy(rt);Destroy(foto);
    }
    IEnumerator Executar()
    {
        yield return new WaitForSeconds(.5f);
        s=FindObjectOfType<Samurai>();rb=s.GetComponent<Rigidbody2D>();old=FindObjectOfType<OldSamurai>();
        s.GetComponent<ResultadoTemplo>().enabled=false;
        Verificar("Guardiao vinculado ao objetivo",old!=null&&s.guardiaoFinal==old.GetComponent<AlvoTemplo>());
        Verificar("Mago mantem proporcao uniforme",Mathf.Abs(old.transform.localScale.x-old.transform.localScale.y)<.001f);
        foreach(GuardaTemplo g in FindObjectsOfType<GuardaTemplo>())g.enabled=false;
        LimparTiros();s.enabled=false;rb.isKinematic=true;
        Vector3 p=old.transform.position;yield return new WaitForSeconds(.4f);
        Verificar("Patrulha se move sem jogador proximo",Mathf.Abs(old.transform.position.x-p.x)>.1f);
        Posicionar(old.transform.position.x-2.6f,1);
        yield return new WaitForSeconds(.08f);
        Verificar("Detecta e persegue a distancia intermediaria",old.estado==OldSamurai.Estado.Perseguindo);
        p=old.transform.position;yield return new WaitForSeconds(.25f);
        Verificar("Perseguicao aproxima fisicamente",old.transform.position.x<p.x);
        Posicionar(2,1);p=old.transform.position;yield return new WaitForSeconds(.04f);
        Verificar("Perde alvo e retoma patrulha sem salto",old.estado==OldSamurai.Estado.Patrulha&&Vector3.Distance(p,old.transform.position)<.15f);
        // Parede temporaria entre jogador e guardiao verifica linha de visao.
        Posicionar(old.transform.position.x-3.8f,1);
        GameObject parede=new GameObject("Parede_TEST_ONLY");parede.transform.position=new Vector3(old.transform.position.x-1.9f,1,0);parede.AddComponent<BoxCollider2D>().size=new Vector2(.3f,3);
        yield return new WaitForSeconds(.1f);
        Verificar("Parede bloqueia deteccao e disparo",old.estado==OldSamurai.Estado.Patrulha);
        Destroy(parede);yield return new WaitForSeconds(.08f);
        Verificar("Ataque tem preparacao visivel",old.estado==OldSamurai.Estado.Preparando&&old.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Ataque"));
        int vidas=s.VidasMissao;yield return new WaitForSeconds(.2f);
        Verificar("Aviso nao causa dano imediato",s.VidasMissao==vidas);
        Captura("old_samurai_aviso");
        yield return new WaitForSeconds(old.aviso-.5f+.6f);
        bool tiro=false;foreach(ProjetilTemplo t in FindObjectsOfType<ProjetilTemplo>())tiro |=t.hostil;
        Verificar("Old Samurai lanca projetil apos aviso",tiro);
        s.DefinirDefesa(true);yield return new WaitForSeconds(.7f);
        Verificar("F protege contra disparo do Old Samurai",s.VidasMissao==vidas);
        Verificar("Ataque tem janela de recuperacao",old.estado==OldSamurai.Estado.Recuperando);
        Posicionar(2,1);s.DefinirDefesa(false);LimparTiros();
        yield return new WaitForSeconds(1);
        Posicionar(old.transform.position.x-1.25f,1);
        yield return new WaitForSeconds(.08f);
        Verificar("Curta distancia prepara golpe proximo",old.estado==OldSamurai.Estado.Preparando&&old.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName(old.modoMago ? "Ataque2" : "Ataque"));
        yield return new WaitForSeconds(old.aviso-.5f+.85f);
        Verificar("Katana inimiga atinge corpo uma vez",s.VidasMissao==vidas-1);
        Posicionar(2,1);yield return new WaitForSeconds(1.4f);
        Posicionar(old.transform.position.x-1.25f,1);yield return new WaitForSeconds(.1f);
        vidas=s.VidasMissao;Posicionar(2,1);yield return new WaitForSeconds(.9f);
        Verificar("Sair do alcance evita golpe anunciado",s.VidasMissao==vidas);
        old.GetComponent<AlvoTemplo>().ReceberDano(1,s);yield return null;
        Verificar("Dano reduz vida e entra em Ferido",old.GetComponent<AlvoTemplo>().vida==4&&old.estado==OldSamurai.Estado.Ferido);
        yield return new WaitForSeconds(.08f);
        Verificar("Animacao de dano usa Hurt",old.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Ferido"));
        old.enabled=false;
        // Projetil parado toca apenas a katana, sem tocar o corpo real.
        s.areaKatanaDireita.enabled=true;
        GameObject hostil=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Bimestres34/DisparoGuarda.prefab"),s.transform.position+Vector3.right*1.35f,Quaternion.identity);
        vidas=s.VidasMissao;yield return new WaitForSeconds(.1f);
        Verificar("Area da katana nao recebe dano pelo jogador",s.VidasMissao==vidas&&hostil!=null);
        Destroy(hostil);s.areaKatanaDireita.enabled=false;
        s.DefinirDefesa(false);Verificar("Golpe pode iniciar",s.AtacarKatana());
        Verificar("Golpe impede disparo simultaneo",s.DispararMissao()==null);
        s.enabled=true;rb.isKinematic=false;yield return new WaitForSeconds(.55f);
        GameObject salvo=GameObject.FindGameObjectWithTag("projetil1");salvo.SetActive(false);s.tipoProjetil=0;
        Verificar("Modelo por tag ausente nao causa excecao",s.DispararMissao()==null);salvo.SetActive(true);
        ObjetoTemplo caixa=null,portal=null;foreach(ObjetoTemplo o in FindObjectsOfType<ObjetoTemplo>()) {if(o.tipo==ObjetoTemplo.Tipo.CaixaMovel)caixa=o;if(o.tipo==ObjetoTemplo.Tipo.Portal)portal=o;}
        caixa.transform.position=new Vector3(8,-8,0);yield return new WaitForSeconds(.1f);
        Verificar("Caixa essencial retorna apos queda",caixa.transform.position.y>0);
        s.ponteAtivada=true;
        Verificar("Portal exige os tres guardas",!s.Teletransportar(portal.destino));
        s.guardasDerrotados=3;
        Plataforma plataforma=FindObjectOfType<Plataforma>();s.transform.SetParent(plataforma.transform,true);
        Verificar("Portal funciona com objetivos",s.Teletransportar(portal.destino));
        Verificar("Teleporte solta parentesco da plataforma",s.transform.parent==null);
        Verificar("Cooldown impede teleporte repetido",!s.Teletransportar(portal.destino));
        Verificar("Chefe vivo impede concluir",!s.ConcluirMissao());
        old.enabled=true;old.GetComponent<AlvoTemplo>().ReceberDano(100,s);yield return null;
        Verificar("Chefe derrotado desativa colisao e IA",old.estado==OldSamurai.Estado.Derrotado&&!old.GetComponent<Collider2D>().enabled);
        Verificar("Derrota do chefe nao duplica contagem dos guardas",s.guardasDerrotados==3);
        old.GetComponent<AlvoTemplo>().ReceberDano(100,s);
        Verificar("Derrota do mago aciona vitoria",s.missaoEncerrada&&s.venceuMissao);
        Verificar("Vitoria nao e executada duas vezes",!s.ConcluirMissao());
        Captura("objetivo_concluido");
        yield return new WaitForSeconds(1);
        Verificar("Derrota do chefe libera objeto apos feedback",old==null);
        // Novo carregamento testa reinicio sem reaproveitar estado preparado acima.
        DontDestroyOnLoad(gameObject);SceneManager.LoadScene("TravessiaDoTemplo");yield return new WaitForSeconds(.5f);
        s=FindObjectOfType<Samurai>();rb=s.GetComponent<Rigidbody2D>();
        Verificar("Reinicio restaura vidas objetivos e chefe",s.VidasMissao==5&&s.guardasDerrotados==0&&!s.ponteAtivada&&s.guardiaoFinal.vida==5);
        s.ReceberDanoMissao(1,false);Posicionar(2,-6);yield return new WaitForSeconds(.08f);
        Verificar("Queda encerra mesmo durante invulnerabilidade",s.VidasMissao==0&&s.missaoEncerrada&&s.painelMissao.text.Contains("O SAMURAI CAIU"));
    }
}
#endif


