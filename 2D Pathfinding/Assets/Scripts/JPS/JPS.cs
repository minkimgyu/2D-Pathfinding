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

        // ���� ���� �ݿø��� ���� ���� ����� ��带 ã�´�.
        public async Task<List<Vector2>> FindPath(Vector2 startPos, Vector2 targetPos)
        {
            // ����Ʈ �ʱ�ȭ
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

                // ������ ��� �����������
                Node targetNode = _openList.ReturnMin();

                if (targetNode == endNode) // �������� Ÿ���� ������ ��
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

                _openList.DeleteMin(); // �ش� �׸��� ������
                _closedList.Add(targetNode); // �ش� �׸��� �߰�����
                _closeListPoints.Add(targetNode.WorldPos); // �ش� �׸��� �߰�����

                await Jump(targetNode, endNode); // �̸� ���� OpenList�� ��带 �߰��Ѵ�.
            }

            // �� ���� ��θ� ã�� ���� ��Ȳ��
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

        // openList�� ���� �� F G H ��� �ʿ�
        // ParentNode �߰� �ʿ�

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

            // ���� ��ΰ� �ְ� Block�� �ƴ� ���
            while (true)
            {
                await Task.Delay(_awaitDuration);

                if (node == endNode) return node; // ��ǥ ������ ������ ���

                bool haveLeftBlockNode = node.NearNodes.ContainsKey(Way.LeftStraight) == true && node.NearNodes[Way.LeftStraight].Block == true;
                bool haveUpperLeftPassNode = node.NearNodes.ContainsKey(Way.UpLeftDiagonal) == true && node.NearNodes[Way.UpLeftDiagonal].Block == false;

                if (haveLeftBlockNode && haveUpperLeftPassNode) return node;

                bool haveRightBlockNode = node.NearNodes.ContainsKey(Way.RightStraight) == true && node.NearNodes[Way.RightStraight].Block == true;
                bool haveUpperRightPassNode = node.NearNodes.ContainsKey(Way.UpRightDiagonal) == true && node.NearNodes[Way.UpRightDiagonal].Block == false;

                if (haveRightBlockNode && haveUpperRightPassNode) return node;

                if (node.NearNodes.ContainsKey(Way.UpStraight) == false || node.NearNodes[Way.UpStraight].Block == true) return null;

                node = node.NearNodes[Way.UpStraight]; // �� ������ �������� ������ �״�� ����
                _passListPoints.Add(node.WorldPos);
            }
        }

        async Task<Node> MoveDownStraight(Node node, Node endNode)
        {
            if (node == null || node.Block == true) return null;

            Node originNode = node;
            _passListPoints.Add(node.WorldPos);

            // �Ʒ��� ��ΰ� �ְ� Block�� �ƴ� ���
            while (true)
            {
                await Task.Delay(_awaitDuration);

                if (node == endNode) return node; // ��ǥ ������ ������ ���

                bool haveLeftBlockNode = node.NearNodes.ContainsKey(Way.LeftStraight) == true && node.NearNodes[Way.LeftStraight].Block == true;
                bool haveDownLeftPassNode = node.NearNodes.ContainsKey(Way.DownLeftDiagonal) == true && node.NearNodes[Way.DownLeftDiagonal].Block == false;

                if (haveLeftBlockNode && haveDownLeftPassNode) return node;

                bool haveRightBlockNode = node.NearNodes.ContainsKey(Way.RightStraight) == true && node.NearNodes[Way.RightStraight].Block == true;
                bool haveDownRightPassNode = node.NearNodes.ContainsKey(Way.DownRightDiagonal) == true && node.NearNodes[Way.DownRightDiagonal].Block == false;

                if (haveRightBlockNode && haveDownRightPassNode) return node;

                if (node.NearNodes.ContainsKey(Way.DownStraight) == false || node.NearNodes[Way.DownStraight].Block == true) return null;

                node = node.NearNodes[Way.DownStraight]; // �� ������ �������� ������ �״�� ����
                _passListPoints.Add(node.WorldPos);
            }
        }

        async Task<Node> MoveLeftStraight(Node node, Node endNode)
        {
            if (node == null || node.Block == true) return null;

            Node originNode = node;
            _passListPoints.Add(node.WorldPos);

            // ���� ��ΰ� �ְ� Block�� �ƴ� ���
            while (true)
            {
                await Task.Delay(_awaitDuration);

                if (node == endNode) return node; // ��ǥ ������ ������ ���

                bool haveUpBlockNode = node.NearNodes.ContainsKey(Way.UpStraight) == true && node.NearNodes[Way.UpStraight].Block == true;
                bool haveUpLeftPassNode = node.NearNodes.ContainsKey(Way.UpLeftDiagonal) == true && node.NearNodes[Way.UpLeftDiagonal].Block == false;

                if (haveUpBlockNode && haveUpLeftPassNode) return node;

                bool haveDownBlockNode = node.NearNodes.ContainsKey(Way.DownStraight) == true && node.NearNodes[Way.DownStraight].Block == true;
                bool haveDownLeftPassNode = node.NearNodes.ContainsKey(Way.DownLeftDiagonal) == true && node.NearNodes[Way.DownLeftDiagonal].Block == false;

                if (haveDownBlockNode && haveDownLeftPassNode) return node;

                if (node.NearNodes.ContainsKey(Way.LeftStraight) == false || node.NearNodes[Way.LeftStraight].Block == true) return null;

                node = node.NearNodes[Way.LeftStraight]; // �� ������ �������� ������ �״�� ����
                _passListPoints.Add(node.WorldPos);
            }
        }

        async Task<Node> MoveRightStraight(Node node, Node endNode)
        {
            if (node == null || node.Block == true) return null;

            Node originNode = node;
            _passListPoints.Add(node.WorldPos);

            // ������ ��ΰ� �ְ� Block�� �ƴ� ���
            while (true)
            {
                await Task.Delay(_awaitDuration);

                if (node == endNode) return node; // ��ǥ ������ ������ ���

                bool haveUpBlockNode = node.NearNodes.ContainsKey(Way.UpStraight) == true && node.NearNodes[Way.UpStraight].Block == true;
                bool haveUpRightPassNode = node.NearNodes.ContainsKey(Way.UpRightDiagonal) == true && node.NearNodes[Way.UpRightDiagonal].Block == false;

                if (haveUpBlockNode && haveUpRightPassNode) return node;

                bool haveDownBlockNode = node.NearNodes.ContainsKey(Way.DownStraight) == true && node.NearNodes[Way.DownStraight].Block == true;
                bool haveDownRightPassNode = node.NearNodes.ContainsKey(Way.DownRightDiagonal) == true && node.NearNodes[Way.DownRightDiagonal].Block == false;

                if (haveDownBlockNode && haveDownRightPassNode) return node;

                if (node.NearNodes.ContainsKey(Way.RightStraight) == false || node.NearNodes[Way.RightStraight].Block == true) return null;

                node = node.NearNodes[Way.RightStraight]; // �� ������ �������� ������ �״�� ����
                _passListPoints.Add(node.WorldPos);
            }
        }

        async Task<Node> MoveUpLeftDiagonal(Node node, Node endNode)
        {
            if (node == null || node.Block == true) return null;

            _passListPoints.Add(node.WorldPos);

            // ���� ����� Block�� ���°� �ƴϱ� ������ ���� �ʿ���
            // ���� �� ��ΰ� �ְ� Block�� �ƴ� ���
            while (true)
            {
                await Task.Delay(_awaitDuration);
                // ���� ��� �߰�

                if (node == endNode) return node; // ��ǥ ������ ������ ���

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

                node = node.NearNodes[Way.UpLeftDiagonal]; // �� ������ �������� ������ �״�� ����
                _passListPoints.Add(node.WorldPos);
            }
        }

        async Task<Node> MoveUpRightDiagonal(Node node, Node endNode)
        {
            if (node == null || node.Block == true) return null;

            _passListPoints.Add(node.WorldPos);

            // ������ �� ��ΰ� �ְ� Block�� �ƴ� ���
            while (true)
            {
                await Task.Delay(_awaitDuration);

                if (node == endNode) return node; // ��ǥ ������ ������ ���

                // ���� ��� �߰�

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

                node = node.NearNodes[Way.UpRightDiagonal]; // �� ������ �������� ������ �״�� ����
                _passListPoints.Add(node.WorldPos);
            }
        }

        async Task<Node> MoveDownLeftDiagonal(Node node, Node endNode)
        {
            if (node == null || node.Block == true) return null;

            _passListPoints.Add(node.WorldPos);

            // ���� �Ʒ� ��ΰ� �ְ� Block�� �ƴ� ���
            while (true)
            {
                await Task.Delay(_awaitDuration);

                if (node == endNode) return node; // ��ǥ ������ ������ ���

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

                node = node.NearNodes[Way.DownLeftDiagonal]; // �� ������ �������� ������ �״�� ����
                _passListPoints.Add(node.WorldPos);
            }
        }

        async Task<Node> MoveDownRightDiagonal(Node node, Node endNode)
        {
            if (node == null || node.Block == true) return null;

            _passListPoints.Add(node.WorldPos);

            // ���� �Ʒ� ��ΰ� �ְ� Block�� �ƴ� ���
            while (true)
            {
                await Task.Delay(_awaitDuration);

                if (node == endNode) return node; // ��ǥ ������ ������ ���

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

                node = node.NearNodes[Way.DownRightDiagonal]; // �� ������ �������� ������ �״�� ����
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