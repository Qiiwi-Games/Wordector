using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Create DAWGConfig", fileName = "DAWGConfig", order = 0)]
public class DAWGConfig : ScriptableObject
{
    [SerializeField, HideInInspector] private string _baseDictionaryPath;
    [SerializeField] private List<string> _lettersToId;
    [SerializeField] private TextAsset _dawgAsset;
    [SerializeField] private bool _generateBackwardWords;
    public string BaseDictionaryPath => _baseDictionaryPath;
    public TextAsset DawgAsset => _dawgAsset;
    public bool GenerateBackwardsWords => _generateBackwardWords;
    public Dawg CreateDawg()
    {
        return new Dawg(_dawgAsset.bytes,GetLetterMap());
    }

    public Dictionary<char, byte> GetLetterMap()
    {
        var result = new Dictionary<char, byte>();
        for (var i = 0; i < _lettersToId.Count; i++)
        {
            var letters = _lettersToId[i];
            foreach (var c in letters)
            {
                result[c] = (byte)i;
            }
        }

        return result;
    }
}