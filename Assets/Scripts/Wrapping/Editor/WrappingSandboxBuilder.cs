using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Meniu: Little Bloom > Build Wrapping Sandbox
// Pune in scena sandbox_wrapping tot ce trebuie pentru ambalare (graybox, totul gri):
// peretele cu role de hartie si panglici, panoul de statistici, statia de pe masa.
// Daca e rulat din nou, sterge ce a facut data trecuta si reface totul.
public static class WrappingSandboxBuilder
{
    const string PrefabsFolder = "Assets/Prefabs/Wrapping";
    const string BaseMaterialPath = "Assets/Graybox_Wrapping.mat";
    const string OutlineMaterialPath = "Assets/Materials/Wrapping/HintOutline.mat";
    const string StationPrefabPath = "Assets/Prefabs/Flowers/BouquetStation.prefab";
    const string FlowerPrefabPath = "Assets/Prefabs/Flowers/Flower.prefab";

    const float SheetSize = 0.4f;          // foaia: 40 x 40 cm, la fel in mana, pe rola si pe masa
    const float SheetThickness = 0.003f;

    // numele culorilor (pentru comanda); in graybox fiecare are alta nuanta de gri
    static string[] paperNames = { "Pink", "Kraft", "White", "Lilac", "Mint" };
    static string[] ribbonNames = { "Red", "Pink", "Gold", "White", "Green", "Lilac", "Blue", "Peach", "Silver", "Burgundy" };

    [MenuItem("Little Bloom/Build Wrapping Sandbox")]
    public static void Build()
    {
        if (GameObject.Find("WrappingStation") != null)
        {
            bool rebuild = EditorUtility.DisplayDialog("Little Bloom",
                "Scena are deja statia de ambalare. O construiesc din nou?", "Da", "Nu");
            if (!rebuild) return;
        }
        Rebuild();
    }

    // La fel ca meniul, dar fara intrebare.
    public static void Rebuild()
    {
        GameObject shop = GameObject.Find("Shop");
        Material gray = AssetDatabase.LoadAssetAtPath<Material>(BaseMaterialPath);
        Material outline = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
        GameObject stationPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StationPrefabPath);
        GameObject flowerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FlowerPrefabPath);
        if (shop == null || gray == null || outline == null || stationPrefab == null || flowerPrefab == null)
        {
            Debug.LogError("Lipseste ceva: Shop, Graybox_Wrapping.mat, HintOutline.mat, BouquetStation sau Flower.");
            return;
        }
        RemoveOldSandbox();

        // 10 nuante de gri, de la deschis la inchis: panglicile le folosesc pe toate, hartiile din 2 in 2
        Material[] shades = new Material[10];
        for (int i = 0; i < shades.Length; i++)
        {
            shades[i] = MakeShade("Gray_" + (i + 1), 0.95f - i * 0.075f, gray);
        }

        GameObject paperSheet = MakePaperSheetPrefab(gray);
        GameObject ribbonPiece = MakeRibbonPrefab(gray);

        // ---------- masa de ambalare: buchetul (prefab-ul Andreei) + statia; devine WrappingTable.prefab ----------
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

        // ---------- statia de ambalare ----------
        GameObject stationObject = new GameObject("WrappingStation");
        stationObject.transform.SetParent(table.transform, false);

        // locul pentru hartie: zona in care foaia "se prinde" cand ii dai drumul
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

        // foaia intinsa pe masa: mijloc + 2 clape (stanga, dreapta)
        GameObject laidPaper = new GameObject("LaidPaper");
        laidPaper.transform.SetParent(stationObject.transform, false);
        float inner = 0.08f;                     // jumatate din latimea mijlocului
        float flapSize = SheetSize / 2f - inner; // 12 cm
        GameObject middle = MakeBox("Middle", laidPaper.transform, Vector3.zero, new Vector3(inner * 2f, SheetThickness, SheetSize), gray, false);
        PaperFlap leftFlap = MakeFlap("FlapLeft", laidPaper.transform, -inner, Vector3.left, flapSize, gray, outline);
        PaperFlap rightFlap = MakeFlap("FlapRight", laidPaper.transform, inner, Vector3.right, flapSize, gray, outline);

        // locul pentru panglica, la baza buchetului
        GameObject ribbonSpot = new GameObject("RibbonSpot");
        ribbonSpot.transform.SetParent(stationObject.transform, false);
        SphereCollider ribbonTrigger = ribbonSpot.AddComponent<SphereCollider>();
        ribbonTrigger.isTrigger = true;
        ribbonTrigger.radius = 0.18f;
        WrapSocket ribbonSocket = ribbonSpot.AddComponent<WrapSocket>();
        ribbonSocket.acceptRibbon = true;

        // contururile: apar doar cat tii in mana obiectul potrivit
        GameObject paperHint = MakeOutlineRect("PaperHint", paperSpot.transform, SheetSize + 0.02f, SheetSize + 0.02f, outline);
        GameObject flowerHint = MakeOutlineRect("FlowerHint", stationObject.transform, 0.16f, 0.34f, outline);
        // conul are ~9 cm raza unde se leaga panglica, deci inelul trebuie sa fie mai mare, ca sa se vada
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

        // ---------- dulapul si rafturile vechi din graybox nu mai sunt folosite (doar in sandbox) ----------
        string[] oldFurniture = { "WrappingPaperCounter", "RibbonsShelve1", "RibbonsShelve2" };
        for (int i = 0; i < oldFurniture.Length; i++)
        {
            Transform old = shop.transform.Find(oldFurniture[i]);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }

        GameObject wall = BuildWrapWall(shop.transform, paperSheet, ribbonPiece, gray, shades);
        BuildStatsBoard(shop.transform, gray);
        ArrangeTable();

        // prefab-urile pe care le pune Irina in ver2_scene (ca si copii ai Shop)
        PrefabUtility.SaveAsPrefabAssetAndConnect(wall, PrefabsFolder + "/WrapWall.prefab", InteractionMode.AutomatedAction);
        PrefabUtility.SaveAsPrefabAssetAndConnect(table, PrefabsFolder + "/WrappingTable.prefab", InteractionMode.AutomatedAction);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(shop.scene);
        Debug.Log("Wrapping sandbox gata. Salveaza scena (Ctrl+S).");
    }

    // Peretele de ambalare: o placa; sus 5 role de hartie, jos 2 bare cu cate 5 bobine de panglica.
    static GameObject BuildWrapWall(Transform shop, GameObject sheetPrefab, GameObject ribbonPrefab, Material gray, Material[] shades)
    {
        Bounds room = GameObject.Find("Room").GetComponent<Renderer>().bounds;
        GameObject wall = new GameObject("WrapWall");
        wall.transform.SetParent(shop, false);
        wall.transform.position = new Vector3(0.55f, room.min.y, room.min.z);

        float rollLength = SheetSize;
        float frontZ = 0.075f;    // cat de departe de perete stau rolele si bobinele

        MakeBox("Board", wall.transform, new Vector3(0f, 1.26f, 0.01f), new Vector3(0.6f, 1.34f, 0.02f), gray, false);

        // rolele de hartie
        for (int i = 0; i < paperNames.Length; i++)
        {
            Material paper = shades[i * 2];
            GameObject row = new GameObject("Roll_" + paperNames[i]);
            row.transform.SetParent(wall.transform, false);
            row.transform.localPosition = new Vector3(0f, 1.8f - i * 0.17f, frontZ);

            GameObject roll = MakeCylinder("RollVisual", row.transform, Vector3.zero, 0.07f, rollLength, paper);

            // de aici iese hartia (partea din fata-jos a rolei)
            GameObject pullOrigin = new GameObject("PullOrigin");
            pullOrigin.transform.SetParent(row.transform, false);
            pullOrigin.transform.localPosition = new Vector3(0f, -0.01f, 0.037f);

            // hartia care atarna si se desfasoara
            GameObject pulled = new GameObject("PulledPaper");
            pulled.transform.SetParent(row.transform, false);
            pulled.transform.localPosition = pullOrigin.transform.localPosition;
            MakeBox("Paper", pulled.transform, new Vector3(0f, -0.05f, 0f), new Vector3(rollLength - 0.02f, 0.1f, 0.002f), paper, false);

            // "manerul": marginea care atarna, de care apuci
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

        // panglicile: 2 bare de aceeasi lungime ca rolele, cate 5 bobine pe fiecare
        float[] barY = { 0.92f, 0.74f };
        for (int bar = 0; bar < 2; bar++)
        {
            MakeCylinder("RibbonBar", wall.transform, new Vector3(0f, barY[bar], frontZ), 0.016f, rollLength + 0.1f, gray);

            for (int k = 0; k < 5; k++)
            {
                int c = bar * 5 + k;
                GameObject spool = MakeCylinder("Spool_" + ribbonNames[c], wall.transform,
                    new Vector3(-rollLength / 2f + k * 0.1f, barY[bar], frontZ), 0.07f, 0.035f, shades[c]);
                spool.AddComponent<BoxCollider>();   // ca sa poata fi apucata
                spool.AddComponent<XRSimpleInteractable>();
                WrapDispenser dispenser = spool.AddComponent<WrapDispenser>();
                dispenser.itemPrefab = ribbonPrefab;
                dispenser.itemMaterial = shades[c];
                dispenser.colorName = ribbonNames[c];
            }
        }
        return wall;
    }

    // Panoul cu statisticile florariei: deocamdata doar o placa gri langa peretele de ambalare.
    static void BuildStatsBoard(Transform shop, Material gray)
    {
        Bounds room = GameObject.Find("Room").GetComponent<Renderer>().bounds;
        GameObject board = MakeBox("ScoreBoard", shop, Vector3.zero, new Vector3(0.8f, 0.95f, 0.02f), gray, false);
        board.transform.position = new Vector3(-0.55f, room.min.y + 1.3f, room.min.z + 0.01f);
    }

    // Aseaza masa de asamblare: locul pentru hartie, contururile, florile de test si foarfeca.
    [MenuItem("Little Bloom/Arrange Wrapping Table")]
    public static void ArrangeTable()
    {
        Bouquet bouquet = Object.FindFirstObjectByType<Bouquet>();
        BouquetZone zone = Object.FindFirstObjectByType<BouquetZone>();
        WrappingStation station = Object.FindFirstObjectByType<WrappingStation>();
        GameObject counter = GameObject.Find("AssemblingCounter");
        if (bouquet == null || zone == null || station == null || counter == null)
        {
            Debug.LogError("Lipsesc din scena: Bouquet, BouquetZone, WrappingStation sau AssemblingCounter.");
            return;
        }

        // statia de flori sta exact unde a pus-o Andreea in prefab
        bouquet.transform.parent.localPosition = Vector3.zero;

        // masa e rotita, de aceea luam marginile din bounds
        Bounds table = counter.GetComponent<Renderer>().bounds;

        // hartia sta sub mijlocul conului
        Vector3 paperCenter = FindChild(bouquet.transform, "WrapCone").position;
        paperCenter.y = table.max.y + SheetThickness / 2f + 0.002f;

        station.paperSocket.transform.position = paperCenter;
        // foaia se prinde de marginea dinspre tije si se intinde spre flori
        station.paperSocket.attachTransform.position = paperCenter + new Vector3(0f, 0f, -SheetSize / 2f);
        station.paperSocket.attachTransform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.back);
        station.laidPaper.transform.position = paperCenter;
        station.flowerHint.transform.position = paperCenter + new Vector3(0f, 0.004f, 0f);

        // panglica si inelul ei: in jurul tijelor, unde apare fundita
        station.ribbonSocket.transform.position = station.ribbonBow.transform.position;
        station.ribbonHint.transform.position = station.ribbonBow.transform.position;
        station.ribbonHint.transform.rotation = bouquet.transform.rotation;

        // florile de test: culcate, mai mici, in dreapta zonei buchetului
        // (daca ar sta in zona, ar intra singure in buchet)
        Bounds zoneBounds = zone.GetComponent<BoxCollider>().bounds;
        float flowerScale = 0.6f;
        float flowerLength = 0.66f * flowerScale;
        float[] turns = { -5f, 4f, -2f, 6f };   // putin strambe, sa nu para aliniate cu rigla
        for (int i = 0; i < 4; i++)
        {
            GameObject flower = GameObject.Find("TestFlower" + (i + 1));
            if (flower == null) continue;
            flower.transform.localScale = new Vector3(flowerScale, flowerScale, flowerScale);
            flower.transform.rotation = Quaternion.Euler(0f, turns[i], 90f);
            flower.transform.position = new Vector3(table.center.x + flowerLength / 2f, table.max.y + 0.02f, zoneBounds.max.z + 0.06f + i * 0.08f);
        }

        // foarfeca in coltul liber din dreapta
        GameObject scissors = GameObject.Find("Scissors");
        if (scissors != null)
        {
            Vector3 p = scissors.transform.position;
            scissors.transform.position = new Vector3(table.center.x + 0.2f, p.y, table.max.z - 0.12f);
        }

        EditorSceneManager.MarkSceneDirty(bouquet.gameObject.scene);
    }

    // Sterge ce a pus builder-ul data trecuta.
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

    // Un material gri cu luminozitatea data (0 = negru, 1 = alb). Daca exista deja, il refoloseste.
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

    // Cilindru culcat (pe axa X): role, bare, bobine.
    static GameObject MakeCylinder(string name, Transform parent, Vector3 localPosition, float diameter, float length, Material material)
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = name;
        Object.DestroyImmediate(cylinder.GetComponent<Collider>());
        cylinder.transform.SetParent(parent, false);
        cylinder.transform.localPosition = localPosition;
        cylinder.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        // cilindrul din Unity are inaltimea 2, deci scara pe Y e jumatate din lungime
        cylinder.transform.localScale = new Vector3(diameter, length / 2f, diameter);
        cylinder.GetComponent<Renderer>().sharedMaterial = material;
        return cylinder;
    }

    // O clapa a foii de pe masa: obiectul sta pe balama, hartia se intinde spre openDirection.
    static PaperFlap MakeFlap(string name, Transform parent, float hingeX, Vector3 openDirection, float width, Material paper, Material tabMaterial)
    {
        GameObject hinge = new GameObject(name);
        hinge.transform.SetParent(parent, false);
        hinge.transform.localPosition = new Vector3(hingeX, 0f, 0f);

        // hartia clapei, cu un collider mai gros ca sa fie usor de apucat
        GameObject paperPiece = MakeBox("Paper", hinge.transform, openDirection * (width / 2f), new Vector3(width, SheetThickness, SheetSize), paper, true);
        paperPiece.GetComponent<BoxCollider>().size = new Vector3(1f, 6f, 1f);

        // semnul de la margine: "de aici apuci"
        GameObject tab = MakeBox("Tab", hinge.transform, openDirection * (width - 0.012f) + new Vector3(0f, 0.003f, 0f),
            new Vector3(0.01f, 0.002f, SheetSize * 0.5f), tabMaterial, false);

        hinge.AddComponent<XRSimpleInteractable>();
        PaperFlap flap = hinge.AddComponent<PaperFlap>();
        flap.openDirection = openDirection;
        flap.tab = tab;
        flap.width = width;
        flap.length = SheetSize;
        // cele 2 clape se intalnesc deasupra mijlocului: triunghi cu baza = mijlocul foii
        flap.foldedAngle = 180f - Mathf.Acos(Mathf.Abs(hingeX) / width) * Mathf.Rad2Deg;
        return flap;
    }

    // Contur dreptunghiular (4 fasii subtiri), culcat pe masa, fara umplutura.
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

    // Contur rotund (inel din bucati mici), in planul XZ al obiectului.
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

    // Foaia rupta de pe rola: o tii de margine si atarna sub mana.
    static GameObject MakePaperSheetPrefab(Material material)
    {
        GameObject item = new GameObject("PaperSheet");
        // originea e marginea de sus (cea dinspre rola); foaia coboara de acolo
        GameObject paper = MakeBox("Paper", item.transform, new Vector3(0f, -SheetSize / 2f, 0f), new Vector3(SheetSize, SheetSize, SheetThickness), material, true);
        // se poate taia cu foarfeca Biancai (Cuttable sta pe obiectul cu collider)
        paper.AddComponent<Cuttable>().cutType = Cuttable.CutType.WrappingPaper;

        // marginea de jos e cea pe care o tii in mana
        GameObject holdPoint = new GameObject("HoldPoint");
        holdPoint.transform.SetParent(item.transform, false);
        holdPoint.transform.localPosition = new Vector3(0f, -SheetSize, 0f);
        holdPoint.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);

        Rigidbody body = item.AddComponent<Rigidbody>();
        body.mass = 0.02f;
        body.linearDamping = 4f;      // cade incet, ca o foaie
        body.angularDamping = 3f;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;   // foaia e subtire, sa nu treaca prin masa

        XRGrabInteractable grab = item.AddComponent<XRGrabInteractable>();
        grab.attachTransform = holdPoint.transform;
        grab.throwOnDetach = false;
        item.AddComponent<WrapItem>().isRibbon = false;

        return SavePrefab(item);
    }

    // Bucata de panglica luata de pe bobina.
    static GameObject MakeRibbonPrefab(Material material)
    {
        // lunga pe X, pentru ca foarfeca Biancai scurteaza panglica pe axa X
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
