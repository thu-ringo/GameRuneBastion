#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
namespace RuneBastion {
public sealed partial class RuneBastionGame {
    public void VerificationSelection(){verificationMode=true;ShowCharacterSelection();}
    public void VerificationHero(bool frieren){verificationMode=true;SelectedHero=frieren?Hero.Frieren:Hero.Himmel;dailyMode=false;StartRun();VerificationSpawn();}
    public void VerificationEnding(bool won){verificationMode=true;StartRun();ClearBattle();SaveRunResult(won);if(won)ShowWedding();else StartCoroutine(DefeatEnding());}
    public void VerificationArmory(){verificationMode=true;ShowArmory(1);}
    public void VerificationRunePanel(){VerificationHero(false);OpenRunePad();}
    public void VerificationLiveCombat(){StartCoroutine(LiveCombatCheck());}
    IEnumerator LiveCombatCheck(){
        Hero previous=SelectedHero;var lines=new List<string>();verificationMode=true;
        try{
        foreach(var hero in new[]{Hero.Himmel,Hero.Frieren}){
            SelectedHero=hero;dailyMode=false;StartRun();VerificationSpawn();autoClock=0;toSpawn=0;
            bool sawProjectile=false;float deadline=Time.realtimeSinceStartup+20;
            while(elapsed<1.8f&&Time.realtimeSinceStartup<deadline){sawProjectile|=shots.Any(s=>s.blade==(hero==Hero.Himmel));yield return null;}
            if(!sawProjectile)throw new Exception("Live projectile missing for "+hero+" elapsed="+elapsed+" paused="+Paused);
            lines.Add("PASS "+hero+" releases correct projectile after animation windup");
            if(!enemies.Any(e=>e.health<e.maxHealth)&&Kills==0)throw new Exception("Auto attack failed for "+hero);
            lines.Add("PASS "+hero+" real projectile damages enemies");
            Mana=100;float before=enemies.Sum(e=>e.health);if(!CastHeroSkill(1))throw new Exception("Live skill failed "+hero);
            float until=elapsed+2;while(elapsed<until)yield return null;
            if(enemies.Sum(e=>e.health)>=before&&Kills==0)throw new Exception("Skill failed to hit "+hero);
            lines.Add("PASS "+hero+" live skill reaches enemy group");
        }
        SelectedHero=Hero.Himmel;StartRun();VerificationSpawn();toSpawn=0;foreach(var e in enemies)e.root.transform.position=new Vector3(Gate,Ground,0);Health=1000;autoClock=100;
        yield return new WaitForSeconds(.12f);if(Health<=0||Health>=1000)throw new Exception("Gate attack behavior incorrect");lines.Add("PASS enemies reaching gate damage it without instant loss");
        Health=0;yield return new WaitForSeconds(.1f);if(State!=Phase.Lost)throw new Exception("Gate zero must lose");lines.Add("PASS zero gate health triggers defeat ending");
        yield return new WaitForSecondsRealtime(2.5f);if(endingActive||!overlay||State!=Phase.Lost)throw new Exception("Defeat ending did not reach results");lines.Add("PASS defeat cinematic reaches Game Over");
        }finally{SelectedHero=previous;verificationMode=false;ShowMenu();}
        File.WriteAllLines("RuneBastionDocs/Verification/v2-live-combat.txt",lines);Debug.Log("RUNE BASTION live combat: "+lines.Count+" checks passed");
    }
    public void VerificationV2(){
        var lines=new List<string>();Action<bool,string> check=(ok,msg)=>{if(!ok)throw new Exception("FAIL "+msg);lines.Add("PASS "+msg);};
        Hero previous=SelectedHero;string previousName=PlayerName;int previousCoins=coins;var levels=(int[])gearLevels.Clone();var slots=(int[,])equipped.Clone();
        verificationMode=true;
        try{
            check(CleanPlayerName("  <A>\n  ")=="A","player names stripped of markup / control characters");
            ShowCharacterSelection();playerNameInput.text="";ConfirmCharacter();check(State==Phase.Menu,"empty name blocks run");
            playerNameInput.text="Rune Keeper";ConfirmCharacter();check(State==Phase.Battle&&PlayerName=="Rune Keeper","valid player name starts selected character run");
            foreach(var hero in new[]{Hero.Himmel,Hero.Frieren}){
                SelectedHero=hero;RefreshHeroArt();foreach(var clip in new[]{"idle","move","attack","hurt","defeat","victory"})check(wizardClips[clip].Length==4,hero+" "+clip+" has four frames");
                dailyMode=false;StartRun();VerificationSpawn();check(wizard.gameObject.activeSelf&&!wizard.flipX,hero+" selected actor active and facing enemy");
                float before=Mana;check(CastHeroSkill(0)&&Mana<before,hero+" skill spends mana");check(!CastHeroSkill(0),hero+" skill cooldown blocks spam");
                Mana=100;check(CastRune(1),hero+" external ice rune works");check(!CastRune(1),hero+" rune cooldown blocks spam");
                check(cooldown[1]==0&&runeCooldown[1]>0,hero+" rune cooldown independent of character skill");
                if(hero==Hero.Himmel){check(CastHeroSkill(1),"Himmel moon slash independent of ice rune cooldown");}
                TogglePause();check(Paused&&Time.timeScale==0,hero+" pause");TogglePause();check(!Paused&&Time.timeScale==1,hero+" resume");
            }
            SelectedHero=Hero.Himmel;coins=2000;gearLevels[1]=-1;check(BuyGear(1)&&coins==1780&&gearLevels[1]==0,"shop purchase debits correct currency");check(!BuyGear(1),"shop rejects buying owned item");
            check(EquipGear(1)&&EquipmentFor(SelectedHero,0)==1,"equip compatible weapon");check(!EquipGear(2),"Himmel cannot equip Frieren staff");
            check(UpgradeGear(1)&&gearLevels[1]==1,"forge upgrades owned item");check(!SalvageGear(1),"cannot dismantle equipped gear");
            equipped[0,0]=0;check(SalvageGear(1)&&gearLevels[1]==-1,"dismantle unequipped gear");
            coins=0;gearLevels[1]=-1;check(!BuyGear(1)&&coins==0,"insufficient funds protected");gearLevels[1]=0;equipped[0,0]=1;
            dailyMode=false;StartRun();float geared=damageMultiplier;dailyMode=true;StartRun();check(geared>damageMultiplier&&gateGuard==0,"daily mode excludes gear and forge advantage");dailyMode=false;
            SaveRunResult(true);ShowWedding();check(State==Phase.Won&&endingActive&&overlay.name=="WeddingEnding","victory opens wedding");
            check(overlay.GetComponentsInChildren<HeroPortrait>().Select(a=>a.hero).Distinct().Count()==2,"both heroes appear in wedding");
            check(Resources.Load<AudioClip>("Audio/WeddingPromise")!=null,"wedding music available");ShowRunResults();check(!endingActive&&State==Phase.Won,"skip wedding keeps victory result");
            StartRun();check(Wave==1&&Health==1000,"retry resets battle");SaveRunResult(false);ShowRunResults();check(State==Phase.Lost&&!endingActive,"loss opens game over without wedding");
        }finally{
            StopAllCoroutines();ClearBattle();Array.Copy(levels,gearLevels,levels.Length);Array.Copy(slots,equipped,slots.Length);coins=previousCoins;SelectedHero=previous;PlayerName=previousName;dailyMode=false;verificationMode=false;ShowMenu();
        }
        Directory.CreateDirectory("RuneBastionDocs/Verification");File.WriteAllLines("RuneBastionDocs/Verification/v2-checks.txt",lines);Debug.Log("RUNE BASTION v2: "+lines.Count+" checks passed");
    }
}
}
#endif
