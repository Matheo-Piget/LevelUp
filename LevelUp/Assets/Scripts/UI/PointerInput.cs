using UnityEngine;
using UnityEngine.InputSystem;

namespace LevelUp.UI
{
    /// <summary>État du pointeur principal (doigt ou souris) pour l'image en cours.</summary>
    public readonly struct PointerState
    {
        /// <summary>Position écran, en pixels.</summary>
        public readonly Vector2 Position;

        /// <summary>L'appui a commencé à cette image.</summary>
        public readonly bool Pressed;

        /// <summary>Le pointeur est maintenu appuyé.</summary>
        public readonly bool Held;

        /// <summary>L'appui s'est terminé à cette image.</summary>
        public readonly bool Released;

        public PointerState(Vector2 position, bool pressed, bool held, bool released)
        {
            Position = position;
            Pressed = pressed;
            Held = held;
            Released = released;
        }
    }

    /// <summary>
    /// Lecture unique du pointeur pour tout le jeu : les gestes
    /// (<see cref="GameInputController"/>) et le survol (<see cref="HandView"/>)
    /// lisent la même source, tactile prioritaire puis souris.
    ///
    /// Une source scriptée peut la remplacer (<see cref="Override"/>) : le
    /// harnais de développement s'en sert pour rejouer de vrais gestes (toucher,
    /// glisser) et vérifier l'input sans appareil. Aucun effet en jeu normal.
    /// </summary>
    public static class PointerInput
    {
        /// <summary>Source scriptée ; null = périphériques réels. Retourner null = aucun pointeur.</summary>
        public static System.Func<PointerState?>? Override { get; set; }

        /// <summary>Dernière image où un pointeur a été appuyé, relâché ou déplacé (rendu à la demande).</summary>
        public static int LastActivityFrame { get; private set; } = -1;

        private static Vector2 _lastPosition;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Override = null;
            LastActivityFrame = -1;
            _lastPosition = default;
        }

        /// <summary>État du pointeur principal ; faux si aucun pointeur n'est disponible.</summary>
        public static bool TryRead(out PointerState state)
        {
            if (Override != null)
            {
                PointerState? scripted = Override();
                state = scripted ?? default;
                if (scripted.HasValue) Track(state);
                return scripted.HasValue;
            }

            // Tactile prioritaire dès qu'un doigt est actif : certains Android
            // exposent un périphérique Mouse fantôme qui masquerait le toucher
            // si la souris était testée en premier.
            var touch = Touchscreen.current?.primaryTouch;
            if (touch != null && (touch.press.isPressed || touch.press.wasReleasedThisFrame))
            {
                state = new PointerState(touch.position.ReadValue(), touch.press.wasPressedThisFrame,
                    touch.press.isPressed, touch.press.wasReleasedThisFrame);
                Track(state);
                return true;
            }

            Mouse? mouse = Mouse.current;
            if (mouse != null)
            {
                state = new PointerState(mouse.position.ReadValue(), mouse.leftButton.wasPressedThisFrame,
                    mouse.leftButton.isPressed, mouse.leftButton.wasReleasedThisFrame);
                Track(state);
                return true;
            }

            state = default;
            return false;
        }

        /// <summary>
        /// Position à utiliser pour le survol : le doigt posé, sinon la souris.
        /// Faux si rien ne pointe l'écran (tactile sans doigt posé).
        /// </summary>
        public static bool TryReadHover(out Vector2 position)
        {
            if (Override != null)
            {
                PointerState? scripted = Override();
                position = scripted?.Position ?? default;
                return scripted.HasValue;
            }

            Touchscreen? touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.press.isPressed)
            {
                position = touchscreen.primaryTouch.position.ReadValue();
                return true;
            }

            Mouse? mouse = Mouse.current;
            if (mouse != null)
            {
                position = mouse.position.ReadValue();
                return true;
            }

            position = default;
            return false;
        }

        private static void Track(PointerState state)
        {
            if (state.Pressed || state.Held || state.Released
                || (state.Position - _lastPosition).sqrMagnitude > 1f)
            {
                LastActivityFrame = Time.frameCount;
            }
            _lastPosition = state.Position;
        }
    }
}
