﻿using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Games.Common.ColliderGenerator {
    public class OutputPolygonCollider2D : OutputPolygonCollider2DBase {
        public RectInt SourceRect;

        public override void AddShape(List<Vector2> points) {
            var path = collider.pathCount++;
            var scale = new float2(1f / SourceRect.width, 1f / SourceRect.height);
            var offset = new float2(SourceRect.yMin, SourceRect.xMin) * scale - new float2(0.5f, 0.5f) + scale / 2f;
            for (int i = 0; i < points.Count; ++i) {
                points[i] = offset + (float2)points[i] * scale;
            }
            collider.SetPath(path, points);
        }
    }
}