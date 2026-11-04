using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.Linq;
using System.Collections.Generic;
public static class Revisao02Nov {
 class Fundo {public Sprite sprite; public Vector3 pos, escala; public int ordem; public Color cor;}
 static void Rect(RectTransform t,float x,float y,float w,float h){t.anchorMin=t.anchorMax=t.pivot=new Vector2(0,1);t.anchoredPosition=new Vector2(x,-y);t.sizeDelta=new Vector2(w,h);t.localScale=Vector3.one;}
 static void Texto(Text t,float x,float y,float w,int fonte){Rect(t.rectTransform,x,y,w,32);t.fontSize=fonte;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;}
 public static void Aplicar(){
  EditorSceneManager.OpenScene("Assets/Cena2.unity");
  var fundos=new List<Fundo>();
  foreach(var s in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>()) if(s.sprite!=null && s.enabled && (s.sprite.name=="background1" || s.sprite.name=="background4b")) fundos.Add(new Fundo{sprite=s.sprite,pos=s.transform.position,escala=s.transform.lossyScale,ordem=s.sortingOrder,cor=s.color});
  if(fundos.Count<2)throw new Exception("Camadas da Cena2 ausentes");
  Color fundoCamera=Camera.main.backgroundColor;
  foreach(string cena in new[]{"Assets/Cena1.unity","Assets/Cena2.unity","Assets/Bimestres34/TravessiaDoTemplo.unity"}){
   EditorSceneManager.OpenScene(cena);
   foreach(HudRestaurado h in UnityEngine.Object.FindObjectsOfType<HudRestaurado>()){
    Samurai s=h.jogador;
    foreach(Text t in h.GetComponentsInChildren<Text>(true)) {
     if(t.name=="Text")Texto(t,88,15,170,26);
     if(t.name=="Text (1)")t.enabled=false;
    }
    s.UITextVidas.enabled=false;
    Rect(h.transform.Find("HUD retrato original").GetComponent<RectTransform>(),12,8,400,133.333f);
    Rect(h.transform.Find("HUD coletaveis originais").GetComponent<RectTransform>(),82,70,180,55.5f);
    Texto(s.UITextMoedas,101.75f,122,30,22);Texto(s.UITextEspadas,159.25f,122,30,22);Texto(s.UITextSkates,217,122,30,22);
    h.coracoes=new Text[5];
    for(int i=0;i<5;i++){
     string nome="Coracao "+(i+1);Transform antigo=h.transform.Find(nome);GameObject go=antigo!=null?antigo.gameObject:new GameObject(nome,typeof(RectTransform),typeof(Text));go.transform.SetParent(h.transform,false);
     Text t=go.GetComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("Arial.ttf");t.text="\u2665";Texto(t,91+i*30,46,28,25);t.color=new Color(1,.15f,.2f);h.coracoes[i]=t;
    }
    for(int i=0;i<h.iconesProjeteis.Length;i++)Rect(h.iconesProjeteis[i].rectTransform,14+28*i,113,24,24);
    EditorUtility.SetDirty(h);
   }
   foreach(AlvoTemplo a in UnityEngine.Object.FindObjectsOfType<AlvoTemplo>()){
    if(a.GetComponent<GuardaTemplo>()!=null)a.vida=2;
    OldSamurai b=a.GetComponent<OldSamurai>();if(b!=null)a.vida=b.modoMago?5:2;
   }
   if(cena.Contains("Travessia")){
    foreach(SpriteRenderer s in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>())if(s.name.Contains("Caixa do mecanismo")||s.name.Contains("Bau de vento")){
     BoxCollider2D c=s.GetComponent<BoxCollider2D>(); if(c==null)throw new Exception("Caixa sem colisor");
          ProporcaoAnimada proporcao=s.GetComponent<ProporcaoAnimada>();
     if(proporcao!=null){Vector2 nativo=s.sprite.rect.size/s.sprite.pixelsPerUnit;s.size=new Vector2(nativo.x*proporcao.escalaVisual/Mathf.Abs(s.transform.lossyScale.x),nativo.y*proporcao.escalaVisual/Mathf.Abs(s.transform.lossyScale.y));}
     c.size=s.drawMode==SpriteDrawMode.Simple?(Vector2)s.sprite.bounds.size:s.size;
     Vector2 pivot=s.sprite.pivot;Vector2 dimensao=s.sprite.rect.size;
     c.offset=new Vector2((.5f-pivot.x/dimensao.x)*c.size.x,(.5f-pivot.y/dimensao.y)*c.size.y);
     // Superficie original do piso em y=0. A borda inferior da arte coincide com o colisor.
     s.transform.position+=Vector3.up*(.015f-s.bounds.min.y);
    }
    foreach(SpriteRenderer s in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>())if(s.name.StartsWith("Fundo da caverna"))s.enabled=false;
    GameObject velho=GameObject.Find("Fundo igual a Cena2");if(velho!=null)UnityEngine.Object.DestroyImmediate(velho);
    GameObject raiz=new GameObject("Fundo igual a Cena2");
        foreach(string nome in new[]{"background1","background4b"}){
     Fundo f=fundos.First(v=>v.sprite.name==nome);float largura=f.sprite.bounds.size.x*f.escala.x;
     for(float x=-17;x<84;x+=largura){var go=new GameObject(nome);go.transform.SetParent(raiz.transform);go.transform.position=new Vector3(x,4.5f,0);go.transform.localScale=f.escala;var r=go.AddComponent<SpriteRenderer>();r.sprite=f.sprite;r.sortingOrder=nome=="background1"?-51:-50;r.color=f.cor;}
    }
    Camera.main.backgroundColor=fundoCamera;
   }
   EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
  }
  AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Bimestres34/TravessiaDoTemplo.unity");
 }
}

