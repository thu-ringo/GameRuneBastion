using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RuneBastion {
public sealed partial class RuneBastionGame {
    enum Rescue { Pending, Choice, Open, Closed, Sheltered, Escorted, Missed }

    const float RescueDuration = 15f;
    const float EscortDuration = 4f;
    static readonly Color RescueGold = new Color(1f, .8f, .39f);
    Rescue rescue;
    float rescueTime, rescueHealth, rescueWalk, distractionUntil;
    bool golemHelped;
    GameObject rescueCard, rescueActors, rescueGate, golemAssist;
    Transform rescueChild, rescueFlower, escortBody, retreatActor, gateGrille;
    Text rescueMessage;
    Button openGateButton, holdGateButton;

    bool RescueInProgress => rescue == Rescue.Choice || rescue == Rescue.Open || rescue == Rescue.Closed;
    bool RescueSucceeded => rescue == Rescue.Sheltered || rescue == Rescue.Escorted;

    void StartRescue() {
        rescue = Rescue.Choice;
        rescueTime = RescueDuration;
        rescueHealth = 100;
        rescueWalk = 0;
        rescueActors = CreateEscort(world, new Vector3(-.9f, Ground, 0), true);
        rescueActors.name = "Golem carries the child";
        for (int i = 0; i < 4; i++) {
            var bat = Spawn(1);
            bat.pursuer = true;
            bat.health = bat.maxHealth = 85;
            bat.speed = 2.2f;
            bat.root.name = "Pursuing bat";
            bat.root.transform.position = new Vector3(-5f - i * .8f, Ground + 1.65f + i % 2 * .36f, 0);
        }

        rescueCard = RoundPanel("RescueEvent", hudRoot, 390, 122, 820, 164, new Color(.045f, .07f, .15f, .96f)).gameObject;
        RoundPanel("RescueAccent", rescueCard.transform, 28, 6, 764, 3, RescueGold).raycastTarget = false;
        Heading(rescueCard.transform, "VỊ KHÁCH NGOÀI CỔNG", 30, 14, 760, 36, 27, RescueGold);
        rescueMessage = Label("RescueStatus", rescueCard.transform, "", 25, 52, 770, 34, 20, Color.white, TextAnchor.MiddleCenter);
        openGateButton = Button("OpenRescueGate", rescueCard.transform, "MỞ CỔNG · CHẶN DƠI", 48, 99, 350, 48,
            new Color(.34f, .23f, .12f), () => ChooseRescue(true), 19);
        holdGateButton = Button("HoldRescueGate", rescueCard.transform, "GIỮ CỔNG · DIỆT DƠI", 422, 99, 350, 48,
            new Color(.13f, .29f, .4f), () => ChooseRescue(false), 19);
        UpdateRescueMessage();
        Play(runeSound, .55f);
    }

    void ChooseRescue(bool open) {
        if (State != Phase.Battle || Paused || rescue != Rescue.Choice) return;
        rescue = open ? Rescue.Open : Rescue.Closed;
        openGateButton.gameObject.SetActive(false);
        holdGateButton.gameObject.SetActive(false);
        if (open) {
            rescueTime = EscortDuration;
            rescueGate = new GameObject("Open gate passage");
            rescueGate.transform.SetParent(world, false);
            rescueGate.transform.position = new Vector3(Gate + .1f, Ground, 0);
            Shape("Warm light from the gate", rescueGate.transform, new Vector3(0, .65f, 0), new Vector3(1.4f, 1.8f, 1),
                new Color(1f, .68f, .24f, .35f), 1);
            gateGrille = new GameObject("Rising ward bars").transform;
            gateGrille.SetParent(rescueGate.transform, false);
            for (int i = -2; i <= 2; i++)
                Shape("Ward bar", gateGrille, new Vector3(i * .14f, .65f, 0), new Vector3(.045f, 1.3f, 1),
                    new Color(1f, .76f, .32f, .8f), 8, false);
            Ring(rescueGate.transform.position, 1.45f, RescueGold);
            Play(gateSound, .45f);
        }
        UpdateRescueMessage();
    }

    void TickRescue(float dt) {
        if (Wave == 3 && !dailyMode && rescue == Rescue.Pending && waveSpawned >= 4) StartRescue();
        if (retreatActor) {
            retreatActor.position += Vector3.left * 3.5f * dt;
            AnimateEscort(dt, true);
            if (retreatActor.position.x < -9.5f) Destroy(retreatActor.gameObject);
        }
        if (!RescueInProgress) return;

        rescueTime -= dt;
        var actor = rescueActors.transform;
        float destination = rescue == Rescue.Open ? Gate + .52f : 1.45f;
        actor.position = Vector3.MoveTowards(actor.position, new Vector3(destination, Ground, 0), (rescue == Rescue.Open ? 2.4f : 1.5f) * dt);
        bool walking = actor.position.x < destination - .04f;
        AnimateEscort(dt, walking);
        if (!walking && rescue != Rescue.Open) {
            // The golem sets the child behind it and raises its arm toward the bats.
            rescueChild.localPosition = Vector3.MoveTowards(rescueChild.localPosition, new Vector3(.93f, .02f, 0), 3.8f * dt);
            escortBody.localRotation = Quaternion.Euler(0, 0, 7);
            if (rescueFlower) {
                rescueFlower.SetParent(world, true);
                rescueFlower.position = actor.position + new Vector3(.85f, .12f, 0);
                Destroy(rescueFlower.gameObject, 5);
                rescueFlower = null;
            }
        } else if (rescue == Rescue.Open) {
            float raised = Mathf.Clamp01(Mathf.Min(EscortDuration - rescueTime, rescueTime) * 2);
            gateGrille.localPosition = Vector3.up * (1.3f * raised);
            rescueChild.localPosition = Vector3.MoveTowards(rescueChild.localPosition, new Vector3(.36f, 1.18f, 0), 4f * dt);
        }

        if (rescue == Rescue.Open && rescueTime <= 0) FinishRescue(Rescue.Sheltered);
        else if (rescueHealth <= 0 || rescueTime <= 0) FinishRescue(Rescue.Missed);
        else if (rescue == Rescue.Closed && !enemies.Any(e => e.pursuer)) FinishRescue(Rescue.Escorted);
        else UpdateRescueMessage();
    }

    void UpdateRescueMessage() {
        int seconds = Mathf.Max(0, Mathf.CeilToInt(rescueTime));
        if (rescue == Rescue.Choice) rescueMessage.text = $"Nó đang bảo vệ đứa trẻ!   •   {seconds} giây";
        else if (rescue == Rescue.Open) rescueMessage.text = $"Cổng đang mở! Dơi lọt vào sẽ gây sát thương.   •   {seconds} giây";
        else rescueMessage.text = $"Hạ đàn dơi   •   Golem {Mathf.CeilToInt(rescueHealth)}%   •   {seconds} giây";
    }

    bool TickPursuer(Enemy bat, float dt, float speedFactor) {
        if (!bat.pursuer || !RescueInProgress) return false;
        bool open = rescue == Rescue.Open;
        float x = open ? Gate + .35f : rescueActors.transform.position.x - .72f;
        var position = bat.root.transform.position;
        position.x = Mathf.MoveTowards(position.x, x, (open ? 4f : bat.speed) * speedFactor * dt);
        bat.root.transform.position = position;
        if (Mathf.Abs(position.x - x) > .03f) return true;
        bat.attackClock -= dt;
        if (bat.attackClock > 0) return true;
        bat.attackClock = 1.15f;
        if (open) {
            Health = Mathf.Max(0, Health - 32 * (1 - gateGuard));
            Floating("CỔNG BỊ TẤN CÔNG!", new Vector3(Gate, .65f, 0), new Color(1, .45f, .4f));
            Play(gateSound, .28f);
            shake = .32f;
            bat.alive = false;
            enemies.Remove(bat);
            Destroy(bat.root);
        } else {
            rescueHealth = Mathf.Max(0, rescueHealth - 8);
            Burst(rescueActors.transform.position + Vector3.up, new Color(.9f, .4f, .85f), 5, .7f);
        }
        return true;
    }

    void FinishRescue(Rescue outcome) {
        rescue = outcome;
        foreach (var bat in enemies.Where(e => e.pursuer).ToArray()) {
            bat.alive = false;
            enemies.Remove(bat);
            Destroy(bat.root);
        }
        DestroyRescueUi();
        if (outcome == Rescue.Sheltered) {
            Burst(rescueActors.transform.position + Vector3.up, RescueGold, 20, 1.5f);
            Destroy(rescueActors);
        } else {
            retreatActor = rescueActors.transform;
            rescueChild.localPosition = new Vector3(.36f, 1.18f, 0);
            retreatActor.localScale = new Vector3(-1, 1, 1);
        }
        rescueActors = null;
        targetX = Gate;
        ShowTip(outcome == Rescue.Missed ? "Golem đưa đứa trẻ rút về phía rừng." :
            outcome == Rescue.Sheltered ? "Cả hai đã vào thành an toàn!" : "Đàn dơi đã tan. Golem đưa đứa trẻ đến nơi an toàn!");
        if (RescueSucceeded) Play(coinSound, .55f);
    }

    void DestroyRescueUi() {
        if (rescueCard) {
            rescueCard.SetActive(false);
            Destroy(rescueCard);
        }
        if (rescueGate) Destroy(rescueGate);
    }

    void ClearRescueActors() {
        DestroyRescueUi();
        if (rescueActors) Destroy(rescueActors);
        if (retreatActor) Destroy(retreatActor.gameObject);
        if (golemAssist) Destroy(golemAssist);
        rescueActors = null;
        retreatActor = null;
    }

    void ResetRescue() {
        rescue = Rescue.Pending;
        golemHelped = false;
        distractionUntil = 0;
    }

    bool DistractNox(Enemy boss) {
        if (boss.kind != 3) return false;
        if (!golemHelped && rescue == Rescue.Escorted && boss.root.transform.position.x >= 3.5f) {
            GolemIntervenes(boss, "GOLEM ĐÁNH LẠC HƯỚNG!");
            distractionUntil = elapsed + 3;
        }
        bool distracted = elapsed < distractionUntil;
        if (rescue == Rescue.Escorted) boss.body.flipX = distracted;
        return distracted;
    }

    bool BlockNoxAttack(Enemy boss) {
        if (boss.kind != 3 || golemHelped || rescue != Rescue.Sheltered) return false;
        GolemIntervenes(boss, "GOLEM CHẶN ĐÒN!");
        return true;
    }

    GameObject CreateEscort(Transform parent, Vector3 position, bool withChild) {
        var actor = new GameObject("Brum the guardian");
        actor.transform.SetParent(parent, false);
        actor.transform.position = position;
        var brum = Render("Brum", Resources.Load<Sprite>("Rescue/Brum"), Vector3.up * .87f, Color.white, 8, actor.transform);
        brum.transform.localScale = Vector3.one * 1.22f;
        if (withChild) {
            var child = Render("Child", Resources.Load<Sprite>("Rescue/Child"), new Vector3(.36f, 1.18f, 0), Color.white, 9, actor.transform);
            child.transform.localScale = Vector3.one * .57f;
            var flower = Render("Dropped flowers", Resources.Load<Sprite>("Rescue/Flowers"), new Vector3(.24f, -.09f, 0), Color.white, 10, child.transform);
            flower.transform.localScale = Vector3.one * .28f;
            rescueChild = child.transform;
            rescueFlower = flower.transform;
            escortBody = brum.transform;
        }
        return actor;
    }

    void AnimateEscort(float dt, bool walking) {
        rescueWalk += dt;
        float bob = walking ? Mathf.Abs(Mathf.Sin(rescueWalk * 13)) * .075f : 0;
        escortBody.localPosition = Vector3.up * (.87f + bob);
        escortBody.localRotation = Quaternion.Euler(0, 0, walking ? Mathf.Sin(rescueWalk * 13) * 5 : 0);
    }

    void AddWeddingGuests(Transform parent) {
        var guests = Node("Rescued wedding guests", parent, 1030, 460, 360, 240);
        WeddingGuest(guests, "Brum", 86, 4, 206, 200);
        WeddingGuest(guests, "Child", 31, 119, 83, 100);
        WeddingGuest(guests, "Flowers", 140, 38, 130, 118);
    }

    void WeddingGuest(Transform parent, string sprite, float x, float y, float w, float h) {
        var image = Panel(sprite, parent, x, y, w, h, Color.white);
        image.sprite = Resources.Load<Sprite>("Rescue/" + sprite);
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

#if UNITY_EDITOR
    void VerifyRescue(System.Action<bool, string> check) {
        StartRun();
        Wave = 3;
        waveSpawned = 4;
        TickRescue(.05f);
        check(rescue == Rescue.Choice && enemies.Count(e => e.pursuer) == 4, "wave three spawns the escort and four pursuers");
        openGateButton.onClick.Invoke();
        float before = Health;
        for (int i = 0; i < 85; i++) {
            elapsed += .05f;
            TickRescue(.05f);
            TickEnemies(.05f);
        }
        check(rescue == Rescue.Sheltered && Health < before, "open gate shelters both while bats can damage the gate");
        Wave = TotalWaves;
        var boss = Spawn(3);
        boss.root.transform.position = new Vector3(Gate, Ground, 0);
        before = Health;
        TickEnemies(.05f);
        check(golemHelped && Health == before, "sheltered golem blocks the first Nox hit");
        TickEnemies(1.25f);
        check(Health < before, "golem does not block the next Nox hit");
        EndRun(true);
        check(overlay.GetComponentsInChildren<Text>().Any(t => t.name == "RescueResult"), "victory confirms the rescue");
        overlay.GetComponentsInChildren<Button>().Single(b => b.name == "ContinueToWedding").onClick.Invoke();
        check(overlay.transform.Find("Rescued wedding guests")?.childCount == 3, "golem, child and bouquet appear at the wedding");

        StartRun();
        Wave = 3;
        waveSpawned = 4;
        TickRescue(.05f);
        holdGateButton.onClick.Invoke();
        check(rescue == Rescue.Closed && PickTarget().pursuer, "closed gate prioritizes pursuers");
        foreach (var bat in enemies.Where(e => e.pursuer).ToArray()) Damage(bat, 1000, Color.white);
        TickRescue(.05f);
        check(rescue == Rescue.Escorted && retreatActor, "defeating the bats lets the escort leave safely");
        Wave = TotalWaves;
        boss = Spawn(3);
        boss.root.transform.position = new Vector3(3.5f, Ground, 0);
        TickEnemies(.05f);
        check(golemHelped && boss.root.transform.position.x == 3.5f, "outside golem distracts Nox");
        elapsed += 3.1f;
        TickEnemies(.05f);
        check(boss.root.transform.position.x > 3.5f, "Nox moves again after the distraction");

        StartRun();
        Wave = 3;
        waveSpawned = 4;
        TickRescue(.05f);
        holdGateButton.onClick.Invoke();
        for (int i = 0; i < 310 && RescueInProgress; i++) {
            elapsed += .05f;
            TickRescue(.05f);
            TickEnemies(.05f);
        }
        check(rescue == Rescue.Missed && retreatActor && !enemies.Any(e => e.pursuer), "failed escort retreats and the battle can continue");
        EndRun(true);
        check(!overlay.GetComponentsInChildren<Text>().Any(t => t.name == "RescueResult"), "failed rescue is omitted from victory");
        overlay.GetComponentsInChildren<Button>().Single(b => b.name == "ContinueToWedding").onClick.Invoke();
        check(!overlay.transform.Find("Rescued wedding guests"), "failed rescue does not add wedding guests");
        StartRun();
        check(rescue == Rescue.Pending && !golemHelped, "starting a new run resets the rescue");
    }
#endif

    void GolemIntervenes(Enemy boss, string message) {
        golemHelped = true;
        Vector3 spot = boss.root.transform.position;
        spot.x += rescue == Rescue.Sheltered ? .9f : -1.05f;
        golemAssist = CreateEscort(world, spot, false);
        Destroy(golemAssist, 3);
        Ring(spot, 1.7f, RescueGold);
        Burst(spot + Vector3.up, RescueGold, 25, 1.65f);
        Floating(message, spot + Vector3.up * 2.5f, RescueGold);
        ShowTip(message);
        Play(runeSound, .6f);
    }
}
}
