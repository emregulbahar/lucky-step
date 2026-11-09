using UnityEngine;

namespace LuckyStep
{
    /// <summary>
    /// Number atlas of the step-counter score track (Story 005). One 64 px cell per number 0..31 in two blocks:
    /// rows 0-3 dark digits on white (tinted by the space material), rows 4-7 white digits plus a skull on black
    /// (deadly space). A 5x7 pixel font is drawn procedurally, so no font asset or TextMeshPro resources are needed.
    /// The editor builder saves <see cref="Generate"/> as a PNG; BoardView uses <see cref="CellUV"/> for space quads.
    /// </summary>
    public static class TrackAtlas
    {
        /// <summary>Atlas width and height in pixels.</summary>
        public const int Size = 512;
        /// <summary>Highest number with a cell.</summary>
        public const int MaxNumber = 31;
        private const int Cell = 64;
        private const int Columns = Size / Cell;

        private static readonly string[] Digits =
        {
            "01110100011001110101110011000101110", "00100011000010000100001000010001110",
            "01110100010000100010001000100011111", "11110000010000101110000010000111110",
            "00010001100101010010111110001000010", "11111100001111000001000011000101110",
            "00110010001000011110100011000101110", "11111000010001000100010000100001000",
            "01110100011000101110100011000101110", "01110100011000101111000010001001100",
        };

        private const string Skull = "0111110111111110010011111111011011001111100101010";

        /// <summary>UV rectangle (xMin, yMin, xMax, yMax) of number <paramref name="n"/>; deadly picks the skull block.</summary>
        public static Vector4 CellUV(int n, bool deadly)
        {
            n = Mathf.Clamp(n, 0, MaxNumber);
            int col = n % Columns;
            int row = n / Columns + (deadly ? 4 : 0);
            float s = 1f / Columns;
            return new Vector4(col * s, row * s, (col + 1) * s, (row + 1) * s);
        }

        /// <summary>Builds the atlas texture (RGBA32, sRGB colours).</summary>
        public static Texture2D Generate()
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true);
            var pixels = new Color32[Size * Size];
            var white = new Color32(255, 255, 255, 255);
            var black = new Color32(18, 16, 16, 255);
            var ink = new Color32(30, 24, 20, 255);
            for (int n = 0; n <= MaxNumber; n++)
            {
                for (int block = 0; block < 2; block++)
                {
                    bool deadly = block == 1;
                    int x0 = (n % Columns) * Cell;
                    int y0 = (n / Columns + block * 4) * Cell;
                    Fill(pixels, x0, y0, Cell, Cell, deadly ? black : white);
                    Color32 fg = deadly ? white : ink;
                    int scale = deadly ? 4 : 5;
                    int top = deadly ? y0 + Cell - 6 : y0 + Cell - (Cell - 7 * scale) / 2;
                    DrawNumber(pixels, n, x0 + Cell / 2, top, scale, fg);
                    if (deadly) DrawBitmap(pixels, Skull, 7, x0 + (Cell - 21) / 2, y0 + 27, 3, fg);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(true);
            return tex;
        }

        private static void DrawNumber(Color32[] px, int n, int centreX, int top, int scale, Color32 c)
        {
            string text = n.ToString();
            int glyph = 5 * scale;
            int gap = scale;
            int width = text.Length * glyph + (text.Length - 1) * gap;
            int x = centreX - width / 2;
            foreach (char ch in text)
            {
                DrawBitmap(px, Digits[ch - '0'], 5, x, top, scale, c);
                x += glyph + gap;
            }
        }

        /// <summary>Draws a row-major bitmap ('1' = set), row 0 at the top, with its top edge at pixel row <paramref name="top"/>.</summary>
        private static void DrawBitmap(Color32[] px, string bits, int w, int left, int top, int scale, Color32 c)
        {
            int h = bits.Length / w;
            for (int r = 0; r < h; r++)
            {
                for (int k = 0; k < w; k++)
                {
                    if (bits[r * w + k] != '1') continue;
                    Fill(px, left + k * scale, top - (r + 1) * scale, scale, scale, c);
                }
            }
        }

        private static void Fill(Color32[] px, int x, int y, int w, int h, Color32 c)
        {
            for (int j = Mathf.Max(0, y); j < Mathf.Min(Size, y + h); j++)
            {
                for (int i = Mathf.Max(0, x); i < Mathf.Min(Size, x + w); i++) px[j * Size + i] = c;
            }
        }
    }
}
