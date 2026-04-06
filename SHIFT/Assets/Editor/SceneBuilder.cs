#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// One-click scene builder for SHIFT Phase 1 test scene.
/// Menu: SHIFT → Build Test Scene
/// </summary>
public static class SceneBuilder
{
    // Layer numbers (matches what we set in Player Settings)
    private const int LAYER_INTERACTABLE = 6;
    private const int LAYER_SURFACE      = 7;

    [MenuItem("SHIFT/Build Test Scene")]
    public static void BuildTestScene()
    {
        // ── Create / clear the scene ──────────────────────────────────────────────
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // ── Tags & Layers ─────────────────────────────────────────────────────────
        EnsureTag("ShiftObject");
        EnsureTag("Surface");
        EnsureTag("GoalZone");
        EnsureLayer("Interactable", LAYER_INTERACTABLE);
        EnsureLayer("Surface",      LAYER_SURFACE);

        // ── Directional Light ─────────────────────────────────────────────────────
        var light = GameObject.Find("Directional Light");
        if (light != null) light.transform.rotation = Quaternion.Euler(50, -30, 0);

        // ── Floor ─────────────────────────────────────────────────────────────────
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.localScale = new Vector3(3, 1, 3);   // 30×30 units
        floor.layer = LAYER_SURFACE;
        floor.tag   = "Surface";

        // ── Walls ─────────────────────────────────────────────────────────────────
        CreateWall("Wall_North", new Vector3( 0,  2.5f,  15), new Vector3(30, 5, 1));
        CreateWall("Wall_South", new Vector3( 0,  2.5f, -15), new Vector3(30, 5, 1));
        CreateWall("Wall_East",  new Vector3( 15, 2.5f,   0), new Vector3(1,  5, 30));
        CreateWall("Wall_West",  new Vector3(-15, 2.5f,   0), new Vector3(1,  5, 30));

        // ── ShiftObjects (puzzle cubes) ───────────────────────────────────────────
        CreateShiftCube("ShiftCube_A", new Vector3(-3, 0.5f,  3), Color.red);
        CreateShiftCube("ShiftCube_B", new Vector3( 2, 0.5f,  5), Color.cyan);
        CreateShiftCube("ShiftCube_C", new Vector3( 0, 0.5f, -2), Color.yellow);

        // ── Goal Zone ─────────────────────────────────────────────────────────────
        var goalGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        goalGO.name = "GoalZone";
        goalGO.transform.position   = new Vector3(10, 0.05f, 10);
        goalGO.transform.localScale = new Vector3(2, 0.05f, 2);
        goalGO.tag = "GoalZone";
        goalGO.layer = LAYER_SURFACE;
        SetColour(goalGO, Color.green, 0.35f);
        Undo.RegisterCreatedObjectUndo(goalGO, "Create GoalZone");
        // Remove Collider, re-add as trigger
        Object.DestroyImmediate(goalGO.GetComponent<Collider>());
        var trigger = goalGO.AddComponent<CapsuleCollider>();
        trigger.isTrigger = true;
        trigger.radius = 1f;
        trigger.height = 1f;
        goalGO.AddComponent<GoalZone>();

        // ── Player ────────────────────────────────────────────────────────────────
        var player = new GameObject("Player");
        player.transform.position = new Vector3(0, 1, -8);
        Undo.RegisterCreatedObjectUndo(player, "Create Player");

        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0, 0, 0);

        var pc = player.AddComponent<PlayerController>();

        // ── Camera → child of Player ───────────────────────────────────────────────
        var cam = Camera.main?.gameObject ?? new GameObject("Main Camera");
        cam.tag = "MainCamera";
        cam.transform.SetParent(player.transform);
        cam.transform.localPosition = new Vector3(0, 0.8f, 0);
        cam.transform.localRotation = Quaternion.identity;

        pc.cameraTransform = cam.transform;

        var pickup = player.AddComponent<PerspectivePickup>();
        pickup.interactableLayer = LayerMask.GetMask("Interactable");
        pickup.surfaceLayer      = LayerMask.GetMask("Surface");

        // ── GameManager ───────────────────────────────────────────────────────────
        var gm = new GameObject("GameManager");
        gm.AddComponent<GameManager>();
        gm.AddComponent<TestBootstrap>();   // auto-starts game on Play
        Undo.RegisterCreatedObjectUndo(gm, "Create GameManager");

        // ── AudioManager ──────────────────────────────────────────────────────────
        var am = new GameObject("AudioManager");
        am.AddComponent<AudioManager>();
        Undo.RegisterCreatedObjectUndo(am, "Create AudioManager");

        // ── Save scene ────────────────────────────────────────────────────────────
        string scenePath = "Assets/Game.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.Refresh();

        Debug.Log("[SceneBuilder] ✅ Test scene built and saved to Assets/Game.unity");
        EditorUtility.DisplayDialog("SHIFT Scene Builder",
            "✅ Test scene built!\n\nPress Play to test the perspective scaling mechanic.\nWASD = move, Mouse = look, Left Click = pick up / drop.",
            "Let's go!");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────

    private static void CreateWall(string name, Vector3 pos, Vector3 scale)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.position   = pos;
        wall.transform.localScale = scale;
        wall.layer = LAYER_SURFACE;
        wall.tag   = "Surface";
        SetColour(wall, new Color(0.55f, 0.55f, 0.55f));
        Undo.RegisterCreatedObjectUndo(wall, $"Create {name}");
    }

    private static void CreateShiftCube(string name, Vector3 pos, Color colour)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name  = name;
        cube.transform.position = pos;
        cube.layer = LAYER_INTERACTABLE;
        cube.tag   = "ShiftObject";
        SetColour(cube, colour);

        var rb = cube.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.useGravity = true;

        cube.AddComponent<ShiftObject>();

        Undo.RegisterCreatedObjectUndo(cube, $"Create {name}");
    }

    private static void SetColour(GameObject go, Color colour, float alpha = 1f)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        colour.a = alpha;
        mat.color = colour;
        if (alpha < 1f)
        {
            mat.SetFloat("_Surface", 1);   // Transparent
            mat.renderQueue = 3000;
        }
        r.sharedMaterial = mat;
    }

    private static void EnsureTag(string tag)
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tags = tagManager.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();
    }

    private static void EnsureLayer(string layerName, int index)
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        var prop = layers.GetArrayElementAtIndex(index);
        if (prop.stringValue == layerName) return;
        prop.stringValue = layerName;
        tagManager.ApplyModifiedProperties();
    }
}
#endif
