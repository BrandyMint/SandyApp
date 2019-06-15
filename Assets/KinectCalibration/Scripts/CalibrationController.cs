using System;
using HumanCollider;
using Launcher.Scripts;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Launcher.KinectCalibration {
    public class CalibrationController : MonoBehaviour {
        [SerializeField] private GameObject kinectField;
        [SerializeField] private GameObject grid;
        [SerializeField] private Transform pnlSettings;

        private class SliderField {
            public Slider sl { get; set; }
            public Text txtVal { get; set; }
            public Button btnInc { get; set; }
            public Button btnDec { get; set; }
        }
        private class Fields {
            public SliderField PosX { get; set; }
            public SliderField PosY { get; set; }
            public SliderField Size { get; set; }
            public SliderField ZoneCut { get; set; }
            public SliderField ZoneTouch { get; set; }
            public SliderField ZoneTouchFoot { get; set; }
        }
        private Fields fields;
        private const float COUNT_INC_DEC_STEPS = 200.0f;
        private HumanMaskCreater hmc;

        void Start() {
            hmc = HumanMaskCreater.GetInstance();

            fields = new Fields();
            SetPropsByGameObjects(fields, pnlSettings, 1);

            SetField(fields.PosX, KinectSettings.PosX, val => UpdatePos());
            SetField(fields.PosY, KinectSettings.PosY, val => UpdatePos());
            SetField(fields.Size, KinectSettings.Size, UpdateSize);
            SetField(fields.ZoneCut, KinectSettings.ZoneCut, val => hmc.zoneCut = (ushort) val);
            SetField(fields.ZoneTouch, KinectSettings.ZoneTouch, val => hmc.zoneTouch = (ushort) val);
            SetField(fields.ZoneTouchFoot, KinectSettings.ZoneTouchFoot, val => hmc.zoneTouchFoot = (ushort) val);
        }

        private static void SetPropsByGameObjects(object obj, Transform root, uint depth) {
            foreach (var propInfo in obj.GetType().GetProperties()) {
                var row = root.Find(propInfo.Name);
                object prop;
                if (depth == 0) {
                    prop = row.GetComponent(propInfo.PropertyType.Name);
                } else {
                    prop = Activator.CreateInstance(propInfo.PropertyType);
                    SetPropsByGameObjects(prop, row, depth - 1);
                }
                propInfo.SetValue(obj, prop, null);
            }
        }

        public void OnBtnCreateDepthSnapshot() {
            hmc.CreateStaticSceneDepth();
        }

        public void OnBtnSave() {
            KinectSettings.PosX = fields.PosX.sl.value;
            KinectSettings.PosY = fields.PosY.sl.value;
            KinectSettings.Size = fields.Size.sl.value;
            KinectSettings.ZoneCut = (int) fields.ZoneCut.sl.value;
            KinectSettings.ZoneTouch = (int) fields.ZoneTouch.sl.value;
            KinectSettings.ZoneTouchFoot = (int) fields.ZoneTouchFoot.sl.value;
            KinectSettings.Save();
            if (GamesLoader.Instance() != null)
                GamesLoader.Instance().UnLoadKinectCalibration();
        }

        public void OnBtnCancel() {
            hmc.zoneCut = (ushort) KinectSettings.ZoneCut;
            hmc.zoneTouch = (ushort) KinectSettings.ZoneTouch;
            if (GamesLoader.Instance() != null)
                GamesLoader.Instance().UnLoadKinectCalibration();
        }

        public void OnTglGrid(bool enable) {
            grid.SetActive(enable);
        }

        private void SetField(SliderField fld, float loadedVal, UnityAction<float> act) {
            UnityAction<float> changeTxt = val => {
                fld.txtVal.text = (fld.sl.wholeNumbers)
                    ? val.ToString()
                    : val.ToString("0.000");
            };
            changeTxt(loadedVal);
            fld.sl.onValueChanged.AddListener(changeTxt);
            fld.sl.onValueChanged.AddListener(act);
            fld.sl.value = loadedVal;

            fld.btnInc.onClick.AddListener(CreateOnBtnIncDec(fld.sl, 1.0f));
            fld.btnDec.onClick.AddListener(CreateOnBtnIncDec(fld.sl, -1.0f));
        }

        private UnityAction CreateOnBtnIncDec(Slider sl, float mult) {
            return () => {
                var val = (sl.wholeNumbers) ?
                    1 :
                    (sl.maxValue - sl.minValue) / COUNT_INC_DEC_STEPS;
                sl.value = sl.value + val * mult;
            };
        }

        private void UpdatePos() {
            UpdatePos(kinectField);
        }

        private void UpdatePos(GameObject obj) {
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.localPosition = new Vector3(fields.PosX.sl.value, fields.PosY.sl.value, 0.0f);
        }

        private void UpdateSize(float val) {
            UpdateSize(kinectField, val);
        }

        private void UpdateSize(GameObject obj, float val) {
            RectTransform rect = obj.GetComponent<RectTransform>();
            var size = KinectSettings.INITIAL_SIZE * val;
            rect.localScale = new Vector3(size, size, 1);
        }
    }
}