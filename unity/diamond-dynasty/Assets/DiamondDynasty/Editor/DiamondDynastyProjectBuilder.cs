using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DiamondDynasty.Editor
{
    /// <summary>Creates reproducible configuration, scene, and sprite import settings.</summary>
    public static class DiamondDynastyProjectBuilder
    {
        private const string ConfigPath = "Assets/Resources/DiamondDynasty/AtBatConfig.asset";
        private const string ScenePath = "Assets/Scenes/Main.unity";

        /// <summary>Builds all generated Unity assets for the first playable slice.</summary>
        [MenuItem("Diamond Dynasty/Build First Vertical Slice")]
        public static void BuildFirstVerticalSlice()
        {
            ConfigureSprites();
            var config = CreateOrUpdateConfig();
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<Camera>();
            var gameObject = new GameObject("Diamond Dynasty Game");
            var controller = gameObject.AddComponent<DiamondDynastyController>();
            var serializedController = new SerializedObject(controller);
            serializedController.FindProperty("config").objectReferenceValue = config;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            PlayerSettings.defaultScreenWidth = config.NativeWidth * 3;
            PlayerSettings.defaultScreenHeight = config.NativeHeight * 3;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Diamond Dynasty first vertical slice generated successfully.");
        }

        /// <summary>Enables the installed Coplay package to serve MCP when the editor opens.</summary>
        public static void EnableMcpAutoStart()
        {
            EditorPrefs.SetBool("MCPForUnity.AutoStartOnLoad", true);
            Debug.Log("Coplay Unity MCP auto-start enabled for future editor sessions.");
        }

        private static AtBatConfig CreateOrUpdateConfig()
        {
            Directory.CreateDirectory("Assets/Resources/DiamondDynasty");
            var config = AssetDatabase.LoadAssetAtPath<AtBatConfig>(ConfigPath);
            if (config == null)
            {
                config = AtBatConfig.CreatePrototypeDefaults();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }
            EditorUtility.SetDirty(config);
            return config;
        }

        private static void ConfigureSprites()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/DiamondDynasty/Art" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 1f;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }
    }
}
