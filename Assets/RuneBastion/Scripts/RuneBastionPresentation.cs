using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RuneBastion {
public sealed partial class RuneBastionGame {
    Font titleFont;
    Sprite rounded;
    Sprite[] abilityIcons;
    Transform hudRoot;
    Image[] cooldownImages=new Image[3];
    Text[] cooldownNumbers=new Text[3];
    RectTransform[] skillRects=new RectTransform[3];
    Text coinText;
    AudioSource musicSource;
    AudioClip clickSound, coinSound, runeSound, gateSound;
    bool highEffects=true, vibration=true;
    float musicVolume=.45f,sfxVolume=.8f;
    bool dailyMode;
    int dailySeed;
    int coins;
    Text waveCaption;
    bool verificationMode;
    Image[] waveDots=new Image[TotalWaves];
    Sprite RoundedSprite() {
        var tex=new Texture2D(64,64,TextureFormat.RGBA32,false);tex.name="UI rounded panel";
        for(int y=0;y<64;y++)for(int x=0;x<64;x++){
            float dx=Mathf.Max(12-x,Mathf.Max(0,x-51)),dy=Mathf.Max(12-y,Mathf.Max(0,y-51));
            tex.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01((12-Mathf.Sqrt(dx*dx+dy*dy))*2)));}
        tex.Apply();return Sprite.Create(tex,new Rect(0,0,64,64),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(16,16,16,16));
    }
    void LoadUiResources(){
        font=Resources.Load<Font>("Fonts/UiBody")??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleFont=Resources.Load<Font>("Fonts/UiTitle")??font;
        rounded=RoundedSprite();var atlas=Resources.Load<Texture2D>("UI/AbilityAtlas");abilityIcons=new Sprite[6];
        if(atlas)for(int i=0;i<6;i++)abilityIcons[i]=Sprite.Create(atlas,new Rect((i%3)*atlas.width/3f,(1-i/3)*atlas.height/2f,atlas.width/3f,atlas.height/2f),new Vector2(.5f,.5f),100);
        musicVolume=PlayerPrefs.GetFloat("RB.Music",.45f);sfxVolume=PlayerPrefs.GetFloat("RB.Sfx",.8f);
        highEffects=PlayerPrefs.GetInt("RB.Effects",1)==1;vibration=PlayerPrefs.GetInt("RB.Vibration",1)==1;
        coins=PlayerPrefs.GetInt("RB.Coins",0);
    }
    Text Heading(Transform p,string value,float x,float y,float w,float h,int size,Color color){var t=Label("Heading",p,value,x,y,w,h,size,color,TextAnchor.MiddleCenter);t.font=titleFont;return t;}
    Image RoundPanel(string name,Transform p,float x,float y,float w,float h,Color c){var im=Panel(name,p,x,y,w,h,c);im.sprite=rounded;im.type=Image.Type.Sliced;return im;}
    Button Button(string name,Transform p,string value,float x,float y,float w,float h,Color c,Action action,int textSize=22) {
        var edge=RoundPanel(name,p,x,y,w,h,new Color(.46f,.56f,.77f,.8f));
        var fill=RoundPanel("Fill",edge.transform,2,2,w-4,h-4,c);fill.raycastTarget=false;
        var b=edge.gameObject.AddComponent<Button>();b.targetGraphic=fill;
        var colors=b.colors;colors.highlightedColor=new Color(1.2f,1.2f,1.2f,1);colors.pressedColor=new Color(.65f,.75f,.9f,1);b.colors=colors;
        Label("Caption",edge.transform,value,12,3,w-24,h-6,textSize,Color.white,TextAnchor.MiddleCenter);
        b.onClick.AddListener(()=>{PlayUi();action();});return b;
    }
    Button CircleButton(string name,Transform p,int icon,float x,float y,float size,Action action){
        var node=Node(name,p,x,y,size,size);var im=node.gameObject.AddComponent<Image>();im.sprite=disk;im.color=new Color(.08f,.12f,.24f);
        var mask=node.gameObject.AddComponent<Mask>();mask.showMaskGraphic=true;
        var graphic=Panel("Art",node,0,0,size,size,Color.white);graphic.sprite=abilityIcons[icon];graphic.raycastTarget=false;
        var b=node.gameObject.AddComponent<Button>();b.targetGraphic=graphic;
        var colors=b.colors;colors.highlightedColor=new Color(1.15f,1.15f,1.15f);colors.pressedColor=new Color(.7f,.7f,.85f);b.colors=colors;
        b.onClick.AddListener(()=>{PlayUi();action();});return b;
    }
    void CreateUi() {
        LoadUiResources();var go=new GameObject("Rune Bastion HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        uiRoot=Node("Safe content",go.transform,0,0,1600,900);uiRoot.anchorMin=uiRoot.anchorMax=uiRoot.pivot=new Vector2(.5f,.5f);uiRoot.anchoredPosition=Vector2.zero;
        hudRoot=Node("Combat HUD",uiRoot,0,0,1600,900);
        Panel("Top shade",hudRoot,0,0,1600,118,new Color(.025f,.035f,.1f,.72f)).raycastTarget=false;
        var brand=Heading(hudRoot,"Rune Bastion",28,5,450,55,35,new Color(.8f,.94f,1));brand.alignment=TextAnchor.MiddleLeft;heroHud=Label("HeroHud",hudRoot,"",35,95,520,22,14,new Color(.65f,.84f,.95f));
        RoundPanel("HealthBorder",hudRoot,34,67,373,25,new Color(.56f,.4f,.62f));
        RoundPanel("HealthTrack",hudRoot,37,70,367,19,new Color(.16f,.055f,.14f));
        hpFill=RoundPanel("Health",hudRoot,39,72,363,15,new Color(1,.24f,.36f));
        hpText=Label("HealthValue",hudRoot,"1000 / 1000",55,65,335,27,18,Color.white,TextAnchor.MiddleCenter);
        waveText=Heading(hudRoot,"ĐỢT 1 / "+TotalWaves,600,8,400,47,29,Color.white);
        waveCaption=Label("WaveCaption",hudRoot,"BẢO VỆ CỔNG SAO",600,92,400,20,12,new Color(.64f,.78f,.9f),TextAnchor.MiddleCenter);
        Panel("WaveLine",hudRoot,650,76,300,2,new Color(.38f,.42f,.61f)).raycastTarget=false;
        progress=Panel("WaveProgress",hudRoot,650,76,0,2,new Color(.2f,.85f,1));progress.raycastTarget=false;
        for(int i=0;i<TotalWaves;i++){var dot=Panel("WaveDot"+i,hudRoot,642+i*(300f/(TotalWaves-1)),68,17,17,new Color(.33f,.37f,.5f));dot.sprite=disk;dot.raycastTarget=false;waveDots[i]=dot;}
        RoundPanel("ScorePill",hudRoot,1240,25,230,52,new Color(.04f,.06f,.15f,.8f));
        scoreText=Label("Score",hudRoot,"0",1275,27,175,46,27,new Color(1,.84f,.43f),TextAnchor.MiddleRight);
        var scoreIcon=Panel("ScoreIcon",hudRoot,1256,32,32,32,Color.white);scoreIcon.sprite=Resources.Load<Sprite>("Equipment/gemBlue");scoreIcon.preserveAspect=true;scoreIcon.raycastTarget=false;
        CircleButton("Pause",hudRoot,5,1505,19,72,TogglePause);
        Panel("BottomShade",hudRoot,0,720,1600,180,new Color(.025f,.035f,.1f,.86f)).raycastTarget=false;
        Panel("BottomEdge",hudRoot,35,720,1530,1,new Color(.4f,.5f,.7f,.5f)).raycastTarget=false;
        RoundPanel("ManaBorder",hudRoot,560,720,530,24,new Color(.29f,.44f,.65f));
        RoundPanel("ManaTrack",hudRoot,563,723,524,18,new Color(.04f,.13f,.25f));
        manaFill=RoundPanel("Mana",hudRoot,565,725,520,14,new Color(.1f,.7f,1));
        manaText=Label("ManaValue",hudRoot,"100 / 100",565,718,520,26,17,Color.white,TextAnchor.MiddleCenter);
        CircleButton("Staff",hudRoot,3,88,749,116,FocusAttack);
        Heading(hudRoot,"TẤN CÔNG",58,865,176,25,17,new Color(.85f,.9f,1));
        string[] names={"LỬA","BĂNG","SÉT"};
        for(int i=0;i<3;i++){int index=i;float x=600+i*175;
            var b=CircleButton("Spell"+i,hudRoot,i,x,752,110,()=>CastHeroSkill(index));skillRects[i]=b.GetComponent<RectTransform>();
            var shade=Panel("Cooldown",b.transform,0,0,110,110,new Color(.02f,.03f,.1f,.75f));shade.sprite=disk;shade.type=Image.Type.Filled;shade.fillMethod=Image.FillMethod.Radial360;shade.fillOrigin=2;shade.fillClockwise=false;shade.fillAmount=0;shade.raycastTarget=false;cooldownImages[i]=shade;
            cooldownNumbers[i]=Label("CooldownValue",b.transform,"",0,30,110,50,30,Color.white,TextAnchor.MiddleCenter);
            skillLabels[i]=Heading(hudRoot,names[i],x-12,865,134,24,18,spellColors[i]);
            Label("Cost",hudRoot,costs[i]+"  NL",x+111,788,55,25,14,new Color(.62f,.82f,.96f),TextAnchor.MiddleLeft);
        }
        CircleButton("Rune",hudRoot,4,1360,744,122,OpenRunePad);
        Heading(hudRoot,"VẼ CỔ NGỮ",1320,869,200,25,17,new Color(.65f,.92f,1));
        tip=Label("Announcement",hudRoot,"",300,125,1000,58,25,new Color(.9f,.95f,1),TextAnchor.MiddleCenter);
        status=Label("Status",hudRoot,"",245,870,310,24,13,new Color(.6f,.73f,.87f));
        if(!FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>())new GameObject("EventSystem",typeof(UnityEngine.EventSystems.EventSystem),typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
    }
    void FocusAttack(){if(State==Phase.Battle&&!Paused){targetX=Gate;autoClock=0;ShowTip("Ưu tiên quái gần cổng thành");}}
    void ShowTip(string message){tip.text=message;StartCoroutine(ClearTipAfter(2.5f));if(State==Phase.Menu){var toast=RoundPanel("MenuToast",uiRoot,350,825,900,55,new Color(.08f,.12f,.23f,.98f));Label("Toast",toast.transform,message,15,3,870,48,19,new Color(1,.84f,.55f),TextAnchor.MiddleCenter);Destroy(toast.gameObject,2.7f);}}
    GameObject Overlay(string title,string subtitle){
        if(overlay)Destroy(overlay);var shade=Panel("ModalShade",uiRoot,0,0,1600,900,new Color(.01f,.02f,.07f,.82f));overlay=shade.gameObject;
        RoundPanel("ModalBorder",shade.transform,160,140,1280,620,new Color(.35f,.45f,.65f));
        var p=RoundPanel("ModalCard",shade.transform,163,143,1274,614,new Color(.045f,.07f,.15f,.98f));
        Heading(p.transform,title,60,26,1150,75,38,new Color(.86f,.94f,1));
        Label("Description",p.transform,subtitle,110,108,1050,90,22,new Color(.65f,.76f,.9f),TextAnchor.MiddleCenter);
        return p.gameObject;
    }
    void ShowMenu(){
        State=Phase.Menu;Paused=false;Time.timeScale=1;ClearBattle();StopEndingMusic();if(arMode)ExitAr();RefreshHeroArt();
        hudRoot.gameObject.SetActive(false);if(overlay)Destroy(overlay);
        PlaceHero(true);
        var p=Panel("MainMenu",uiRoot,0,0,1600,900,new Color(.015f,.025f,.07f,.4f));overlay=p.gameObject;
        RoundPanel("MenuGlass",p.transform,85,90,680,720,new Color(.025f,.04f,.1f,.82f));
        var title=Heading(p.transform,"Rune\nBastion",125,118,600,180,70,new Color(.8f,.94f,1));title.lineSpacing=.85f;
        Label("Subtitle",p.transform,"KIẾM & PHÉP — LỜI HẸN BÌNH MINH",150,305,550,40,23,new Color(.98f,.78f,.42f),TextAnchor.MiddleCenter);
        Button("Play",p.transform,"BẮT ĐẦU CHƠI",190,380,470,70,new Color(.25f,.3f,.5f),()=>ShowCharacterSelection(),27);
        Button("Daily",p.transform,"THỬ THÁCH NGÀY",190,470,225,57,new Color(.1f,.17f,.29f),()=>ShowCharacterSelection(true),18);
        Button("Forge",p.transform,"TÚI ĐỒ / SHOP",435,470,225,57,new Color(.1f,.17f,.29f),()=>ShowArmory(),18);
        Button("AR",p.transform,"CHƠI TRONG AR",190,545,225,53,new Color(.1f,.17f,.29f),()=>ShowCharacterSelection(false,true),18);
        Button("Settings",p.transform,"CÀI ĐẶT",435,545,225,53,new Color(.1f,.17f,.29f),()=>ShowSettings(ShowMenu),18);
        Button("Help",p.transform,"HƯỚNG DẪN",190,615,225,53,new Color(.1f,.17f,.29f),ShowHelp,18);
        Button("Scores",p.transform,"BẢNG ĐIỂM",435,615,225,53,new Color(.1f,.17f,.29f),ShowLeaderboard,18);
        Label("Best",p.transform,"Kỷ lục: "+PlayerPrefs.GetInt("RuneBastion.Best",0)+"     •     Tinh thạch: "+coins,150,700,550,38,18,new Color(.66f,.77f,.9f),TextAnchor.MiddleCenter);
        Button("Credits",p.transform,"TÀI NGUYÊN & NHÓM THỰC HIỆN",1000,820,510,44,new Color(.04f,.07f,.15f,.8f),ShowCredits,16);
        Button("ForgeMenu",p.transform,"LÒ RÈN VĨNH VIỄN",190,752,470,42,new Color(.1f,.17f,.29f),ShowForge,17);
        Label("Version",p.transform,"v2.0  •  KIẾM & PHÉP",1000,65,510,28,14,new Color(.64f,.75f,.89f),TextAnchor.MiddleRight);
        Button("Quit",p.transform,"THOÁT",1360,110,150,45,new Color(.04f,.07f,.15f,.8f),QuitGame,17);
    }
    void ShowHelp(){var p=Overlay("CÁCH GIỮ THÀNH","Chọn Himmel hoặc Frieren, giữ cổng qua "+TotalWaves+" đợt và hạ Nox để mở kết thúc đám cưới.");
        Label("HelpText",p.transform,"• Nhân vật tự đánh. Chạm quái để ưu tiên mục tiêu.\n• Himmel: kiếm khí tầm xa, liên trảm, xuyên quái và thánh kiếm.\n• Frieren: Lửa thiêu đốt, Băng làm chậm, Sét phá băng.\n• Vẽ cổ ngữ là ngoại lực dùng cho cả hai: tam giác / tròn / sét.\n• Kỹ năng và cổ ngữ có hồi chiêu riêng, dùng chung năng lượng.\n• Trang bị / lò rèn áp dụng trận thường; thử thách ngày bỏ lợi thế này.\n• Phím 1/2/3: kỹ năng; Esc: tạm dừng. Thành hết máu: Game Over.",90,200,1100,280,21,new Color(.8f,.87f,.96f));
        Button("Back",p.transform,"QUAY LẠI",440,530,390,60,new Color(.15f,.24f,.4f),ShowMenu);}
    void ShowCredits(){var p=Overlay("NGUỒN TÀI NGUYÊN","RUNE BASTION — Đồ án Unity nhóm 3 thành viên");
        Label("Credits",p.transform,"Orc, Troll và pháp sư cũ: CraftPix.net — Free Game Assets\nHimmel/Frieren: fan art tạo mới bằng imagegen; nhân vật gốc thuộc Frieren\nIcon trang bị: Ravenmore — CC BY 3.0 — ravenmore.itch.io\nHiệu ứng âm thanh: Kenney RPG Audio — CC0\nFont giao diện: Noto Sans — SIL Open Font License\nDữ liệu nét vẽ: Google Quick, Draw! — CC BY 4.0\nNền và biểu tượng phép: tạo bằng imagegen cho dự án\nNhạc Twilight Theme / Wedding Promise: sáng tác và tổng hợp riêng bằng mã\n\nĐiền tên và MSSV của nhóm trong hồ sơ bàn giao.",110,210,1050,285,19,new Color(.8f,.87f,.96f));
        Button("Back",p.transform,"QUAY LẠI",440,520,390,60,new Color(.15f,.24f,.4f),ShowMenu);}
    void ShowSettings(Action back){var p=Overlay("CÀI ĐẶT","Tùy chỉnh được lưu trên thiết bị.");
        AddSlider(p.transform,"NHẠC NỀN",220,musicVolume,v=>{musicVolume=v;if(musicSource)musicSource.volume=v;PlayerPrefs.SetFloat("RB.Music",v);});
        AddSlider(p.transform,"HIỆU ỨNG ÂM THANH",295,sfxVolume,v=>{sfxVolume=v;PlayerPrefs.SetFloat("RB.Sfx",v);});
        Button("Fx",p.transform,"HIỆU ỨNG: "+(highEffects?"ĐẦY ĐỦ":"TIẾT KIỆM"),150,380,460,54,new Color(.12f,.18f,.3f),()=>{highEffects=!highEffects;PlayerPrefs.SetInt("RB.Effects",highEffects?1:0);ShowSettings(back);},18);
        Button("Haptic",p.transform,"RUNG: "+(vibration?"BẬT":"TẮT"),650,380,460,54,new Color(.12f,.18f,.3f),()=>{vibration=!vibration;PlayerPrefs.SetInt("RB.Vibration",vibration?1:0);ShowSettings(back);},18);
        Button("Server",p.transform,"KẾT NỐI SERVER",150,450,460,50,new Color(.1f,.15f,.26f),()=>ShowServerSettings(()=>ShowSettings(back)),18);
        Button("Back",p.transform,"LƯU & QUAY LẠI",440,530,390,58,new Color(.16f,.27f,.44f),()=>{PlayerPrefs.Save();back();});}
    void AddSlider(Transform parent,string caption,float y,float value,Action<float> changed){
        Label("SliderLabel",parent,caption,150,y,450,30,21,new Color(.8f,.9f,1));
        var node=Node("VolumeSlider",parent,660,y,450,34);var slider=node.gameObject.AddComponent<Slider>();
        var track=RoundPanel("Track",node,0,12,450,10,new Color(.15f,.23f,.37f));var fill=RoundPanel("Fill",node,0,12,450,10,new Color(.2f,.75f,1));
        fill.rectTransform.pivot=new Vector2(.5f,.5f);fill.rectTransform.anchorMin=new Vector2(0,.5f);fill.rectTransform.anchorMax=new Vector2(1,.5f);fill.rectTransform.sizeDelta=new Vector2(0,10);fill.rectTransform.anchoredPosition=Vector2.zero;
        var handle=Panel("Handle",node,0,0,26,26,Color.white);handle.sprite=disk;handle.rectTransform.pivot=new Vector2(.5f,.5f);handle.rectTransform.anchorMin=handle.rectTransform.anchorMax=new Vector2(0,.5f);handle.rectTransform.anchoredPosition=Vector2.zero;
        slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.minValue=0;slider.maxValue=1;slider.SetValueWithoutNotify(value);slider.onValueChanged.AddListener(v=>changed(v));
    }
    public void TogglePause(){if(State!=Phase.Battle)return;
        if(Paused){Paused=false;Time.timeScale=1;if(overlay)Destroy(overlay);return;}
        Paused=true;Time.timeScale=0;PauseMenu();}
    void PauseMenu(){var p=Overlay("TẠM DỪNG","Một nhịp nghỉ trước đợt sóng tiếp theo.");
        Button("Resume",p.transform,"TIẾP TỤC",410,230,450,66,new Color(.15f,.3f,.45f),TogglePause,25);
        Button("Settings",p.transform,"CÀI ĐẶT",410,320,450,58,new Color(.1f,.17f,.29f),()=>ShowSettings(PauseMenu));
        Button("Home",p.transform,"VỀ MÀN HÌNH CHÍNH",410,400,450,58,new Color(.1f,.17f,.29f),ConfirmReturnHome);}
    void ConfirmReturnHome(){var p=Overlay("RỜI TRẬN HIỆN TẠI?","Tiến trình của trận đang chơi sẽ kết thúc. Kỷ lục đã lưu vẫn được giữ.");
        Button("Stay",p.transform,"TIẾP TỤC CHƠI",180,290,420,70,new Color(.15f,.27f,.4f),PauseMenu);
        Button("Leave",p.transform,"VỀ MENU",670,290,420,70,new Color(.25f,.13f,.22f),ShowMenu);}
    void RefreshHud(){
        hpFill.rectTransform.sizeDelta=new Vector2(363*Mathf.Clamp01(Health/1000),15);hpText.text=Mathf.CeilToInt(Health)+" / 1000";
        manaFill.rectTransform.sizeDelta=new Vector2(520*Mana/100,14);manaText.text=Mathf.FloorToInt(Mana)+" / 100";
        scoreText.text=Score.ToString("N0");waveText.text="ĐỢT "+Wave+" / "+TotalWaves;progress.rectTransform.sizeDelta=new Vector2(300*Mathf.Max(0,Wave-1)/(TotalWaves-1),2);
        for(int i=0;i<TotalWaves;i++)waveDots[i].color=i<Wave?new Color(.25f,.9f,1):new Color(.33f,.37f,.5f);
        waveCaption.text=dailyMode?"THỬ THÁCH NGÀY "+DateTime.UtcNow.ToString("dd/MM"):Wave==TotalWaves?"NOX — GIỮ VỮNG CỔNG THÀNH":HeroName.ToUpperInvariant()+" — BẢO VỆ CỔNG SAO";
        RefreshHeroHud();for(int i=0;i<3;i++){cooldownImages[i].fillAmount=cooldown[i]/(i==2?6:i==1?5:4);cooldownNumbers[i].text=cooldown[i]>0?Mathf.CeilToInt(cooldown[i]).ToString():"";
            skillRects[i].localScale=Vector3.one*(Mana<costs[i]? .92f:1);}
    }
    void ShowUpgrades(int[] previewChoices=null){
        State=Phase.Upgrade;Score+=Wave*50;Play(coinSound,.5f);
        var p=Overlay("ĐỢT "+Wave+" HOÀN THÀNH","Chọn một nâng cấp để chuẩn bị cho đợt tiếp theo.");
        int[] choices=previewChoices??System.Linq.Enumerable.Range(0,6).OrderBy(_=>random.Next()).Take(3).ToArray();
        string[] titles={"SỨC MẠNH","TỐC ĐỘ TẤN CÔNG","HỒI NĂNG LƯỢNG","SỬA CỔNG THÀNH","SÉT LAN","BĂNG VĨNH CỬU"};
        string[] desc={"Tăng 20% sát thương","Giảm 15% thời gian chờ\ngiữa các đòn đánh thường","Hồi thêm 2 năng lượng\nmỗi giây","Phục hồi 250 máu thành\nTối đa 1.000 máu","Sét đánh thêm 1 mục tiêu","Băng làm chậm quái\ntrong 6 giây thay vì 4 giây"};
        string[] icons={SelectedHero==Hero.Himmel?"upg_sword":"upg_wand","gemGreen","potionBlue","hammer","gemBlue","gemBlue"};
        string[] categories={"SÁT THƯƠNG","TỐC ĐỘ","NĂNG LƯỢNG","PHÒNG THỦ","CỔ NGỮ SÉT","CỔ NGỮ BĂNG"};
        Color[] accents={new Color(1,.75f,.35f),new Color(.4f,1,.68f),new Color(.3f,.8f,1),new Color(1,.55f,.55f),new Color(1,.85f,.35f),new Color(.5f,.9f,1)};
        if(SelectedHero==Hero.Himmel){titles[4]="KIẾM KHÍ CƯỜNG HÓA";desc[4]="+10% sát thương cơ bản\nCổ ngữ Sét thêm 1 mục tiêu";titles[5]="NHỊP KIẾM";desc[5]="Giảm 10% thời gian chờ đánh\nCổ ngữ Băng kéo dài 6 giây";icons[4]="upg_sword";icons[5]="sword";categories[4]="KIẾM KHÍ + SÉT";categories[5]="NHỊP KIẾM + BĂNG";}
        for(int i=0;i<3;i++){
            int id=choices[i];var b=Button("Upgrade"+id,p.transform,"",60+i*398,220,365,300,new Color(.065f,.11f,.21f),()=>SelectUpgrade(id));
            var strip=RoundPanel("UpgradeAccent",b.transform,16,15,333,4,accents[id]);strip.raycastTarget=false;
            var disc=Panel("UpgradeIconDisc",b.transform,123,31,118,118,new Color(accents[id].r*.23f,accents[id].g*.23f,accents[id].b*.23f));disc.sprite=disk;disc.raycastTarget=false;
            var icon=Panel("UpgradeIcon",disc.transform,15,15,88,88,Color.white);icon.preserveAspect=true;icon.raycastTarget=false;
            icon.sprite=id==4&&SelectedHero==Hero.Frieren?abilityIcons[2]:id==5&&SelectedHero==Hero.Frieren?abilityIcons[1]:Resources.Load<Sprite>("Equipment/"+icons[id]);
            if(SelectedHero==Hero.Himmel&&(id==4||id==5)){var rune=Panel("ExternalRuneBadge",b.transform,214,99,45,45,Color.white);rune.sprite=abilityIcons[id==4?2:1];rune.preserveAspect=true;rune.raycastTarget=false;}
            Label("UpgradeCategory",b.transform,categories[id],18,156,329,23,13,accents[id],TextAnchor.MiddleCenter);
            Heading(b.transform,titles[id],15,183,335,48,21,Color.white);
            Label("UpgradeDescription",b.transform,desc[id],20,238,325,52,18,new Color(.77f,.86f,.96f),TextAnchor.MiddleCenter);
        }
        Label("UpgradeHint",p.transform,"Chạm vào thẻ nâng cấp để nhận hiệu ứng và bắt đầu đợt tiếp theo.",90,548,1100,35,18,new Color(.65f,.78f,.9f),TextAnchor.MiddleCenter);
    }
    void EndRun(bool won){if(State==Phase.Won||State==Phase.Lost)return;SaveRunResult(won);if(arMode)ExitAr();if(won)ShowWedding();else StartCoroutine(DefeatEnding());}
    void CreateAudio(){
        audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.spatialBlend=0;
        musicSource=gameObject.AddComponent<AudioSource>();musicSource.playOnAwake=false;musicSource.loop=true;musicSource.spatialBlend=0;
        musicSource.clip=Resources.Load<AudioClip>("Audio/TwilightTheme");musicSource.volume=musicVolume;if(musicSource.clip)musicSource.Play();
        clickSound=Resources.Load<AudioClip>("Audio/click");coinSound=Resources.Load<AudioClip>("Audio/coin");runeSound=Resources.Load<AudioClip>("Audio/rune");gateSound=Resources.Load<AudioClip>("Audio/gate");hitSound=Resources.Load<AudioClip>("Audio/hit");
        fireSound=Tone("Fire",150,.24f,.8f);iceSound=Tone("Ice",950,.26f,.15f);lightningSound=Tone("Lightning",80,.3f,.9f);
    }
    void Play(AudioClip clip,float volume){if(audioSource&&clip)audioSource.PlayOneShot(clip,volume*sfxVolume);}
    void PlayUi(){Play(clickSound,.45f);}
    void QuitGame(){
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying=false;
#else
        Application.Quit();
#endif
    }
    void Haptic(){
#if UNITY_ANDROID && !UNITY_EDITOR
        if(vibration)Handheld.Vibrate();
#endif
    }
    void ShowForge(){var p=Overlay("LÒ RÈN ẤN THUẬT","Tinh thạch: "+coins+"  •  Nâng cấp vĩnh viễn cho chế độ thường. Thử thách ngày không dùng nâng cấp này.");
        string[] keys={"RB.ForgeDamage","RB.ForgeMana","RB.ForgeSpeed"};string[] titles={"UY LỰC CHIẾN ĐẤU","DÒNG NĂNG LƯỢNG","NHỊP CHIẾN ĐẤU"};string[] desc={"+8% sát thương mỗi cấp","+1 năng lượng / giây mỗi cấp","Tự đánh nhanh hơn 7% mỗi cấp"};
        for(int i=0;i<3;i++){int id=i;int level=PlayerPrefs.GetInt(keys[i],0);int cost=100+level*100;
            Button("Forge"+i,p.transform,titles[i]+"\n\n"+desc[i]+"\nCấp "+level+" / 3\n"+(level>=3?"ĐÃ TỐI ĐA":cost+" tinh thạch"),60+i*398,235,365,245,new Color(.1f,.16f,.28f),()=>{
                if(level>=3)return;if(coins<cost){ShowTip("Chưa đủ tinh thạch. Hoàn thành thêm trận để nhận thưởng.");return;}
                coins-=cost;PlayerPrefs.SetInt("RB.Coins",coins);PlayerPrefs.SetInt(keys[id],level+1);PlayerPrefs.Save();Play(coinSound,.6f);ShowForge();});}
        Button("Back",p.transform,"QUAY LẠI",440,530,390,58,new Color(.15f,.25f,.4f),ShowMenu);}
#if UNITY_EDITOR
    public void VerificationSetMode(bool value){verificationMode=value;}
#endif
    void OnApplicationPause(bool pause){if(pause&&State==Phase.Battle&&!Paused)TogglePause();PlayerPrefs.Save();}
}
}
