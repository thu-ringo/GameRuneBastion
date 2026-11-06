using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RuneBastion {
public sealed partial class RuneBastionGame {
    public enum Hero { Himmel, Frieren }
    public Hero SelectedHero {get;private set;}=Hero.Himmel;
    public string PlayerName {get;private set;}="Người giữ thành";
    bool selectingDaily,selectingAr,runeCasting,endingActive;
    InputField playerNameInput;Text selectionError;Image[] heroCards=new Image[2];
    readonly float[] runeCooldown=new float[3];
    int runBest,runEarned,runGeneration;string runLoot="";float hurtUntil;Text heroHud;
    AudioClip weddingMusic;GameObject equipmentAura;Hero? lastHudHero;
    string HeroName=>SelectedHero.ToString();
    string[] SkillNames=>SelectedHero==Hero.Himmel?new[]{"LIÊN TRẢM","NGUYỆT TRẢM","THÁNH KIẾM"}:new[]{"LỬA","BĂNG","SÉT"};
    void LoadHeroProfile(){
        SelectedHero=(Hero)Mathf.Clamp(PlayerPrefs.GetInt("RB.Hero",0),0,1);
        PlayerName=CleanPlayerName(PlayerPrefs.GetString("RB.PlayerName","Người giữ thành"));LoadEquipment();
    }
    static string CleanPlayerName(string value){
        var s=new string((value??"").Where(c=>!char.IsControl(c)&&c!='<'&&c!='>').ToArray()).Trim();
        return s.Length>20?s.Substring(0,20):s;
    }
    void RefreshHeroArt(){
        foreach(string c in new[]{"idle","move","attack","hurt","defeat","victory"})wizardClips[c]=HeroArt.Frames(HeroName,c);
        wizardClip="idle";animationClock=0;castUntil=0;
        if(wizard){wizard.sprite=wizardClips["idle"].FirstOrDefault();wizard.flipX=false;wizard.name=HeroName+" - Player";wizard.sharedMaterial=TextureMaterial(wizard.sprite.texture);}
    }
    void PlaceHero(bool menu){
        if(!wizard)return;wizard.gameObject.SetActive(true);
        wizard.transform.position=menu?new Vector3(4.1f,Ground,0):new Vector3(7,Ground,0);
        wizard.transform.localScale=Vector3.one*(menu?1.65f:1.12f);RefreshEquipmentAura();
    }
    Vector3 AttackOrigin=>wizard?wizard.transform.position+new Vector3(-.65f,1,0):new Vector3(6.3f,1.3f,0);
    Image HeroImage(Transform parent,string hero,float x,float y,float width,float height,string clip="idle",bool right=false){
        var image=Panel(hero+"Portrait",parent,x,y,width,height,Color.white);image.raycastTarget=false;
        image.rectTransform.pivot=new Vector2(.5f,.5f);image.rectTransform.anchoredPosition=new Vector2(x+width/2,-y-height/2);
        image.sprite=HeroArt.Frames(hero,clip).FirstOrDefault();image.preserveAspect=true;
        var animator=image.gameObject.AddComponent<HeroPortrait>();animator.hero=hero;animator.clip=clip;animator.faceRight=right;return image;
    }
    void ShowCharacterSelection(bool daily=false,bool ar=false){
        selectingDaily=daily;selectingAr=ar;State=Phase.Menu;Paused=false;Time.timeScale=1;ClearBattle();hudRoot.gameObject.SetActive(false);if(wizard)wizard.gameObject.SetActive(false);
        var p=Overlay("CHỌN NGƯỜI GIỮ THÀNH",daily?"Thử thách ngày · Không áp dụng trang bị hoặc nâng cấp vĩnh viễn.":"Một người chiến đấu. Một lời hẹn sau bình minh.");
        for(int i=0;i<2;i++){
            int id=i;float x=65+i*585;var card=RoundPanel("HeroCard"+i,p.transform,x,172,560,270,new Color(.1f,.17f,.29f));heroCards[i]=card;
            HeroImage(card.transform,i==0?"Himmel":"Frieren",-25,15,310,230);
            Heading(card.transform,i==0?"HIMMEL":"FRIEREN",255,18,275,42,28,new Color(.9f,.94f,1));
            Label("HeroRole",card.transform,i==0?"DŨNG SĨ KIẾM KHÍ":"PHÁP SƯ NGUYÊN TỐ",270,68,270,26,15,new Color(.5f,.87f,1));
            Label("HeroDescription",card.transform,i==0?"Kiếm khí tầm xa\nChém liên tiếp · Xuyên quái\nĐòn thánh kiếm diện rộng":"Đạn phép tầm xa\nLửa · Băng · Sét\nPhối hợp nguyên tố",270,105,265,110,19,new Color(.76f,.84f,.95f));
            Button("ChooseHero"+i,card.transform,"CHỌN",310,215,190,40,new Color(.17f,.32f,.48f),()=>SelectHero((Hero)id),17);
        }
        Label("NameLabel",p.transform,"TÊN NGƯỜI CHƠI",65,463,225,34,17,new Color(.7f,.8f,.94f));
        var bg=RoundPanel("PlayerNameInput",p.transform,290,456,610,52,new Color(.08f,.14f,.24f));playerNameInput=bg.gameObject.AddComponent<InputField>();
        var text=Label("NameText",bg.transform,"",16,4,578,44,22,Color.white);text.supportRichText=false;playerNameInput.textComponent=text;playerNameInput.characterLimit=20;playerNameInput.text=PlayerName;
        selectionError=Label("SelectionError",p.transform,"",65,508,830,30,16,new Color(1,.7f,.52f));
        Button("BackSelection",p.transform,"QUAY LẠI",65,550,245,45,new Color(.1f,.17f,.28f),ShowMenu,18);
        Button("ConfirmCharacter",p.transform,"BẮT ĐẦU PHÒNG THỦ",810,540,385,60,new Color(.19f,.35f,.48f),ConfirmCharacter,22);
        PaintHeroCards();
    }
    void SelectHero(Hero hero){SelectedHero=hero;RefreshHeroArt();PaintHeroCards();}
    void PaintHeroCards(){for(int i=0;i<2;i++)if(heroCards[i])heroCards[i].color=i==(int)SelectedHero?new Color(.17f,.3f,.46f):new Color(.065f,.11f,.21f);}
    void ConfirmCharacter(){
        string name=CleanPlayerName(playerNameInput.text);if(name.Length==0){selectionError.text="Hãy nhập tên từ 1 đến 20 ký tự.";return;}
        PlayerName=name;if(!verificationMode){PlayerPrefs.SetString("RB.PlayerName",PlayerName);PlayerPrefs.SetInt("RB.Hero",(int)SelectedHero);PlayerPrefs.Save();}
        if(selectingAr){BeginAr();return;}if(selectingDaily){Overlay("ĐANG CHUẨN BỊ THỬ THÁCH","Đang lấy mã ngày. Nếu không có mạng, game sẽ dùng mã offline.");StartDaily();return;}dailyMode=false;StartRun();
    }
    void PrepareHeroRun(){
        runGeneration++;endingActive=false;StopEndingMusic();RefreshHeroArt();PlaceHero(false);Array.Clear(runeCooldown,0,3);hurtUntil=0;
        if(!dailyMode)ApplyEquipment();
        attackInterval*=SelectedHero==Hero.Himmel?.92f:1;RefreshHeroHud();
    }
    void RefreshHeroHud(){
        if(heroHud){heroHud.supportRichText=false;heroHud.text=PlayerName+" · "+HeroName;}
        if(lastHudHero==SelectedHero)return;lastHudHero=SelectedHero;
        for(int i=0;i<3;i++)if(skillLabels[i]){skillLabels[i].text=SkillNames[i];skillLabels[i].fontSize=SelectedHero==Hero.Himmel?14:18;
            var art=skillRects[i].Find("Art").GetComponent<Image>();art.sprite=SelectedHero==Hero.Himmel?Resources.Load<Sprite>("Equipment/"+(i==0?"sword":"upg_sword")):abilityIcons[i];art.color=SelectedHero==Hero.Himmel&&i==2?new Color(1,.85f,.5f):Color.white;art.preserveAspect=true;}
        var basic=hudRoot.Find("Staff/Art").GetComponent<Image>();basic.sprite=SelectedHero==Hero.Himmel?Resources.Load<Sprite>("Equipment/sword"):abilityIcons[3];basic.preserveAspect=true;
    }
    public bool CastHeroSkill(int kind){
        if(SelectedHero==Hero.Frieren)return CastSpell(kind);
        if(kind<0||kind>2||State!=Phase.Battle||Paused||endingActive)return false;
        if(Mana<costs[kind]||cooldown[kind]>0){ShowTip("Kỹ năng chưa sẵn sàng — chờ hồi hoặc thêm năng lượng");return false;}
        var target=PickTarget();if(target==null)return false;Mana-=costs[kind];cooldown[kind]=kind==2?6:kind==1?5:4;SetWizardAttack();
        StartCoroutine(SwordSkill(kind,target.root.transform.position+Vector3.up*.6f,runGeneration));RefreshHud();return true;
    }
    IEnumerator SwordSkill(int kind,Vector3 point,int generation){
        yield return new WaitForSeconds(.18f);if(State!=Phase.Battle||generation!=runGeneration)yield break;
        if(kind==0){for(int i=0;i<3;i++){if(State!=Phase.Battle||generation!=runGeneration)yield break;var target=PickTarget();if(target==null)break;LaunchShot(target,true,27*damageMultiplier,false);Play(iceSound,.12f);yield return new WaitForSeconds(.12f);}}
        else if(kind==1){LaunchShot(PickTarget(),true,80*damageMultiplier,true);Play(iceSound,.3f);}
        else {shake=.5f;Play(lightningSound,.35f);SwordArc(point,3.4f,new Color(.75f,.9f,1),.35f);
            foreach(var e in enemies.ToArray())if(Mathf.Abs(e.root.transform.position.x-point.x)<3.5f){Burst(e.root.transform.position+Vector3.up*.7f,new Color(.5f,.85f,1),12,1.4f);Damage(e,155*damageMultiplier,new Color(.7f,.9f,1));}}
    }
    void LaunchShot(Enemy target,bool blade,float damage,bool piercing){
        if(target==null||!target.alive)return;Vector3 from=AttackOrigin;Transform root;
        if(blade){root=SwordArc(from,piercing?.65f:.36f,new Color(.55f,.9f,1),3);}
        else root=Shape("Frieren magic bolt",world,from,new Vector3(.2f,.2f,1),new Color(.65f,.9f,1),15).transform;
        shots.Add(new Shot{root=root,target=target,from=from,damage=damage,blade=blade,piercing=piercing,t=0});
    }
    Transform SwordArc(Vector3 position,float size,Color color,float lifetime){
        var g=new GameObject("Himmel crescent slash",typeof(LineRenderer));g.layer=29;g.transform.SetParent(world);g.transform.position=position;
        var l=g.GetComponent<LineRenderer>();l.useWorldSpace=false;l.sharedMaterial=spriteMaterial;l.positionCount=18;l.startWidth=.035f;l.endWidth=.035f;l.widthMultiplier=1;l.sortingOrder=20;l.startColor=l.endColor=color;
        l.widthCurve=new AnimationCurve(new Keyframe(0,.03f),new Keyframe(.5f,.2f),new Keyframe(1,.03f));
        for(int i=0;i<18;i++){float a=Mathf.Lerp(-1.2f,1.2f,i/17f);l.SetPosition(i,new Vector3(-Mathf.Cos(a)*size,Mathf.Sin(a)*size*1.6f,0));}
        Destroy(g,lifetime);return g.transform;
    }
    void TickHeroShots(float dt){
        for(int i=shots.Count-1;i>=0;i--){var s=shots[i];if(!s.root){shots.RemoveAt(i);continue;}
            if(s.piercing){s.root.position+=Vector3.left*10*dt;foreach(var e in enemies.ToArray())if(!s.hit.Contains(e)&&Mathf.Abs(e.root.transform.position.x-s.root.position.x)<.65f){s.hit.Add(e);Damage(e,s.damage,new Color(.6f,.9f,1));}if(s.root.position.x< -10){Destroy(s.root.gameObject);shots.RemoveAt(i);}continue;}
            if(s.target==null||!s.target.alive){s.target=PickTarget();if(s.target==null){Destroy(s.root.gameObject);shots.RemoveAt(i);continue;}}
            s.t+=dt*2.1f;Vector3 end=s.target.root.transform.position+Vector3.up*.65f;
            s.root.position=Vector3.Lerp(s.from,end,Mathf.Min(1,s.t))+(s.blade?Vector3.zero:Vector3.up*Mathf.Sin(s.t*Mathf.PI)*.35f);
            if(s.t>=1){Damage(s.target,s.damage,s.blade?new Color(.55f,.88f,1):new Color(.7f,.6f,1));Burst(end,new Color(.6f,.8f,1),5,.65f);Destroy(s.root.gameObject);shots.RemoveAt(i);}
        }
    }
    void RefreshEquipmentAura(){
        if(equipmentAura)Destroy(equipmentAura);if(!wizard||State!=Phase.Battle||dailyMode)return;
        int id=EquipmentFor(SelectedHero,3);if(id<0)return;Color color=gear[id].icon=="gemRed"?new Color(1,.3f,.2f,.35f):gear[id].icon=="gemGreen"?new Color(.3f,1,.55f,.35f):new Color(.25f,.8f,1,.3f);var r=Shape("Equipped rune aura",wizard.transform,new Vector3(0,.48f,0),new Vector3(1.05f,.12f,1),color,9);equipmentAura=r.gameObject;
    }
    void SaveRunResult(bool won){
        State=won?Phase.Won:Phase.Lost;runBest=Mathf.Max(Score,PlayerPrefs.GetInt("RuneBastion.Best",0));runEarned=Score/20+(won?100:0);runLoot="";
        if(!verificationMode){PlayerPrefs.SetInt("RuneBastion.Best",runBest);coins+=runEarned;PlayerPrefs.SetInt("RB.Coins",coins);if(won)runLoot=AwardEquipment();PlayerPrefs.Save();SubmitScore(won);}
    }
    void ShowVictory(){
        Play(runeSound,.65f);
        ShowRunResults(true);
    }
    void ShowRunResults(){ShowRunResults(false);}
    void ShowRunResults(bool beforeWedding){
        endingActive=false;StopEndingMusic();if(wizard){wizard.gameObject.SetActive(true);PlaceHero(false);}bool won=State==Phase.Won;
        var p=Overlay(won?"CHIẾN THẮNG":"GAME OVER — THÀNH THẤT THỦ",won?"Bạn đã bảo vệ thành công thành trì!":"Cổng thành đã vỡ. Hãy thử bộ trang bị và cách phối hợp khác.");
        var name=Heading(p.transform,PlayerName+" · "+HeroName,200,200,870,40,25,new Color(.7f,.86f,1));name.supportRichText=false;
        Heading(p.transform,Score.ToString("N0")+" ĐIỂM",250,250,770,65,42,new Color(1,.83f,.4f));
        Label("Stats",p.transform,(won ? Wave+" / "+TotalWaves+" đợt đã vượt qua   •   Máu cổng: "+Mathf.CeilToInt(Health)+" / 1000" : "Đợt "+Wave+" / "+TotalWaves)+"\n"+Kills+" quái   •   +"+runEarned+" tinh thạch   •   Kỷ lục: "+runBest+(runLoot.Length>0?"\n"+runLoot:""),150,327,970,100,21,new Color(.77f,.86f,.96f),TextAnchor.MiddleCenter);
        if(won && RescueSucceeded)
            Label("RescueResult",p.transform,"Golem và đứa trẻ đã an toàn.",175,425,924,29,20,RescueGold,TextAnchor.MiddleCenter);
        if(beforeWedding){
            Button("ContinueToWedding",p.transform,"ĐẾN LỄ CƯỚI",412,460,450,65,new Color(.38f,.24f,.12f),ShowWedding,24);
            return;
        }
        Button("Retry",p.transform,"CHƠI LẠI",160,460,450,65,new Color(.15f,.28f,.43f),StartRun,24);
        Button("Home",p.transform,"VỀ MENU",670,460,450,65,new Color(.1f,.16f,.28f),ShowMenu,24);
    }
    void ShowWedding(){
        endingActive=true;ClearBattle();hudRoot.gameObject.SetActive(false);if(wizard)wizard.gameObject.SetActive(false);if(overlay)Destroy(overlay);
        var bg=Panel("WeddingEnding",uiRoot,0,0,1600,900,Color.white);overlay=bg.gameObject;bg.sprite=Resources.Load<Sprite>("WeddingGarden");
        Panel("WeddingTitleShade",bg.transform,0,0,1600,205,new Color(.05f,.025f,.12f,.55f));
        Heading(bg.transform,"LỜI HẸN DƯỚI BÌNH MINH",150,30,1300,70,46,new Color(1,.92f,.69f));
        var text=Label("WeddingThanks",bg.transform,PlayerName+", bạn đã giữ được thành — và một lời hẹn.",200,110,1200,45,24,Color.white,TextAnchor.MiddleCenter);text.supportRichText=false;
        HeroImage(bg.transform,"Himmel",440,290,520,350,"idle",true);HeroImage(bg.transform,"Frieren",650,290,520,350,"idle");
        Heading(bg.transform,"Himmel  &  Frieren",400,655,800,55,39,new Color(.25f,.12f,.28f));
        Label("WeddingLine",bg.transform,"Sau trận chiến cuối cùng, họ chọn cùng nhau đón những ngày bình yên.",200,720,1200,36,21,new Color(.25f,.12f,.28f),TextAnchor.MiddleCenter);
        if(RescueSucceeded) AddWeddingGuests(bg.transform);
        Button("WeddingResults",bg.transform,"XEM LẠI KẾT QUẢ",390,795,420,60,new Color(.24f,.15f,.31f,.95f),ShowRunResults,20);
        Button("WeddingHome",bg.transform,"VỀ MENU",850,795,350,60,new Color(.18f,.25f,.38f,.95f),ShowMenu,21);
        if(!weddingMusic)weddingMusic=Resources.Load<AudioClip>("Audio/WeddingPromise");
        StartCoroutine(FadeMusic(weddingMusic));StartCoroutine(WeddingParticles(bg.transform));
    }
    IEnumerator WeddingParticles(Transform parent){
        while(endingActive&&parent){int count=highEffects?2:1;for(int i=0;i<count;i++){
            var petal=Panel("WeddingPetal",parent,UnityEngine.Random.Range(80,1520),-20,8,13,new Color(1,UnityEngine.Random.Range(.65f,.9f),.82f,.85f));petal.raycastTarget=false;petal.sprite=disk;petal.transform.SetSiblingIndex(2);
            var fx=petal.gameObject.AddComponent<WeddingPetal>();fx.velocity=new Vector2(UnityEngine.Random.Range(-30,30),-UnityEngine.Random.Range(40,75));
        }yield return new WaitForSecondsRealtime(.4f);}
    }
    IEnumerator FadeMusic(AudioClip clip){if(!musicSource||!clip)yield break;var source=musicSource;float v=source.volume;for(float t=0;t<.45f;t+=Time.unscaledDeltaTime){if(!endingActive)yield break;source.volume=Mathf.Lerp(v,0,t/.45f);yield return null;}
        if(!endingActive)yield break;source.clip=clip;source.loop=true;source.Play();for(float t=0;t<.65f;t+=Time.unscaledDeltaTime){if(!endingActive)yield break;source.volume=musicVolume*t/.65f;yield return null;}source.volume=musicVolume;}
    void StopEndingMusic(){endingActive=false;if(!musicSource)return;var theme=Resources.Load<AudioClip>("Audio/TwilightTheme");if(musicSource.clip!=theme){musicSource.clip=theme;musicSource.loop=true;if(theme)musicSource.Play();}musicSource.volume=musicVolume;}
    IEnumerator DefeatEnding(){
        endingActive=true;var p=Panel("CastleFalls",uiRoot,0,0,1600,900,new Color(.12f,.015f,.045f,.7f));overlay=p.gameObject;
        Heading(p.transform,"CỔNG THÀNH ĐÃ VỠ",200,315,1200,90,53,new Color(1,.67f,.65f));
        Label("DefeatLine",p.transform,"Bóng tối tràn qua cổng. Nhưng câu chuyện vẫn có thể bắt đầu lại.",200,420,1200,60,24,Color.white,TextAnchor.MiddleCenter);
        Burst(new Vector3(6.3f,Ground+.5f,0),new Color(1,.3f,.15f),40,3);Play(gateSound,.8f);
        for(float t=0;t<2.2f;t+=Time.unscaledDeltaTime){if(!endingActive||State!=Phase.Lost)yield break;foreach(var e in enemies)if(e.root)e.root.transform.position+=Vector3.right*Time.unscaledDeltaTime*.8f;yield return null;}if(State==Phase.Lost&&endingActive)ShowRunResults();
    }
}
public sealed class WeddingPetal:MonoBehaviour {
    public Vector2 velocity;float life;
    void Update(){life+=Time.unscaledDeltaTime;var r=(RectTransform)transform;r.anchoredPosition+=velocity*Time.unscaledDeltaTime;transform.Rotate(0,0,55*Time.unscaledDeltaTime);if(life>18)Destroy(gameObject);}
}
}
