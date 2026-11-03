using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RuneBastion {
public static class HeroArt {
    static readonly Dictionary<string,Sprite[]> cache=new Dictionary<string,Sprite[]>();
    public static Sprite[] Frames(string hero,string clip){
        string key=hero+"/"+clip;if(cache.TryGetValue(key,out var found))return found;
        int row=clip=="move"?1:clip=="attack"?2:clip=="hurt"?3:clip=="defeat"?4:clip=="victory"?5:0;
        var library=Resources.Load<HeroFrameLibrary>("Heroes/"+hero+"Frames");
        if(library&&library.frames!=null&&library.frames.Length==24){var saved=new Sprite[4];System.Array.Copy(library.frames,row*4,saved,0,4);return cache[key]=saved;}
        var texture=Resources.Load<Texture2D>("Heroes/"+hero);if(!texture)return cache[key]=new Sprite[0];
        var frames=new Sprite[4];
        for(int c=0;c<4;c++){
            int x=c*texture.width/4,right=(c+1)*texture.width/4,top=row*texture.height/6,bottom=(row+1)*texture.height/6;
            frames[c]=Sprite.Create(texture,new Rect(x,texture.height-bottom,right-x,bottom-top),new Vector2(.5f,.025f),100,0,SpriteMeshType.FullRect);
            frames[c].name=hero+"_"+clip+"_"+c;
        }
        return cache[key]=frames;
    }
}
public sealed class HeroPortrait : MonoBehaviour {
    public string hero="Himmel",clip="idle";public bool faceRight;
    Image image;float clock;
    void Awake(){image=GetComponent<Image>();}
    void Update(){var frames=HeroArt.Frames(hero,clip);if(frames.Length==0)return;clock+=Time.unscaledDeltaTime;
        image.sprite=frames[(int)(clock*(clip=="attack"?7:5))%frames.Length];image.preserveAspect=true;
        transform.localScale=new Vector3(faceRight?-1:1,1,1);
    }
}
}
