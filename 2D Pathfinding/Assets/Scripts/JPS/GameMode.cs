using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace JPS
{
    public class GameMode : MonoBehaviour
    {
        [SerializeField] Transform _startPoint;
        [SerializeField] Transform _endPoint;

        [SerializeField] JPS _jps;
        [SerializeField] JPSNoDelay _jpsNoDelay;
        [SerializeField] GridComponent _gridComponent;

        List<Vector2> _points;

        bool _nowFind = false;

        private void Start()
        {
            _points = new List<Vector2>();
            _gridComponent.Initialize(_jps);
            _gridComponent.Initialize(_jpsNoDelay);
        }

        //private void Update()
        //{
        //    if (_nowFind == true) return;

        //    if (Input.GetMouseButtonDown(0))
        //    {
        //        _nowFind = true;
        //        _points = _jpsNoDelay.FindPath(_startPoint.position, _endPoint.position);
        //        _nowFind = false;
        //    }
        //}

        private async void Update()
        {
            if (_nowFind == true) return;

            if (Input.GetMouseButtonDown(0))
            {
                _nowFind = true;
                _points = await _jps.FindPath(_startPoint.position, _endPoint.position);
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