//#define Draw_Progress // Ȱ��ȭ

using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using UnityEngine;




namespace FastJPS
{
    public class FastJPSNoDelay : MonoBehaviour
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

        // ���� ���� �ݿø��� ���� ���� ����� ��带 ã�´�.
        public List<Vector2> FindPath(Vector2 startPos, Vector2 targetPos)
        {
#if Draw_Progress
            _openListPoints.Clear();
            _closeListPoints.Clear();
            _passListPoints.Clear();
#endif

            // ����Ʈ �ʱ�ȭ
            _openList.Clear();
            _closedList.Clear();

            Grid2D startIndex = ReturnNodeIndex(startPos);
            Grid2D endIndex = ReturnNodeIndex(targetPos);

            Node startNode = ReturnNode(startIndex);
            Node endNode = ReturnNode(endIndex);

            if (startNode == null || endNode == null) return null;

            _openList.Insert(startNode);
#if Draw_Progress
            _openListPoints.Add(startNode.WorldPos); // �ش� �׸��� �߰�����
#endif

            while (_openList.Count > 0)
            {
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

#if Draw_Progress
                _closeListPoints.Add(targetNode.WorldPos); // �ش� �׸��� �߰�����
#endif

                Jump(targetNode, endNode); // �̸� ���� OpenList�� ��带 �߰��Ѵ�.
            }

            // �� ���� ��θ� ã�� ���� ��Ȳ��
            return null;
        }

        void Jump(Node targetNode, Node endNode)
        {
            Node[] directions = targetNode.NearNodes;
            bool[] haveDirections = targetNode.HaveNodes;

            for (int i = 0; i < 8; i++)
            {
                if (haveDirections[i] == false || directions[i].Block == true) continue;
                UpdateJumpPoints(Move(i, directions[i], endNode), targetNode, endNode);
            }

            //await UpdateJumpPoints(await Move(0, directions[0], endNode), targetNode, endNode);
            //await UpdateJumpPoints(await Move(1, directions[1], endNode), targetNode, endNode);
            //await UpdateJumpPoints(await Move(2, directions[2], endNode), targetNode, endNode);
            //await UpdateJumpPoints(await Move(3, directions[3], endNode), targetNode, endNode);

            //await UpdateJumpPoints(await Move(4, directions[4], endNode), targetNode, endNode);
            //await UpdateJumpPoints(await Move(5, directions[5], endNode), targetNode, endNode);
            //await UpdateJumpPoints(await Move(6, directions[6], endNode), targetNode, endNode);
            //await UpdateJumpPoints(await Move(7, directions[7], endNode), targetNode, endNode);
        }

        // openList�� ���� �� F G H ��� �ʿ�
        // ParentNode �߰� �ʿ�

        float GetDistance(Vector2 a, Vector2 b) { return MathF.Abs(a.x - b.x) + MathF.Abs(a.y - b.y); }

        void UpdateJumpPoints(Node jumpEnd, Node jumpStart, Node endNode)
        {
            if (jumpEnd == null) return;

            if (_closedList.Contains(jumpEnd) == true) return;

            if (_openList.Contains(jumpEnd))
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

#if Draw_Progress
                _openListPoints.Add(jumpEnd.WorldPos);
#endif
            }
        }

        Node MoveUpStraight(Node node, Node endNode)
        {
#if Draw_Progress
            _passListPoints.Add(node.WorldPos);
#endif
            // ���� ��ΰ� �ְ� Block�� �ƴ� ���
            while (true)
            {
                if (node == endNode) return node; // ��ǥ ������ ������ ���

                bool haveLeftBlockNode = node.HaveNodes[3] == true && node.NearNodes[3].Block == true;
                bool haveUpperLeftPassNode = node.HaveNodes[4] == true && node.NearNodes[4].Block == false;

                if (haveLeftBlockNode && haveUpperLeftPassNode) return node;

                bool haveRightBlockNode = node.HaveNodes[1] == true && node.NearNodes[1].Block == true;
                bool haveUpperRightPassNode = node.HaveNodes[5] == true && node.NearNodes[5].Block == false;

                if (haveRightBlockNode && haveUpperRightPassNode) return node;

                if (node.HaveNodes[0] == false || node.NearNodes[0].Block == true) return null;

                node = node.NearNodes[0]; // �� ������ �������� ������ �״�� ����
#if Draw_Progress
                _passListPoints.Add(node.WorldPos);
#endif
            }
        }

        Node MoveDownStraight(Node node, Node endNode)
        {
#if Draw_Progress
            _passListPoints.Add(node.WorldPos);
#endif

            // �Ʒ��� ��ΰ� �ְ� Block�� �ƴ� ���
            while (true)
            {
                if (node == endNode) return node; // ��ǥ ������ ������ ���

                bool haveLeftBlockNode = node.HaveNodes[3] == true && node.NearNodes[3].Block == true;
                bool haveDownLeftPassNode = node.HaveNodes[7] == true && node.NearNodes[7].Block == false;

                if (haveLeftBlockNode && haveDownLeftPassNode) return node;

                bool haveRightBlockNode = node.HaveNodes[1] == true && node.NearNodes[1].Block == true;
                bool haveDownRightPassNode = node.HaveNodes[6] == true && node.NearNodes[6].Block == false;

                if (haveRightBlockNode && haveDownRightPassNode) return node;

                if (node.HaveNodes[2] == false || node.NearNodes[2].Block == true) return null;

                node = node.NearNodes[2]; // �� ������ �������� ������ �״�� ����
#if Draw_Progress
                _passListPoints.Add(node.WorldPos);
#endif
            }
        }

        Node MoveLeftStraight(Node node, Node endNode)
        {
#if Draw_Progress
            _passListPoints.Add(node.WorldPos);
#endif

            // ���� ��ΰ� �ְ� Block�� �ƴ� ���
            while (true)
            {
                if (node == endNode) return node; // ��ǥ ������ ������ ���

                bool haveUpBlockNode = node.HaveNodes[0] == true && node.NearNodes[0].Block == true;
                bool haveUpLeftPassNode = node.HaveNodes[4] == true && node.NearNodes[4].Block == false;

                if (haveUpBlockNode && haveUpLeftPassNode) return node;

                bool haveDownBlockNode = node.HaveNodes[2] == true && node.NearNodes[2].Block == true;
                bool haveDownLeftPassNode = node.HaveNodes[7] == true && node.NearNodes[7].Block == false;

                if (haveDownBlockNode && haveDownLeftPassNode) return node;

                if (node.HaveNodes[3] == false || node.NearNodes[3].Block == true) return null;

                node = node.NearNodes[3]; // �� ������ �������� ������ �״�� ����
#if Draw_Progress
                _passListPoints.Add(node.WorldPos);
#endif
            }
        }

        Node MoveRightStraight(Node node, Node endNode)
        {
#if Draw_Progress
            _passListPoints.Add(node.WorldPos);
#endif

            // ������ ��ΰ� �ְ� Block�� �ƴ� ���
            while (true)
            {
                if (node == endNode) return node; // ��ǥ ������ ������ ���

                bool haveUpBlockNode = node.HaveNodes[0] == true && node.NearNodes[0].Block == true;
                bool haveUpRightPassNode = node.HaveNodes[5] == true && node.NearNodes[5].Block == false;

                if (haveUpBlockNode && haveUpRightPassNode) return node;

                bool haveDownBlockNode = node.HaveNodes[2] == true && node.NearNodes[2].Block == true;
                bool haveDownRightPassNode = node.HaveNodes[6] == true && node.NearNodes[6].Block == false;

                if (haveDownBlockNode && haveDownRightPassNode) return node;

                if (node.HaveNodes[1] == false || node.NearNodes[1].Block == true) return null;

                node = node.NearNodes[1]; // �� ������ �������� ������ �״�� ����
#if Draw_Progress
                _passListPoints.Add(node.WorldPos);
#endif
            }
        }

        Node MoveUpLeftDiagonal(Node node, Node endNode)
        {
#if Draw_Progress
            _passListPoints.Add(node.WorldPos);
#endif

            // ���� ����� Block�� ���°� �ƴϱ� ������ ���� �ʿ���
            // ���� �� ��ΰ� �ְ� Block�� �ƴ� ���
            while (true)
            {
                if (node == endNode) return node; // ��ǥ ������ ������ ���

                bool haveRightBlockNode = node.HaveNodes[1] == true && node.NearNodes[1].Block == true;
                bool haveUpperRightPassNode = node.HaveNodes[5] == true && node.NearNodes[5].Block == false;

                if (haveRightBlockNode && haveUpperRightPassNode) return node;

                bool haveDownBlockNode = node.HaveNodes[2] == true && node.NearNodes[2].Block == true;
                bool haveDownLeftPassNode = node.HaveNodes[7] == true && node.NearNodes[7].Block == false;

                if (haveDownBlockNode && haveDownLeftPassNode) return node;

                Node leftNode;
                leftNode = MoveLeftStraight(node, endNode);
                if (leftNode != null) return node;

                Node upNode;
                upNode = MoveUpStraight(node, endNode);

                if (upNode != null) return node;

                if (node.HaveNodes[4] == false || node.NearNodes[4].Block == true) return null;

                node = node.NearNodes[4]; // �� ������ �������� ������ �״�� ����
#if Draw_Progress
                _passListPoints.Add(node.WorldPos);
#endif
            }
        }

        Node MoveUpRightDiagonal(Node node, Node endNode)
        {
#if Draw_Progress
            _passListPoints.Add(node.WorldPos);
#endif

            // ������ �� ��ΰ� �ְ� Block�� �ƴ� ���
            while (true)
            {
                if (node == endNode) return node; // ��ǥ ������ ������ ���

                // ���� ��� �߰�

                bool haveDownBlockNode = node.HaveNodes[2] == true && node.NearNodes[2].Block == true;
                bool haveDownRightPassNode = node.HaveNodes[6] == true && node.NearNodes[6].Block == false;

                if (haveDownBlockNode && haveDownRightPassNode) return node;

                bool haveLeftBlockNode = node.HaveNodes[3] == true && node.NearNodes[3].Block == true;
                bool haveUpperLeftPassNode = node.HaveNodes[4] == true && node.NearNodes[4].Block == false;

                if (haveLeftBlockNode && haveUpperLeftPassNode) return node;

                Node rightNode;
                rightNode = MoveRightStraight(node, endNode);
                if (rightNode != null) return node;

                Node upNode;
                upNode = MoveUpStraight(node, endNode);
                if (upNode != null) return node;

                if (node.HaveNodes[5] == false || node.NearNodes[5].Block == true) return null;

                node = node.NearNodes[5]; // �� ������ �������� ������ �״�� ����
#if Draw_Progress
                _passListPoints.Add(node.WorldPos);
#endif
            }
        }

        Node MoveDownLeftDiagonal(Node node, Node endNode)
        {
#if Draw_Progress
            _passListPoints.Add(node.WorldPos);
#endif

            // ���� �Ʒ� ��ΰ� �ְ� Block�� �ƴ� ���
            while (true)
            {
                if (node == endNode) return node; // ��ǥ ������ ������ ���

                bool haveRightBlockNode = node.HaveNodes[1] == true && node.NearNodes[1].Block == true;
                bool haveDownRightPassNode = node.HaveNodes[6] == true && node.NearNodes[6].Block == false;

                if (haveRightBlockNode && haveDownRightPassNode) return node;

                bool haveUpBlockNode = node.HaveNodes[0] == true && node.NearNodes[0].Block == true;
                bool haveUpLeftPassNode = node.HaveNodes[4] == true && node.NearNodes[4].Block == false;

                if (haveUpBlockNode && haveUpLeftPassNode) return node;

                Node leftNode;
                leftNode = MoveLeftStraight(node, endNode);
                if (leftNode != null) return node;

                Node downNode;
                downNode = MoveDownStraight(node, endNode);
                if (downNode != null) return node;

                if (node.HaveNodes[7] == false || node.NearNodes[7].Block == true) return null;

                node = node.NearNodes[7]; // �� ������ �������� ������ �״�� ����
#if Draw_Progress
                _passListPoints.Add(node.WorldPos);
#endif
            }
        }

        Node MoveDownRightDiagonal(Node node, Node endNode)
        {
#if Draw_Progress
            _passListPoints.Add(node.WorldPos);
#endif

            // ���� �Ʒ� ��ΰ� �ְ� Block�� �ƴ� ���
            while (true)
            {
                if (node == endNode) return node; // ��ǥ ������ ������ ���

                bool haveLeftBlockNode = node.HaveNodes[3] == true && node.NearNodes[3].Block == true;
                bool haveDownLeftPassNode = node.HaveNodes[7] == true && node.NearNodes[7].Block == false;

                if (haveLeftBlockNode && haveDownLeftPassNode) return node;


                bool haveUpBlockNode = node.HaveNodes[0] == true && node.NearNodes[0].Block == true;
                bool haveUpRightPassNode = node.HaveNodes[5] == true && node.NearNodes[5].Block == false;

                if (haveUpBlockNode && haveUpRightPassNode) return node;

                Node rightNode;
                rightNode = MoveRightStraight(node, endNode);
                if (rightNode != null) return node;

                Node downNode;
                downNode = MoveDownStraight(node, endNode);
                if (downNode != null) return node;

                if (node.HaveNodes[6] == false || node.NearNodes[6].Block == true) return null;

                node = node.NearNodes[6]; // �� ������ �������� ������ �״�� ����
#if Draw_Progress
                _passListPoints.Add(node.WorldPos);
#endif
            }
        }

        Node Move(int way, Node node, Node endNode)
        {
            switch (way)
            {
                case 0:
                    return MoveUpStraight(node, endNode);
                case 1:
                    return MoveRightStraight(node, endNode);
                case 2:
                    return MoveDownStraight(node, endNode);
                case 3:
                    return MoveLeftStraight(node, endNode);
                case 4:
                    return MoveUpLeftDiagonal(node, endNode);
                case 5:
                    return MoveUpRightDiagonal(node, endNode);
                case 6:
                    return MoveDownRightDiagonal(node, endNode);
                case 7:
                    return MoveDownLeftDiagonal(node, endNode);
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
