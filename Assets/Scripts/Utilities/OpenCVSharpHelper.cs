using System;
using System.Linq;
using AsyncGPUReadbackPluginNs;
using OpenCvSharp;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;

namespace Utilities {
    public static class OpenCVSharpHelper {
        /*public class LshIndexParams : IndexParams {
            public LshIndexParams(int table_number = 12, int key_size = 20, int multi_probe_level = 2) {
                SetAlgorithm(6);
                // The number of hash tables to use
                SetInt("table_number", table_number);
                // The length of the key in the hash tables
                SetInt("key_size", key_size);
                // Number of levels to use in multi-probe (0 for standard LSH)
                SetInt("multi_probe_level", multi_probe_level);
            }
        };*/

        public static void DisposeManual(this Mat m) {
            m.IsEnabledDispose = true;
            m?.Dispose();
        }
        
        public static void DisposeManual(ref Mat m) {
            m?.DisposeManual();
            m = null;
        }
        
        public static bool ReCreateIfNeed(ref Mat m, int width, int height, MatType t) {
            if (m == null) {
                m = new Mat();
                m.IsEnabledDispose = false;
            }

            if (m.Width != width || m.Height != height || m.Type() != t) {
                m.Create(height, width, t);
                m.SetTo(Scalar.All(0.5));
                return true;
            }
            
            return false;
        }
        
        public static bool ReCreateIfNeedCompatible(ref Mat m, Texture t) {
            if (GetCompatibleFormat(t.graphicsFormat, out var type)) {
                return ReCreateIfNeed(ref m, t.width, t.height, type);
            }
            return false;
        }
        
        public static bool ReCreateWithLinkedDataIfNeed(ref Mat m, Texture2D t) {
            if (GetCompatibleFormat(t.graphicsFormat, out var type)) {
                var a = t.GetRawTextureData<byte>();
                var ptr = a.IntPtr();
                if (m == null || m.DataStart != ptr || m.Width != t.width || m.Height != t.height || m.Type() != type) {
                    m?.DisposeManual();
                    m = new Mat(t.height, t.width, type, a.IntPtr());
                    m.IsEnabledDispose = false;
                    return true;
                };
            }
            return false;
        }
        
        public static bool ReCreateIfNeedCompatible(ref Texture2D t, Mat m) {
            if (GetCompatibleFormat(m.Type(), out TextureFormat type)) {
                return TexturesHelper.ReCreateIfNeed(ref t, m.Width, m.Height, type);
            }
            return false;
        }
        
        public static long GetLengthInBytes(this Mat m) {
            return  m.Total() * m.ElemSize();
        }

        public static AsyncGPUReadbackRequest AsyncSetFrom(this Mat m, Texture t, Action<AsyncGPUReadbackRequest> onDone = null) {
            var lenBytes = t.GetLengthInBytes();
            Assert.AreEqual(m.GetLengthInBytes(), lenBytes, "Mat and Texture must be equal length");
            Assert.IsTrue(m.IsContinuous(), "Mat must be continuous");
            m.IsEnabledDispose = false;
            var array = MemUtils.ConvertPtrToNativeArray<byte>(m.Data, lenBytes);
            return AsyncGPUReadback.RequestIntoNativeArray(ref array, t, 0, onDone);
        }

        public static void SetFrom(this Mat m, Texture2D t) {
            var a = t.GetRawTextureData();
            m.SetArray(0, 0, a.ToArray());
        }

        private static Texture2D _texCache;
        public static void SetFrom(this Mat m, RenderTexture t) {
            TexturesHelper.ReCreateIfNeedCompatible(ref _texCache, t);
            TexturesHelper.Copy(t, _texCache);
            m.SetFrom(_texCache);
        }
        
        public static void SetFrom(this Mat m, Texture t) {
            var t2d = t as Texture2D;
            if (t2d != null) {
                m.SetFrom(t2d);
                return;
            }
            var rend = t as RenderTexture;
            if (rend != null) {
                m.SetFrom(rend);
                return;
            }
            throw new NotImplementedException();
        }

        public static AsyncGPUReadbackRequest AsyncSetFrom(this Mat m, Texture t, Action onSuccessDone = null) {
            return m.AsyncSetFrom(t, r => {
                if (r.hasError) {
                    Debug.LogError("AsyncGPUReadback error");
                } else {
                    onSuccessDone?.Invoke();
                }
            });
        }

        public static int GetBytesPerMatTypeChanel(int depth) {
            switch (depth) {
                case MatType.CV_8U:
                case MatType.CV_8S:
                    return 1;
                case MatType.CV_16U:
                case MatType.CV_16S:
                    return 2;
                case MatType.CV_32S:
                case MatType.CV_32F:
                    return 4;
                case MatType.CV_64F:
                    return 8;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static Scalar GetScalarFrom(Color c) {
            const int k = byte.MaxValue; 
            return Scalar.FromRgb((int) (c.r * k), (int) (c.g * k), (int) (c.b * k));
        }

        private class FormatsCompatibles {
            public int opencvDepth;
            public GraphicsFormat[] unity;
        }
        private static readonly FormatsCompatibles[] _formatsEquals = new FormatsCompatibles[] {
            new FormatsCompatibles {
                opencvDepth = MatType.CV_8U,
                unity = new [] {
                    GraphicsFormat.R8_UInt, GraphicsFormat.R8_UNorm,
                    GraphicsFormat.R8G8_UInt, GraphicsFormat.R8G8_UNorm,
                    GraphicsFormat.R8G8B8_UInt, GraphicsFormat.R8G8B8_UNorm,
                    GraphicsFormat.B8G8R8_UInt, GraphicsFormat.B8G8R8_UNorm,
                    GraphicsFormat.R8G8B8A8_UInt, GraphicsFormat.R8G8B8A8_UNorm,
                    GraphicsFormat.B8G8R8A8_UInt, GraphicsFormat.B8G8R8A8_UNorm,
                }
            },
            new FormatsCompatibles {
                opencvDepth = MatType.CV_8S,
                unity = new [] {
                    GraphicsFormat.R8_SInt, GraphicsFormat.R8_SNorm,
                    GraphicsFormat.R8G8_SInt, GraphicsFormat.R8G8_SNorm,
                    GraphicsFormat.R8G8B8_SInt, GraphicsFormat.R8G8B8_SNorm,
                    GraphicsFormat.B8G8R8_SInt, GraphicsFormat.B8G8R8_SNorm,
                    GraphicsFormat.R8G8B8A8_SInt, GraphicsFormat.R8G8B8A8_SNorm,
                    GraphicsFormat.B8G8R8A8_SInt, GraphicsFormat.B8G8R8A8_SNorm,
                }
            },
            new FormatsCompatibles {
                opencvDepth = MatType.CV_16U,
                unity = new [] {
                    GraphicsFormat.R16_UInt, GraphicsFormat.R16_UNorm,
                    GraphicsFormat.R16G16_UInt, GraphicsFormat.R16G16_UNorm,
                    GraphicsFormat.R16G16B16_UInt, GraphicsFormat.R16G16B16_UNorm,
                    GraphicsFormat.R16G16B16A16_UInt, GraphicsFormat.R16G16B16A16_UNorm,
                }
            },
            new FormatsCompatibles {
                opencvDepth = MatType.CV_16S,
                unity = new [] {
                    GraphicsFormat.R16_SInt, GraphicsFormat.R16_SNorm,
                    GraphicsFormat.R16G16_SInt, GraphicsFormat.R16G16_SNorm,
                    GraphicsFormat.R16G16B16_SInt, GraphicsFormat.R16G16B16_SNorm,
                    GraphicsFormat.R16G16B16A16_SInt, GraphicsFormat.R16G16B16A16_SNorm,
                }
            },
            new FormatsCompatibles {
                opencvDepth = MatType.CV_32S,
                unity = new [] {
                    GraphicsFormat.R32_SInt,
                    GraphicsFormat.R32G32_SInt,
                    GraphicsFormat.R32G32B32_SInt,
                    GraphicsFormat.R32G32B32A32_SInt,
                }
            },
            new FormatsCompatibles {
                opencvDepth = MatType.CV_32F,
                unity = new [] {
                    GraphicsFormat.R32_SFloat,
                    GraphicsFormat.R32G32_SFloat,
                    GraphicsFormat.R32G32B32_SFloat,
                    GraphicsFormat.R32G32B32A32_SFloat,
                }
            },
        };

        public static bool GetCompatibleFormat(GraphicsFormat format, out MatType t) {
            t = MatType.CV_8U;
            var comp = _formatsEquals.FirstOrDefault(c => c.unity.Contains(format));
            if (comp == null)
                return false;

            t = MatType.MakeType(comp.opencvDepth, (int) GraphicsFormatUtility.GetComponentCount(format));
            return true;
        }
        
        public static bool GetCompatibleFormat(MatType t, out GraphicsFormat format) {
            format = GraphicsFormat.None;
            var comp = _formatsEquals.FirstOrDefault(c => c.opencvDepth == t.Depth);
            if (comp == null)
                return false;

            format = comp.unity.FirstOrDefault(f => t.Channels == (int) GraphicsFormatUtility.GetComponentCount(f));
            if (format == GraphicsFormat.None)
                return false;
            return true;
        }
        
        public static bool GetCompatibleFormat(MatType t, out TextureFormat format) {
            if (GetCompatibleFormat(t, out GraphicsFormat gf)) {
                format = GraphicsFormatUtility.GetTextureFormat(gf);
                return true;
            }

            format = TextureFormat.Alpha8;
            return false;
        }

        public static bool GetConversionToGray(GraphicsFormat format, out ColorConversionCodes code) {
            return GetConversionGray(format, out code, out _);
        }

        public static bool GetConversionFromGray(GraphicsFormat format, out ColorConversionCodes code) {
            return GetConversionGray(format, out _, out code);
        }

        public static bool GetConversionGray(GraphicsFormat format, out ColorConversionCodes codeTo, out ColorConversionCodes codeFrom) {
            switch (format) {
                case GraphicsFormat.R8_SRGB:
                case GraphicsFormat.R8G8_SRGB:
                case GraphicsFormat.R8G8B8_SRGB:
                case GraphicsFormat.R8G8B8_UNorm:
                case GraphicsFormat.R8G8B8_SNorm:
                case GraphicsFormat.R8G8B8_UInt:
                case GraphicsFormat.R8G8B8_SInt:
                case GraphicsFormat.R16G16B16_UNorm:
                case GraphicsFormat.R16G16B16_SNorm:
                case GraphicsFormat.R16G16B16_UInt:
                case GraphicsFormat.R16G16B16_SInt:
                case GraphicsFormat.R32G32B32_UInt:
                case GraphicsFormat.R32G32B32_SInt:
                case GraphicsFormat.R16G16B16_SFloat:
                case GraphicsFormat.R32G32B32_SFloat:
                    codeTo = ColorConversionCodes.RGB2GRAY;
                    codeFrom = ColorConversionCodes.GRAY2RGB;
                    break;
                
                
                case GraphicsFormat.R8G8B8A8_SRGB:
                case GraphicsFormat.R8G8B8A8_UNorm:
                case GraphicsFormat.R8G8B8A8_SNorm:
                case GraphicsFormat.R8G8B8A8_UInt:
                case GraphicsFormat.R8G8B8A8_SInt:
                case GraphicsFormat.R16G16B16A16_UNorm:
                case GraphicsFormat.R16G16B16A16_SNorm:
                case GraphicsFormat.R16G16B16A16_UInt:
                case GraphicsFormat.R16G16B16A16_SInt:
                case GraphicsFormat.R32G32B32A32_UInt:
                case GraphicsFormat.R32G32B32A32_SInt:
                case GraphicsFormat.R16G16B16A16_SFloat:
                case GraphicsFormat.R32G32B32A32_SFloat:
                case GraphicsFormat.R4G4B4A4_UNormPack16:
                    codeTo = ColorConversionCodes.RGBA2GRAY;
                    codeFrom = ColorConversionCodes.GRAY2RGBA;
                    break;
                
                case GraphicsFormat.B8G8R8_SRGB:
                case GraphicsFormat.B8G8R8_UNorm:
                case GraphicsFormat.B8G8R8_SNorm:
                case GraphicsFormat.B8G8R8_UInt:
                case GraphicsFormat.B8G8R8_SInt:
                    codeTo = ColorConversionCodes.BGR2GRAY;
                    codeFrom = ColorConversionCodes.GRAY2BGR;
                    break;
                
                case GraphicsFormat.B8G8R8A8_SRGB:
                case GraphicsFormat.B8G8R8A8_UNorm:
                case GraphicsFormat.B8G8R8A8_SNorm:
                case GraphicsFormat.B8G8R8A8_UInt:
                case GraphicsFormat.B8G8R8A8_SInt:
                case GraphicsFormat.B4G4R4A4_UNormPack16:
                    codeTo = ColorConversionCodes.BGRA2GRAY;
                    codeFrom = ColorConversionCodes.GRAY2BGRA;
                    break;
                    
                case GraphicsFormat.B5G6R5_UNormPack16:
                    codeTo = ColorConversionCodes.BGR5652GRAY;
                    codeFrom = ColorConversionCodes.GRAY2BGR565;
                    break;
                
                default:
                    codeTo = ColorConversionCodes.RGB2GRAY;
                    codeFrom = ColorConversionCodes.GRAY2RGB;
                    return false;
            }

            return true;
        }
    }
}