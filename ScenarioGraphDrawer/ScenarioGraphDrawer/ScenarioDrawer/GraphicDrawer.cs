using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScenarioGraphDrawer.ScenarioDrawer
{
    public class GraphicDrawer
    {
        private const string RootNodeName = "&_RootNode";

        public class Node
        {
            public string Name { get; private set; }
            public List<Node> from;
            public List<Node> to;

            public Node(string name)
            {
                Name = name;
                from = new List<Node>();
                to = new List<Node>();
            }
        }

        private Node RootNode; // 시작점이 없는 노드들을의 시작점으로 취급하는 가상의 노드
        private Dictionary<string, Node> NodeDictionary;

        public GraphicDrawer()
        {
            RootNode = new Node(RootNodeName);
            NodeDictionary = new Dictionary<string, Node>();
        }

        public void AddNode(string nodeName)
        {
            if (NodeDictionary.ContainsKey(nodeName))
            {
                throw new InvalidOperationException("Nodename " + nodeName + " is already Added!");
            }

            NodeDictionary.Add(nodeName, new Node(nodeName));
        }

        public void AddLink(string from, string to)
        {
            if (!NodeDictionary.ContainsKey(from))
            {
                throw new InvalidOperationException("Nodename " + from + " is Invalid!");
            }
            else if (!NodeDictionary.ContainsKey(to))
            {
                throw new InvalidOperationException("Nodename " + to + " is Invalid!");
            }

            Node fromNode = NodeDictionary[from];
            Node toNode = NodeDictionary[to];

            fromNode.to.Add(toNode);
            toNode.from.Add(fromNode);
        }

        public void Build()
        {

        }
    }
}
