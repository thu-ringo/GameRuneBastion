using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RuneBastion {
public sealed class EnemySpriteAnimator : MonoBehaviour {
    readonly Dictionary<string,Sprite[]> clips=new Dictionary<string,Sprite[]>();
    SpriteRenderer body;Func<Texture,Material> material;string state="walk";float clock;Color tint=Color.white;
    public void Initialize(string folder,SpriteRenderer renderer,Func<Texture,Material> materialFactory,float height){
        body=renderer;material=materialFactory;var all=Resources.LoadAll<Sprite>("Enemies/"+folder);
        foreach(var name in new[]{"idle","walk","attack","hurt","death"})clips[name]=all.Where(s=>s.name.StartsWith(name+"_",StringComparison.Ordinal)).OrderBy(s=>s.name).ToArray();
        if(clips["idle"].Length>0){body.sprite=clips["idle"][0];body.transform.localScale=Vector3.one*(height/body.sprite.bounds.size.y);}
    }
    public void SetState(string next,Color color){if(state=="death")return;if(next!=state){state=next;clock=0;}tint=color;}
    void Update(){
        if(!body)return;clock+=Time.deltaTime;var frames=clips.ContainsKey(state)&&clips[state].Length>0?clips[state]:clips["idle"];if(frames.Length==0)return;
        int index=(int)(clock*(state=="hurt"?16:state=="attack"?12:state=="death"?12:10));index=state=="death"?Mathf.Min(index,frames.Length-1):index%frames.Length;
        body.sprite=frames[index];body.sharedMaterial=material(body.sprite.texture);body.color=tint;
        if(state=="death"){var c=body.color;c.a=Mathf.Clamp01((.85f-clock)*3);body.color=c;}
    }
}
public sealed partial class RuneBastionGame {
    int ChooseEnemyKind(){
        if(Wave==TotalWaves&&waveSpawned==0)return 3;
        double roll=random.NextDouble();
        if(Wave>=4&&roll<.15)return 8;
        if(Wave>=3&&roll<.30)return 7;
        if(Wave>=2&&roll<.45)return 6;
        if(Wave>=2&&roll<.65)return 5;
        if(roll<.85)return 4;
        return Wave>=2?1:0;
    }
    void AttachAnimatedEnemy(Enemy e){
        string folder=e.kind==3?"Troll2":e.kind>=7?"Troll"+(e.kind-6):e.kind>=4?"Orc"+(e.kind-3):null;
        if(folder==null)return;var first=Resources.LoadAll<Sprite>("Enemies/"+folder).FirstOrDefault();if(first==null)return;
        foreach(Transform child in e.art)Destroy(child.gameObject);
        e.art.localScale=Vector3.one;e.body=Render("Animated "+folder,first,Vector3.zero,Color.white,6,e.art);
        e.animator=e.body.gameObject.AddComponent<EnemySpriteAnimator>();e.animator.Initialize(folder,e.body,TextureMaterial,e.kind==3?2.65f:e.kind>=7?1.65f:1.35f);
    }
}
}
