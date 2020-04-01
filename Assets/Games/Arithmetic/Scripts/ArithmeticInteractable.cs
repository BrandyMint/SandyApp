using Games.Common.GameFindObject;
using UnityEngine;
using UnityEngine.UI;

namespace Games.Arithmetic {
    public class ArithmeticInteractable : InteractableModel {
        [SerializeField] private Text _txt;
        
        public override int ItemType {
            get => _itemType;
            set {
                _itemType = value;
                _txt.text = _itemType.ToString(); 
            }
        }
    }
}