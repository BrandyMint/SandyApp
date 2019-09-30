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
		
		public static DepthSensorManager Instance {get; private set;}
		public DepthSensorDevice Device {get; private set;}
		public event Action OnInitialized;

		private DepthSensorDevice.Internal _internalDevice;

		private bool _initializing;

		private void Awake() {
			Instance = this;
			DontDestroyOnLoad(gameObject);
		}

		private void Start() {
			TryInit();
		}

		private void OnDestroy() {
			Stop(true);
		}

		public void TryInit() {
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
			foreach (var typeSensor in _TRYING_INIT_QUEUE) {
				if (CreateDeviceFromType(typeSensor)) {
					var maxWait = Time.realtimeSinceStartup + _WAIT_AVAILABLE;
					yield return new WaitUntil(() => IsInitialized() || Time.realtimeSinceStartup > maxWait);
					if (IsInitialized()) break;
				}
			}
			
			if (IsInitialized())
				Initialized();
			else
				Debug.LogWarning(GetType().Name + ": No devices are available!");
			_initializing = false;
		}

		private bool CreateDeviceFromType(Type type) {
			try {
				Debug.Log("Initializing " + type.Name);
				Device = (DepthSensorDevice) Activator.CreateInstance(type);
				_internalDevice = new DepthSensorDevice.Internal(Device);
				return true;
			} catch (EntryPointNotFoundException e) {
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

