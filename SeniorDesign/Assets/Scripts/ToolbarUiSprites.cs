using UnityEngine;

public static class ToolbarUiSprites
{
    private static Sprite tile;

    public static Sprite RoundedTile
    {
        get
        {
            if (tile != null)
            {
                return tile;
            }

            const int size = 32;
            const float border = 9f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, Color.white);
                }
            }

            tex.Apply();
            tile = Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
            tile.name = "ToolbarTile";
            return tile;
        }
    }
}
