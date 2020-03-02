using UnityEngine;

namespace Games.Common.GameFindObject {
    public interface IInteractable {
        int ItemType { get; set; }
        GameObject gameObject { get; }
        void Bang(bool isRight);
        void PlayAudioBang(bool isRight);
        void Dead();
        void Show(bool show);
    }
}