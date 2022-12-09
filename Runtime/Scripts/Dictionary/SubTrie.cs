using System.Collections.Generic;

namespace Dawg
{
    public partial class DAWGGenerator
    {
        public class SubTrie
        {
            public TrieNode Node;
            public int Length;
            public List<TrieNode> ParentNodes;


            public SubTrie(TrieNode node, TrieNode parentNode, int length)
            {
                Node = node;
                Length = length;
                ParentNodes = new List<TrieNode>();
                ParentNodes.Add(parentNode);
            }
        }
    }
}