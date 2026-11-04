#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

[DefaultExecutionOrder(100)]
public class PolimentoPlayTests : MonoBehaviour
{
    [Serializable] public class Check { public string nome; public bool passou; public string detalhe; }
    [Serializable] public class Resultado { public string unity; public bool completo; public List<Check> testes = new List<Check>(); public List<string> erros = new List<string>(); }
    Resultado resultado = new Resultado(); string pasta; float inicio; bool terminou;
    Samurai mover; float direcao; bool correr;
    bool somenteHud;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar()
    {
        bool hud = File.Exists("Library/PolimentoHUD.request");
        if (!hud && !File.Exists("Library/Polimento.request")) return;
        File.Delete(hud ? "Library/PolimentoHUD.request" : "Library/Polimento.request");
        GameObject g = new GameObject("Polimento_TEST_ONLY"); DontDestroyOnLoad(g); g.AddComponent<PolimentoPlayTests>().somenteHud = hud;
    }
    void Start()
    {
        inicio = Time.realtimeSinceStartup; pasta = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Diagnostico/EstruturaSlides/" + (somenteHud ? "Hud" : "Jogabilidade"))); Directory.CreateDirectory(pasta);
        resultado.unity = Application.unityVersion; Application.logMessageReceived += Log; StartCoroutine(Protegido());
    }
    void Log(string m, string stack, LogType t) { if (t == LogType.Error || t == LogType.Exception || t == LogType.Assert) resultado.erros.Add(m + "\n" + stack); }
    void Verificar(string nome, bool ok, string detalhe) { resultado.testes.Add(new Check { nome = nome, passou = ok, detalhe = detalhe }); Salvar(); }
    void Salvar() { File.WriteAllText(Path.Combine(pasta, "resultados.json"), JsonUtility.ToJson(resultado, true)); }
    IEnumerator Protegido()
    {
        IEnumerator suite = Executar();
        while (true) { bool mais = false; object passo = null; try { mais = suite.MoveNext(); if (mais) passo = suite.Current; } catch (Exception e) { resultado.erros.Add(e.ToString()); Finalizar(false); yield break; } if (!mais) break; yield return passo; }
        Finalizar(true);
    }
    void Finalizar(bool completo)
    {
        terminou = true; resultado.completo = completo; Salvar(); Application.logMessageReceived -= Log;
        bool ok = completo && resultado.erros.Count == 0; foreach (Check c in resultado.testes) ok &= c.passou;
        Debug.Log("POLIMENTO_TESTES_FINAL ok=" + ok + " total=" + resultado.testes.Count);
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-batchmode") >= 0) EditorApplication.Exit(ok ? 0 : 1); else EditorApplication.isPlaying = false;
    }
    void Update() { if (!terminou && Time.realtimeSinceStartup - inicio > 90) { resultado.erros.Add("Timeout"); Finalizar(false); } }
    void LateUpdate() { if (mover != null) mover.AplicarMovimento(direcao, correr); }
    void Resetar()
    {
        foreach (string nome in new[] { "ContadorMoedas", "ContadorEspadas", "ContadorSkates" }) typeof(Samurai).GetField(nome, BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, 0);
        typeof(Samurai).GetField("ContadorVidas", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, 3);
        typeof(Samurai).GetField("PegouPezinho", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, false);
    }
    void Captura(string nome)
    {
        Camera c = Camera.main; RenderTexture antes = c.targetTexture, ativa = RenderTexture.active;
        RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24); Texture2D foto = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        c.targetTexture = rt; Canvas.ForceUpdateCanvases(); c.Render(); RenderTexture.active = rt;
        foto.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); foto.Apply();
        File.WriteAllBytes(Path.Combine(pasta, nome + ".png"), foto.EncodeToPNG()); c.targetTexture = antes; RenderTexture.active = ativa; Destroy(rt); Destroy(foto);
    }
    void Proporcoes(string cena)
    {
        int total = 0; string falhas = "";
        foreach (SpriteRenderer sr in FindObjectsOfType<SpriteRenderer>())
        {
            if (!sr.enabled || sr.sprite == null || AssetDatabase.GetAssetPath(sr.sprite.texture) == "Assets/Square.png") continue;
            Vector3 e = sr.transform.lossyScale; if (Mathf.Abs(e.x * e.y) < .00001f) continue;
            float razao = Mathf.Abs(e.x / e.y);
            if (sr.drawMode == SpriteDrawMode.Sliced) razao *= (sr.size.x / sr.size.y) / (sr.sprite.rect.width / sr.sprite.rect.height);
            total++; if (Mathf.Abs(razao - 1) > .005f) falhas += sr.name + "=" + razao.ToString("F3") + "; ";
        }
        Verificar(cena + " sprites proporcionais", falhas == "", total + " renderizadores; " + falhas);
    }
    IEnumerator Executar()
    {
        if (somenteHud)
        {
            foreach (string cena in new[] { "Cena1", "Cena2", "TravessiaDoTemplo" })
            {
                Resetar(); SceneManager.LoadScene(cena); yield return new WaitForSeconds(.6f); Captura(cena);
                Samurai s = FindObjectsOfType<Samurai>().First(p => p.enabled);
                HudRestaurado hud = FindObjectOfType<HudRestaurado>();
                Text nome = hud.GetComponentsInChildren<Text>().First(t => t.name == "Text");
                Verificar(cena + " nome Samurai visivel com fonte original", nome.cachedTextGenerator.vertexCount > 4, nome.font.name + " vertices=" + nome.cachedTextGenerator.vertexCount);
                Verificar(cena + " contadores renderizados", new[] { s.UITextMoedas, s.UITextEspadas, s.UITextSkates, s.UITextVidas }.All(t => t.cachedTextGenerator.vertexCount > 4), "Quatro Text com glifos visiveis");
                Verificar(cena + " icones preservam proporcao", hud.IconesProjeteis.All(i => i.preserveAspect), "Tres icones sem deformacao");
            }
            yield break;
        }
        foreach (string cena in new[] { "Cena1", "Cena2", "TravessiaDoTemplo" })
        {
            mover = null; Resetar(); SceneManager.LoadScene(cena); yield return new WaitForSeconds(.65f);
            Samurai s = FindObjectsOfType<Samurai>().First(p => p.enabled); Rigidbody2D rb = s.GetComponent<Rigidbody2D>();
            AnimacaoSamurai anim = s.GetComponent<AnimacaoSamurai>(); Animator animator = s.GetComponent<Animator>();
            foreach (GuardaTemplo g in FindObjectsOfType<GuardaTemplo>()) g.enabled = false;
            foreach (OldSamurai g in FindObjectsOfType<OldSamurai>()) g.enabled = false;
            Verificar(cena + " animacoes e combate compartilhados", anim != null && s.combateHabilitado && s.projeteisMissao.Length == 3, animator.runtimeAnimatorController.name);
            HudRestaurado hud = FindObjectOfType<HudRestaurado>();
            bool referencias = s.UITextMoedas != s.UITextEspadas && s.UITextMoedas != s.UITextSkates && s.UITextEspadas != s.UITextSkates;
            Verificar(cena + " HUD original e contadores separados", hud != null && hud.retrato != null && hud.coletaveis != null && referencias, "Molduras originais e Text separados");
            s.tipoProjetil = 2; yield return null;
            Verificar(cena + " selecao escurece somente o item ativo", hud.IconesProjeteis[2].color.r < .8f && hud.IconesProjeteis[0].color.r == 1 && hud.IconesProjeteis[1].color.r == 1, "Selecionado = 70%; outros = 100%");
            Captura(cena + "_hud"); Proporcoes(cena);
            if (!s.modoMissao)
            {
                GameObject pe = GameObject.FindGameObjectWithTag("Pezinho");
                if (pe != null) s.ReceberColetavel(pe.GetComponent<Collider2D>());
                else typeof(Samurai).GetField("PegouPezinho", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, true);
            }
            float antesCorrida = rb.position.x;
            mover = s; direcao = 1; correr = true; yield return new WaitForSeconds(.12f);
            Verificar(cena + " corrida usa Run", animator.GetBool("correndo") && animator.GetCurrentAnimatorStateInfo(0).IsName("Correndo") && anim.EstadoAtual == "Correndo" && rb.position.x > antesCorrida + .05f, anim.EstadoAtual + " dx=" + (rb.position.x - antesCorrida));
            direcao = 0; correr = false; yield return new WaitForSeconds(.05f);
            s.TentarPular(true); yield return new WaitForSeconds(.15f);
            Verificar(cena + " subida anima uma vez", animator.GetBool("pulando") && animator.GetCurrentAnimatorStateInfo(0).IsName("Pulando") && anim.EstadoAtual == "Pulando" && !anim.PuloPausado && rb.velocity.y > 0, anim.EstadoAtual + " vy=" + rb.velocity.y);
            float limite = Time.time + 1.2f; while (rb.velocity.y > 0 && Time.time < limite) yield return null;
            yield return new WaitForSeconds(.04f);
            Sprite quadro = s.GetComponent<SpriteRenderer>().sprite; float tempo = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            yield return new WaitForSeconds(.12f);
            Verificar(cena + " queda conserva quadro sem reiniciar", anim.PuloPausado && s.GetComponent<SpriteRenderer>().sprite == quadro && Mathf.Abs(animator.GetCurrentAnimatorStateInfo(0).normalizedTime - tempo) < .01f,
                "frame=" + quadro.name + " tempo=" + tempo + " -> " + animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
            limite = Time.time + 2; while (!anim.NoChao && Time.time < limite) yield return null;
            yield return new WaitForSeconds(.05f);
            Verificar(cena + " aterrissar retoma idle", anim.NoChao && !anim.PuloPausado && animator.speed == 1 && anim.EstadoAtual == "Parado", anim.EstadoAtual);
            float x = rb.position.x; s.GetComponent<SpriteRenderer>().flipX = false;
            Verificar(cena + " dash inicia e respeita recarga", s.IniciarDash() && !s.IniciarDash(), "Q; velocidade configurada=" + s.velocidadeDash);
            yield return new WaitForSeconds(.06f);
            Verificar(cena + " dash imprime velocidade e animacao", rb.velocity.x > 11.9f && anim.EstadoAtual == "Dash", "vx=" + rb.velocity.x + " " + anim.EstadoAtual);
            yield return new WaitForSeconds(.19f);
            Verificar(cena + " dash termina", !s.dashAtivo && Mathf.Abs(rb.velocity.x) < .1f, "deslocamento=" + (rb.position.x - x));
            // Ataque real contra alvo temporario: sem dano na preparacao e uma vez por golpe.
            GameObject alvoGo = new GameObject("Alvo_TEST_ONLY"); alvoGo.transform.position = s.transform.position + Vector3.right * 1.2f;
            alvoGo.AddComponent<BoxCollider2D>().size = new Vector2(.3f, .8f); AlvoTemplo alvo = alvoGo.AddComponent<AlvoTemplo>(); alvo.vida = 3;
            Verificar(cena + " componente sozinho nao torna objeto um alvo", AlvoTemplo.Identificar(alvoGo.GetComponent<Collider2D>()) == null, "Identificacao depende da tag");
            alvoGo.tag = "destrutivel1";
            Verificar(cena + " katana inicia", s.AtacarKatana(), "Golpe de 0.5 segundo");
            yield return new WaitForSeconds(.15f);
            Verificar(cena + " katana nao acerta antes do saque", alvo.vida == 3 && animator.GetBool("estaAtacando") && animator.GetCurrentAnimatorStateInfo(0).IsName("Atacando"), "vida=" + alvo.vida);
            yield return new WaitForSeconds(.21f); Captura(cena + "_katana");
            Verificar(cena + " katana acerta no quadro da lamina", alvo.vida == 2 && s.JanelaKatana && anim.EstadoAtual == "Atacando", "vida=" + alvo.vida + " sprite=" + s.GetComponent<SpriteRenderer>().sprite.name);
            yield return new WaitForSeconds(.25f);
            Verificar(cena + " golpe termina sem dano duplicado", alvo.vida == 2 && !s.ataqueAtivo && !s.areaKatanaDireita.enabled, "vida=" + alvo.vida);
            Destroy(alvoGo); mover = null;
        }
        Samurai jogador = FindObjectOfType<Samurai>(); Rigidbody2D corpo = jogador.GetComponent<Rigidbody2D>();
        jogador.transform.position = new Vector3(2, 1, 0); corpo.velocity = Vector2.zero;
        yield return new WaitForSeconds(.2f);
        GameObject parede = new GameObject("ParedeDash_TEST_ONLY"); parede.transform.position = new Vector3(3, 1.5f, 0); parede.AddComponent<BoxCollider2D>().size = new Vector2(.2f, 3);
        jogador.GetComponent<SpriteRenderer>().flipX = false; jogador.IniciarDash(); yield return new WaitForSeconds(.3f);
        Verificar("Dash respeita parede com colisao continua", jogador.GetComponent<Collider2D>().bounds.max.x <= 2.95f, "borda=" + jogador.GetComponent<Collider2D>().bounds.max.x);
        Destroy(parede);
        jogador.transform.position = new Vector3(15, 1.1f, 0); corpo.velocity = Vector2.zero; yield return new WaitForSeconds(.1f); Captura("grade_pedra");
        Verificar("Sem legendas ou HUD textual adicionado", FindObjectsOfType<TextMesh>().Length == 0 && !jogador.painelMissao.gameObject.activeInHierarchy, "Legendas desativadas, referencias preservadas");
        GuardaTemplo guarda = FindObjectOfType<GuardaTemplo>(); AlvoTemplo vida = guarda.GetComponent<AlvoTemplo>(); SpriteRenderer imagem = guarda.GetComponent<SpriteRenderer>();
        jogador.transform.position = guarda.transform.position + Vector3.left * 2.5f; corpo.velocity = Vector2.zero;
        yield return new WaitForSeconds(.05f); Captura("inimigo_original");
        Verificar("Guarda usa personagem da caverna sem vermelho permanente", AssetDatabase.GetAssetPath(imagem.sprite.texture).Contains("Polimento/Guarda") && imagem.color == Color.white, imagem.sprite.name);
        vida.ReceberDano(1, jogador); yield return new WaitForSeconds(.05f);
        Verificar("Hit aplica feedback temporario", imagem.color.g < .5f && vida.vida == 1, imagem.color.ToString());
        yield return new WaitForSeconds(.2f);
        Verificar("Cor volta ao normal depois do hit", imagem.color == Color.white, imagem.color.ToString());
        vida.ReceberDano(20, jogador); yield return new WaitForSeconds(.05f);
        Verificar("Derrota interrompe colisao antes de despawn", guarda != null && vida.Derrotado && !guarda.GetComponent<Collider2D>().enabled, "Feedback antes de remover");
        yield return new WaitForSeconds(.4f);
        Verificar("Inimigo despawna apos intervalo curto", guarda == null, "0.35 segundo");
        jogador.transform.position = new Vector3(2, -6, 0); yield return new WaitForSeconds(1.25f);
        Verificar("Derrota usa tela original", SceneManager.GetActiveScene().name == "GameOver", SceneManager.GetActiveScene().name);
    }
}
#endif

