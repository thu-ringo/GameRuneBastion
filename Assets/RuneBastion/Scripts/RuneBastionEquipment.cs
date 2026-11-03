using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RuneBastion {
public sealed partial class RuneBastionGame {
    sealed class Gear {
        public string name,icon,description;public int slot,hero,price;public float damage,regen,speed,guard;public Color color;
        public Gear(string name,string icon,string description,int slot,int hero,int price,float damage=0,float regen=0,float speed=0,float guard=0){
            this.name=name;this.icon=icon;this.description=description;this.slot=slot;this.hero=hero;this.price=price;this.damage=damage;this.regen=regen;this.speed=speed;this.guard=guard;color=slot==3?new Color(.5f,.85f,1):new Color(1,.84f,.48f);
        }
    }
    static readonly Gear[] gear={
        new Gear("Kiếm lữ hành","sword","Kiếm khí ổn định cho người dũng sĩ.",0,0,0,.04f),
        new Gear("Kiếm bình minh","upg_sword","Kiếm khí mạnh hơn và nhịp chém nhanh hơn.",0,0,220,.14f,0,.06f),
        new Gear("Trượng lữ hành","wand","Trượng phép đồng hành qua những miền đất xa.",0,1,0,.04f),
        new Gear("Trượng tinh tú","upg_wand","Tăng uy lực phép và khơi dòng năng lượng.",0,1,220,.14f,1),
        new Gear("Mũ hộ vệ","helmet","Bảo hộ cổng bằng một ấn chú bền bỉ.",1,-1,100,0,0,0,.05f),
        new Gear("Mũ thánh quang","upg_helmet","Bảo hộ mạnh hơn, tăng khả năng hồi năng lượng.",1,-1,180,0,.5f,0,.09f),
        new Gear("Giáp lữ hành","armor","Kết giới giáp giúp cổng chịu ít sát thương hơn.",2,-1,120,0,0,0,.08f),
        new Gear("Giáp hoàng hôn","upg_armor","Giáp cổ ngữ gia cố cổng và tăng tốc đánh.",2,-1,220,0,0,.04f,.13f),
        new Gear("Ngọc thủy triều","gemBlue","Năng lượng chảy quanh người mang ngọc.",3,-1,130,0,1.5f),
        new Gear("Ngọc hỏa tâm","gemRed","Ánh đỏ tăng sức mạnh mọi đòn tấn công.",3,-1,150,.12f),
        new Gear("Ngọc phong linh","gemGreen","Nhịp chiến đấu nhẹ và nhanh như gió.",3,-1,150,0,0,.10f),
        new Gear("Sách khế ước","tome","Cân bằng uy lực và năng lượng của cổ ngữ.",3,-1,200,.06f,1)
    };
    readonly int[] gearLevels=new int[12];readonly int[,] equipped=new int[2,4];
    int selectedGear,gearTab;float gateGuard;
    readonly string[] slotNames={"VŨ KHÍ","MŨ","GIÁP","CỔ NGỮ"};
    void LoadEquipment(){
        for(int i=0;i<gear.Length;i++)gearLevels[i]=Mathf.Clamp(PlayerPrefs.GetInt("RB.Gear.Level."+i,i==0||i==2?0:-1),-1,3);
        for(int h=0;h<2;h++)for(int s=0;s<4;s++){
            int id=PlayerPrefs.GetInt("RB.Gear.Equip."+h+"."+s,s==0?(h==0?0:2):-1);
            equipped[h,s]=id>=0&&id<gear.Length&&gearLevels[id]>=0&&gear[id].slot==s&&(gear[id].hero<0||gear[id].hero==h)?id:-1;
        }
    }
    int EquipmentFor(Hero hero,int slot)=>equipped[(int)hero,slot];
    void SaveEquipment(){if(verificationMode)return;for(int i=0;i<gear.Length;i++)PlayerPrefs.SetInt("RB.Gear.Level."+i,gearLevels[i]);for(int h=0;h<2;h++)for(int s=0;s<4;s++)PlayerPrefs.SetInt("RB.Gear.Equip."+h+"."+s,equipped[h,s]);PlayerPrefs.SetInt("RB.Coins",coins);PlayerPrefs.Save();}
    void ApplyEquipment(){gateGuard=0;for(int s=0;s<4;s++){int id=EquipmentFor(SelectedHero,s);if(id<0)continue;var g=gear[id];float factor=1+gearLevels[id]*.25f;damageMultiplier+=g.damage*factor;manaRegen+=g.regen*factor;attackInterval*=1-g.speed*factor;gateGuard+=g.guard*factor;}gateGuard=Mathf.Min(.45f,gateGuard);}
    string GearStats(int id){var g=gear[id];float f=1+Mathf.Max(0,gearLevels[id])*.25f;string value="";
        if(g.damage>0)value+="Sát thương +"+(g.damage*f*100).ToString("0")+"%\n";if(g.regen>0)value+="Năng lượng +"+(g.regen*f).ToString("0.0")+" / giây\n";
        if(g.speed>0)value+="Tốc đánh +"+(g.speed*f*100).ToString("0")+"%\n";if(g.guard>0)value+="Giảm sát thương cổng "+(g.guard*f*100).ToString("0")+"%\n";return value.Trim();}
    bool CanUseGear(int id)=>gear[id].hero<0||gear[id].hero==(int)SelectedHero;
    bool BuyGear(int id){if(id<0||id>=gear.Length||gearLevels[id]>=0||coins<gear[id].price)return false;coins-=gear[id].price;gearLevels[id]=0;SaveEquipment();return true;}
    bool EquipGear(int id){if(id<0||id>=gear.Length||gearLevels[id]<0||!CanUseGear(id))return false;equipped[(int)SelectedHero,gear[id].slot]=id;SaveEquipment();return true;}
    bool UpgradeGear(int id){if(id<0||id>=gear.Length||gearLevels[id]<0||gearLevels[id]>=3)return false;int price=80+gearLevels[id]*80;if(coins<price)return false;coins-=price;gearLevels[id]++;SaveEquipment();return true;}
    bool SalvageGear(int id){if(id<0||id>=gear.Length||id==0||id==2||gearLevels[id]<0)return false;
        for(int h=0;h<2;h++)for(int s=0;s<4;s++)if(equipped[h,s]==id)return false;
        coins+=Mathf.Max(20,gear[id].price/3)+gearLevels[id]*25;gearLevels[id]=-1;SaveEquipment();return true;}
    string AwardEquipment(){var available=Enumerable.Range(0,gear.Length).Where(i=>gear[i].price>0&&gearLevels[i]<0&&CanUseGear(i)).ToArray();
        if(available.Length==0){coins+=60;PlayerPrefs.SetInt("RB.Coins",coins);return "Kho đồ đã đủ: +60 tinh thạch thưởng boss";}
        int id=available[random.Next(available.Length)];gearLevels[id]=0;SaveEquipment();return "Chiến lợi phẩm boss: "+gear[id].name;
    }
    void ShowArmory(int tab=0){
        gearTab=tab;State=Phase.Menu;hudRoot.gameObject.SetActive(false);PlaceHero(true);
        var p=Overlay("THÁP CỔ NGỮ",HeroName+"  ·  "+coins+" tinh thạch  ·  Trang bị áp dụng từ trận thường tiếp theo; thử thách ngày không dùng trang bị.");
        Button("InventoryTab",p.transform,"TÚI ĐỒ",60,165,210,43,tab==0?new Color(.18f,.32f,.46f):new Color(.08f,.14f,.24f),()=>ShowArmory(0),18);
        Button("ShopTab",p.transform,"CỬA HÀNG",285,165,210,43,tab==1?new Color(.18f,.32f,.46f):new Color(.08f,.14f,.24f),()=>ShowArmory(1),18);
        Button("SwitchGearHero",p.transform,HeroName+" / ĐỔI",510,165,210,43,new Color(.08f,.14f,.24f),()=>{SelectedHero=SelectedHero==Hero.Himmel?Hero.Frieren:Hero.Himmel;PlayerPrefs.SetInt("RB.Hero",(int)SelectedHero);RefreshHeroArt();ShowArmory(tab);},18);
        var ids=Enumerable.Range(0,gear.Length).Where(i=>tab==1||gearLevels[i]>=0).ToArray();
        if(!ids.Contains(selectedGear))selectedGear=ids.FirstOrDefault();
        for(int i=0;i<ids.Length;i++){
            int id=ids[i];float x=60+(i%4)*169,y=230+(i/4)*95;bool selected=id==selectedGear;
            var b=Button("Gear"+id,p.transform,"",x,y,157,85,selected?new Color(.2f,.32f,.43f):new Color(.08f,.13f,.23f),()=>{selectedGear=id;ShowArmory(tab);});
            var icon=Panel("GearIcon",b.transform,6,6,55,55,Color.white);icon.sprite=Resources.Load<Sprite>("Equipment/"+gear[id].icon);icon.preserveAspect=true;icon.raycastTarget=false;
            Label("GearName",b.transform,gear[id].name,62,5,90,44,13,Color.white);
            bool isEquipped=equipped[(int)SelectedHero,gear[id].slot]==id;
            Label("GearState",b.transform,gearLevels[id]<0?gear[id].price+" TT":isEquipped?"ĐANG ĐEO":"CẤP "+gearLevels[id],8,60,142,20,12,gear[id].color,TextAnchor.MiddleCenter);
        }
        int item=selectedGear;var g=gear[item];bool owned=gearLevels[item]>=0;
        RoundPanel("GearDetails",p.transform,760,165,450,365,new Color(.055f,.1f,.19f));
        var detailIcon=Panel("ItemIcon",p.transform,785,182,105,105,Color.white);detailIcon.sprite=Resources.Load<Sprite>("Equipment/"+g.icon);detailIcon.preserveAspect=true;
        Heading(p.transform,g.name,895,185,290,70,24,new Color(1,.86f,.55f));
        Label("GearSlot",p.transform,slotNames[g.slot]+" · "+(g.hero<0?"Cả hai nhân vật":((Hero)g.hero).ToString()),895,257,290,25,14,new Color(.6f,.8f,.95f));
        Label("GearDescription",p.transform,g.description+"\n\n"+GearStats(item),785,305,400,125,19,new Color(.8f,.87f,.96f));
        bool wearing=equipped[(int)SelectedHero,g.slot]==item;
        Button("EquipOrBuy",p.transform,!owned?"MUA · "+g.price+" TT":!CanUseGear(item)?"DÀNH CHO "+((Hero)g.hero):wearing?"THÁO TRANG BỊ":"TRANG BỊ",785,438,400,43,new Color(.15f,.28f,.4f),()=>{
            if(!owned){if(!BuyGear(item)){ShowTip("Chưa đủ tinh thạch.");return;}Play(coinSound,.45f);}
            else if(CanUseGear(item)){if(wearing){equipped[(int)SelectedHero,g.slot]=-1;SaveEquipment();}else EquipGear(item);}
            ShowArmory(tab);
        },17);
        if(owned)Button("UpgradeGear",p.transform,gearLevels[item]>=3?"ĐÃ NÂNG TỐI ĐA":"RÈN CẤP "+(gearLevels[item]+1)+" · "+(80+gearLevels[item]*80)+" TT",785,488,400,34,new Color(.14f,.19f,.3f),()=>{if(!UpgradeGear(item)){ShowTip("Chưa đủ tinh thạch hoặc vật phẩm đã tối đa.");return;}Play(coinSound,.4f);ShowArmory(tab);},15);
        for(int s=0;s<4;s++){int id=equipped[(int)SelectedHero,s];Label("EquippedSlot"+s,p.transform,slotNames[s]+": "+(id<0?"Trống":gear[id].name),60+(s%2)*335,520+(s/2)*24,330,23,13,new Color(.67f,.83f,.94f));}
        Button("ArmoryBack",p.transform,"VỀ MENU",60,577,220,30,new Color(.12f,.2f,.33f),ShowMenu,15);
        if(owned&&item!=0&&item!=2)Button("Salvage",p.transform,"PHÂN GIẢI ĐỒ CHƯA ĐEO",785,555,400,36,new Color(.26f,.12f,.2f),()=>ConfirmSalvage(item,tab),15);
        Label("ArmoryFeedback",p.transform,"Vật phẩm đổi chỉ số và hào quang; không đổi quần áo nhân vật.",320,580,420,27,11,new Color(.55f,.68f,.82f));
    }
    void ConfirmSalvage(int id,int tab){
        var p=Overlay("PHÂN GIẢI TRANG BỊ?",gear[id].name+" sẽ biến thành tinh thạch. Chỉ phân giải được đồ không được nhân vật nào trang bị.");
        Button("CancelSalvage",p.transform,"GIỮ LẠI",150,330,440,65,new Color(.12f,.23f,.35f),()=>ShowArmory(tab));
        Button("ConfirmSalvage",p.transform,"PHÂN GIẢI",670,330,440,65,new Color(.28f,.12f,.2f),()=>{if(!SalvageGear(id)){ShowTip("Hãy tháo vật phẩm ở cả hai nhân vật trước.");}ShowArmory(tab);});
    }
}
}
