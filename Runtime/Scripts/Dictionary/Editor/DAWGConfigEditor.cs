using System.IO;
using UnityEditor;
using UnityEngine;

namespace Dawg
{
    [CustomEditor(typeof(DAWGConfig))]
    public class DAWGConfigEditor : Editor
    {
        private TextAsset cachedBaseDictionary = null;
        private Dawg cachedDawg = null;
        private int cachedWordCount = -1;
        private TextAsset cachedDawgTextAsset = null;

        public override void OnInspectorGUI()
        {
            var config = target as DAWGConfig;

            var dictProperty = serializedObject.FindProperty("_baseDictionaryPath");

            if (cachedBaseDictionary == null && dictProperty.stringValue != null)
                cachedBaseDictionary = AssetDatabase.LoadAssetAtPath<TextAsset>(dictProperty.stringValue);

            var prevDictionaryAsset = cachedBaseDictionary;
            EditorGUILayout.LabelField("Field below is not a hard reference, will not be included in build");
            cachedBaseDictionary = (TextAsset)EditorGUILayout.ObjectField(new GUIContent("Base dictionary"), cachedBaseDictionary, typeof(TextAsset), false);
            EditorGUILayout.Space();

            if (prevDictionaryAsset != cachedBaseDictionary)
            {
                dictProperty.stringValue = AssetDatabase.GetAssetPath(cachedBaseDictionary);
                serializedObject.ApplyModifiedProperties();
            }

            DrawDefaultInspector();

            if ((cachedDawgTextAsset != config.DawgAsset || cachedDawg == null) && config.DawgAsset != null)
            {
                cachedDawgTextAsset = config.DawgAsset;
                cachedWordCount = -1;
                cachedDawg = new Dawg(config.DawgAsset.bytes, config.GetLetterMap());
            }

            if (cachedBaseDictionary != null && GUILayout.Button("Generate DAWG"))
            {
                WordDictionary wordDictionary = new WordDictionary(cachedBaseDictionary.name, cachedBaseDictionary.text);

                DAWGGenerator generator = null;
                if (!config.GenerateBackwardsWords) generator = new DAWGGenerator(wordDictionary.GetAllWords(), config.GetLetterMap());
                else
                {
                    generator = new DAWGGenerator(config.GetLetterMap());
                    generator.AddWordBatch(wordDictionary.GetAllWords());
                    generator.AddWordBatch(wordDictionary.GetAllWords(), true);
                    generator.Generate();
                }

                var newAsset = SaveToTextAsset(generator.Data, config.name, config.GenerateBackwardsWords);

                serializedObject.FindProperty("_dawgAsset").objectReferenceValue = newAsset;
                serializedObject.ApplyModifiedProperties();
            }

            if (cachedDawg != null)
            {
                if (cachedWordCount <= 0) cachedWordCount = cachedDawg.GetPossibleWordsCount();
                EditorGUILayout.LabelField("Loaded DAWG word count: " + cachedWordCount);
            }
        }

        private TextAsset SaveToTextAsset(byte[] data, string languageId, bool twoWay = false)
        {
            var thisAssetPath = AssetDatabase.GetAssetPath(target);

            var splitPath = thisAssetPath.Substring(0, thisAssetPath.LastIndexOf("/"));

            var newPath = splitPath + "/" + languageId + "_dawg" + (twoWay ? "_twoWay" : "") + ".txt";

            File.WriteAllBytes(newPath, data);
            AssetDatabase.Refresh();

            return AssetDatabase.LoadAssetAtPath<TextAsset>(newPath);
        }
    }
}