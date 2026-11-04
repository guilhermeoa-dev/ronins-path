using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
// AudioSource + AudioClip (aula 04x02). Um banco compartilhado evita cortar sons ao destruir alvos.
public class SomRonin : MonoBehaviour
{
    static SomRonin atual;
    AudioSource[] vozes;
    AudioSource rock;
    Dictionary<AudioSource, float> musicaCena = new Dictionary<AudioSource, float>();
    Samurai jogador;
    OldSamurai chefe;
    float mistura;
    bool iniciouChefe;
    int voz;
    Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    Dictionary<string, float> ultimos = new Dictionary<string, float>();
    public string UltimoEfeito
    {
        get;
        private set;
    }

    public int EfeitosTocados
    {
        get;
        private set;
    }

    public bool RockAtivo
    {
        get
        {
            return rock != null && rock.isPlaying && rock.volume > .01f;
        }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Inicializar()
    {
        if (atual != null)
            return;
        var go = new GameObject("Audio - Ronin's Path");
        DontDestroyOnLoad(go);
        atual = go.AddComponent<SomRonin>();
    }

    void Awake()
    {
        atual = this;
        vozes = new AudioSource[8];
        for (int i = 0; i < vozes.Length; i++)
            vozes[i] = Fonte();
        rock = Fonte();
        rock.loop = true;
        rock.volume = 0;
        rock.clip = Clip("boss_rock");
        SceneManager.sceneLoaded += Carregou;
    }

    AudioSource Fonte()
    {
        var f = gameObject.AddComponent<AudioSource>();
        f.playOnAwake = false;
        f.spatialBlend = 0;
        f.priority = 100;
        return f;
    }

    AudioClip Clip(string nome)
    {
        AudioClip c;
        if (!clips.TryGetValue(nome, out c))
        {
            c = Resources.Load<AudioClip>("SonsRonin/" + nome);
            clips[nome] = c;
        }
        return c;
    }

    void Carregou(Scene cena, LoadSceneMode modo)
    {
        iniciouChefe = false;
        mistura = 0;
        rock.Stop();
        rock.volume = 0;
        jogador = FindObjectOfType<Samurai>();
        chefe = FindObjectOfType<OldSamurai>();
        musicaCena.Clear();
        foreach (var f in FindObjectsOfType<AudioSource>())
            if (f.gameObject != gameObject)
        {
            f.volume = Mathf.Min(f.volume, .10f);
            musicaCena[f] = f.volume;
        }
        if (cena.name.StartsWith("Vitoria"))
            Tocar("vitoria", Vector3.zero);
        if (cena.name == "GameOver")
            Tocar("derrota", Vector3.zero);
    }

    void Update()
    {
        if (jogador != null && chefe != null && chefe.modoMago && !jogador.missaoEncerrada && Vector2.Distance(jogador.transform.position, chefe.transform.position) < 8)
            iniciouChefe = true;
        bool combate = iniciouChefe && jogador != null && !jogador.missaoEncerrada && chefe != null;
        mistura = Mathf.MoveTowards(mistura, combate ? 1 : 0, Time.unscaledDeltaTime / 1.2f);
        if (combate && !rock.isPlaying && rock.clip != null)
            rock.Play();
        rock.volume = .28f * mistura;
        if (!combate && mistura == 0 && rock.isPlaying)
            rock.Stop();
        foreach (var entrada in musicaCena)
            if (entrada.Key != null)
                entrada.Key.volume = entrada.Value * (1 - mistura);
    }

    public static void Tocar(string nome, Vector3 posicao)
    {
        if (atual == null)
            return;
        float distancia = atual.jogador != null ? Vector2.Distance(atual.jogador.transform.position, posicao) : 0;
        if (nome != "vitoria" && nome != "derrota" && distancia > 18)
            return;
        float ultimo;
        if (atual.ultimos.TryGetValue(nome, out ultimo) && Time.unscaledTime - ultimo < .045f)
            return;
        AudioClip clip = atual.Clip(nome);
        if (clip == null)
            return;
        atual.ultimos[nome] = Time.unscaledTime;
        AudioSource f = atual.vozes[atual.voz++ % atual.vozes.Length];
        f.Stop();
        f.clip = clip;
        f.volume = .55f * Mathf.Lerp(1, .25f, Mathf.Clamp01(distancia / 18));
        f.Play();
        atual.UltimoEfeito = nome;
        atual.EfeitosTocados++;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= Carregou;
        if (atual == this)
            atual = null;
    }
}
