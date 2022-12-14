using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Wordector
{
    public class Dawg : IWordDictionary
    {
        public const byte EndOfWord = 0b100000;
        public const byte LetterBits = 0b11111;
        public const byte NaturalContinuationBit = 0b1000000;
        public const byte AddressedChildrenBit = 0b10000000;
    
        private byte[] _data;
        private int _startingAddress;
        private Dictionary<char, byte> _letterMap;
        private Dictionary<byte, char> _inverseLetterMap;

        public Dawg(byte[] data, Dictionary<char, byte> letterMap, int trunkIndex = 0)
        {
            _data = data;
            _letterMap = letterMap;
            _startingAddress = data[trunkIndex*4] + (data[trunkIndex*4+1] << 8) + (data[trunkIndex*4+2] << 16) + (data[trunkIndex*4+3] << 24);
            _inverseLetterMap = letterMap.Reverse();
        }

        public string SolutionToWord(byte[] solution, int length = 0)
        {
            char[] chars = new char[length > 0 ? length : solution.Length];
            for (int i = 0; i < chars.Length; i++)
                chars[i] = _inverseLetterMap[solution[i]];
            return new string(chars);
        }

        public List<byte[]> GetPossibleWords(string letters)
        {
            byte[] selection = new byte[letters.Length];
            byte[] tempLetters = new byte[16];
            List<byte[]> result = new List<byte[]>();

            for (int i = 0; i < letters.Length; i++)
            {
                selection[i] = _letterMap[letters[i]];
            }

            GetPossibleWordsRecursive(selection, tempLetters, result, -1, _startingAddress);

            return result;
        }
    
        public int GetPossibleWordsCount()
        {
            byte[] selection = _letterMap.Values.ToArray();

            return GetPossibleWordsRecursive(selection, -1, _startingAddress);
        }
    
        public int GetPossibleWordsCount(string letters)
        {
            byte[] selection = new byte[letters.Length];

            for (int i = 0; i < letters.Length; i++)
            {
                selection[i] = _letterMap[letters[i]];
            }

            return GetPossibleWordsRecursive(selection, -1, _startingAddress);
        }
    


        public int GetPossibleWordsCountFast(string letters)
        {
            ulong selection = 0UL;
            
            for (int i = 0; i < 12; i++)
            {
                if (i < letters.Length) selection |= (ulong)_letterMap[letters[i]] << i * 5;
                else selection |= 31UL << i * 5;
            }
        
            if (IntPtr.Size == 8)
            {
                return GetPossibleWordsFastRecursive(selection, -1, _startingAddress);
            }
            else
            {
                return GetPossibleWordsFastRecursive((uint)(selection & 0xFFFFFFFF00000000UL >> 32), (uint)(selection & 0xFFFFFFFFU), -1, _startingAddress);
            }
        }

        public List<byte[]> GetPossibleWordsWildcardMap(string wildcardMap, int minimumLength)
        {
            bool[] wildcard = new bool[wildcardMap.Length];
            byte[] selection = new byte[wildcardMap.Length];
            byte[] tempLetters = new byte[20];
            List<byte[]> result = new List<byte[]>();

            for (int i = 0; i < wildcardMap.Length; i++)
            {
                wildcard[i] = wildcardMap[i] == ' ';
                selection[i] = (byte)(_letterMap.ContainsKey(wildcardMap[i]) ? _letterMap[wildcardMap[i]] : 0);
            }

            GetPossibleWildCardWordsRecursive(selection, tempLetters, wildcard, result, -1, _startingAddress, minimumLength);
            return result;
        }

        private void GetPossibleWildCardWordsRecursive(byte[] selection, byte[] tempLetters, bool[] wildcardMap, List<byte[]> solutions, int depth, int address, int minimumLength)
        {
            if (depth >= 0)
            {
                if (depth >= selection.Length) return;

                var letter = (byte)(_data[address] & LetterBits);
                tempLetters[depth] = letter;

                if (!wildcardMap[depth] && selection[depth] != letter) return;

                if (depth + 1 >= minimumLength && (_data[address] & EndOfWord) != 0)
                {
                    var newWord = new byte[depth + 1];
                    Array.Copy(tempLetters, newWord, depth + 1);
                    solutions.Add(newWord);
                }
            }

            bool hasNaturalContinuation = (_data[address] & NaturalContinuationBit) != 0;

            if ((_data[address++] & AddressedChildrenBit) != 0)
            {
                do
                {
                    int newAddress = _data[address++] + (_data[address++] << 8) + ((_data[address] & 0b01111111) << 16);
                    GetPossibleWildCardWordsRecursive(selection, tempLetters, wildcardMap, solutions, depth + 1, newAddress, minimumLength);
                } while ((_data[address++] & AddressedChildrenBit) == 0);
            }

            if (hasNaturalContinuation) GetPossibleWildCardWordsRecursive(selection, tempLetters, wildcardMap, solutions, depth + 1, address, minimumLength);
        }

        public int GetPossibleWordsCountWithElimination(string letters, int minimumLenth = 0)
        {
            byte[] selection = new byte[letters.Length];

            for (int i = 0; i < letters.Length; i++)
            {
                selection[i] = _letterMap[letters[i]];
            }

            return GetPossibleWordsWithElimination(selection, 0, -1, _startingAddress, minimumLenth);
        }

        private void GetPossibleWordsRecursive(byte[] selection, byte[] tempLetters, List<byte[]> solutions, int depth, int address)
        {
            if (depth >= 0)
            {
                tempLetters[depth] = (byte)(_data[address] & LetterBits);
                var letter = (byte)(_data[address] & LetterBits);
            
                bool contains = false;
                for (int i = 0; i < selection.Length; i++)
                    if (selection[i] == letter)
                    {
                        contains = true;
                        break;
                    }
                if (!contains) return;
                if ((_data[address] & EndOfWord) != 0)
                {
                    var newWord = new byte[depth + 1];
                    Array.Copy(tempLetters, newWord, depth + 1);
                    solutions.Add(newWord);
                }
            }

            bool hasNaturalContinuation = (_data[address] & NaturalContinuationBit) != 0;
        
            if ((_data[address++] & AddressedChildrenBit) != 0)
            {
                do
                {
                    int newAddress = _data[address++] + (_data[address++] << 8) + ((_data[address] & 0b01111111) << 16);
                    GetPossibleWordsRecursive(selection, tempLetters, solutions, depth + 1, newAddress);
                } while ((_data[address++] & AddressedChildrenBit) == 0);
            }
        
            if (hasNaturalContinuation) GetPossibleWordsRecursive(selection, tempLetters, solutions, depth + 1, address);
        }
    
        private int GetPossibleWordsRecursive(byte[] selection, int depth, int address)
        {
            int solutions = 0;
            if (depth >= 0)
            {
                var letter = (byte)(_data[address] & LetterBits);
                bool contains = false;
                for (int i = 0; i < selection.Length; i++)
                    if (selection[i] == letter)
                    {
                        contains = true;
                        break;
                    }
                if (!contains) return 0;
                if ((_data[address] & EndOfWord) != 0) solutions++;
            }

            bool hasNaturalContinuation = (_data[address] & NaturalContinuationBit) != 0;
        
            if ((_data[address++] & AddressedChildrenBit) != 0)
            {
                do
                {
                    int newAddress = _data[address++] | (_data[address++] << 8) | ((_data[address] & 0b01111111) << 16);
                    solutions += GetPossibleWordsRecursive(selection, depth + 1, newAddress);
                } while ((_data[address++] & AddressedChildrenBit) == 0);
            }
        
            if (hasNaturalContinuation) solutions += GetPossibleWordsRecursive(selection, depth + 1, address);
        
            return solutions;
        }
    
        private int GetPossibleWordsFastRecursive(ulong selection, int depth, int address)
        {
            int solutions = 0;
            if (depth >= 0)
            {
                if (!HasLetter(selection, (byte)(_data[address] & LetterBits))) return 0;
                if ((_data[address] & EndOfWord) != 0) solutions++;
            }

            bool hasNaturalContinuation = (_data[address] & NaturalContinuationBit) != 0;
        
            if ((_data[address++] & AddressedChildrenBit) != 0)
            {
                do
                {
                    int newAddress = _data[address++] | (_data[address++] << 8) | ((_data[address] & 0b01111111) << 16);
                    solutions += GetPossibleWordsFastRecursive(selection, depth + 1, newAddress);
                } while ((_data[address++] & AddressedChildrenBit) == 0);
            }
        
            if (hasNaturalContinuation) solutions += GetPossibleWordsFastRecursive(selection, depth + 1, address);
        
            return solutions;
        }
    
        private int GetPossibleWordsFastRecursive(UInt32 selectionHigh, UInt32 selectionLow, int depth, int address)
        {
            int solutions = 0;
            if (depth >= 0)
            {
                byte letter = (byte)(_data[address] & LetterBits);
                if (!HasLetter(selectionHigh, letter) && !HasLetter(selectionLow, letter)) return 0;
                if ((_data[address] & EndOfWord) != 0) solutions++;
            }

            bool hasNaturalContinuation = (_data[address] & NaturalContinuationBit) != 0;
        
            if ((_data[address++] & AddressedChildrenBit) != 0)
            {
                do
                {
                    int newAddress = _data[address++] | (_data[address++] << 8) | ((_data[address] & 0b01111111) << 16);
                    solutions += GetPossibleWordsFastRecursive(selectionHigh, selectionLow, depth + 1, newAddress);
                } while ((_data[address++] & AddressedChildrenBit) == 0);
            }
        
            if (hasNaturalContinuation) solutions += GetPossibleWordsFastRecursive(selectionHigh, selectionLow, depth + 1, address);
        
            return solutions;
        }
    
        private int GetPossibleWordsWithElimination(byte[] selection, uint elimination, int depth, int address, int minimumLength = 0)
        {
            int solutions = 0;
            uint elimMask = 1;
            if (depth >= 0)
            {
                var letter = (byte)(_data[address] & LetterBits);
                bool contains = false;
                for (int i = 0; i < selection.Length; i++, elimMask <<= 1)
                    if ((elimination & elimMask) == 0 && selection[i] == letter)
                    {
                        elimination |= elimMask;
                        contains = true;
                        break;
                    }
                if (!contains) return 0;
                if (depth >= minimumLength - 1 && (_data[address] & EndOfWord) != 0) solutions++;
            }

            bool hasNaturalContinuation = (_data[address] & NaturalContinuationBit) != 0;
        
            if ((_data[address++] & AddressedChildrenBit) != 0)
            {
                do
                {
                    int newAddress = _data[address++] + (_data[address++] << 8) + ((_data[address] & 0b01111111) << 16);
                    solutions += GetPossibleWordsWithElimination(selection, elimination, depth + 1, newAddress, minimumLength);
                } while ((_data[address++] & AddressedChildrenBit) == 0);
            }
            if (hasNaturalContinuation) solutions += GetPossibleWordsWithElimination(selection, elimination, depth + 1, address, minimumLength);

            return solutions;
        }
    
        public bool CheckWord(string word)
        {
            int currentAddress = _startingAddress;

            for (int i = 0; i < word.Length; i++)
            {
                currentAddress = GetLetterAddress(_letterMap[word[i]], currentAddress);

                if (currentAddress == 0) return false;

                if (i == word.Length - 1 && (_data[currentAddress] & EndOfWord) != 0) return true;
            }

            return false;
        }
    
        private int GetLetterAddress(byte letter, int address)
        {
            bool hasNaturalContinuation = (_data[address] & NaturalContinuationBit) != 0;
        
            if ((_data[address++] & AddressedChildrenBit) != 0)
            {
                do
                {
                    int newAddress = _data[address++] + (_data[address++] << 8) + ((_data[address] & 0b01111111) << 16);
                    if ((_data[newAddress] & LetterBits) == letter) return newAddress;
                } while ((_data[address++] & AddressedChildrenBit) == 0);
            }
            if (hasNaturalContinuation && (_data[address] & LetterBits) == letter) return address;
        
            return 0;
        }

        private int[] cachedAddresses = new int[32];
    
        public string GetRandomWord()
        {
            int currentAddress = _startingAddress;
            byte[] randomizedWordBytes = new byte[16];

            int i = 0;
            for (; i < 16; i++)
            {
                currentAddress = GetRandomBranch(currentAddress);

                if (currentAddress == 0) break;
            
                randomizedWordBytes[i] = (byte)(_data[currentAddress] & LetterBits);

                if ((_data[currentAddress] & EndOfWord) != 0 && Random.Range(0, 1f) > 0.5f)
                {
                    i++;
                    break;
                }
            }

            return SolutionToWord(randomizedWordBytes, i);
        }
        
        //Biased towards obscure words
        private int GetRandomBranch(int address)
        {
            int i = 0;
            bool hasNaturalContinuation = (_data[address] & NaturalContinuationBit) != 0;
        
            if ((_data[address++] & AddressedChildrenBit) != 0)
            {
                do
                {
                    cachedAddresses[i++] = _data[address++] + (_data[address++] << 8) + ((_data[address] & 0b01111111) << 16);
                } while ((_data[address++] & AddressedChildrenBit) == 0);
            }
            if (hasNaturalContinuation) cachedAddresses[i++] = address;
        
            return i > 0 ? cachedAddresses[Random.Range(0,i)] : 0;
        }

        //Solution from https://graphics.stanford.edu/~seander/bithacks.html#ValueInWord
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasLetter(ulong input, byte value)
        {
            return HasZero(input ^ (0b111111111111111111111111111111111111111111111111111111111111UL / 31 * value));
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasZero(ulong input)
        {
            return ((input - 0b0001000010000100001000010000100001000010000100001000010000100001UL) & ~input &
                             0b0000100001000010000100001000010000100001000010000100001000010000UL) != 0;
        }
    
        //Solution from https://graphics.stanford.edu/~seander/bithacks.html#ValueInWord
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasLetter(UInt32 input, byte value)
        {
            return HasZero(input ^ (0b111111111111111111111111111111U / 31 * value));
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasZero(UInt32 input)
        {
            return ((input - 0b000010000100001000010000100001U) & ~input &
                             0b100001000010000100001000010000U) != 0;
        }
    }
}