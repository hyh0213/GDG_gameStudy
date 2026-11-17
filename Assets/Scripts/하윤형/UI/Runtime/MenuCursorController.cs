using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DarkMaze.UI
{
    /// <summary>Scene-scoped native cursor. Uses the top UI raycast, not overlapping hover events.</summary>
    [DisallowMultipleComponent]
    public sealed class MenuCursorController : MonoBehaviour
    {
        public enum CursorKind { Arrow, Select }

        [SerializeField] private Texture2D arrowTexture;
        [SerializeField] private Vector2 arrowHotspot = new Vector2(4, 2);
        [SerializeField] private Texture2D selectTexture;
        [SerializeField] private Vector2 selectHotspot = new Vector2(12, 3);

        private static MenuCursorController owner;
        private readonly List<RaycastResult> hits = new List<RaycastResult>();
        private EventSystem cachedEvents;
        private PointerEventData pointer;
        private bool applied;

        public CursorKind CurrentKind { get; private set; }

        private void OnEnable()
        {
            if (owner != null && owner != this)
            {
                Debug.LogWarning("Only one menu cursor should be active. The duplicate was disabled.", this);
                enabled = false;
                return;
            }
            owner = this;
        }

        private void Update()
        {
            if (owner != this) return;
            if (!Application.isFocused || Mouse.current == null || !Cursor.visible || Cursor.lockState != CursorLockMode.None)
            {
                ReleaseCursor();
                return;
            }
            Vector2 position = Mouse.current.position.ReadValue();
            if (position.x < 0 || position.y < 0 || position.x >= Screen.width || position.y >= Screen.height)
            {
                ReleaseCursor();
                return;
            }
            ApplyCursor(GetCursorKindAt(position));
        }

        /// <summary>Read-only hit testing, also useful for verifying the menu without moving the OS mouse.</summary>
        public CursorKind GetCursorKindAt(Vector2 screenPosition)
        {
            EventSystem events = EventSystem.current;
            if (events == null || events.gameObject.scene != gameObject.scene)
                return CursorKind.Arrow;
            if (cachedEvents != events)
            {
                cachedEvents = events;
                pointer = new PointerEventData(events);
            }
            pointer.Reset();
            pointer.position = screenPosition;
            hits.Clear();
            events.RaycastAll(pointer, hits);
            // A modal's backdrop or a disabled control must NOT reveal a clickable control below it.
            return hits.Count > 0 && IsInteractive(hits[0].gameObject) ? CursorKind.Select : CursorKind.Arrow;
        }

        public static bool IsInteractive(GameObject hit)
        {
            if (hit == null || !hit.activeInHierarchy || !AllowsInteraction(hit.transform)) return false;
            Selectable control = hit.GetComponentInParent<Selectable>();
            if (control != null)
                return control.isActiveAndEnabled && control.IsInteractable();
            MenuCursorTarget custom = hit.GetComponentInParent<MenuCursorTarget>();
            return custom != null && custom.isActiveAndEnabled && custom.InteractionEnabled;
        }

        private static bool AllowsInteraction(Transform current)
        {
            while (current != null)
            {
                bool ignoreParents = false;
                foreach (CanvasGroup group in current.GetComponents<CanvasGroup>())
                {
                    if (!group.enabled) continue;
                    if (!group.interactable || !group.blocksRaycasts) return false;
                    ignoreParents |= group.ignoreParentGroups;
                }
                if (ignoreParents) break;
                current = current.parent;
            }
            return true;
        }

        private void ApplyCursor(CursorKind kind)
        {
            if (applied && CurrentKind == kind) return;
            Texture2D texture = kind == CursorKind.Select ? selectTexture : arrowTexture;
            Vector2 hotspot = kind == CursorKind.Select ? selectHotspot : arrowHotspot;
            Cursor.SetCursor(texture, texture != null ? hotspot : Vector2.zero, CursorMode.Auto);
            CurrentKind = kind;
            applied = true;
        }

        private void ReleaseCursor()
        {
            if (!applied) return;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            CurrentKind = CursorKind.Arrow;
            applied = false;
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused && owner == this) ReleaseCursor();
        }

        private void OnDisable()
        {
            if (owner != this) return;
            ReleaseCursor();
            owner = null;
        }
    }
}
