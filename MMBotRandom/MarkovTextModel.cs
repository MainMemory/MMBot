using System;
using System.Collections.Generic;

namespace MMBotRandom
{
    public class MarkovWordTextModel
    {
        internal class MarkovNode
        {
            public string Ch;
            public int Count;
            public int FollowCount;

            public Dictionary<string, MarkovNode> Children;
            public MarkovNode(string c)
            {
                Ch = c;
                Count = 1;
                FollowCount = 0;
                Children = new Dictionary<string, MarkovNode>();
            }

            public MarkovNode AddChild(string c)
            {
                if (Children == null)
                    Children = new Dictionary<string, MarkovNode>();
                FollowCount += 1;
                MarkovNode child = null;
                if (Children.TryGetValue(c, out child))
                {
                    child.Count += 1;
                }
                else
                {
                    child = new MarkovNode(c);
                    Children.Add(c, child);
                }
                return child;
            }
        }
        public const char StartChar = '\ufffe';

        public const char StopChar = '\uffff';
        private MarkovNode Root;

        private int ModelOrder;
        public MarkovWordTextModel(int order)
        {
            ModelOrder = order;
            Root = new MarkovNode(StartChar.ToString());
        }

        public void AddString(string s)
        {
            // Construct the string that will be added.
            List<string> arr = new List<string>();
            //Dim sb As New StringBuilder(s.Length + 2 * (ModelOrder))
            // Order+1 Start characters.
            // The string to add.
            // Order+1 Stop characters.
            for (int i = 1; i <= ModelOrder; i++)
                arr.Add(StartChar.ToString());
            foreach (string item in s.Split(' '))
                arr.Add(item);
            for (int i = 1; i <= ModelOrder; i++)
                arr.Add(StopChar.ToString());
            // Naive method
            for (int iStart = 0; iStart < arr.Count; iStart++)
            {
                // Get the order 0 node
                MarkovNode parent = Root.AddChild(arr[iStart]);

                // Now add N-grams starting with this node
                int i = 1;
                while (i <= ModelOrder && i + iStart < arr.Count)
                {
                    MarkovNode child = parent.AddChild(arr[iStart + i]);
                    parent = child;
                    i += 1;
                }
            }
        }

        public void AddStrings(params string[] s)
        {
            foreach (string item in s)
                AddString(item);
        }

        public void Clear()
        {
            Root = new MarkovNode(StartChar.ToString());
        }


        private Random RandomSelector = new Random();
        public string Generate(int order)
        {
            string functionReturnValue = null;
            if (order > ModelOrder)
                throw new ApplicationException("Cannot generate higher order than was built.");
            List<string> rslt = new List<string>();
            for (int i = 1; i <= order; i++)
                rslt.Add(StartChar.ToString());
            int iStart = 0;
            string ch = StartChar.ToString();
            do
            {
                MarkovNode node = Root.Children[rslt[iStart]];
                for (int i = 1; i < order; i++)
                    node = node.Children[rslt[i + iStart]];
                ch = SelectChildChar(node);
                if (ch != StopChar.ToString())
                    rslt.Add(ch);
                iStart += 1;
            } while (ch != StopChar.ToString());

            // remove start characters from the string
            functionReturnValue = "";
            for (int i = 0; i < rslt.Count; i++)
            {
                if (rslt[i] != StartChar.ToString() & rslt[i] != StopChar.ToString())
                {
                    functionReturnValue += rslt[i];
                    if (i < rslt.Count - 1)
                        functionReturnValue += " ";
                }
            }
            return functionReturnValue;
            //Generate.TrimEnd(" ")
            //Return rslt.ToString().TrimStart(StartChar)
        }

        public string Generate(int order, string startword)
        {
            string functionReturnValue = null;
            if (order > ModelOrder)
                throw new ApplicationException("Cannot generate higher order than was built.");
            List<string> rslt = new List<string>();
            rslt.Add(startword);
            MarkovNode nd = Root.Children[startword];
            for (int i = 1; i < order; i++)
            {
                rslt.Add(SelectChildChar(nd));
                nd = nd.Children[rslt[i]];
            }
            int iStart = 0;
            string ch = startword;
            do
            {
                MarkovNode node = Root.Children[rslt[iStart]];
                for (int i = 1; i < order; i++)
                    node = node.Children[rslt[i + iStart]];
                ch = SelectChildChar(node);
                if (ch != StopChar.ToString())
                    rslt.Add(ch);
                iStart += 1;
            } while (ch != StopChar.ToString());
            functionReturnValue = "";
            for (int i = 0; i < rslt.Count; i++)
            {
                if (rslt[i] != StartChar.ToString() & rslt[i] != StopChar.ToString())
                {
                    functionReturnValue += rslt[i];
                    if (i < rslt.Count - 1 && rslt[i] != StopChar.ToString())
                        functionReturnValue += " ";
                }
            }
            return functionReturnValue;
            //Generate.TrimEnd(" ")
            //Return rslt.ToString().TrimStart(StartChar)
        }

        private string SelectChildChar(MarkovNode node)
        {
            // Generate a random number in the range 0..(node.Count-1)
            int rnd = RandomSelector.Next(node.FollowCount);

            // Go through the children to select the node
            int cnt = 0;
            foreach (KeyValuePair<string, MarkovNode> kvp in node.Children)
            {
                cnt += kvp.Value.Count;
                if (cnt > rnd)
                {
                    return kvp.Key;
                }
            }
            throw new System.ApplicationException("This can't happen!");
        }
    }
}