using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

// Configuracoes no Inspector: menus 02x08/09, UI 02x07, tags e colliders.
public static class AuditoriaCompleta
{
    static string Pasta { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "../../Diagnostico/AuditoriaCompleta")); } }
    static IEnumerable<T> Componentes<T>() where T : Component
    {
        return EditorSceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true));
    }
    static void CriarTags()
    {
        SerializedObject projeto = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty lista = projeto.FindProperty("tags");
        foreach (string tag in new[] { "botao_start", "botao_fase1", "botao_fase2", "botao_templo", "botao_menu", "reiniciar", "fechar" })
        {
            bool existe = false;
            for (int i = 0; i < lista.arraySize; i++) existe |= lista.GetArrayElementAtIndex(i).stringValue == tag;
            if (existe) continue;
            int n = lista.arraySize; lista.InsertArrayElementAtIndex(n); lista.GetArrayElementAtIndex(n).stringValue = tag;
        }
        projeto.ApplyModifiedProperties();
    }
    static void AreaMenu(SpriteRenderer arte, string tag, Vector2 centro, Vector2 tamanho)
    {
        GameObject botao = GameObject.FindGameObjectWithTag(tag);
        if (botao == null) botao = new GameObject("Clique - " + tag);
        botao.tag = tag;
        Bounds b = arte.bounds;
        botao.transform.position = new Vector3(b.min.x + b.size.x * centro.x, b.min.y + b.size.y * centro.y, b.center.z - .01f);
        BoxCollider2D colisor = botao.GetComponent<BoxCollider2D>();
        if (colisor == null) colisor = botao.AddComponent<BoxCollider2D>();
        colisor.size = new Vector2(b.size.x * tamanho.x, b.size.y * tamanho.y);
        // Sem trigger: OnMouseDown nao depende da opcao global queriesHitTriggers.
        colisor.isTrigger = false;
        if (botao.GetComponent<ClickAsset>() == null) botao.AddComponent<ClickAsset>();
    }
    static void PrepararMenu(string cena)
    {
        SpriteRenderer arte = Componentes<SpriteRenderer>().Where(s => s.enabled && s.sprite != null)
            .OrderByDescending(s => s.bounds.size.x * s.bounds.size.y).FirstOrDefault();
        if (arte == null) throw new Exception("Menu sem imagem: " + cena);
        if (cena == "PrimeiraTela")
        {
            AreaMenu(arte, "botao_start", new Vector2(.21f, .28f), new Vector2(.25f, .10f));
            AreaMenu(arte, "fechar", new Vector2(.21f, .13f), new Vector2(.25f, .10f));
        }
        else if (cena == "FAses")
        {
            AreaMenu(arte, "botao_fase1", new Vector2(.30f, .25f), new Vector2(.23f, .09f));
            AreaMenu(arte, "botao_fase2", new Vector2(.70f, .25f), new Vector2(.23f, .09f));
            AreaMenu(arte, "botao_menu", new Vector2(.50f, .105f), new Vector2(.40f, .10f));
        }
        else
        {
            Vector2 centro = cena == "Vitoria3" ? new Vector2(.70f, .23f) :
                cena == "Vitoria2" ? new Vector2(.16f, .10f) :
                cena == "Vitoria1" ? new Vector2(.18f, .30f) : new Vector2(.20f, .21f);
            AreaMenu(arte, "reiniciar", centro, new Vector2(cena == "Vitoria3" ? .37f : .32f, .11f));
        }
    }
    static void AnimacaoInimigo(string caminho)
    {
        AnimatorController controlador = AssetDatabase.LoadAssetAtPath<AnimatorController>(caminho);
        string[] nomes = { "Parado", "Correndo", "Ataque", "Ferido" };
        string[] parametros = { "andando", "estaAtacando", "ferido" };
        foreach (string p in parametros)
            if (!controlador.parameters.Any(x => x.name == p)) controlador.AddParameter(p, AnimatorControllerParameterType.Bool);
        AnimatorStateMachine maquina = controlador.layers[0].stateMachine;
        for (int i = 0; i < nomes.Length; i++)
        {
            AnimatorState estado = maquina.states.Select(s => s.state).First(s => s.name == nomes[i]);
            string nome = "Slides -> " + nomes[i];
            AnimatorStateTransition transicao = maquina.anyStateTransitions.FirstOrDefault(t => t.name == nome);
            if (transicao == null) { transicao = maquina.AddAnyStateTransition(estado); transicao.name = nome; }
            transicao.hasExitTime = false; transicao.duration = 0; transicao.canTransitionToSelf = false;
            foreach (AnimatorCondition c in transicao.conditions) transicao.RemoveCondition(c);
            for (int j = 0; j < parametros.Length; j++) transicao.AddCondition(i == j + 1 ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, parametros[j]);
        }
        EditorUtility.SetDirty(controlador);
    }
    [MenuItem("Samurai Boy/Conferir e preparar menus e HUD")]
    public static void Aplicar()
    {
        if (Application.unityVersion != "2017.4.40f1") throw new Exception("Use Unity 2017.4.40f1.");
        if ((Array.IndexOf(Environment.GetCommandLineArgs(), "-batchmode") < 0) && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        CriarTags();
        AnimacaoInimigo("Assets/Bimestres34/Polimento/Guarda/Animacoes.controller");
        AnimacaoInimigo("Assets/Bimestres34/OldSamurai/OldSamurai.controller");
        GameObject chefe = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Bimestres34/OldSamurai/OldSamurai.prefab");
        chefe.tag = "inimigo02"; EditorUtility.SetDirty(chefe);
        foreach (EditorBuildSettingsScene entrada in EditorBuildSettings.scenes)
        {
            if (!entrada.enabled) continue;
            EditorSceneManager.OpenScene(entrada.path);
            string nome = EditorSceneManager.GetActiveScene().name;
            HudRestaurado hud = Componentes<HudRestaurado>().FirstOrDefault(h => h.isActiveAndEnabled);
            if (hud != null) PrepararHud(hud);
            else PrepararMenu(nome);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); EditorSceneManager.SaveOpenScenes();
        }
        AssetDatabase.SaveAssets(); Validar();
    }
    [Serializable] class Item { public string origem, objeto, tag, script; public bool ativo; }
    [Serializable] class Relatorio
    {
        public string unity;
        public int cenas, prefabs, componentes;
        public List<string> erros = new List<string>();
        public List<Item> scriptsEmObjetos = new List<Item>();
        public List<string> scriptsDoProjeto = new List<string>();
    }
    static void Inspecionar(GameObject raiz, string origem, Relatorio r)
    {
        foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
        foreach (Component componente in t.GetComponents<Component>())
        {
            r.componentes++;
            if (componente == null) { r.erros.Add(origem + "/" + t.name + ": script ausente"); continue; }
            SerializedObject serializado = new SerializedObject(componente);
            SerializedProperty campo = serializado.GetIterator();
            while (campo.NextVisible(true))
                if (campo.propertyType == SerializedPropertyType.ObjectReference && campo.objectReferenceValue == null && campo.objectReferenceInstanceIDValue != 0)
                    r.erros.Add(origem + "/" + t.name + ": referencia quebrada " + campo.propertyPath);
            MonoBehaviour script = componente as MonoBehaviour;
            if (script != null)
                r.scriptsEmObjetos.Add(new Item { origem = origem, objeto = t.name, tag = t.tag,
                    script = AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(script)), ativo = script.isActiveAndEnabled });
            ClickAsset botao = componente as ClickAsset;
            if (botao != null && (botao.GetComponent<Collider2D>() == null || !botao.GetComponent<Collider2D>().enabled))
                r.erros.Add(origem + ": botao sem colisor " + t.tag);
            AreaKatana area = componente as AreaKatana;
            if (area != null && (area.jogador == null || !area.GetComponent<Collider2D>().isTrigger || !t.tag.StartsWith("areaAtaque")))
                r.erros.Add(origem + ": area katana mal configurada");
            Samurai jogador = componente as Samurai;
            if (jogador != null && jogador.enabled && (!t.CompareTag("Player") || jogador.UITextVidas == null || jogador.UITextMoedas == null ||
                jogador.UITextEspadas == null || jogador.UITextSkates == null || jogador.GetComponent<Rigidbody2D>() == null))
                r.erros.Add(origem + ": personagem sem dependencias");
            HudRestaurado hud = componente as HudRestaurado;
            if (hud != null && (hud.iconesProjeteis == null || hud.iconesProjeteis.Length != 3 || hud.iconesProjeteis.Any(i => i == null) ||
                hud.GetComponent<Canvas>().renderMode != RenderMode.ScreenSpaceCamera || hud.GetComponent<Canvas>().worldCamera == null ||
                hud.GetComponentInParent<Samurai>() != hud.jogador))
                r.erros.Add(origem + ": HUD fora da estrutura 02x07");
        }
    }
    public static void Validar()
    {
        Relatorio r = new Relatorio { unity = Application.unityVersion };
        foreach (string p in Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories)) r.scriptsDoProjeto.Add(p.Replace('\\', '/'));
        foreach (EditorBuildSettingsScene entrada in EditorBuildSettings.scenes)
        {
            if (!entrada.enabled) continue;
            if (!File.Exists(entrada.path)) { r.erros.Add("Cena ausente: " + entrada.path); continue; }
            EditorSceneManager.OpenScene(entrada.path); r.cenas++;
            foreach (GameObject raiz in EditorSceneManager.GetActiveScene().GetRootGameObjects()) Inspecionar(raiz, entrada.path, r);
        }
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid); r.prefabs++;
            Inspecionar(AssetDatabase.LoadAssetAtPath<GameObject>(p), p, r);
        }
        Directory.CreateDirectory(Pasta); File.WriteAllText(Path.Combine(Pasta, "auditoria_unity.json"), JsonUtility.ToJson(r, true));
        if (r.erros.Count > 0) throw new Exception(string.Join("; ", r.erros.ToArray()));
        EditorSceneManager.OpenScene("Assets/PrimeiraTela.unity");
    }
    public static void Testar()
    {
        EditorSceneManager.OpenScene("Assets/PrimeiraTela.unity");
        File.WriteAllText("Library/AuditoriaCompleta.request", "testar"); EditorApplication.isPlaying = true;
    }
    static void PrepararHud(HudRestaurado hud)
    {
        if (hud.jogador == null) hud.jogador = hud.GetComponentInParent<Samurai>();
        if (hud.jogador == null) return;
        foreach (SpriteRenderer original in hud.jogador.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (original.name == "ChatGPT Image 14 de mai. de 2026, 21_51_20-Photoroom")
            { if (hud.retrato == null) hud.retrato = original.sprite; original.enabled = false; }
            if (original.name == "image-Photoroom (3) (1)-Photoroom")
            { if (hud.coletaveis == null) hud.coletaveis = original.sprite; original.enabled = false; }
        }
        Canvas canvas = hud.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main; canvas.planeDistance = 1f;
        // A camera tem escala mundial unitaria; evita ampliar o Canvas pela escala do corpo.
        // Continua descendente do Samurai, sem mudar escalas nem referencias.
        canvas.transform.SetParent(canvas.worldCamera.transform, false);
        CanvasScaler escala = hud.GetComponent<CanvasScaler>();
        if (escala == null) escala = hud.gameObject.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1280, 720);
        escala.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        escala.matchWidthOrHeight = .5f;
        Imagem(hud.transform, "HUD retrato original", hud.retrato, new Vector2(12, -8), new Vector2(400, 133.333f), false);
        Imagem(hud.transform, "HUD coletaveis originais", hud.coletaveis, new Vector2(82, -43), new Vector2(180, 55.5f), false);
        // Centros medidos na arte: moeda 139/720, espada 369/720, skate 600/720.
        Texto(hud.jogador.UITextMoedas, 101.75f, 96, 30, 24, TextAnchor.UpperCenter);
        Texto(hud.jogador.UITextEspadas, 159.25f, 96, 30, 24, TextAnchor.UpperCenter);
        Texto(hud.jogador.UITextSkates, 217, 96, 30, 24, TextAnchor.UpperCenter);
        Texto(hud.jogador.UITextVidas, 85, 15, 30, 26, TextAnchor.UpperLeft);
        foreach (Text texto in hud.GetComponentsInChildren<Text>())
        {
            if (texto.name == "Text") Texto(texto, 120, 12, 170, 30, TextAnchor.UpperLeft);
            else if (texto.name == "Text (1)") Texto(texto, 68, 15, 24, 26, TextAnchor.UpperLeft);
        }
        hud.iconesProjeteis = new Image[3];
        if (hud.projeteis != null) for (int i = 0; i < Mathf.Min(3, hud.projeteis.Length); i++)
            hud.iconesProjeteis[i] = Imagem(hud.transform, "Projetil " + (i + 1), hud.projeteis[i], new Vector2(25, -113 - 38 * i), new Vector2(32, 32), true);
        EditorUtility.SetDirty(hud);
    }
    static Image Imagem(Transform pai, string nome, Sprite sprite, Vector2 posicao, Vector2 tamanho, bool frente)
    {
        if (sprite == null) return null;
        Transform existente = pai.Find(nome);
        GameObject objeto = existente != null ? existente.gameObject : new GameObject(nome, typeof(RectTransform), typeof(Image));
        if (existente == null) objeto.transform.SetParent(pai, false);
        if (!frente) objeto.transform.SetAsFirstSibling();
        Image imagem = objeto.GetComponent<Image>();
        imagem.sprite = sprite; imagem.preserveAspect = true; imagem.raycastTarget = false;
        RectTransform rt = imagem.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = posicao; rt.sizeDelta = tamanho;
        return imagem;
    }
    static void Texto(Text texto, float x, float y, float largura, int fonte, TextAnchor alinhamento)
    {
        if (texto == null) return;
        RectTransform rt = texto.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y); rt.sizeDelta = new Vector2(largura, 34);
        texto.fontSize = fonte; texto.alignment = alinhamento;
        texto.raycastTarget = false;
        Shadow sombra = texto.GetComponent<Shadow>();
        if (sombra == null) sombra = texto.gameObject.AddComponent<Shadow>();
        sombra.effectColor = new Color(0, 0, 0, .8f); sombra.effectDistance = new Vector2(1, -1);
    }
}
