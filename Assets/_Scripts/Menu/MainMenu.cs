using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LexiconQuest.Menu
{
    public class MainMenu : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject _mainMenuPanel;
        [SerializeField] private GameObject _highscoresPanel;

        public void StartGame()
        {
            StartCoroutine(LoadScene());
        }

        public void ExitGame()
        {
            Application.Quit();
        }

        IEnumerator LoadScene()
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(1, LoadSceneMode.Single);

            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }

        public void ShowHighscores()
        {
            _mainMenuPanel.SetActive(false);
            _highscoresPanel.SetActive(true);
        }

        public void ReturnToMenu()
        {
            _mainMenuPanel.SetActive(true);
            _highscoresPanel.SetActive(false);
        }
    }
}