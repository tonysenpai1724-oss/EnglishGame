using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EnglishLearning
{
    /// <summary>Visual darkness and pointer gestures only; knows nothing about lessons.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FlashlightOverlay : MaskableGraphic, IPointerMoveHandler, IPointerDownHandler,
        IPointerUpHandler, IDragHandler, IInitializePotentialDragHandler, ICancelHandler
    {
        private Material ownedMaterial;
        private Vector2 center = new Vector2(.5f, .5f);
        private Vector2 pressPosition;
        private float radiusFraction = .22f, featherFraction = .025f;
        private int? activePointer;
        private bool eligible, dragged;
        public event Action<Vector2> Selected;
        public bool InputEnabled { get; set; } = true;

        protected override void Awake()
        {
            base.Awake();
            useLegacyMeshGeneration = false;
            var shader = Resources.Load<Shader>("EnglishLearning/Flashlight");
            if (shader == null)
            {
                Debug.LogError("Flashlight shader is missing. Restore Resources/EnglishLearning/Flashlight.shader.", this);
                InputEnabled = false;
                color = Color.black;
                return;
            }
            ownedMaterial = new Material(shader);
            material = ownedMaterial;
            color = Color.white;
            raycastTarget = true;
        }

        public void Configure(float radius, float feather)
        {
            radiusFraction = radius;
            featherFraction = feather;
            ResetLight();
        }

        public void ResetLight()
        {
            center = new Vector2(.5f, .5f);
            activePointer = null;
            eligible = dragged = false;
            UploadLightProperties();
        }

        private void LateUpdate() => UploadLightProperties();

        private void UploadLightProperties()
        {
            if (ownedMaterial == null) return;
            Rect rect = rectTransform.rect;
            float side = Mathf.Min(rect.width, rect.height);
            // materialForRendering includes a stencil material when this graphic is masked.
            var target = materialForRendering;
            target.SetVector("_Center", center);
            target.SetVector("_RectSize", new Vector4(rect.width, rect.height, 0, 0));
            target.SetFloat("_Radius", side * radiusFraction);
            target.SetFloat("_Feather", side * featherFraction);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;
            vh.AddVert(new Vector3(r.xMin, r.yMin, 0), color, new Vector2(0, 0));
            vh.AddVert(new Vector3(r.xMin, r.yMax, 0), color, new Vector2(0, 1));
            vh.AddVert(new Vector3(r.xMax, r.yMax, 0), color, new Vector2(1, 1));
            vh.AddVert(new Vector3(r.xMax, r.yMin, 0), color, new Vector2(1, 0));
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        private bool Local(PointerEventData e, out Vector2 point) =>
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, e.position, e.pressEventCamera, out point);

        private bool IsLit(Vector2 point)
        {
            Rect r = rectTransform.rect;
            Vector2 light = r.min + Vector2.Scale(center, r.size);
            return (point - light).sqrMagnitude <= Mathf.Pow(Mathf.Min(r.width, r.height) * radiusFraction, 2);
        }

        private void MoveLight(Vector2 point)
        {
            Rect r = rectTransform.rect;
            if (r.width <= 0 || r.height <= 0) return;
            center = new Vector2(Mathf.Clamp01((point.x - r.xMin) / r.width), Mathf.Clamp01((point.y - r.yMin) / r.height));
            UploadLightProperties();
        }

        public void OnPointerMove(PointerEventData e)
        {
            // Negative IDs are mouse pointers. Touch movement is handled by the captured drag.
            if (InputEnabled && e.pointerId < 0 && !activePointer.HasValue && Local(e, out var p)) MoveLight(p);
        }

        public void OnInitializePotentialDrag(PointerEventData e) { e.useDragThreshold = true; }

        public void OnPointerDown(PointerEventData e)
        {
            if (!InputEnabled || activePointer.HasValue || e.button != PointerEventData.InputButton.Left || !Local(e, out var p)) return;
            activePointer = e.pointerId;
            pressPosition = e.position;
            eligible = IsLit(p); // First tap in darkness reveals; it must not select an unseen object.
            dragged = false;
            MoveLight(p);
        }

        public void OnDrag(PointerEventData e)
        {
            if (activePointer != e.pointerId || !InputEnabled) return;
            dragged = true;
            if (Local(e, out var p)) MoveLight(p);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (activePointer != e.pointerId) return;
            activePointer = null;
            float threshold = EventSystem.current != null ? EventSystem.current.pixelDragThreshold : 10;
            bool tap = !dragged && (e.position - pressPosition).sqrMagnitude <= threshold * threshold;
            if (InputEnabled && eligible && tap && Local(e, out var p) && rectTransform.rect.Contains(p)) Selected?.Invoke(p);
            eligible = false;
        }

        public void OnCancel(BaseEventData e) => ResetLight();
        protected override void OnDisable() { activePointer = null; eligible = false; base.OnDisable(); }
        private void OnApplicationFocus(bool focused) { if (!focused) ResetLight(); }
        protected override void OnDestroy()
        {
            if (ownedMaterial != null) Destroy(ownedMaterial);
            base.OnDestroy();
        }
    }
}
