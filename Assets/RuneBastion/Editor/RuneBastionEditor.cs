using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using System.Reflection;

namespace RuneBastion.Editor {
[InitializeOnLoad]
public static class RuneBastionEditor {
    const string Command="Library/RuneBastion.command";
    const string Result="Library/RuneBastion.result";
    const string ScenePath="Assets/RuneBastion/Scenes/RuneBastion.unity";
    static double nextTick;
    static int fullCheck=-1;static double fullDeadline;static System.Collections.Generic.List<string> fullResults;
    static RuneBastionEditor(){EditorApplication.update+=Tick;}
    static void Tick(){
        if(EditorApplication.timeSinceStartup<nextTick||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        nextTick=EditorApplication.timeSinceStartup+.3;
        try {
            if(fullCheck>=0){FullCheckTick();return;}
            if(SessionState.GetBool("RB.PendingAndroid",false)){SessionState.SetBool("RB.PendingAndroid",false);RuneBastionBuild.Android();return;}
            if(File.Exists("Library/RuneBastion.install")&&!EditorApplication.isPlayingOrWillChangePlaymode){
                File.Delete("Library/RuneBastion.install");Install();File.WriteAllText(Result,"INSTALL OK "+DateTime.UtcNow.ToString("O"));return;
            }
            if(!File.Exists(Command))return;var cmd=File.ReadAllText(Command).Trim();File.Delete(Command);
            switch(cmd){
                case "install":Install();break;
                case "play":EditorApplication.isPaused=false;EditorApplication.isPlaying=true;break;
                case "stop":EditorApplication.isPlaying=false;break;
                case "capture":Capture();break;
                case "verify":Verify();break;
                case "demo":Demo();break;
                case "refresh":AssetDatabase.Refresh();break;
                case "configure":RuneBastionBuild.Configure();break;
                case "windows":RuneBastionBuild.Windows();break;
                case "android":RuneBastionBuild.Android();break;
                case "fullverify":BeginFullCheck();break;
                case "ai":VerifyAi();break;
                case "gestures":VerifyGestures();break;
                case "cloud":RuneBastionCloudChecks.Start();break;
                case "menu":Running().VerificationHome();break;
                default:throw new Exception("Unknown Rune Bastion command: "+cmd);
            }
            File.WriteAllText(Result,cmd+" OK "+DateTime.UtcNow.ToString("O"));
        }catch(IOException){return;}catch(Exception e){fullCheck=-1;File.WriteAllText(Result,e.ToString());Debug.LogException(e);}
    }
    [MenuItem("Rune Bastion/Build Game Scene")]
    public static void Install(){
        if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode first");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        foreach(string path in Directory.GetFiles("Assets/RuneBastion/Resources","*.png",SearchOption.AllDirectories)){
            var imp=AssetImporter.GetAtPath(path.Replace('\\','/')) as TextureImporter;if(imp==null)continue;
            imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;
            imp.spritePixelsPerUnit=100;imp.mipmapEnabled=false;imp.alphaIsTransparency=true;imp.maxTextureSize=2048;
            imp.maxTextureSize=path.Contains("Enemies")?512:path.Contains("Wizard")?512:2048;
            imp.textureCompression=TextureImporterCompression.CompressedHQ;imp.filterMode=FilterMode.Bilinear;
            var settings=new TextureImporterSettings();imp.ReadTextureSettings(settings);
            settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=(path.Contains("Wizard")||path.Contains("Enemies"))?new Vector2(.5f,0):new Vector2(.5f,.5f);imp.SetTextureSettings(settings);imp.SaveAndReimport();
        }
        var root=new GameObject("Rune Bastion - Game Controller");root.AddComponent<RuneBastionGame>();
        if(!AssetDatabase.LoadAssetAtPath<Material>("Assets/RuneBastion/Resources/RuneSprite.mat"))
            AssetDatabase.CreateAsset(new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")),"Assets/RuneBastion/Resources/RuneSprite.mat");
        var preview=new GameObject("Rune Bastion Preview");
        var cam=new GameObject("Preview Camera",typeof(Camera));cam.transform.position=new Vector3(0,0,-10);cam.tag="MainCamera";cam.GetComponent<Camera>().orthographic=true;cam.GetComponent<Camera>().orthographicSize=5;
        var bg=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RuneBastion/Resources/Battlefield.png");
        if(bg){var g=new GameObject("Twilight battlefield",typeof(SpriteRenderer));g.transform.SetParent(preview.transform);var sr=g.GetComponent<SpriteRenderer>();sr.sprite=bg;sr.sortingOrder=-20;g.transform.localScale=new Vector3(17.78f/bg.bounds.size.x,10/bg.bounds.size.y,1);}
        var hero=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RuneBastion/Resources/Wizard/idle_1.png");
        if(hero){var g=new GameObject("Aster preview",typeof(SpriteRenderer));g.transform.SetParent(preview.transform);g.transform.position=new Vector3(7,.25f,0);g.transform.localScale=Vector3.one*.5f;var sr=g.GetComponent<SpriteRenderer>();sr.sprite=hero;sr.flipX=true;sr.sortingOrder=10;}
        Directory.CreateDirectory("Assets/RuneBastion/Scenes");EditorSceneManager.SaveScene(scene,ScenePath);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
        PlayerSettings.productName="RUNE BASTION";PlayerSettings.companyName="Lumen Studio";
        PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;
        PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToLandscapeLeft=true;PlayerSettings.allowedAutorotateToLandscapeRight=true;
        PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
        PlayerSettings.runInBackground=true;
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android,"com.lumenstudio.runebastion");
        QualitySettings.vSyncCount=0;
        // Existing source was copied out of Assets before deletion; keep packages and shared settings.
        if(!Directory.GetDirectories("LegacyBackup","BeforeRuneBastion-*").Any())throw new Exception("Legacy backup missing. Refusing cleanup.");
        if(AssetDatabase.IsValidFolder("Assets/W8"))AssetDatabase.DeleteAsset("Assets/W8");
        if(AssetDatabase.IsValidFolder("Assets/StreamingAssets/Dataset"))AssetDatabase.DeleteAsset("Assets/StreamingAssets/Dataset");
        RuneBastionBuild.Configure();AssetDatabase.SaveAssets();SetGameSize(1600,900);Debug.Log("RUNE BASTION scene installed. Legacy Animal Explorer archived outside Assets.");
    }
    static void SetGameSize(int width,int height){
        try{var asm=typeof(UnityEditor.Editor).Assembly;var type=asm.GetType("UnityEditor.GameViewSizes");var singleton=typeof(ScriptableSingleton<>).MakeGenericType(type);
        var sizes=singleton.GetProperty("instance",BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy).GetValue(null);
        var group=type.GetMethod("GetGroup").Invoke(sizes,new object[]{0});var sizeType=asm.GetType("UnityEditor.GameViewSize");var enumType=asm.GetType("UnityEditor.GameViewSizeType");
        var size=Activator.CreateInstance(sizeType,new object[]{Enum.ToObject(enumType,1),width,height,"Rune Bastion 16:9"});group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
        int total=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);var vt=asm.GetType("UnityEditor.GameView");var view=EditorWindow.GetWindow(vt);
        vt.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(view,total-1);view.Show();view.Focus();}catch(Exception e){Debug.LogWarning("Set Game View manually to 16:9: "+e.Message);}
    }
    static RuneBastionGame Running(){var game=UnityEngine.Object.FindFirstObjectByType<RuneBastionGame>();if(!EditorApplication.isPlaying||!game)throw new Exception("Game must be in Play Mode");return game;}
    [MenuItem("Rune Bastion/Verify Combat")]
    public static void Verify(){
        var game=Running();var checks=new List<string>();Action<bool,string> check=(yes,msg)=>{if(!yes)throw new Exception("FAIL: "+msg);checks.Add("PASS: "+msg);};
        game.StartRun();check(game.Wave==1&&game.Health==1000,"New run starts at wave 1 with full castle health");
        game.VerificationSpawn();check(game.EnemyCount==3,"Three enemy types spawn");
        check(game.CastSpell(1),"Ice consumes mana and activates");check(game.CastSpell(2),"Lightning activates after ice");check(game.Score>0&&game.Kills>0,"Real combat kills enemies and awards score");
        check(game.Mana<100,"Skills spend energy");check(!game.CastSpell(2),"Cooldown rejects repeated lightning");
        game.TogglePause();check(game.Paused&&Time.timeScale==0,"Pause stops simulation");game.TogglePause();check(!game.Paused&&Time.timeScale==1,"Resume restores simulation");
        check(EditorBuildSettings.scenes.Length==1&&EditorBuildSettings.scenes[0].path==ScenePath,"Only Rune Bastion in build scenes");
        check(!AssetDatabase.IsValidFolder("Assets/W8"),"Legacy Animal Explorer removed from active Assets");
        check(Resources.LoadAll<Sprite>("Wizard").Length>=20,"CraftPix animation frames imported");
        Directory.CreateDirectory("RuneBastionDocs/Verification");File.WriteAllLines("RuneBastionDocs/Verification/smoke-checks.txt",checks);
        game.StartRun();Debug.Log("RUNE BASTION verification: "+checks.Count+" checks passed");
    }
    static void Demo(){var game=Running();game.StartRun();game.VerificationSpawn();game.CastSpell(1);game.CastSpell(0);}
    [MenuItem("Rune Bastion/Capture Game")]
    public static void Capture(){Running();Directory.CreateDirectory("RuneBastionDocs/Verification");ScreenCapture.CaptureScreenshot("RuneBastionDocs/Verification/gameplay.png");}
    [Serializable] class GoldenSet{public GoldenSample[] samples;}
    [Serializable] class GoldenSample{public int label;public float[] pixels;public float[] probabilities;}
    static void VerifyAi(){var data=JsonUtility.FromJson<GoldenSet>(Resources.Load<TextAsset>("AI/golden").text);var results=new System.Collections.Generic.List<string>();
        using(var classifier=new RuneClassifier()){foreach(var sample in data.samples){var output=classifier.Predict(sample.pixels);int prediction=Array.IndexOf(output,output.Max());float delta=output.Zip(sample.probabilities,(a,b)=>Math.Abs(a-b)).Max();if(prediction!=sample.label||delta>.001f)throw new Exception("AI golden test failed: "+sample.label+" delta "+delta);results.Add("PASS ONNX class "+sample.label+" numerical delta "+delta);}}
        File.WriteAllLines("RuneBastionDocs/Verification/ai-unity.txt",results);
    }
    static void VerifyGestures(){var results=new System.Collections.Generic.List<string>();
        var go=new GameObject("Gesture pipeline verification",typeof(RectTransform),typeof(RawImage),typeof(RuneDrawingPad));var pad=go.GetComponent<RuneDrawingPad>();pad.Initialize();
        var samples=new System.Collections.Generic.List<Vector2[]>();samples.Add(new[]{new Vector2(180,15),new Vector2(330,235),new Vector2(30,235),new Vector2(180,15)});
        samples.Add(Enumerable.Range(0,65).Select(i=>new Vector2(180+100*Mathf.Cos(i*Mathf.PI/32),128+100*Mathf.Sin(i*Mathf.PI/32))).ToArray());
        samples.Add(new[]{new Vector2(240,10),new Vector2(100,135),new Vector2(220,115),new Vector2(120,245)});
        using(var classifier=new RuneClassifier()){for(int i=0;i<samples.Count;i++){pad.Clear();pad.Strokes.Add(samples[i].ToList());var values=classifier.Predict(pad.Rasterize());int predicted=Array.IndexOf(values,values.Max());results.Add((predicted==i?"PASS":"FAIL")+" drawn gesture "+i+" predicted "+predicted+" confidence "+values.Max());}}
        UnityEngine.Object.Destroy(go);File.WriteAllLines("RuneBastionDocs/Verification/gesture-pipeline.txt",results);if(results.Any(x=>x.StartsWith("FAIL")))throw new Exception("Gesture pipeline mismatch; inspect gesture-pipeline.txt");
    }
    static void BeginFullCheck(){EditorApplication.isPaused=false;var g=Running();g.VerificationSetMode(true);g.StartRun();fullCheck=0;fullDeadline=EditorApplication.timeSinceStartup+90;fullResults=new System.Collections.Generic.List<string>();}
    static void FullCheckTick(){var g=Running();if(EditorApplication.timeSinceStartup>fullDeadline){fullCheck=-1;throw new Exception("Full run verification timed out");}
        if(g.State==RuneBastionGame.Phase.Battle){g.VerificationResolveWave();return;}
        if(g.State==RuneBastionGame.Phase.Upgrade){fullResults.Add("PASS wave "+g.Wave+" -> upgrade -> next wave");g.SelectUpgrade(g.Wave%6);return;}
        if(g.State==RuneBastionGame.Phase.Won&&fullCheck==0){if(g.Wave!=RuneBastionGame.TotalWaves)throw new Exception("Victory at wrong wave");fullResults.Add("PASS wave "+g.Wave+" boss -> victory");g.StartRun();g.VerificationLose();fullCheck=1;return;}
        if(g.State==RuneBastionGame.Phase.Lost&&fullCheck==1){fullResults.Add("PASS castle destroyed -> defeat");g.StartRun();if(g.Wave!=1||g.Health!=1000)throw new Exception("Retry failed");fullResults.Add("PASS retry resets run");g.VerificationSetMode(false);g.VerificationHome();fullResults.Add("PASS return to main menu");fullCheck=-1;File.WriteAllLines("RuneBastionDocs/Verification/full-run.txt",fullResults);File.WriteAllText(Result,"FULL RUN PASS "+fullResults.Count);}
    }
}
}
