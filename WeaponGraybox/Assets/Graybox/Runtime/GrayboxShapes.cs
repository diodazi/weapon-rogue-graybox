using UnityEngine;

namespace WeaponGraybox
{
    public static class GrayboxShapes
    {
        static Sprite square;
        static Material material;
        public static Sprite Square
        {
            get
            {
                if (!square) square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), new Vector2(.5f, .5f), Texture2D.whiteTexture.width);
                return square;
            }
        }
        public static Material Material
        {
            get
            {
                if (!material) material = Resources.Load<Material>("GrayboxSprite");
                if (!material) material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default"));
                return material;
            }
        }
        public static GameObject Box(string name, Transform parent, Vector2 position, Vector2 size, Color color, int order = 0)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Square; sr.sharedMaterial = Material; sr.color = color; sr.sortingOrder = order;
            return go;
        }
        public static LineRenderer Outline(string name, Transform parent, Vector2 center, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false; line.loop = true; line.positionCount = 4;
            line.sharedMaterial = Material; line.startColor = line.endColor = color;
            line.startWidth = line.endWidth = .025f; line.sortingOrder = 7;
            var h = size * .5f;
            line.SetPositions(new[] { (Vector3)(center + new Vector2(-h.x, -h.y)), (Vector3)(center + new Vector2(h.x, -h.y)), (Vector3)(center + new Vector2(h.x, h.y)), (Vector3)(center + new Vector2(-h.x, h.y)) });
            return line;
        }
    }
}
