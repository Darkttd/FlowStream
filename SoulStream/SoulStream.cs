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
            Tetris,
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

        private class NodeBlock
        {
            public int depthFrom;
            public int depthTo;
            public int xPos;

            public List<Node> includeNodes;

            public List<NodeBlock> fromBlock;
            public List<NodeBlock> toBlock;

            public NodeBlock()
            {
                depthFrom = -1;
                depthTo = -1;
                xPos = 0;

                includeNodes = new List<Node>();

                fromBlock = new List<NodeBlock>();
                toBlock = new List<NodeBlock>();
            }
        }

        public Node RootNode { get; private set; } // 시작점이 없는 노드들을의 시작점으로 취급하는 가상의 노드
        private Dictionary<string, Node> NodeDictionary;
        private List<NodeBlock> NodeBlockList;

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
        public void MakeFlow(Method method = Method.Tetris)
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
            SeperateLoopLink(RootNode, null, null);

            // 각 노드의 깊이를 계산합니다.
            CalculateDepth(RootNode);

            // 각 노드의 Power값을 계산합니다.
            CalculatePower();

            // 브랜치의 마지막 노드는 다음 노드의 직전위치로 depth 를 변경합니다.
            RepositionDepth();

            // 노드를 블럭 단위로 묶습니다.
            BindNodeBlocks();

            // 각 노드의 X 좌표값을 계산합니다.
            CalculateXpos(method);
        }

        private void SeperateLoopLink(Node node, Stack<Node> stack, List<Node> visited)
        {
            // 루프를 판단하는 함수
            if (stack == null)
                stack = new Stack<Node>();

            if (visited == null)
                visited = new List<Node>();

            stack.Push(node);
            visited.Add(node);

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
                    if (!visited.Contains(child))
                        SeperateLoopLink(child, stack, visited);
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
            int hiddenNodeCount = 0;

            List<Node> hiddenNodeList = new List<Node>();

            foreach (Node n in NodeDictionary.Values)
            {
                if (n.to.Count > 1 || (n.to.Count > 0 && n.to.Max(v => v.from.Count) > 1))
                {
                    // 갈라지는 마지막 노드의 depth 를 재조정

                    int lastDepth = n.to.Min(v => v.depth) - 1;

                    if ((n.from.Count > 1 || (n.from.Count > 0 && n.from.Max(v => v.to.Count) > 1))
                        && n.depth < lastDepth)
                    {
                        // 단, 현재 노드가 유일한 노드이고, 차지하는 깊이가 2 이상이면, 임시 노드를 생성한다

                        string nodeName = HiddenNodePrefix + (hiddenNodeCount++);
                        Node hiddenNode = new Node(nodeName);
                        hiddenNode.from.Add(n);
                        hiddenNode.to = n.to;
                        hiddenNode.depth = lastDepth;
                        n.to = new List<Node>();
                        n.to.Add(hiddenNode);

                        foreach (Node nextNode in hiddenNode.to)
                        {
                            nextNode.from.Remove(n);
                            nextNode.from.Add(hiddenNode);
                        }

                        hiddenNodeList.Add(hiddenNode);
                    }
                    else
                    {
                        n.depth = lastDepth;
                    }
                }
            }

            foreach (Node n in hiddenNodeList)
            {
                NodeDictionary.Add(n.Name, n);
            }
        }

        private void BindNodeBlocks()
        {
            NodeBlockList = new List<NodeBlock>();

            foreach (Node n in NodeDictionary.Values)
            {
                if (n.to.Count != 1 || (n.to.Count > 0 && n.to.Max(v => v.from.Count) > 1))
                {
                    NodeBlock nodeBlock = new NodeBlock();

                    int maxDepth = int.MinValue;
                    int minDepth = int.MaxValue;
                    Node prevNode = n;

                    do
                    {
                        nodeBlock.includeNodes.Add(prevNode);

                        if (maxDepth < prevNode.depth)
                            maxDepth = prevNode.depth;
                        if (minDepth > prevNode.depth)
                            minDepth = prevNode.depth;

                        if (prevNode.from.Count != 1)
                            break;
                        else
                            prevNode = prevNode.from[0];
                    } while (prevNode.to.Count == 1);

                    nodeBlock.depthFrom = minDepth;
                    nodeBlock.depthTo = maxDepth;
                    // 역순으로 추가한 뒤 뒤집었으므로,
                    // nodeBlock.includeNodes 는 depth 순서대로입니다.
                    nodeBlock.includeNodes.Reverse();

                    NodeBlockList.Add(nodeBlock);
                }
            }

            foreach (NodeBlock nodeBlock in NodeBlockList)
            {
                // 노드블록끼리의 연결을 링크합니다

                Node n = nodeBlock.includeNodes[0];

                foreach (Node prevBlock in n.from)
                {
                    NodeBlock includePrev = NodeBlockList.FirstOrDefault(v => v.includeNodes.Contains(prevBlock));

                    nodeBlock.fromBlock.Add(includePrev);
                    includePrev.toBlock.Add(nodeBlock);
                }
            }
        }

        private void CalculateXpos(Method method)
        {
            switch (method)
            {
                case Method.Tetris:
                    {
                        int maxDepth = 0;

                        foreach (NodeBlock nb in NodeBlockList)
                        {
                            if (maxDepth < nb.depthTo)
                                maxDepth = nb.depthTo;
                        }

                        List<bool>[] isFilled = new List<bool>[maxDepth + 1];

                        for (int i = 0; i < isFilled.Length; ++i)
                        {
                            isFilled[i] = new List<bool>();
                        }

                        LinkedList<NodeBlock> CurrentNodeBlock = new LinkedList<NodeBlock>();
                        List<NodeBlock> AlreadyRegistered = new List<NodeBlock>();

                        CurrentNodeBlock.AddLast(NodeBlockList.FirstOrDefault(v => v.includeNodes.Contains(RootNode)));
                        AlreadyRegistered.Add(CurrentNodeBlock.First.Value);

                        while (CurrentNodeBlock.Count > 0)
                        {
                            NodeBlock nodeBlock = CurrentNodeBlock.First.Value;
                            CurrentNodeBlock.RemoveFirst();

                            int targetX = 0;
                            do
                            {
                                bool isPlaced = true;

                                for (int depth = nodeBlock.depthFrom; depth <= nodeBlock.depthTo; ++depth)
                                {
                                    if (isFilled[depth].Count > targetX && isFilled[depth][targetX])
                                    {
                                        // 이미 차 있으므로, 중단하고 다음 targetX 를 찾는다.
                                        isPlaced = false;
                                        break;
                                    }
                                }

                                if (isPlaced)
                                {
                                    // 위치를 찾은 경우, x 좌표를 대입한다.

                                    nodeBlock.xPos = targetX;

                                    for (int depth = nodeBlock.depthFrom; depth <= nodeBlock.depthTo; ++depth)
                                    {
                                        while (isFilled[depth].Count <= targetX)
                                        {
                                            isFilled[depth].Add(false);
                                        }

                                        isFilled[depth][targetX] = true;
                                    }

                                    foreach (Node n in nodeBlock.includeNodes)
                                    {
                                        n.xPos = targetX;
                                    }

                                    // 그리고 중단
                                    break;
                                }

                                // 찾지 못한 경우, 계속해서 탐색
                                ++targetX;

                            } while (true);

                            for (int i = nodeBlock.toBlock.Count - 1; i >= 0; --i)
                            {
                                NodeBlock next = nodeBlock.toBlock[i];

                                if (!AlreadyRegistered.Contains(next))
                                {
                                    CurrentNodeBlock.AddFirst(next);
                                    AlreadyRegistered.Add(next);
                                }
                            }
                        }
                    }
                    break;
            }
        }
    }
}
