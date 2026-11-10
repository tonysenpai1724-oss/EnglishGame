using System.Collections.Generic;
using UnityEngine;

namespace EnglishLearning
{
    /// <summary>Small self-contained demo illustrations. Production artwork is configured in the course.</summary>
    public sealed class DemoObjectArt : System.IDisposable
    {
        private const int Size = 192;
        private readonly Dictionary<DemoObjectIcon, Sprite> cache = new Dictionary<DemoObjectIcon, Sprite>();
        private static readonly Color Ink = new Color32(39, 57, 62, 255);
        private Color[] pixels;

        public Sprite Get(DemoObjectIcon icon)
        {
            if (cache.TryGetValue(icon, out var sprite)) return sprite;
            pixels = new Color[Size * Size];
            Draw(icon);
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { name = icon + " demo", filterMode = FilterMode.Bilinear };
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(.5f, .5f), Size);
            cache.Add(icon, sprite);
            pixels = null;
            return sprite;
        }

        private void Draw(DemoObjectIcon icon)
        {
            Color red = new Color32(238, 78, 75, 255), green = new Color32(91, 169, 96, 255);
            Color yellow = new Color32(255, 205, 78, 255), blue = new Color32(87, 159, 212, 255);
            switch (icon)
            {
                case DemoObjectIcon.Apple:
                    Line(new Vector2(.5f, .7f), new Vector2(.53f, .9f), .035f, Ink);
                    Ellipse(.64f, .84f, .13f, .065f, green);
                    Ellipse(.38f, .47f, .25f, .31f, Ink); Ellipse(.62f, .47f, .25f, .31f, Ink);
                    Ellipse(.38f, .47f, .225f, .285f, red); Ellipse(.62f, .47f, .225f, .285f, red);
                    Ellipse(.3f, .58f, .045f, .09f, new Color(1, .68f, .62f)); break;
                case DemoObjectIcon.Banana:
                    // Filled crescent, transparent on the cutout side.
                    for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
                    {
                        Vector2 p = new Vector2((x + .5f) / Size, (y + .5f) / Size);
                        float outer = Vector2.Distance(p, new Vector2(.43f, .56f));
                        float inner = Vector2.Distance(p, new Vector2(.33f, .69f));
                        if (outer < .4f && inner > .32f) pixels[y * Size + x] = outer > .375f || inner < .342f ? Ink : yellow;
                    }
                    Line(new Vector2(.75f, .77f), new Vector2(.79f, .86f), .035f, Ink);
                    Line(new Vector2(.13f, .32f), new Vector2(.19f, .26f), .03f, Ink); break;
                case DemoObjectIcon.Orange:
                    Ellipse(.5f, .46f, .34f, .34f, Ink); Ellipse(.5f, .46f, .315f, .315f, new Color(1, .57f, .16f));
                    Ellipse(.65f, .83f, .15f, .06f, green);
                    Line(new Vector2(.5f, .77f), new Vector2(.52f, .86f), .025f, Ink);
                    Ellipse(.34f, .6f, .05f, .08f, new Color(1, .78f, .4f));
                    for (int i = 0; i < 10; i++) Ellipse(.28f + (i * .137f % .46f), .27f + (i * .119f % .36f), .008f, .008f, new Color(.87f, .4f, .1f)); break;
                case DemoObjectIcon.Strawberry:
                    Polygon(Ink, new Vector2(.17f,.72f), new Vector2(.3f,.86f), new Vector2(.7f,.86f), new Vector2(.83f,.72f), new Vector2(.7f,.39f), new Vector2(.5f,.12f), new Vector2(.3f,.39f));
                    Polygon(red, new Vector2(.2f,.71f), new Vector2(.32f,.82f), new Vector2(.68f,.82f), new Vector2(.8f,.71f), new Vector2(.67f,.4f), new Vector2(.5f,.17f), new Vector2(.33f,.4f));
                    for (int row = 0; row < 3; row++) for (int col = 0; col < 3 - row; col++) Ellipse(.32f + col * .17f + row * .085f, .68f - row * .16f, .013f, .027f, yellow);
                    Polygon(green, new Vector2(.22f,.86f), new Vector2(.41f,.85f), new Vector2(.5f,.96f), new Vector2(.58f,.85f), new Vector2(.78f,.87f), new Vector2(.63f,.72f), new Vector2(.5f,.8f), new Vector2(.36f,.72f)); break;
                case DemoObjectIcon.Pear:
                    Ellipse(.5f, .39f, .32f, .28f, Ink); Ellipse(.5f, .64f, .19f, .24f, Ink);
                    Ellipse(.5f, .39f, .295f, .255f, new Color(.71f, .81f, .28f)); Ellipse(.5f, .64f, .165f, .215f, new Color(.71f, .81f, .28f));
                    Line(new Vector2(.5f,.83f), new Vector2(.56f,.95f), .026f, Ink);
                    Ellipse(.65f,.88f,.11f,.05f,green); Ellipse(.35f,.45f,.04f,.09f,new Color(.87f,.93f,.5f)); break;
                case DemoObjectIcon.Book:
                    Box(.17f,.15f,.69f,.7f,Ink); Box(.2f,.18f,.63f,.64f,blue);
                    Box(.22f,.19f,.07f,.62f,new Color(.2f,.4f,.63f)); Box(.32f,.21f,.49f,.07f,new Color(1,.96f,.84f));
                    Box(.4f,.59f,.3f,.065f,new Color(1,.96f,.84f)); Box(.44f,.47f,.22f,.03f,new Color(.8f,.91f,1)); break;
                case DemoObjectIcon.Pencil:
                    Polygon(Ink, new Vector2(.17f,.17f),new Vector2(.25f,.43f),new Vector2(.72f,.9f),new Vector2(.9f,.72f),new Vector2(.43f,.25f));
                    Polygon(yellow,new Vector2(.3f,.43f),new Vector2(.69f,.82f),new Vector2(.82f,.69f),new Vector2(.43f,.3f));
                    Polygon(new Color(1,.82f,.61f),new Vector2(.2f,.2f),new Vector2(.29f,.42f),new Vector2(.42f,.29f));
                    Polygon(Ink,new Vector2(.19f,.19f),new Vector2(.23f,.29f),new Vector2(.29f,.23f));
                    Line(new Vector2(.73f,.84f),new Vector2(.84f,.73f),.06f,red);
                    Line(new Vector2(.38f,.42f),new Vector2(.7f,.74f),.012f,new Color(.91f,.57f,.12f)); break;
                case DemoObjectIcon.Cup:
                    Ellipse(.75f,.5f,.2f,.2f,Ink); Ellipse(.75f,.5f,.15f,.15f,blue); Ellipse(.75f,.5f,.095f,.105f,Color.clear);
                    Box(.19f,.23f,.49f,.5f,Ink); Box(.22f,.26f,.43f,.44f,blue);
                    Ellipse(.435f,.72f,.245f,.065f,Ink); Ellipse(.435f,.73f,.21f,.038f,new Color(.76f,.9f,.98f));
                    Box(.28f,.35f,.04f,.24f,new Color(.65f,.85f,.99f)); break;
                case DemoObjectIcon.Key:
                    Ellipse(.36f,.66f,.24f,.24f,Ink); Ellipse(.36f,.66f,.215f,.215f,yellow); Ellipse(.36f,.66f,.105f,.105f,Ink); Ellipse(.36f,.66f,.075f,.075f,Color.clear);
                    Line(new Vector2(.49f,.5f),new Vector2(.8f,.19f),.065f,Ink); Line(new Vector2(.49f,.5f),new Vector2(.8f,.19f),.04f,yellow);
                    Line(new Vector2(.71f,.27f),new Vector2(.8f,.36f),.035f,Ink); Line(new Vector2(.78f,.2f),new Vector2(.87f,.29f),.035f,Ink); break;
                case DemoObjectIcon.Ball:
                    Ellipse(.5f,.5f,.37f,.37f,Ink); Ellipse(.5f,.5f,.345f,.345f,blue);
                    Polygon(yellow,new Vector2(.5f,.845f),new Vector2(.55f,.6f),new Vector2(.78f,.3f),new Vector2(.7f,.22f),new Vector2(.46f,.46f),new Vector2(.25f,.72f));
                    Ellipse(.5f,.5f,.095f,.095f,red); Ellipse(.34f,.7f,.045f,.07f,new Color(.7f,.9f,1)); break;
            }
        }

        private void Box(float x, float y, float w, float h, Color color) => Polygon(color,
            new Vector2(x,y),new Vector2(x+w,y),new Vector2(x+w,y+h),new Vector2(x,y+h));
        private void Ellipse(float cx, float cy, float rx, float ry, Color color)
        {
            for (int y = Mathf.Max(0, (int)((cy - ry) * Size)); y < Mathf.Min(Size, Mathf.CeilToInt((cy + ry) * Size)); y++)
            for (int x = Mathf.Max(0, (int)((cx - rx) * Size)); x < Mathf.Min(Size, Mathf.CeilToInt((cx + rx) * Size)); x++)
            {
                float dx = ((x + .5f) / Size - cx) / rx, dy = ((y + .5f) / Size - cy) / ry;
                if (dx * dx + dy * dy <= 1) pixels[y * Size + x] = color;
            }
        }
        private void Line(Vector2 a, Vector2 b, float radius, Color color)
        {
            Vector2 ab = b - a;
            for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
            {
                Vector2 p = new Vector2((x + .5f) / Size, (y + .5f) / Size);
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                if ((p - a - ab * t).sqrMagnitude <= radius * radius) pixels[y * Size + x] = color;
            }
        }
        private void Polygon(Color color, params Vector2[] points)
        {
            for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
            {
                float px = (x + .5f) / Size, py = (y + .5f) / Size;
                bool inside = false;
                for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
                {
                    Vector2 a = points[i], b = points[j];
                    if ((a.y > py) != (b.y > py) && px < (b.x - a.x) * (py - a.y) / (b.y - a.y) + a.x) inside = !inside;
                }
                if (inside) pixels[y * Size + x] = color;
            }
        }
        public void Dispose()
        {
            foreach (var sprite in cache.Values)
            {
                Object.Destroy(sprite.texture);
                Object.Destroy(sprite);
            }
            cache.Clear();
        }
    }
}
