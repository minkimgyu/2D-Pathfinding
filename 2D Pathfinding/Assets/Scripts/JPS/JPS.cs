using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace JPS
{
    public enum Way
    {
        UpStraight,
        RightStraight,
        DownStraight,
        LeftStraight,

        UpLeftDiagonal,
        UpRightDiagonal,
        DownRightDiagonal,
        DownLeftDiagonal,
    }

    public class JPS : MonoBehaviour
    {
        Func<Vector2, Grid2D> ReturnNodeIndex;
        Func<Grid2D, Node> ReturnNode;

        const int maxSize = 1000;

        Heap<Node> _openList = new Heap<Node>(maxSize);
        HashSet<Node> _closedList = new HashSet<Node>();

        public void Initialize(GridComponent gridComponent)
        {
            ReturnNodeIndex = gridComponent.ReturnNodeIndex;
            ReturnNode = gridComponent.ReturnNode;
        }

        List<Vector2> ConvertNodeToV2(Stack<Node> stackNode)
        {
            List<Vector2> points = new List<Vector2>();
            while (stackNode.Count > 0)
            {
                Node node = stackNode.Pop();
                points.Add(node.WorldPos);
            }

            return points;
        }

        [SerializeField] int _awaitDuration = 30;

        // 가장 먼저 반올림을 통해 가장 가까운 노드를 찾는다.
        public async Task<List<Vector2>> FindPath(Vector2 startPos, Vector2 targetPos)
        {
            // 리스트 초기화
            _openList.Clear();
            _closedList.Clear();

            Grid2D startIndex = ReturnNodeIndex(startPos);
            Grid2D endIndex = ReturnNodeIndex(targetPos);

            Node startNode = ReturnNode(startIndex);
            Node endNode = ReturnNode(endIndex);

            if (startNode == null || endNode == null) return null;

            _openList.Insert(startNode);

            while (_openList.Count > 0)
            {
                await Task.Delay(_awaitDuration);

                // 시작의 경우 제외해줘야함
                Node targetNode = _openList.ReturnMin();

                if (targetNode == endNode) // 목적지와 타겟이 같으면 끝
                {
                    Stack<Node> finalList = new Stack<Node>();

                    Node TargetCurNode = targetNode;
                    while (TargetCurNode != startNode)
                    {
                        finalList.Push(TargetCurNode);
                        TargetCurNode = TargetCurNode.ParentNode;
                    }

                    finalList.Push(startNode);
                    return ConvertNodeToV2(finalList);
                }

                _openList.DeleteMin(); // 해당 그리드 지워줌
                _closedList.Add(targetNode); // 해당 그리드 추가해줌
                _closeListPoints.Add(targetNode.WorldPos); // 해당 그리드 추가해줌

                await Jump(targetNode, endNode); // 이를 통해 OpenList에 노드를 추가한다.
            }

            // 이 경우는 경로를 찾지 못한 상황임
            return null;
        }

        async Task Jump(Node targetNode, Node endNode)
        {
            Dictionary<Way, Node> directions = targetNode.NearNodes;

            await UpdateJumpPoints(await Move(Way.UpStraight, directions[Way.UpStraight], endNode), targetNode, endNode);
            await UpdateJumpPoints(await Move(Way.RightStraight, directions[Way.RightStraight], endNode), targetNode, endNode);
            await UpdateJumpPoints(await Move(Way.DownStraight, directions[Way.DownStraight], endNode), targetNode, endNode);
            await UpdateJumpPoints(await Move(Way.LeftStraight, directions[Way.LeftStraight], endNode), targetNode, endNode);

            await UpdateJumpPoints(await Move(Way.UpLeftDiagonal, directions[Way.UpLeftDiagonal], endNode), targetNode, endNode);
            await UpdateJumpPoints(await Move(Way.UpRightDiagonal, directions[Way.UpRightDiagonal], endNode), targetNode, endNode);
            await UpdateJumpPoints(await Move(Way.DownRightDiagonal, directions[Way.DownRightDiagonal], endNode), targetNode, endNode);
            await UpdateJumpPoints(await Move(Way.DownLeftDiagonal, directions[Way.DownLeftDiagonal], endNode), targetNode, endNode);
        }

        // openList에 넣을 때 F G H 계산 필요
        // ParentNode 추가 필요

        float GetDistance(Vector2 a, Vector2 b) { return MathF.Abs(a.x - b.x) + MathF.Abs(a.y - b.y); }

        async Task UpdateJumpPoints(Node jumpEnd, Node jumpStart, Node endNode)
        {
            await Task.Delay(_awaitDuration);

            if (jumpEnd == null) return;

            if (_closedList.Contains(jumpEnd) == true) return;

            if (_openList.Contain(jumpEnd))
            {
                float distance = GetDistance(jumpEnd.WorldPos, jumpStart.WorldPos);

                if (jumpEnd.G > jumpStart.G + distance)
                {
                    jumpEnd.ParentNode = jumpStart;
                    jumpEnd.G = jumpStart.G + distance;

                }
                return;

            }
            else
            {
                jumpEnd.ParentNode = jumpStart;
                jumpEnd.G = jumpStart.G + GetDistance(jumpEnd.WorldPos, jumpStart.WorldPos);
                jumpEnd.H = GetDistance(jumpEnd.WorldPos, endNode.WorldPos); // update distance

                _openList.Insert(jumpEnd);
                _openListPoints.Add(jumpEnd.WorldPos);
            }
        }

        async Task<Node> MoveUpStraight(Node node, Node endNode)
        {
            if (node == null || node.Block == true) return null;

            Node originNode = node;
            _passListPoints.Add(node.WorldPos);

            // 위쪽 경로가 있고 Block이 아닌 경우
            while (true)
            {
                await Task.Delay(_awaitDuration);

                if (node == endNode) return node; // 목표 지점에 도달한 경우

                bool haveLeftBlockNode = node.NearNodes.ContainsKey(Way.LeftStraight) == true && node.NearNodes[Way.LeftStraight].Block == true;
                bool haveUpperLeftPassNode = node.NearNodes.ContainsKey(Way.UpLeftDiagonal) == true && node.NearNodes[Way.UpLeftDiagonal].Block == false;

                if (haveLeftBlockNode && haveUpperLeftPassNode) return node;

                bool haveRightBlockNode = node.NearNodes.ContainsKey(Way.RightStraight) == true && node.NearNodes[Way.RightStraight].Block == true;
                bool haveUpperRightPassNode = node.NearNodes.ContainsKey(Way.UpRightDiagonal) == true && node.NearNodes[Way.UpRightDiagonal].Block == false;

                if (haveRightBlockNode && haveUpperRightPassNode) return node;

                if (node.NearNodes.ContainsKey(Way.UpStraight) == false || node.NearNodes[Way.UpStraight].Block == true) return null;

                node = node.NearNodes[Way.UpStraight]; // 위 조건을 만족하지 않으면 그대로 진행
                _passListPoints.Add(node.WorldPos);
            }
        }

        async Task<Node> MoveDownStraight(Node node, Node endNode)
        {
            if (node == null || node.Block == true) return null;

            Node originNode = node;
            _passListPoints.Add(node.WorldPos);

            // 아래쪽 경로가 있고 Block이 아닌 경우
            while (true)
            {
                await Task.Delay(_awaitDuration);

                if (node == endNode) return node; // 목표 지점에 도달한 경우

                bool haveLeftBlockNode = node.NearNodes.ContainsKey(Way.LeftStraight) == true && node.NearNodes[Way.LeftStraight].Block == true;
                bool haveDownLeftPassNode = node.NearNodes.ContainsKey(Way.DownLeftDiagonal) == true && node.NearNodes[Way.DownLeftDiagonal].Block == false;

                if (haveLeftBlockNode && haveDownLeftPassNode) return node;

                bool haveRightBlockNode = node.NearNodes.ContainsKey(Way.RightStraight) == true && node.NearNodes[Way.RightStraight].Block == true;
                bool haveDownRightPassNode = node.NearNodes.ContainsKey(Way.DownRightDiagonal) == true && node.NearNodes[Way.DownRightDiagonal].Block == false;

                if (haveRightBlockNode && haveDownRightPassNode) return node;

                if (node.NearNodes.ContainsKey(Way.DownStraight) == false || node.NearNodes[Way.DownStraight].Block == true) return null;

                node = node.NearNodes[Way.DownStraight]; // 위 조건을 만족하지 않으면 그대로 진행
                _passListPoints.Add(node.WorldPos);
            }
        }

        async Task<Node> MoveLeftStraight(Node node, Node endNode)
        {
            if (node == null || node.Block == true) return null;

            Node originNode = node;
            _passListPoints.Add(node.WorldPos);

            // 왼쪽 경로가 있고 Block이 아닌 경우
            while (true)
            {
                await Task.Delay(_awaitDuration);

                if (node == endNode) return node; // 목표 지점에 도달한 경우

                bool haveUpBlockNode = node.NearNodes.ContainsKey(Way.UpStraight) == true && node.NearNodes[Way.UpStraight].Block == true;
                bool haveUpLeftPassNode = node.NearNodes.ContainsKey(Way.UpLeftDiagonal) == true && node.NearNodes[Way.UpLeftDiagonal].Block == false;

                if (haveUpBlockNode && haveUpLeftPassNode) return node;

                bool haveDownBlockNode = node.NearNodes.ContainsKey(Way.DownStraight) == true && node.NearNodes[Way.DownStraight].Block == true;
                bool haveDownLeftPassNode = node.NearNodes.ContainsKey(Way.DownLeftDiagonal) == true && node.NearNodes[Way.DownLeftDiagonal].Block == false;

                if (haveDownBlockNode && haveDownLeftPassNode) return node;

                if (node.NearNodes.ContainsKey(Way.LeftStraight) == false || node.NearNodes[Way.LeftStraight].Block == true) return null;

                node = node.NearNodes[Way.LeftStraight]; // 위 조건을 만족하지 않으면 그대로 진행
                _passListPoints.Add(node.WorldPos);
            }
        }

        async Task<Node> MoveRightStraight(Node node, Node endNode)
        {
            if (node == null || node.Block == true) return null;

            Node originNode = node;
            _passListPoints.Add(node.WorldPos);

            // 오른쪽 경로가 있고 Block이 아닌 경우
            while (true)
            {
                await Task.Delay(_awaitDuration);

                if (node == endNode) return node; // 목표 지점에 도달한 경우

                bool haveUpBlockNode = node.NearNodes.ContainsKey(Way.UpStraight) == true && node.NearNodes[Way.UpStraight].Block == true;
                bool haveUpRightPassNode = node.NearNodes.ContainsKey(Way.UpRightDiagonal) == true && node.NearNodes[Way.UpRightDiagonal].Block == false;

                if (haveUpBlockNode && haveUpRightPassNode) return node;

                bool haveDownBlockNode = node.NearNodes.ContainsKey(Way.DownStraight) == true && node.NearNodes[Way.DownStraight].Block == true;
                bool haveDownRightPassNode = node.NearNodes.ContainsKey(Way.DownRightDiagonal) == true && node.NearNodes[Way.DownRightDiagonal].Block == false;

                if (haveDownBlockNode && haveDownRightPassNode) return node;

                if (node.NearNodes.ContainsKey(Way.RightStraight) == false || node.NearNodes[Way.RightStraight].Block == true) return null;

                node = node.NearNodes[Way.RightStraight]; // 위 조건을 만족하지 않으면 그대로 진행
                _passListPoints.Add(node.WorldPos);
            }
        }

        async Task<Node> MoveUpLeftDiagonal(Node node, Node endNode)
        {
            if (node == null || node.Block == true) return null;

            _passListPoints.Add(node.WorldPos);

            // 현재 노드의 Block을 보는게 아니기 때문에 수정 필요함
            // 왼쪽 위 경로가 있고 Block이 아닌 경우
            while (true)
            {
                await Task.Delay(_awaitDuration);
                // 열린 노드 추가

                if (node == endNode) return node; // 목표 지점에 도달한 경우

                bool haveRightBlockNode = node.NearNodes.ContainsKey(Way.RightStraight) == true && node.NearNodes[Way.RightStraight].Block == true;
                bool haveUpperRightPassNode = node.NearNodes.ContainsKey(Way.UpRightDiagonal) == true && node.NearNodes[Way.UpRightDiagonal].Block == false;

                if (haveRightBlockNode && haveUpperRightPassNode) return node;

                bool haveDownBlockNode = node.NearNodes.ContainsKey(Way.DownStraight) == true && node.NearNodes[Way.DownStraight].Block == true;
                bool haveDownLeftPassNode = node.NearNodes.ContainsKey(Way.DownLeftDiagonal) == true && node.NearNodes[Way.DownLeftDiagonal].Block == false;

                if (haveDownBlockNode && haveDownLeftPassNode) return node;

                Node leftNode;
                leftNode = await MoveLeftStraight(node, endNode);

                if (leftNode != null && _closedList.Contains(leftNode) == false && _openList.Contain(leftNode) == false) return node;

                Node upNode;
                upNode = await MoveUpStraight(node, endNode);

                if (upNode != null && _closedList.Contains(upNode) == false && _openList.Contain(upNode) == false) return node;

                if (node.NearNodes.ContainsKey(Way.UpLeftDiagonal) == false || node.NearNodes[Way.UpLeftDiagonal].Block == true) return null;

                node = node.NearNodes[Way.UpLeftDiagonal]; // 위 조건을 만족하지 않으면 그대로 진행
                _passListPoints.Add(node.WorldPos);
            }
        }

        async Task<Node> MoveUpRightDiagonal(Node node, Node endNode)
        {
            if (node == null || node.Block == true) return null;

            _passListPoints.Add(node.WorldPos);

            // 오른쪽 위 경로가 있고 Block이 아닌 경우
            while (true)
            {
                await Task.Delay(_awaitDuration);

                if (node == endNode) return node; // 목표 지점에 도달한 경우

                // 열린 노드 추가

                bool haveDownBlockNode = node.NearNodes.ContainsKey(Way.DownStraight) == true && node.NearNodes[Way.DownStraight].Block == true;
                bool haveDownRightPassNode = node.NearNodes.ContainsKey(Way.DownRightDiagonal) == true && node.NearNodes[Way.DownRightDiagonal].Block == false;

                if (haveDownBlockNode && haveDownRightPassNode) return node;

                bool haveLeftBlockNode = node.NearNodes.ContainsKey(Way.LeftStraight) == true && node.NearNodes[Way.LeftStraight].Block == true;
                bool haveUpperLeftPassNode = node.NearNodes.ContainsKey(Way.UpLeftDiagonal) == true && node.NearNodes[Way.UpLeftDiagonal].Block == false;

                if (haveLeftBlockNode && haveUpperLeftPassNode) return node;

                Node rightNode;
                rightNode = await MoveRightStraight(node, endNode);
                if (rightNode != null && _closedList.Contains(rightNode) == false && _openList.Contain(rightNode) == false) return node;

                Node upNode;
                upNode = await MoveUpStraight(node, endNode);
                if (upNode != null && _closedList.Contains(upNode) == false && _openList.Contain(upNode) == false) return node;

                if (node.NearNodes.ContainsKey(Way.UpRightDiagonal) == false || node.NearNodes[Way.UpRightDiagonal].Block == true) return null;

                node = node.NearNodes[Way.UpRightDiagonal]; // 위 조건을 만족하지 않으면 그대로 진행
                _passListPoints.Add(node.WorldPos);
            }
        }

        async Task<Node> MoveDownLeftDiagonal(Node node, Node endNode)
        {
            if (node == null || node.Block == true) return null;

            _passListPoints.Add(node.WorldPos);

            // 왼쪽 아래 경로가 있고 Block이 아닌 경우
            while (true)
            {
                await Task.Delay(_awaitDuration);

                if (node == endNode) return node; // 목표 지점에 도달한 경우

                bool haveRightBlockNode = node.NearNodes.ContainsKey(Way.RightStraight) == true && node.NearNodes[Way.RightStraight].Block == true;
                bool haveDownRightPassNode = node.NearNodes.ContainsKey(Way.DownRightDiagonal) == true && node.NearNodes[Way.DownRightDiagonal].Block == false;

                if (haveRightBlockNode && haveDownRightPassNode) return node;

                bool haveUpBlockNode = node.NearNodes.ContainsKey(Way.UpStraight) == true && node.NearNodes[Way.UpStraight].Block == true;
                bool haveUpLeftPassNode = node.NearNodes.ContainsKey(Way.UpLeftDiagonal) == true && node.NearNodes[Way.UpLeftDiagonal].Block == false;

                if (haveUpBlockNode && haveUpLeftPassNode) return node;

                Node leftNode;
                leftNode = await MoveLeftStraight(node, endNode);
                if (leftNode != null && _closedList.Contains(leftNode) == false && _openList.Contain(leftNode) == false) return node;

                Node downNode;
                downNode = await MoveDownStraight(node, endNode);
                if (downNode != null && _closedList.Contains(downNode) == false && _openList.Contain(downNode) == false) return node;

                if (node.NearNodes.ContainsKey(Way.DownLeftDiagonal) == false || node.NearNodes[Way.DownLeftDiagonal].Block == true) return null;

                node = node.NearNodes[Way.DownLeftDiagonal]; // 위 조건을 만족하지 않으면 그대로 진행
                _passListPoints.Add(node.WorldPos);
            }
        }

        async Task<Node> MoveDownRightDiagonal(Node node, Node endNode)
        {
            if (node == null || node.Block == true) return null;

            _passListPoints.Add(node.WorldPos);

            // 왼쪽 아래 경로가 있고 Block이 아닌 경우
            while (true)
            {
                await Task.Delay(_awaitDuration);

                if (node == endNode) return node; // 목표 지점에 도달한 경우

                bool haveLeftBlockNode = node.NearNodes.ContainsKey(Way.LeftStraight) == true && node.NearNodes[Way.LeftStraight].Block == true;
                bool haveDownLeftPassNode = node.NearNodes.ContainsKey(Way.DownLeftDiagonal) == true && node.NearNodes[Way.DownLeftDiagonal].Block == false;

                if (haveLeftBlockNode && haveDownLeftPassNode) return node;


                bool haveUpBlockNode = node.NearNodes.ContainsKey(Way.UpStraight) == true && node.NearNodes[Way.UpStraight].Block == true;
                bool haveUpRightPassNode = node.NearNodes.ContainsKey(Way.UpRightDiagonal) == true && node.NearNodes[Way.UpRightDiagonal].Block == false;

                if (haveUpBlockNode && haveUpRightPassNode) return node;

                Node rightNode;
                rightNode = await MoveRightStraight(node, endNode);
                if (rightNode != null && _closedList.Contains(rightNode) == false && _openList.Contain(rightNode) == false) return node;

                Node downNode;
                downNode = await MoveDownStraight(node, endNode);
                if (downNode != null && _closedList.Contains(downNode) == false && _openList.Contain(downNode) == false) return node;

                if (node.NearNodes.ContainsKey(Way.DownRightDiagonal) == false || node.NearNodes[Way.DownRightDiagonal].Block == true) return null;

                node = node.NearNodes[Way.DownRightDiagonal]; // 위 조건을 만족하지 않으면 그대로 진행
                _passListPoints.Add(node.WorldPos);
            }
        }

        async Task<Node> Move(Way way, Node node, Node endNode)
        {
            switch (way)
            {
                case Way.UpStraight:
                    return await MoveUpStraight(node, endNode);
                case Way.DownStraight:
                    return await MoveDownStraight(node, endNode);
                case Way.LeftStraight:
                    return await MoveLeftStraight(node, endNode);
                case Way.RightStraight:
                    return await MoveRightStraight(node, endNode);
                case Way.UpLeftDiagonal:
                    return await MoveUpLeftDiagonal(node, endNode);
                case Way.UpRightDiagonal:
                    return await MoveUpRightDiagonal(node, endNode);
                case Way.DownLeftDiagonal:
                    return await MoveDownLeftDiagonal(node, endNode);
                case Way.DownRightDiagonal:
                    return await MoveDownRightDiagonal(node, endNode);
                default:
                    return null;
            }
        }

        List<Vector2> _passListPoints = new List<Vector2>();
        List<Vector2> _closeListPoints = new List<Vector2>();
        List<Vector2> _openListPoints = new List<Vector2>();

        void OnDrawGizmos()
        {
            for (int i = 0; i < _passListPoints.Count; i++)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawCube(_passListPoints[i], new Vector2(0.8f, 0.8f));
            }

            for (int i = 0; i < _openListPoints.Count; i++)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawCube(_openListPoints[i], new Vector2(0.8f, 0.8f));
            }

            for (int i = 0; i < _closeListPoints.Count; i++)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawCube(_closeListPoints[i], new Vector2(0.8f, 0.8f));
            }
        }
    }
}