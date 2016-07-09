using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowStream
{
    public class SoulStream
    {
        private const string RootNodeName = "&_RootNode";

        public class Node
        {
            public string Name { get; private set; }
            public List<Node> from;
            public List<Node> to;
            public List<Node> fromLoop;
            public List<Node> toLoop;

            public int depth;

            public Node(string name)
            {
                Name = name;
                from = new List<Node>();
                to = new List<Node>();
                fromLoop = new List<Node>();
                toLoop = new List<Node>();

                depth = int.MinValue;
            }
        }

        private Node RootNode; // 시작점이 없는 노드들을의 시작점으로 취급하는 가상의 노드
        private Dictionary<string, Node> NodeDictionary;

        public SoulStream()
        {
            RootNode = new Node(RootNodeName);
            NodeDictionary = new Dictionary<string, Node>();
            NodeDictionary.Add(RootNode.Name, RootNode);
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

        /// <summary>
        /// 노드의 입력이 끝났다면, 노드의 배치를 결정하는 함수
        /// </summary>
        public void MakeFlow()
        {
            // 먼저 부모 없는 노드의 부모를 루트로 설정합니다.
            // <TODO> 부모 없이 닫혀있는 노드집합은 현재 생각하지 않습니다.

            foreach (var node in NodeDictionary.Values)
            {
                if (node.from.Count == 0 && node != RootNode)
                {
                    node.from.Add(RootNode);
                    RootNode.to.Add(node);
                }
            }

            // 루프를 판단해서 별개의 연결로 취급합니다.
            SeperateLoopLink(RootNode, null);

            // 각 노드의 깊이를 계산합니다.
            CalculateDepth(RootNode);

            // Test
            foreach (Node n in NodeDictionary.Values)
            {
                Console.WriteLine(n.Name + ", depth = " + n.depth);
                Console.Write("    childs: ");

                foreach (Node c in n.to)
                {
                    Console.Write(c.Name + ", ");
                }

                Console.Write("[");

                foreach (Node c in n.toLoop)
                {
                    Console.Write(c.Name + ", ");
                }

                Console.Write("]");

                Console.WriteLine();
            }
        }

        private void SeperateLoopLink(Node node, Stack<Node> stack)
        {
            if (stack == null)
                stack = new Stack<Node>();

            stack.Push(node);

            for (int i = node.to.Count - 1; i >= 0; i--)
            {
                Node child = node.to[i];

                if (stack.Contains(child))
                {
                    var loopedNode = stack.FirstOrDefault(v => v == child);
                    loopedNode.fromLoop.Add(child);
                    loopedNode.from.Remove(child);

                    node.toLoop.Add(child);
                    node.to.Remove(child);
                }
                else
                {
                    SeperateLoopLink(child, stack);
                }
            }

            stack.Pop();
        }

        private void CalculateDepth(Node node, int depth = 0)
        {
            if (node.depth < depth)
            {
                node.depth = depth;

                foreach (Node child in node.to)
                {
                    CalculateDepth(child, depth + 1);
                }
            }
        }
    }
}
