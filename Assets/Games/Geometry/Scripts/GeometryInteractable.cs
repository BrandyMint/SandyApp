﻿using Games.Common.GameFindObject;
using UnityEngine;

namespace Games.Geometry {
    public class GeometryInteractable : Interactable {
        private static readonly int _COLOR = Shader.PropertyToID("_Color");
        
        protected override void Awake() {
            base.Awake();

            var startColor = _r.material.GetColor(_COLOR);
            Color.RGBToHSV(startColor, out _, out var s, out var v);
            startColor = Color.HSVToRGB(Random.value, s, v);

            
            var props = new MaterialPropertyBlock();
            _r.GetPropertyBlock(props);
            props.SetColor(_COLOR, startColor);
            _r.SetPropertyBlock(props);
        }
    }
}