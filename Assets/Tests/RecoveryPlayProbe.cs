#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

// Executado somente quando solicitado pelo menu de testes; nao entra em builds.
public class RecoveryPlayProbe : MonoBehaviour
{
    [Serializable] public class Check { public string name; public bool passed; public string detail; }
    [Serializable] public class Report
    {
        public string unityVersion;
        public bool completed;
        public List<Check> checks = new List<Check>();
        public List<string> errors = new List<string>();
        public List<string> warnings = new List<string>();
    }
    Report report = new Report();
    Samurai moving;
    string folder;
    bool finished;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BeginIfRequested()
    {
        if (!File.Exists("Library/RecoveryPlayTest.request")) return;
        File.Delete("Library/RecoveryPlayTest.request");
        GameObject go = new GameObject("RecoveryPlayProbe_TEST_ONLY");
        DontDestroyOnLoad(go);
        go.AddComponent<RecoveryPlayProbe>();
    }
    void Start()
    {
        folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Diagnostico/EstruturaSlides/Regressao"));
        Directory.CreateDirectory(folder);
        report.unityVersion = Application.unityVersion;
        Application.logMessageReceived += OnLog;
        StartCoroutine(ProtectedSuite());
    }
    void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            report.errors.Add(message + "\n" + stack);
        else if (type == LogType.Warning && !report.warnings.Contains(message)) report.warnings.Add(message);
    }
    void LateUpdate() { if (moving != null) moving.AplicarMovimento(1, false); }
    IEnumerator ProtectedSuite()
    {
        IEnumerator suite = Suite();
        while (true)
        {
            bool more = false; object next = null;
            try { more = suite.MoveNext(); if (more) next = suite.Current; }
            catch (Exception e) { report.errors.Add(e.ToString()); Finish(false); yield break; }
            if (!more) break;
            yield return next;
        }
        Finish(true);
    }
    void CheckResult(string name, bool passed, string detail)
    {
        report.checks.Add(new Check { name = name, passed = passed, detail = detail });
        File.WriteAllText(Path.Combine(folder, "play_mode_resultado.json"), JsonUtility.ToJson(report, true));
    }
    Samurai Player()
    {
        foreach (Samurai s in FindObjectsOfType<Samurai>()) if (s.enabled) return s;
        throw new Exception("Personagem ativo nao encontrado em " + SceneManager.GetActiveScene().name);
    }
    Collider2D Item(string tag, bool preferRestored)
    {
        foreach (GameObject go in GameObject.FindGameObjectsWithTag(tag))
        {
            if (preferRestored && go.GetComponent<ColetavelRestaurado>() == null) continue;
            foreach (Collider2D c in go.GetComponents<Collider2D>()) if (c.enabled && c.isTrigger) return c;
        }
        throw new Exception("Item real com trigger nao encontrado: " + tag);
    }
    void PutOnPlayer(Collider2D item, Samurai p)
    {
        item.transform.position += p.GetComponent<Collider2D>().bounds.center - item.bounds.center;
    }
    void SetCounter(string name, object value)
    {
        typeof(Samurai).GetField(name, BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, value);
    }
    void ResetTestSession()
    {
        SetCounter("ContadorMoedas", 0); SetCounter("ContadorEspadas", 0); SetCounter("ContadorSkates", 0);
        SetCounter("ContadorVidas", 3); SetCounter("PegouPezinho", false); SetCounter("BonusBotinha", 0f);
    }
    void Capture(string name)
    {
        Camera camera = Camera.main;
        if (camera == null) return;
        RenderTexture old = camera.targetTexture; RenderTexture active = RenderTexture.active;
        float aspect = camera.aspect;
        // Capture na proporcao real da Game View, sem invalidar o layout do Canvas.
        int width = Screen.width, height = Screen.height;
        RenderTexture rt = new RenderTexture(width, height, 24);
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            // Screen Space - Camera atualiza a pose do Canvas ao renderizar.
            // Medir antes disso compara o HUD do frame anterior com a camera ja movida.
            camera.Render();
            foreach (Canvas canvas in FindObjectsOfType<Canvas>())
            {
                bool inside = true;
                string outside = "";
                Camera uiCamera = canvas.worldCamera != null ? canvas.worldCamera : camera;
                foreach (UnityEngine.UI.Graphic graphic in canvas.GetComponentsInChildren<UnityEngine.UI.Graphic>())
                {
                    if (!graphic.enabled) continue;
                    Vector3[] corners = new Vector3[4]; graphic.rectTransform.GetWorldCorners(corners);
                    foreach (Vector3 corner in corners)
                    {
                        // Overlay usa pixels de tela; Camera/World Space usam coordenadas do mundo.
                        Vector3 point = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                            ? new Vector3(corner.x / width, corner.y / height, 1)
                            : uiCamera.WorldToViewportPoint(corner);
                        // Uma margem de um pixel evita falhas por arredondamento na borda do Canvas.
                        bool cornerInside = point.x >= -1f / width && point.x <= 1 + 1f / width
                            && point.y >= -1f / height && point.y <= 1 + 1f / height && point.z > 0;
                        inside &= cornerInside;
                        if (!cornerInside) outside += " " + graphic.name + "=" + point.ToString("F4");
                    }
                }
                CheckResult("hud_dentro_da_tela_" + name, inside, width + "x" + height + outside);
            }
            RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
            File.WriteAllBytes(Path.Combine(folder, "play_" + name + ".png"), texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = old; camera.aspect = aspect; RenderTexture.active = active;
            Destroy(texture); Destroy(rt);
        }
    }
    IEnumerator Suite()
    {
        foreach (string scene in new string[] { "PrimeiraTela", "FAses", "Cena1", "Cena2", "GameOver", "Vitoria1", "Vitoria2", "Vitoria3" })
        {
            ResetTestSession();
            SceneManager.LoadScene(scene);
            yield return new WaitForSeconds(0.6f);
            int missing = 0, sprites = 0;
            foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (Transform tr in go.GetComponentsInChildren<Transform>(true))
            foreach (Component c in tr.GetComponents<Component>())
            {
                if (c == null) missing++;
                SpriteRenderer sr = c as SpriteRenderer;
                if (sr != null && sr.sprite == null) sprites++;
            }
            CheckResult("abrir_" + scene, SceneManager.GetActiveScene().name == scene && missing == 0 && sprites == 0,
                "missingScripts=" + missing + "; spritesNulos=" + sprites);
            Capture(scene);
        }
        ResetTestSession(); SceneManager.LoadScene("Cena1");
        yield return new WaitForSeconds(0.6f);
        Samurai player = Player(); Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        float x = body.position.x;
        moving = player;
        yield return new WaitForSeconds(0.3f);
        moving = null;
        CheckResult("andar_fisica", body.position.x > x + 0.05f, "deslocamento=" + (body.position.x - x));
        player.AplicarMovimento(-1, true);
        CheckResult("correr_e_virar", body.velocity.x < -4.9f && player.GetComponent<SpriteRenderer>().flipX, "vx=" + body.velocity.x);
        player.AplicarMovimento(0, false);
        float vy = body.velocity.y;
        player.TentarPular(true);
        CheckResult("pulo_bloqueado_antes_pezinho", Mathf.Abs(body.velocity.y - vy) < 0.001f, "Regra original do backup");
        body.gravityScale = 0; body.velocity = Vector2.zero;
        int coins = int.Parse(player.UITextMoedas.text);
        Collider2D coin = Item("Moedas", true);
        PutOnPlayer(coin, player);
        yield return new WaitForSeconds(0.15f);
        CheckResult("moeda_colisao_real_sem_duplicar", coin == null && int.Parse(player.UITextMoedas.text) == coins + 1,
            "antes=" + coins + "; depois=" + player.UITextMoedas.text);
        Collider2D foot = Item("Pezinho", false);
        PutOnPlayer(foot, player);
        yield return new WaitForSeconds(0.15f);
        CheckResult("coletar_pezinho_fisica", foot == null, "Item original da cena");
        body.gravityScale = 1.2f;
        float y = body.position.y;
        player.TentarPular(true);
        CheckResult("pulo_velocidade", body.velocity.y > 6.4f, "vy=" + body.velocity.y);
        yield return new WaitForSeconds(0.1f);
        CheckResult("pulo_deslocamento", body.position.y > y, "dy=" + (body.position.y-y));

        ResetTestSession(); SceneManager.LoadScene("Cena1"); yield return new WaitForSeconds(0.5f);
        player = Player(); SetCounter("ContadorMoedas", 49);
        player.ReceberColetavel(Item("Moedas", true));
        yield return new WaitForSeconds(0.5f);
        CheckResult("vitoria_50_moedas", SceneManager.GetActiveScene().name == "Vitoria2", SceneManager.GetActiveScene().name);
        ResetTestSession(); SceneManager.LoadScene("Cena1"); yield return new WaitForSeconds(0.5f);
        player = Player(); SetCounter("ContadorEspadas", 1); SetCounter("ContadorSkates", 3);
        player.ReceberColetavel(Item("Espadas", false));
        yield return new WaitForSeconds(0.5f);
        CheckResult("vitoria_espadas_skates", SceneManager.GetActiveScene().name == "Vitoria1", SceneManager.GetActiveScene().name);
        ResetTestSession(); SceneManager.LoadScene("Cena2"); yield return new WaitForSeconds(0.5f);
        player = Player(); player.ReceberColetavel(Item("Espada2", false));
        yield return new WaitForSeconds(0.5f);
        CheckResult("vitoria_espada2", SceneManager.GetActiveScene().name == "Vitoria3", SceneManager.GetActiveScene().name);
        ResetTestSession(); SceneManager.LoadScene("Cena1"); yield return new WaitForSeconds(0.5f);
        player = Player(); SetCounter("ContadorMoedas", 20);
        player.ReceberColetavel(Item("Caverna", false));
        yield return new WaitForSeconds(0.5f);
        CheckResult("passagem_caverna", SceneManager.GetActiveScene().name == "Cena2", SceneManager.GetActiveScene().name);
        ResetTestSession(); SceneManager.LoadScene("Cena1"); yield return new WaitForSeconds(0.5f);
        player = Player(); SetCounter("ContadorVidas", 2);
        player.ReceberColetavel(Item("Limbo", false));
        yield return new WaitForSeconds(0.5f);
        CheckResult("morte_recarrega_fase", SceneManager.GetActiveScene().name == "Cena1" && Player().UITextVidas.text == "1", "Uma vida restante");
        player = Player(); player.ReceberColetavel(Item("Limbo", false));
        yield return new WaitForSeconds(0.5f);
        CheckResult("game_over", SceneManager.GetActiveScene().name == "GameOver", SceneManager.GetActiveScene().name);
        CheckResult("sem_excecoes_runtime", report.errors.Count == 0, "erros=" + report.errors.Count);
    }
    void Finish(bool completed)
    {
        if (finished) return;
        finished = true; moving = null; report.completed = completed;
        Application.logMessageReceived -= OnLog;
        File.WriteAllText(Path.Combine(folder, "play_mode_resultado.json"), JsonUtility.ToJson(report, true));
        bool passed = completed && report.errors.Count == 0;
        foreach (Check check in report.checks) passed &= check.passed;
        Debug.Log("RECOVERY_PLAY_TEST_COMPLETE passed=" + passed + " checks=" + report.checks.Count);
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-batchmode") >= 0) EditorApplication.Exit(passed ? 0 : 1);
        else EditorApplication.isPlaying = false;
    }
}
#endif
