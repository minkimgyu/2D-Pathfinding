using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

namespace Dijkstra
{
    public class Dijkstra : MonoBehaviour
    {
        GridComponent _gridComponent;

        public void Initialize(GridComponent gridComponent)
        {
            _gridComponent = gridComponent;
            _heap = new Heap<Node>(10000);
            _visited = new HashSet<Node>();
        }

        Vector2 TopLeft = new Vector2(-1, 1);
        Vector2 TopRight = new Vector2(1, 1);
        Vector2 BottomLeft = new Vector2(-1, -1);
        Vector2 BottomRight = new Vector2(1, -1);

        Heap<Node> _heap;
        HashSet<Node> _visited;

        public void FindPathWithProcedure(Vector2Int index)
        {
            _gridComponent.ResetNodeWeight();
            Node startNode = _gridComponent.ReturnNode(index);
            startNode.PathWeight = 0;

            _heap.Insert(startNode); // 시작 노드 삽입

            while (_heap.Count > 0)
            {
                Node minNode = _heap.ReturnMin();
                _heap.DeleteMin();

                if(_visited.Contains(minNode) == true) continue; // 이미 방문한 경우 continue;

                _visited.Add(minNode);  // ✅ 이제 방문 확정!
                _visitedList.Add(minNode);

                List<Node> nearNodes = minNode.NearNodes;
                for (int i = 0; i < nearNodes.Count; i++)
                {
                    float currentWeight = nearNodes[i].Weight;

                    Vector2 directionVec = nearNodes[i].WorldPos - minNode.WorldPos;
                    if (directionVec == TopLeft ||
                        directionVec == TopRight ||
                        directionVec == BottomLeft ||
                        directionVec == BottomRight)
                    {
                        // 대각 방향이면 가중치를 1.4배 추가해줘야한다.
                        currentWeight *= 1.4f;
                    }

                    // minNode의 지금까지의 경로 가중치 + 주변 노드의 노드 가중치
                    float pathWeight = minNode.PathWeight + currentWeight;
                    if (nearNodes[i].PathWeight <= pathWeight) continue;  // 가중치가 기존 것보다 더 큰 경우 건너뛰기

                    // 가중치 업데이트
                    nearNodes[i].PathWeight = pathWeight;
                    _heap.Insert(nearNodes[i]);
                    _heapList.Add(nearNodes[i]);
                }
            }

            _gridComponent.CalculateNodePath(startNode);
            _heap.Clear();
            _visited.Clear();
            _visitedList.Clear(); // 새로운 탐색이 시작되기 전에 초기화
            _heapList.Clear(); // 새로운 탐색이 시작되기 전에 초기화
        }

        List<Node> _visitedList = new List<Node>();
        List<Node> _heapList = new List<Node>();

        private void OnDrawGizmosSelected()
        {
            if (_heapList != null && _heapList.Count != 0)
            {
                for (int i = 0; i < _heapList.Count; i++)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawCube(_heapList[i].WorldPos, new Vector2(0.8f, 0.8f));
                }
            }

            if (_visitedList != null && _visitedList.Count != 0)
            {
                for (int i = 0; i < _visitedList.Count; i++)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawCube(_visitedList[i].WorldPos, new Vector2(0.8f, 0.8f));
                }
            }
        }
    }
}