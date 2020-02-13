using System.Collections;
using Games.Common.GameFindObject;
using UnityEngine;
using UnityEngine.UI;

namespace Games.Arithmetic {
    public class ArithmeticInteractable : Interactable {
        [SerializeField] private Text _txt;
        
        public override int ItemType {
            get => _itemType;
            set {
                _itemType = value;
                _txt.text = _itemType.ToString(); 
            }
        }

        public override void Show(bool show) {
            base.Show(show);
            _txt.gameObject.SetActive(show);
            GetComponent<Collider>().enabled = show;
        }

        public override void Bang(bool isRight) {
            GetComponent<Collider>().enabled = false;
            
            StartCoroutine(PlayParticles(isRight ? _rightBang : _wrongBang));
            PlayAudioBang(isRight);
        }

        private IEnumerator PlayParticles(ParticleSystem particles) {
            particles.Play();
            yield return new WaitForSeconds(particles.main.duration + particles.main.startLifetime.constant);
        }
    }
}