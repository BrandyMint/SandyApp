using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Games.Tubs {
    [Flags]
    public enum TubeDir {
        U = 1,
        R = 1 << 1,
        D = 1 << 2,
        L = 1 << 3
    }

    public static class Tube {
        public static readonly TubeDir[] DIRS = Enum.GetValues(typeof(TubeDir)).Cast<TubeDir>().ToArray();
        public static readonly TubeDir ALL_DIRS = DIRS.Aggregate((d1, d2) => d1 | d2);

        public static TubeDir GetOpposite(TubeDir dir) {
            switch (dir) {
                case TubeDir.U:
                    return TubeDir.D;
                case TubeDir.R:
                    return TubeDir.L;
                case TubeDir.D:
                    return TubeDir.U;
                case TubeDir.L:
                    return TubeDir.R;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dir), dir, null);
            }
        }

        public static Vector2Int GetOffset(TubeDir dir) {
            switch (dir) {
                case TubeDir.U:
                    return Vector2Int.up;
                case TubeDir.R:
                    return Vector2Int.right;
                case TubeDir.D:
                    return Vector2Int.down;
                case TubeDir.L:
                    return Vector2Int.left;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dir), dir, null);
            }
        }

        public static IEnumerable<TubeDir> Each(TubeDir dirs) {
            return DIRS.Where(d => dirs.HasFlag(d));
        }

        public static Vector2Int GetPos(Vector2Int pos, TubeDir dir) {
            return pos + GetOffset(dir);
        }
    }
}