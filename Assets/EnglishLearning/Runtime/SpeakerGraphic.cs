using UnityEngine;
using UnityEngine.UI;

namespace EnglishLearning
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SpeakerGraphic : MaskableGraphic
    {
        protected override void Awake() { base.Awake(); useLegacyMeshGeneration = false; raycastTarget = false; }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Quad(vh, new Vector2(.08f,.36f),new Vector2(.29f,.36f),new Vector2(.29f,.64f),new Vector2(.08f,.64f));
            Quad(vh, new Vector2(.25f,.36f),new Vector2(.5f,.14f),new Vector2(.5f,.86f),new Vector2(.25f,.64f));
            Arc(vh,.6f,.18f); Arc(vh,.6f,.32f);
        }
        private void Arc(VertexHelper vh,float x,float radius)
        {
            for (int i = 0; i < 12; i++)
            {
                float a = Mathf.Lerp(-1.05f,1.05f,i / 12f), b = Mathf.Lerp(-1.05f,1.05f,(i+1) / 12f);
                Vector2 center = new Vector2(x,.5f);
                Quad(vh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(radius-.023f),
                    center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*(radius-.023f),
                    center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*(radius+.023f),
                    center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(radius+.023f));
            }
        }
        private void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d)
        {
            int start = vh.currentVertCount;
            Rect r = rectTransform.rect;
            foreach (var p in new[] {a,b,c,d})
                vh.AddVert(new Vector3(r.xMin+p.x*r.width,r.yMin+p.y*r.height,0),color,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2); vh.AddTriangle(start,start+2,start+3);
        }
    }
}
