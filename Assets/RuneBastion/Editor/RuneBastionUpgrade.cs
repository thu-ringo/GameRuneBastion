using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RuneBastion.Editor {
[InitializeOnLoad] public static class RuneBastionUpgrade {
    const string Command="Library/RuneBastion.v2.command",Result="Library/RuneBastion.v2.result";
    static double next;
    static RuneBastionUpgrade(){EditorApplication.update+=Tick;}
    static void Tick(){if(EditorApplication.timeSinceStartup<next||EditorApplication.isCompiling||EditorApplication.isUpdating)return;next=EditorApplication.timeSinceStartup+.3;
        if(!File.Exists(Command))return;
        try{string cmd=File.ReadAllText(Command).Trim();File.Delete(Command);switch(cmd){
            case "import":ImportContent();break;
            case "typography":ImportTypography();break;
            case "upgradea":Game().VerificationUpgradeCards(false);break;
            case "upgradeb":Game().VerificationUpgradeCards(true);break;
            case "verify":Game().VerificationV2();break;
            case "live":Game().VerificationLiveCombat();break;
            case "rune":Game().VerificationRunePanel();break;
            case "selection":Game().VerificationSelection();break;
            case "himmel":Game().VerificationHero(false);break;
            case "frieren":Game().VerificationHero(true);break;
            case "wedding":Game().VerificationEnding(true);break;
            case "loss":Game().VerificationEnding(false);break;
            case "shop":Game().VerificationArmory();break;
            case "capture":RuneBastionEditor.Capture();break;
            default:throw new Exception("Unknown v2 command: "+cmd);
        }File.WriteAllText(Result,cmd+" OK "+DateTime.UtcNow.ToString("O"));}catch(IOException){}catch(Exception ex){File.WriteAllText(Result,ex.ToString());Debug.LogException(ex);}}
    static RuneBastionGame Game(){if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");return UnityEngine.Object.FindFirstObjectByType<RuneBastionGame>();}
    [MenuItem("Rune Bastion/Upgrade v2/Import Heroes and Ending")]
    public static void ImportContent(){
        if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode first");
        foreach(string hero in new[]{"Himmel","Frieren"}){
            string path="Assets/RuneBastion/Resources/Heroes/"+hero+".png";ImportTexture(path,2048,true);
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);string libraryPath="Assets/RuneBastion/Resources/Heroes/"+hero+"Frames.asset";
            var library=AssetDatabase.LoadAssetAtPath<HeroFrameLibrary>(libraryPath);
            if(!library){library=ScriptableObject.CreateInstance<HeroFrameLibrary>();AssetDatabase.CreateAsset(library,libraryPath);}
            var existing=AssetDatabase.LoadAllAssetsAtPath(libraryPath).OfType<Sprite>().ToArray();var frames=new Sprite[24];
            for(int r=0;r<6;r++)for(int c=0;c<4;c++){
                string[] clips={"idle","move","attack","hurt","defeat","victory"};string name=hero+"_"+clips[r]+"_"+c;
                var frame=existing.FirstOrDefault(f=>f.name==name);
                if(!frame){int x=c*texture.width/4,right=(c+1)*texture.width/4,top=r*texture.height/6,bottom=(r+1)*texture.height/6;
                    frame=Sprite.Create(texture,new Rect(x,texture.height-bottom,right-x,bottom-top),new Vector2(.5f,.025f),100,0,SpriteMeshType.FullRect);frame.name=name;AssetDatabase.AddObjectToAsset(frame,library);}
                frames[r*4+c]=frame;
            }
            library.frames=frames;EditorUtility.SetDirty(library);
            Directory.CreateDirectory("Assets/RuneBastion/Animations/"+hero);
            for(int row=0;row<6;row++){
                string[] names={"idle","move","attack","hurt","defeat","victory"};string clipPath="Assets/RuneBastion/Animations/"+hero+"/"+names[row]+".anim";
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,clipPath);}
                float fps=row==2?7:row==1?8:5;clip.frameRate=fps;var keys=new ObjectReferenceKeyframe[5];for(int i=0;i<5;i++)keys[i]=new ObjectReferenceKeyframe{time=i/fps,value=frames[row*4+Math.Min(i,3)]};
                AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},keys);
                var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=row==0||row==1||row==5;AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);
            }
        }
        foreach(string path in Directory.GetFiles("Assets/RuneBastion/Resources/Equipment","*.png"))ImportTexture(path.Replace('\\','/'),256,false);
        ImportTexture("Assets/RuneBastion/Resources/WeddingGarden.png",2048,false);
        var previews=GameObject.Find("Rune Bastion Preview");if(previews){var previous=previews.GetComponentsInChildren<SpriteRenderer>().FirstOrDefault(r=>r.name.Contains("Aster")||r.name.Contains("Himmel"));if(previous){previous.name="Himmel preview";previous.sprite=AssetDatabase.LoadAssetAtPath<HeroFrameLibrary>("Assets/RuneBastion/Resources/Heroes/HimmelFrames.asset").frames[0];previous.flipX=false;previous.transform.position=new Vector3(7,-2.13f,0);previous.transform.localScale=Vector3.one*1.12f;EditorSceneManager.MarkSceneDirty(previous.gameObject.scene);EditorSceneManager.SaveScene(previous.gameObject.scene);}}
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();Debug.Log("RUNE BASTION v2 imported: 48 sprites, 12 clips, wedding and equipment.");
    }
    static void ImportTypography(){
        if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode first");
        foreach(string name in new[]{"hammer","potionBlue","heart"})ImportTexture("Assets/RuneBastion/Resources/Equipment/"+name+".png",256,false);
        var lines=new List<string>();
        foreach(string name in new[]{"UiBody","UiTitle"}){
            string path="Assets/RuneBastion/Resources/Fonts/"+name+".ttf";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var font=AssetDatabase.LoadAssetAtPath<Font>(path);if(!font)throw new Exception("Font missing: "+name);
            string sample="ĐđĂăÂâÊêÔôƠơƯưỮữỄễẤấỘộỲỳ";font.RequestCharactersInTexture(sample,32,FontStyle.Normal);
            foreach(char c in sample)if(!font.HasCharacter(c))throw new Exception("Missing Vietnamese glyph U+"+((int)c).ToString("X4")+" in "+name);
            lines.Add("PASS "+name+" Vietnamese characters imported in Unity");
        }
        AssetDatabase.SaveAssets();File.WriteAllLines("RuneBastionDocs/Verification/typography.txt",lines);
    }
    static void ImportTexture(string path,int max,bool exact){
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.spritePixelsPerUnit=100;imp.mipmapEnabled=false;imp.alphaIsTransparency=true;imp.maxTextureSize=max;imp.npotScale=TextureImporterNPOTScale.None;imp.textureCompression=exact?TextureImporterCompression.Uncompressed:TextureImporterCompression.CompressedHQ;imp.filterMode=FilterMode.Bilinear;imp.SaveAndReimport();
    }
}
}
