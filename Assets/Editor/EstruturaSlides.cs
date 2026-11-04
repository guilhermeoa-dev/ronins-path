using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

// Migracao pontual: conserva objetos, colliders, escalas, sprites e referencias existentes.
public static class EstruturaSlides
{
    static readonly string[] tags = { "mover", "destrutivel1", "tipo1", "armadilha01", "Plataforma", "portal01", "portal02",
        "inimigo01", "inimigo02", "areaAtaqueEsquerda", "areaAtaqueDireita", "projetil1", "projetil2", "projetil3",
        "disparoJogador", "disparoInimigo", "gradeTemplo", "objetivo" };
    static readonly string[] cenas = { "Assets/Cena1.unity", "Assets/Cena2.unity", "Assets/Bimestres34/TravessiaDoTemplo.unity" };
    static readonly string[] parametros = { "andando", "correndo", "pulando", "estaAtacando", "defendendo", "dash" };
    static readonly string[] estados = { "Andando", "Correndo", "Pulando", "Atacando", "Defendendo", "Dash", "Parado" };

    static void Tags()
    {
        SerializedObject configuracao = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty lista = configuracao.FindProperty("tags");
        foreach (string tag in tags)
        {
            bool existe = false;
            for (int i = 0; i < lista.arraySize; i++) existe |= lista.GetArrayElementAtIndex(i).stringValue == tag;
            if (existe) continue;
            int indice = lista.arraySize; lista.InsertArrayElementAtIndex(indice); lista.GetArrayElementAtIndex(indice).stringValue = tag;
        }
        configuracao.ApplyModifiedProperties();
    }
    static void Animator()
    {
        AnimatorController controlador = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Bimestres34/Polimento/Samurai/Animacoes.controller");
        foreach (string parametro in parametros)
            if (!controlador.parameters.Any(p => p.name == parametro)) controlador.AddParameter(parametro, AnimatorControllerParameterType.Bool);
        AnimatorStateMachine maquina = controlador.layers[0].stateMachine;
        for (int i = 0; i < estados.Length; i++)
        {
            AnimatorState estado = maquina.states.Select(s => s.state).First(s => s.name == estados[i]);
            string nome = "Slides -> " + estados[i];
            AnimatorStateTransition transicao = maquina.anyStateTransitions.FirstOrDefault(t => t.name == nome);
            if (transicao == null) { transicao = maquina.AddAnyStateTransition(estado); transicao.name = nome; }
            transicao.hasExitTime = false; transicao.duration = 0; transicao.canTransitionToSelf = false;
            foreach (AnimatorCondition condicao in transicao.conditions) transicao.RemoveCondition(condicao);
            for (int j = 0; j < parametros.Length; j++)
                transicao.AddCondition(i == j ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, parametros[j]);
        }
        EditorUtility.SetDirty(controlador);
    }
    static IEnumerable<T> Componentes<T>() where T : Component
    {
        return EditorSceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true));
    }
    [MenuItem("Samurai Boy/Aplicar estrutura dos slides")]
    public static void Aplicar()
    {
        Tags(); Animator();
        foreach (string nome in new[] { "Kunai", "SeloEspiritual", "Bomba", "DisparoGuarda" })
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Bimestres34/" + nome + ".prefab");
            if (prefab == null) continue;
            ProjetilTemplo projetil = prefab.GetComponent<ProjetilTemplo>();
            if (projetil != null) { prefab.tag = projetil.hostil ? "disparoInimigo" : "disparoJogador"; EditorUtility.SetDirty(prefab); }
        }
        AssetDatabase.SaveAssets();
        foreach (string cena in cenas)
        {
            EditorSceneManager.OpenScene(cena);
            Samurai jogador = Componentes<Samurai>().First(s => s.enabled && s.combateHabilitado);
            jogador.gameObject.tag = "Player";
            jogador.areaKatanaEsquerda.gameObject.tag = "areaAtaqueEsquerda";
            jogador.areaKatanaDireita.gameObject.tag = "areaAtaqueDireita";
            foreach (GuardaTemplo guarda in Componentes<GuardaTemplo>()) guarda.gameObject.tag = "inimigo01";
            foreach (OldSamurai chefe in Componentes<OldSamurai>()) chefe.gameObject.tag = "inimigo02";
            foreach (AlvoTemplo alvo in Componentes<AlvoTemplo>())
                if (!alvo.CompareTag("Limbo") && !alvo.tag.Contains("inimigo")) alvo.gameObject.tag = "destrutivel1";
            foreach (Plataforma plataforma in Componentes<Plataforma>()) plataforma.gameObject.tag = "Plataforma";
            foreach (ObjetoTemplo objeto in Componentes<ObjetoTemplo>())
            {
                // Enum usado apenas para migrar os dados; em jogo, as tags decidem as interacoes.
                switch (objeto.tipo)
                {
                    case ObjetoTemplo.Tipo.CaixaMovel: objeto.gameObject.tag = "mover"; break;
                    case ObjetoTemplo.Tipo.BonusVento: objeto.gameObject.tag = "tipo1"; break;
                    case ObjetoTemplo.Tipo.Selo:
                        objeto.gameObject.tag = "armadilha01";
                        if (objeto.portao != null) objeto.portao.tag = "gradeTemplo";
                        break;
                    case ObjetoTemplo.Tipo.Portal:
                        objeto.gameObject.tag = "portal01";
                        if (objeto.destino != null) objeto.destino.gameObject.tag = "portal02";
                        break;
                    case ObjetoTemplo.Tipo.Pergaminho: objeto.gameObject.tag = "objetivo"; break;
                }
            }
            for (int i = 0; i < 3; i++)
            {
                string tag = "projetil" + (i + 1);
                GameObject modelo = GameObject.FindGameObjectWithTag(tag);
                if (modelo == null)
                {
                    modelo = (GameObject)PrefabUtility.InstantiatePrefab(jogador.projeteisMissao[i]);
                    modelo.name = "Modelo " + tag + " - aula 04x01";
                    modelo.transform.position = new Vector3(-1000 - i * 4, -1000, 0);
                }
                modelo.tag = tag; modelo.GetComponent<ProjetilTemplo>().modelo = true;
                modelo.GetComponent<Rigidbody2D>().simulated = false;
                foreach (Collider2D colisor in modelo.GetComponentsInChildren<Collider2D>()) colisor.enabled = false;
                foreach (SpriteRenderer imagem in modelo.GetComponentsInChildren<SpriteRenderer>()) imagem.enabled = false;
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }
        AssetDatabase.SaveAssets(); Validar();
    }
    [Serializable] class Resultado { public List<string> verificacoes = new List<string>(); public List<string> erros = new List<string>(); }
    public static void Validar()
    {
        Resultado r = new Resultado();
        foreach (string cena in cenas)
        {
            EditorSceneManager.OpenScene(cena);
            GameObject[] jogadores = GameObject.FindGameObjectsWithTag("Player");
            if (jogadores.Length != 1 || jogadores[0].GetComponent<Samurai>() == null) r.erros.Add(cena + ": Player nao unico");
            foreach (string tag in new[] { "areaAtaqueEsquerda", "areaAtaqueDireita", "projetil1", "projetil2", "projetil3" })
                if (GameObject.FindGameObjectsWithTag(tag).Length != 1) r.erros.Add(cena + ": tag ausente ou duplicada " + tag);
            foreach (ProjetilTemplo modelo in Componentes<ProjetilTemplo>().Where(p => p.modelo))
                if (modelo.GetComponent<Rigidbody2D>().simulated || modelo.GetComponentsInChildren<Collider2D>().Any(c => c.enabled) || modelo.GetComponentsInChildren<SpriteRenderer>().Any(s => s.enabled))
                    r.erros.Add(cena + ": modelo participa do jogo");
            r.verificacoes.Add(cena + ": Player, areas de ataque e modelos de projetil");
        }
        string pasta = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Diagnostico/EstruturaSlides"));
        Directory.CreateDirectory(pasta); File.WriteAllText(Path.Combine(pasta, "tags_cenas.json"), JsonUtility.ToJson(r, true));
        if (r.erros.Count > 0) throw new Exception(string.Join("; ", r.erros.ToArray()));
        PolirSamurai.AuditarReferencias();
        File.Copy(Path.GetFullPath(Path.Combine(Application.dataPath, "../../Diagnostico/Polimento/referencias.json")), Path.Combine(pasta, "referencias.json"), true);
        EditorSceneManager.OpenScene("Assets/PrimeiraTela.unity");
    }
    public static void TestarHud()
    {
        EditorSceneManager.OpenScene("Assets/Bimestres34/TravessiaDoTemplo.unity");
        File.WriteAllText("Library/PolimentoHUD.request", "testar"); EditorApplication.isPlaying = true;
    }
}
