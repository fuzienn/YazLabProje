using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonEscape.Level
{
    public enum CellKind
    {
        Wall,
        Floor,
        RatTunnel,
        CellDoor,
        ExitDoor
    }

    public enum WallShape
    {
        None,
        Top,
        FaceUpper,
        FaceLower
    }

    public enum MarkerKind
    {
        PlayerSpawn,
        RatSpawn,
        GuardSpawn,
        Warden,
        Torch
    }

    public readonly struct Marker
    {
        public readonly MarkerKind Kind;
        public readonly Vector2Int Cell;

        public Marker(MarkerKind kind, Vector2Int cell)
        {
            Kind = kind;
            Cell = cell;
        }
    }

    /// <summary>
    /// ASCII harita metnini hucre turlerine ve isaretcilere ayristirir.
    /// Koordinatlar Unity'ye uygundur: x saga, y yukari; (0,0) sol alt hucre.
    /// </summary>
    public class LevelLayout
    {
        private static readonly Dictionary<char, CellKind> CellSymbols = new()
        {
            { '#', CellKind.Wall },
            { '.', CellKind.Floor },
            { 'd', CellKind.Floor },
            { 'r', CellKind.RatTunnel },
            { 'C', CellKind.CellDoor },
            { 'E', CellKind.ExitDoor },
        };

        private static readonly Dictionary<char, MarkerKind> MarkerSymbols = new()
        {
            { 'P', MarkerKind.PlayerSpawn },
            { 'R', MarkerKind.RatSpawn },
            { 'g', MarkerKind.GuardSpawn },
            { 'W', MarkerKind.Warden },
            { 'T', MarkerKind.Torch },
        };

        private readonly CellKind[,] _cells;
        private readonly List<Marker> _markers = new();

        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<Marker> Markers => _markers;

        private LevelLayout(int width, int height)
        {
            Width = width;
            Height = height;
            _cells = new CellKind[width, height];
        }

        public static LevelLayout Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Harita metni bos.");

            var rows = text.Replace("\r", "").Trim('\n').Split('\n');
            int width = rows[0].Length;
            var layout = new LevelLayout(width, rows.Length);

            for (int row = 0; row < rows.Length; row++)
            {
                if (rows[row].Length != width)
                    throw new FormatException($"Satir {row} genisligi {rows[row].Length}, beklenen {width}.");

                int y = rows.Length - 1 - row;
                for (int x = 0; x < width; x++)
                    layout.ReadSymbol(rows[row][x], new Vector2Int(x, y));
            }
            return layout;
        }

        private void ReadSymbol(char symbol, Vector2Int cell)
        {
            if (MarkerSymbols.TryGetValue(symbol, out var marker))
            {
                _markers.Add(new Marker(marker, cell));
                _cells[cell.x, cell.y] = CellKind.Floor;
                return;
            }
            if (!CellSymbols.TryGetValue(symbol, out var kind))
                throw new FormatException($"Bilinmeyen sembol '{symbol}' ({cell.x},{cell.y}).");
            _cells[cell.x, cell.y] = kind;
        }

        public bool InBounds(Vector2Int cell) =>
            cell.x >= 0 && cell.y >= 0 && cell.x < Width && cell.y < Height;

        public CellKind GetCell(Vector2Int cell) =>
            InBounds(cell) ? _cells[cell.x, cell.y] : CellKind.Wall;

        public bool IsFloor(Vector2Int cell) => GetCell(cell) == CellKind.Floor;

        /// <summary>Kapilar ve fare tuneli gorselde duvar gibi cizilir.</summary>
        public bool LooksLikeWall(Vector2Int cell) => GetCell(cell) != CellKind.Floor;

        /// <summary>
        /// Kus bakisi 3/4 perspektif: altinda zemin olan duvar tuglanin alt yuzu,
        /// onun ustundeki duvar ust yuzu, gerisi duvar tepesidir.
        /// </summary>
        public WallShape GetWallShape(Vector2Int cell)
        {
            if (!LooksLikeWall(cell)) return WallShape.None;
            var below = cell + Vector2Int.down;
            if (IsFloor(below)) return WallShape.FaceLower;
            if (LooksLikeWall(below) && IsFloor(below + Vector2Int.down)) return WallShape.FaceUpper;
            return WallShape.Top;
        }

        public IEnumerable<Vector2Int> CellsOf(CellKind kind)
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    if (_cells[x, y] == kind) yield return new Vector2Int(x, y);
        }

        /// <summary>Kapi hucrelerinin sinir kutusu (kapi sprite'i ve collider'i icin).</summary>
        public RectInt GetBounds(CellKind kind)
        {
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var cell in CellsOf(kind))
            {
                minX = Mathf.Min(minX, cell.x);
                minY = Mathf.Min(minY, cell.y);
                maxX = Mathf.Max(maxX, cell.x);
                maxY = Mathf.Max(maxY, cell.y);
            }
            if (minX == int.MaxValue) return new RectInt();
            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }
    }
}
