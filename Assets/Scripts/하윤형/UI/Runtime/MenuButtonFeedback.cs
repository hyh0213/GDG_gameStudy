using UnityEngine;
using UnityEngine.EventSystems;

namespace DarkMaze.UI
{
    public sealed class MenuButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        ISelectHandler, IDeselectHandler
    {
        [SerializeField] private CanvasGroup highlight;
        [SerializeField] private RectTransform arrow;
        private bool pointerOver;
        private bool selected;
        private Vector2 arrowOrigin;

        private void Awake()
        {
            if (arrow != null)
                arrowOrigin = arrow.anchoredPosition;
        }

        private void Update()
        {
            float target = pointerOver || selected ? 1f : 0f;
            if (highlight != null)
                highlight.alpha = Mathf.MoveTowards(highlight.alpha, target, Time.unscaledDeltaTime * 7f);
            if (arrow != null)
                arrow.anchoredPosition = Vector2.Lerp(arrow.anchoredPosition,
                    arrowOrigin + Vector2.right * (target * 8f), 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
        }

        private void OnDisable()
        {
            pointerOver = selected = false;
            if (highlight != null)
                highlight.alpha = 0f;
            if (arrow != null)
                arrow.anchoredPosition = arrowOrigin;
        }

        public void OnPointerEnter(PointerEventData eventData) => pointerOver = true;
        public void OnPointerExit(PointerEventData eventData) => pointerOver = false;
        public void OnSelect(BaseEventData eventData) => selected = true;
        public void OnDeselect(BaseEventData eventData) => selected = false;
    }
}
