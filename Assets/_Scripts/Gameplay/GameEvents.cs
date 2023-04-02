using UnityEngine;
using UnityEngine.Events;

namespace LexiconQuest.Gameplay
{
    public class GameEvents : MonoBehaviour
    {
        private void Awake() => DontDestroyOnLoad(gameObject);

        public UnityEvent<HighScore> OnNewHighscore;

        public void RaiseNewHighscore(HighScore highScore)
            => OnNewHighscore?.Invoke(highScore);
    }
}