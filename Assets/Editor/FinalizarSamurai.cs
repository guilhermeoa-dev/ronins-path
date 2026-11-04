using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

// Ajuste pontual e repetivel da cena existente. Nao recria a fase nem muda Transform.scale.
public static class FinalizarSamurai
{
    const string Pasta="Assets/Bimestres34/OldSamurai";
    const string Origem="Assets/FREE_Samurai 2D Pixel Art v1.2/FREE_Samurai 2D Pixel Art v1.2/Sprites/";
    static void PixelArt(string caminho)
    {
        TextureImporter imp=AssetImporter.GetAtPath(caminho) as TextureImporter;
        if(imp==null) return;
        if(imp.filterMode==FilterMode.Point && !imp.mipmapEnabled && imp.textureCompression==TextureImporterCompression.Uncompressed) return;
        imp.filterMode=FilterMode.Point; imp.mipmapEnabled=false;
        imp.textureCompression=TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();
    }
    static AnimationClip Clipe(string nome,string folha,bool repetir)
    {
        string caminho=Pasta+"/"+folha+".png";
        if(!File.Exists(caminho)) File.Copy(Origem+folha+".png",caminho);
        AssetDatabase.ImportAsset(caminho);
        TextureImporter imp=(TextureImporter)AssetImporter.GetAtPath(caminho);
        Texture2D tex=AssetDatabase.LoadAssetAtPath<Texture2D>(caminho);
        int n=tex.width/96;
        imp.textureType=TextureImporterType.Sprite; imp.spriteImportMode=SpriteImportMode.Multiple;
        imp.spritePixelsPerUnit=16; imp.filterMode=FilterMode.Point; imp.mipmapEnabled=false;
        imp.textureCompression=TextureImporterCompression.Uncompressed;
        imp.npotScale=TextureImporterNPOTScale.None;
        SpriteMetaData[] quadros=new SpriteMetaData[n];
        for(int i=0;i<n;i++) quadros[i]=new SpriteMetaData {name=nome+"_"+i,rect=new Rect(i*96,0,96,96),alignment=9,pivot=new Vector2(.5f,15f/96f)};
        imp.spritesheet=quadros; imp.SaveAndReimport();
        string destino=Pasta+"/"+nome+".anim";
        AnimationClip clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(destino);
        if(clip!=null) return clip;
        clip=new AnimationClip(); clip.frameRate=12;
        Sprite[] sprites=AssetDatabase.LoadAllAssetsAtPath(caminho).OfType<Sprite>().OrderBy(s=>int.Parse(s.name.Split('_').Last())).ToArray();
        ObjectReferenceKeyframe[] chaves=new ObjectReferenceKeyframe[sprites.Length+1];
        for(int i=0;i<sprites.Length;i++) chaves[i]=new ObjectReferenceKeyframe {time=i/12f,value=sprites[i]};
        chaves[sprites.Length]=new ObjectReferenceKeyframe {time=sprites.Length/12f,value=sprites[sprites.Length-1]};
        AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding {path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},chaves);
        AnimationClipSettings config=AnimationUtility.GetAnimationClipSettings(clip);config.loopTime=repetir;AnimationUtility.SetAnimationClipSettings(clip,config);
        AssetDatabase.CreateAsset(clip,destino);return clip;
    }
    [MenuItem("Tools/Samurai Boy/Finalizar Old Samurai (ajuste pontual)")]
    public static void Aplicar()
    {
        if(Application.unityVersion!="2017.4.40f1") throw new Exception("Abra com Unity 2017.4.40f1.");
        Directory.CreateDirectory(Pasta);AssetDatabase.Refresh();
        string controllerPath=Pasta+"/OldSamurai.controller";
        AnimatorController controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if(controller==null)
        {
            controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            string[] nomes={"Parado","Correndo","Ataque","Ferido"};
            string[] folhas={"IDLE","RUN","ATTACK 1","HURT"};
            for(int i=0;i<nomes.Length;i++)
            {
                AnimatorState estado=controller.layers[0].stateMachine.AddState(nomes[i]);
                estado.motion=Clipe(nomes[i],folhas[i],i<2);
                if(i==0) controller.layers[0].stateMachine.defaultState=estado;
            }
        }
        EditorSceneManager.OpenScene("Assets/Bimestres34/TravessiaDoTemplo.unity");
        Samurai jogador=UnityEngine.Object.FindObjectOfType<Samurai>();
        if(jogador==null || !jogador.modoMissao) throw new Exception("Jogador da missao nao localizado.");
        OldSamurai old=UnityEngine.Object.FindObjectOfType<OldSamurai>();
        if(old==null)
        {
            GameObject g=new GameObject("Old Samurai - guardiao do pergaminho");g.transform.position=new Vector3(58,.02f,0);
            SpriteRenderer sr=g.AddComponent<SpriteRenderer>();sr.sortingOrder=5;
            sr.sprite=AssetDatabase.LoadAllAssetsAtPath(Pasta+"/IDLE.png").OfType<Sprite>().OrderBy(s=>s.name).First();
            BoxCollider2D col=g.AddComponent<BoxCollider2D>();col.size=new Vector2(.65f,1.7f);col.offset=new Vector2(.1f,.85f);
            Rigidbody2D rb=g.AddComponent<Rigidbody2D>();rb.isKinematic=true;rb.freezeRotation=true;rb.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
            g.AddComponent<Animator>().runtimeAnimatorController=controller;
            AlvoTemplo alvo=g.AddComponent<AlvoTemplo>();alvo.vida=8;alvo.atrasoDestruicao=.85f;
            old=g.AddComponent<OldSamurai>();old.projetil=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Bimestres34/DisparoGuarda.prefab");
            GameObject placa=new GameObject("Vida e aviso");placa.transform.SetParent(g.transform,false);placa.transform.localPosition=new Vector3(0,2.7f,0);
            TextMesh texto=placa.AddComponent<TextMesh>();texto.anchor=TextAnchor.MiddleCenter;texto.alignment=TextAlignment.Center;
            texto.fontSize=40;texto.characterSize=.065f;texto.text="OLD SAMURAI  8/8";placa.GetComponent<MeshRenderer>().sortingOrder=20;old.indicador=texto;
            PrefabUtility.CreatePrefab(Pasta+"/OldSamurai.prefab",g);
        }
        old.jogador=jogador;jogador.guardiaoFinal=old.GetComponent<AlvoTemplo>();
        old.indicador.characterSize=.065f;
        GameObject instrucoes=GameObject.Find("Instrucao final");
        if(instrucoes!=null) {
            instrucoes.GetComponent<TextMesh>().text="Venca o Old Samurai\ne recupere o pergaminho";
            instrucoes.transform.position=new Vector3(60,4.2f,0);
        }
        HashSet<string> texturas=new HashSet<string>();
        foreach(SpriteRenderer sr in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>())
            if(sr.sprite!=null) texturas.Add(AssetDatabase.GetAssetPath(sr.sprite.texture));
        foreach(string arquivo in Directory.GetFiles("Assets/Bimestres34/Arte","*.png"))texturas.Add(arquivo.Replace('\\','/'));
        foreach(string textura in texturas) PixelArt(textura);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        Debug.Log("FINALIZACAO_APLICADA Old Samurai integrado; escalas existentes preservadas.");
    }
    public static void TestarMissao()
    {
        EditorSceneManager.OpenScene("Assets/Bimestres34/TravessiaDoTemplo.unity");
        File.WriteAllText("Library/TestarTemplo.request","testar");EditorApplication.isPlaying=true;
    }
    public static void AplicarETestarMissao() { Aplicar();TestarMissao(); }
    [Serializable] public class Auditoria {
        public string unity;public int cenas,componentes,prefabs;public List<string> erros=new List<string>();
    }
    public static void ValidarERegressao()
    {
        Auditoria resultado=new Auditoria();resultado.unity=Application.unityVersion;
        foreach(EditorBuildSettingsScene entrada in EditorBuildSettings.scenes) {
            if(!entrada.enabled)continue;
            var cena=EditorSceneManager.OpenScene(entrada.path);resultado.cenas++;
            foreach(GameObject g in cena.GetRootGameObjects()) ValidarObjeto(g,entrada.path,resultado);
        }
        foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Bimestres34"})) {
            string path=AssetDatabase.GUIDToAssetPath(guid);resultado.prefabs++;
            ValidarObjeto(AssetDatabase.LoadAssetAtPath<GameObject>(path),path,resultado);
        }
        string pasta=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Diagnostico/RevisaoFinal"));
        File.WriteAllText(Path.Combine(pasta,"referencias_unity.json"),JsonUtility.ToJson(resultado,true));
        if(resultado.erros.Count>0)throw new Exception("Auditoria encontrou referencias invalidas. Veja referencias_unity.json.");
        SamuraiRecoveryPlayTests.Start();
    }
    static void ValidarObjeto(GameObject g,string origem,Auditoria resultado)
    {
        foreach(Transform t in g.GetComponentsInChildren<Transform>(true))foreach(Component c in t.GetComponents<Component>()) {
            resultado.componentes++;
            if(c==null){resultado.erros.Add(origem+" / "+t.name+": script ausente");continue;}
            SerializedObject so=new SerializedObject(c);SerializedProperty prop=so.GetIterator();
            while(prop.NextVisible(true)) if(prop.propertyType==SerializedPropertyType.ObjectReference && prop.objectReferenceValue==null && prop.objectReferenceInstanceIDValue!=0)
                resultado.erros.Add(origem+" / "+t.name+" / "+prop.propertyPath+": referencia quebrada");
            Samurai s=c as Samurai;
            if(s!=null && s.enabled && (s.GetComponent<Rigidbody2D>()==null || s.GetComponent<Animator>()==null || s.UITextVidas==null))resultado.erros.Add(origem+": Samurai incompleto");
            if(s!=null && s.modoMissao && (s.guardiaoFinal==null || s.projeteisMissao.Length!=3 || s.projeteisMissao.Any(x=>x==null)))resultado.erros.Add(origem+": configuracao da missao incompleta");
        }
    }
    public static void TestarRevisao()
    {
        EditorSceneManager.OpenScene("Assets/Bimestres34/TravessiaDoTemplo.unity");
        File.WriteAllText("Library/TestarRevisao.request","testar");EditorApplication.isPlaying=true;
    }
}
