using System.Collections.Generic;
using System;
using UnityEngine.Tilemaps;
using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Dijkstra
{
    public struct NearNodeData
    {
        List<Node> nearNodes;
        public List<Node> NearNodes { get { return nearNodes; } }

        bool haveBlockNodeInNeighbor;
        public bool HaveBlockNodeInNeighbor { get { return haveBlockNodeInNeighbor; } }

        public NearNodeData(List<Node> nearNodes, bool haveBlockNodeInNeighbor)
        {
            this.nearNodes = nearNodes;
            this.haveBlockNodeInNeighbor = haveBlockNodeInNeighbor;
        }
    }

    public class GridComponent : MonoBehaviour
    {
        public enum Direction
        {
            UpLeft,
            Up,
            UpRight,
            Left,
            Current,
            Right,
            DownLeft,
            Down,
            DownRight
        }

        public Dictionary<Vector2Int, Direction> directions = new Dictionary<Vector2Int, Direction>()
    {
        { new Vector2Int(-1, 1), Direction.UpLeft },
        { new Vector2Int(0, 1), Direction.Up },
        { new Vector2Int(1, 1), Direction.UpRight },

        { new Vector2Int(-1, 0), Direction.Left },
        { new Vector2Int(0, 0), Direction.Current },
        { new Vector2Int(1, 0), Direction.Right },

        { new Vector2Int(-1, -1), Direction.DownLeft },
        { new Vector2Int(0, -1), Direction.Down },
        { new Vector2Int(1, -1), Direction.DownRight },
    };

        public Direction ReturnDirection(Vector2Int directionVector)
        {
            return directions[directionVector];
        }

        public enum ShowType
        {
            Block,
            Weight,
            Direction
        }

        GridGenerator _gridGenerator;
        Dijkstra _dijkstra;
        [SerializeField] ShowType _showType;

        Node[,] _nodes; // r, c
        Vector2 _topLeftWorldPoint;
        Vector2Int _topLeftLocalPoint;

        Vector2Int _gridSize;
        const int _nodeSize = 1;

        public Node ReturnNode(Vector2Int grid) { return _nodes[grid.x, grid.y]; }
        public Node ReturnNode(int r, int c) { return _nodes[r, c]; }

        public void ResetNodeWeight()
        {
            for (int i = 0; i < _gridSize.x; i++)
            {
                for (int j = 0; j < _gridSize.y; j++)
                {
                    _nodes[i, j].PathWeight = float.MaxValue;
                }
            }
        }

        public void CalculateNodePath(Node startNode)
        {
            for (int i = 0; i < _gridSize.x; i++)
            {
                for (int j = 0; j < _gridSize.y; j++)
                {
                    if (startNode == _nodes[i, j])
                    {
                        startNode.DirectionToMove = Vector2Int.zero;
                        continue;
                    }

                    List<Node> nearNodes = _nodes[i, j].NearNodes;
                    float minWeight = float.MaxValue;
                    int minIndex = 0;

                    bool haveBlockNodeInNeighbor = _nodes[i, j].HaveBlockNodeInNeighbor;

                    for (int k = 0; k < nearNodes.Count; k++)
                    {
                        // 현재 노드 주변에 Block 노드가 있고 주변 노드의 주변에 Block 노드가 있는 경우 건너뛰기
                        if (haveBlockNodeInNeighbor && nearNodes[k].HaveBlockNodeInNeighbor == true) continue;

                        if (nearNodes[k].PathWeight < minWeight)
                        {
                            minWeight = nearNodes[k].PathWeight;
                            minIndex = k;
                        }
                    }

                    Vector2 dir = nearNodes[minIndex].WorldPos - _nodes[i, j].WorldPos;
                    _nodes[i, j].DirectionToMove = new Vector2Int((int)dir.x, (int)dir.y);
                }
            }
        }

        public Vector2 ReturnClampedRange(Vector2 pos)
        {
            Vector2 topLeftPos = ReturnNode(0, 0).WorldPos;
            Vector2 bottomRightPos = ReturnNode(_nodes.GetLength(0) - 1, _nodes.GetLength(1) - 1).WorldPos;

            // 반올림하고 범위 안에 맞춰줌
            // 이 부분은 GridSize 바뀌면 수정해야함
            float xPos = Mathf.Clamp(pos.x, topLeftPos.x, bottomRightPos.x);
            float yPos = Mathf.Clamp(pos.y, bottomRightPos.y, topLeftPos.y);

            return new Vector2(xPos, yPos);
        }

        public Vector2 ReturnNodeDirection(Vector2 worldPos)
        {
            Vector2Int index = ReturnNodeIndex(worldPos);
            Node node = ReturnNode(index);
            return node.DirectionToMove;
        }

        public Vector2Int ReturnNodeIndex(Vector2 worldPos)
        {
            Vector2 clampedPos = ReturnClampedRange(worldPos);
            Vector2 topLeftPos = ReturnNode(0, 0).WorldPos;

            int r = Mathf.RoundToInt(Mathf.Abs(topLeftPos.y - clampedPos.y) / _nodeSize);
            int c = Mathf.RoundToInt(Mathf.Abs(topLeftPos.x - clampedPos.x) / _nodeSize); // 인덱스이므로 1 빼준다.
            return new Vector2Int(r, c);
        }

        public Vector2 ReturnClampPos(Vector2 worldPos)
        {
            Vector2Int grid = ReturnNodeIndex(worldPos);
            return _nodes[grid.x, grid.y].WorldPos;
        }

        bool IsOutOfRange(Vector2Int index) { return index.x < 0 || index.y < 0 || index.x >= _gridSize.x || index.y >= _gridSize.y; }

        public NearNodeData ReturnNearNodeData(Vector2Int index)
        {
            List<Node> nearNodes = new List<Node>();
            bool haveBlockNode = false;

            //      (1)
            // (0)↖ ↑ ↗(2)
            // (3)←    →(4)
            // (5)↙ ↓ ↘(7)
            //      (6)
            //              의 경우
            Vector2Int[] nearIndexes = new Vector2Int[]
            {
            new Vector2Int(index.x - 1, index.y + 1), new Vector2Int(index.x, index.y + 1), new Vector2Int(index.x + 1, index.y + 1),

            new Vector2Int(index.x - 1, index.y), new Vector2Int(index.x + 1, index.y),

            new Vector2Int(index.x - 1, index.y - 1), new Vector2Int(index.x, index.y - 1), new Vector2Int(index.x + 1, index.y - 1),
            };

            for (int i = 0; i < nearIndexes.Length; i++)
            {
                bool isOutOfRange = IsOutOfRange(nearIndexes[i]);
                if (isOutOfRange == true) continue;

                bool canPass = true;
                bool topIsBlock, leftIsBlock, rightIsBlock, bottomIsBlock;

                switch (i)
                {
                    case 0:
                        topIsBlock = ReturnNode(nearIndexes[1]).CurrentState == Node.State.Block;
                        leftIsBlock = ReturnNode(nearIndexes[3]).CurrentState == Node.State.Block;
                        if (topIsBlock || leftIsBlock) canPass = false;

                        break;
                    case 2:
                        topIsBlock = ReturnNode(nearIndexes[1]).CurrentState == Node.State.Block;
                        rightIsBlock = ReturnNode(nearIndexes[4]).CurrentState == Node.State.Block;
                        if (topIsBlock || rightIsBlock) canPass = false;

                        break;
                    case 5:
                        leftIsBlock = ReturnNode(nearIndexes[3]).CurrentState == Node.State.Block;
                        bottomIsBlock = ReturnNode(nearIndexes[6]).CurrentState == Node.State.Block;
                        if (leftIsBlock || bottomIsBlock) canPass = false;

                        break;
                    case 7:
                        rightIsBlock = ReturnNode(nearIndexes[4]).CurrentState == Node.State.Block;
                        bottomIsBlock = ReturnNode(nearIndexes[6]).CurrentState == Node.State.Block;
                        if (rightIsBlock || bottomIsBlock) canPass = false;

                        break;
                    default:
                        break;
                }

                if (canPass == false) continue; // 못 가는 지역이라면 건너뛰기

                Node node = ReturnNode(nearIndexes[i]);
                if (node.CurrentState == Node.State.Block)
                {
                    haveBlockNode = true;
                }

                nearNodes.Add(node);
            }

            NearNodeData nearNodeData = new NearNodeData(nearNodes, haveBlockNode);
            return nearNodeData;
        }

        private void OnDrawGizmos()
        {
            if (_nodes == null) return;

            for (int i = 0; i < _gridSize.x; i++)
            {
                for (int j = 0; j < _gridSize.y; j++)
                {
                    if (_showType == ShowType.Direction)
                    {
                        if (_nodes[i, j].CurrentState == Node.State.Block)
                        {
                            Handles.Label(_nodes[i, j].WorldPos, "X", blockStyle);
                        }
                        else
                        {
                            Direction direction = directions[_nodes[i, j].DirectionToMove];
                            switch (direction)
                            {
                                case Direction.UpLeft:
                                    Handles.Label(_nodes[i, j].WorldPos, "↖", emptyStyle);
                                    break;
                                case Direction.Up:
                                    Handles.Label(_nodes[i, j].WorldPos, "↑", emptyStyle);
                                    break;
                                case Direction.UpRight:
                                    Handles.Label(_nodes[i, j].WorldPos, "↗", emptyStyle);
                                    break;
                                case Direction.Left:
                                    Handles.Label(_nodes[i, j].WorldPos, "←", emptyStyle);
                                    break;
                                case Direction.Current:
                                    Handles.Label(_nodes[i, j].WorldPos, "○", startPointStyle);
                                    break;
                                case Direction.Right:
                                    Handles.Label(_nodes[i, j].WorldPos, "→", emptyStyle);
                                    break;
                                case Direction.DownLeft:
                                    Handles.Label(_nodes[i, j].WorldPos, "↙", emptyStyle);
                                    break;
                                case Direction.Down:
                                    Handles.Label(_nodes[i, j].WorldPos, "↓", emptyStyle);
                                    break;
                                case Direction.DownRight:
                                    Handles.Label(_nodes[i, j].WorldPos, "↘", emptyStyle);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    else if (_showType == ShowType.Block)
                    {
                        switch (_nodes[i, j].CurrentState)
                        {
                            case Node.State.Empty:
                                Handles.Label(_nodes[i, j].WorldPos, "○", emptyStyle);
                                break;
                            case Node.State.Block:
                                Handles.Label(_nodes[i, j].WorldPos, "X", blockStyle);
                                break;
                            default:
                                break;
                        }
                    }
                    else if (_showType == ShowType.Weight)
                    {
                        switch (_nodes[i, j].CurrentState)
                        {
                            case Node.State.Empty:
                                Handles.Label(_nodes[i, j].WorldPos, _nodes[i, j].PathWeight.ToString(), emptyStyle);
                                break;
                            case Node.State.Block:
                                Handles.Label(_nodes[i, j].WorldPos, _nodes[i, j].PathWeight.ToString(), blockStyle);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }

        // 스타일 지정
        GUIStyle blockStyle = new GUIStyle();
        GUIStyle emptyStyle = new GUIStyle();
        GUIStyle weightStyle = new GUIStyle();

        GUIStyle startPointStyle = new GUIStyle();

        public void Initialize(GridGenerator gridGenerator, Dijkstra dijkstra)
        {
            _gridGenerator = gridGenerator;
            _dijkstra = dijkstra;

            _showType = ShowType.Direction;

            blockStyle.fontSize = 20;
            blockStyle.alignment = TextAnchor.MiddleCenter;
            blockStyle.normal.textColor = Color.red;

            emptyStyle.fontSize = 20;
            emptyStyle.alignment = TextAnchor.MiddleCenter;
            emptyStyle.normal.textColor = Color.blue;

            weightStyle.fontSize = 5;
            weightStyle.alignment = TextAnchor.MiddleCenter;
            weightStyle.normal.textColor = Color.white;

            startPointStyle.fontSize = 20;
            startPointStyle.alignment = TextAnchor.MiddleCenter;
            startPointStyle.normal.textColor = Color.white;

            _nodes = _gridGenerator.CreateGrid();
            _gridSize = new Vector2Int(_nodes.GetLength(0), _nodes.GetLength(1)); // --> x는 row y는 column이다.

            for (int i = 0; i < _gridSize.x; i++)
            {
                for (int j = 0; j < _gridSize.y; j++)
                {
                    NearNodeData neaNodeData = ReturnNearNodeData(new Vector2Int(i, j));

                    _nodes[i, j].NearNodes = neaNodeData.NearNodes;
                    _nodes[i, j].HaveBlockNodeInNeighbor = neaNodeData.HaveBlockNodeInNeighbor;
                }
            }
            _dijkstra.Initialize(this);
        }
    }
}