using UnityEngine;

namespace LevelUp.UI
{
    /// <summary>
    /// Marqueur : un appui sur cet élément d'interface (top bar, carte contrat,
    /// étapes, tri…) ne doit jamais être interprété comme un geste de jeu
    /// ("clic sur la table", pioche…). Les boutons (Selectable) sont déjà
    /// exclus d'office par <see cref="GameInputController"/>.
    /// </summary>
    public class UIInputBlocker : MonoBehaviour
    {
    }
}
