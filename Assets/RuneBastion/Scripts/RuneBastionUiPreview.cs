#if UNITY_EDITOR
namespace RuneBastion {
public sealed partial class RuneBastionGame {
    public void VerificationUpgradeCards(bool second){verificationMode=true;SelectedHero=second?Hero.Frieren:Hero.Himmel;dailyMode=false;StartRun();ShowUpgrades(second?new[]{1,4,5}:new[]{0,2,3});}
}
}
#endif
