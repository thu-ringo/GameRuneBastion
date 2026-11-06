using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RuneBastion {
public sealed partial class RuneBastionGame {
    sealed class DormantEnemy {
        public Enemy enemy;
        public SpriteRenderer eyes;
        public float risesAt;
    }

    const int FeigningEnemies = 4;
    const float WakeDelay = 6f;
    const float MirrorDelay = 5f;
    static readonly Color BellGold = new Color(1, .79f, .36f);
    static readonly Color MirrorViolet = new Color(.82f, .58f, 1);
    static readonly string[] RuneNames = { "Lửa", "Băng", "Sét" };

    readonly List<DormantEnemy> dormantEnemies = new List<DormantEnemy>();
    GameObject encounterCard;
    Text encounterStatus;
    AudioClip bellSound;
    Button encounterButton;
    bool bellUsed, mirrorUsed, mirrorFocused;
    Enemy mirrorBearer;
    SpriteRenderer mirrorGlass;
    Transform mirrorFrame;
    int mirrorSpell = -1;
    float mirrorDeadline;

    void PrepareWaveEncounter() {
        ClearWaveEncounter();
        if (dailyMode) return;
        if (Wave == 2) {
            CreateEncounterCard("ĐÀN QUÁI GIẢ CHẾT", BellGold);
            encounterButton = Button("RingTheBell", encounterCard.transform, "KÉO CHUÔNG", 285, 83, 230, 45,
                new Color(.35f, .23f, .12f), RingTheBell, 19);
            TickBellEncounter();
        } else if (Wave == 4) {
            CreateEncounterCard("KẺ TRỘM NÉT VẼ", MirrorViolet);
            encounterStatus.text = "Dùng Vẽ phép để dụ nó sao chép rune.";
            encounterButton = Button("FocusMirror", encounterCard.transform, "NHẮM GƯƠNG", 285, 83, 230, 45,
                new Color(.26f, .16f, .43f), FocusMirror, 19);
            encounterButton.gameObject.SetActive(false);
        }
    }

    void CreateEncounterCard(string title, Color accent) {
        var card = RoundPanel("WaveEncounter", hudRoot, 400, 122, 800, 137, new Color(.045f, .07f, .15f, .94f));
        card.raycastTarget = false;
        encounterCard = card.gameObject;
        RoundPanel("EncounterAccent", card.transform, 26, 5, 748, 3, accent).raycastTarget = false;
        Heading(card.transform, title, 20, 11, 760, 33, 25, accent);
        encounterStatus = Label("EncounterStatus", card.transform, "", 18, 46, 764, 34, 18, Color.white, TextAnchor.MiddleCenter);
    }

    void SpawnWaveEnemy() {
        int kind = ChooseEnemyKind();
        if (!dailyMode && Wave == 4 && waveSpawned == 3) {
            SpawnMirrorBearer();
            return;
        }
        var enemy = Spawn(kind);
        if (!dailyMode && Wave == 2 && waveSpawned < FeigningEnemies) {
            enemy.feignsDeath = true;
            var mark = Shape("False life rune", enemy.root.transform, new Vector3(0, -.04f, 0),
                new Vector3(.85f, .14f, 1), new Color(.85f, .26f, .56f, .8f), 3);
            enemy.encounterMark = mark.gameObject;
        }
    }

    bool TryFeignDeath(Enemy enemy) {
        if (!enemy.feignsDeath) return false;
        enemy.feignsDeath = false;
        enemy.alive = false;
        enemies.Remove(enemy);
        if (enemy.animator) enemy.animator.enabled = false;
        enemy.art.localPosition = new Vector3(0, .13f, 0);
        enemy.art.localRotation = Quaternion.Euler(0, 0, 72);
        enemy.body.color = new Color(.46f, .36f, .58f);
        enemy.hp.parent.gameObject.SetActive(false);
        var position = enemy.root.transform.position;
        position.y = Ground;
        enemy.root.transform.position = position;
        var eyes = Shape("Reviving eyes", enemy.root.transform, new Vector3(-.37f, .28f, 0),
            new Vector3(.18f, .09f, 1), new Color(1, .37f, .66f), 14);
        dormantEnemies.Add(new DormantEnemy { enemy = enemy, eyes = eyes, risesAt = elapsed + WakeDelay });
        return true;
    }

    void TickBellEncounter() {
        float nextWake = WakeDelay;
        for (int i = dormantEnemies.Count - 1; i >= 0; i--) {
            var entry = dormantEnemies[i];
            float remaining = entry.risesAt - elapsed;
            if (remaining <= 0) {
                WakeEnemy(entry);
                dormantEnemies.RemoveAt(i);
                continue;
            }
            nextWake = Mathf.Min(nextWake, remaining);
            var glow = entry.eyes.color;
            float age = WakeDelay - remaining;
            glow.a = .25f + .75f * Mathf.Abs(Mathf.Sin(age * 2 + age * age * 1.5f));
            entry.eyes.color = glow;
        }
        encounterButton.interactable = !bellUsed && dormantEnemies.Count > 0;
        encounterStatus.text = dormantEnemies.Count == 0
            ? bellUsed ? "Chuông đã được kéo." : "Quái có dấu hồng sẽ bật dậy. Chuông chỉ dùng một lần."
            : $"{dormantEnemies.Count} quái nằm im   •   Con đầu tiên bật dậy sau {Mathf.CeilToInt(nextWake)} giây";
    }

    void WakeEnemy(DormantEnemy entry) {
        var enemy = entry.enemy;
        Destroy(entry.eyes.gameObject);
        if (enemy.encounterMark) Destroy(enemy.encounterMark);
        enemy.alive = true;
        enemy.health = enemy.maxHealth * .5f;
        enemy.burnUntil = enemy.frozenUntil = enemy.hitUntil = 0;
        enemy.attackClock = 0;
        enemy.speed *= 1.65f;
        enemy.art.localRotation = Quaternion.identity;
        enemy.hp.parent.gameObject.SetActive(true);
        enemy.body.color = Color.white;
        if (enemy.animator) enemy.animator.enabled = true;
        if (enemy.kind == 1) enemy.root.transform.position += Vector3.up * 1.55f;
        enemies.Add(enemy);
        Burst(enemy.root.transform.position + Vector3.up * .3f, new Color(1, .38f, .69f), 10, 1.3f);
    }

    void RingTheBell() {
        if (State != Phase.Battle || Paused || bellUsed || dormantEnemies.Count == 0) return;
        bellUsed = true;
        encounterButton.interactable = false;
        Play(bellSound, .8f);
        SweepBellAlongBridge();
        foreach (var entry in dormantEnemies) {
            if (entry.eyes) Destroy(entry.eyes.gameObject);
            Burst(entry.enemy.root.transform.position + Vector3.up * .22f, BellGold, 8, 1.1f);
            DefeatEnemy(entry.enemy);
        }
        dormantEnemies.Clear();
        ShowTip("Tiếng chuông đã phá lời nguyền trên những con quái đang nằm im.");
    }

    AudioClip CreateBellSound() {
        const int rate = 22050;
        var samples = new float[rate * 2];
        for (int i = 0; i < samples.Length; i++) {
            float t = i / (float)rate;
            float phase = 2 * Mathf.PI * t;
            float tone = .53f * Mathf.Sin(392 * phase) + .22f * Mathf.Sin(794 * phase) + .18f * Mathf.Sin(1157 * phase);
            samples[i] = tone * Mathf.Min(1, t * 230) * Mathf.Exp(-2.5f * t) * .65f;
        }
        var clip = AudioClip.Create("Watchtower bell", samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    void SweepBellAlongBridge() {
        for (int i = 0; i < 3; i++) {
            var position = new Vector3(Gate - i * .65f, Ground + .035f + i * .05f, 0);
            var size = new Vector3(1.55f - i * .35f, .026f, 1);
            var streak = Shape("Bell reflection on the bridge", world, position, size, BellGold, 3, false);
            effects.Add(new Visual {
                root = streak.transform, sprite = streak, scale = size,
                velocity = Vector3.left * (23f - i * 2f), life = .65f
            });
        }
    }

    void SpawnMirrorBearer() {
        mirrorBearer = Spawn(6);
        mirrorBearer.root.name = "Rune Mirror Thief";
        mirrorBearer.health = mirrorBearer.maxHealth = 220;
        mirrorBearer.speed = .36f;
        mirrorBearer.root.transform.position = new Vector3(-7.8f, Ground, 0);
        mirrorFrame = new GameObject("Rune mirror").transform;
        mirrorFrame.SetParent(mirrorBearer.root.transform, false);
        mirrorFrame.localPosition = new Vector3(.64f, 1.13f, 0);
        Shape("Mirror frame", mirrorFrame, Vector3.zero, new Vector3(.8f, 1.02f, 1), MirrorViolet, 9);
        mirrorGlass = Shape("Mirror glass", mirrorFrame, Vector3.zero, new Vector3(.66f, .87f, 1), new Color(.15f, .11f, .29f), 10);
        var gleam = Shape("Glass reflection", mirrorFrame, new Vector3(-.16f, .18f, 0),
            new Vector3(.07f, .32f, 1), new Color(.81f, .8f, 1, .65f), 11);
        gleam.transform.localRotation = Quaternion.Euler(0, 0, -27);
    }

    void CaptureMirrorRune(int spell) {
        if (mirrorBearer == null || !mirrorBearer.alive || mirrorUsed) return;
        mirrorUsed = true;
        mirrorSpell = spell;
        mirrorDeadline = elapsed + MirrorDelay;
        mirrorBearer.health = Mathf.Min(mirrorBearer.health, 60);
        mirrorGlass.color = spellColors[spell] * .7f + new Color(.08f, .06f, .11f, .3f);
        DrawMirrorRune(spell);
        encounterButton.gameObject.SetActive(true);
        TickMirrorEncounter();
        Ring(mirrorFrame.position, 1.05f, spellColors[spell]);
        Play(runeSound, .55f);
    }

    void DrawMirrorRune(int spell) {
        var glyph = new GameObject("Copied rune", typeof(LineRenderer));
        glyph.layer = 29;
        glyph.transform.SetParent(mirrorFrame, false);
        var line = glyph.GetComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.sharedMaterial = spriteMaterial;
        line.startWidth = line.endWidth = .044f;
        line.startColor = line.endColor = Color.white;
        line.sortingOrder = 13;
        if (spell == 1) {
            line.positionCount = 24;
            line.loop = true;
            for (int i = 0; i < line.positionCount; i++) {
                float angle = i * 2 * Mathf.PI / line.positionCount;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * .19f, Mathf.Sin(angle) * .19f, 0));
            }
        } else {
            Vector3[] points = spell == 0
                ? new[] { new Vector3(0, .24f), new Vector3(-.22f, -.19f), new Vector3(.22f, -.19f), new Vector3(0, .24f) }
                : new[] { new Vector3(.13f, .25f), new Vector3(-.14f, .03f), new Vector3(.12f, .03f), new Vector3(-.12f, -.24f) };
            line.positionCount = points.Length;
            line.SetPositions(points);
        }
    }

    void FocusMirror() {
        if (State != Phase.Battle || Paused || mirrorSpell < 0) return;
        mirrorFocused = true;
        Ring(mirrorFrame.position, .85f, MirrorViolet);
    }

    void TickMirrorEncounter() {
        if (mirrorSpell < 0) return;
        float remaining = mirrorDeadline - elapsed;
        if (remaining <= 0) {
            FireCopiedRune();
            return;
        }
        mirrorFrame.localScale = Vector3.one * (1 + .07f * Mathf.Sin(elapsed * (remaining < 2 ? 22 : 9)));
        encounterStatus.text = $"{RuneNames[mirrorSpell]} đã bị sao chép   •   Phá gương trong {Mathf.CeilToInt(remaining)} giây";
    }

    void FireCopiedRune() {
        int spell = mirrorSpell;
        Vector3 origin = mirrorFrame.position;
        ResolveMirror("Gương đã bắn bản sao vào cổng!");
        int damage = spell == 0 ? 85 : spell == 1 ? 65 : 100;
        Health = Mathf.Max(0, Health - damage * (1 - gateGuard));
        var impact = new Vector3(Gate, Ground + .9f, 0);
        MirrorTrace(origin, impact, spellColors[spell]);
        if (spell == 2) Lightning(impact);
        Ring(impact, 1.8f, spellColors[spell]);
        Burst(impact, spellColors[spell], 22, 1.6f);
        Floating("CỔNG -" + damage, impact + Vector3.up, spellColors[spell]);
        Play(gateSound, .55f);
        shake = .5f;
    }

    void MirrorTrace(Vector3 origin, Vector3 impact, Color color) {
        var beam = new GameObject("Copied rune strikes the gate", typeof(LineRenderer));
        beam.layer = 29;
        beam.transform.SetParent(world, false);
        var line = beam.GetComponent<LineRenderer>();
        line.sharedMaterial = spriteMaterial;
        line.startColor = color;
        line.endColor = Color.white;
        line.startWidth = .05f;
        line.endWidth = .16f;
        line.sortingOrder = 17;
        line.positionCount = 3;
        line.SetPosition(0, origin);
        line.SetPosition(1, Vector3.Lerp(origin, impact, .55f) + Vector3.up * .38f);
        line.SetPosition(2, impact);
        Destroy(beam, .2f);
    }

    void ShatterMirror(Vector3 impact) {
        int spell = mirrorSpell;
        ResolveMirror("Gương vỡ! Rune dội ngược vào đội quân bóng tối.");
        if (spell == 2) Lightning(impact);
        Ring(impact, 3.2f, spellColors[spell]);
        Burst(impact, spellColors[spell], 30, 1.75f);
        Floating("PHẢN RUNE!", impact + Vector3.up, spellColors[spell]);
        Play(runeSound, .7f);
        foreach (var enemy in enemies.ToArray()) {
            if (Mathf.Abs(enemy.root.transform.position.x - impact.x) > 3.2f) continue;
            if (spell == 0) { enemy.burnUntil = elapsed + 3; enemy.burnTick = 0; }
            if (spell == 1) enemy.frozenUntil = Mathf.Max(enemy.frozenUntil, elapsed + 4);
            Damage(enemy, spell == 0 ? 45 : spell == 1 ? 20 : 100, spellColors[spell]);
        }
    }

    void ResolveMirror(string message) {
        mirrorSpell = -1;
        mirrorFocused = false;
        if (mirrorFrame) {
            mirrorFrame.gameObject.SetActive(false);
            Destroy(mirrorFrame.gameObject);
            mirrorFrame = null;
        }
        encounterButton.gameObject.SetActive(false);
        encounterStatus.text = message;
    }

    void TickWaveEncounter() {
        if (!encounterCard) return;
        if (Wave == 2) TickBellEncounter();
        else if (Wave == 4) TickMirrorEncounter();
    }

    void ClearWaveEncounter() {
        foreach (var entry in dormantEnemies) if (entry.enemy.root) Destroy(entry.enemy.root);
        dormantEnemies.Clear();
        if (encounterCard) {
            encounterCard.SetActive(false);
            Destroy(encounterCard);
        }
        if (mirrorFrame) Destroy(mirrorFrame.gameObject);
        encounterCard = null;
        mirrorBearer = null;
        mirrorSpell = -1;
        mirrorFocused = mirrorUsed = bellUsed = false;
    }
}
}
