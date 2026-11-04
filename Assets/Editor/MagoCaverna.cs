using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
public static class MagoCaverna {
 const string P="Assets/Bimestres34/EvilWizard2";
 public static void AjustarArte() {
  string f=P+"/CaveTiles.png";if(!File.Exists(f))File.Copy("Assets/PixelFantasy_Caves_1.0/mainlev_build.png",f);AssetDatabase.ImportAsset(f);
  TextureImporter imp=(TextureImporter)AssetImporter.GetAtPath(f);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Multiple;imp.spritePixelsPerUnit=32;imp.filterMode=FilterMode.Point;imp.textureCompression=TextureImporterCompression.Uncompressed;
  imp.spritesheet=new[]{new SpriteMetaData{name="Topo",rect=new Rect(224,624,32,32),alignment=0,pivot=Vector2.one*.5f},new SpriteMetaData{name="Interior",rect=new Rect(32,960,32,32),alignment=0,pivot=Vector2.one*.5f},new SpriteMetaData{name="Parede",rect=new Rect(848,464,32,32),alignment=0,pivot=Vector2.one*.5f},new SpriteMetaData{name="Plataforma",rect=new Rect(224,644,32,12),alignment=0,pivot=Vector2.one*.5f}};imp.SaveAndReimport();
  Sprite[] tiles=AssetDatabase.LoadAllAssetsAtPath(f).OfType<Sprite>().ToArray();
  EditorSceneManager.OpenScene("Assets/Bimestres34/TravessiaDoTemplo.unity");
  foreach(SpriteRenderer r in Object.FindObjectsOfType<SpriteRenderer>()){
   if(r.sprite!=null && r.sprite.name=="RochaTopo")r.sprite=tiles.First(s=>s.name=="Topo");
   if(r.sprite!=null && r.sprite.name=="RochaInterior")r.sprite=tiles.First(s=>s.name=="Interior");
   if(r.sprite!=null && r.sprite.name=="Alvenaria")r.sprite=tiles.First(s=>s.name=="Parede");
   if(r.name=="Arvore" || r.GetComponentsInParent<Transform>().Any(t=>t.name=="Arvore"))r.enabled=false;
  }
    GameObject plataforma=GameObject.Find("Plataforma do desfiladeiro");
  if(plataforma!=null && plataforma.transform.Find("Plataforma rochosa")==null){
   foreach(SpriteRenderer antigo in plataforma.GetComponentsInChildren<SpriteRenderer>())antigo.enabled=false;
   Bounds b=plataforma.GetComponent<Collider2D>().bounds;
   SpriteRenderer visual=Tile(plataforma.transform,"Plataforma rochosa",tiles.First(s=>s.name=="Topo"),b.center,3);
   visual.drawMode=SpriteDrawMode.Tiled;visual.size=new Vector2(b.size.x,b.size.y);
  }
EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
 }
 static Sprite Corte(string nome,int x,int y,int w,int h,float ppu) {
  string f=P+"/"+nome+".asset";
  Sprite s=AssetDatabase.LoadAssetAtPath<Sprite>(f); if(s!=null)return s;
  Texture2D t=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PixelFantasy_Caves_1.0/mainlev_build.png");
  s=Sprite.Create(t,new Rect(x,t.height-y-h,w,h),new Vector2(.5f,.5f),ppu);s.name=nome;AssetDatabase.CreateAsset(s,f);return s;
 }
 static AnimationClip Clip(string nome,string folha,bool loop) {
  string f=P+"/Sprites/"+folha+".png";
  TextureImporter imp=(TextureImporter)AssetImporter.GetAtPath(f);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Multiple;imp.spritePixelsPerUnit=48;imp.filterMode=FilterMode.Point;imp.textureCompression=TextureImporterCompression.Uncompressed;
  Texture2D t=AssetDatabase.LoadAssetAtPath<Texture2D>(f); int n=t.width/250;
  imp.spritesheet=Enumerable.Range(0,n).Select(i=>new SpriteMetaData{name=nome+"_"+i,rect=new Rect(i*250,0,250,250),alignment=9,pivot=new Vector2(.5f,.28f)}).ToArray();imp.SaveAndReimport();
  Sprite[] ss=AssetDatabase.LoadAllAssetsAtPath(f).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
  AnimationClip c=new AnimationClip();c.frameRate=12;
  AnimationUtility.SetObjectReferenceCurve(c,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},ss.Select((s,i)=>new ObjectReferenceKeyframe{time=i/12f,value=s}).ToArray());
  var settings=AnimationUtility.GetAnimationClipSettings(c);settings.loopTime=loop;AnimationUtility.SetAnimationClipSettings(c,settings);AssetDatabase.CreateAsset(c,P+"/"+nome+".anim");return c;
 }
 static SpriteRenderer Tile(Transform pai,string nome,Sprite s,Vector3 pos,int ordem) {
  GameObject g=new GameObject(nome);g.transform.SetParent(pai,true);g.transform.position=pos;Vector3 escala=pai.lossyScale;g.transform.localScale=new Vector3(1/escala.x,1/escala.y,1/escala.z);SpriteRenderer r=g.AddComponent<SpriteRenderer>();r.sprite=s;r.sortingOrder=ordem;return r;
 }
 public static void Aplicar() {
  AssetDatabase.Refresh();
  AnimatorController a=AnimatorController.CreateAnimatorControllerAtPath(P+"/Mago.controller");
  foreach(string b in new[]{"andando","estaAtacando","ferido"})a.AddParameter(b,AnimatorControllerParameterType.Bool);
  string[] nomes={"Parado","Correndo","Ataque","Ataque2","Ferido","Morte"},folhas={"Idle","Run","Attack1","Attack2","Take hit","Death"};
  for(int i=0;i<nomes.Length;i++){var e=a.layers[0].stateMachine.AddState(nomes[i]);e.motion=Clip(nomes[i],folhas[i],i<2);if(i==0)a.layers[0].stateMachine.defaultState=e;}
  EditorSceneManager.OpenScene("Assets/Bimestres34/TravessiaDoTemplo.unity");
  OldSamurai chefe=Object.FindObjectOfType<OldSamurai>();chefe.modoMago=true;chefe.name="Mago maligno - guardiao do pergaminho";chefe.GetComponent<Animator>().runtimeAnimatorController=a;
  chefe.GetComponent<SpriteRenderer>().sprite=AssetDatabase.LoadAllAssetsAtPath(P+"/Sprites/Idle.png").OfType<Sprite>().First();
  chefe.GetComponent<AlvoTemplo>().atrasoDestruicao=.65f;
  GameObject tiro=Object.Instantiate(chefe.projetil);tiro.name="Rajada do mago";
  foreach(SpriteRenderer r in tiro.GetComponentsInChildren<SpriteRenderer>())r.enabled=false;
  Sprite spell=AssetDatabase.LoadAllAssetsAtPath("Assets/Pixel Holy Spell Effect 32x32 Pack 3/01.png").OfType<Sprite>().First();
  SpriteRenderer magia=Tile(tiro.transform,"Energia magica",spell,Vector3.zero,8);magia.color=new Color(.8f,.35f,1);float k=.5f/Mathf.Max(spell.bounds.size.x,spell.bounds.size.y);magia.transform.localScale=Vector3.one*k;
  chefe.projetil=PrefabUtility.CreatePrefab(P+"/RajadaMago.prefab",tiro);Object.DestroyImmediate(tiro);
  // Somente renderizadores de ambiente sao substituidos; logica e colisores permanecem.
  foreach(SpriteRenderer r in Object.FindObjectsOfType<SpriteRenderer>()){
   string path=r.sprite==null?"":AssetDatabase.GetAssetPath(r.sprite);
   if(path.Contains("2 Background") || r.name.StartsWith("Piso ") || r.transform.parent!=null && (r.transform.parent.name=="Muralha do santuario" || r.transform.parent.name=="Grade do desfiladeiro"))r.enabled=false;
  }
  Sprite topo=Corte("RochaTopo",160,160,32,32,32),miolo=Corte("RochaInterior",320,240,32,32,32),parede=Corte("Alvenaria",848,528,32,32,32);
  GameObject arte=new GameObject("Caverna - ambiente");
  foreach(BoxCollider2D c in Object.FindObjectsOfType<BoxCollider2D>().Where(c=>c.name.StartsWith("Piso "))){foreach(SpriteRenderer antigo in c.GetComponentsInChildren<SpriteRenderer>())antigo.enabled=false;float x=c.bounds.center.x;Tile(arte.transform,"Rocha do piso",topo,new Vector3(x,-.5f,0),1);for(int j=1;j<4;j++)Tile(arte.transform,"Interior rochoso",miolo,new Vector3(x,-.5f-j,0),1);}
  foreach(string nome in new[]{"Grade do desfiladeiro","Muralha do santuario"}){
   GameObject g=GameObject.Find(nome);foreach(SpriteRenderer r in g.GetComponentsInChildren<SpriteRenderer>())r.enabled=false;
   Bounds b=g.GetComponent<Collider2D>().bounds;int nx=nome.StartsWith("Grade")?1:2,ny=Mathf.CeilToInt(b.size.y);
   for(int y=0;y<ny;y++)for(int x=0;x<nx;x++)Tile(g.transform,"Parede de pedra "+x+"-"+y,y==ny-1?topo:parede,new Vector3(b.center.x+(x-(nx-1)*.5f),b.min.y+.5f+y,0),3);
  }
  Texture2D bg=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PixelFantasy_Caves_1.0/background1.png");
  Sprite fundo=Sprite.Create(bg,new Rect(0,0,bg.width,bg.height),new Vector2(.5f,.5f),32);AssetDatabase.CreateAsset(fundo,P+"/FundoCaverna.asset");
  float largura=fundo.bounds.size.x;for(float x=-12;x<85;x+=largura)Tile(arte.transform,"Fundo da caverna",fundo,new Vector3(x,3,0),-20);
  Camera.main.backgroundColor=new Color(.035f,.025f,.06f);
  EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
  AuditoriaCompleta.Validar();
 }
}
