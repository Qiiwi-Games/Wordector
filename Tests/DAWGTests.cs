using System;
using System.Collections.Generic;
using Wordector;
using NUnit.Framework;
using UnityEngine;
using Random = UnityEngine.Random;

public class DAWGTests
{
    private DAWGGenerator _dawgGenerator;
    private DAWGGenerator _twoWayDawgGenerator;
    private List<string> _allWords;
    private Dawg _dawg;

    private Dictionary<char, byte> LetterMap()
    {
        Dictionary<char, byte> letterMap = new Dictionary<char, byte>();
        letterMap.Add('a', 0);
        letterMap.Add('b', 1);
        letterMap.Add('c', 2);
        letterMap.Add('d', 3);
        letterMap.Add('e', 4);
        letterMap.Add('f', 5);
        letterMap.Add('g', 6);
        letterMap.Add('h', 7);
        letterMap.Add('i', 8);
        letterMap.Add('j', 9);
        letterMap.Add('k', 10);
        letterMap.Add('l', 11);
        letterMap.Add('m', 12);
        letterMap.Add('n', 13);
        letterMap.Add('o', 14);
        letterMap.Add('p', 15);
        letterMap.Add('q', 16);
        letterMap.Add('r', 17);
        letterMap.Add('s', 18);
        letterMap.Add('t', 19);
        letterMap.Add('u', 20);
        letterMap.Add('v', 21);
        letterMap.Add('w', 22);
        letterMap.Add('x', 23);
        letterMap.Add('y', 24);
        letterMap.Add('z', 25);
        letterMap.Add('å', 26);
        letterMap.Add('ä', 27);
        letterMap.Add('ö', 28);
        return letterMap;
    }
    
    private void SetupDawgSE()
    {
        if (_dawgGenerator != null) return;
        
        var textAsset = Resources.Load<TextAsset>("test_se_dict");
        var wordDictionary = new WordDictionary("se", textAsset.text);
        _allWords = wordDictionary.GetAllWords();
        _dawgGenerator = new DAWGGenerator(_allWords, LetterMap());
    }

    private void SetupTwoWayDawgSE()
    {
        if (_twoWayDawgGenerator != null) return;
        
        var textAsset = Resources.Load<TextAsset>("test_se_dict");
        var wordDictionary = new WordDictionary("se", textAsset.text);
        _allWords = wordDictionary.GetAllWords();
        _twoWayDawgGenerator = new DAWGGenerator(LetterMap());
        _twoWayDawgGenerator.AddWordBatch(_allWords);
        _twoWayDawgGenerator.AddWordBatch(_allWords, true);
        _twoWayDawgGenerator.Generate();
    }
    
    private void SetupDawg()
    {
        _dawg = new Dawg(_dawgGenerator.Data, LetterMap());
    }

    [Test]
    public void TestGeneration()
    {
        SetupDawgSE();
    }

    [Test]
    public void TestInitialTree()
    {
        SetupDawgSE();
        foreach (var word in _allWords)
        {
            if (_dawgGenerator.ValidWord(word))
            {
                bool hasWord = _dawgGenerator.HasWord(word);
                if (!hasWord) Debug.Log("Could not find word: " + word);
                Assert.IsTrue(hasWord);
            }
        }
    }

    [Test]
    public void TestSubTries()
    {
        Assert.IsTrue(_dawgGenerator.HasWordSubtries("armlösa"));

        foreach (var word in _allWords)
        {
            if (_dawgGenerator.ValidWord(word))
            {
                bool hasWord = _dawgGenerator.HasWordSubtries(word);
                if (!hasWord) Debug.Log("Could not find word: " + word);
                Assert.IsTrue(hasWord);
            }
        }
        
        Assert.IsFalse(_dawgGenerator.HasWord("superfalse"));
        Assert.IsFalse(_dawgGenerator.HasWord("aaaaaaa"));
        Assert.IsFalse(_dawgGenerator.HasWord("rtwerdvs"));
        Assert.IsFalse(_dawgGenerator.HasWord("a"));
        Assert.IsFalse(_dawgGenerator.HasWord("b"));
    }

    [Test]
    public void TestAllDawgWords()
    {
        SetupDawgSE();
        SetupDawg();
        
        foreach (var word in _allWords)
        {
            if (_dawgGenerator.ValidWord(word))
            {
                bool hasWord = _dawg.CheckWord(word);
                if (!hasWord) Debug.Log("Could not find word: " + word);
                Assert.IsTrue(hasWord);
            }
        }
        
        Assert.IsFalse(_dawg.CheckWord("superfalse"));
        Assert.IsFalse(_dawg.CheckWord("aaaaaaa"));
        Assert.IsFalse(_dawg.CheckWord("rtwerdvs"));
        Assert.IsFalse(_dawg.CheckWord("a"));
        Assert.IsFalse(_dawg.CheckWord("b"));
    }
    
    [Test]
    public void TestFastEnough()
    {
        SetupDawgSE();
        SetupDawg();

        int solutions = 0;
        var startTime = Time.realtimeSinceStartup;
        for (int i = 0; i < 100; i++)
        {
            solutions = _dawg.GetPossibleWordsCount("aeortms");
        }
        Debug.Log("Solutions: " + solutions);
        
        var newTime = Time.realtimeSinceStartup;
        Assert.Greater(newTime-startTime, 0);
        Assert.Less(newTime-startTime, 1 / 60f);
        
        startTime = Time.realtimeSinceStartup;
        for (int i = 0; i < 200; i++)
        {
            solutions = _dawg.GetPossibleWordsCountWithElimination("aeortms");
        }
        
        newTime = Time.realtimeSinceStartup;
        Assert.Greater(newTime-startTime, 0);
        Assert.Less(newTime-startTime, 1 / 60f);
    }
    
    [Test]
    public void TestSuperFastEnough()
    {
        SetupDawgSE();
        SetupDawg();
        int solutions = 0;
        var startTime = Time.realtimeSinceStartup;
        for (int i = 0; i < 180; i++)
        {
            solutions = _dawg.GetPossibleWordsCountFast("aeortms");
        }
        Debug.Log("Solutions: " + solutions);
        var newTime = Time.realtimeSinceStartup;
        Assert.Greater(newTime-startTime, 0);
        Assert.Less(newTime-startTime, 1 / 60f);
    }
    
    [Test]
    public void TestSolveWithEliminiation()
    {
        SetupDawgSE();
        SetupDawg();

        var solutions1 = _dawg.GetPossibleWordsCount("aeort");
        var solutions2 = _dawg.GetPossibleWordsCountWithElimination("aeort");

        Debug.Log("Solutions without elimination: " + solutions1);
        Debug.Log("Solutions with elimination: " + solutions2);
        
        Assert.AreNotEqual(solutions1, solutions2);
    }

    [Test]
    public void TestRandomWord()
    {
        SetupDawgSE();
        SetupDawg();

        for (int i = 0; i < 100; i++)
        {
            var word = _dawg.GetRandomWord();
            Debug.Log("Random word: " + word);
            Assert.IsTrue(_dawg.CheckWord(word));
        }
    }
    
    [Test]
    public void TestOnlyOppositeWayGeneration()
    {
        var textAsset = Resources.Load<TextAsset>("test_se_dict");
        var wordDictionary = new WordDictionary("se", textAsset.text);
        _allWords = wordDictionary.GetAllWords();
        var _twoWayDawgGenerator = new DAWGGenerator(LetterMap());
        _twoWayDawgGenerator.AddWordBatch(_allWords, true);
        _twoWayDawgGenerator.Generate();

        foreach (var word in _allWords)
        {
            if (_twoWayDawgGenerator.ValidWord(word))
            {
                bool hasWord = _twoWayDawgGenerator.HasWord(Reverse(word), 0);
                if (!hasWord) Debug.Log("Could not find word: " + word);
                Assert.IsTrue(hasWord);
            }
        }
        
        foreach (var word in _allWords)
        {
            if (_twoWayDawgGenerator.ValidWord(word))
            {
                bool hasWord = _twoWayDawgGenerator.HasWordSubtries(Reverse(word), 0);
                if (!hasWord) Debug.Log("Could not find word: " + word);
                Assert.IsTrue(hasWord);
            }
        }
    }
    
    [Test]
    public void TestBothWaysGeneration()
    {
        SetupTwoWayDawgSE();
        
        Assert.AreNotEqual(_twoWayDawgGenerator.StartIndex(0), _twoWayDawgGenerator.StartIndex(1));
        
        Assert.IsTrue(_twoWayDawgGenerator.HasWord("armlösa"));
        Assert.IsFalse(_twoWayDawgGenerator.HasWord("armlösa", 1));
        Assert.IsTrue(_twoWayDawgGenerator.HasWord("asölmra", 1));
        
        Assert.IsTrue(_twoWayDawgGenerator.HasWordSubtries("armlösa"));
        Assert.IsFalse(_twoWayDawgGenerator.HasWordSubtries("armlösa", 1));
        Assert.IsTrue(_twoWayDawgGenerator.HasWordSubtries("asölmra", 1));
        
        foreach (var word in _allWords)
        {
            if (_twoWayDawgGenerator.ValidWord(word))
            {
                bool hasWord = _twoWayDawgGenerator.HasWord(Reverse(word), 1);
                if (!hasWord) Debug.Log("Could not find word: " + word);
                Assert.IsTrue(hasWord);
            }
        }
        
        foreach (var word in _allWords)
        {
            if (_twoWayDawgGenerator.ValidWord(word))
            {
                bool hasWord = _twoWayDawgGenerator.HasWordSubtries(Reverse(word), 1);
                if (!hasWord) Debug.Log("Could not find word: " + word);
                Assert.IsTrue(hasWord);
            }
        }
    }
    
    [Test]
    public void TestBackwards()
    {
        SetupTwoWayDawgSE();
        var dawg = new Dawg(_twoWayDawgGenerator.Data, LetterMap(), 1);
        
        foreach (var word in _allWords)
        {
            if (_twoWayDawgGenerator.ValidWord(word))
            {
                bool hasWord = dawg.CheckWord(Reverse(word));
                if (!hasWord) Debug.Log("Could not find word: " + word);
                Assert.IsTrue(hasWord);
            }
        }
    }

    [Test]
    public void TestLongLetterMatching()
    {
        for (ulong testValue = 0; testValue < 32; testValue++)
        {
            ulong value = 0UL;
            
            for (int i = 0; i < 12; i++)
            {
                ulong randomValue = (ulong)Random.Range(0, 31);
                if (randomValue == testValue) randomValue = (randomValue + 1) % 32;
                value |= randomValue << i*5;
            }
            
            Assert.IsFalse(Dawg.HasLetter(value,
                (byte)testValue));
        }
        
        for (ulong testValue = 0; testValue < 32; testValue++)
        {
            ulong value = 0UL;

            int randomIndex = Random.Range(0, 11);
            for (int i = 0; i < 12; i++)
            {
                ulong randomValue = (ulong)Random.Range(0, 31);
                if (i == randomIndex) value |= testValue << i*5;
            }
            
            Assert.IsTrue(Dawg.HasLetter(value, (byte)testValue));
        }

        Assert.IsFalse(Dawg.HasZero(0b1111110011100111001110011010111001110011100111000010011100100001));
        Assert.IsTrue(Dawg.HasZero(0b1111110011100111001110011100111001110011100111001110011100100000));
        Assert.IsTrue(Dawg.HasZero(0b1111110010000111001110011100111001110010000011001110011100100010));
        
        Assert.IsFalse(Dawg.HasLetter(0b1111110010000111001110011100111001110010000011001110011100100100, 0b00010));
        Assert.IsFalse(Dawg.HasLetter(0b1111110010000111001110011100111001110010000011001110011100111111, 0b00010));
    }

    [Test]
    public void TestWildcardSolutions()
    {
        SetupDawgSE();
        SetupDawg();
        List<string> testWords = new List<string>() // Swedish dictionary
        {
            "he  an", 
            "telef  ",
            "      ",
            "bröd  ",
            "  tes ",
            "systern",
            "s st m ",
        };

        for (int i = 0; i < testWords.Count; i++)
        {
            var solutions = _dawg.GetPossibleWordsWildcardMap(testWords[i], 6);
            if (solutions.Count > 0)
            {
                Debug.Log("First solution: " + _dawg.SolutionToWord(solutions[0]));

            }
            Assert.Greater(solutions.Count, 0);
        }

        List<string> wrongWords = new List<string>() // Swedish dictionary
        {
            "    xyz",
            "    aaaa"
        };

        for (int i = 0; i < wrongWords.Count; i++)
        {
            var solutions = _dawg.GetPossibleWordsWildcardMap(wrongWords[i], 6);
            if (solutions.Count > 0)
            {
                Debug.Log("First solution: " + _dawg.SolutionToWord(solutions[0]));
            }
            Assert.AreEqual(0, solutions.Count);
        }
    }

    private static string Reverse(string Input) 
    { 
     
        // Converting string to character array
        char[] charArray = Input.ToCharArray(); 
     
        // Declaring an empty string
        string reversedString = String.Empty; 
     
        // Iterating the each character from right to left
        for(int i = charArray.Length - 1; i > -1; i--) 
        { 
         
            // Append each character to the reversedstring.
            reversedString += charArray[i]; 
        }
     
        // Return the reversed string.
        return reversedString;
    } 
}
