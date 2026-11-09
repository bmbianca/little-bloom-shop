using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Menu: Little Bloom > Build Wrapping Sandbox
// Builds everything for wrapping in sandbox_wrapping (graybox, all gray):
// the wall with paper rolls and ribbons, the stats board, the table station.
// Running it again removes the old objects and rebuilds everything.
public static class WrappingSandboxBuilder
{
    const string PrefabsFolder = "Assets/Prefabs/Wrapping";
    const string BaseMaterialPath = "Assets/Graybox_Wrapping.mat";
    const string OutlineMaterialPath = "Assets/Materials/Wrapping/HintOutline.mat";
    const string StationPrefabPath = "Assets/Prefabs/Flowers/BouquetStation.prefab";
    const string FlowerPrefabPath = "Assets/Prefabs/Flowers/Flower.prefab";

    const float SheetSize = 0.4f;          // sheet: 40 x 40 cm, same in hand, on the roll and on the table
    const float SheetThickness = 0.003f;

    // color names (for orders); in graybox each one is a different gray
    static string[] paperNames = { "Pink", "Kraft", "White", "Lilac", "Mint" };
    static string[] ribbonNames = { "Red", "Pink", "Gold", "White", "Green", "Lilac", "Blue", "Peach", "Silver", "Burgundy" };

    [MenuItem("Little Bloom/Build Wrapping Sandbox")]
    public static void Build()
    {
        if (GameObject.Find("WrappingStation") != null)
        {
            bool rebuild = EditorUtility.DisplayDialog("Little Bloom",
                "The scene already has the wrapping station. Build it again?", "Yes", "No");
            if (!rebuild) return;
        }
        Rebuild();
    }

    // Same as the menu, without the dialog.
    public static void Rebuild()
    {
        GameObject shop = GameObject.Find("Shop");
        Material gray = AssetDatabase.LoadAssetAtPath<Material>(BaseMaterialPath);
        Material outline = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
        GameObject stationPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StationPrefabPath);
        GameObject flowerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FlowerPrefabPath);
        if (shop == null || gray == null || outline == null || stationPrefab == null || flowerPrefab == null)
        {
            Debug.LogError("Missing: Shop, Graybox_Wrapping.mat, HintOutline.mat, BouquetStation or Flower.");
            return;
        }
        RemoveOldSandbox();

        // 10 grays, light to dark: ribbons use all of them, papers every second one
        Material[] shades = new Material[10];
        for (int i = 0; i < shades.Length; i++)
        {
            shades[i] = MakeShade("Gray_" + (i + 1), 0.95f - i * 0.075f, gray);
        }

        GameObject paperSheet = MakePaperSheetPrefab(gray);
        GameObject ribbonPiece = MakeRibbonPrefab(gray);

        // ---------- wrapping table: bouquet (Andreea's prefab) + station; saved as WrappingTable.prefab ----------
        GameObject table = new GameObject("WrappingTable");
        table.transform.SetParent(shop.transform, false);
        GameObject bouquetStation = (GameObject)PrefabUtility.InstantiatePrefab(stationPrefab, table.transform);
        Bouquet bouquet = bouquetStation.GetComponentInChildren<Bouquet>();
        Transform cone = FindChild(bouquet.transform, "WrapCone");
        Transform bow = FindChild(bouquet.transform, "Ribbon");

        GrowIn coneGrow = cone.gameObject.AddComponent<GrowIn>();
        coneGrow.playOnEnable = false;
        bow.gameObject.AddComponent<GrowIn>();

        for (int i = 0; i < 4; i++)
        {
            GameObject flower = (GameObject)PrefabUtility.InstantiatePrefab(flowerPrefab, shop.transform);
            flower.name = "TestFlower" + (i + 1);
        }

        // ---------- wrapping station ----------
        GameObject stationObject = new GameObject("WrappingStation");
        stationObject.transform.SetParent(table.transform, false);

        // paper spot: the area that catches the sheet when you let go
        GameObject paperSpot = new GameObject("PaperSpot");
        paperSpot.transform.SetParent(stationObject.transform, false);
        BoxCollider spotTrigger = paperSpot.AddComponent<BoxCollider>();
        spotTrigger.isTrigger = true;
        spotTrigger.size = new Vector3(0.6f, 0.3f, 0.6f);
        WrapSocket paperSocket = paperSpot.AddComponent<WrapSocket>();
        paperSocket.acceptRibbon = false;

        GameObject paperAttach = new GameObject("PaperAttach");
        paperAttach.transform.SetParent(paperSpot.transform, false);
        paperSocket.attachTransform = paperAttach.transform;

        // laid paper: middle + 2 flaps (left, right)
        GameObject laidPaper = new GameObject("LaidPaper");
        laidPaper.transform.SetParent(stationObject.transform, false);
        float inner = 0.08f;                     // half the middle width
        float flapSize = SheetSize / 2f - inner; // 12 cm
        GameObject middle = MakeBox("Middle", laidPaper.transform, Vector3.zero, new Vector3(inner * 2f, SheetThickness, SheetSize), gray, false);
        PaperFlap leftFlap = MakeFlap("FlapLeft", laidPaper.transform, -inner, Vector3.left, flapSize, gray, outline);
        PaperFlap rightFlap = MakeFlap("FlapRight", laidPaper.transform, inner, Vector3.right, flapSize, gray, outline);

        // ribbon spot at the bouquet base
        GameObject ribbonSpot = new GameObject("RibbonSpot");
        ribbonSpot.transform.SetParent(stationObject.transform, false);
        SphereCollider ribbonTrigger = ribbonSpot.AddComponent<SphereCollider>();
        ribbonTrigger.isTrigger = true;
        ribbonTrigger.radius = 0.18f;
        WrapSocket ribbonSocket = ribbonSpot.AddComponent<WrapSocket>();
        ribbonSocket.acceptRibbon = true;

        // outlines: shown only while holding the matching item
        GameObject paperHint = MakeOutlineRect("PaperHint", paperSpot.transform, SheetSize + 0.02f, SheetSize + 0.02f, outline);
        GameObject flowerHint = MakeOutlineRect("FlowerHint", stationObject.transform, 0.16f, 0.34f, outline);
        // the cone is ~9 cm wide where the ribbon goes, so the ring must be bigger to be visible
        GameObject ribbonHint = MakeOutlineRing("RibbonHint", stationObject.transform, 0.1f, 24, outline);
        paperHint.SetActive(false);
        flowerHint.SetActive(false);
        ribbonHint.SetActive(false);

        WrappingStation station = stationObject.AddComponent<WrappingStation>();
        station.bouquet = bouquet;
        station.bouquetGrab = bouquet.GetComponent<XRGrabInteractable>();
        station.paperSocket = paperSocket;
        station.paperSpotMarker = paperHint;
        station.laidPaper = laidPaper;
        station.flaps = new PaperFlap[] { leftFlap, rightFlap };
        station.laidPaperPieces = new Renderer[]
        {
            middle.GetComponent<Renderer>(),
            leftFlap.transform.Find("Paper").GetComponent<Renderer>(),
            rightFlap.transform.Find("Paper").GetComponent<Renderer>(),
        };
        leftFlap.station = station;
        rightFlap.station = station;
        station.wrapCone = cone.GetComponent<Renderer>();
        station.wrapConeGrow = coneGrow;
        station.ribbonBow = bow.gameObject;
        station.ribbonSocket = ribbonSocket;
        station.flowerHint = flowerHint;
        station.ribbonHint = ribbonHint;

        // ---------- old graybox cabinet and shelves are not used anymore (sandbox only) ----------
        string[] oldFurniture = { "WrappingPaperCounter", "RibbonsShelve1", "RibbonsShelve2" };
        for (int i = 0; i < oldFurniture.Length; i++)
        {
            Transform old = shop.transform.Find(oldFurniture[i]);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }

        GameObject wall = BuildWrapWall(shop.transform, paperSheet, ribbonPiece, gray, shades);
        BuildStatsBoard(shop.transform, gray);
        ArrangeTable();

        // prefabs used in ver2_scene (as children of Shop)
        PrefabUtility.SaveAsPrefabAssetAndConnect(wall, PrefabsFolder + "/WrapWall.prefab", InteractionMode.AutomatedAction);
        PrefabUtility.SaveAsPrefabAssetAndConnect(table, PrefabsFolder + "/WrappingTable.prefab", InteractionMode.AutomatedAction);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(shop.scene);
        Debug.Log("Wrapping sandbox ready. Save the scene (Ctrl+S).");
    }

    // Wrapping wall: a board, 5 paper rolls on top, 2 bars with 5 ribbon spools each below.
    static GameObject BuildWrapWall(Transform shop, GameObject sheetPrefab, GameObject ribbonPrefab, Material gray, Material[] shades)
    {
        Bounds room = GameObject.Find("Room").GetComponent<Renderer>().bounds;
        GameObject wall = new GameObject("WrapWall");
        wall.transform.SetParent(shop, false);
        wall.transform.position = new Vector3(0.55f, room.min.y, room.min.z);

        float rollLength = SheetSize;
        float frontZ = 0.075f;    // distance of rolls and spools from the wall

        MakeBox("Board", wall.transform, new Vector3(0f, 1.26f, 0.01f), new Vector3(0.6f, 1.34f, 0.02f), gray, false);

        // paper rolls
        for (int i = 0; i < paperNames.Length; i++)
        {
            Material paper = shades[i * 2];
            GameObject row = new GameObject("Roll_" + paperNames[i]);
            row.transform.SetParent(wall.transform, false);
            row.transform.localPosition = new Vector3(0f, 1.8f - i * 0.17f, frontZ);

            GameObject roll = MakeCylinder("RollVisual", row.transform, Vector3.zero, 0.07f, rollLength, paper);

            // the paper comes out here (front bottom of the roll)
            GameObject pullOrigin = new GameObject("PullOrigin");
            pullOrigin.transform.SetParent(row.transform, false);
            pullOrigin.transform.localPosition = new Vector3(0f, -0.01f, 0.037f);

            // hanging paper that unrolls
            GameObject pulled = new GameObject("PulledPaper");
            pulled.transform.SetParent(row.transform, false);
            pulled.transform.localPosition = pullOrigin.transform.localPosition;
            MakeBox("Paper", pulled.transform, new Vector3(0f, -0.05f, 0f), new Vector3(rollLength - 0.02f, 0.1f, 0.002f), paper, false);

            // "handle": the hanging edge you grab
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(row.transform, false);
            handle.transform.localPosition = new Vector3(0f, -0.065f, 0.04f);
            BoxCollider handleCollider = handle.AddComponent<BoxCollider>();
            handleCollider.size = new Vector3(rollLength, 0.11f, 0.03f);
            handle.AddComponent<XRSimpleInteractable>();
            PaperRoll paperRoll = handle.AddComponent<PaperRoll>();
            paperRoll.sheetPrefab = sheetPrefab;
            paperRoll.paperMaterial = paper;
            paperRoll.colorName = paperNames[i];
            paperRoll.pullOrigin = pullOrigin.transform;
            paperRoll.pulledPaper = pulled.transform;
            paperRoll.rollVisual = roll.transform;
            paperRoll.sheetLength = SheetSize;
        }

        // ribbons: 2 bars as long as the rolls, 5 spools each
        float[] barY = { 0.92f, 0.74f };
        for (int bar = 0; bar < 2; bar++)
        {
            MakeCylinder("RibbonBar", wall.transform, new Vector3(0f, barY[bar], frontZ), 0.016f, rollLength + 0.1f, gray);

            for (int k = 0; k < 5; k++)
            {
                int c = bar * 5 + k;
                GameObject spool = MakeCylinder("Spool_" + ribbonNames[c], wall.transform,
                    new Vector3(-rollLength / 2f + k * 0.1f, barY[bar], frontZ), 0.07f, 0.035f, shades[c]);
                spool.AddComponent<BoxCollider>();   // so it can be grabbed
                spool.AddComponent<XRSimpleInteractable>();
                WrapDispenser dispenser = spool.AddComponent<WrapDispenser>();
                dispenser.itemPrefab = ribbonPrefab;
                dispenser.itemMaterial = shades[c];
                dispenser.colorName = ribbonNames[c];
            }
        }
        return wall;
    }

    // Shop stats board: for now just a gray board next to the wrapping wall.
    static void BuildStatsBoard(Transform shop, Material gray)
    {
        Bounds room = GameObject.Find("Room").GetComponent<Renderer>().bounds;
        GameObject board = MakeBox("ScoreBoard", shop, Vector3.zero, new Vector3(0.8f, 0.95f, 0.02f), gray, false);
        board.transform.position = new Vector3(-0.55f, room.min.y + 1.3f, room.min.z + 0.01f);
    }

    // Arranges the assembling counter: paper spot, outlines, test flowers and scissors.
    [MenuItem("Little Bloom/Arrange Wrapping Table")]
    public static void ArrangeTable()
    {
        Bouquet bouquet = Object.FindFirstObjectByType<Bouquet>();
        BouquetZone zone = Object.FindFirstObjectByType<BouquetZone>();
        WrappingStation station = Object.FindFirstObjectByType<WrappingStation>();
        GameObject counter = GameObject.Find("AssemblingCounter");
        if (bouquet == null || zone == null || station == null || counter == null)
        {
            Debug.LogError("Missing in the scene: Bouquet, BouquetZone, WrappingStation or AssemblingCounter.");
            return;
        }

        // the bouquet station stays where Andreea placed it in the prefab
        bouquet.transform.parent.localPosition = Vector3.zero;

        // the table is rotated, so the edges come from bounds
        Bounds table = counter.GetComponent<Renderer>().bounds;

        // paper sits under the middle of the cone
        Vector3 paperCenter = FindChild(bouquet.transform, "WrapCone").position;
        paperCenter.y = table.max.y + SheetThickness / 2f + 0.002f;

        station.paperSocket.transform.position = paperCenter;
        // the sheet starts at the stem end and goes toward the flowers
        station.paperSocket.attachTransform.position = paperCenter + new Vector3(0f, 0f, -SheetSize / 2f);
        station.paperSocket.attachTransform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.back);
        station.laidPaper.transform.position = paperCenter;
        station.flowerHint.transform.position = paperCenter + new Vector3(0f, 0.004f, 0f);

        // ribbon spot and ring: around the stems, where the bow appears
        station.ribbonSocket.transform.position = station.ribbonBow.transform.position;
        station.ribbonHint.transform.position = station.ribbonBow.transform.position;
        station.ribbonHint.transform.rotation = bouquet.transform.rotation;

        // test flowers: lying down, smaller, right of the bouquet zone
        // (inside the zone they would join the bouquet by themselves)
        Bounds zoneBounds = zone.GetComponent<BoxCollider>().bounds;
        float flowerScale = 0.6f;
        float flowerLength = 0.66f * flowerScale;
        float[] turns = { -5f, 4f, -2f, 6f };   // slightly turned so they don't look perfectly aligned
        for (int i = 0; i < 4; i++)
        {
            GameObject flower = GameObject.Find("TestFlower" + (i + 1));
            if (flower == null) continue;
            flower.transform.localScale = new Vector3(flowerScale, flowerScale, flowerScale);
            flower.transform.rotation = Quaternion.Euler(0f, turns[i], 90f);
            flower.transform.position = new Vector3(table.center.x + flowerLength / 2f, table.max.y + 0.02f, zoneBounds.max.z + 0.06f + i * 0.08f);
        }

        // scissors in the free right corner
        GameObject scissors = GameObject.Find("Scissors");
        if (scissors != null)
        {
            Vector3 p = scissors.transform.position;
            scissors.transform.position = new Vector3(table.center.x + 0.2f, p.y, table.max.z - 0.12f);
        }

        EditorSceneManager.MarkSceneDirty(bouquet.gameObject.scene);
    }

    // Removes what the builder added last time.
    static void RemoveOldSandbox()
    {
        string[] oldNames = { "WrappingTable", "WrappingStation", "PaperRack", "WrapWall", "ScoreBoard", "TestFlower1", "TestFlower2", "TestFlower3", "TestFlower4" };
        for (int i = 0; i < oldNames.Length; i++)
        {
            GameObject old = GameObject.Find(oldNames[i]);
            if (old != null) Object.DestroyImmediate(old);
        }

        Bouquet oldBouquet = Object.FindFirstObjectByType<Bouquet>();
        if (oldBouquet != null && oldBouquet.transform.parent != null)
        {
            Object.DestroyImmediate(oldBouquet.transform.parent.gameObject);
        }
    }

    // Gray material with the given brightness (0 = black, 1 = white). Reused if it exists.
    static Material MakeShade(string name, float value, Material gray)
    {
        string path = "Assets/Materials/Wrapping/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(gray);
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = new Color(value, value, value);
        EditorUtility.SetDirty(material);
        return material;
    }

    static GameObject MakeBox(string name, Transform parent, Vector3 localPosition, Vector3 size, Material material, bool keepCollider)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        if (!keepCollider) Object.DestroyImmediate(box.GetComponent<Collider>());
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPosition;
        box.transform.localScale = size;
        box.GetComponent<Renderer>().sharedMaterial = material;
        return box;
    }

    // Cylinder lying on the X axis: rolls, bars, spools.
    static GameObject MakeCylinder(string name, Transform parent, Vector3 localPosition, float diameter, float length, Material material)
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = name;
        Object.DestroyImmediate(cylinder.GetComponent<Collider>());
        cylinder.transform.SetParent(parent, false);
        cylinder.transform.localPosition = localPosition;
        cylinder.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        // a Unity cylinder is 2 tall, so the Y scale is half the length
        cylinder.transform.localScale = new Vector3(diameter, length / 2f, diameter);
        cylinder.GetComponent<Renderer>().sharedMaterial = material;
        return cylinder;
    }

    // One paper flap: the object sits on the hinge, the paper goes toward openDirection.
    static PaperFlap MakeFlap(string name, Transform parent, float hingeX, Vector3 openDirection, float width, Material paper, Material tabMaterial)
    {
        GameObject hinge = new GameObject(name);
        hinge.transform.SetParent(parent, false);
        hinge.transform.localPosition = new Vector3(hingeX, 0f, 0f);

        // flap paper, with a thicker collider so it is easy to grab
        GameObject paperPiece = MakeBox("Paper", hinge.transform, openDirection * (width / 2f), new Vector3(width, SheetThickness, SheetSize), paper, true);
        paperPiece.GetComponent<BoxCollider>().size = new Vector3(1f, 6f, 1f);

        // edge marker: "grab here"
        GameObject tab = MakeBox("Tab", hinge.transform, openDirection * (width - 0.012f) + new Vector3(0f, 0.003f, 0f),
            new Vector3(0.01f, 0.002f, SheetSize * 0.5f), tabMaterial, false);

        hinge.AddComponent<XRSimpleInteractable>();
        PaperFlap flap = hinge.AddComponent<PaperFlap>();
        flap.openDirection = openDirection;
        flap.tab = tab;
        flap.width = width;
        flap.length = SheetSize;
        // the 2 flaps meet above the middle: a triangle whose base is the middle
        flap.foldedAngle = 180f - Mathf.Acos(Mathf.Abs(hingeX) / width) * Mathf.Rad2Deg;
        return flap;
    }

    // Rectangle outline (4 thin strips) lying on the table, no fill.
    static GameObject MakeOutlineRect(string name, Transform parent, float sizeX, float sizeZ, Material material)
    {
        GameObject outline = new GameObject(name);
        outline.transform.SetParent(parent, false);
        float line = 0.006f;
        MakeBox("Top", outline.transform, new Vector3(0f, 0f, sizeZ / 2f), new Vector3(sizeX + line, 0.002f, line), material, false);
        MakeBox("Bottom", outline.transform, new Vector3(0f, 0f, -sizeZ / 2f), new Vector3(sizeX + line, 0.002f, line), material, false);
        MakeBox("Left", outline.transform, new Vector3(-sizeX / 2f, 0f, 0f), new Vector3(line, 0.002f, sizeZ), material, false);
        MakeBox("Right", outline.transform, new Vector3(sizeX / 2f, 0f, 0f), new Vector3(line, 0.002f, sizeZ), material, false);
        return outline;
    }

    // Round outline (ring of small pieces) in the object's XZ plane.
    static GameObject MakeOutlineRing(string name, Transform parent, float radius, int pieces, Material material)
    {
        GameObject ring = new GameObject(name);
        ring.transform.SetParent(parent, false);
        float pieceLength = 2f * Mathf.PI * radius / pieces + 0.002f;
        for (int i = 0; i < pieces; i++)
        {
            Quaternion turn = Quaternion.Euler(0f, i * 360f / pieces, 0f);
            GameObject piece = MakeBox("Piece", ring.transform, turn * new Vector3(0f, 0f, radius), new Vector3(pieceLength, 0.01f, 0.01f), material, false);
            piece.transform.localRotation = turn;
        }
        return ring;
    }

    // Sheet torn from the roll: held by its edge, hanging below the hand.
    static GameObject MakePaperSheetPrefab(Material material)
    {
        GameObject item = new GameObject("PaperSheet");
        // origin is the top edge (roll side); the sheet hangs down from it
        GameObject paper = MakeBox("Paper", item.transform, new Vector3(0f, -SheetSize / 2f, 0f), new Vector3(SheetSize, SheetSize, SheetThickness), material, true);
        // can be cut with the scissors (Cuttable is on the object with the collider)
        paper.AddComponent<Cuttable>().cutType = Cuttable.CutType.WrappingPaper;

        // the bottom edge is the one you hold
        GameObject holdPoint = new GameObject("HoldPoint");
        holdPoint.transform.SetParent(item.transform, false);
        holdPoint.transform.localPosition = new Vector3(0f, -SheetSize, 0f);
        holdPoint.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);

        Rigidbody body = item.AddComponent<Rigidbody>();
        body.mass = 0.02f;
        body.linearDamping = 4f;      // falls slowly, like paper
        body.angularDamping = 3f;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;   // thin sheet, so it does not pass through the table

        XRGrabInteractable grab = item.AddComponent<XRGrabInteractable>();
        grab.attachTransform = holdPoint.transform;
        grab.throwOnDetach = false;
        item.AddComponent<WrapItem>().isRibbon = false;

        return SavePrefab(item);
    }

    // Ribbon piece taken from a spool.
    static GameObject MakeRibbonPrefab(Material material)
    {
        // long on X, because the scissors shorten the ribbon along X
        GameObject item = MakeBox("RibbonPiece", null, Vector3.zero, new Vector3(0.3f, 0.004f, 0.03f), material, true);
        item.AddComponent<Cuttable>().cutType = Cuttable.CutType.Ribbon;
        Rigidbody body = item.AddComponent<Rigidbody>();
        body.mass = 0.05f;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        item.AddComponent<XRGrabInteractable>();
        item.AddComponent<WrapItem>().isRibbon = true;
        return SavePrefab(item);
    }

    static GameObject SavePrefab(GameObject item)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(item, PrefabsFolder + "/" + item.name + ".prefab");
        Object.DestroyImmediate(item);
        return prefab;
    }

    static Transform FindChild(Transform root, string name)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == name) return children[i];
        }
        return null;
    }
}
