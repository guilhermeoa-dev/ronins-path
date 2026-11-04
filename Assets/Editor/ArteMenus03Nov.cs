using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.Linq;
using System.Collections.Generic;

public static class ArteMenus03Nov {
    static Font fonte;
    static Color marfim=new Color(.96f,.96f,.9f),ouro=new Color(.83f,.72f,.43f);
    static RectTransform Ret(Transform pai,string nome,Vector2 ancora,float x,float y,float w,float h) {
        GameObject go=new GameObject(nome,typeof(RectTransform));go.transform.SetParent(pai,false);
        RectTransform r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=ancora;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);return r;
    }
    static Text Texto(Transform pai,string nome,string valor,Vector2 ancora,float x,float y,float w,float h,int tamanho,Color cor,FontStyle estilo=FontStyle.Normal) {
        Text t=Ret(pai,nome,ancora,x,y,w,h).gameObject.AddComponent<Text>();t.font=fonte;t.text=valor;t.fontSize=tamanho;t.fontStyle=estilo;t.color=cor;t.raycastTarget=false;
        t.alignment=ancora.x==1?TextAnchor.MiddleRight:TextAnchor.MiddleLeft;
        t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;
        Shadow sombra=t.gameObject.AddComponent<Shadow>();sombra.effectColor=new Color(0,0,0,.5f);sombra.effectDistance=new Vector2(0,-2);return t;
    }
    static Image Linha(Transform pai,Vector2 ancora,float x,float y,float w,float h,Color cor) {
        Image i=Ret(pai,"Detalhe",ancora,x,y,w,h).gameObject.AddComponent<Image>();i.color=cor;i.raycastTarget=false;return i;
    }
    static MenuPintado.Opcao Botao(Transform pai,string tag,string legenda,float y) {
        GameObject go=GameObject.FindGameObjectWithTag(tag);if(go==null){go=new GameObject("Clique - "+tag);go.tag=tag;}
        BoxCollider2D col=go.GetComponent<BoxCollider2D>();if(col==null)col=go.AddComponent<BoxCollider2D>();col.enabled=true;col.isTrigger=false;
        if(go.GetComponent<ClickAsset>()==null)go.AddComponent<ClickAsset>();
        RectTransform area=Ret(pai,"Opcao - "+tag,new Vector2(1,0),-80,y,330,48);
        Image destaque=area.gameObject.AddComponent<Image>();destaque.color=new Color(.56f,.46f,.22f,.25f);destaque.raycastTarget=false;
        Text texto=Texto(area,"Legenda",legenda,new Vector2(1,.5f),-14,0,285,48,23,marfim);
        Text marcador=Texto(area,"Selecao","◆",new Vector2(0,.5f),12,0,28,40,19,ouro);
        return new MenuPintado.Opcao{area=area,colisor=col,texto=texto,marcador=marcador,destaque=destaque};
    }
    public static void Aplicar() {
        PlayerSettings.productName="Ronin`s Path";
        fonte=Resources.GetBuiltinResource<Font>("Arial.ttf");
        foreach(string nome in new[]{"Menu","Selecao","Vitoria1","Vitoria2","Vitoria3","Derrota"}) {
            string path="Assets/Arte03Nov/"+nome+".png";TextureImporter imp=(TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType=TextureImporterType.Default;imp.mipmapEnabled=false;imp.wrapMode=TextureWrapMode.Clamp;imp.maxTextureSize=2048;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.SaveAndReimport();
        }
        foreach(string cena in new[]{"PrimeiraTela","FAses","Vitoria1","Vitoria2","Vitoria3","GameOver"}) {
            EditorSceneManager.OpenScene("Assets/"+cena+".unity");
            GameObject anterior=GameObject.Find("Menu pintado");if(anterior!=null)UnityEngine.Object.DestroyImmediate(anterior);
            foreach(Renderer r in UnityEngine.Object.FindObjectsOfType<Renderer>())r.enabled=false;
            foreach(Canvas c in UnityEngine.Object.FindObjectsOfType<Canvas>())c.enabled=false;
            Camera cam=Camera.main;cam.rect=new Rect(0,0,1,1);cam.backgroundColor=new Color(.035f,.055f,.05f);
            GameObject raiz=new GameObject("Menu pintado",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(MenuPintado));
            Canvas canvas=raiz.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;canvas.sortingOrder=200;
            CanvasScaler scaler=raiz.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=0;
            string arte=cena=="PrimeiraTela"?"Menu":cena=="FAses"?"Selecao":cena=="GameOver"?"Derrota":cena;
            Texture2D tex=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Arte03Nov/"+arte+".png");
            RectTransform fundo=Ret(raiz.transform,"Pintura - "+arte,new Vector2(.5f,.5f),0,0,1280,720);
            RawImage imagem=fundo.gameObject.AddComponent<RawImage>();imagem.texture=tex;imagem.raycastTarget=false;
            AspectRatioFitter fit=fundo.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=(float)tex.width/tex.height;
            // Elementos UI reais mantem texto limpo e clicavel em diferentes resolucoes.
            var opcoes=new List<MenuPintado.Opcao>();Vector2 esquerda=new Vector2(0,0),direita=new Vector2(1,0);
            if(cena=="PrimeiraTela") {
                Texto(raiz.transform,"Titulo","RONIN`S",esquerda,80,224,470,88,76,marfim,FontStyle.Bold);
                Texto(raiz.transform,"Titulo inferior","PATH",esquerda,80,113,460,150,100,marfim,FontStyle.Bold);
                Texto(raiz.transform,"Subtitulo","MENU PRINCIPAL",esquerda,86,97,470,30,19,marfim);
                opcoes.Add(Botao(raiz.transform,"botao_start","Jogar   [K]",202));opcoes.Add(Botao(raiz.transform,"fechar","Sair   [L]",148));
            } else if(cena=="FAses") {
                Texto(raiz.transform,"Titulo","SELEÇÃO\nDE FASES",esquerda,80,130,500,150,47,marfim,FontStyle.Bold);
                Texto(raiz.transform,"Subtitulo","Ronin`s Path",esquerda,83,98,490,30,15,marfim);
                opcoes.Add(Botao(raiz.transform,"botao_fase1","01   Floresta   [K]",310));opcoes.Add(Botao(raiz.transform,"botao_fase2","02   Caverna   [J]",256));opcoes.Add(Botao(raiz.transform,"botao_templo","03   Templo   [I]",202));opcoes.Add(Botao(raiz.transform,"botao_menu","Voltar ao menu   [L]",148));
            } else {
                bool derrota=cena=="GameOver";
                string legenda=derrota?"A jornada ainda não terminou.":cena=="Vitoria1"?"A floresta ficou para trás.":cena=="Vitoria2"?"Um novo caminho se revela.":"O mago caiu. O pergaminho está a salvo.";
                Texto(raiz.transform,"Capitulo",derrota?"Ronin`s Path":cena=="Vitoria3"?"A JORNADA CONCLUÍDA":"ETAPA CONCLUÍDA",esquerda,84,269,490,28,16,ouro);
                Texto(raiz.transform,"Titulo",derrota?"DERROTA":"VITÓRIA",esquerda,80,173,510,100,76,marfim,FontStyle.Bold);
                Texto(raiz.transform,"Subtitulo",legenda,esquerda,84,125,480,48,21,marfim);
                opcoes.Add(Botao(raiz.transform,"reiniciar","Voltar ao menu   [K]",148));
            }
            Texto(raiz.transform,"Nome do jogo","Ronin`s Path",new Vector2(0,1),80,-38,400,30,18,marfim);
            Linha(raiz.transform,direita,-80,126,330,1,new Color(.85f,.82f,.67f,.45f));
            Texto(raiz.transform,"Instrucoes","Clique para escolher  ·  ↑ ↓ e Enter",direita,-80,74,350,30,16,marfim);
            MenuPintado menu=raiz.GetComponent<MenuPintado>();menu.cameraMenu=cam;menu.opcoes=opcoes.ToArray();menu.Atualizar();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
        }
        AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/PrimeiraTela.unity");
    }
}



