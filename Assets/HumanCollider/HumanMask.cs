using System;
using UnityEngine;

namespace HumanCollider {
    public class HumanMask {
        public const byte EMPTY = 0;
        public const byte NEAR = 1;
        public const byte FAR = 2;
        public const byte NEAR_FOOT = 3;
        public const byte FAR_FOOT = 4;

        public byte[] arr = null;
        public int width;
        public int height;
        public int borderH1, borderH2, borderW1, borderW2;

        private float
            saveBorderH1 = 0.0f, saveBorderH2 = 1.0f,
            saveBorderW1 = 0.0f, saveBorderW2 = 1.0f;
        private int borderNbr0, borderNbr2, borderNbr4, borderNbr6;
        private int[] cacheIndexesInBorders;

        public void SetDimens(int w, int h) {
            width = w;
            height = h;
            arr = new byte[w * h];
            SetBorders(saveBorderW1, saveBorderH1, saveBorderW2, saveBorderH2, false);
        }

        internal void SetBorders(float w1, float h1, float w2, float h2, bool clear = true) {
            saveBorderH1 = h1;
            saveBorderH2 = h2;
            saveBorderW1 = w1;
            saveBorderW2 = w2;
            borderW1 = (int) (Mathf.Clamp01(w1) * width);
            borderW2 = (int) (Mathf.Clamp01(w2) * width);
            borderH1 = (int) (Mathf.Clamp01(h1) * height);
            borderH2 = (int) (Mathf.Clamp01(h2) * height);
            borderNbr0 = (borderH1 + 1) * width;
            borderNbr2 = borderW2 - 2;
            borderNbr4 = width * (borderH2 - 1);
            borderNbr6 = borderW1 + 1;
            if (arr != null) {
                if (clear)
                    Array.Clear(arr, 0, arr.Length);
                CalcIndexesInBorders();
            }
        }

        public int GetIndexOfNeighbor(int i, int nbr) {
            //  7 0 1
            //  6 i 2
            //  5 4 3
            switch (nbr) {
                case 0:
                    return (i < borderNbr0) ? -1 : i - width;
                case 1:
                    return (i < borderNbr0 || i % width > borderNbr2) ? -1 : i - width + 1;
                case 2:
                    return (i % width > borderNbr2) ? -1 : i + 1;
                case 3:
                    return (i > borderNbr4 || i % width > borderNbr2) ? -1 : i + width + 1;
                case 4:
                    return (i > borderNbr4) ? -1 : i + width;
                case 5:
                    return (i > borderNbr4 || i % width < borderNbr6) ? -1 : i + width - 1;
                case 6:
                    return (i % width < borderNbr6) ? -1 : i - 1;
                case 7:
                    return (i < borderNbr0 || i % width < borderNbr6) ? -1 : i - width - 1;
            }
            return -1;
        }

        public int[] GetIndexesInBorders() {
            return cacheIndexesInBorders;
        }

        private void CalcIndexesInBorders() {
            int idx = borderH1 * width + borderW1;
            int yd = width - borderW2 + borderW1;
            cacheIndexesInBorders =
                new int[(borderH2 - borderH1) * (borderW2 - borderW1)];
            int i = 0;
            for (int y = borderH1; y < borderH2; ++y) {
                for (int x = borderW1; x < borderW2; ++x) {
                    cacheIndexesInBorders[i++] = idx++;
                }
                idx += yd;
            }
        }
    }
}