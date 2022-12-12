using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using Random = UnityEngine.Random;

namespace Wordector
{
    public class WordDictionary : IWordDictionary
    {
        private HashSet<string> Words;
        private List<string> AllWords;

        private bool Initialized;

        private string _localeId;

        public WordDictionary(string localeId, string encryptedDictionary)
        {
            _localeId = localeId;
            Words = new HashSet<string>();
            AllWords = new List<string>();

            var words = DecryptList(encryptedDictionary);
            if (words != null && words.Count > 0)
            {
                foreach (var w in words)
                {
                    var word = w.ToString().ToLowerInvariant().Replace('é', 'e');
                    Words.Add(word);
                    AllWords.Add(word);
                }
                Initialized = true;
            }
        }

        public bool HasPossibleWords(string[] letters, int maxLength)
        {
            return GetPossibleWords(letters, maxLength).Count > 0;
        }

        public string GetRandomWord()
        {
            return AllWords[Random.Range(0, AllWords.Count)];
        }

        public List<string> GetAllWords()
        {
            return AllWords;
        }

        public IList<string> GetPossibleWords(string[] letters, int maxLength)
        {
            string lettersString = string.Join("", letters);
            string regexMatch = "^[{" + lettersString + "}]+$";
            IList<string> possibleWords = Words.Where(x => x.Length <= maxLength && Regex.Match(x, regexMatch, RegexOptions.IgnoreCase).Success).ToList();

            IList<string> validWords = new List<string>();
            foreach (var w in possibleWords)
            {
                var thisWordLetters = (lettersString + "").ToUpper();

                var word = w.ToUpper();

                if (word.Length <= 1)
                {
                    continue;
                }

                var valid = true;
                for (int i = 0; i < word.Length; i++)
                {
                    var l = word.Substring(i, 1);

                    if (thisWordLetters.Contains(l))
                    {
                        var regex = new Regex(Regex.Escape(l));
                        thisWordLetters = regex.Replace(thisWordLetters, "", 1);
                    }
                    else
                    {
                        valid = false;
                    }
                }

                if (valid)
                {
                    validWords.Add(word);
                }
            }

            return validWords;
        }

        public bool CheckWord(string word)
        {
            word = word.ToLowerInvariant();

            if (Words != null && Words.Count > 0)
            {
                return Words.Contains(word);
            }

            return false;
        }

        public static string EncryptList(List<string> words)
        {
            var jsonString = UnityEngine.Purchasing.MiniJSON.Json.Serialize(words);
            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonString);
            jsonBytes = Encryption.CompressWithGzip(jsonBytes);
            return Convert.ToBase64String(jsonBytes);
        }

        public static IList DecryptList(string wordsText)
        {
            try
            {
                byte[] gzippedBytes = Convert.FromBase64String(wordsText);
                byte[] rawBytes = Encryption.ReadCompressedDataWithGzip(gzippedBytes);

                string wordsJson = System.Text.Encoding.UTF8.GetString(rawBytes);
                IList words = (IList)UnityEngine.Purchasing.MiniJSON.Json.Deserialize(wordsJson);

                return words;

            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                return null;
            }
        }
    }
}