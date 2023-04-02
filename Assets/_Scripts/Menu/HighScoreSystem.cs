using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using WordSleuth.Gameplay;

namespace WordSleuth.Menu
{
    public class HighScoreSystem : MonoBehaviour
    {
        [Header("Event References")]
        [SerializeField] private GameEvents _gameEvents;

        [Header("References")]
        [SerializeField] private TMP_Text _highscoresText;

        [Header("High Score Related")]
        [SerializeField] private List<HighScore> _highScores;

        private void Awake()
        {
            LoadGame();

            DontDestroyOnLoad(gameObject);
        }

        #region Event System
        private void OnEnable()
        {
            if (_gameEvents == null)
                _gameEvents = GameObject.Find("GameEvents").GetComponent<GameEvents>();

            _gameEvents.OnNewHighscore.AddListener(NewHighScore);
        }

        private void OnDisable()
        {
            _gameEvents.OnNewHighscore.RemoveListener(NewHighScore);
        }

        private void NewHighScore(HighScore highscore)
        {
            _highScores.Add(highscore);
            _highScores = _highScores.OrderByDescending(h => h.Score).ThenByDescending(k => k.Time).ToList();
            SaveGame();
        }
        #endregion

        private void SaveGame()
        {
            if (_highScores == null)
                return;

            var jsonData = JsonConvert.SerializeObject(_highScores);
            PlayerPrefs.SetString("HighScores", jsonData);
        }

        private void LoadGame()
        {
            var jsonData = PlayerPrefs.GetString("HighScores", string.Empty);

            if (jsonData == string.Empty)
                return;

            _highScores = JsonConvert.DeserializeObject<List<HighScore>>(jsonData);
        }

        public void ShowHighscores()
        {
            StringBuilder highscores = new();

            var highscoresCount = _highScores.Count;
            for (int i = 0; i < highscoresCount; i++)
            {
                var highscore = _highScores[i];
                highscores.Append($"<color=#fade55>#{(i + 1)}</color>, {highscore.Date} - Score: {highscore.Score} & Time: {highscore.Time}\n");
            }

            _highscoresText.SetText(highscores.ToString());
        }
    }
}