using UnityEngine;
using UnityEngine.EventSystems;
namespace VerdichotomyFramework
{
    /// <summary>
    ///     Translates touch or mouse drag input into swipe events.
    ///     Attach to the card's RectTransform (or a full-screen drag area).
    ///     Implements Unity's IBeginDragHandler, IDragHandler, IEndDragHandler.
    ///     Works in both mouse (editor/PC) and touch (mobile) contexts via EventSystem.
    /// </summary>
    public class SwipeInputHandler : MonoBehaviour,
		IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		// ── Dependencies ──────────────────────────────────────────────────────

		[Tooltip("The CardPlayer to notify of swipe events.")]
		public CardPlayer cardPlayer;

		// ── Settings ──────────────────────────────────────────────────────────

		[Header("Thresholds"), Tooltip("Minimum horizontal distance in pixels before a drag registers as a swipe (not a tap).")]
		public float dragThreshold = 20f;

		[Tooltip("Horizontal distance in pixels at which a swipe is considered 'committed' (progress = 1.0).")]
		public float commitDistance = 200f;

		[Tooltip("If the drag ends with progress above this value, the swipe is committed. Below = snap back."), Range(0f, 1f)]
		public float commitThreshold = 0.5f;
		private bool _committed;
		private SwipeDirection _currentDir = SwipeDirection.Right;
		private bool _dragging;

		// ── State ─────────────────────────────────────────────────────────────

		private Vector2 _dragStartPos;

		// ── Keyboard fallback (editor / accessibility) ────────────────────────

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.LeftArrow))
				cardPlayer?.CommitSwipe(SwipeDirection.Left);
			else if (Input.GetKeyDown(KeyCode.RightArrow))
				cardPlayer?.CommitSwipe(SwipeDirection.Right);
		}

		// ── IBeginDragHandler ─────────────────────────────────────────────────

		public void OnBeginDrag(PointerEventData eventData)
		{
			_dragStartPos = eventData.position;
			_dragging = true;
			_committed = false;
		}

		// ── IDragHandler ──────────────────────────────────────────────────────

		public void OnDrag(PointerEventData eventData)
		{
			if (!_dragging || _committed) return;

			var delta = eventData.position.x - _dragStartPos.x;
			if (Mathf.Abs(delta) < dragThreshold) return;

			_currentDir = delta < 0 ? SwipeDirection.Left : SwipeDirection.Right;
			var progress = Mathf.Clamp01(Mathf.Abs(delta) / commitDistance);

			cardPlayer?.UpdateSwipeProgress(_currentDir, progress);
		}

		// ── IEndDragHandler ───────────────────────────────────────────────────

		public void OnEndDrag(PointerEventData eventData)
		{
			if (!_dragging) return;
			_dragging = false;

			var delta = eventData.position.x - _dragStartPos.x;
			var progress = Mathf.Clamp01(Mathf.Abs(delta) / commitDistance);

			if (progress >= commitThreshold)
			{
				_committed = true;
				cardPlayer?.CommitSwipe(_currentDir);
			}
			else
			{
				// Snap back — send zero progress
				cardPlayer?.UpdateSwipeProgress(_currentDir, 0f);
			}
		}
	}
}