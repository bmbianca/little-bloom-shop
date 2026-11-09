using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

// Menu: Little Bloom > Build Front Windows
// Builds the front wall with the door and 2 large shop windows:
// - removes the front face of the Room cube to open the view outside
// - gives separate materials to the door, walls, frames and glass
// - creates the 2 windows (frame, clear glass, inner sill for flowers)
// - creates the pillars and wall panels around the door and windows
public static class ShopWindowBuilder
{
    const string WallMaterialPath = "Assets/Materials/Environment/ShopWall.mat";
    const string DoorMaterialPath = "Assets/Materials/Environment/Door.mat";
    const string GlassMaterialPath = "Assets/Materials/Environment/Glass.mat";
    const string FrameMaterialPath = "Assets/Materials/Environment/WindowFrame.mat";

    [MenuItem("Little Bloom/Build Front Windows")]
    public static void Build()
    {
        GameObject shop = GameObject.Find("Shop");
        if (shop == null)
        {
            EditorUtility.DisplayDialog("Little Bloom", "Could not find 'Shop' in the current scene.", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Build Front Windows");

        // 1. Separate materials (changing one does not change the others)
        Material wallMat = GetOrCreateWallMaterial();
        Material doorMat = GetOrCreateDoorMaterial();
        Material glassMat = GetOrCreateGlassMaterial();
        Material frameMat = GetOrCreateFrameMaterial();

        // 2. Door material
        AssignDoorMaterial(shop, doorMat);

        // 3. Remove the front face of the Room cube and apply the wall material
        CutRoomFrontWallAndAssignMaterial(shop, wallMat);

        // 4. Remove the old facade if it exists
        Transform oldFacade = shop.transform.Find("FrontFacade");
        if (oldFacade != null)
        {
            Undo.DestroyObjectImmediate(oldFacade.gameObject);
        }

        // 5. Build the new facade with the 2 windows
        GameObject facade = new GameObject("FrontFacade");
        Undo.RegisterCreatedObjectUndo(facade, "Create FrontFacade");
        facade.transform.SetParent(shop.transform, false);

        BuildFacadeGeometry(facade.transform, wallMat, frameMat, glassMat);

        EditorSceneManager.MarkSceneDirty(shop.scene);
        EditorUtility.DisplayDialog("Little Bloom", 
            "The windows were updated.\n\n" +
            "Separate materials:\n" +
            "- Door: 'Assets/Materials/Environment/Door.mat'\n" +
            "- Walls: 'Assets/Materials/Environment/ShopWall.mat'\n" +
            "- Frames: 'Assets/Materials/Environment/WindowFrame.mat'\n" +
            "- Glass: 'Assets/Materials/Environment/Glass.mat'\n\n" +
            "You can change the door color without changing the walls.", "OK");
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

        // Same wall material on Room, so all walls match
        Renderer roomRenderer = roomTransform.GetComponent<Renderer>();
        if (roomRenderer != null && wallMat != null)
        {
            Undo.RecordObject(roomRenderer, "Assign Room Wall Material");
            roomRenderer.sharedMaterial = wallMat;
        }

        ProBuilderMesh pbMesh = roomTransform.GetComponent<ProBuilderMesh>();
        if (pbMesh == null) return;

        // Find the front face of the room (facing world +Z)
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

            // The front wall with the door is at Z = 3.98
            if (avgZ > maxAverageZ)
            {
                maxAverageZ = avgZ;
                frontFace = face;
            }
        }

        // Only if it is the front wall (Z > 2.5) and it was not removed already
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

        // Wall sizes:
        // Floor at local Y = -1.4 (world 0), ceiling at local Y = 1.4 (world 2.8)
        // Height 2.8 m, width 4 m (X from -2 to +2)
        // Door at X = -0.062, 0.9 m wide (X -0.512 to 0.388), 2.1 m tall (Y -1.4 to 0.7)

        // 1. Left wall panel (X -2.0 to -1.55)
        MakeBox("Wall_Left", facadeParent, 
            new Vector3(-1.775f, 0f, wallZ), 
            new Vector3(0.45f, 2.8f, wallThickness), wallMat);

        // 2. Right wall panel (X 1.55 to 2.0)
        MakeBox("Wall_Right", facadeParent, 
            new Vector3(1.775f, 0f, wallZ), 
            new Vector3(0.45f, 2.8f, wallThickness), wallMat);

        // 3. Top beam above the windows and door (Y 0.7 to 1.4)
        MakeBox("Wall_Header", facadeParent, 
            new Vector3(0f, 1.05f, wallZ), 
            new Vector3(3.10f, 0.7f, wallThickness), wallMat);

        // 4. Pillar between the left window and the door (X -0.58 to -0.512)
        MakeBox("Pillar_LeftDoor", facadeParent, 
            new Vector3(-0.546f, -0.35f, wallZ), 
            new Vector3(0.068f, 2.1f, wallThickness), frameMat ?? wallMat);

        // 5. Pillar between the door and the right window (X 0.388 to 0.46)
        MakeBox("Pillar_RightDoor", facadeParent, 
            new Vector3(0.424f, -0.35f, wallZ), 
            new Vector3(0.072f, 2.1f, wallThickness), frameMat ?? wallMat);

        // 6. Left window (X -1.55 to -0.58, 0.97 m wide)
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

        // 7. Right window (X 0.46 to 1.55, 1.09 m wide)
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

        // Wall under the window (from the floor up to sillHeight)
        float baseCenterY = -1.4f + (sillHeight / 2f);
        MakeBox("Sill_Wall", windowRoot.transform, 
            new Vector3(centerX, baseCenterY, wallZ), 
            new Vector3(width, sillHeight, wallThickness), wallMat);

        // Inner sill (shelf for vases and flowers)
        float ledgeY = -1.4f + sillHeight;
        MakeBox("Interior_FlowerLedge", windowRoot.transform, 
            new Vector3(centerX, ledgeY, wallZ - 0.15f), 
            new Vector3(width + 0.05f, 0.04f, 0.35f), frameMat ?? wallMat);

        // Center and size of the glass opening
        float glassCenterY = ledgeY + (windowHeight / 2f);
        const float frameSize = 0.04f;

        // Window frame
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

        // Large clear glass
        GameObject glass = MakeBox("Glass_Pane", windowRoot.transform, 
            new Vector3(centerX, glassCenterY, wallZ), 
            new Vector3(width - (frameSize * 2f), windowHeight - (frameSize * 2f), 0.015f), glassMat);

        BoxCollider bc = glass.GetComponent<BoxCollider>();
        if (bc == null) glass.AddComponent<BoxCollider>();

        // Decorative horizontal bar at about 2/3 height
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
            // Light gray for the walls
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
            // Separate door color (sage green)
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
