using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;

// Ferramenta editorial: grava objetos, referencias, clips e prefabs reais no projeto.
// Equivale a montar no Inspector as configuracoes descritas nas aulas 03x01-08/04x01-04.
public static class ConstruirTemplo {
 const string pasta="Assets/Bimestres34";
 static Sprite SpriteDe(string caminho) { return AssetDatabase.LoadAllAssetsAtPath(caminho).OfType<Sprite>().First(); }
 static GameObject Imagem(string nome,string arquivo,Vector2 pos,Vector2 tamanho,int ordem) {
  GameObject g=new GameObject(nome); g.transform.position=pos;
  SpriteRenderer sr=g.AddComponent<SpriteRenderer>(); sr.sprite=SpriteDe(arquivo); sr.sortingOrder=ordem;
  g.transform.localScale=new Vector3(tamanho.x/sr.sprite.bounds.size.x,tamanho.y/sr.sprite.bounds.size.y,1); return g;
 }
 static BoxCollider2D Caixa(GameObject g,bool trigger) {
  BoxCollider2D c=g.AddComponent<BoxCollider2D>(); c.size=g.GetComponent<SpriteRenderer>().sprite.bounds.size; c.isTrigger=trigger; return c;
 }
 static Text Texto(Transform pai,string nome,string conteudo,Vector2 ancora,Vector2 pos,Vector2 tamanho,int fonte) {
  GameObject g=new GameObject(nome,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text)); g.transform.SetParent(pai,false);
  Text t=g.GetComponent<Text>(); t.font=Resources.GetBuiltinResource<Font>("Arial.ttf"); t.fontSize=fonte; t.text=conteudo;
  t.color=Color.white; t.raycastTarget=false;
  RectTransform rt=t.rectTransform;rt.anchorMin=rt.anchorMax=rt.pivot=ancora;rt.anchoredPosition=pos;rt.sizeDelta=tamanho;return t;
 }
 static Canvas Tela() {
  GameObject g=new GameObject("Interface da missao",typeof(Canvas),typeof(CanvasScaler));
  Canvas c=g.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;
  CanvasScaler s=g.GetComponent<CanvasScaler>();s.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;s.referenceResolution=new Vector2(1280,720);s.matchWidthOrHeight=.5f; return c;
 }
 static void FundoTexto(Transform pai,Vector2 ancora,Vector2 pos,Vector2 tamanho) {
  GameObject g=new GameObject("Fundo legivel",typeof(RectTransform),typeof(Image));g.transform.SetParent(pai,false);
  Image i=g.GetComponent<Image>();i.color=new Color(.025f,.045f,.075f,.88f);i.raycastTarget=false;
  RectTransform rt=i.rectTransform;rt.anchorMin=rt.anchorMax=rt.pivot=ancora;rt.anchoredPosition=pos;rt.sizeDelta=tamanho;
 }
 static AnimationClip Animacao(string nome,string folha) {
  string destino=pasta+"/Arte/"+folha+".png";
  File.Copy("Assets/Samurai/"+folha+".png",destino,false);AssetDatabase.ImportAsset(destino);
  TextureImporter imp=(TextureImporter)AssetImporter.GetAtPath(destino);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Multiple;imp.spritePixelsPerUnit=64;imp.filterMode=FilterMode.Point;
  Texture2D tex=AssetDatabase.LoadAssetAtPath<Texture2D>(destino);int quantidade=tex.width/128;
  SpriteMetaData[] frames=new SpriteMetaData[quantidade];
  for(int i=0;i<quantidade;i++) frames[i]=new SpriteMetaData{name=nome+"_"+i,rect=new Rect(i*128,0,128,128),alignment=9,pivot=new Vector2(.5f,.32f)};
  imp.spritesheet=frames;imp.SaveAndReimport();
  Sprite[] sprites=AssetDatabase.LoadAllAssetsAtPath(destino).OfType<Sprite>().OrderBy(x=>int.Parse(x.name.Split('_').Last())).ToArray();
  AnimationClip clip=new AnimationClip();clip.frameRate=10;
  ObjectReferenceKeyframe[] keys=new ObjectReferenceKeyframe[sprites.Length];
  for(int i=0;i<keys.Length;i++) keys[i]=new ObjectReferenceKeyframe{time=i/10f,value=sprites[i]};
  AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},keys);
  AnimationClipSettings settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,settings);
  AssetDatabase.CreateAsset(clip,pasta+"/"+nome+".anim");return clip;
 }
 static AnimatorController Controlador() {
  AnimatorController c=AnimatorController.CreateAnimatorControllerAtPath(pasta+"/SamuraiTemplo.controller");
  c.AddParameter("VelocidadeX",AnimatorControllerParameterType.Float);
  foreach(string p in new[]{"defendendo","atacando","pulando"})c.AddParameter(p,AnimatorControllerParameterType.Bool);
  var sm=c.layers[0].stateMachine;
  string[] nomes={"Parado","Andando","Pulando","Atacando","Defendendo"};string[] folhas={"Idle","Walk","Jump","Attack_1","Shield"};
  for(int i=0;i<nomes.Length;i++) {
   AnimatorState s=sm.AddState(nomes[i]);s.motion=Animacao(nomes[i],folhas[i]);if(i==0)sm.defaultState=s;
   AnimatorStateTransition t=sm.AddAnyStateTransition(s);t.hasExitTime=false;t.duration=0;t.canTransitionToSelf=false;
   t.AddCondition(i==4?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"defendendo");
   if(i!=4)t.AddCondition(i==3?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"atacando");
   if(i<3)t.AddCondition(i==2?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"pulando");
   if(i<2)t.AddCondition(i==1?AnimatorConditionMode.Greater:AnimatorConditionMode.Less,.1f,"VelocidadeX");
  }return c;
 }
 static GameObject Tiro(string nome,int tipo,bool hostil) {
  string arte=tipo==0?"Assets/3 Objects/Other/Skateboard1.png":tipo==1?"Assets/Pixel Holy Spell Effect 32x32 Pack 3/01.png":"Assets/3 Objects/Stones/1.png";
  GameObject g=Imagem(nome,arte,Vector2.zero,tipo==0?new Vector2(.55f,.12f):new Vector2(.35f,.35f),8);
  g.GetComponent<SpriteRenderer>().color=hostil?new Color(1,.3f,.2f):tipo==1?Color.cyan:Color.white;
  Caixa(g,true);Rigidbody2D rb=g.AddComponent<Rigidbody2D>();rb.gravityScale=tipo==2?1:0;rb.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
  ProjetilTemplo p=g.AddComponent<ProjetilTemplo>();p.tipo=tipo;p.dano=tipo==0?1:2;p.hostil=hostil;
  GameObject prefab=PrefabUtility.CreatePrefab(pasta+"/"+nome+".prefab",g);UnityEngine.Object.DestroyImmediate(g);return prefab;
 }
 [MenuItem("Tools/Samurai Boy/Construir missao dos bimestres")]
 public static void Criar() {
  if(File.Exists(pasta+"/TravessiaDoTemplo.unity")) throw new Exception("Missao ja construida; preservada sem sobrescrever.");
  Directory.CreateDirectory(pasta+"/Arte");AssetDatabase.Refresh();
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  AnimatorController controller=Controlador();
  GameObject[] tiros={Tiro("Kunai",0,false),Tiro("SeloEspiritual",1,false),Tiro("Bomba",2,false)};GameObject inimigoTiro=Tiro("DisparoGuarda",0,true);
  Canvas canvas=Tela();FundoTexto(canvas.transform,new Vector2(0,1),Vector2.zero,new Vector2(1280,110));
  Text painel=Texto(canvas.transform,"Objetivo","TRAVESSIA DO TEMPLO",new Vector2(0,1),new Vector2(22,-12),new Vector2(1220,100),22);
  FundoTexto(canvas.transform,new Vector2(0,0),Vector2.zero,new Vector2(1280,80));
  Texto(canvas.transform,"Comandos","A/D ou setas: andar | Shift: correr | Espaco: pular | Segure E: carregar; solte E: largar\nZ: katana | 1/2/3: escolher projetil | X: disparar | Segure Q: defender | R: reiniciar | Esc: menu",Vector2.zero,new Vector2(18,76),new Vector2(1240,72),19).rectTransform.pivot=new Vector2(0,1);
  GameObject player=new GameObject("Samurai");player.tag="Player";player.transform.position=new Vector3(1,1.1f,0);player.transform.localScale=Vector3.one*1.5f;
  player.AddComponent<SpriteRenderer>().sprite=SpriteDe(pasta+"/Arte/Idle.png");player.GetComponent<SpriteRenderer>().sortingOrder=5;
  player.AddComponent<Animator>().runtimeAnimatorController=controller;
  Rigidbody2D corpo=player.AddComponent<Rigidbody2D>();corpo.freezeRotation=true;corpo.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
  BoxCollider2D pc=player.AddComponent<BoxCollider2D>();pc.size=new Vector2(.43f,1.16f);pc.offset=new Vector2(-.04f,0);
  Samurai s=player.AddComponent<Samurai>();s.modoMissao=true;s.painelMissao=painel;s.projeteisMissao=tiros;
  Text contador=Texto(canvas.transform,"Contadores herdados","",new Vector2(0,1),Vector2.zero,Vector2.one,10);contador.gameObject.SetActive(false);
  s.UITextVidas=contador;s.UITextMoedas=contador;s.UITextEspadas=contador;s.UITextSkates=contador;
  for(int lado=-1;lado<=1;lado+=2) {
   GameObject a=new GameObject(lado<0?"Katana esquerda":"Katana direita");a.transform.SetParent(player.transform,false);a.transform.localPosition=new Vector3(lado*.62f,0,0);
   CircleCollider2D c=a.AddComponent<CircleCollider2D>();c.radius=.55f;c.isTrigger=true;c.enabled=false;
   a.AddComponent<AreaKatana>().jogador=s;if(lado<0)s.areaKatanaEsquerda=c;else s.areaKatanaDireita=c;
  }
  GameObject cameraGo=new GameObject("Main Camera");cameraGo.tag="MainCamera";cameraGo.transform.SetParent(player.transform,false);cameraGo.transform.localPosition=new Vector3(2.5f,1.6f,-10);cameraGo.transform.localScale=Vector3.one/1.5f;
  Camera camera=cameraGo.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=5.6f;camera.backgroundColor=new Color(.12f,.18f,.25f);cameraGo.AddComponent<AudioListener>();
  for(int x=-12;x<74;x+=16) {
   Imagem("Floresta ao fundo","Assets/2 Background/Day/Background.png",new Vector2(x,4),new Vector2(16,12),-20);
  }
  for(int x=-6;x<66;x++) {
   if(x>=19 && x<25)continue;
   GameObject g=Imagem("Piso "+x,"Assets/1 Tiles/Tile_02.png",new Vector2(x+.5f,-.6f),new Vector2(1,1.2f),0);g.tag="chao";Caixa(g,false);
  }
  foreach(int x in new[]{2,11,28,39,57})Imagem("Arvore","Assets/3 Objects/Other/Tree1.png",new Vector2(x,2.2f),new Vector2(3,4.4f),-2);
  GameObject caixa=Imagem("Caixa do mecanismo - segure E","Assets/3 Objects/Other/Box.png",new Vector2(8,.65f),new Vector2(1,1),3);Caixa(caixa,false);caixa.AddComponent<Rigidbody2D>().freezeRotation=true;caixa.AddComponent<ObjetoTemplo>().tipo=ObjetoTemplo.Tipo.CaixaMovel;
  GameObject portao=Imagem("Grade do desfiladeiro","Assets/3 Objects/Fence/1.png",new Vector2(18,2.5f),new Vector2(.8f,5),3);Caixa(portao,false);
  GameObject plataforma=Imagem("Plataforma do desfiladeiro","Assets/1 Tiles/Tile_02.png",new Vector2(19.7f,.2f),new Vector2(2.8f,.4f),3);plataforma.tag="chao";Caixa(plataforma,false);plataforma.AddComponent<Rigidbody2D>().isKinematic=true;
  Plataforma movimento=plataforma.AddComponent<Plataforma>();movimento.Distancia=4.8f;
  GameObject selo=Imagem("Selo - largue a caixa aqui","Assets/3 Objects/Stones/1.png",new Vector2(15,.18f),new Vector2(1.8f,.35f),3);Caixa(selo,true);
  ObjetoTemplo placa=selo.AddComponent<ObjetoTemplo>();placa.tipo=ObjetoTemplo.Tipo.Selo;placa.portao=portao;placa.plataforma=movimento;
  foreach(float x in new[]{5f,30f,43f}) {
   GameObject bau=Imagem("Bau de vento - acerte com Z ou X","Assets/3 Objects/Other/Box.png",new Vector2(x,.65f),Vector2.one,3);Caixa(bau,false);
   AlvoTemplo alvo=bau.AddComponent<AlvoTemplo>();alvo.vida=1;
   GameObject bonus=Imagem("Vento de 8 segundos","Assets/Pixel Holy Spell Effect 32x32 Pack 3/01.png",new Vector2(x,1),new Vector2(.7f,.7f),6);Caixa(bonus,true);bonus.AddComponent<ObjetoTemplo>().tipo=ObjetoTemplo.Tipo.BonusVento;
   bonus.transform.SetParent(bau.transform,true);bonus.SetActive(false);alvo.premio=bonus;
  }
  foreach(float x in new[]{12f,32f,41f}) {
   GameObject g=new GameObject("Guarda do templo");g.transform.position=new Vector3(x,1,0);g.transform.localScale=Vector3.one*1.5f;
   SpriteRenderer sr=g.AddComponent<SpriteRenderer>();sr.sprite=SpriteDe(pasta+"/Arte/Idle.png");sr.color=new Color(1,.42f,.4f);sr.sortingOrder=5;
   BoxCollider2D col=g.AddComponent<BoxCollider2D>();col.size=new Vector2(.43f,1.16f);col.offset=new Vector2(-.04f,0);
   g.AddComponent<Rigidbody2D>().isKinematic=true;g.AddComponent<AlvoTemplo>().vida=3;
   GuardaTemplo guarda=g.AddComponent<GuardaTemplo>();guarda.jogador=s;guarda.projetil=inimigoTiro;
  }
  GameObject destino=Imagem("Saida do portal","Assets/Pixel Holy Spell Effect 32x32 Pack 3/01.png",new Vector2(54,1.1f),new Vector2(1,2.2f),2);destino.GetComponent<SpriteRenderer>().color=Color.cyan;
  GameObject portal=Imagem("Portal do santuario","Assets/Pixel Holy Spell Effect 32x32 Pack 3/01.png",new Vector2(47,1.1f),new Vector2(1,2.2f),2);Caixa(portal,true);portal.GetComponent<SpriteRenderer>().color=Color.cyan;
  ObjetoTemplo tele=portal.AddComponent<ObjetoTemplo>();tele.tipo=ObjetoTemplo.Tipo.Portal;tele.destino=destino.transform;
  GameObject muro=Imagem("Muralha do santuario","Assets/3 Objects/Stones/6.png",new Vector2(50,4),new Vector2(2,8),1);Caixa(muro,false);
  GameObject pergaminho=Imagem("Pergaminho da cidade","Assets/4 Animated objects/Chest_open.png",new Vector2(60,.75f),new Vector2(1.2f,1.2f),4);Caixa(pergaminho,true);pergaminho.AddComponent<ObjetoTemplo>().tipo=ObjetoTemplo.Tipo.Pergaminho;
  EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),pasta+"/TravessiaDoTemplo.unity");
  var scenes=EditorBuildSettings.scenes.ToList();scenes.Add(new EditorBuildSettingsScene(pasta+"/TravessiaDoTemplo.unity",true));EditorBuildSettings.scenes=scenes.ToArray();
  EditorSceneManager.OpenScene("Assets/FAses.unity");
  GameObject entrada=new GameObject("Entrada da missao do templo");entrada.AddComponent<EntradaTemplo>();
  Canvas menu=Tela();FundoTexto(menu.transform,new Vector2(.5f,1),Vector2.zero,new Vector2(730,46));
  Text convite=Texto(menu.transform,"Missao adicional","I  -  TRAVESSIA DO TEMPLO: recupere o pergaminho",new Vector2(.5f,1),new Vector2(0,-10),new Vector2(720,38),23);convite.alignment=TextAnchor.UpperCenter;
  EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
  EditorSceneManager.OpenScene(pasta+"/TravessiaDoTemplo.unity");
  Debug.Log("TEMPLO_CONSTRUIDO");
 }
}
