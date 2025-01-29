using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Dijkstra
{
    public class GameMode : MonoBehaviour
    {
        [SerializeField] GridComponent _gridComponent;
        [SerializeField] GridGenerator _gridGenerator;
        [SerializeField] Dijkstra _dijkstra;

        bool _nowFind = false;

        private void Start()
        {
            _gridComponent.Initialize(_gridGenerator, _dijkstra);
        }

        // 마우스 위치 받아서 접근
        private void Update()
        {
            if (_nowFind == true) return;

            if (Input.GetMouseButtonDown(0))
            {
                _nowFind = true;
                Vector2 mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -Camera.main.transform.position.z));
                Vector2Int startIndex = _gridComponent.ReturnNodeIndex(mousePos);
                _dijkstra.FindPathWithProcedure(startIndex);
                _nowFind = false;
            }
        }
    }

}