using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

// Edicao pontual das cenas: conserva os Transform e os colliders existentes.
public static class PolirSamurai
{
    const string Pasta = "Assets/Bimestres34/Polimento";
    const string Velho = "Assets/FREE_Samurai 2D Pixel Art v1.2/FREE_Samurai 2D Pixel Art v1.2/Sprites/";
    static readonly HashSet<string> importadas = new HashSet<string>();
    static readonly List<string> ajustes = new List<string>();
    static Sprite SpriteDe(string path) { return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).First(); }
    static void Importacao(string path, bool retangulo)
    {
        if (!importadas.Add(path + retangulo)) return;
        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;
        TextureImporterSettings config = new TextureImporterSettings(); imp.ReadTextureSettings(config);
        bool mudou = imp.filterMode != FilterMode.Point || imp.mipmapEnabled || imp.textureCompression != TextureImporterCompression.Uncompressed ||
            (retangulo && config.spriteMeshType != SpriteMeshType.FullRect);
        if (!mudou) return;
        if (retangulo) config.spriteMeshType = SpriteMeshType.FullRect;
        imp.SetTextureSettings(config);
        imp.filterMode = FilterMode.Point; imp.mipmapEnabled = false;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();
    }
    static AnimationClip Clipe(string pasta, string nome, string origem, int lado, int ppu, Vector2 pivo, bool loop)
    {
        string path = pasta + "/" + Path.GetFileName(origem);
        if (!File.Exists(path)) File.Copy(origem, path);
        AssetDatabase.ImportAsset(path);
        TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(path);
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int n = tex.width / lado;
        imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Multiple;
        imp.spritePixelsPerUnit = ppu; imp.filterMode = FilterMode.Point; imp.mipmapEnabled = false;
        imp.textureCompression = TextureImporterCompression.Uncompressed; imp.npotScale = TextureImporterNPOTScale.None;
        SpriteMetaData[] quadros = new SpriteMetaData[n];
        for (int i = 0; i < n; i++) quadros[i] = new SpriteMetaData { name = nome + "_" + i, rect = new Rect(i * lado, 0, lado, lado), alignment = 9, pivot = pivo };
        imp.spritesheet = quadros; imp.SaveAndReimport();
        string destino = pasta + "/" + nome + ".anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(destino);
        bool novo = clip == null; if (novo) clip = new AnimationClip();
        clip.frameRate = 12;
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => int.Parse(s.name.Split('_').Last())).ToArray();
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[n + 1];
        for (int i = 0; i < n; i++) keys[i] = new ObjectReferenceKeyframe { time = i / 12f, value = sprites[i] };
        keys[n] = new ObjectReferenceKeyframe { time = n / 12f, value = sprites[n - 1] };
        AnimationUtility.SetObjectReferenceCurve(clip, new EditorCurveBinding { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" }, keys);
        AnimationClipSettings config = AnimationUtility.GetAnimationClipSettings(clip); config.loopTime = loop; AnimationUtility.SetAnimationClipSettings(clip, config);
        if (novo) AssetDatabase.CreateAsset(clip, destino); else EditorUtility.SetDirty(clip);
        return clip;
    }
    static AnimatorController Controlador(bool guarda)
    {
        string sub = Pasta + (guarda ? "/Guarda" : "/Samurai"); Directory.CreateDirectory(sub); AssetDatabase.Refresh();
        string path = sub + "/Animacoes.controller";
        AnimatorController controlador = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controlador == null) controlador = AnimatorController.CreateAnimatorControllerAtPath(path);
        if (controlador.layers.Length == 0) controlador.AddLayer("Base Layer");
        if (!guarda && !controlador.parameters.Any(p => p.name == "VelocidadeX")) controlador.AddParameter("VelocidadeX", AnimatorControllerParameterType.Float);
        string[] nomes = guarda ? new[] { "Parado", "Correndo", "Ataque", "Ferido" } : new[] { "Parado", "Andando", "Correndo", "Pulando", "Atacando", "Defendendo", "Dash" };
        string[] folhas = guarda ? new[] { "IDLE", "RUN", "ATTACK 1", "HURT" } : new[] { "Idle", "Walk", "Run", "Jump", "Attack_1", "Shield", "Run" };
        for (int i = 0; i < nomes.Length; i++)
        {
            AnimationClip clip = nomes[i] == "Dash" ? AssetDatabase.LoadAssetAtPath<AnimationClip>(sub + "/Correndo.anim") :
                Clipe(sub, nomes[i], (guarda ? Velho : "Assets/Samurai/") + folhas[i] + ".png", guarda ? 96 : 128, guarda ? 32 : 64,
                    guarda ? new Vector2(.5f, 1f / 3) : new Vector2(.5f, .32f), guarda ? i < 2 : i < 3);
            var maquina = controlador.layers[0].stateMachine;
            AnimatorState estado = maquina.states.Select(x => x.state).FirstOrDefault(x => x.name == nomes[i]);
            if (estado == null) estado = maquina.AddState(nomes[i]);
            estado.motion = clip; if (i == 0) maquina.defaultState = estado;
        }
        EditorUtility.SetDirty(controlador); AssetDatabase.SaveAssets();
        return controlador;
    }
    static Text Texto(Transform pai, string nome, string conteudo)
    {
        GameObject g = new GameObject(nome, typeof(RectTransform), typeof(Text)); g.transform.SetParent(pai, false);
        Text t = g.GetComponent<Text>(); t.font = Resources.GetBuiltinResource<Font>("Arial.ttf"); t.text = conteudo; t.color = Color.white; return t;
    }
    static void Hud(Samurai s)
    {
        HudRestaurado hud = UnityEngine.Object.FindObjectsOfType<HudRestaurado>().FirstOrDefault(h => h.jogador == s || h.GetComponentInParent<Samurai>() == s);
        if (hud == null)
        {
            // A interface textual da missao fica arquivada/inativa, preservando suas referencias.
            Canvas anterior = s.painelMissao != null ? s.painelMissao.GetComponentInParent<Canvas>() : null;
            if (anterior != null) anterior.gameObject.SetActive(false);
            GameObject g = new GameObject("HUD original - Samurai", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(HudRestaurado));
            g.transform.SetParent(s.transform, false); hud = g.GetComponent<HudRestaurado>();
            s.UITextMoedas = Texto(g.transform, "Moedas", "0"); s.UITextEspadas = Texto(g.transform, "Espadas", "0");
            s.UITextSkates = Texto(g.transform, "Skates", "0"); s.UITextVidas = Texto(g.transform, "Vidas", "3");
            Texto(g.transform, "Text", "Samurai"); Texto(g.transform, "Text (1)", "x");
        }
        hud.jogador = s;
        hud.retrato = SpriteDe("Assets/ChatGPT Image 14 de mai. de 2026, 21_51_20-Photoroom.png");
        hud.coletaveis = SpriteDe("Assets/image-Photoroom (3) (1)-Photoroom.png");
        hud.projeteis = s.projeteisMissao.Select(p => p.GetComponentInChildren<SpriteRenderer>(true).sprite).ToArray();
    }
    static void Katana(Samurai s, bool esquerda)
    {
        Collider2D c = esquerda ? s.areaKatanaEsquerda : s.areaKatanaDireita;
        if (c == null)
        {
            GameObject g = new GameObject(esquerda ? "Katana esquerda" : "Katana direita"); g.transform.SetParent(s.transform, false);
            g.transform.localPosition = new Vector3(esquerda ? -.62f : .62f, 0, 0);
            CircleCollider2D area = g.AddComponent<CircleCollider2D>(); area.radius = .55f; c = area;
            g.AddComponent<AreaKatana>().jogador = s;
        }
        c.isTrigger = true; c.enabled = false;
        if (esquerda) s.areaKatanaEsquerda = c; else s.areaKatanaDireita = c;
    }
    // Renderiza as pecas em escala uniforme; a raiz conserva escala, posicao e fisica.
    static void Repetir(SpriteRenderer sr, float fator)
    {
        if (sr.transform.Find("Arte em pecas proporcionais") != null) return;
        Bounds antes = sr.bounds;
        Importacao(AssetDatabase.GetAssetPath(sr.sprite.texture), true);
        GameObject g = new GameObject("Arte em pecas proporcionais");
        g.transform.SetParent(sr.transform, false); g.transform.position = antes.center;
        Vector3 escala = sr.transform.lossyScale;
        g.transform.localScale = new Vector3(fator / Mathf.Abs(escala.x), fator / Mathf.Abs(escala.y), 1);
        SpriteRenderer novo = g.AddComponent<SpriteRenderer>(); novo.sprite = sr.sprite;
        novo.sortingLayerID = sr.sortingLayerID; novo.sortingOrder = sr.sortingOrder;
        novo.color = sr.color; novo.flipX = sr.flipX; novo.flipY = sr.flipY;
        novo.drawMode = SpriteDrawMode.Tiled;
        novo.size = new Vector2(antes.size.x / fator, antes.size.y / fator);
        sr.enabled = false; ajustes.Add(sr.name + ": repeticao sem deformar, raiz preservada");
    }
    static void Proporcional(SpriteRenderer sr, bool cobrir)
    {
        Vector2 original = sr.sprite.rect.size / sr.sprite.pixelsPerUnit;
        Vector3 escala = sr.transform.lossyScale;
        float fator = cobrir ? Mathf.Max(Mathf.Abs(escala.x), Mathf.Abs(escala.y)) : Mathf.Min(Mathf.Abs(escala.x), Mathf.Abs(escala.y));
        Importacao(AssetDatabase.GetAssetPath(sr.sprite.texture), true);
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(original.x * fator / Mathf.Abs(escala.x), original.y * fator / Mathf.Abs(escala.y));
        if (sr.GetComponent<Animator>() != null)
        {
            ProporcaoAnimada ajuste = sr.GetComponent<ProporcaoAnimada>();
            if (ajuste == null) ajuste = sr.gameObject.AddComponent<ProporcaoAnimada>();
            ajuste.escalaVisual = fator;
        }
        ajustes.Add(sr.name + ": proporcao original, escala da raiz preservada");
    }
    static void Visuais()
    {
        foreach (GameObject raiz in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        foreach (SpriteRenderer sr in raiz.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (!sr.enabled || sr.sprite == null || sr.GetComponent<Samurai>() != null) continue;
            string path = AssetDatabase.GetAssetPath(sr.sprite.texture);
            // Molduras originais serao imagens UI com preserveAspect; o quadrado puro nao tem arte para deformar.
            if (path == "Assets/Square.png" || sr.name == "ChatGPT Image 14 de mai. de 2026, 21_51_20-Photoroom" || sr.name == "image-Photoroom (3) (1)-Photoroom") continue;
            Vector3 e = sr.transform.lossyScale;
            if (Mathf.Abs(Mathf.Abs(e.x) - Mathf.Abs(e.y)) < .0001f || Mathf.Abs(e.x * e.y) < .00001f) continue;
            if (path.Contains("/1 Tiles/") || sr.name == "Grade do desfiladeiro" || sr.name == "Muralha do santuario" || path.Contains("mainlev_build"))
                Repetir(sr, Mathf.Min(Mathf.Abs(e.x), Mathf.Abs(e.y)));
            else Proporcional(sr, path.Contains("Background"));
        }
    }
    static void Cofre()
    {
        GameObject cofre = GameObject.Find("Pergaminho da cidade"); if (cofre == null) return;
        string path = Pasta + "/Cofre.png";
        if (!File.Exists(path)) File.Copy("Assets/4 Animated objects/Chest_open.png", path);
        AssetDatabase.ImportAsset(path);
        TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Multiple;
        imp.spritePixelsPerUnit = 32; imp.filterMode = FilterMode.Point; imp.mipmapEnabled = false;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.spritesheet = new[] { new SpriteMetaData { name = "Cofre", rect = new Rect(0, 0, 37, 22), alignment = 0, pivot = new Vector2(.5f, .5f) } }; imp.SaveAndReimport();
        SpriteRenderer sr = cofre.GetComponent<SpriteRenderer>(); sr.sprite = SpriteDe(path);
        Vector3 e = sr.transform.lossyScale;
        Importacao(path, true); sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(1.2f / Mathf.Abs(e.x), (1.2f * 22 / 37) / Mathf.Abs(e.y));
        ajustes.Add("Cofre: quadro de 37x22 em vez da folha inteira de animacao");
    }
    [MenuItem("Tools/Samurai Boy/Aplicar polimento solicitado")]
    public static void Aplicar()
    {
        if (Application.unityVersion != "2017.4.40f1") throw new Exception("Use Unity 2017.4.40f1.");
        Directory.CreateDirectory(Pasta); AssetDatabase.Refresh();
        AnimatorController samurai = Controlador(false), guarda = Controlador(true);
        GameObject[] tiros = new[] { "Kunai", "SeloEspiritual", "Bomba" }.Select(n => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Bimestres34/" + n + ".prefab")).ToArray();
        AjustarPrefabs();
        foreach (string cena in new[] { "Assets/Cena1.unity", "Assets/Cena2.unity", "Assets/Bimestres34/TravessiaDoTemplo.unity" })
        {
            EditorSceneManager.OpenScene(cena);
            tiros = new[] { "Kunai", "SeloEspiritual", "Bomba" }.Select(n => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Bimestres34/" + n + ".prefab")).ToArray();
            samurai = AssetDatabase.LoadAssetAtPath<AnimatorController>(Pasta + "/Samurai/Animacoes.controller");
            guarda = AssetDatabase.LoadAssetAtPath<AnimatorController>(Pasta + "/Guarda/Animacoes.controller");
            Samurai s = UnityEngine.Object.FindObjectsOfType<Samurai>().First(x => x.enabled);
            s.combateHabilitado = true; s.projeteisMissao = tiros;
            s.GetComponent<Animator>().runtimeAnimatorController = samurai;
            if (s.GetComponent<AnimacaoSamurai>() == null) s.gameObject.AddComponent<AnimacaoSamurai>();
            s.GetComponent<Rigidbody2D>().collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Katana(s, true); Katana(s, false); Hud(s);
            foreach (GuardaTemplo g in UnityEngine.Object.FindObjectsOfType<GuardaTemplo>())
            {
                SpriteRenderer sr = g.GetComponent<SpriteRenderer>(); sr.color = Color.white;
                sr.sprite = SpriteDe(Pasta + "/Guarda/IDLE.png");
                Animator a = g.GetComponent<Animator>(); if (a == null) a = g.gameObject.AddComponent<Animator>(); a.runtimeAnimatorController = guarda;
                g.GetComponent<AlvoTemplo>().atrasoDestruicao = .35f;
            }
            // O inimigo original da caverna mantem seu controlador e ganha recepcao de dano.
            foreach (Animator a in UnityEngine.Object.FindObjectsOfType<Animator>())
                if (a.CompareTag("Limbo") && a.GetComponent<AlvoTemplo>() == null)
                { AlvoTemplo alvo = a.gameObject.AddComponent<AlvoTemplo>(); alvo.vida = 3; alvo.atrasoDestruicao = .35f; }
            if (s.modoMissao)
            {
                foreach (TextMesh t in UnityEngine.Object.FindObjectsOfType<TextMesh>()) t.gameObject.SetActive(false);
                if (s.GetComponent<ResultadoTemplo>() == null) s.gameObject.AddComponent<ResultadoTemplo>();
            }
            Visuais(); if (s.modoMissao) Cofre();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
        AssetDatabase.SaveAssets();
        string pasta = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Diagnostico/Polimento")); Directory.CreateDirectory(pasta);
        File.WriteAllLines(Path.Combine(pasta, "ajustes_visuais.txt"), ajustes.ToArray());
        Debug.Log("POLIMENTO_APLICADO ajustes=" + ajustes.Count);
    }
    static void AjustarPrefabs()
    {
        foreach (string nome in new[] { "Kunai", "SeloEspiritual", "Bomba", "DisparoGuarda" })
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Bimestres34/" + nome + ".prefab");
            foreach (SpriteRenderer sr in prefab.GetComponentsInChildren<SpriteRenderer>(true))
            { Proporcional(sr, false); EditorUtility.SetDirty(sr); }
        }
        AssetDatabase.SaveAssets();
    }
    public static void RepararProjeteisETestar()
    {
        AjustarPrefabs();
        foreach (string cena in new[] { "Assets/Cena1.unity", "Assets/Cena2.unity", "Assets/Bimestres34/TravessiaDoTemplo.unity" })
        {
            EditorSceneManager.OpenScene(cena);
            foreach (Samurai s in UnityEngine.Object.FindObjectsOfType<Samurai>()) if (s.enabled)
                s.projeteisMissao = new[] { "Kunai", "SeloEspiritual", "Bomba" }.Select(n => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Bimestres34/" + n + ".prefab")).ToArray();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
        Testar();
    }
    public static void AjustarRecortesETestar()
    {
        foreach (string cena in new[] { "Assets/Cena1.unity", "Assets/Cena2.unity" })
        {
            EditorSceneManager.OpenScene(cena);
            foreach (Animator a in UnityEngine.Object.FindObjectsOfType<Animator>())
            {
                SpriteRenderer sr = a.GetComponent<SpriteRenderer>();
                if (sr != null && sr.drawMode == SpriteDrawMode.Sliced) Proporcional(sr, false);
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
        Testar();
    }
    public static void Testar()
    {
        AuditarReferencias();
        EditorSceneManager.OpenScene("Assets/Bimestres34/TravessiaDoTemplo.unity");
        File.WriteAllText("Library/Polimento.request", "testar"); EditorApplication.isPlaying = true;
    }
    class EstiloTexto
    {
        public string fonte, conteudo; public Color cor; public FontStyle estilo;
        public HorizontalWrapMode horizontal; public VerticalWrapMode vertical; public float espaco;
        public EstiloTexto(Text t) { fonte = AssetDatabase.GetAssetPath(t.font); conteudo = t.text; cor = t.color; estilo = t.fontStyle; horizontal = t.horizontalOverflow; vertical = t.verticalOverflow; espaco = t.lineSpacing; }
        public void Aplicar(Text t, bool conteudoOriginal)
        {
            Font f = AssetDatabase.LoadAssetAtPath<Font>(fonte); if (f != null) t.font = f;
            t.color = cor; t.fontStyle = estilo; if (conteudoOriginal) t.text = conteudo;
            t.horizontalOverflow = horizontal; t.verticalOverflow = vertical; t.lineSpacing = espaco;
        }
    }
    public static void AcabamentoETestar()
    {
        EditorSceneManager.OpenScene("Assets/Cena1.unity"); Samurai antigo = UnityEngine.Object.FindObjectsOfType<Samurai>().First(s => s.enabled);
        EstiloTexto[] estilos = new[] { antigo.UITextMoedas, antigo.UITextEspadas, antigo.UITextSkates, antigo.UITextVidas }.Select(t => new EstiloTexto(t)).ToArray();
        Dictionary<string, EstiloTexto> rotulos = new Dictionary<string, EstiloTexto>();
        foreach (Text t in antigo.GetComponentsInChildren<Text>(true)) if (t.name == "Text" || t.name == "Text (1)") rotulos[t.name] = new EstiloTexto(t);
        EditorSceneManager.OpenScene("Assets/Bimestres34/TravessiaDoTemplo.unity"); Samurai novo = UnityEngine.Object.FindObjectOfType<Samurai>();
        Text[] campos = { novo.UITextMoedas, novo.UITextEspadas, novo.UITextSkates, novo.UITextVidas };
        for (int i = 0; i < campos.Length; i++) estilos[i].Aplicar(campos[i], false);
        foreach (Text t in novo.GetComponentsInChildren<Text>(true)) if (rotulos.ContainsKey(t.name)) rotulos[t.name].Aplicar(t, true);
        // Cada bloco de piso usa uma peca inteira alinhada ao topo fisico.
        foreach (GameObject raiz in EditorSceneManager.GetActiveScene().GetRootGameObjects()) if (raiz.name.StartsWith("Piso "))
        {
            Transform arte = raiz.transform.Find("Arte em pecas proporcionais");
            if (arte == null) continue; SpriteRenderer sr = arte.GetComponent<SpriteRenderer>();
            sr.size = Vector2.one; Vector3 p = arte.position; p.y = raiz.GetComponent<Collider2D>().bounds.max.y - .5f; arte.position = p;
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Bimestres34/TravessiaDoTemplo.unity");
        File.WriteAllText("Library/PolimentoHUD.request", "testar"); EditorApplication.isPlaying = true;
    }
    public static void AbrirJogo() { EditorSceneManager.OpenScene("Assets/PrimeiraTela.unity"); }
    [Serializable] public class Auditoria
    {
        public string unity; public int cenas, componentes, prefabs; public List<string> erros = new List<string>();
    }
    public static void AuditarReferencias()
    {
        Auditoria r = new Auditoria(); r.unity = Application.unityVersion;
        foreach (EditorBuildSettingsScene entrada in EditorBuildSettings.scenes)
        {
            if (!entrada.enabled) continue; var cena = EditorSceneManager.OpenScene(entrada.path); r.cenas++;
            foreach (GameObject g in cena.GetRootGameObjects()) AuditarObjeto(g, entrada.path, r);
        }
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Bimestres34" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid); r.prefabs++;
            AuditarObjeto(AssetDatabase.LoadAssetAtPath<GameObject>(path), path, r);
        }
        File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "../../Diagnostico/Polimento/referencias.json")), JsonUtility.ToJson(r, true));
        if (r.erros.Count > 0) throw new Exception("Referencias invalidas: " + r.erros.Count);
    }
    static void AuditarObjeto(GameObject g, string origem, Auditoria r)
    {
        foreach (Transform t in g.GetComponentsInChildren<Transform>(true)) foreach (Component c in t.GetComponents<Component>())
        {
            r.componentes++; if (c == null) { r.erros.Add(origem + ": script ausente em " + t.name); continue; }
            SerializedObject so = new SerializedObject(c); SerializedProperty p = so.GetIterator();
            while (p.NextVisible(true)) if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue == null && p.objectReferenceInstanceIDValue != 0)
                r.erros.Add(origem + "/" + t.name + "/" + p.propertyPath);
            Samurai s = c as Samurai;
            if (s != null && s.enabled && (s.UITextMoedas == null || s.UITextEspadas == null || s.UITextSkates == null || s.UITextVidas == null || s.GetComponent<AnimacaoSamurai>() == null))
                r.erros.Add(origem + ": Samurai/HUD incompleto");
        }
    }
}
