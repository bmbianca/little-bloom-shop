using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

// Meniu: Little Bloom > Build Front Windows (Vitrine)
// Construiește fațada cu 2 vitrine mari de florărie pe peretele cu ușa:
// - Taie / șterge fața frontală a cubului Room pentru a deschide vederea spre exterior
// - Alocă materiale INDEPENDENTE pentru: Ușă (Door.mat), Pereți (ShopWall.mat), Rame (WindowFrame.mat), Sticlă (Glass.mat)
// - Creează cele 2 geamuri mari dreptunghiulare (ramă, sticlă transparentă, pervaz interior pentru flori)
// - Creează stâlpii și panourile de perete din jurul ușii și al ferestrelor
public static class ShopWindowBuilder
{
    const string WallMaterialPath = "Assets/Materials/Environment/ShopWall.mat";
    const string DoorMaterialPath = "Assets/Materials/Environment/Door.mat";
    const string GlassMaterialPath = "Assets/Materials/Environment/Glass.mat";
    const string FrameMaterialPath = "Assets/Materials/Environment/WindowFrame.mat";

    [MenuItem("Little Bloom/Build Front Windows (Vitrine)")]
    public static void Build()
    {
        GameObject shop = GameObject.Find("Shop");
        if (shop == null)
        {
            EditorUtility.DisplayDialog("Little Bloom", "Nu am găsit obiectul 'Shop' în scena curentă!", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Build Front Windows");

        // 1. Materiale INDEPENDENTE (ca să nu se mai schimbe toate la un loc)
        Material wallMat = GetOrCreateWallMaterial();
        Material doorMat = GetOrCreateDoorMaterial();
        Material glassMat = GetOrCreateGlassMaterial();
        Material frameMat = GetOrCreateFrameMaterial();

        // 2. Setăm materialul dedicat pe Ușă (astfel ușa are culoarea ei separată)
        AssignDoorMaterial(shop, doorMat);

        // 3. Tăierea / Ștergerea feței frontale din cubul Room (ProBuilder) și aplicarea materialului de perete pe Room
        CutRoomFrontWallAndAssignMaterial(shop, wallMat);

        // 4. Ștergem fațada veche dacă a mai fost generată anterior
        Transform oldFacade = shop.transform.Find("FrontFacade");
        if (oldFacade != null)
        {
            Undo.DestroyObjectImmediate(oldFacade.gameObject);
        }

        // 5. Construim noua fațadă cu cele 2 vitrine mari
        GameObject facade = new GameObject("FrontFacade");
        Undo.RegisterCreatedObjectUndo(facade, "Create FrontFacade");
        facade.transform.SetParent(shop.transform, false);

        BuildFacadeGeometry(facade.transform, wallMat, frameMat, glassMat);

        EditorSceneManager.MarkSceneDirty(shop.scene);
        EditorUtility.DisplayDialog("Little Bloom", 
            "Vitrinele au fost actualizate cu succes!\n\n" +
            "Materialele sunt acum COMPLET SEPARATE:\n" +
            "• Ușa: 'Assets/Materials/Environment/Door.mat'\n" +
            "• Pereții: 'Assets/Materials/Environment/ShopWall.mat'\n" +
            "• Ramele: 'Assets/Materials/Environment/WindowFrame.mat'\n" +
            "• Sticla: 'Assets/Materials/Environment/Glass.mat'\n\n" +
            "Acum poți schimba culoarea ușii fără să se mai schimbe și pereții!", "Super!");
    }

    static void AssignDoorMaterial(GameObject shop, Material doorMat)
    {
        Transform door = shop.transform.Find("Door");
        if (door != null)
        {
            Renderer r = door.GetComponent<Renderer>();
            if (r != null)
            {
                Undo.RecordObject(r, "Assign Door Material");
                r.sharedMaterial = doorMat;
            }
        }
    }

    static void CutRoomFrontWallAndAssignMaterial(GameObject shop, Material wallMat)
    {
        Transform roomTransform = shop.transform.Find("Room");
        if (roomTransform == null) return;

        // Punem același material de perete și pe Room, ca pereții să fie uniformi
        Renderer roomRenderer = roomTransform.GetComponent<Renderer>();
        if (roomRenderer != null && wallMat != null)
        {
            Undo.RecordObject(roomRenderer, "Assign Room Wall Material");
            roomRenderer.sharedMaterial = wallMat;
        }

        ProBuilderMesh pbMesh = roomTransform.GetComponent<ProBuilderMesh>();
        if (pbMesh == null) return;

        // Căutăm fața din față a camerei (orientată spre Z pozitiv în coordonate World)
        Face frontFace = null;
        float maxAverageZ = -9999f;

        foreach (Face face in pbMesh.faces)
        {
            IList<int> indices = face.indexes;
            if (indices == null || indices.Count == 0) continue;

            float avgZ = 0f;
            for (int i = 0; i < indices.Count; i++)
            {
                Vector3 worldPt = roomTransform.TransformPoint(pbMesh.positions[indices[i]]);
                avgZ += worldPt.z;
            }
            avgZ /= indices.Count;

            // Peretele frontal cu ușa este situat la Z ≈ 3.98
            if (avgZ > maxAverageZ)
            {
                maxAverageZ = avgZ;
                frontFace = face;
            }
        }

        // Dacă peretele frontal există și are Z > 2.5 (semn că e peretele din față și nu a fost deja șters)
        if (frontFace != null && maxAverageZ > 2.5f)
        {
            Undo.RecordObject(pbMesh, "Cut Room Front Wall");
            pbMesh.DeleteFace(frontFace);
            pbMesh.ToMesh();
            pbMesh.Refresh();

            MeshCollider mc = roomTransform.GetComponent<MeshCollider>();
            if (mc != null)
            {
                mc.sharedMesh = null;
                mc.sharedMesh = roomTransform.GetComponent<MeshFilter>().sharedMesh;
            }
        }
    }

    static void BuildFacadeGeometry(Transform facadeParent, Material wallMat, Material frameMat, Material glassMat)
    {
        const float wallZ = 3.982f;
        const float wallThickness = 0.10f;

        // Dimensiuni perete:
        // Podea local Y = -1.4f (World Y = 0.0), Tavan local Y = 1.4f (World Y = 2.8)
        // Înălțime totală = 2.8m. Lățime totală de la X = -2.0m la X = +2.0m (4m)
        // Ușa este la X = -0.062, lățime 0.9m (de la X = -0.512 la X = +0.388), înălțime 2.1m (Y = -1.4 la +0.7)

        // 1. Panou perete plin Stânga (de la X = -2.0 la X = -1.55)
        MakeBox("Wall_Left", facadeParent, 
            new Vector3(-1.775f, 0f, wallZ), 
            new Vector3(0.45f, 2.8f, wallThickness), wallMat);

        // 2. Panou perete plin Dreapta (de la X = +1.55 la X = +2.0)
        MakeBox("Wall_Right", facadeParent, 
            new Vector3(1.775f, 0f, wallZ), 
            new Vector3(0.45f, 2.8f, wallThickness), wallMat);

        // 3. Grinda de sus (lintel deasupra ferestrelor și ușii, de la Y = +0.7 la Y = +1.4)
        MakeBox("Wall_Header", facadeParent, 
            new Vector3(0f, 1.05f, wallZ), 
            new Vector3(3.10f, 0.7f, wallThickness), wallMat);

        // 4. Stâlp între Fereastra Stânga și Ușă (de la X = -0.58 la X = -0.512, Y = -1.4 la +0.7)
        MakeBox("Pillar_LeftDoor", facadeParent, 
            new Vector3(-0.546f, -0.35f, wallZ), 
            new Vector3(0.068f, 2.1f, wallThickness), frameMat ?? wallMat);

        // 5. Stâlp între Ușă și Fereastra Dreapta (de la X = +0.388 la X = +0.46, Y = -1.4 la +0.7)
        MakeBox("Pillar_RightDoor", facadeParent, 
            new Vector3(0.424f, -0.35f, wallZ), 
            new Vector3(0.072f, 2.1f, wallThickness), frameMat ?? wallMat);

        // 6. Vitrina din Stânga (X: -1.55 la -0.58 => lățime 0.97m)
        BuildShopWindow("Window_Left", facadeParent, 
            centerX: -1.065f, 
            width: 0.97f, 
            sillHeight: 0.50f, 
            windowHeight: 1.60f, 
            wallZ: wallZ, 
            wallThickness: wallThickness, 
            wallMat: wallMat, 
            frameMat: frameMat, 
            glassMat: glassMat);

        // 7. Vitrina din Dreapta (X: +0.46 la +1.55 => lățime 1.09m)
        BuildShopWindow("Window_Right", facadeParent, 
            centerX: 1.005f, 
            width: 1.09f, 
            sillHeight: 0.50f, 
            windowHeight: 1.60f, 
            wallZ: wallZ, 
            wallThickness: wallThickness, 
            wallMat: wallMat, 
            frameMat: frameMat, 
            glassMat: glassMat);
    }

    static void BuildShopWindow(string name, Transform parent, float centerX, float width, 
        float sillHeight, float windowHeight, float wallZ, float wallThickness, 
        Material wallMat, Material frameMat, Material glassMat)
    {
        GameObject windowRoot = new GameObject(name);
        windowRoot.transform.SetParent(parent, false);

        // Parapetul de sub geam (de la podea Y = -1.4 până la Y = -1.4 + sillHeight)
        float baseCenterY = -1.4f + (sillHeight / 2f);
        MakeBox("Sill_Wall", windowRoot.transform, 
            new Vector3(centerX, baseCenterY, wallZ), 
            new Vector3(width, sillHeight, wallThickness), wallMat);

        // Pervaz interior (raft pentru vaze / flori la vitrină)
        float ledgeY = -1.4f + sillHeight;
        MakeBox("Interior_FlowerLedge", windowRoot.transform, 
            new Vector3(centerX, ledgeY, wallZ - 0.15f), 
            new Vector3(width + 0.05f, 0.04f, 0.35f), frameMat ?? wallMat);

        // Centrul și dimensiunile golului de sticlă
        float glassCenterY = ledgeY + (windowHeight / 2f);
        const float frameSize = 0.04f;

        // Rame exterioare geam
        MakeBox("Frame_Bottom", windowRoot.transform, 
            new Vector3(centerX, ledgeY + frameSize / 2f, wallZ), 
            new Vector3(width, frameSize, wallThickness + 0.02f), frameMat);
        MakeBox("Frame_Top", windowRoot.transform, 
            new Vector3(centerX, ledgeY + windowHeight - frameSize / 2f, wallZ), 
            new Vector3(width, frameSize, wallThickness + 0.02f), frameMat);
        MakeBox("Frame_Left", windowRoot.transform, 
            new Vector3(centerX - (width / 2f) + frameSize / 2f, glassCenterY, wallZ), 
            new Vector3(frameSize, windowHeight, wallThickness + 0.02f), frameMat);
        MakeBox("Frame_Right", windowRoot.transform, 
            new Vector3(centerX + (width / 2f) - frameSize / 2f, glassCenterY, wallZ), 
            new Vector3(frameSize, windowHeight, wallThickness + 0.02f), frameMat);

        // Sticla mare transparentă
        GameObject glass = MakeBox("Glass_Pane", windowRoot.transform, 
            new Vector3(centerX, glassCenterY, wallZ), 
            new Vector3(width - (frameSize * 2f), windowHeight - (frameSize * 2f), 0.015f), glassMat);

        BoxCollider bc = glass.GetComponent<BoxCollider>();
        if (bc == null) glass.AddComponent<BoxCollider>();

        // Baghetă decorativă / bară orizontală la 2/3 din înălțime (stil boutique clasic)
        float barY = ledgeY + (windowHeight * 0.65f);
        MakeBox("Frame_Mullion", windowRoot.transform, 
            new Vector3(centerX, barY, wallZ), 
            new Vector3(width - (frameSize * 2f), 0.025f, 0.03f), frameMat);
    }

    static GameObject MakeBox(string name, Transform parent, Vector3 localPosition, Vector3 size, Material material)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPosition;
        box.transform.localScale = size;
        if (material != null)
        {
            box.GetComponent<Renderer>().sharedMaterial = material;
        }
        return box;
    }

    static Material GetOrCreateWallMaterial()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(WallMaterialPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            mat.name = "ShopWall";
            // Culoare gri deschis / crem cald pentru pereți
            mat.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            mat.SetFloat("_Glossiness", 0.1f);
            mat.SetFloat("_Metallic", 0.0f);

            AssetDatabase.CreateAsset(mat, WallMaterialPath);
            AssetDatabase.SaveAssets();
        }
        return mat;
    }

    static Material GetOrCreateDoorMaterial()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(DoorMaterialPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            mat.name = "Door";
            // Culoare distinctă pentru ușă: lemn cald / sage green de florărie
            mat.color = new Color(0.40f, 0.48f, 0.42f, 1f);
            mat.SetFloat("_Glossiness", 0.3f);
            mat.SetFloat("_Metallic", 0.05f);

            AssetDatabase.CreateAsset(mat, DoorMaterialPath);
            AssetDatabase.SaveAssets();
        }
        return mat;
    }

    static Material GetOrCreateGlassMaterial()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(GlassMaterialPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            mat.name = "Glass";
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            mat.color = new Color(0.82f, 0.93f, 0.97f, 0.22f);
            mat.SetFloat("_Glossiness", 0.95f);
            mat.SetFloat("_Metallic", 0.05f);

            AssetDatabase.CreateAsset(mat, GlassMaterialPath);
            AssetDatabase.SaveAssets();
        }
        return mat;
    }

    static Material GetOrCreateFrameMaterial()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(FrameMaterialPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            mat.name = "WindowFrame";
            mat.color = new Color(0.22f, 0.22f, 0.23f, 1f);
            mat.SetFloat("_Glossiness", 0.4f);
            mat.SetFloat("_Metallic", 0.1f);

            AssetDatabase.CreateAsset(mat, FrameMaterialPath);
            AssetDatabase.SaveAssets();
        }
        return mat;
    }
}
