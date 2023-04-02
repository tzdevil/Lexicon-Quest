using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace WordSleuth.Gameplay
{
    public class GameManager : MonoBehaviour
    {
        private System.Random _random;

        [Header("Resources")]
        [SerializeField] private TextAsset _wordData;

        [Header("Event References")]
        [SerializeField] private GameEvents _gameEvents;

        [Header("Object References")]
        [SerializeField] private Transform _lettersTransform;

        [Header("UI References")]
        [SerializeField] private TMP_Text _gameTimeText;
        [SerializeField] private TMP_Text _guessingTimeText;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _scoreYouCanGainText;
        [SerializeField] private GameObject _pauseButton;

        [Header("Game UI References")]
        [SerializeField] private TMP_Text _definitionText;
        [SerializeField] private TMP_Text _questionIndexText;
        [SerializeField] private TMP_Text _correctWord;

        [Header("Guessing References")]
        [SerializeField] private GameObject _guessWordPanel;
        [SerializeField] private TMP_InputField _guessInput;
        [SerializeField] private TMP_Text _correctOrWrongText;

        [Header("Panel References")]
        [SerializeField] private GameObject _continuePanel;
        [SerializeField] private GameObject _loadingPanel;
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private GameObject _pauseMenuPanel;

        [Header("Game Over References")]
        [SerializeField] private TMP_Text _finalScoreText;
        [SerializeField] private TMP_Text _lastQuestionText;
        [SerializeField] private TMP_Text _correctAnswersText;
        [SerializeField] private TMP_Text _finalTimeText;

        [Header("Data References")]
        [SerializeField] private List<string> _textData;

        [Header("Word Related")]
        [SerializeField] private List<WordData> _wordList;
        [SerializeField] private WordData _currentWord;

        [Header("Game Related")]
        [SerializeField] private bool _playing;
        [SerializeField] private bool _paused;
        [SerializeField] private float _gameTime;
        [SerializeField] private int _questionIndex;
        [SerializeField] private int _currentScore;
        [SerializeField] private int _scoreYouCanGain;
        [SerializeField] private int _correctAnswersCount;

        [Header("Guessing Related")]
        [SerializeField] private float _guessingTime;
        [SerializeField] private bool _guessing;
        [SerializeField] private bool _canContinue;

        [Header("Canvas Related")]
        [SerializeField] private RectTransform _letterBackgroundPrefab;
        [SerializeField] private List<RectTransform> _letterRectTransforms; // TODO: listeye koyup sýfýrlayacaðým Restart'ta
        [SerializeField] private List<TMP_Text> _letterTexts;

        private void Awake()
        {
            InitPanels();

            _playing = false;
            _gameTime = 180;
            _random = new System.Random();

            SetupTexts();

            ReadData();
        }

        #region Event System
        private void OnEnable()
        {
            if (_gameEvents == null)
                _gameEvents = GameObject.Find("GameEvents").GetComponent<GameEvents>();
        }
        #endregion

        private void SetupTexts()
        {
            string minutes = ((int)(_gameTime / 60)).ToString().PadLeft(2, '0');
            string seconds = ((int)(_gameTime % 60)).ToString().PadLeft(2, '0');
            _gameTimeText.SetText($"{minutes}:{seconds}");
            _scoreText.SetText($"0");
            _questionIndexText.SetText($"Question: 1/12");
        }

        private void InitPanels()
        {
            _gameOverPanel.SetActive(false);
            _continuePanel.SetActive(false);
            _pauseMenuPanel.SetActive(false);
            _loadingPanel.SetActive(true);
        }

        private void Start()
        {
            SetupWords();
        }

        private void SetupWords()
        {
            for (int i = 4; i < 10; i++)
                for (int j = 0; j < 2; j++)
                    StartCoroutine(GetWord(i));
        }

        private void Update()
        {
            CheckGameTime();

            CheckGuessingTime();

            KeyboardShortcuts();
        }

        private void KeyboardShortcuts()
        {
            if (_guessing && !_paused && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
                ConfirmGuess();

            if (_canContinue && !_paused && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
                StartNewRound();

            if (_playing && !_paused && Input.GetKeyDown(KeyCode.G))
                TryGuessWord();

            if (_playing && !_paused && (Input.GetKeyDown(KeyCode.L)))
                GetNewLetter();

            if ((_playing || _guessing) && (Input.GetKeyDown(KeyCode.Escape)))
                PauseGame(!_paused);
        }

        private void CheckGameTime()
        {
            if (_playing && !_paused)
            {
                _gameTime -= Time.deltaTime;
                if (_gameTime <= 0)
                    GameOver();
                else
                {
                    string minutes = ((int)(_gameTime / 60)).ToString().PadLeft(2, '0');
                    string seconds = ((int)(_gameTime % 60)).ToString().PadLeft(2, '0');
                    _gameTimeText.SetText($"{minutes}:{seconds}");
                }
            }
        }

        private void CheckGuessingTime()
        {
            if (_guessing && !_paused)
            {
                _guessingTime -= Time.deltaTime;
                if (_guessingTime <= 0)
                    GuessWord(false);
                else
                    _guessingTimeText.SetText(_guessingTime.ToString("0"));
            }
        }

        public void GetNewLetter()
        {
            if (!_playing)
                return;

            if (_currentWord.VisibleLetterCount < _currentWord.Length - 1)
            {
                var invisibleLetters = _currentWord.Letters.Select((c, i) => new { Char = c, Index = i })
                                                           .Where(x => x.Char == '\0')
                                                           .ToArray();
                var letterForIndex = invisibleLetters.ElementAt(_random.Next(invisibleLetters.Length));
                var letterInWord = _currentWord.Word[letterForIndex.Index];
                _currentWord.Letters[letterForIndex.Index] = letterInWord;
                _currentWord.VisibleLetterCount++;

                _letterTexts[letterForIndex.Index].SetText(letterInWord.ToString());

                _scoreYouCanGain -= 100;
                _scoreYouCanGainText.SetText(_scoreYouCanGain.ToString());
            }
        }

        private void ReadData()
        {
            string[] wordData = _wordData.text.Replace(" ", "").Split('\n');
            _textData = wordData.Where(word => word.Length > 4 && word.Length <= 10)
                                .Select(w => w[..^1])
                                .Where(w => Regex.IsMatch(w, @"^[a-zA-Z]+$"))
                                .Select(w => w.ToUpper())
                                .ToList();
        }

        IEnumerator GetWord(int wordLength)
        {
            var newData = _textData.Where(w => w.Length == wordLength).ToList();
            var word = newData[_random.Next(newData.Count)];
            string url = $"https://api.dictionaryapi.dev/api/v2/entries/en/{word}";
            UnityWebRequest www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
                StartCoroutine(GetWord(wordLength));
            else
            {
                string json = www.downloadHandler.text;
                JArray jsonArray = JArray.Parse(json);
                JObject jsonObject = (JObject)jsonArray[0];
                JArray definitionsArray = (JArray)jsonObject["meanings"][0]["definitions"];
                string definition = (string)definitionsArray[0]["definition"];

                _wordList.Add(new(word, definition));

                if (_wordList.Count == 12)
                {
                    ChooseWord(0);
                    _loadingPanel.SetActive(false);
                    _pauseButton.SetActive(true);
                }
            }
        }

        private void ChooseWord(int index)
        {
            _wordList = _wordList.OrderBy(w => w.Length).ToList();
            _currentWord = _wordList[index];

            foreach (var l in _letterTexts)
                l.SetText("");

            if (index == 0)
            {
                for (int i = 0; i < _currentWord.Length; i++)
                {
                    var letterBg = Instantiate(_letterBackgroundPrefab, _lettersTransform);
                    _letterRectTransforms.Add(letterBg);
                    letterBg.anchoredPosition = new(-756.5f + 115 * i, -35);
                    _letterTexts.Add(letterBg.GetChild(0).GetComponent<TMP_Text>());
                }
            }
            else if (index % 2 == 0)
            {
                var letterBg = Instantiate(_letterBackgroundPrefab, _lettersTransform);
                _letterRectTransforms.Add(letterBg);
                letterBg.anchoredPosition = new(-756.5f + 115 * (_currentWord.Length - 1), -35);
                _letterTexts.Add(letterBg.GetChild(0).GetComponent<TMP_Text>());
            }

            _definitionText.SetText(_currentWord.Definition);
            _questionIndexText.SetText($"Question: {_questionIndex+1}/12");
            _playing = true;

            _scoreYouCanGain = _currentWord.Length * 100;
            _scoreYouCanGainText.SetText(_scoreYouCanGain.ToString());
        }

        public void TryGuessWord()
        {
            if (!_playing)
                return;

            _playing = false;
            _guessingTime = 20;
            _guessing = true;
            _guessInput.text = "";
            _guessWordPanel.SetActive(true);
            _guessInput.ActivateInputField();
        }

        public void ConfirmGuess()
        {
            var word = _guessInput.text.Replace(" ", "").ToUpperInvariant();

            if (word == _currentWord.Word.ToUpperInvariant())
                GuessWord(true);
            else
            {
                _guessInput.text = "";
                _guessInput.ActivateInputField();
            }
        }

        private void GuessWord(bool guessedCorrect)
        {
            _correctOrWrongText.SetText(guessedCorrect ? "CORRECT" : "WRONG");
            _correctWord.SetText(_currentWord.Word);
            _continuePanel.SetActive(true);

            _guessWordPanel.SetActive(false);

            if (guessedCorrect)
                _correctAnswersCount++;

            var score = 100 * (_currentWord.Length - _currentWord.VisibleLetterCount);
            _currentScore = Mathf.Clamp(_currentScore + (guessedCorrect ? score : -score), 0, 9999999);

            _scoreText.SetText(_currentScore.ToString());

            _guessing = false;
            _canContinue = true;
        }

        public void StartNewRound()
        {
            _canContinue = false;

            _continuePanel.SetActive(false);

            _questionIndex++;

            if (_questionIndex == 12)
                GameOver();
            else
                ChooseWord(_questionIndex);
        }

        private void GameOver()
        {
            _gameEvents.RaiseNewHighscore(new(_currentScore, _gameTime));

            _playing = false;
            _finalScoreText.SetText($"Score: {_currentScore}");
            _lastQuestionText.SetText($"Last Question: {_questionIndex}");
            _correctAnswersText.SetText($"Correct Answers: {_correctAnswersCount}");

            string minutes = ((int)(_gameTime / 60)).ToString().PadLeft(2, '0');
            string seconds = ((int)(_gameTime % 60)).ToString().PadLeft(2, '0');
            _finalTimeText.SetText($"Time: {minutes}:{seconds}");

            _gameOverPanel.SetActive(true);
        }

        public void RestartGame()
        {
            _playing = false;

            InitPanels();

            _questionIndex = 0;
            _currentScore = 0;
            _gameTime = 180;
            _wordList.Clear();
            _currentWord = null;
            _correctAnswersCount = 0;

            foreach (var v in _letterRectTransforms)
                Destroy(v.gameObject);

            _letterRectTransforms.Clear();

            SetupTexts();

            SetupWords();
        }

        public void PauseGame(bool pausing)
        {
            _pauseButton.SetActive(!pausing);
            _pauseMenuPanel.SetActive(pausing);
            _paused = pausing;
        }

        public void ReturnToMainMenu()
            => StartCoroutine(LoadScene());

        IEnumerator LoadScene()
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);

            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }
    }
}