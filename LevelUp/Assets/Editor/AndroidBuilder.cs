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
