using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Debug = UnityEngine.Debug;

namespace JPS
{
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

        public Grid2D ReturnNodeIndex(Vector2 worldPos)
        {
            Vector2 clampedPos = ReturnClampedRange(worldPos);
            Vector2 topLeftPos = ReturnNode(0, 0).WorldPos;

            int r = Mathf.RoundToInt(Mathf.Abs(topLeftPos.y - clampedPos.y) / _nodeSize);
            int c = Mathf.RoundToInt(Mathf.Abs(topLeftPos.x - clampedPos.x) / _nodeSize); // 인덱스이므로 1 빼준다.
            return new Grid2D(r, c);
        }

        public Dictionary<Way, Grid2D> _direction = new Dictionary<Way, Grid2D>()
        {
            { Way.UpStraight, new Grid2D(-1, 0) }, // r, c
            { Way.DownStraight, new Grid2D(1, 0) },
            { Way.LeftStraight, new Grid2D(0, -1) },
            { Way.RightStraight, new Grid2D(0, 1) },

            { Way.UpLeftDiagonal, new Grid2D(-1, -1) }, // r, c
            { Way.UpRightDiagonal, new Grid2D(-1, 1) },
            { Way.DownLeftDiagonal, new Grid2D(1, -1) },
            { Way.DownRightDiagonal, new Grid2D(1, 1) },
        };

        public Dictionary<Way, Node> GetDirectionInfo(Grid2D index)
        {
            Dictionary<Way, Node> nearNodes = new Dictionary<Way, Node>();

            foreach (var item in _direction)
            {
                Grid2D newGrid = new Grid2D(index.Row + item.Value.Row, index.Column + item.Value.Column);
                bool isOutOfRange = IsOutOfRange(newGrid);
                if (isOutOfRange == true) continue;

                nearNodes.Add(item.Key, ReturnNode(newGrid));
            }

            return nearNodes;
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
                    _nodes[i, j].NearNodes = GetDirectionInfo(new Grid2D(i, j));
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

        public void Initialize(JPSNoDelay pathfinder)
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

        public void Initialize(JPS pathfinder)
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