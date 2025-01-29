using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dijkstra
{
    public class GridGenerator : MonoBehaviour
    {
        [SerializeField] Tilemap _wallTile;
        [SerializeField] Tilemap _groundTile;

        Node[,] _nodes; // r, c
        Vector2 _topLeftWorldPoint;
        UnityEngine.Vector2Int _topLeftLocalPoint;

        Vector2Int _gridSize;

        public Node[,] CreateGrid()
        {
            _groundTile.CompressBounds(); // 타일의 바운더리를 맞춰준다.
            BoundsInt bounds = _groundTile.cellBounds;

            int rowSize = bounds.yMax - bounds.yMin;
            int columnSize = bounds.xMax - bounds.xMin;

            _topLeftLocalPoint = new UnityEngine.Vector2Int(bounds.xMin, bounds.yMax - 1);
            _topLeftWorldPoint = new Vector2(transform.position.x + bounds.xMin + _groundTile.tileAnchor.x, transform.position.y + bounds.yMax - _groundTile.tileAnchor.y);

            //Debug.Log(_topLeftLocalPoint);
            //Debug.Log(_topLeftWorldPoint);

            _gridSize = new Vector2Int(rowSize, columnSize);
            _nodes = new Node[_gridSize.x, _gridSize.y];
            for (int i = 0; i < _gridSize.x; i++)
            {
                for (int j = 0; j < _gridSize.y; j++)
                {
                    UnityEngine.Vector2Int localPos = _topLeftLocalPoint + new UnityEngine.Vector2Int(j, -i);
                    Vector2 worldPos = _topLeftWorldPoint + new UnityEngine.Vector2Int(j, -i);

                    Vector2Int grid2D = new Vector2Int(i, j);

                    TileBase tile = _wallTile.GetTile(new Vector3Int(localPos.x, localPos.y, 0));
                    Node node;
                    if (tile == null)
                    {
                        node = new Node(worldPos, grid2D, Node.State.Empty);
                    }
                    else
                    {
                        node = new Node(worldPos, grid2D, Node.State.Block);
                    }

                    _nodes[i, j] = node;
                    // 타일이 없다면 바닥
                    // 타일이 존재한다면 벽
                }
            }

            return _nodes;
        }
    }
}