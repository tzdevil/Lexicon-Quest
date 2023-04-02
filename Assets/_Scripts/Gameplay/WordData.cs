using System;
using UnityEngine;

namespace LexiconQuest.Gameplay
{
    [Serializable]
    public class WordData
    {
        public WordData(WordData wordData)
        {
            Word = wordData.Word;
            Definition = wordData.Definition;
            Length = wordData.Length;
            Letters = new char[Length];
        }

        public WordData(string word, string definition)
        {
            Word = word;
            Definition = definition;
            Length = word.Length;
            Letters = new char[Length];
        }

        [field: SerializeField] public string Word { get; private set; }
        [field: SerializeField] public string Definition { get; private set; }
        [field: SerializeField] public int Length { get; private set; }
        [field: SerializeField] public char[] Letters { get; set; }
        [field: SerializeField] public int VisibleLetterCount { get; set; }
    }
}