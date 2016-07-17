using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowStream
{
    public class SoulStream
    {
        public const string RootNodeName = "?RootNode";
        public const string HiddenNodePrefix = "?Hidden";

        public enum Method
        {
            Mountain,
        }

        public class Node
        {
            public string Name { get; private set; }
            public List<Node> from;
            public List<Node> to;
            public List<Node> fromLoop;
            public List<Node> toLoop;

            public int depth;
            public int power; // 현재 노드가 있는 줄의 최대 깊이

            public int xPos; // x 좌표

            public Node(string name)
            {
                Name = name;
                from = new List<Node>();
                to = new List<Node>();
                fromLoop = new List<Node>();
                toLoop = new List<Node>();

                depth = int.MinValue;
                power = int.MinValue;
                xPos = 0;
            }
        }

        public Node RootNode { get; private set; } // 시작점이 없는 노드들을의 시작점으로 취급하는 가상의 노드
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

        public IList<Node>GetNodes()
        {
            return NodeDictionary.Values.ToList().AsReadOnly();
        }

        /// <summary>
        /// 노드의 입력이 끝났다면, 노드의 배치를 결정하는 함수
        /// </summary>
        public void MakeFlow(Method method = Method.Mountain)
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

            // 각 노드의 Power값을 계산합니다.
            CalculatePower();

            // 각 노드의 X 좌표값을 계산합니다.
            CalculateXpos(method);

            // 브랜치의 마지막 노드는 다음 노드의 직전위치로 depth 를 변경합니다.
            RepositionDepth();

            // Test
            //foreach (Node n in NodeDictionary.Values)
            //{
            //    Console.WriteLine(n.Name + ", depth = " + n.depth + ", power = " + n.power + ", xPos = " + n.xPos);
            //    //Console.Write("    childs: ");

            //    foreach (Node c in n.to)
            //    {
            //        Console.Write(c.Name + ", ");
            //    }

            //    Console.Write("[");

            //    foreach (Node c in n.toLoop)
            //    {
            //        Console.Write(c.Name + ", ");
            //    }

            //    Console.Write("]");

            //    Console.WriteLine();
            //}
        }

        private void SeperateLoopLink(Node node, Stack<Node> stack)
        {
            // 루프를 판단하는 함수
            if (stack == null)
                stack = new Stack<Node>();

            stack.Push(node);

            for (int i = node.to.Count - 1; i >= 0; i--)
            {
                Node child = node.to[i];

                if (stack.Contains(child))
                {
                    var loopedNode = stack.FirstOrDefault(v => v == child);
                    loopedNode.fromLoop.Add(node);
                    loopedNode.from.Remove(node);

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
            // 깊이를 계산하는 함수

            if (node.depth < depth)
            {
                node.depth = depth;

                foreach (Node child in node.to)
                {
                    CalculateDepth(child, depth + 1);
                }
            }
        }

        private void CalculatePower()
        {
            // Power를 계산하는 함수

            List<Node> routeNode = new List<Node>(NodeDictionary.Values).OrderByDescending(v => v.depth).ToList();

            foreach (Node node in routeNode)
            {
                if (node.power >= node.depth)
                {
                    continue;
                }

                node.power = node.depth;

                foreach (Node p in node.from)
                {
                    CalculatePowerRecursive(p, node.power - (node.depth - p.depth - 1));
                }
            }
        }

        private void CalculatePowerRecursive(Node n, int power)
        {
            if (n.power < power)
            {
                n.power = power;

                foreach (Node p in n.from)
                {
                    CalculatePowerRecursive(p, n.power - (n.depth - p.depth - 1));
                }
            }
        }

        private void RepositionDepth()
        {
            foreach (Node n in NodeDictionary.Values)
            {
                Console.WriteLine(n.Name + " => " + (n.to.Count > 0 ? n.to.Max(v => v.from.Count) : -1));
                if (n.to.Count > 1 || (n.to.Count > 0 && n.to.Max(v => v.from.Count) > 1))
                {
                    n.depth = n.to.Min(v => v.depth) - 1;
                }
            }
        }

        private void CalculateXpos(Method method)
        {
            switch (method)
            {
                case Method.Mountain:
                    {
                        Queue<Node> CurrentNodeSet = new Queue<Node>();
                        Queue<Node> NextNodeSet = new Queue<Node>();

                        CurrentNodeSet.Enqueue(RootNode);
                        int currentDepth = 0;

                        while (CurrentNodeSet.Count > 0)
                        {
                            int xPos = 0;

                            while (CurrentNodeSet.Count > 0)
                            {
                                Node n = CurrentNodeSet.Dequeue();

                                if (n.depth == currentDepth)
                                {
                                    n.xPos = xPos;

                                    foreach (Node next in n.to)
                                    {
                                        if (!NextNodeSet.Contains(next))
                                            NextNodeSet.Enqueue(next);
                                    }
                                }
                                else
                                {
                                    if (!NextNodeSet.Contains(n))
                                        NextNodeSet.Enqueue(n);
                                }

                                xPos++;
                            }

                            CurrentNodeSet = NextNodeSet;
                            NextNodeSet = new Queue<Node>();
                            currentDepth++;
                        }
                    }

                    break;
            }
        }
    }
}
