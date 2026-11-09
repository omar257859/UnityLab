using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

// ONE-TIME setup for the labs project. Menu: Labs > Create Level 1
// It sets up the art import settings, layers, animations and prefabs, then lays out Level1 like in the labs.
// After that, Level1 is a normal scene: edit it by hand in the next labs. This tool never overwrites it.
public static class LevelBuilder
{
    const string ScenePath = "Assets/Scenes/Level1.unity";

    static int groundLayer;
    static PhysicsMaterial2D roughMaterial, bouncyMaterial;

    [MenuItem("Labs/Create Level 1")]
    public static void CreateLevel1()
    {
        if (File.Exists(ScenePath))
        {
            EditorUtility.DisplayDialog("Create Level 1",
                ScenePath + " already exists, so nothing was changed.\n\nOpen it from Assets/Scenes and keep editing it by hand.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        foreach (var folder in new[] { "Assets/Animations", "Assets/Materials", "Assets/Prefabs", "Assets/Scenes" })
            Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();

        SetupTagsAndLayers();
        SetupArt();
        CreatePhysicsMaterials();
        var animator = CreateBobbyAnimator();
        BuildLevel(animator);
    }

    // ================================================================== project setup

    static void SetupTagsAndLayers()
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        // Lab 6: walls tagged "Wall" make the enemy turn around ("Player" tag is built in)
        var tags = tagManager.FindProperty("tags");
        if (!Enumerable.Range(0, tags.arraySize).Any(i => tags.GetArrayElementAtIndex(i).stringValue == "Wall"))
        {
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = "Wall";
        }

        // Lab 2: "Ground" layer for everything the player can stand on
        var layers = tagManager.FindProperty("layers");
        groundLayer = -1;
        for (int i = 0; i < 32 && groundLayer < 0; i++)
            if (layers.GetArrayElementAtIndex(i).stringValue == "Ground")
                groundLayer = i;
        for (int i = 6; i < 32 && groundLayer < 0; i++)
            if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
            {
                layers.GetArrayElementAtIndex(i).stringValue = "Ground";
                groundLayer = i;
            }

        // Sorting layers: Background (sky, clouds, tree) < Default (ground) < Foreground (player, lake)
        AddSortingLayer(tagManager, "Background", true, 1001);
        AddSortingLayer(tagManager, "Foreground", false, 1002);

        tagManager.ApplyModifiedPropertiesWithoutUndo();
    }

    static void AddSortingLayer(SerializedObject tagManager, string name, bool first, int id)
    {
        var sortingLayers = tagManager.FindProperty("m_SortingLayers");
        for (int i = 0; i < sortingLayers.arraySize; i++)
            if (sortingLayers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name)
                return;
        int index = first ? 0 : sortingLayers.arraySize;
        sortingLayers.InsertArrayElementAtIndex(index);
        var layer = sortingLayers.GetArrayElementAtIndex(index);
        layer.FindPropertyRelative("name").stringValue = name;
        layer.FindPropertyRelative("uniqueID").intValue = id;
    }

    // Lab 2: import settings for pixel art (Single / Multiple sprite mode, sliced sheets)
    static void SetupArt()
    {
        var center = new Vector2(0.5f, 0.5f);
        var bottom = new Vector2(0.5f, 0f);

        // Bobby: 3x3 sprite sheet, cells of 16x16 with 1 pixel between them
        string[,] bobby =
        {
            { "Idle 0", "Run 0", "Run 1" },
            { "Idle 1", "Jump 0", "Fall 0" },
            { "Idle 2", "Run 2", "Run 3" },
        };
        var frames = new SpriteMetaData[9];
        for (int row = 0; row < 3; row++)
            for (int col = 0; col < 3; col++)
                frames[row * 3 + col] = Frame(bobby[row, col], col * 17, (2 - row) * 17, 16, 16, bottom);
        Import("Assets/Art/Characters/Player.png", 16, frames);

        Import("Assets/Art/Characters/Slime.png", 16);
        Import("Assets/Art/Environment/Ground.png", 64, tiled: true);
        Import("Assets/Art/Environment/Sky.png", 16, tiled: true);
        Import("Assets/Art/Environment/Clouds.png", 16, tiled: true);
        Import("Assets/Art/Environment/Water.png", 50, tiled: true);
        Import("Assets/Art/Environment/ExplodedBlock.png", 16);

        // pieces we use from the terrain sheet (x, y measured from the bottom-left of the image)
        Import("Assets/Art/Environment/Terrain.png", 16, new[]
        {
            Frame("Grass Platform", 112, 160, 16, 16, center),
            Frame("Bouncy Platform", 112, 32, 16, 16, center),
            Frame("Brick", 272, 96, 16, 16, center),
            Frame("Stone", 192, 80, 16, 16, center),
            Frame("Trapdoor", 272, 168, 48, 8, center),
        }, tiled: true);
        Import("Assets/Art/Environment/Trees.png", 16, new[] { Frame("Tree", 158, 16, 124, 116, bottom) });

        foreach (var file in new[] { "Mushroom", "Bush Red", "Bush Green", "Flower" })
            Import("Assets/Art/Decoration/" + file + ".png", 16, pivot: bottom);
    }

    static SpriteMetaData Frame(string name, int x, int y, int w, int h, Vector2 pivot) => new SpriteMetaData
    {
        name = name,
        rect = new Rect(x, y, w, h),
        alignment = (int)SpriteAlignment.Custom,
        pivot = pivot,
    };

    static void Import(string path, int pixelsPerUnit, SpriteMetaData[] sheet = null, bool tiled = false, Vector2? pivot = null)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = tiled ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
        if (pivot.HasValue)
        {
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot.Value;
        }
        importer.SetTextureSettings(settings);

        importer.spriteImportMode = sheet == null ? SpriteImportMode.Single : SpriteImportMode.Multiple;
        if (sheet != null)
            importer.spritesheet = sheet;
        importer.SaveAndReimport();
    }

    static Sprite Art(string path, string spriteName = null)
    {
        if (spriteName == null)
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First(s => s.name == spriteName);
    }

    // Lab 4: friction and bounciness
    static void CreatePhysicsMaterials()
    {
        roughMaterial = new PhysicsMaterial2D("Rough Material") { friction = 0.6f, bounciness = 0 };
        AssetDatabase.CreateAsset(roughMaterial, "Assets/Materials/Rough Material.physicsMaterial2D");
        bouncyMaterial = new PhysicsMaterial2D("Bounciness") { friction = 0.5f, bounciness = 0.5f };
        AssetDatabase.CreateAsset(bouncyMaterial, "Assets/Materials/Bounciness.physicsMaterial2D");
    }

    // ================================================================== Lab 3: animations

    static AnimationClip Clip(string name, float samples, bool loop, params string[] frameNames)
    {
        var sprites = frameNames.Select(f => Art("Assets/Art/Characters/Player.png", f)).ToArray();
        var clip = new AnimationClip { frameRate = samples };
        var keys = new ObjectReferenceKeyframe[sprites.Length + 1]; // last frame repeated so it shows for a full frame
        for (int i = 0; i < keys.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / samples, value = sprites[Mathf.Min(i, sprites.Length - 1)] };
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, "Assets/Animations/" + name + ".anim");
        return clip;
    }

    // Lab 3 note: uncheck Has Exit Time and Fixed Duration, Transition Duration = 0
    static AnimatorStateTransition Instant(AnimatorStateTransition t)
    {
        t.hasExitTime = false;
        t.hasFixedDuration = false;
        t.duration = 0;
        return t;
    }

    static AnimatorController CreateBobbyAnimator()
    {
        var idleClip = Clip("BobbyIdle", 3, true, "Idle 0", "Idle 1");
        var runClip = Clip("BobbyRun", 8, true, "Run 0", "Run 1", "Run 2", "Run 3");
        var jumpClip = Clip("BobbyJump", 3, false, "Jump 0");
        var fallClip = Clip("BobbyFall", 3, false, "Fall 0");

        var controller = AnimatorController.CreateAnimatorControllerAtPath("Assets/Animations/Bobby.controller");
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Height", AnimatorControllerParameterType.Float);

        var states = controller.layers[0].stateMachine;
        var idle = states.AddState("BobbyIdle", new Vector3(300, 0));
        idle.motion = idleClip;
        var run = states.AddState("BobbyRun", new Vector3(550, 0));
        run.motion = runClip;
        states.defaultState = idle;

        // Exercise #09: Jumping Motion blend tree on "Height" (Fall = -1, Jump = 1)
        var tree = new BlendTree
        {
            name = "JumpingMotion",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "Height",
            useAutomaticThresholds = false,
            hideFlags = HideFlags.HideInHierarchy,
        };
        AssetDatabase.AddObjectToAsset(tree, controller);
        tree.AddChild(fallClip, -1f);
        tree.AddChild(jumpClip, 1f);
        var jumping = states.AddState("JumpingMotion", new Vector3(300, 120));
        jumping.motion = tree;

        // Exercise #08: Idle <-> Run on Speed
        Instant(idle.AddTransition(run)).AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        Instant(run.AddTransition(idle)).AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        // Exercise #09: Any State -> JumpingMotion when Grounded is false, back to Idle when true
        var toJumping = Instant(states.AddAnyStateTransition(jumping));
        toJumping.AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
        toJumping.canTransitionToSelf = false;
        Instant(jumping.AddTransition(idle)).AddCondition(AnimatorConditionMode.If, 0, "Grounded");

        return controller;
    }

    // ================================================================== scene helpers

    static GameObject Place(string name, Sprite sprite, float x, float y, string sortingLayer = "Default", int order = 0)
    {
        var go = new GameObject(name);
        go.transform.position = new Vector3(x, y, 0);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = order;
        return go;
    }

    // A sprite repeated (Draw Mode = Tiled) to fill a rectangle given by its edges in world units
    static GameObject Tiled(string name, Sprite sprite, float left, float right, float bottom, float top,
        string sortingLayer = "Default", int order = 0)
    {
        var go = Place(name, sprite, (left + right) / 2, (bottom + top) / 2, sortingLayer, order);
        var sr = go.GetComponent<SpriteRenderer>();
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(right - left, top - bottom);
        return go;
    }

    // Lab 2 Exercise #02: solid ground = sprite + BoxCollider2D on the Ground layer
    static GameObject Ground(string name, Sprite sprite, float left, float right, float bottom, float top,
        PhysicsMaterial2D material = null)
    {
        var go = Tiled(name, sprite, left, right, bottom, top);
        go.layer = groundLayer;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(right - left, top - bottom);
        box.sharedMaterial = material != null ? material : roughMaterial;
        return go;
    }

    static GameObject InvisibleWall(string name, float x)
    {
        var go = new GameObject(name);
        go.tag = "Wall";
        go.transform.position = new Vector3(x, 5, 0);
        go.AddComponent<BoxCollider2D>().size = new Vector2(1, 20);
        return go;
    }

    // Lab 2: "give it a color" so an empty GameObject is visible in the Scene view
    static void Icon(GameObject go)
    {
        var icon = EditorGUIUtility.IconContent("sv_icon_dot1_pix16_gizmo").image as Texture2D;
        if (icon != null)
            EditorGUIUtility.SetIconForObject(go, icon);
    }

    static GameObject Child(GameObject parent, string name, float localX, float localY)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = new Vector3(localX, localY, 0);
        Icon(go);
        return go;
    }

    // ================================================================== Level 1

    static void BuildLevel(AnimatorController bobbyAnimator)
    {
        const string env = "Assets/Art/Environment/";
        const string deco = "Assets/Art/Decoration/";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---------- background ----------
        Tiled("SKY", Art(env + "Sky.png"), -12, 76, -4, 15, "Background", 0);
        Tiled("CLOUDS", Art(env + "Clouds.png"), -12, 76, -1.5f, 9.3f, "Background", 1);
        Place("Tree", Art(env + "Trees.png", "Tree"), 6, 0, "Background", 2);
        Place("Bush 1", Art(deco + "Bush Green.png"), -5, 0, "Background", 3);
        Place("Bush 2", Art(deco + "Bush Red.png"), 13, 0, "Background", 3);
        Place("Flowers", Art(deco + "Flower.png"), 34, 0, "Background", 3);

        // ---------- Lab 2: ground (Ground layer + BoxCollider2D, Lab 4: Rough Material) ----------
        var grass = Art(env + "Terrain.png", "Grass Platform");
        Ground("Main Platform", Art(env + "Ground.png"), -10, 20, -2, 0);
        Ground("Cliff", Art(env + "Ground.png"), 28, 42, -2, 0);
        Ground("Cliff 2", Art(env + "Ground.png"), 46, 60, -2, 0);
        Ground("Goal Platform", Art(env + "Ground.png"), 64, 72, 3, 5);
        InvisibleWall("Invisible Left Wall", -10.5f);
        InvisibleWall("Invisible Right Wall", 72.5f);

        // ---------- Lab 2: player "Bobby" ----------
        var bobby = Place("Bobby", Art("Assets/Art/Characters/Player.png", "Idle 0"), -7, 0.5f, "Foreground", 2);
        bobby.tag = "Player";
        var box = bobby.AddComponent<BoxCollider2D>(); // fit the box to the character
        box.size = new Vector2(0.8f, 0.95f);
        box.offset = new Vector2(0, 0.48f);
        var body = bobby.AddComponent<Rigidbody2D>();
        body.gravityScale = 2;
        body.interpolation = RigidbodyInterpolation2D.Interpolate; // Exercise #05
        body.constraints = RigidbodyConstraints2D.FreezeRotation; // freeze rotation Z
        var groundCheck = Child(bobby, "groundCheck", 0, 0); // at the feet

        var controller = bobby.AddComponent<PlayerController>(); // Exercise #06
        controller.moveSpeed = 7;
        controller.jumpHeight = 11;
        controller.Spacebar = KeyCode.Space;
        controller.L = KeyCode.LeftArrow;
        controller.R = KeyCode.RightArrow;
        controller.groundCheck = groundCheck.transform;
        controller.groundCheckRadius = 0.2f;
        controller.whatIsGround = 1 << groundLayer;
        bobby.AddComponent<Animator>().runtimeAnimatorController = bobbyAnimator; // Lab 3
        bobby.AddComponent<PlayerStats>(); // Lab 6

        // ---------- Lab 2 + 4: camera (orthographic, follow + zoom) ----------
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(-2.9f, 1, -10);
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(160, 236, 232, 255);
        cameraObject.AddComponent<AudioListener>();
        var follow = cameraObject.AddComponent<CameraFollow>(); // Exercise #12
        follow.Target = bobby.transform;
        follow.Cameraspeed = 6;
        follow.minX = -2.9f;
        follow.maxX = 65;
        follow.minY = 1;
        follow.maxY = 4;
        cameraObject.AddComponent<CameraZoom>().ZoomSpeed = 3; // Exercise #13

        // ---------- Lab 4: friction and bounciness platforms ----------
        Ground("Bouncy Platform", Art(env + "Terrain.png", "Bouncy Platform"), 9, 12, 1.5f, 2.5f, bouncyMaterial);
        Ground("Floaty Platform 1", grass, 23, 25, 0.5f, 1.5f);

        // ---------- Lab 4 + 5: deadly lake with water glitter ----------
        var lake = Tiled("Lake", Art(env + "Water.png"), 20, 28, -2.9f, -0.2f, "Foreground", 1);
        var lakeCollider = lake.AddComponent<BoxCollider2D>();
        lakeCollider.size = new Vector2(8, 2.7f);
        lakeCollider.isTrigger = true; // Exercise #15 step 1
        lake.AddComponent<DeadlyDrop>();
        WaterGlitter(lake);

        var pit = new GameObject("Drop"); // falling below the level also respawns
        pit.transform.position = new Vector3(30, -8, 0);
        var pitCollider = pit.AddComponent<BoxCollider2D>();
        pitCollider.size = new Vector2(90, 2);
        pitCollider.isTrigger = true;
        pit.AddComponent<DeadlyDrop>();

        // ---------- Lab 5: level manager + checkpoints (prefab) ----------
        new GameObject("LevelManager").AddComponent<LevelManager>();

        var mushroom = Place("Red Mushroom", Art(deco + "Mushroom.png"), 16, 0, "Background", 4);
        var checkpoint = Child(mushroom, "Checkpoint", -1, 0.5f);
        var checkpointCollider = checkpoint.AddComponent<CircleCollider2D>();
        checkpointCollider.radius = 0.5f;
        checkpointCollider.isTrigger = true;
        checkpoint.AddComponent<Checkpoint>();
        var mushroomPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(mushroom, "Assets/Prefabs/Red Mushroom.prefab",
            InteractionMode.AutomatedAction);

        var mushroom2 = (GameObject)PrefabUtility.InstantiatePrefab(mushroomPrefab, scene);
        mushroom2.name = "Red Mushroom 2";
        mushroom2.transform.position = new Vector3(31, 0, 0);
        PrefabUtility.RecordPrefabInstancePropertyModifications(mushroom2.transform);

        // ---------- Lab 5: hinge joint trapdoor ----------
        var trapdoor = Art(env + "Terrain.png", "Trapdoor");
        var trapdoorParent = new GameObject("Trapdoor");
        var left = Ground("Left Platform", trapdoor, 42, 42.5f, -0.5f, 0);
        left.transform.SetParent(trapdoorParent.transform);
        left.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        var right = Ground("Right Platform", trapdoor, 42.5f, 45.9f, -0.5f, 0);
        right.transform.SetParent(trapdoorParent.transform);
        right.AddComponent<Rigidbody2D>(); // Dynamic
        var hinge = right.AddComponent<HingeJoint2D>();
        hinge.connectedBody = left.GetComponent<Rigidbody2D>();
        hinge.anchor = new Vector2(-1.7f, 0); // between the two platforms
        hinge.useMotor = true;
        hinge.motor = new JointMotor2D { motorSpeed = -100, maxMotorTorque = 40 };
        hinge.useLimits = true;
        hinge.limits = new JointAngleLimits2D { min = 0, max = 90 };

        // ---------- Lab 5: brick block ----------
        var brick = Place("BrickBlock", Art(env + "Terrain.png", "Brick"), -2, 3.5f);
        brick.layer = groundLayer;
        brick.AddComponent<BoxCollider2D>().size = Vector2.one;
        brick.AddComponent<BrickBreak>().explodedBlock = Art(env + "ExplodedBlock.png");

        // ---------- Lab 5 (optional): moving platform ----------
        var pointA = new GameObject("Point A");
        pointA.transform.position = new Vector3(62, 4.5f, 0);
        Icon(pointA);
        var pointB = new GameObject("Point B");
        pointB.transform.position = new Vector3(62, 0.5f, 0);
        Icon(pointB);
        var rising = Ground("Moving Platform", grass, 61, 63, 0, 1).AddComponent<RisingPlatform>();
        rising.PointA = pointA.transform;
        rising.PointB = pointB.transform;
        rising.speed = 2;

        // ---------- Lab 6: one walking enemy between two walls ----------
        var stone = Art(env + "Terrain.png", "Stone");
        Ground("Tiny Wall 1", stone, 48, 49, 0, 1).tag = "Wall";
        Ground("Tiny Wall 2", stone, 57, 58, 0, 1).tag = "Wall";

        var slime = Place("Slime", Art("Assets/Art/Characters/Slime.png"), 53, 0.5f, "Foreground", 2);
        var slimeBody = slime.AddComponent<Rigidbody2D>();
        slimeBody.gravityScale = 2;
        slimeBody.constraints = RigidbodyConstraints2D.FreezeRotation;
        var slimeCollider = slime.AddComponent<BoxCollider2D>(); // body
        slimeCollider.size = new Vector2(0.75f, 0.6f);
        slimeCollider.offset = new Vector2(0, -0.2f);
        var slimeTrigger = slime.AddComponent<BoxCollider2D>(); // detects walls and the player
        slimeTrigger.size = new Vector2(0.95f, 0.6f);
        slimeTrigger.offset = new Vector2(0, -0.2f);
        slimeTrigger.isTrigger = true;
        var enemy = slime.AddComponent<SlimeEnemy>();
        enemy.maxSpeed = 2;
        enemy.damage = 1;
        PrefabUtility.SaveAsPrefabAssetAndConnect(slime, "Assets/Prefabs/Slime.prefab", InteractionMode.AutomatedAction);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("Level1 created: " + ScenePath);
    }

    // Lab 4, Exercise #14: sparkles on the lake (built-in particle system)
    static void WaterGlitter(GameObject lake)
    {
        var go = new GameObject("Water Glitter");
        go.transform.SetParent(lake.transform, false);
        var particles = go.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = particles.main;
        main.duration = 5;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(8, 15);
        main.startSpeed = 0;
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);

        var emission = particles.emission;
        emission.rateOverTime = 10;

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(7.5f, 2.2f, 0.01f);

        var color = particles.colorOverLifetime;
        color.enabled = true;
        var lightBlue = new Color32(210, 238, 237, 255);
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(lightBlue, 0), new GradientColorKey(lightBlue, 1) },
            new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.2f), new GradientAlphaKey(1, 0.8f), new GradientAlphaKey(0, 1) });
        color.color = gradient;

        var size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(
            new Keyframe(0, 0), new Keyframe(0.3f, 1), new Keyframe(0.5f, 0.5f), new Keyframe(0.7f, 1), new Keyframe(1, 0)));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Particles/Center_starten.mat");
        renderer.sortingLayerName = "Foreground";
        renderer.sortingOrder = 3;
    }
}
