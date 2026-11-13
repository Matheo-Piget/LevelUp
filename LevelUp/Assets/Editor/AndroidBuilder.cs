#nullable enable
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LevelUp.EditorTools
{
    /// <summary>
    /// Build Android en un clic (menu LevelUp > Build Android APK) ou en
    /// batchmode : Unity.exe -batchmode -quit -projectPath ... -executeMethod
    /// LevelUp.EditorTools.AndroidBuilder.BuildApk
    /// Cible : ARM64 / IL2CPP (Galaxy S25 et tout appareil 64 bits récent).
    /// </summary>
    public static class AndroidBuilder
    {
        private const string DefaultOutput = "Build/Android/LevelUp.apk";

        [MenuItem("LevelUp/Build Android APK")]
        public static void BuildApkFromMenu()
        {
            BuildApk();
        }

        public static void BuildApk()
        {
            string output = GetArg("-output") ?? DefaultOutput;
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);

            // Verrouille la config attendue même si ProjectSettings a dérivé.
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = false;

            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            BuildPlayerOptions options = new()
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[AndroidBuilder] APK OK : {output} " +
                          $"({summary.totalSize / (1024 * 1024)} Mo, {summary.totalTime.TotalMinutes:F1} min)");
            }
            else
            {
                Debug.LogError($"[AndroidBuilder] Build échoué : {summary.result}, " +
                               $"{summary.totalErrors} erreur(s).");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private const string DefaultAabOutput = "Build/Android/LevelUp.aab";

        /// <summary>
        /// Build Play Store : App Bundle (.aab) signé avec TA clé. Rien de secret
        /// n'est écrit dans le projet : la clé et ses mots de passe viennent de
        /// variables d'environnement, et les réglages sont remis en l'état après.
        ///   LEVELUP_KEYSTORE       chemin du fichier .keystore
        ///   LEVELUP_KEYSTORE_PASS  mot de passe du fichier
        ///   LEVELUP_KEY_ALIAS      alias de la clé
        ///   LEVELUP_KEY_PASS       mot de passe de la clé (défaut : celui du fichier)
        /// Batchmode : -executeMethod LevelUp.EditorTools.AndroidBuilder.BuildAab
        /// (voir tools/build-aab.ps1).
        /// </summary>
        public static void BuildAab()
        {
            string? keystore = Environment.GetEnvironmentVariable("LEVELUP_KEYSTORE");
            string? storePass = Environment.GetEnvironmentVariable("LEVELUP_KEYSTORE_PASS");
            string? alias = Environment.GetEnvironmentVariable("LEVELUP_KEY_ALIAS");
            string? keyPass = Environment.GetEnvironmentVariable("LEVELUP_KEY_PASS");
            if (string.IsNullOrEmpty(keyPass)) keyPass = storePass;

            if (string.IsNullOrEmpty(keystore) || string.IsNullOrEmpty(storePass) || string.IsNullOrEmpty(alias))
            {
                Fail("clé de signature absente : définis LEVELUP_KEYSTORE, LEVELUP_KEYSTORE_PASS et LEVELUP_KEY_ALIAS.");
                return;
            }
            if (!File.Exists(keystore))
            {
                Fail($"fichier de clé introuvable : {keystore}");
                return;
            }

            string output = GetArg("-output") ?? DefaultAabOutput;
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);

            bool oldCustom = PlayerSettings.Android.useCustomKeystore;
            string oldStore = PlayerSettings.Android.keystoreName;
            string oldAlias = PlayerSettings.Android.keyaliasName;
            bool oldBundle = EditorUserBuildSettings.buildAppBundle;
            try
            {
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = Path.GetFullPath(keystore);
                PlayerSettings.Android.keystorePass = storePass;
                PlayerSettings.Android.keyaliasName = alias;
                PlayerSettings.Android.keyaliasPass = keyPass;
                EditorUserBuildSettings.buildAppBundle = true;

                BuildPlayerOptions options = new()
                {
                    scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                    locationPathName = output,
                    target = BuildTarget.Android,
                    options = BuildOptions.None
                };

                BuildSummary summary = BuildPipeline.BuildPlayer(options).summary;
                if (summary.result == BuildResult.Succeeded)
                {
                    Debug.Log($"[AndroidBuilder] AAB OK : {output} (version {PlayerSettings.bundleVersion}, " +
                              $"code {PlayerSettings.Android.bundleVersionCode}, " +
                              $"{summary.totalSize / (1024 * 1024)} Mo, {summary.totalTime.TotalMinutes:F1} min)");
                }
                else
                {
                    Fail($"build échoué : {summary.result}, {summary.totalErrors} erreur(s).");
                }
            }
            finally
            {
                // Le chemin de la clé et l'alias ne restent pas dans ProjectSettings.
                PlayerSettings.Android.useCustomKeystore = oldCustom;
                PlayerSettings.Android.keystoreName = oldStore;
                PlayerSettings.Android.keyaliasName = oldAlias;
                PlayerSettings.Android.keystorePass = "";
                PlayerSettings.Android.keyaliasPass = "";
                EditorUserBuildSettings.buildAppBundle = oldBundle;
            }
        }

        private static void Fail(string message)
        {
            Debug.LogError("[AndroidBuilder] AAB : " + message);
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }

        private const string DefaultWindowsOutput = "Build/Windows/LevelUp.exe";

        [MenuItem("LevelUp/Build Windows")]
        public static void BuildWindowsFromMenu()
        {
            BuildWindows();
        }

        /// <summary>
        /// Build PC (Windows 64 bits) — même code et même version que l'APK, donc
        /// compatible en ligne avec les téléphones. Batchmode :
        /// -executeMethod LevelUp.EditorTools.AndroidBuilder.BuildWindows
        /// </summary>
        public static void BuildWindows()
        {
            string output = GetArg("-output") ?? DefaultWindowsOutput;
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);

            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            BuildPlayerOptions options = new()
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[AndroidBuilder] Windows OK : {output} " +
                          $"({summary.totalSize / (1024 * 1024)} Mo, {summary.totalTime.TotalMinutes:F1} min)");
            }
            else
            {
                Debug.LogError($"[AndroidBuilder] Build Windows échoué : {summary.result}, " +
                               $"{summary.totalErrors} erreur(s).");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static string? GetArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name) return args[i + 1];
            }
            return null;
        }
    }
}
