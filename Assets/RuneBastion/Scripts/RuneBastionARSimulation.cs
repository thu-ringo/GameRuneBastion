using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RuneBastion {
public sealed partial class RuneBastionGame {
    const int SimulationLayer = 27;
    const float DeskY = .75f;
    const float BoardWidth = .94f;
    const float BoardHeight = BoardWidth * 9f / 16f;
    static readonly Vector3 DeskCenter = new Vector3(0, DeskY, 0);
    readonly Dictionary<Color, Material> simulationMaterials = new Dictionary<Color, Material>();
    GameObject simulationGuide, simulationBadge;
    bool arSimulated;
    float simulationYaw, simulationPitch, simulationDistance;

    void BeginArSimulation() {
        if (overlay) Destroy(overlay);
        hudRoot.gameObject.SetActive(false);
        arMode = arWaiting = arSimulated = true;
        CreateArTexture();

        arRoot = new GameObject("AR Simulation Room");
        arCamera = new GameObject("Simulated AR Camera", typeof(Camera)).GetComponent<Camera>();
        arCamera.transform.SetParent(arRoot.transform, false);
        arCamera.nearClipPlane = .025f;
        arCamera.farClipPlane = 25;
        arCamera.fieldOfView = 42;
        arCamera.depth = 5;
        arCamera.cullingMask = (1 << SimulationLayer) | (1 << 28);
        arCamera.clearFlags = CameraClearFlags.SolidColor;
        arCamera.backgroundColor = new Color(.045f, .06f, .105f);

        simulationYaw = -8;
        simulationPitch = 66;
        simulationDistance = 1.23f;
        SetSimulationCameraPose();
        BuildSimulationRoom();
        simulationGuide = CreatePlacementGuide();
        simulationGuide.transform.position = DeskCenter;

        var hint = RoundPanel("AR Simulation Placement", uiRoot, 360, 115, 880, 132, new Color(.025f, .05f, .11f, .96f));
        hint.raycastTarget = false;
        overlay = hint.gameObject;
        Heading(hint.transform, "SÂN KHẤU AR MÔ PHỎNG", 24, 9, 832, 40, 29, new Color(.51f, .9f, 1f));
        Label("PlacementInstructions", hint.transform, "Bấm mặt bàn để đặt thành. Giữ chuột phải để xoay, cuộn để thu phóng.",
            28, 53, 824, 34, 20, Color.white, TextAnchor.MiddleCenter);
        Button("CancelSimulation", hint.transform, "VỀ MENU", 335, 91, 210, 34,
            new Color(.14f, .23f, .35f), ShowMenu, 16);
    }

    void BuildSimulationRoom() {
        var oak = new Color(.39f, .23f, .16f);
        var wood = new Color(.24f, .13f, .1f);
        var metal = new Color(.13f, .18f, .24f);
        RoomBox("Floor", new Vector3(0, -.045f, 0), new Vector3(8, .09f, 8), new Color(.15f, .19f, .25f));
        RoomBox("Floor rug", new Vector3(0, .008f, 0), new Vector3(2.48f, .016f, 1.92f), new Color(.23f, .3f, .39f));
        RoomBox("Rug inset", new Vector3(0, .018f, 0), new Vector3(2.35f, .005f, 1.79f), new Color(.16f, .23f, .31f));
        RoomBox("North wall", new Vector3(0, 1.4f, 2.15f), new Vector3(8, 2.8f, .1f), new Color(.24f, .32f, .43f));
        RoomBox("Wall trim", new Vector3(0, .12f, 2.085f), new Vector3(8, .18f, .06f), metal);
        RoomBox("Window casing", new Vector3(-.43f, 1.42f, 2.08f), new Vector3(1.35f, 1.02f, .08f), metal);
        RoomBox("Evening window", new Vector3(-.43f, 1.42f, 2.025f), new Vector3(1.25f, .92f, .024f), new Color(.33f, .54f, .72f));
        RoomBox("Window mullion", new Vector3(-.43f, 1.42f, 2f), new Vector3(.025f, .94f, .024f), metal);
        RoomBox("Desk top", new Vector3(0, DeskY - .035f, 0), new Vector3(1.47f, .07f, .92f), oak);
        for (int side = -1; side <= 1; side += 2) {
            RoomBox("Desk front leg", new Vector3(side * .61f, .345f, -.33f), new Vector3(.075f, .69f, .075f), wood);
            RoomBox("Desk rear leg", new Vector3(side * .61f, .345f, .33f), new Vector3(.075f, .69f, .075f), wood);
        }
        for (int i = -3; i <= 3; i++)
            RoomBox("Wood grain", new Vector3(0, DeskY + .0005f, i * .119f), new Vector3(1.45f, .001f, .002f), wood);
        RoomBox("Notebook", new Vector3(-.59f, DeskY + .019f, .315f), new Vector3(.17f, .038f, .18f), new Color(.16f, .36f, .42f));
        RoomBox("Notebook pages", new Vector3(-.59f, DeskY + .025f, .222f), new Vector3(.155f, .025f, .012f), new Color(.74f, .69f, .56f));
        RoomBox("Lantern foot", new Vector3(.61f, DeskY + .012f, .31f), new Vector3(.115f, .024f, .115f), metal);
        RoomBox("Lantern glow", new Vector3(.61f, DeskY + .075f, .31f), new Vector3(.075f, .10f, .075f), new Color(1, .69f, .32f));
        RoomBox("Lantern top", new Vector3(.61f, DeskY + .137f, .31f), new Vector3(.10f, .026f, .10f), metal);
    }

    GameObject RoomBox(string name, Vector3 position, Vector3 size, Color color) {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.layer = SimulationLayer;
        box.transform.SetParent(arRoot.transform, false);
        box.transform.localPosition = position;
        box.transform.localScale = size;
        Destroy(box.GetComponent<Collider>());
        if (!simulationMaterials.TryGetValue(color, out var material)) {
            material = new Material(Resources.Load<Material>("ARBoardMaterial"));
            material.SetColor("_BaseColor", color);
            simulationMaterials.Add(color, material);
        }
        box.GetComponent<Renderer>().sharedMaterial = material;
        return box;
    }

    GameObject CreatePlacementGuide() {
        var guide = new GameObject("Projected board outline", typeof(LineRenderer));
        guide.layer = SimulationLayer;
        guide.transform.SetParent(arRoot.transform, false);
        var line = guide.GetComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 4;
        line.startWidth = line.endWidth = .006f;
        line.sharedMaterial = spriteMaterial;
        line.startColor = line.endColor = new Color(.33f, .93f, 1f);
        float x = BoardWidth / 2, z = BoardHeight / 2;
        line.SetPositions(new[] {
            new Vector3(-x, .007f, -z), new Vector3(x, .007f, -z),
            new Vector3(x, .007f, z), new Vector3(-x, .007f, z)
        });
        return guide;
    }

    void SetSimulationCameraPose() {
        var orbit = Quaternion.Euler(simulationPitch, simulationYaw, 0);
        float distance = simulationDistance * Mathf.Max(1f, 1.58f / arCamera.aspect);
        arCamera.transform.SetPositionAndRotation(DeskCenter + orbit * (Vector3.back * distance), orbit);
    }

    bool TrySimulationPlacement(Vector2 screen, out Vector3 placement) {
        var ray = arCamera.ScreenPointToRay(screen);
        var desk = new Plane(Vector3.up, DeskCenter);
        placement = Vector3.zero;
        if (!desk.Raycast(ray, out float distance)) return false;
        var point = ray.GetPoint(distance);
        if (Mathf.Abs(point.x) > .72f || Mathf.Abs(point.z) > .45f) return false;
        placement = new Vector3(Mathf.Clamp(point.x, -.15f, .15f), DeskY, Mathf.Clamp(point.z, -.145f, .145f));
        return true;
    }

    void TickArSimulation() {
        var mouse = Mouse.current;
        if (mouse == null || Paused || (!arWaiting && overlay)) return;
        if (mouse.rightButton.isPressed) {
            var delta = mouse.delta.ReadValue();
            simulationYaw = Mathf.Clamp(simulationYaw + delta.x * .28f, -38, 38);
            simulationPitch = Mathf.Clamp(simulationPitch - delta.y * .26f, 38, 84);
        }
        simulationDistance = Mathf.Clamp(simulationDistance - mouse.scroll.ReadValue().y * .0006f, .86f, 2.4f);
        SetSimulationCameraPose();
        if (!arWaiting) return;
        bool overDesk = TrySimulationPlacement(mouse.position.ReadValue(), out var placement);
        simulationGuide.SetActive(overDesk);
        if (!overDesk) return;
        simulationGuide.transform.position = placement;
        if (mouse.leftButton.wasPressedThisFrame && !(UnityEngine.EventSystems.EventSystem.current?.IsPointerOverGameObject() ?? false))
            PlaceArBoard(placement, false);
    }

    void ShowSimulationBadge() {
        var badge = RoundPanel("AR Simulation Badge", hudRoot, 1030, 36, 178, 35, new Color(.05f, .12f, .21f, .93f));
        badge.raycastTarget = false;
        Label("Badge", badge.transform, "AR MÔ PHỎNG", 4, 0, 170, 35, 15, new Color(.56f, .9f, 1f), TextAnchor.MiddleCenter);
        simulationBadge = badge.gameObject;
    }

    void ReleaseSimulationRoom() {
        foreach (var material in simulationMaterials.Values) Destroy(material);
        simulationMaterials.Clear();
        if (simulationBadge) Destroy(simulationBadge);
        simulationBadge = simulationGuide = null;
        arSimulated = false;
    }
}
}
