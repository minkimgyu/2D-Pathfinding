using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace AStar
{
    public class GameMode : MonoBehaviour
    {
        [SerializeField] Transform _startPoint;
        [SerializeField] Transform _endPoint;

        [SerializeField] AStarNoDelay _aStarNoDelay;
        [SerializeField] GridComponent _gridComponent;

        List<Vector2> _points;

        bool _nowFind = false;

        private void Start()
        {
            _points = new List<Vector2>();
            _gridComponent.Initialize(_aStarNoDelay);
        }

        private void Update()
        {
            if (_nowFind == true) return;

            if (Input.GetMouseButtonDown(0))
            {
                _nowFind = true;
                _points = _aStarNoDelay.FindPath(_startPoint.position, _endPoint.position);
                _nowFind = false;
            }
        }

        private void OnDrawGizmos()
        {
            if (_points == null || _points.Count == 0) return;

            for (int i = 1; i < _points.Count; i++)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(_points[i - 1], _points[i]);
            }
        }
    }
}