using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WeaponGraybox.Editor
{
    public static class GrayboxBuilder
    {
        public const string ScenePath = "Assets/Graybox/Scenes/CombatGraybox.unity";
        [MenuItem("Graybox/Create or Open Test Scene")]
        public static void Prepare()
        {
            Directory.CreateDirectory("Assets/Graybox/Scenes");
            Directory.CreateDirectory("Assets/Graybox/Settings");
            Directory.CreateDirectory("Assets/Graybox/Resources");
            const string materialPath = "Assets/Graybox/Resources/GrayboxSprite.mat";
            if (!AssetDatabase.LoadAssetAtPath<Material>(materialPath))
                AssetDatabase.CreateAsset(new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")), materialPath);
            var settings = AssetDatabase.LoadAssetAtPath<GrayboxSettings>("Assets/Graybox/Settings/PrototypeSettings.asset");
            if (!settings)
            {
                settings = ScriptableObject.CreateInstance<GrayboxSettings>();
                AssetDatabase.CreateAsset(settings, "Assets/Graybox/Settings/PrototypeSettings.asset");
            }
            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var game = new GameObject("GrayboxGame").AddComponent<GrayboxGame>();
                game.Settings = settings;
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            else EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.productName = "Weapon Graybox";
            PlayerSettings.companyName = "Prototype";
            PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            AssetDatabase.SaveAssets();
            Debug.Log("[Graybox] Scene prepared: " + ScenePath);
        }
        [MenuItem("Graybox/Build Windows Test")]
        public static void BuildWindows()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before building.");
            Prepare();
            Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath }, locationPathName = "Builds/Windows/WeaponGraybox.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
            });
            Directory.CreateDirectory("Evidence");
            File.WriteAllText("Evidence/build-result.json", JsonUtility.ToJson(new BuildResult
            {
                result = report.summary.result.ToString(), errors = (int)report.summary.totalErrors,
                warnings = (int)report.summary.totalWarnings, bytes = report.summary.totalSize.ToString(),
                editorVersion = Application.unityVersion
            }, true));
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.Exception("Windows build failed: " + report.summary.result);
        }
        [System.Serializable] class BuildResult { public string result, bytes, editorVersion; public int errors, warnings; }
    }
}
