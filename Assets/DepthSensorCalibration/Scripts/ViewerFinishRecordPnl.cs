using System;
using System.IO;
using System.Linq;
using Launcher;
using Launcher.KeyMapping;
using UnityEngine;
using UnityEngine.UI;
using Utilities;

namespace DepthSensorCalibration {
    public class ViewerFinishRecordPnl : MonoBehaviour {
        [SerializeField] private GameObject _pnl;
        [SerializeField] private RecordingController _controller;
        
        private class Field {
            public InputField fld { get; set; }
            public GameObject Error { get; set; }
        }
        
        private class Hint {
            public Text txtShortCut { get; set; }
        }

        private class UI {
            public Field Name { get; set; }
            public Hint Ok { get; set; }
            public Hint Cancel { get; set; }
            public Hint OpenDir { get; set; }
        }
        
        private GameObject _lastSelected;
        private int _overrideLayerId;
        private UI _ui = new UI();
        private string _recordPath;
        private string _defaultName;

        private void Awake() {
            _pnl.SetActive(false);
            UnityHelper.SetPropsByGameObjects(_ui, _pnl.transform);
        }

        private void Start() {
            InitHint(_ui.Ok, KeyEvent.ENTER);
            InitHint(_ui.Cancel, KeyEvent.BACK);
            InitHint(_ui.OpenDir, KeyEvent.OPEN);
            _ui.Name.fld.onValueChanged.AddListener(str => _ui.Name.Error.SetActive(false));
            KeyMapper.AddListener(KeyEvent.OPEN, OpenFolder);
            _controller.OnRecordFinished += OnRecordFinished;
        }

        private void OnDestroy() {
            KeyMapper.RemoveListener(KeyEvent.OPEN, OpenFolder);
            _controller.OnRecordFinished -= OnRecordFinished;
        }

        private void OpenUI() {
            _pnl.SetActive(true);
            _overrideLayerId = KeyMapper.PushOverrideLayer();
            var overrideEvents = KeyMapper.GetListenedEvents(EventLayer.LOCAL);
            var notOverride = new[] {KeyEvent.OPEN};
            foreach (var ev in overrideEvents.
                Except(notOverride)
            ) {
                KeyMapper.AddListener(ev, DoNothing, _overrideLayerId);
            }
            KeyMapper.AddListener(KeyEvent.BACK, OnCancel, _overrideLayerId);
            KeyMapper.AddListener(KeyEvent.ENTER, OnTryRename, _overrideLayerId);
        }

        private void CloseUI() {
            KeyMapper.PopOverrideLayer();
            _pnl.SetActive(false);
        }

        private static void DoNothing() {}

        private void OnCancel() {
            CloseUI();
        }

        private void OnTryRename() {
            var newName = _ui.Name.fld.text;
            if (_defaultName != newName) {
                try {
                    Directory.Move(
                        Path.Combine(_recordPath, _defaultName),
                        Path.Combine(_recordPath, newName)
                    );
                    CloseUI();
                } catch (Exception e) {
                    Debug.LogWarning("Fail rename record:");
                    Debug.LogException(e);
                    _ui.Name.Error.SetActive(true);
                }
            } else {
                CloseUI();
            }
        }

        private void OnRecordFinished(string path, string recordName) {
            OpenUI();
            _recordPath = path;
            _defaultName = recordName;
            _ui.Name.fld.text = recordName;
            _ui.Name.fld.Select();
            _ui.Name.fld.ActivateInputField();
            _ui.Name.Error.SetActive(false);
        }

        private void OpenFolder() {
            Application.OpenURL($"file://{_controller.RecordsPath}");
        }

        private static void InitHint(Hint hint, KeyEvent ev) {
            var key = KeyMapper.FindFirstKey(ev);
            if (key != null)
                hint.txtShortCut.text = key.ShortCut;
        }
    }
}