using System;
using System.Collections.Generic;
using UnityEngine;

public partial class DAWGGenerator
{
    private byte[] byteArray;
    private List<TrieNode> _trunks;
    private byte[] _data;

    private Dictionary<UInt64, SubTrie> _subTries;
    private int _address;
    private Dictionary<char, byte> _letterMap;

    public byte[] Data => _data;
    
    public UInt64 IterateHash(UInt64 letterHash, UInt64 childHash, int nodeCount)
    {
        return letterHash*3 + childHash*(ulong)nodeCount;
    }

    public int StartIndex(int trunkIndex)
    {
        return _trunks[trunkIndex].Address;
    }
    
    public bool ValidWord(string word)
    {
        for (int i = 0; i < word.Length; i++)
        {
            if (!_letterMap.ContainsKey(word[i]))
                return false;
        }

        return true;
    }
    
    public bool HasWord(string word, int trunkIndex = 0)
    {
        TrieNode node = _trunks[trunkIndex];

        for (int i = 0; i < word.Length; i++)
        {
            
            if (node.Children.ContainsKey(_letterMap[word[i]]))
            {
                var letter = _letterMap[word[i]];
                node = node.Children[letter];
                
                if (i == word.Length - 1 && node.EndOfWord == 0) return false;
            }
            else return false;
        }

        return true;
    }
    
    public bool HasWordSubtries(string word, int trunkIndex = 0)
    {
        TrieNode node = _trunks[trunkIndex];

        for (int i = 0; i < word.Length; i++)
        {
            var letter = word[i];
            var letterByte = _letterMap[letter];

            if (node.Children.ContainsKey(letterByte))
            {
                node = node.Children[letterByte];
                if (node.SubTrieHash != 0 && _subTries[node.SubTrieHash].Node.SubTrieHash == node.SubTrieHash) node = _subTries[node.SubTrieHash].Node;
                
                if (i == word.Length - 1 && node.EndOfWord == 0) return false;
            }
            else return false;
        }

        return true;
    }

    public DAWGGenerator(Dictionary<char, byte> letterMap)
    {
        _trunks = new List<TrieNode>();
        _letterMap = letterMap;
        _subTries = new Dictionary<UInt64, SubTrie>();
    }
    
    public void AddWordBatch(List<string> words, bool reverse = false)
    {
        TrieNode trunk = new TrieNode(0);
        
        byte[] letters = new byte[16];
        foreach (var word in words)
        {
            for (int i = 0; i < word.Length && i < 16; i++)
            {
                int index = reverse ? word.Length - i - 1 : i;
                if (!_letterMap.ContainsKey(word[index])) continue;
                letters[i] = _letterMap[word[index]];
            }
            
            trunk.UnpackWord(-1, word.Length, letters);
        }
        
        trunk.CollectSubTries(_subTries, out _, null);
        _trunks.Add(trunk);
    }

    public DAWGGenerator(List<string> words, Dictionary<char, byte> letterMap) : this(letterMap)
    {
        AddWordBatch(words);
        Generate();
    }

    public void Generate()
    {
        _address = _trunks.Count*4;
        _data = new byte[2000000];
        
        List<(UInt64, int)> sortedRarity = new List<(ulong, int)>();

        foreach (var subTrie in _subTries)
        {
            if (subTrie.Value.ParentNodes.Count > 1) sortedRarity.Add((subTrie.Key, subTrie.Value.Length * (subTrie.Value.ParentNodes.Count - 1)));
            else
            {
                foreach (var parentNode in subTrie.Value.ParentNodes)
                {
                    parentNode.SubTrieHash = 0;
                }
                subTrie.Value.Node.SubTrieHash = 0;
            }
        }
        
        sortedRarity.Sort((a, b) => a.Item2.CompareTo(b.Item2));

        foreach (var rarity in sortedRarity)
        {
            var subTrie = _subTries[rarity.Item1];

            AddSubtrie(subTrie.Node, _subTries);
        }

        for (int i = 0; i < _trunks.Count; i++)
        {
            int startingAdress = AddSubtrie(_trunks[i], _subTries);

            int trunkAdress = i * 4;
            
            _data[trunkAdress] = (byte) startingAdress;
            _data[trunkAdress + 1] = (byte) (startingAdress >> 8);
            _data[trunkAdress + 2] = (byte) (startingAdress >> 16);
            _data[trunkAdress + 3] = (byte) (startingAdress >> 24);
        }

        Array.Resize(ref _data, _address + 1);
    }

    private int AddSubtrie(TrieNode node, Dictionary<UInt64, SubTrie> subTries)
    {
        if (node.Address != 0) return node.Address;
        
        int thisNodeAdress = _address;
        node.Address = thisNodeAdress;
        
        _data[thisNodeAdress] = (byte)(node.Letter + node.EndOfWord);
        
        _address++;
        
        if (node.HasChildren != 0)
        {
            int childrenAdded = node.Children.Count;

            _address += 3 * (childrenAdded - 1);
            
            if (childrenAdded > 0)
            {
                int jumpAdress = thisNodeAdress + 1;
                bool firstChild = true;
                bool addedAddresses = false;
                foreach (var child in node.Children.Values)
                {
                    var generationNode = child;
                    if (child.SubTrieHash != 0)
                    {
                        generationNode = subTries[child.SubTrieHash].Node;
                    }
                    
                    if (firstChild)
                    {
                        if (child.SubTrieHash != 0 && generationNode.Address != 0)
                        {
                            firstChild = false;
                            _address += 3;
                            _data[thisNodeAdress] |= 0b10000000;
                        }
                        else
                        {
                            AddSubtrie(generationNode, subTries);
                            _data[thisNodeAdress] |= 0b01000000;
                        }
                    }
                    
                    if (!firstChild)
                    {
                        _data[thisNodeAdress] |= 0b10000000;
                        
                        if (generationNode.Address != 0)
                        {
                            _data[jumpAdress] = (byte)(generationNode.Address & 0xFF);
                            _data[jumpAdress + 1] = (byte)((generationNode.Address >> 8) & 0xFF);
                            _data[jumpAdress + 2] = (byte)((generationNode.Address >> 16) & 0xFF);
                        }
                        else
                        {
                            int address = AddSubtrie(generationNode, subTries);
                            _data[jumpAdress] = (byte)(address & 0xFF);
                            _data[jumpAdress + 1] = (byte)(address >> 8 & 0xFF);
                            _data[jumpAdress + 2] = (byte)(address >> 16 & 0xFF);
                        }

                        addedAddresses = true;
                        jumpAdress += 3;
                    }
                    
                    firstChild = false;
                }
                if (addedAddresses) _data[jumpAdress - 1] |= 0b10000000;
            }
        }

        return thisNodeAdress;
    }
}

public static class DictionaryExtensions {
    public static Dictionary<TValue, TKey> Reverse<TKey, TValue>(this IDictionary<TKey, TValue> source)
    {
        var dictionary = new Dictionary<TValue, TKey>();
        foreach (var entry in source)
        {
            if(!dictionary.ContainsKey(entry.Value))
                dictionary.Add(entry.Value, entry.Key);
        }
        return dictionary;
    }
}
