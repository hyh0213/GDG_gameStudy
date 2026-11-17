using UnityEngine;

namespace DarkMaze.UI
{
    /// <summary>Opt in custom, non-Selectable click targets. Does not implement the click itself.</summary>
    [DisallowMultipleComponent]
    public sealed class MenuCursorTarget : MonoBehaviour
    {
        [Tooltip("Turn off when the custom action is unavailable. Buttons/sliders/toggles do not need this component.")]
        [SerializeField] private bool interactionEnabled = true;

        public bool InteractionEnabled
        {
            get => interactionEnabled;
            set => interactionEnabled = value;
        }
    }
}
