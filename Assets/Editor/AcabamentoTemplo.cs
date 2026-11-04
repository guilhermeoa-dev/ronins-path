using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
public static class AcabamentoTemplo {
 public static void RepararReferencias(){
  EditorSceneManager.OpenScene("Assets/Bimestres34/TravessiaDoTemplo.unity");
  Samurai s=Object.FindObjectOfType<Samurai>();
  s.projeteisMissao=new[]{"Kunai","SeloEspiritual","Bomba"}.Select(n=>AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Bimestres34/"+n+".prefab")).ToArray();
  foreach(GuardaTemplo g in Object.FindObjectsOfType<GuardaTemplo>())g.projetil=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Bimestres34/DisparoGuarda.prefab");
  EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
 }
 static void Placa(string nome,Vector2 pos,string texto,Color cor) {
  if(GameObject.Find(nome)!=null)return;
  GameObject g=new GameObject(nome);g.transform.position=pos;
  TextMesh t=g.AddComponent<TextMesh>();t.text=texto;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.fontSize=36;t.characterSize=.065f;t.color=cor;
  g.GetComponent<MeshRenderer>().sortingOrder=20;
 }
 public static void Aplicar(){
  string arte="Assets/Bimestres34/Arte/Lamina.png";
  if(!File.Exists(arte))File.Copy("Assets/image-Photoroom (2).png",arte);
  AssetDatabase.ImportAsset(arte);
  TextureImporter imp=(TextureImporter)AssetImporter.GetAtPath(arte);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Multiple;imp.spritePixelsPerUnit=100;imp.filterMode=FilterMode.Point;
  imp.spritesheet=new[]{new SpriteMetaData{name="Lamina",rect=new Rect(345,170,110,735),alignment=0,pivot=new Vector2(.5f,.5f)}};imp.SaveAndReimport();
  Sprite sprite=AssetDatabase.LoadAllAssetsAtPath(arte).OfType<Sprite>().First();
  foreach(string nome in new[]{"Kunai","DisparoGuarda"}){
   GameObject prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Bimestres34/"+nome+".prefab");
   GameObject g=Object.Instantiate(prefab);g.transform.localScale=Vector3.one;
   Object.DestroyImmediate(g.GetComponent<SpriteRenderer>());
   g.GetComponent<BoxCollider2D>().size=new Vector2(.65f,.15f);
   GameObject visual=new GameObject("Lamina reaproveitada");visual.transform.SetParent(g.transform,false);visual.transform.localRotation=Quaternion.Euler(0,0,-90);visual.transform.localScale=new Vector3(.15f/sprite.bounds.size.x,.65f/sprite.bounds.size.y,1);
   SpriteRenderer sr=visual.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sortingOrder=8;if(nome=="DisparoGuarda")sr.color=new Color(1,.3f,.3f);
   PrefabUtility.ReplacePrefab(g,prefab,ReplacePrefabOptions.ConnectToPrefab);Object.DestroyImmediate(g);
  }
  EditorSceneManager.OpenScene("Assets/Bimestres34/TravessiaDoTemplo.unity");
  foreach(Canvas c in Object.FindObjectsOfType<Canvas>()){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=Camera.main;c.planeDistance=1;}
  foreach(ObjetoTemplo o in Object.FindObjectsOfType<ObjetoTemplo>()){
   if(o.tipo==ObjetoTemplo.Tipo.CaixaMovel)o.GetComponent<SpriteRenderer>().color=new Color(.45f,.85f,1);
  }
  Placa("Instrucao bau",new Vector2(5,2.3f),"Z / X\nBau de vento",Color.yellow);
  Placa("Instrucao caixa",new Vector2(8,2.3f),"Segure E\nCaixa do mecanismo",Color.cyan);
  Placa("Instrucao selo",new Vector2(15,3.2f),"Solte a caixa sobre o selo",Color.cyan);
  Placa("Instrucao ponte",new Vector2(22,3),"Pegue carona na plataforma",Color.white);
  Placa("Instrucao defesa",new Vector2(29,3.6f),"Q: defesa   |   Z: katana\n1/2/3 + X: projeteis",Color.white);
  Placa("Instrucao portal",new Vector2(46,3),"Portal: derrote os 3 guardas",Color.cyan);
  Placa("Instrucao final",new Vector2(60,2.5f),"Recupere o pergaminho\nno cofre da cidade",Color.yellow);
  EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
  RepararReferencias();
 }
}
