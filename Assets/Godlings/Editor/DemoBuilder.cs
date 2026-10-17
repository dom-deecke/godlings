using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Godlings.Editor
{
    public static class DemoBuilder
    {
        public const string ScenePath = "Assets/Godlings/Scenes/Demo.unity";

        [MenuItem("Godlings/Prepare playable demo")]
        public static void Prepare()
        {
            RenderingSetup.EnsureConfigured();
            Directory.CreateDirectory("Assets/Godlings/Data");
            File.Copy("Design/Data/level-sketch.json","Assets/Godlings/Data/level-sketch.json",true);
            AssetDatabase.Refresh();
            CoreChecks.PrepareAndCheck();
            Directory.CreateDirectory("Assets/Godlings/Scenes");
            AssetDatabase.Refresh();
            const string spritePath = "Assets/Godlings/Art/Fi/fi-large-side.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(spritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(0.5f,0);
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var game = new GameObject("Godlings demo").AddComponent<DemoGame>();
            game.FlatMaterial = RenderingSetup.FlatMaterial;
            game.SpriteMaterial = RenderingSetup.SpriteMaterial;
            game.Rules = AssetDatabase.LoadAssetAtPath<DemoRules>("Assets/Godlings/DemoRules.asset");
            game.LevelSketch = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Godlings/Data/level-sketch.json");
            // Leave this optional: the agreed placeholder is a synthetic fox yelp.
            game.FiHurtClip = null;
            game.MeteorSprite = ImportMeteorSprite("meteor-with-crater",new Vector2(0.5f,0.12f));
            game.MeteorShadowSprite = ImportMeteorSprite("meteor-shadow",new Vector2(0.5f,0.5f));
            game.FiSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            string[] enemyColors = { "orange", "yellow", "blue", "pink", "green", "purple" };
            game.EnemySprites = new Sprite[enemyColors.Length];
            for (int i=0;i<enemyColors.Length;i++)
            {
                string enemyPath = "Assets/Godlings/Art/Enemies/enemy-"+enemyColors[i]+".png";
                var enemyImporter = (TextureImporter)AssetImporter.GetAtPath(enemyPath);
                var enemySettings = new TextureImporterSettings();
                enemyImporter.ReadTextureSettings(enemySettings);
                enemySettings.spriteAlignment = (int)SpriteAlignment.Custom;
                // Anchor the feet, excluding the soft shadow beneath the drawing.
                enemySettings.spritePivot = new Vector2(0.46f,0.14f);
                enemyImporter.SetTextureSettings(enemySettings);
                enemyImporter.SaveAndReimport();
                game.EnemySprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(enemyPath);
                if (game.EnemySprites[i] == null) throw new Exception("Missing supplied enemy variant: "+enemyPath);
            }
            if (game.FiSprite == null || game.LevelSketch == null) throw new Exception("Demo assets are missing.");
            var level = DemoLevel.Load(game.LevelSketch);
            if (level.Floor.Count != 444 || level.EnemyStarts.Count != 4 || level.RockCenters().Length != 223)
                throw new Exception("Scene layout does not match the transcribed sketch.");
            foreach (Vector2Int center in level.RockCenters())
                foreach (Vector2Int cell in AttackPatterns.Rock(center))
                    if (!level.MeteorZone.Contains(cell)) throw new Exception("Rock escapes boss arena.");
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath,true) };
            PlayerSettings.productName = "Godlings Demo";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            AssetDatabase.SaveAssets();
            Debug.Log("GODLINGS_DEMO_SCENE_READY");
        }

        private static Sprite ImportMeteorSprite(string name, Vector2 pivot)
        {
            string path = "Assets/Godlings/Art/Meteors/"+name+".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        public static void BuildMac()
        {
            Prepare();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Mac/Godlings Demo.app",
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Mac build failed: " + report.summary.result);
            Debug.Log("GODLINGS_MAC_BUILD_PASSED");
        }

        // Mono supports building Windows x64 from this Mac. IL2CPP needs a
        // Windows editor and C++ toolchain; keep that separate from this demo.
        [MenuItem("Godlings/Build Windows demo")]
        public static void BuildWindows()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                throw new Exception("Install Windows Build Support (Mono) for this Unity editor first.");
            Prepare();
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Windows/Godlings Demo.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Windows build failed: " + report.summary.result);
            File.WriteAllText("Builds/Windows/READ-ME.txt",
                "Godlings Windows x64 demo. Keep the entire folder together.\n" +
                "Run Godlings Demo.exe. Controls and playtest notes are in PLAY-DEMO.txt.\n" +
                "Built on macOS; playtest on a Windows PC before distributing.\n");
            File.Copy("PLAY-DEMO.txt", "Builds/Windows/PLAY-DEMO.txt", true);
            Debug.Log("GODLINGS_WINDOWS_BUILD_PASSED");
        }

        public static void BuildWeb()
        {
            Prepare();
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Web",
                target = BuildTarget.WebGL,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Web build failed: " + report.summary.result);
            File.Copy("WebReview/index.html","Builds/Web/index.html",true);
            Debug.Log("GODLINGS_WEB_BUILD_PASSED");
        }
    }
}
