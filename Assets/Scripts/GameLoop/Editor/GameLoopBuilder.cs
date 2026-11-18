using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Menu: Little Bloom > Build Game Loop
// Adds the game loop to ver2_scene, using the objects that already exist:
// - "GameLoop" object with GameLoop, DayCycle, ScoreBoard, FlowerStock, EncyclopediaPage
// - text in the notebook (orders), on the board (score) and on the encyclopedia
// - TouchButtons on the notebook, the desk bell and the OPEN/CLOSED sign
// - a flower type, color and meaning for each vase (+ a label above the vase)
// - the last 2 vases are locked (empty) until they are unlocked
// - a few example orders
// Safe to run again: it removes what it built before and builds it again.
public static class GameLoopBuilder
{
    const string MaterialFolder = "Assets/Materials/GameLoop";

    class FlowerType
    {
        public string name;
        public string color;
        public string meaning;
        public Color tint;
        public FlowerType(string n, string c, string m, Color t) { name = n; color = c; meaning = m; tint = t; }
    }

    // Vase1 ... Vase10. The last 2 are locked at the start (unlocked in this order).
    static FlowerType[] vaseFlowers =
    {
        new FlowerType("Rose", "Red", "romantic love", new Color(0.85f, 0.1f, 0.15f)),
        new FlowerType("Rose", "Pink", "gratitude, admiration", new Color(1f, 0.6f, 0.75f)),
        new FlowerType("Rose", "Yellow", "friendship, joy", new Color(1f, 0.88f, 0.2f)),
        new FlowerType("Hyacinth", "Purple", "\"I'm sorry\"", new Color(0.45f, 0.2f, 0.7f)),
        new FlowerType("Lily", "White", "sympathy", new Color(0.97f, 0.97f, 0.95f)),
        new FlowerType("Sunflower", "Yellow", "happiness, loyalty", new Color(1f, 0.7f, 0f)),
        new FlowerType("Lavender", "Purple", "calm, devotion", new Color(0.7f, 0.6f, 0.95f)),
        new FlowerType("Daisy", "White", "innocence, new beginnings", new Color(1f, 1f, 0.85f)),
        new FlowerType("Tulip", "Red", "declaration of love", new Color(0.95f, 0.3f, 0.2f)),
        new FlowerType("Carnation", "Pink", "a mother's love", new Color(0.95f, 0.45f, 0.6f)),
    };
    const int LockedVaseCount = 2;

    [MenuItem("Little Bloom/Build Game Loop")]
    public static void Build()
    {
        // ---------- 1. find the shop parts ----------
        WrappingStation station = Object.FindFirstObjectByType<WrappingStation>();
        GameObject notepad = GameObject.Find("Notepad");
        GameObject notepadPages = GameObject.Find("NotepadPages");
        GameObject bell = GameObject.Find("Bell");
        GameObject bellButton = GameObject.Find("BellButton");
        GameObject mainDesk = GameObject.Find("MainDesk");
        GameObject boardPanel = GameObject.Find("BoardPanel");
        GameObject book = GameObject.Find("EncyclopediaBook");
        GameObject sign = GameObject.Find("OpenClosedSign");
        TrashBin trashBin = Object.FindFirstObjectByType<TrashBin>();
        Light sun = FindSun();

        string missing = "";
        if (station == null) missing += "WrappingStation (WrappingTable prefab)\n";
        if (notepad == null || notepadPages == null) missing += "Notepad / NotepadPages\n";
        if (bell == null) missing += "Bell\n";
        if (mainDesk == null) missing += "MainDesk\n";
        if (missing != "")
        {
            EditorUtility.DisplayDialog("Little Bloom", "Open ver2_scene first. Could not find:\n\n" + missing, "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Build Game Loop");
        int undoGroup = Undo.GetCurrentGroup();

        RemoveOldBuild();

        Vector3 roomCenter = RoomCenter();

        // ---------- 2. the GameLoop object ----------
        GameObject loopObject = new GameObject("GameLoop");
        Undo.RegisterCreatedObjectUndo(loopObject, "Create GameLoop");

        GameLoop loop = loopObject.AddComponent<GameLoop>();
        DayCycle dayCycle = loopObject.AddComponent<DayCycle>();
        ScoreBoard scoreBoard = loopObject.AddComponent<ScoreBoard>();
        FlowerStock stock = loopObject.AddComponent<FlowerStock>();
        EncyclopediaPage encyclopedia = loopObject.AddComponent<EncyclopediaPage>();

        dayCycle.sun = sun;
        loop.station = station;
        loop.dayCycle = dayCycle;
        loop.scoreBoard = scoreBoard;
        loop.flowerStock = stock;
        loop.encyclopedia = encyclopedia;
        loop.trashBin = trashBin;
        loop.notebook = notepad.transform;

        // ---------- 3. notebook (orders) ----------
        loop.notebookText = CreateFlatText("LB_NotebookText", notepadPages, roomCenter, 0.03f, 0.18f);
        loop.notebookText.text = "Notebook";
        TouchButton notebookButton = AddTouchButton(notepad);
        Wire(notebookButton, loop.OnNotebookPressed);

        // ---------- 4. desk bell + delivery area ----------
        TouchButton bellTouch = AddTouchButton(bell);
        if (bellButton != null) bellTouch.pressPart = bellButton.transform;
        Wire(bellTouch, loop.OnBellPressed);

        Bounds desk = WorldBounds(mainDesk);
        Vector3 areaCenter = new Vector3(desk.center.x, desk.max.y + 0.25f, desk.center.z);
        loop.deliveryArea = new Bounds(areaCenter, new Vector3(desk.size.x + 0.1f, 0.6f, desk.size.z + 0.1f));

        // ---------- 5. OPEN / CLOSED sign ----------
        if (sign != null)
        {
            TouchButton signTouch = AddTouchButton(sign);
            ShopSign shopSign = sign.GetComponent<ShopSign>();
            if (shopSign == null) shopSign = Undo.AddComponent<ShopSign>(sign);
            loop.sign = shopSign;
            Wire(signTouch, loop.OnSignPressed);
        }

        // ---------- 6. score board ----------
        if (boardPanel != null) BuildBoard(boardPanel, scoreBoard, roomCenter);

        // ---------- 7. encyclopedia ----------
        if (book != null)
        {
            encyclopedia.pageText = CreateFlatText("LB_EncyclopediaText", book, roomCenter, 0.03f, 0.14f);
        }

        // ---------- 8. flowers in vases + locked vases ----------
        scoreBoard.lockedFlowerGroups = SetUpVases(roomCenter);

        // ---------- 9. example orders ----------
        loop.orders = ExampleOrders();
        loop.ordersPerDay = 3;

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(loopObject.scene);
        Selection.activeGameObject = loopObject;

        string report = "Game loop built.\n\nSave the scene (Ctrl+S), then press Play and flip the sign on the door to OPEN.";
        if (sign == null) report += "\n\nNo OpenClosedSign found: the day can't start. Add the sign prefab and build again.";
        if (boardPanel == null) report += "\n\nNo BoardPanel found: the score won't be shown.";
        if (sun == null) report += "\n\nNo Directional Light found: the sun won't move.";
        Debug.Log(report);
    }

    // ================= removing the old build =================

    static void RemoveOldBuild()
    {
        GameObject oldLoop = GameObject.Find("GameLoop");
        if (oldLoop != null) Undo.DestroyObjectImmediate(oldLoop);

        Transform[] all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        // locked vase groups: put the flowers back first, then remove the group
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null || !all[i].name.StartsWith("LB_Locked")) continue;

            Transform group = all[i];
            List<Transform> children = new List<Transform>();
            for (int c = 0; c < group.childCount; c++) children.Add(group.GetChild(c));

            for (int c = 0; c < children.Count; c++)
            {
                if (children[c].name.StartsWith("LB_")) continue;
                Undo.SetTransformParent(children[c], group.parent, "Unlock flowers");
            }
            Undo.DestroyObjectImmediate(group.gameObject);
        }

        // every other object made by this builder
        all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].name.StartsWith("LB_")) Undo.DestroyObjectImmediate(all[i].gameObject);
        }
    }

    // ================= touch buttons =================

    static TouchButton AddTouchButton(GameObject target)
    {
        if (target.GetComponent<XRSimpleInteractable>() == null) Undo.AddComponent<XRSimpleInteractable>(target);
        TouchButton button = target.GetComponent<TouchButton>();
        if (button == null) button = Undo.AddComponent<TouchButton>(target);

        // it needs a collider (on itself or a child) to be touched
        if (target.GetComponentInChildren<Collider>() == null)
        {
            BoxCollider box = Undo.AddComponent<BoxCollider>(target);
            Bounds local = LocalBounds(target.transform);
            box.center = local.center;
            box.size = local.size;
        }
        return button;
    }

    static void Wire(TouchButton button, UnityAction action)
    {
        Undo.RecordObject(button, "Wire TouchButton");
        while (button.onPressed.GetPersistentEventCount() > 0)
        {
            UnityEventTools.RemovePersistentListener(button.onPressed, 0);
        }
        UnityEventTools.AddPersistentListener(button.onPressed, action);
        PrefabUtility.RecordPrefabInstancePropertyModifications(button);
    }

    // ================= text =================

    // Text lying flat on top of an object (notebook, book), readable from the player's side.
    static TextMeshPro CreateFlatText(string name, GameObject surface, Vector3 roomCenter, float minSize, float maxSize)
    {
        Bounds b = WorldBounds(surface);
        Vector3 toPlayer = Horizontal(roomCenter - b.center);
        Vector3 textUp = -toPlayer;   // top of the text points away from the player

        float height = Mathf.Abs(textUp.x) * b.size.x + Mathf.Abs(textUp.z) * b.size.z;
        float width = Mathf.Abs(textUp.z) * b.size.x + Mathf.Abs(textUp.x) * b.size.z;

        GameObject textObject = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(textObject, "Create text");
        textObject.transform.position = new Vector3(b.center.x, b.max.y + 0.002f, b.center.z);
        textObject.transform.rotation = Quaternion.LookRotation(Vector3.down, textUp);
        Undo.SetTransformParent(textObject.transform, surface.transform, "Parent text");

        TextMeshPro text = textObject.AddComponent<TextMeshPro>();
        text.rectTransform.sizeDelta = new Vector2(width * 0.88f, height * 0.88f);
        SetUpText(text, minSize, maxSize, new Color(0.15f, 0.12f, 0.12f));
        text.alignment = TextAlignmentOptions.TopLeft;
        return text;
    }

    static void SetUpText(TextMeshPro text, float minSize, float maxSize, Color color)
    {
        text.enableAutoSizing = true;
        text.fontSizeMin = minSize;
        text.fontSizeMax = maxSize;
        text.fontSize = maxSize;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.richText = true;
    }

    // ================= score board =================

    static void BuildBoard(GameObject boardPanel, ScoreBoard scoreBoard, Vector3 roomCenter)
    {
        Bounds b = WorldBounds(boardPanel);
        Vector3 toRoom = Horizontal(roomCenter - b.center);
        Vector3 forward = -toRoom;   // the text looks into the wall, so the player reads it
        Vector3 right = Vector3.Cross(Vector3.up, forward);

        float width = Mathf.Abs(right.x) * b.size.x + Mathf.Abs(right.z) * b.size.z;
        float depth = Mathf.Abs(toRoom.x) * b.size.x + Mathf.Abs(toRoom.z) * b.size.z;
        float height = b.size.y;
        Vector3 front = b.center + toRoom * (depth / 2f + 0.004f);
        Quaternion facing = Quaternion.LookRotation(forward, Vector3.up);

        // text in the top part of the board
        GameObject textObject = new GameObject("LB_BoardText");
        Undo.RegisterCreatedObjectUndo(textObject, "Create board text");
        textObject.transform.SetPositionAndRotation(front + Vector3.up * height * 0.1f, facing);
        Undo.SetTransformParent(textObject.transform, boardPanel.transform, "Parent board text");

        TextMeshPro text = textObject.AddComponent<TextMeshPro>();
        text.rectTransform.sizeDelta = new Vector2(width * 0.85f, height * 0.7f);
        SetUpText(text, 0.05f, 0.5f, new Color(0.15f, 0.12f, 0.12f));
        text.alignment = TextAlignmentOptions.Center;
        scoreBoard.boardText = text;

        // progress bar at the bottom
        GameObject bar = new GameObject("LB_ProgressBar");
        Undo.RegisterCreatedObjectUndo(bar, "Create progress bar");
        bar.transform.SetPositionAndRotation(front - Vector3.up * height * 0.36f, facing);
        Undo.SetTransformParent(bar.transform, boardPanel.transform, "Parent bar");

        float barWidth = width * 0.8f;
        float barHeight = Mathf.Max(height * 0.07f, 0.02f);

        MakeBox("LB_BarBack", bar.transform, new Vector3(barWidth, barHeight, 0.004f), 0f,
                                  GetMaterial("LB_BarBack", new Color(0.35f, 0.3f, 0.3f)));
        GameObject fill = MakeBox("LB_BarFill", bar.transform, new Vector3(barWidth, barHeight * 0.8f, 0.004f), -0.003f,
                                  GetMaterial("LB_BarFill", new Color(0.95f, 0.45f, 0.65f)));
        scoreBoard.barFill = fill.transform;
    }

    static GameObject MakeBox(string name, Transform parent, Vector3 size, float z, Material material)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(box, "Create box");
        box.name = name;
        Object.DestroyImmediate(box.GetComponent<Collider>());
        box.transform.SetParent(parent, false);
        box.transform.localPosition = new Vector3(0f, 0f, z);
        box.transform.localScale = size;
        box.GetComponent<Renderer>().sharedMaterial = material;
        return box;
    }

    // ================= vases =================

    static GameObject[] SetUpVases(Vector3 roomCenter)
    {
        List<GameObject> lockedGroups = new List<GameObject>();
        FlowerItem[] flowers = Object.FindObjectsByType<FlowerItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        // where each vase is
        GameObject[] vases = new GameObject[vaseFlowers.Length];
        Bounds[] vaseBounds = new Bounds[vaseFlowers.Length];
        for (int v = 0; v < vases.Length; v++)
        {
            vases[v] = GameObject.Find("Vase" + (v + 1));
            if (vases[v] != null) vaseBounds[v] = WorldBounds(vases[v]);
        }

        // each flower belongs to the closest vase (if it is less than 40 cm away)
        List<FlowerItem>[] inVases = new List<FlowerItem>[vases.Length];
        for (int v = 0; v < vases.Length; v++) inVases[v] = new List<FlowerItem>();
        for (int f = 0; f < flowers.Length; f++)
        {
            int closest = -1;
            float closestDistance = 0.4f;
            for (int v = 0; v < vases.Length; v++)
            {
                if (vases[v] == null) continue;
                float distance = Horizontal(flowers[f].transform.position - vaseBounds[v].center, false).magnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = v;
                }
            }
            if (closest >= 0) inVases[closest].Add(flowers[f]);
        }

        for (int v = 0; v < vaseFlowers.Length; v++)
        {
            List<FlowerItem> inVase = inVases[v];
            if (vases[v] == null || inVase.Count == 0) continue;

            FlowerType type = vaseFlowers[v];
            Bounds vaseBox = vaseBounds[v];
            Material headMaterial = null;

            float flowerTop = vaseBox.max.y;
            for (int f = 0; f < inVase.Count; f++)
            {
                FlowerItem flower = inVase[f];
                Undo.RecordObject(flower, "Flower type");
                flower.flowerName = type.name;
                flower.colorName = type.color;
                flower.meaning = type.meaning;
                PrefabUtility.RecordPrefabInstancePropertyModifications(flower);

                // greybox color on the flower head
                Renderer[] parts = flower.GetComponentsInChildren<Renderer>(true);
                for (int r = 0; r < parts.Length; r++)
                {
                    if (parts[r].name != "Head") continue;
                    if (headMaterial == null) headMaterial = GetHeadMaterial(type, parts[r].sharedMaterial);
                    Undo.RecordObject(parts[r], "Flower color");
                    parts[r].sharedMaterial = headMaterial;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(parts[r]);
                }

                Bounds fb = WorldBounds(flower.gameObject);
                if (fb.max.y > flowerTop) flowerTop = fb.max.y;
            }

            // locked vase: its flowers go in a hidden group
            Transform labelParent = null;
            bool locked = v >= vaseFlowers.Length - LockedVaseCount;
            if (locked)
            {
                GameObject group = new GameObject("LB_LockedVase" + (v + 1));
                Undo.RegisterCreatedObjectUndo(group, "Locked vase");
                Transform firstRoot = GroupRoot(inVase[0].gameObject).transform;
                Undo.SetTransformParent(group.transform, firstRoot.parent, "Locked vase");
                group.transform.position = vaseBox.center;

                for (int f = 0; f < inVase.Count; f++)
                {
                    Transform root = GroupRoot(inVase[f].gameObject).transform;
                    if (root.parent != group.transform) Undo.SetTransformParent(root, group.transform, "Locked vase");
                }
                labelParent = group.transform;
                lockedGroups.Add(group);
            }

            // label above the vase
            Vector3 labelPosition = new Vector3(vaseBox.center.x, flowerTop + 0.07f, vaseBox.center.z);
            CreateLabel("LB_Label_Vase" + (v + 1), labelPosition, roomCenter,
                        type.color + " " + type.name + "\n<size=60%>" + type.meaning + "</size>", labelParent);
        }

        // hide the locked groups (they are shown again when unlocked)
        for (int i = 0; i < lockedGroups.Count; i++)
        {
            Undo.RecordObject(lockedGroups[i], "Lock vase");
            lockedGroups[i].SetActive(false);
        }
        return lockedGroups.ToArray();
    }

    // The object to move into a locked group: the whole prefab instance (Flower or FlowerPair).
    static GameObject GroupRoot(GameObject flower)
    {
        GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(flower);
        if (root != null) return root;
        return flower;
    }

    static void CreateLabel(string name, Vector3 position, Vector3 roomCenter, string content, Transform parent)
    {
        GameObject label = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(label, "Create label");
        Vector3 away = Horizontal(position - roomCenter);   // look away from the room, so the room can read it
        label.transform.SetPositionAndRotation(position, Quaternion.LookRotation(away, Vector3.up));
        if (parent != null) Undo.SetTransformParent(label.transform, parent, "Parent label");

        TextMeshPro text = label.AddComponent<TextMeshPro>();
        text.rectTransform.sizeDelta = new Vector2(0.3f, 0.1f);
        SetUpText(text, 0.05f, 0.35f, new Color(0.2f, 0.15f, 0.15f));
        text.alignment = TextAlignmentOptions.Bottom;
        text.text = content;
    }

    // ================= example orders =================

    static List<OrderData> ExampleOrders()
    {
        List<OrderData> list = new List<OrderData>();
        list.Add(MakeOrder("Mrs. Ionescu",
            "Tomorrow is our 30th wedding anniversary. I want my husband to know I still love him like on our first day.",
            new string[] { "Rose/Red" }, 0,
            "Oh, he will understand right away. Thank you!", "Hmm, these don't really say 'I love you'..."));
        list.Add(MakeOrder("Andrei",
            "My teacher helped me so much this year. I want to thank her properly.",
            new string[] { "Rose/Pink" }, 0,
            "She'll know exactly what I mean. Thanks!", "I'm not sure this says 'thank you'..."));
        list.Add(MakeOrder("Maria",
            "I said something hurtful to my best friend. I need to tell her I'm sorry, and that our friendship matters.",
            new string[] { "Hyacinth/Purple", "Rose/Yellow" }, 0,
            "I think she'll forgive me now.", "This doesn't feel like an apology..."));
        list.Add(MakeOrder("Elena",
            "My grandmother passed away last week. I need flowers for the funeral.",
            new string[] { "Lily/White" }, 0,
            "They are beautiful and calm. Thank you.", "These feel too cheerful for a funeral..."));
        list.Add(MakeOrder("Radu",
            "My best friend just got her dream job! Let's celebrate her success.",
            new string[] { "Sunflower/Yellow", "Rose/Yellow" }, 0,
            "So bright and happy, she'll love it!", "Hmm, this doesn't feel like a celebration."));
        list.Add(MakeOrder("Ioana",
            "My sister has her final exams next week and she's really stressed. Something to calm her down.",
            new string[] { "Lavender/Purple", "Rose/Yellow" }, 0,
            "Perfect, this will help her relax.", "I'm not sure this will calm her..."));
        list.Add(MakeOrder("Mihai",
            "My sister just had a baby girl! Something sweet and innocent for the new mom.",
            new string[] { "Daisy/White" }, 0,
            "So sweet, just like the baby!", "Hmm, not quite right for a newborn."));
        list.Add(MakeOrder("Vlad",
            "I have a first date tonight. I want to tell her how I feel, without saying it.",
            new string[] { "Tulip/Red", "Rose/Red" }, 1,
            "Wish me luck! Thank you!", "I don't think this says what I feel..."));
        list.Add(MakeOrder("Cristina",
            "It's Mother's Day on Sunday. Something that shows how much I love my mom.",
            new string[] { "Carnation/Pink", "Rose/Pink" }, 2,
            "Mom is going to cry, thank you!", "This doesn't really feel like it's for my mom..."));
        return list;
    }

    static OrderData MakeOrder(string client, string request, string[] good, int requiredUnlocks, string happy, string sad)
    {
        OrderData order = new OrderData();
        order.clientName = client;
        order.request = request;
        order.goodFlowers = good;
        order.requiredUnlocks = requiredUnlocks;
        order.happyReply = happy;
        order.sadReply = sad;
        return order;
    }

    // ================= helpers =================

    static Light FindSun()
    {
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i].type == LightType.Directional) return lights[i];
        }
        return null;
    }

    static Vector3 RoomCenter()
    {
        GameObject room = GameObject.Find("Room");
        if (room != null) return WorldBounds(room).center;
        GameObject shop = GameObject.Find("Shop");
        if (shop != null) return shop.transform.position;
        return Vector3.zero;
    }

    static Vector3 Horizontal(Vector3 v)
    {
        return Horizontal(v, true);
    }

    static Vector3 Horizontal(Vector3 v, bool normalize)
    {
        v.y = 0f;
        if (!normalize) return v;
        if (v.sqrMagnitude < 0.0001f) return Vector3.forward;
        return v.normalized;
    }

    static Bounds WorldBounds(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        bool found = false;
        Bounds bounds = new Bounds(target.transform.position, Vector3.zero);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] is ParticleSystemRenderer) continue;
            if (renderers[i].GetComponent<TMP_Text>() != null) continue;
            if (!found) { bounds = renderers[i].bounds; found = true; }
            else bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds;
    }

    // Bounds of the renderers, in the local space of root (for a fitted BoxCollider).
    static Bounds LocalBounds(Transform root)
    {
        Bounds world = WorldBounds(root.gameObject);
        Bounds local = new Bounds(root.InverseTransformPoint(world.center), Vector3.zero);
        Vector3 e = world.extents;
        for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                    local.Encapsulate(root.InverseTransformPoint(world.center + new Vector3(e.x * x, e.y * y, e.z * z)));
        return local;
    }

    // ================= materials =================

    static Material GetHeadMaterial(FlowerType type, Material original)
    {
        return GetMaterial("LB_Head_" + type.name + "_" + type.color, type.tint, original);
    }

    static Material GetMaterial(string name, Color color)
    {
        return GetMaterial(name, color, null);
    }

    static Material GetMaterial(string name, Color color, Material copyFrom)
    {
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
        {
            AssetDatabase.CreateFolder("Assets/Materials", "GameLoop");
        }

        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            if (copyFrom != null) material = new Material(copyFrom);
            else material = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }
}
