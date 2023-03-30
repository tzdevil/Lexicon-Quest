using System;
using UnityEngine;

namespace WordSleuth.Gameplay
{
    [Serializable]
    public class HighScore
    {
        public HighScore(int score, float time)
        {
            var exactTime = DateTime.Now;

            string hour = exactTime.Hour.ToString().PadLeft(2, '0');
            string minute = exactTime.Minute.ToString().PadLeft(2, '0');
            string second = exactTime.Second.ToString().PadLeft(2, '0');

            Date = $"{exactTime.Day}/{exactTime.Month}/{exactTime.Year} {hour}:{minute}:{second}";

            Score = score;
            Time = time;
        }

        [field: SerializeField] public string Date { get; private set; }
        [field: SerializeField] public int Score { get; private set; }
        [field: SerializeField] public float Time { get; private set; }
    }
}