using UnityEngine;
using UnityEngine.UI;

public static class MainMenuVisualDesign
{
    private static readonly Color Brass = new Color(.69f,.54f,.32f,1);
    private static readonly Color Paper = new Color(.95f,.88f,.73f,1);
    private static RectTransform Rect(Transform parent,string name,Vector2 position,Vector2 size)
    {
        var t=parent.Find(name) as RectTransform;
        if(t==null){t=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();t.SetParent(parent,false);}
        t.anchorMin=t.anchorMax=t.pivot=new Vector2(.5f,.5f);t.anchoredPosition=position;t.sizeDelta=size;
        return t;
    }
    private static void Fill(RectTransform rect,Color color)
    {
        var im=rect.GetComponent<Image>();if(im==null)im=rect.gameObject.AddComponent<Image>();
        im.sprite=null;im.color=color;im.raycastTarget=false;
    }
    private static void Border(RectTransform parent,Vector2 size,float inset)
    {
        Fill(Rect(parent,"TopRule",new Vector2(0,size.y/2-inset),new Vector2(size.x-inset*2,2)),Brass);
        Fill(Rect(parent,"BottomRule",new Vector2(0,-size.y/2+inset),new Vector2(size.x-inset*2,2)),Brass);
        Fill(Rect(parent,"LeftRule",new Vector2(-size.x/2+inset,0),new Vector2(2,size.y-inset*2)),Brass);
        Fill(Rect(parent,"RightRule",new Vector2(size.x/2-inset,0),new Vector2(2,size.y-inset*2)),Brass);
    }
    private static void Label(Transform parent,string name,string value,Vector2 pos,Vector2 size,int fontSize,Font font,Color color)
    {
        var rt=Rect(parent,name,pos,size);var text=rt.GetComponent<Text>();if(text==null)text=rt.gameObject.AddComponent<Text>();
        text.text=value;text.font=font;text.fontSize=fontSize;text.color=color;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;
    }
    public static void Apply(RectTransform panel)
    {
        var names=new[]{"StartButton","OptionButton","ExitButton"};
        var labels=new[]{"START GAME","SETTINGS","EXIT"};
        Font font=null;
        for(int i=0;i<3;i++)
        {
            var rt=Rect(panel,names[i],new Vector2(0,-155-i*96),new Vector2(400,76));
            var button=rt.GetComponent<Button>();if(button==null)continue;
            var image=rt.GetComponent<Image>();image.sprite=null;image.color=Color.white;image.raycastTarget=true;
            var colors=button.colors;
            colors.normalColor=new Color(.12f,.10f,.08f,.96f);
            colors.highlightedColor=new Color(.28f,.21f,.13f,1);
            colors.selectedColor=colors.highlightedColor;
            colors.pressedColor=new Color(.39f,.29f,.16f,1);
            colors.disabledColor=new Color(.13f,.13f,.13f,.6f);colors.fadeDuration=.12f;button.colors=colors;
            var text=button.GetComponentInChildren<Text>();font=text.font;text.text=labels[i];text.color=Paper;text.fontSize=26;text.fontStyle=FontStyle.Normal;
            Border(rt,new Vector2(400,76),5);
            Label(rt,"Index","0"+(i+1),new Vector2(-164,0),new Vector2(42,42),16,font,Brass);
            Label(rt,"Arrow",">",new Vector2(164,0),new Vector2(32,42),22,font,Brass);
        }
        var box=Rect(panel,"GameTitleBox",new Vector2(0,260),new Vector2(650,218));
        Fill(box,new Color(.10f,.085f,.065f,.88f));Border(box,new Vector2(650,218),10);
        Label(box,"Eyebrow","D E T E C T I V E   A R C H I V E",new Vector2(0,64),new Vector2(580,35),17,font,Brass);
        Label(box,"GameTitle","가제",new Vector2(0,0),new Vector2(580,95),60,font,Paper);
        Label(box,"PlaceholderHint","W O R K I N G   T I T L E",new Vector2(0,-68),new Vector2(580,30),14,font,Brass);
    }
}
