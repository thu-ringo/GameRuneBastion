using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace RuneBastion {
public sealed partial class RuneBastionGame : MonoBehaviour {
    public enum Phase { Menu, Battle, Upgrade, Won, Lost }
    public Phase State { get; private set; }
    public int Wave { get; private set; }
    public int Score { get; private set; }
    public int Kills { get; private set; }
    public float Health { get; private set; } = 1000;
    public float Mana { get; private set; } = 100;
    public int EnemyCount => enemies.Count;
    public const int TotalWaves = 5;
    public bool Paused { get; private set; }
    const float Ground = -2.13f, Gate = 6.15f;
    readonly List<Enemy> enemies = new List<Enemy>();
    readonly List<Shot> shots = new List<Shot>();
    readonly List<Visual> effects = new List<Visual>();
    readonly Dictionary<string,Sprite[]> wizardClips = new Dictionary<string,Sprite[]>();
    readonly Dictionary<Texture,Material> textureMaterials = new Dictionary<Texture,Material>();
    readonly float[] cooldown = new float[3];
    readonly int[] costs = { 30, 25, 40 };
    readonly Color[] spellColors = { new Color(1,.38f,.13f), new Color(.25f,.9f,1), new Color(1,.85f,.24f) };
    Camera gameCamera; Transform world, scenery; Canvas canvas; RectTransform uiRoot;
    Font font; Sprite disk, square; Material spriteMaterial; SpriteRenderer wizard;
    GameObject overlay; Text hpText, manaText, waveText, scoreText, tip, status;
    Image hpFill, manaFill, progress; Text[] skillLabels = new Text[3];
    AudioSource audioSource; AudioClip fireSound, iceSound, lightningSound, hitSound;
    float spawnClock, autoClock, animationClock, castUntil, damageMultiplier=1, attackInterval=.85f, manaRegen=6;
    float elapsed, shake, hitFlash, targetX; int toSpawn, waveSpawned, extraChains; bool upgradedIce;
    string wizardClip="idle"; Vector3 cameraOrigin;
    System.Random random;
    float lastHitSound;

    sealed class Enemy {
        public GameObject root; public Transform art; public SpriteRenderer body; public Image unused;
        public Transform hp; public float health,maxHealth,speed,attackClock,frozenUntil,burnUntil,burnTick,seed;
        public int kind; public bool alive=true,pursuer,feignsDeath; public GameObject encounterMark; public Vector3 scale; public EnemySpriteAnimator animator; public float baseSpeed; public float hitUntil;
    }
    sealed class Shot { public Transform root; public Enemy target; public Vector3 from; public float t; public bool basic; public float damage; public bool blade,piercing; public System.Collections.Generic.HashSet<Enemy> hit=new System.Collections.Generic.HashSet<Enemy>(); }
    sealed class Visual { public Transform root; public float time,life; public Vector3 velocity,scale; public SpriteRenderer sprite; public TextMesh text; }

    void Awake() {
        Time.timeScale=1; Application.targetFrameRate=60; Screen.sleepTimeout=SleepTimeout.NeverSleep;
        foreach(var c in FindObjectsByType<Camera>()) if(c.gameObject!=gameObject) Destroy(c.gameObject);
        foreach(var c in FindObjectsByType<Canvas>()) Destroy(c.gameObject);
        var preview=GameObject.Find("Rune Bastion Preview"); if(preview) preview.SetActive(false);
        random=new System.Random(); font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        spriteMaterial=new Material(Shader.Find("Sprites/Default"));spriteMaterial.mainTexture=Texture2D.whiteTexture;
        disk=MakeDisk(); square=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);
        world=new GameObject("Battlefield Runtime").transform;
        var cam=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)); cam.tag="MainCamera";
        gameCamera=cam.GetComponent<Camera>();gameCamera.orthographic=true;gameCamera.orthographicSize=5;gameCamera.cullingMask=1<<29;
        cameraOrigin=new Vector3(0,0,-10);cam.transform.position=cameraOrigin;gameCamera.backgroundColor=new Color(.025f,.035f,.09f);
        scenery=new GameObject("Battlefield scenery").transform;
        scenery.SetParent(world,false);
        var bg=Resources.Load<Sprite>("Battlefield");
        if(bg) { var b=Render("Twilight Valley",bg,Vector3.zero,Color.white,-20,scenery); b.transform.localScale=new Vector3(17.78f/bg.bounds.size.x,10/bg.bounds.size.y,1); }
        LoadHeroProfile(); LoadWizard(); CreateWizard(); CreateUi(); CreateAudio(); CreateStars(); InitializeCloud(); ShowMenu();
    }

    void LoadWizard() {RefreshHeroArt();}
    void CreateWizard() {
        Sprite s=wizardClips["idle"].FirstOrDefault();if(!s)return;
        wizard=Render(HeroName+" - Player",s,new Vector3(7,.25f,0),Color.white,10);
        wizard.flipX=false;wizard.transform.localScale=Vector3.one*1.12f;
    }
    SpriteRenderer Render(string name,Sprite sprite,Vector3 position,Color color,int order,Transform parent=null) {
        var g=new GameObject(name,typeof(SpriteRenderer));g.transform.SetParent(parent?parent:world,false);g.transform.localPosition=position;
        var r=g.GetComponent<SpriteRenderer>();r.sprite=sprite;r.color=color;r.sortingOrder=order;r.sharedMaterial=TextureMaterial(sprite.texture);g.layer=29;return r;
    }
    Material TextureMaterial(Texture texture) {
        if(!textureMaterials.TryGetValue(texture,out var mat)) {mat=new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));mat.mainTexture=texture;textureMaterials[texture]=mat;}
        return mat;
    }
    SpriteRenderer Shape(string name,Transform parent,Vector3 position,Vector3 scale,Color color,int order,bool round=true) {
        var r=Render(name,round?disk:square,position,color,order,parent);r.transform.localScale=scale;return r;
    }
    Sprite MakeDisk() {
        var tex=new Texture2D(96,96,TextureFormat.RGBA32,false);tex.name="Procedural soft disc";
        for(int y=0;y<96;y++)for(int x=0;x<96;x++) {float d=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(48,48))/47;
            float a=Mathf.Clamp01((1-d)*16);tex.SetPixel(x,y,new Color(1,1,1,a));}
        tex.Apply();return Sprite.Create(tex,new Rect(0,0,96,96),new Vector2(.5f,.5f),96);
    }
    void CreateStars() {
        for(int i=0;i<22;i++) {var p=new Vector3(UnityEngine.Random.Range(-8.5f,5.8f),UnityEngine.Random.Range(-1.4f,3.7f),0);
            var s=Shape("Ambient firefly",scenery,p,Vector3.one*UnityEngine.Random.Range(.025f,.055f),new Color(.55f,.8f,1,.55f),-3);
            effects.Add(new Visual{root=s.transform,sprite=s,time=0,life=10000,velocity=new Vector3(.015f,.025f,0),scale=s.transform.localScale});}
    }

    RectTransform Node(string name,Transform parent,float x,float y,float w,float h) {
        var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);
        r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
    }
    Image Panel(string name,Transform p,float x,float y,float w,float h,Color c) {var n=Node(name,p,x,y,w,h);var im=n.gameObject.AddComponent<Image>();im.color=c;return im;}
    Text Label(string name,Transform p,string value,float x,float y,float w,float h,int size,Color c,TextAnchor alignment=TextAnchor.MiddleLeft) {
        var n=Node(name,p,x,y,w,h);var t=n.gameObject.AddComponent<Text>();t.font=font;t.text=(value??"").Normalize(System.Text.NormalizationForm.FormC);t.fontSize=size;t.color=c;t.alignment=alignment;
        t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;
    }




    public void StartRun() {
        if(overlay)Destroy(overlay); ClearBattle();State=Phase.Battle;Paused=false;Time.timeScale=1;
        hudRoot.gameObject.SetActive(true); random=dailyMode?new System.Random(dailySeed):new System.Random();
        ResetRescue();Wave=0;Score=Kills=0;Health=1000;Mana=100;elapsed=0;damageMultiplier=1;attackInterval=.85f;manaRegen=6;extraChains=0;upgradedIce=false;targetX=Gate;
        if(!dailyMode){damageMultiplier+=PlayerPrefs.GetInt("RB.ForgeDamage",0)*.08f;manaRegen+=PlayerPrefs.GetInt("RB.ForgeMana",0);attackInterval*=Mathf.Pow(.93f,PlayerPrefs.GetInt("RB.ForgeSpeed",0));}
        gateGuard=0;PrepareHeroRun();Array.Clear(cooldown,0,3);autoClock=.4f;BeginWave();RefreshHud();
    }
    void ClearBattle() {
        ClearRescueActors();
        ClearWaveEncounter();
        foreach(var e in enemies)if(e.root)Destroy(e.root);enemies.Clear();
        foreach(var s in shots)if(s.root)Destroy(s.root.gameObject);shots.Clear();
    }
    public void BeginWave() {
        Wave++;State=Phase.Battle;toSpawn=Wave==TotalWaves?5:5+Wave*2;waveSpawned=0;spawnClock=1.4f;
        PrepareWaveEncounter();
        tip.text=Wave==TotalWaves?"NOX XUẤT HIỆN — GIỮ CỔNG THÀNH!":"ĐỢT "+Wave+"  •  Chuẩn bị phòng thủ";
        StartCoroutine(ClearTipAfter(3));
    }
    IEnumerator ClearTipAfter(float seconds) {yield return new WaitForSeconds(seconds);tip.text="";}
    void Update() {
        TickAr();
        if(Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame)TogglePause();
        if(!Paused)AnimateWizard();
        if(State!=Phase.Battle || Paused)return;
        float dt=Mathf.Min(Time.deltaTime,.05f);elapsed+=dt;Mana=Mathf.Min(100,Mana+manaRegen*dt);
        for(int i=0;i<3;i++){cooldown[i]=Mathf.Max(0,cooldown[i]-dt);runeCooldown[i]=Mathf.Max(0,runeCooldown[i]-dt);}
        HandleInput();spawnClock-=dt;
        if(toSpawn>0 && spawnClock<=0) {SpawnWaveEnemy();toSpawn--;waveSpawned++;spawnClock=Mathf.Max(.48f,1.5f-Wave*.08f);}
        TickRescue(dt);TickWaveEncounter();TickEnemies(dt);TickShots(dt);TickEffects(dt);
        autoClock-=dt;if(autoClock<=0) {var target=PickTarget();if(target!=null){Shoot(target);autoClock=attackInterval;}else autoClock=.1f;}
        hitFlash=Mathf.Max(0,hitFlash-dt);shake=Mathf.Max(0,shake-dt*2);
        gameCamera.transform.position=cameraOrigin+(Vector3)UnityEngine.Random.insideUnitCircle*shake*.07f;
        if(Health<=0)EndRun(false);else if(!RescueInProgress && dormantEnemies.Count==0 && toSpawn==0 && enemies.Count==0 && shots.Count==0){if(Wave>=TotalWaves)EndRun(true);else ShowUpgrades();}
        RefreshHud();
    }
    void LateUpdate() {if(State!=Phase.Battle && !Paused)TickEffects(Time.deltaTime);}
    void HandleInput() {
        if(Keyboard.current!=null) {
            if(Keyboard.current.digit1Key.wasPressedThisFrame)CastHeroSkill(0);
            if(Keyboard.current.digit2Key.wasPressedThisFrame)CastHeroSkill(1);
            if(Keyboard.current.digit3Key.wasPressedThisFrame)CastHeroSkill(2);
        }
        Vector2 screen=Vector2.zero;bool click=false;
        if(Mouse.current!=null && Mouse.current.leftButton.wasPressedThisFrame){screen=Mouse.current.position.ReadValue();click=true;}
        if(Touchscreen.current!=null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame){screen=Touchscreen.current.primaryTouch.position.ReadValue();click=true;}
        if(click && screen.y>Screen.height*.22f && screen.y<Screen.height*.85f && !overlay) {
            Vector3 p;if(arMode){if(!TryArScreenToGame(screen,out p))return;}else p=gameCamera.ScreenToWorldPoint(new Vector3(screen.x,screen.y,10));
            if(mirrorSpell>=0 && Vector2.Distance(mirrorFrame.position,p)<.8f){FocusMirror();return;}
            var e=enemies.OrderBy(n=>Vector2.Distance(n.root.transform.position,p)).FirstOrDefault();
            if(e!=null && Vector2.Distance(e.root.transform.position+Vector3.up*.4f,p)<1.5f){mirrorFocused=e==mirrorBearer && mirrorSpell>=0;targetX=e.root.transform.position.x;Ring(e.root.transform.position+Vector3.up*.3f,.8f,new Color(1,.8f,.3f));}
        }
    }
    Enemy PickTarget() {
        if(mirrorFocused && mirrorBearer!=null && mirrorBearer.alive)return mirrorBearer;
        if(enemies.Count==0)return null;
        return enemies.OrderByDescending(e=>RescueInProgress && e.pursuer).ThenBy(e=>Mathf.Abs(e.root.transform.position.x-targetX)).ThenByDescending(e=>e.root.transform.position.x).FirstOrDefault();
    }
    Enemy Spawn(int kind) {
        var root=new GameObject(kind==3?"Nox - Boss":kind==2?"Brum - Golem":kind==1?"Pip - Bat":"Mochi - Slime");root.transform.SetParent(world,false);
        root.transform.position=new Vector3(-9.5f,kind==1?Ground+1.55f:Ground,0);
        var art=new GameObject("Animated body").transform;art.SetParent(root.transform,false);
        float max=(kind==3?1800:kind==8?260:kind==7?190:kind==6?130:kind==5?95:kind==4?65:kind==2?170:kind==1?55:75)*(1+Wave*.07f);
        var e=new Enemy{root=root,art=art,health=max,maxHealth=max,kind=kind,speed=kind==3?.3f:kind==8?.36f:kind==7?.42f:kind==6?.5f:kind==5?.7f:kind==4?.95f:kind==2?.32f:kind==1?.95f:.48f,seed=(float)random.NextDouble()*10};
        Color dark=new Color(.16f,.08f,.29f), purple=new Color(.42f,.2f,.68f), glow=new Color(.8f,.38f,1);
        if(kind==0) {
            Shape("Shadow",art,new Vector3(0,.01f,0),new Vector3(.95f,.15f,1),new Color(.04f,.025f,.1f,.65f),2);
            e.body=Shape("Slime body",art,new Vector3(0,.4f,0),new Vector3(.9f,.78f,1),dark,5);
            Shape("Gloss",art,new Vector3(-.17f,.6f,0),new Vector3(.23f,.12f,1),new Color(.5f,.3f,.69f,.7f),6);
            Eyes(art,.45f,.19f,glow,7);Shape("Little smile",art,new Vector3(0,.26f,0),new Vector3(.13f,.035f,1),new Color(.8f,.5f,.9f),7);
        } else if(kind==1) {
            e.body=Shape("Bat body",art,new Vector3(0,.35f,0),new Vector3(.48f,.6f,1),dark,5);
            for(int side=-1;side<=1;side+=2){var wing=Shape("Wing",art,new Vector3(side*.43f,.4f,0),new Vector3(.7f,.28f,1),purple,4);wing.transform.localRotation=Quaternion.Euler(0,0,side*23);Shape("Ear",art,new Vector3(side*.16f,.67f,0),new Vector3(.12f,.3f,1),purple,5);}
            Eyes(art,.4f,.12f,glow,7);
        } else {
            float size=kind==3?1.65f:1.1f;
            e.body=Shape("Stone torso",art,new Vector3(0,.65f,0),new Vector3(.85f,1.05f,1),new Color(.24f,.22f,.38f),5,false);
            Shape("Head",art,new Vector3(0,1.25f,0),new Vector3(.7f,.55f,1),new Color(.38f,.33f,.49f),6,false);
            for(int side=-1;side<=1;side+=2){Shape("Arm",art,new Vector3(side*.58f,.66f,0),new Vector3(.32f,.8f,1),new Color(.29f,.26f,.4f),5,false);Shape("Boot",art,new Vector3(side*.24f,.16f,0),new Vector3(.36f,.35f,1),new Color(.2f,.18f,.3f),5,false);}
            var core=Shape("Crystal core",art,new Vector3(0,.72f,0),new Vector3(.28f,.38f,1),glow,7,false);core.transform.localRotation=Quaternion.Euler(0,0,45);
            Eyes(art,1.3f,.17f,glow,7);art.localScale=Vector3.one*size;
        }
        AttachAnimatedEnemy(e);e.scale=art.localScale;e.baseSpeed=e.speed;
        var bar=Shape("Health track",root.transform,new Vector3(0,kind>=2?kind==3?2.8f:1.85f:1.15f,0),new Vector3(kind==3?1.8f:.8f,.065f,1),new Color(.1f,.06f,.17f),11,false);
        var fill=Shape("Health",bar.transform,new Vector3(0,0,-.01f),Vector3.one,new Color(.82f,.3f,.68f),12,false);e.hp=fill.transform;
        root.AddComponent<BoxCollider2D>().size=new Vector2(1,1.5f);enemies.Add(e);return e;
    }
    void Eyes(Transform parent,float y,float spacing,Color color,int order) {
        for(int s=-1;s<=1;s+=2){Shape("Glowing eye",parent,new Vector3(s*spacing,y,0),new Vector3(.105f,.16f,1),color,order);Shape("Eye glint",parent,new Vector3(s*spacing-.015f,y+.035f,0),new Vector3(.035f,.045f,1),Color.white,order+1);}
    }
    void TickEnemies(float dt) {
        foreach(var e in enemies.ToArray()) {
            if(!e.alive)continue;bool frozen=elapsed<e.frozenUntil;bool attacking=e.root.transform.position.x>=Gate;
            bool distracted=DistractNox(e);
            if(e.animator)e.animator.SetState(distracted?"idle":attacking?"attack":elapsed<e.hitUntil?"hurt":"walk",frozen?new Color(.45f,.85f,1):Color.white);
            if(elapsed<e.burnUntil){e.burnTick-=dt;if(e.burnTick<=0){e.burnTick=.5f;Damage(e,6*damageMultiplier,spellColors[0]);if(!e.alive)continue;}}
            float t=elapsed*5+e.seed;float factor=frozen?.22f:1;
            if(!TickPursuer(e,dt,factor) && !distracted){
                if(!attacking)e.root.transform.position+=Vector3.right*e.speed*factor*dt;
                else {
                    e.attackClock-=dt;
                    if(e.attackClock<=0){
                        e.attackClock=1.2f;
                        if(!BlockNoxAttack(e)){
                            int damage=e.kind==3?75:e.kind>=7?30:e.kind>=4?18:e.kind==2?30:12;
                            Health=Mathf.Max(0,Health-damage*(1-gateGuard));
                            hurtUntil=Time.unscaledTime+.35f;shake=.4f;Play(gateSound,.3f);
                            Floating("CỔNG -"+damage,new Vector3(6.5f,.5f,0),new Color(1,.4f,.4f));
                        }
                    }
                }
            }
            if(!e.alive)continue;
            e.art.localPosition=new Vector3(0,e.kind==1?Mathf.Sin(t*1.4f)*.15f:e.kind==0?Mathf.Abs(Mathf.Sin(t))*.12f:Mathf.Sin(t)*.025f,0);
            e.art.localScale=e.scale*(1+(e.kind==0?.065f:.025f)*Mathf.Sin(t));
            if(!e.animator)e.body.color=frozen?new Color(.2f,.68f,.85f):e.kind>=2?new Color(.24f,.22f,.38f):new Color(.16f,.08f,.29f);
            float healthRatio=Mathf.Clamp01(e.health/e.maxHealth);
            e.hp.localScale=new Vector3(healthRatio,1,1);
            e.hp.localPosition=new Vector3((healthRatio-1)*.5f,0,-.01f);
        }
    }
    void Shoot(Enemy target) {SetWizardAttack();StartCoroutine(ReleaseBasicAttack(target,runGeneration));}
    IEnumerator ReleaseBasicAttack(Enemy target,int generation){yield return new WaitForSeconds(.18f);if(State==Phase.Battle&&generation==runGeneration){if(target==null||!target.alive)target=PickTarget();LaunchShot(target,SelectedHero==Hero.Himmel,20*damageMultiplier,false);}}
    void TickShots(float dt) {TickHeroShots(dt);}
    public bool CastSpell(int kind) {
        if(kind<0||kind>2||State!=Phase.Battle||Paused)return false;var spellCooldown=runeCasting?runeCooldown:cooldown;
        if(Mana<costs[kind]||spellCooldown[kind]>0){tip.text="Phép chưa sẵn sàng — chờ hồi hoặc thêm năng lượng";StartCoroutine(ClearTipAfter(1.5f));return false;}
        var primary=PickTarget();if(primary==null)return false;Mana-=costs[kind];spellCooldown[kind]=kind==2?6:kind==1?5:4;
        Vector3 point=primary.root.transform.position+Vector3.up*.4f;if(!runeCasting)SetWizardAttack();
        if(kind==0){Ring(point,2.2f,spellColors[0]);Burst(point,spellColors[0],32,2.2f);Play(fireSound,.35f);
            foreach(var e in enemies.ToArray())if(Mathf.Abs(e.root.transform.position.x-point.x)<2.2f){e.burnUntil=elapsed+3;Damage(e,65*damageMultiplier,spellColors[0]);}}
        else if(kind==1){Ring(point,3,spellColors[1]);Burst(point,spellColors[1],26,1.8f);Play(iceSound,.3f);
            foreach(var e in enemies.ToArray())if(Mathf.Abs(e.root.transform.position.x-point.x)<3){e.frozenUntil=elapsed+(upgradedIce?6:4);Damage(e,20*damageMultiplier,spellColors[1]);}}
        else {Play(lightningSound,.4f);Haptic();shake=.45f;var targets=enemies.OrderBy(e=>Vector3.Distance(e.root.transform.position,point)).Take(2+extraChains).ToArray();
            foreach(var e in targets){bool frozen=elapsed<e.frozenUntil;Vector3 p=e.root.transform.position+Vector3.up*.5f;Lightning(p);Damage(e,(frozen?180:110)*damageMultiplier,spellColors[2]);Burst(p,spellColors[2],18,1.4f);if(frozen)Floating("PHÁ BĂNG!",p+Vector3.up*.8f,spellColors[1]);}}
        RefreshHud();return true;
    }
    void SetWizardAttack(){castUntil=Time.unscaledTime+.57f;}
    void AnimateWizard() {
        if(!wizard)return;string next=State==Phase.Lost?"defeat":State==Phase.Won?"victory":Time.unscaledTime<hurtUntil?"hurt":Time.unscaledTime<castUntil?"attack":"idle";
        if(next!=wizardClip){wizardClip=next;animationClock=0;}animationClock+=Time.unscaledDeltaTime;
        if(wizardClips.TryGetValue(wizardClip,out var frames)&&frames.Length>0){int frame=(int)(animationClock*(next=="attack"?7:5));if(next=="attack"||next=="defeat"||next=="hurt")frame=Mathf.Min(frame,frames.Length-1);else frame%=frames.Length;wizard.sprite=frames[frame];wizard.sharedMaterial=TextureMaterial(wizard.sprite.texture);}
    }
    void Damage(Enemy e,float damage,Color color) {
        if(!e.alive)return;
        bool guardedMirror=e==mirrorBearer && !mirrorUsed;
        if(guardedMirror)damage*=.55f;
        e.hitUntil=elapsed+.18f;e.health-=damage;
        if(guardedMirror && runeCasting)e.health=Mathf.Max(1,e.health);
        if(Time.unscaledTime-lastHitSound>.09f){Play(hitSound,.18f);lastHitSound=Time.unscaledTime;}
        Floating(Mathf.CeilToInt(damage).ToString(),e.root.transform.position+Vector3.up*(e.kind>=2?1.8f:1),color);
        if(e.health>0 || TryFeignDeath(e))return;
        Vector3 impact=e.root.transform.position+Vector3.up*.6f;
        DefeatEnemy(e);
        if(e==mirrorBearer){
            if(mirrorSpell>=0)ShatterMirror(impact);
            else if(!mirrorUsed)ResolveMirror("Kẻ trộm nét vẽ đã bị hạ.");
        }
    }
    void DefeatEnemy(Enemy e) {
        e.alive=false;Score+=e.kind==3?1000:e.kind==2?100:e.kind==1?60:40;Kills++;
        Mana=Mathf.Min(100,Mana+3);
        Burst(e.root.transform.position+Vector3.up*.5f,new Color(.66f,.3f,1),12,.9f);
        enemies.Remove(e);
        if(e.animator && e.animator.enabled){e.animator.SetState("death",Color.white);Destroy(e.root,.8f);}
        else Destroy(e.root);
    }
    void Burst(Vector3 p,Color color,int count,float speed) {
        count=highEffects?count:Mathf.Max(3,count/3);for(int i=0;i<count;i++){var s=Shape("Spell particle",world,p,new Vector3(.06f,.13f,1),color,20);s.transform.rotation=Quaternion.Euler(0,0,UnityEngine.Random.Range(0,360));
            effects.Add(new Visual{root=s.transform,sprite=s,life=UnityEngine.Random.Range(.25f,.65f),velocity=(Vector3)UnityEngine.Random.insideUnitCircle*speed,scale=s.transform.localScale});}
    }
    void Ring(Vector3 p,float radius,Color color) {
        var go=new GameObject("Spell shockwave",typeof(LineRenderer));go.layer=29;go.transform.SetParent(world);go.transform.position=p;
        var l=go.GetComponent<LineRenderer>();l.sharedMaterial=spriteMaterial;l.useWorldSpace=false;l.loop=true;l.positionCount=48;l.startWidth=l.endWidth=.025f;l.startColor=l.endColor=color;l.sortingOrder=15;
        for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48;l.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius*.28f,0));}
        Destroy(go,.35f);
    }
    void Lightning(Vector3 end) {
        var go=new GameObject("Lightning strike",typeof(LineRenderer));go.layer=29;go.transform.SetParent(world);var l=go.GetComponent<LineRenderer>();l.sharedMaterial=spriteMaterial;l.positionCount=12;l.startWidth=.075f;l.endWidth=.025f;l.startColor=l.endColor=spellColors[2];l.sortingOrder=25;
        for(int i=0;i<12;i++){float t=i/11f;l.SetPosition(i,Vector3.Lerp(new Vector3(end.x+1,4.7f,0),end,t)+Vector3.right*(i==11?0:UnityEngine.Random.Range(-.3f,.3f)));}Destroy(go,.2f);
    }
    void Floating(string value,Vector3 p,Color c) {
        var go=new GameObject("Combat text",typeof(TextMesh));go.layer=29;go.transform.SetParent(world);go.transform.position=p;
        var t=go.GetComponent<TextMesh>();t.text=value;t.font=font;t.fontSize=48;t.characterSize=.065f;t.anchor=TextAnchor.MiddleCenter;t.color=c;
        var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=font.material;r.sortingOrder=30;
        effects.Add(new Visual{root=go.transform,text=t,life=.85f,velocity=new Vector3(0,.7f,0),scale=Vector3.one});
    }
    void TickEffects(float dt) {
        for(int i=effects.Count-1;i>=0;i--){var e=effects[i];if(!e.root){effects.RemoveAt(i);continue;}e.time+=dt;
            if(e.time>=e.life){Destroy(e.root.gameObject);effects.RemoveAt(i);continue;}
            e.root.position+=e.velocity*dt;float alpha=1-e.time/e.life;
            if(e.life<100){if(e.sprite){var c=e.sprite.color;c.a=alpha;e.sprite.color=c;e.root.localScale=e.scale*(.5f+alpha*.5f);}if(e.text){var c=e.text.color;c.a=alpha;e.text.color=c;}}
        }
    }


    public void SelectUpgrade(int id) {
        if(State!=Phase.Upgrade)return;
        switch(id){case 0:damageMultiplier*=1.2f;break;case 1:attackInterval=Mathf.Max(.25f,attackInterval*.85f);break;case 2:manaRegen+=2;break;case 3:Health=Mathf.Min(1000,Health+250);break;case 4:extraChains++;if(SelectedHero==Hero.Himmel)damageMultiplier+=.1f;break;case 5:upgradedIce=true;if(SelectedHero==Hero.Himmel)attackInterval=Mathf.Max(.25f,attackInterval*.9f);break;}
        Mana=Mathf.Min(100,Mana+25);if(overlay)Destroy(overlay);BeginWave();RefreshHud();
    }




    AudioClip Tone(string name,float frequency,float length,float noise) {
        const int rate=22050;var values=new float[(int)(rate*length)];var rng=new System.Random(42);
        for(int i=0;i<values.Length;i++){float t=i/(float)rate;float env=Mathf.Pow(1-i/(float)values.Length,2);values[i]=env*(Mathf.Sin(2*Mathf.PI*frequency*t*(1-t))* (1-noise)+(float)(rng.NextDouble()*2-1)*noise)*.35f;}
        var c=AudioClip.Create(name,values.Length,1,rate,false);c.SetData(values,0);return c;
    }

    void OnDestroy() {Time.timeScale=1;if(classifier!=null)classifier.Dispose();if(arMode)ExitAr();if(spriteMaterial)Destroy(spriteMaterial);foreach(var material in textureMaterials.Values)if(material)Destroy(material);}

    // Used by the editor smoke check, exercising real combat paths rather than mock results.
    public void VerificationSpawn() {Spawn(0);Spawn(4);Spawn(7);foreach(var e in enemies)e.root.transform.position+=Vector3.right*5;}
#if UNITY_EDITOR
    public void VerificationResolveWave(){if(rescue==Rescue.Choice)ChooseRescue(false);while(toSpawn>0){SpawnWaveEnemy();waveSpawned++;toSpawn--;}foreach(var e in enemies.ToArray())Damage(e,100000,Color.white);if(dormantEnemies.Count>0&&!bellUsed)RingTheBell();foreach(var s in shots)if(s.root)Destroy(s.root.gameObject);shots.Clear();}
    public void VerificationLose(){Health=0;}
    public void VerificationHome(){ShowMenu();}
#endif
}
}
