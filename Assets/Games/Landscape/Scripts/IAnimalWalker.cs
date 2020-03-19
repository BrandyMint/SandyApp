using System.Collections;
using Games.Common;

namespace Games.Landscape {
    public interface IAnimalWalker {
        void Init(GameField field);
        void RandomSpawn();
        IEnumerator WalkRandom();
        float CurrentAcceleration();
    }
}