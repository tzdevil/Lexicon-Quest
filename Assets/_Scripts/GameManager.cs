using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace WordSleuth.Gameplay
{
    public class GameManager : MonoBehaviour
    {
        private System.Random _random;

        [Header("References")]
        [SerializeField] private Transform _lettersTransform;
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private TMP_Text _guessingTimeText;
        [SerializeField] private TMP_Text _definitionText;
        [SerializeField] private TMP_Text _questionIndexText;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private GameObject _guessWordPanel;
        [SerializeField] private TMP_InputField _guessInput;

        [Header("Data References")]
        [SerializeField] private List<string> _textData;

        [Header("Word Related")]
        [SerializeField] private List<WordData> _wordList;
        [SerializeField] private WordData _currentWord;

        [Header("Game Related")]
        [SerializeField] private bool _playing;
        [SerializeField] private float _gameTime;
        [SerializeField] private int _currentWordIndex;
        [SerializeField] private int _score;

        [Header("Guessing Related")]
        [SerializeField] private float _guessingTime;
        [SerializeField] private bool _guessing;

        [Header("Canvas Related")]
        [SerializeField] private RectTransform _letterBackgroundPrefab;
        [SerializeField] private List<TMP_Text> _letterTexts;

        private void Awake()
        {
            _playing = false;
            _gameTime = 180;
            _random = new System.Random();
            _guessingTimeText.gameObject.SetActive(false);

            _scoreText.SetText($"Score: 0");
            _scoreText.SetText($"Question: 0");

            ReadData();
        }

        private void Start()
        {
            for (int i = 4; i < 10; i++)
                for (int j = 0; j < 2; j++)
                    StartCoroutine(GetWord(i));
        }

        private void Update()
        {
            CheckGameTime();

            CheckGuessingTime();

            if (_guessing && Input.GetKeyDown(KeyCode.Return))
                ConfirmGuess();
        }

        private void CheckGameTime()
        {
            if (_playing)
            {
                _gameTime -= Time.deltaTime;
                if (_gameTime <= 0)
                {
                    print("You lost.");
                }
                else
                {
                    string minutes = ((int)(_gameTime / 60)).ToString().PadLeft(2, '0');
                    string seconds = ((int)(_gameTime % 60)).ToString().PadLeft(2, '0');
                    _timeText.SetText($"Time: {minutes}:{seconds}");
                }
            }
        }

        private void CheckGuessingTime()
        {
            if (_guessing)
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
            }
        }

        private void GetNewWord()
        {
            _currentWordIndex++;
            ChooseWord(_currentWordIndex);
        }

        private void ReadData()
        {
            TextAsset _data = Resources.Load<TextAsset>("Words");
            string[] wordData = _data.text.Replace(" ", "").Split('\n');
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
                    ChooseWord(0);
            }
        }

        private void ChooseWord(int index)
        {
            _wordList = _wordList.OrderBy(w => w.Length).ToList();
            _currentWord = _wordList[index];
            _playing = true;

            foreach (var t in _letterTexts)
                t.SetText("");

            if (index == 0)
            {
                for (int i = 0; i < _currentWord.Length; i++)
                {
                    var letterBg = Instantiate(_letterBackgroundPrefab, _lettersTransform);
                    letterBg.anchoredPosition = new(-700 + 125 * i, -135);
                    _letterTexts.Add(letterBg.GetChild(0).GetComponent<TMP_Text>());
                }
            }
            else if (index % 2 == 0)
            {
                var letterBg = Instantiate(_letterBackgroundPrefab, _lettersTransform);
                letterBg.anchoredPosition = new(-700 + 125 * (_currentWord.Length - 1), -135);
                _letterTexts.Add(letterBg.GetChild(0).GetComponent<TMP_Text>());
            }

            _definitionText.SetText(_currentWord.Definition);
            _questionIndexText.SetText($"Question: {_currentWordIndex}");
            _playing = true;
        }

        public void TryGuessWord()
        {
            if (!_playing)
                return;

            // TODO:
            // Bir InputField oluþturacaðým, oraya guess'imizi yazacaðýz. Doðru ise GuessWord(true), yanlýþ ise InputField'ý sýfýrlayacak ve baþtan yazabileceðiz.

            _playing = false;
            _guessingTime = 24;
            _guessing = true;
            _guessInput.text = "";
            _guessWordPanel.SetActive(true);
            _guessingTimeText.gameObject.SetActive(true);
        }

        public void ConfirmGuess()
        {
            var word = _guessInput.text.Replace(" ", "").ToUpperInvariant();

            if (word == _currentWord.Word.ToUpperInvariant())
                GuessWord(true);
            else
                _guessInput.text = "";
        }

        private void GuessWord(bool guessedCorrect)
        {
            _guessWordPanel.SetActive(false);
            _guessingTimeText.gameObject.SetActive(false);

            var score = 100 * (_currentWord.Length - _currentWord.VisibleLetterCount);
            _score = Mathf.Clamp(_score + (guessedCorrect ? score : -score), 0, 9999999);

            print(guessedCorrect ? $"Correct guess :) The word was {_currentWord.Word}. You got {score} points."
                                 : $"Wrong guess :( The word was {_currentWord.Word}. You lost {score} points.");

            _scoreText.SetText($"Score: {_score}");

            _guessing = false;

            GetNewWord();
        }
    }
}