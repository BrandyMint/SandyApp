using System;
using System.Collections;
using DepthSensor.Device;
using UnityEngine;

namespace DepthSensor {
	public class DepthSensorManager : MonoBehaviour {
		private const float _WAIT_AVAILABLE = 3f;
		private Type[] _TRYING_INIT_QUEUE = new Type[] {
#if UNITY_STANDALONE_WIN
			typeof(Kinect2Device), 
			//typeof(Kinect1Device),
#endif
#if !DISABLE_OPENNI2
			typeof(OpenNI2Device)
#endif
		};
		private Type[] _TRYING_INIT_QUEUE_RECORD_PLAYER = new Type[] {
			typeof(RecordPlayerDevice)
		};
		
		public static DepthSensorManager Instance {get; private set;}
		public DepthSensorDevice Device {get; private set;}
		public static event Action OnInitialized;

		private DepthSensorDevice.Internal _internalDevice;
		private bool _initializing;
		private string _recordPath;

		private void Awake() {
			Instance = this;
			DontDestroyOnLoad(gameObject);
			if (!Prefs.Sensor.Load())
				Prefs.Sensor.Save();
		}

		private void Start() {
#if !UNITY_EDITOR
			Application.targetFrameRate = 30;
#endif
			if (!string.IsNullOrEmpty(Prefs.CmdLine.Record))
				OpenRecord(Prefs.CmdLine.Record);
			else
				OpenDevice();
		}

		private void OnDestroy() {
			Stop(true);
		}
		
		public void OpenRecord(string recordPath = null) {
			_recordPath = recordPath;
			ReOpen();
		}

		public void OpenDevice() {
			_recordPath = null;
			ReOpen();
		}

		public void ReOpen() {
			Stop();
			StartCoroutine(Initing());
		}

		public void Stop(bool force = false) {
			if (_initializing && !force)
				return;
			StopAllCoroutines();
			if (Device != null) {
				_internalDevice.Close();
				Device = null;
			}
		}

		private IEnumerator Initing() {
			_initializing = true;
			var initQueue = string.IsNullOrEmpty(_recordPath) ? _TRYING_INIT_QUEUE : _TRYING_INIT_QUEUE_RECORD_PLAYER;
			foreach (var typeSensor in initQueue) {
				var isDeviceCreated = string.IsNullOrEmpty(_recordPath) 
					? CreateDeviceFromType(typeSensor)
					: CreateDeviceFromType(typeSensor, _recordPath);
				if (isDeviceCreated) {
					var maxWait = Time.realtimeSinceStartup + _WAIT_AVAILABLE;
					yield return new WaitUntil(() => IsInitialized() || Time.realtimeSinceStartup > maxWait);
					if (IsInitialized()) {
						break;
					} else {
						_internalDevice?.Close();
					}
				}
			}
			
			if (IsInitialized())
				Initialized();
			else
				Debug.LogWarning(GetType().Name + ": No devices are available!");
			_initializing = false;
		}

		private bool CreateDeviceFromType(Type type, params object[] args) {
			try {
				Debug.Log("Initializing " + type.Name);
				Device = (DepthSensorDevice) Activator.CreateInstance(type, args);
				_internalDevice = new DepthSensorDevice.Internal(Device);
				return true;
			}
			catch (Exception e) {
				Debug.Log(e.Data);
				Debug.Log(e.HelpLink);
				Debug.LogException(e);
				return false;
			}
		}

		private void Initialized() {
			StartCoroutine(_internalDevice.Update());
			if (OnInitialized != null) OnInitialized();
		}

		public static bool IsInitialized() {
			return Instance != null && Instance.Device != null 
			       && Instance.Device.IsAvailable();
		}

		public static bool Initializing() {
			return Instance != null && Instance._initializing;
		}
	}
}

