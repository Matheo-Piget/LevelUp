using System;
using UnityEngine;

namespace LevelUp.UI
{
    /// <summary>
    /// Vibrations courtes (Android) : confirmation d'une action, erreur, début de
    /// son tour. Désactivables dans les options. Sans effet sur PC.
    /// Handheld.Vibrate (une seule durée, longue) ne sert que de repli avant
    /// Android 8 ; sa simple présence ajoute aussi la permission VIBRATE au manifeste.
    /// </summary>
    public static class Haptics
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Jamais vrai : garde la référence à Handheld.Vibrate dans le code compilé
        // (Unity n'ajoute la permission VIBRATE que si elle est utilisée).
        private static bool _forcePermissionReference;
#endif

        /// <summary>Tic léger : carte posée, sélection.</summary>
        public static void Light() => Vibrate(10, 50);

        /// <summary>Confirmation : contrat posé, manche gagnée.</summary>
        public static void Success() => Vibrate(28, 160);

        /// <summary>Action refusée.</summary>
        public static void Error() => Vibrate(45, 220);

        /// <summary>C'est ton tour.</summary>
        public static void Turn() => Vibrate(18, 90);

        private static void Vibrate(long milliseconds, int amplitude)
        {
            if (!GameSettings.Haptics) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_forcePermissionReference) Handheld.Vibrate();
            try
            {
                AndroidJavaObject? vibrator = GetVibrator();
                if (vibrator == null) return;
                if (_sdk >= 26)
                {
                    using AndroidJavaClass effectClass = new("android.os.VibrationEffect");
                    using AndroidJavaObject effect = effectClass.CallStatic<AndroidJavaObject>(
                        "createOneShot", milliseconds, Mathf.Clamp(amplitude, 1, 255));
                    vibrator.Call("vibrate", effect);
                }
                else
                {
                    vibrator.Call("vibrate", milliseconds);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Haptics] Vibration indisponible : {e.Message}");
                _unavailable = true;
            }
#else
            _ = milliseconds;
            _ = amplitude;
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject? _vibrator;
        private static int _sdk;
        private static bool _unavailable;

        private static AndroidJavaObject? GetVibrator()
        {
            if (_unavailable) return null;
            if (_vibrator != null) return _vibrator;
            using AndroidJavaClass version = new("android.os.Build$VERSION");
            _sdk = version.GetStatic<int>("SDK_INT");
            using AndroidJavaClass player = new("com.unity3d.player.UnityPlayer");
            using AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            if (_vibrator == null) _unavailable = true;
            return _vibrator;
        }
#endif
    }
}
