using UnityEngine;

namespace LuckyStep
{
    /// <summary>
    /// Layout of the step-counter score-track number atlas (Story 005, "Printed Board" look in design/game-brief.md).
    /// One 128 px cell per number 0..31 in four blocks of 4 rows (1024 x 2048 px), one block per <see cref="Face"/>,
    /// block 0 at the bottom: Plain = ink number on white (tinted cream by the space material); Deadly = cream number
    /// and a skull on ink; Lucky = ink number and a cream star on gold (colours baked, untinted material); Start =
    /// number and "START" on white. The PNG (Assets/Art/Generated/TrackAtlas.png) is drawn in Fredoka Bold by
    /// tools/textures/make_track_atlas.py, which must keep this layout; BoardView uses <see cref="CellUV"/> for space quads.
    /// </summary>
    public static class TrackAtlas
    {
        /// <summary>Printed look of one score-track space.</summary>
        public enum Face
        {
            /// <summary>Ink number on paper.</summary>
            Plain = 0,
            /// <summary>Cream number and skull on ink.</summary>
            Deadly = 1,
            /// <summary>Ink number and a cream star on gold.</summary>
            Lucky = 2,
            /// <summary>Ink number and the word START on paper (first space of the track).</summary>
            Start = 3
        }

        /// <summary>Atlas width in pixels.</summary>
        public const int Width = 1024;
        /// <summary>Atlas height in pixels (four blocks of four rows).</summary>
        public const int Height = 2048;
        /// <summary>Highest number with a cell.</summary>
        public const int MaxNumber = 31;
        private const int Cell = 128;
        private const int Columns = Width / Cell;
        private const int RowsPerBlock = 4;
        private const int Rows = Height / Cell;

        /// <summary>UV rectangle (xMin, yMin, xMax, yMax) of number <paramref name="n"/> printed as <paramref name="face"/>.</summary>
        public static Vector4 CellUV(int n, Face face)
        {
            n = Mathf.Clamp(n, 0, MaxNumber);
            int col = n % Columns;
            int row = n / Columns + (int)face * RowsPerBlock;
            float su = 1f / Columns;
            float sv = 1f / Rows;
            return new Vector4(col * su, row * sv, (col + 1) * su, (row + 1) * sv);
        }
    }
}
