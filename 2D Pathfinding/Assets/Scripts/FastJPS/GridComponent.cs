using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Debug = UnityEngine.Debug;

namespace FastJPS
{
    // ↖ ↑ ↗  5 1 6
    // ←    →  4 0 2
    // ↙ ↓ ↘  8 3 7

    //public enum Way
    //{
    //    UpStraight, // 0
    //    RightStraight, // 1
    //    DownStraight, // 2
    //    LeftStraight, // 3

    //    UpLeftDiagonal, // 4
    //    UpRightDiagonal, // 5
    //    DownRightDiagonal, // 6
    //    DownLeftDiagonal, // 7
    //}

    [Serializable]
    public struct Grid2D
    {
        [SerializeField] int row;
        public int Row { get { return row; } }

        [SerializeField] int column;
        public int Column { get { return column; } }

        public Grid2D(int row, int column)
        {
            this.row = row;
            this.column = column;
        }
    }

    public class GridComponent : MonoBehaviour
    {
        [SerializeField] Tilemap _wallTile;
        [SerializeField] Tilemap _groundTile;

        Node[,] _nodes; // r, c
        Vector2 _topLeftWorldPoint;
        Vector2Int _topLeftLocalPoint;

        Grid2D _gridSize;

        List<Vector2> _points;
        const int _nodeSize = 1;

        public Node ReturnNode(Grid2D grid) { return _nodes[grid.Row, grid.Column]; }
        public Node ReturnNode(int r, int c) { return _nodes[r, c]; }

        Vector2Int _bottomIndex;

        public Vector2 ReturnClampedRange(Vector2 pos)
        {
            Vector2 topLeftPos = ReturnNode(0, 0).WorldPos;
            Vector2 bottomRightPos = ReturnNode(_gridSize.Row - 1, _gridSize.Column - 1).WorldPos;

            // 반올림하고 범위 안에 맞춰줌
            // 이 부분은 GridSize 바뀌면 수정해야함
            float xPos = Mathf.Clamp(pos.x, topLeftPos.x, bottomRightPos.x);
            float yPos = Mathf.Clamp(pos.y, bottomRightPos.y, topLeftPos.y);

            return new Vector2(xPos, yPos);
        }

        public Grid2D ReturnNodeIndex(Vector2 worldPos)
        {
            Vector2 clampedPos = ReturnClampedRange(worldPos);
            Vector2 topLeftPos = ReturnNode(0, 0).WorldPos;

            int r = Mathf.RoundToInt(Mathf.Abs(topLeftPos.y - clampedPos.y) / _nodeSize);
            int c = Mathf.RoundToInt(Mathf.Abs(topLeftPos.x - clampedPos.x) / _nodeSize); // 인덱스이므로 1 빼준다.
            return new Grid2D(r, c);
        }

        //public enum Way
        //{
        //    UpStraight,
        //    RightStraight,
        //    DownStraight,
        //    LeftStraight,

        //    UpLeftDiagonal,
        //    UpRightDiagonal,
        //    DownRightDiagonal,
        //    DownLeftDiagonal,
        //}

        // ↖ ↑ ↗  5 1 6
        // ←    →  4 0 2
        // ↙ ↓ ↘  8 3 7

        // ↖ ↑ ↗  4 0 5
        // ←    →  3   1
        // ↙ ↓ ↘  7 2 6

        Grid2D[] _direction = 
        {
            new Grid2D(-1, 0), // 1
            new Grid2D(0, 1), // 2
            new Grid2D(1, 0), // 3
            new Grid2D(0, -1), // 4

            new Grid2D(-1, -1), // 5
            new Grid2D(-1, 1), // 6
            new Grid2D(1, 1), // 7
            new Grid2D(1, -1) // 8
        };

        public Tuple<bool[], Node[], bool> GetNeighborInfo(Grid2D index)
        {
            int directionSize = _direction.Length;

            bool[] haveNodes = new bool[directionSize];
            Node[] nearNodes = new Node[directionSize];
            bool haveNearBlockNode = false;

            for (int i = 0; i < directionSize; i++)
            {
                Grid2D newGrid = new Grid2D(index.Row + _direction[i].Row, index.Column + _direction[i].Column);
                bool isOutOfRange = IsOutOfRange(newGrid);
                if (isOutOfRange == true)
                {
                    haveNodes[i] = false;
                    continue;
                }

                nearNodes[i] = ReturnNode(newGrid);
                if(nearNodes[i].Block == true)
                {
                    haveNearBlockNode = true;
                }
                haveNodes[i] = true;
            }

            return new Tuple<bool[], Node[], bool>(haveNodes, nearNodes, haveNearBlockNode);
        }


        public bool IsOutOfRange(Grid2D index) 
        {
            return index.Row < 0 || index.Column < 0 || index.Row >= _gridSize.Row || index.Column >= _gridSize.Column;
        }


        void CreateNode()
        {
            for (int i = 0; i < _gridSize.Row; i++)
            {
                for (int j = 0; j < _gridSize.Column; j++)
                {
                    Vector2Int localPos = _topLeftLocalPoint + new Vector2Int(j, -i);
                    Vector2 worldPos = _topLeftWorldPoint + new Vector2Int(j, -i);

                    TileBase tile = _wallTile.GetTile(new Vector3Int(localPos.x, localPos.y, 0));
                    if (tile == null)
                    {
                        _nodes[i, j] = new Node(worldPos, new Grid2D(i, j), false);
                    }
                    else
                    {
                        _nodes[i, j] = new Node(worldPos, new Grid2D(i, j), true);
                    }
                    // 타일이 없다면 바닥
                    // 타일이 존재한다면 벽
                }
            }

            for (int i = 0; i < _gridSize.Row; i++)
            {
                for (int j = 0; j < _gridSize.Column; j++)
                {
                    Tuple<bool[], Node[], bool> nodeDatas = GetNeighborInfo(new Grid2D(i, j));
                    _nodes[i, j].HaveNodes = nodeDatas.Item1;
                    _nodes[i, j].NearNodes = nodeDatas.Item2;
                    _nodes[i, j].HaveNearBlockNode = nodeDatas.Item3;
                }
            }

            Debug.Log("CreateNode");
        }

        private void OnDrawGizmos()
        {
            if (_points == null) return;

            for (int i = 1; i < _points.Count; i++)
            {
                Gizmos.color = new Color(0, 1, 1, 0.1f);
                Gizmos.DrawLine(_points[i - 1], _points[i]);
            }

            if (_nodes == null) return;

            for (int i = 0; i < _nodes.GetLength(0); i++)
            {
                for (int j = 0; j < _nodes.GetLength(1); j++)
                {
                    if (_nodes[i, j].Block)
                    {
                        Gizmos.color = new Color(1, 0, 0, 0.1f);
                        Gizmos.DrawCube(_nodes[i, j].WorldPos, Vector3.one);
                    }
                    else
                    {
                        Gizmos.color = new Color(0, 0, 1, 0.1f);
                        Gizmos.DrawCube(_nodes[i, j].WorldPos, Vector3.one);
                    }
                }
            }
        }

        public void Initialize(FastJPS pathfinder)
        {
            _groundTile.CompressBounds(); // 타일의 바운더리를 맞춰준다.
            _wallTile.CompressBounds(); // 타일의 바운더리를 맞춰준다.
            BoundsInt bounds = _groundTile.cellBounds;

            int rowSize = bounds.yMax - bounds.yMin;
            int columnSize = bounds.xMax - bounds.xMin;

            _topLeftLocalPoint = new Vector2Int(bounds.xMin, bounds.yMax - 1);
            _topLeftWorldPoint = new Vector2(transform.position.x + bounds.xMin + _groundTile.tileAnchor.x, transform.position.y + bounds.yMax - _groundTile.tileAnchor.y);

            Debug.Log(_topLeftLocalPoint);
            Debug.Log(_topLeftWorldPoint);

            _gridSize = new Grid2D(rowSize, columnSize);
            _points = new List<Vector2>();
            _nodes = new Node[_gridSize.Row, _gridSize.Column];
            CreateNode();

            pathfinder.Initialize(this);
        }

        public void Initialize(FastJPSNoDelay pathfinder)
        {
            _groundTile.CompressBounds(); // 타일의 바운더리를 맞춰준다.
            _wallTile.CompressBounds(); // 타일의 바운더리를 맞춰준다.
            BoundsInt bounds = _groundTile.cellBounds;

            int rowSize = bounds.yMax - bounds.yMin;
            int columnSize = bounds.xMax - bounds.xMin;

            _topLeftLocalPoint = new Vector2Int(bounds.xMin, bounds.yMax - 1);
            _topLeftWorldPoint = new Vector2(transform.position.x + bounds.xMin + _groundTile.tileAnchor.x, transform.position.y + bounds.yMax - _groundTile.tileAnchor.y);

            Debug.Log(_topLeftLocalPoint);
            Debug.Log(_topLeftWorldPoint);

            _gridSize = new Grid2D(rowSize, columnSize);
            _points = new List<Vector2>();
            _nodes = new Node[_gridSize.Row, _gridSize.Column];
            CreateNode();

            pathfinder.Initialize(this);
        }
    }
}