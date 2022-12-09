using System;
using System.Collections.Generic;

public class TrieNode
{
    public byte Letter;
    public byte EndOfWord = 0;
    public byte HasChildren = 0;
    public byte HasMoreChildren = 0;
    public int Address;
    public int Visits = 0;
    public UInt64 SubTrieHash = 0;
    public Dictionary<byte,TrieNode> Children;

    public TrieNode(byte letter)
    {
        Letter = letter;
        Children = new Dictionary<byte,TrieNode>(32);
    }
    
    public void UnpackWord(int index, int wordLength, byte[] letters)
    {
        Visits++;
        
        if (index == wordLength - 1)
        {
            EndOfWord = 0b100000;
            return;
        }
        
        if (!Children.ContainsKey(letters[index + 1]))
        {
            Children[letters[index + 1]] = new TrieNode(letters[index + 1]);
            HasChildren = 0b1000000;
        }
        else
        {
            HasMoreChildren = 0b10000000;
        }
        
        Children[letters[index + 1]].UnpackWord(index+1, wordLength, letters);
    }

    public int CollectSubTries(Dictionary<UInt64, DAWGGenerator.SubTrie> subTrie, out UInt64 subTrieHash, TrieNode parent)
    {
        int nodeCount = 0;
        
        subTrieHash = 0;

        int childIndex = 0;
        
        if (HasChildren != 0)
        {
            foreach (var child in Children.Values)
            {
                nodeCount += child.CollectSubTries(subTrie, out var childHash, this);
                subTrieHash = subTrieHash*(256+129) ^ (childHash * (ulong)(nodeCount+childIndex));
                childIndex++;
            }
        }

        subTrieHash = subTrieHash*(256+129) ^ (UInt64)(Letter | EndOfWord);
        
        if (HasChildren != 0) {
            if (!subTrie.ContainsKey(subTrieHash))
                subTrie[subTrieHash] = new DAWGGenerator.SubTrie(this, this, nodeCount);
            else
                subTrie[subTrieHash].ParentNodes.Add(this);

            SubTrieHash = subTrieHash;
        }
        return nodeCount + 1;
    }
}
