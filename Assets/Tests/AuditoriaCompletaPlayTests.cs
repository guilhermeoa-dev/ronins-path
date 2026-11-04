#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

// Executado apenas pela solicitacao explicita AuditoriaCompleta.Testar.
[DefaultExecutionOrder(200)]
public class AuditoriaCompletaPlayTests : MonoBehaviour
{
    [Serializable] public class Check { public string nome, detalhe; public bool passou; }
    [Serializable] public class Colisor { public string cena, nome, tag, tipo; public Vector3 centro, tamanho, escala; public bool trigger; public float atrito; }
    [Serializable] public class Resultado { public bool completo; public List<Check> testes = new List<Check>(); public List<string> erros = new List<string>(); public List<Colisor> colisores = new List<Colisor>(); }
    Resultado r = new Resultado(); Samurai jogador; float direcao; float inicio;
    string arquivo;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar() {
        if (!File.Exists("Library/AuditoriaCompleta.request")) return;
        File.Delete("Library/AuditoriaCompleta.request");
        GameObject g = new GameObject("Auditoria_TEST_ONLY"); DontDestroyOnLoad(g); g.AddComponent<AuditoriaCompletaPlayTests>();
    }
    void Start() {
        arquivo = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Diagnostico/AuditoriaCompleta/playmode_navegacao_colisoes.json"));
        inicio = Time.realtimeSinceStartup; Application.logMessageReceived += Log; StartCoroutine(ExecutarProtegido());
    }
    void Log(string m, string s, LogType t) { if (t == LogType.Error || t == LogType.Exception || t == LogType.Assert) r.erros.Add(m + "\n" + s); }
    void Checkar(string nome, bool passou, string detalhe) { r.testes.Add(new Check { nome = nome, passou = passou, detalhe = detalhe }); Salvar(); }
    void Salvar() { File.WriteAllText(arquivo, JsonUtility.ToJson(r, true)); }
    void LateUpdate() { if (jogador != null) jogador.AplicarMovimento(direcao, false); }
    void Update() { if (Time.realtimeSinceStartup - inicio > 150) { r.erros.Add("Timeout"); Fim(false); } }
    void Fim(bool completo) {
        r.completo = completo; Salvar(); Application.logMessageReceived -= Log;
        bool ok = completo && r.erros.Count == 0 && r.testes.All(t => t.passou);
        Debug.Log("AUDITORIA_TESTES_FINAL ok=" + ok + " total=" + r.testes.Count);
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-batchmode") >= 0) EditorApplication.Exit(ok ? 0 : 1); else EditorApplication.isPlaying = false;
    }
    IEnumerator ExecutarProtegido() {
        IEnumerator e = Executar();
        while (true) { bool mais; object atual; try { mais = e.MoveNext(); atual = mais ? e.Current : null; } catch (Exception ex) { r.erros.Add(ex.ToString()); Fim(false); yield break; } if (!mais) break; yield return atual; }
        Fim(true);
    }
    static FieldInfo Campo(string nome) { return typeof(Samurai).GetField(nome, BindingFlags.Static | BindingFlags.NonPublic); }
    static Collider2D Item(string tag, Vector3 pos) { GameObject g = new GameObject("Item_TEST_ONLY"); g.tag = tag; g.transform.position = pos; BoxCollider2D c = g.AddComponent<BoxCollider2D>(); c.isTrigger = true; c.size = Vector2.one * .12f; g.AddComponent<ColetavelRestaurado>(); return c; }
    IEnumerator Executar() {
        yield return null;
        foreach (string cena in new[] { "PrimeiraTela", "FAses", "GameOver", "Vitoria1", "Vitoria2", "Vitoria3" }) {
            SceneManager.LoadScene(cena); yield return new WaitForSeconds(.15f);
            ClickAsset[] botoes = FindObjectsOfType<ClickAsset>();
            Checkar(cena + " possui area clicavel", botoes.Length > 0 && botoes.All(b => b.GetComponent<Collider2D>().enabled), "botoes=" + botoes.Length);
            string tag = cena == "PrimeiraTela" ? "botao_start" : cena == "FAses" ? "botao_fase1" : "reiniciar";
            GameObject botao = GameObject.FindGameObjectWithTag(tag);
            Vector3 tela = Camera.main.WorldToScreenPoint(botao.GetComponent<Collider2D>().bounds.center);
            RaycastHit2D hitBotao = Physics2D.GetRayIntersection(Camera.main.ScreenPointToRay(tela));
            Checkar(cena + " camera alcanca colisor do botao", hitBotao.collider == botao.GetComponent<Collider2D>(), "raio no centro da area clicavel");
            botao.SendMessage("OnMouseDown"); yield return null;
            string esperado = cena == "PrimeiraTela" ? "FAses" : cena == "FAses" ? "Cena1" : "PrimeiraTela";
            Checkar(cena + " callback de clique navega", SceneManager.GetActiveScene().name == esperado, SceneManager.GetActiveScene().name);
        }
        foreach (string tag in new[] { "botao_fase2", "botao_templo", "botao_menu" }) {
            yield return null; bool aceitou = ClickAsset.Acionar(tag); bool duplicou = ClickAsset.Acionar(tag); yield return null;
            string esperado = tag == "botao_fase2" ? "Cena2" : tag == "botao_templo" ? "TravessiaDoTemplo" : "PrimeiraTela";
            Checkar(tag + " acao compartilhada e troca unica", aceitou && !duplicou && SceneManager.GetActiveScene().name == esperado, SceneManager.GetActiveScene().name);
        }
        Campo("ContadorMoedas").SetValue(null, 10); Campo("BonusBotinha").SetValue(null, 3f); Samurai.ReiniciarPartida();
        Checkar("Nova partida limpa progresso", (int)Campo("ContadorMoedas").GetValue(null) == 0 && (float)Campo("BonusBotinha").GetValue(null) == 0 && (int)Campo("ContadorVidas").GetValue(null) == 5, "estado estatico");
        foreach (string cena in new[] { "Cena1", "Cena2", "TravessiaDoTemplo" }) {
            jogador = null; Samurai.ReiniciarPartida(); SceneManager.LoadScene(cena); yield return new WaitForSeconds(.3f);
            Samurai s = FindObjectsOfType<Samurai>().First(x => x.enabled); Rigidbody2D rb = s.GetComponent<Rigidbody2D>(); Collider2D corpo = s.GetComponent<Collider2D>();
            foreach (Collider2D c in FindObjectsOfType<Collider2D>()) if (c.enabled) r.colisores.Add(new Colisor { cena = cena, nome = c.name, tag = c.tag, tipo = c.GetType().Name, centro = c.bounds.center, tamanho = c.bounds.size, escala = c.transform.lossyScale, trigger = c.isTrigger, atrito = c.sharedMaterial == null ? .4f : c.sharedMaterial.friction });
            Salvar();
            foreach (GuardaTemplo g in FindObjectsOfType<GuardaTemplo>()) g.enabled = false;
            foreach (OldSamurai g in FindObjectsOfType<OldSamurai>()) g.enabled = false;
            // Percurso sobre o piso original, incluindo a emenda inicial da Cena1
            // e tres emendas consecutivas do templo. Nenhum piso e substituido.
            float pisoY = cena == "Cena1" ? -2.95f : cena == "Cena2" ? -3.08f : 0;
            float inicioX = cena == "Cena1" ? -13 : cena == "Cena2" ? -24 : 0;
            float alturaPe = rb.position.y - corpo.bounds.min.y;
            rb.position = new Vector2(inicioX, pisoY + alturaPe + .04f); rb.velocity = Vector2.zero;
            jogador = s; direcao = 0; yield return new WaitForSeconds(.25f);
            direcao = 1; yield return new WaitForSeconds(1f); direcao = 0;
            // Cena2 tem uma corrente real neste percurso e reduz a velocidade.
            Checkar(cena + " percorre piso original sem travar", rb.position.x > inicioX + (cena == "Cena2" ? 2f : 2.5f) && Mathf.Abs(corpo.bounds.min.y - pisoY) < .15f, "dx=" + (rb.position.x - inicioX) + " pe=" + corpo.bounds.min.y);
            if (cena == "TravessiaDoTemplo") {
                CompositeCollider2D piso = FindObjectOfType<CompositeCollider2D>();
                Checkar("Piso composto conserva desfiladeiro", piso != null && piso.pathCount >= 2 && !piso.OverlapPoint(new Vector2(22, -.5f)), piso == null ? "ausente" : "contornos=" + piso.pathCount);
                foreach (float trecho in new[] { 34f, 44f, 61f }) {
                    rb.position = new Vector2(trecho, alturaPe + .04f); rb.velocity = Vector2.zero; direcao = 0;
                    yield return new WaitForSeconds(.15f); direcao = 1;
                    yield return new WaitForSeconds(1f); direcao = 0;
                    Checkar("Templo cruza emendas desde x=" + trecho, rb.position.x > trecho + 2.5f && Mathf.Abs(corpo.bounds.min.y) < .15f, "dx=" + (rb.position.x - trecho));
                }
            }
            // Isola os testes de controle sem alterar os assets serializados.
            rb.position = new Vector2(-100, 30); rb.velocity = Vector2.zero; rb.gravityScale = 0;
            yield return new WaitForFixedUpdate(); jogador = s; direcao = 0;
            Campo("PegouPezinho").SetValue(null, true); Campo("TotalPulos").SetValue(null, 3);
            s.TentarPular(true); float p1 = rb.velocity.y; s.TentarPular(true); float p2 = rb.velocity.y; s.TentarPular(true); float p3 = rb.velocity.y;
            Checkar(cena + " forcas de tres pulos", Mathf.Abs(p1 - 6.5f) < .01f && Mathf.Abs(p2 - 5.2f) < .01f && Mathf.Abs(p3 - 4.55f) < .01f, p1 + "," + p2 + "," + p3);
            typeof(Samurai).GetField("ContadorPulos", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(s, 0);
            typeof(Samurai).GetField("reducaoCorrente", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(s, 0f);
            rb.velocity = Vector2.zero; Campo("BonusBotinha").SetValue(null, 1f); s.GanharBonusTemporario(.2f); s.AplicarMovimento(1, false); float bonus = rb.velocity.x;
            yield return new WaitForSeconds(.3f); s.AplicarMovimento(1, false);
            Checkar(cena + " bonus expira preservando permanente", Mathf.Abs(bonus - 6) < .01f && Mathf.Abs(rb.velocity.x - 4) < .01f, bonus + " -> " + rb.velocity.x);
            s.AplicarMovimento(0, false); rb.velocity = Vector2.zero;
            int moedas = (int)Campo("ContadorMoedas").GetValue(null);
            Collider2D moeda = Item("Moedas", s.areaKatanaDireita.bounds.center);
            // A area esta desabilitada fora do golpe; posiciona pelo Transform e aciona o golpe real.
            moeda.transform.position = s.areaKatanaDireita.transform.position + Vector3.right * .2f;
            s.GetComponent<SpriteRenderer>().flipX = false; s.AtacarKatana();
            yield return new WaitForSeconds(.4f);
            Checkar(cena + " katana nao coleta moeda", (int)Campo("ContadorMoedas").GetValue(null) == moedas, "antes=" + moedas + " depois=" + Campo("ContadorMoedas").GetValue(null));
            if (moeda != null) moeda.transform.position = corpo.bounds.center;
            yield return new WaitForSeconds(.12f);
            Checkar(cena + " corpo coleta uma vez", (int)Campo("ContadorMoedas").GetValue(null) == moedas + 1, "contador=" + Campo("ContadorMoedas").GetValue(null));
            yield return new WaitForSeconds(.2f);
            // Reproduz contato lateral com uma parede solida padrao, acima do piso.
            rb.position = new Vector2(-100, 30); rb.velocity = Vector2.zero; rb.gravityScale = 1.2f;
            yield return new WaitForFixedUpdate();
            GameObject parede = new GameObject("ParedeAtrito_TEST_ONLY"); parede.transform.position = new Vector3(corpo.bounds.max.x + .12f, 28, 0); parede.AddComponent<BoxCollider2D>().size = new Vector2(.2f, 10);
            float y = rb.position.y; direcao = 1; yield return new WaitForSeconds(.65f);
            Checkar(cena + " desliza em parede sem prender", rb.position.y < y - .5f, "queda=" + (y - rb.position.y));
            Destroy(parede); jogador = null;
        }
    }
}
#endif

