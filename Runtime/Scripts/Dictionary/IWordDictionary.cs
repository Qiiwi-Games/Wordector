namespace Wordector
{
    public interface IWordDictionary
    {
        bool CheckWord(string word);
        string GetRandomWord();
    }
}