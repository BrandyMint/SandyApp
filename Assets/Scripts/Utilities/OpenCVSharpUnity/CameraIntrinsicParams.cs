using Newtonsoft.Json;
using OpenCvSharp;
using UnityEngine;

namespace Utilities.OpenCVSharpUnity {
    [Serializable]
    public class CameraIntrinsicParams {
        public double[,] cameraMatrix;
        public double[] distCoeffs;
        public Size imgSizeUsedOnCalculate;
        public double reprojError;
        
        [JsonIgnore]
        public float focusX {
            get { return (float) cameraMatrix[0, 0]; }
            set { cameraMatrix[0, 0] = value; }
        }
        [JsonIgnore]
        public float focusY {
            get { return (float) cameraMatrix[1, 1]; }
            set { cameraMatrix[1, 1] = value; }
        }
        [JsonIgnore]
        public float principalX {
            get { return (float) cameraMatrix[0, 2]; }
            set { cameraMatrix[0, 2] = value; }
        }
        [JsonIgnore]
        public float principalY {
            get { return (float) cameraMatrix[1, 2]; }
            set { cameraMatrix[1, 2] = value; }
        }

        public CameraIntrinsicParams() { }

        public CameraIntrinsicParams(Size imgSizeUsedOnCalculate, int distCoefsCount = 5) {
            cameraMatrix = new double[3,3];
            distCoeffs = new double[distCoefsCount];
            this.imgSizeUsedOnCalculate = imgSizeUsedOnCalculate;
        }
        
        public CameraIntrinsicParams(Vector2 fov, Size imgSizeUsedOnCalculate, int distCoefsCount = 5) 
            : this(imgSizeUsedOnCalculate, distCoefsCount) 
        {
            principalX = (float) imgSizeUsedOnCalculate.Width / 2f;
            principalY = (float) imgSizeUsedOnCalculate.Height / 2f;
            focusX = principalX / Mathf.Tan(fov.x / 2f * Mathf.Deg2Rad);
            focusY = principalY / Mathf.Tan(fov.y / 2f * Mathf.Deg2Rad);
            cameraMatrix[2, 2] = 1d;
        }
        
        //TODO: need testing
        public Matrix4x4 CreateProjectionMatrix(float width, float height, float near, float far, out float newFov) {
            var realCamFOV = GetFOV();
            var fovAspect = realCamFOV.x / realCamFOV.y;
            var cameraAspect = width / height;
            Vector2 newPrincipal;
            if (cameraAspect > fovAspect) {
                newFov = realCamFOV.y;
                var videoScale = height / imgSizeUsedOnCalculate.Height;
                newPrincipal.x = (width - videoScale * imgSizeUsedOnCalculate.Width) / 2f + videoScale * principalX;
                newPrincipal.y = videoScale * principalY;
            } else {
                newFov = realCamFOV.x / cameraAspect;
                var videoScale = width / imgSizeUsedOnCalculate.Width;
                newPrincipal.x = videoScale * principalX;
                newPrincipal.y = (height - videoScale * imgSizeUsedOnCalculate.Height) / 2f + videoScale * principalY;
            }

            var obliqueX = 1f - 2f * newPrincipal.x / width;
            var obliqueY = 1f - 2f * newPrincipal.y / height;
            
            var perspective = Matrix4x4.Perspective(newFov, cameraAspect, near, far);
            perspective.m02 = obliqueX;
            perspective.m12 = - obliqueY;
            return perspective;
            
            /*var fw = imgSizeUsedOnCalculate.Width;
            var fh = imgSizeUsedOnCalculate.Height;
            var proj = new Matrix4x4 {
                m00 = 2f * focusX / fw,
                m02 = 1f - 2f * principalX / fw,
                m11 = 2f * focusY / fh,
                m12 = 1f - 2f * principalY / fh,
                m22 = -(far + near) / (far - near),
                m23 = -(2f * far * near) / (far - near),
                m32 = -1f
            };
            return proj;*/
        }

        public Vector2 GetFOV() {
            /*return new Vector2 {
                x = Mathf.Atan(imgSizeUsedOnCalculate.Width / 2f / focusX),
                y = Mathf.Atan(imgSizeUsedOnCalculate.Height / 2f / focusY)
            } * 2f * Mathf.Rad2Deg;*/
            Point2d cvFov;
            double focalLength, aspect;
            Point2d principalPoint;
            Cv2.CalibrationMatrixValues(cameraMatrix, imgSizeUsedOnCalculate, 0f, 0f,
                out cvFov.X, out cvFov.Y, out focalLength, out principalPoint, out aspect);
            return Convert.ToVector2(cvFov);
        }
    }
}