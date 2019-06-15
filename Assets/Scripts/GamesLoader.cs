using UnityEngine;
using UnityEngine.SceneManagement;

namespace Launcher.Scripts {
    public class GamesLoader : MonoBehaviour {

#pragma warning disable 649
        [SerializeField] private GameObject _objTimer;
        [SerializeField] private GameObject _pnlDescriptionInfo;
        [SerializeField] private GameObject[] _disablebleOnSwitchUi;
#pragma warning restore 649
        [SerializeField] private string _gameName = "Game";
        [SerializeField] private string _launcherName = "Launcher";
        [SerializeField] private string _calibrationSceneName = "KinectCalibration";
/*

        private UIElementsController _uiElementsController;
        private UsersNameController _usersNameController;*/

        private static GamesLoader _instance;

        public static GamesLoader Instance() {
            return _instance;
        }

        // ReSharper disable UnusedMember.Local
        void Awake() {
            _instance = this;
        }

        void Start() {/*
            _uiElementsController = UIElementsController.Instance();
            _usersNameController = UsersNameController.Instance();*/
            LoadGame();
        }

        void OnDestroy() {
            _instance = null;
        }

        // ReSharper restore UnusedMember.Local

        public void LoadGame() {
            UnLoadAllScenesExeptLauncher();
            SceneManager.LoadScene(_gameName, LoadSceneMode.Additive);
        }

        public void LoadKinectCalibration() {
            UnLoadAllScenesExeptLauncher();
            SceneManager.LoadScene(_calibrationSceneName, LoadSceneMode.Additive);
            SwitchUI(false);
        }

        public void UnLoadKinectCalibration() {
            SwitchUI(true);
            LoadGame();
        }

        // ReSharper disable once InconsistentNaming
        private void SwitchUI(bool active) {
            foreach (var obj in _disablebleOnSwitchUi) {
                obj.SetActive(active);
            }
        }

        public void PlayGame() {
            SendMessage("OnPreparation");
            ShowTimer(true);
            /*_uiElementsController.SetIninteractable(false);
            _uiElementsController.SetPlayStateBtns();*/
        }

        public void StopGame() {
            SendMessage("OnBreack");
            /*_usersNameController.SetToZeroUserNames();*/
            ShowTimer(false);
            UnPauseGame();
            /*_uiElementsController.SetIninteractable(true);
            _uiElementsController.SetDefaultStateBtns();*/
        }

        public void GameTimeOver() {
            /*_uiElementsController.SetDefaultStateBtns();
            _uiElementsController.SetIninteractable(true);*/
            ShowTimer(false);
            /*_usersNameController.SetToZeroUserNames();*/

        }

        public void UnLoadAllScenesExeptLauncher() {
            UnPauseGame();
            /*_uiElementsController.SetDefaultStateBtns();*/
            for (var i = SceneManager.sceneCount - 1; i > 0; i--) {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.name.Equals(_launcherName)) continue;

                SceneManager.UnloadSceneAsync(scene);
                Debug.Log("GamesLoader.UnLoadGame: " + scene.name);
            }
        }

        public void RetryGame() {
            SendMessage("OnPreparation");
            UnPauseGame();
        }

        public void PauseGame() {
            /*_uiElementsController.SetPauseStateBtns();*/
            Time.timeScale = 0.0f;
        }
        
        public void UnPauseGame() {
            /*_uiElementsController.SetPlayStateBtns();*/
            Time.timeScale = 1.0f;

        }

        private void ShowTimer(bool show) {
            _objTimer.SetActive(show);
            _pnlDescriptionInfo.SetActive(!show);
        }
    }
}