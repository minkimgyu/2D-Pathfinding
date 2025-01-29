using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Dijkstra
{
    public interface INode<T> : IComparable<T>
    {
    }

    public class Heap<T> where T : INode<T>
    {
        public Heap(int maxItemCount)
        {
            _items = new T[maxItemCount];
            _count = 0;
        }

        int GetLastIndexHavingChild()
        {
            return (_count - 2) / 2;
        }

        int GetParentIndex(int index)
        {
            return (index - 1) / 2;
        }

        int GetChild(int index, bool isLeft = true)
        {
            if (isLeft) return 2 * index + 1;
            else return 2 * index + 2;
        }

        void Swap(int index1, int index2)
        {
            T tmp = _items[index2];
            _items[index2] = _items[index1];
            _items[index1] = tmp;
        }


        void PercolateUp(int index)
        {
            int currentIndex = index;
            int parentIndex = GetParentIndex(index);

            while (currentIndex > 0 && _items[parentIndex].CompareTo(_items[currentIndex]) > 0)
            {
                Swap(parentIndex, currentIndex);

                currentIndex = parentIndex;
                parentIndex = GetParentIndex(parentIndex);
            }
        }

        void PercolateDown(int index)
        {
            int currentIndex = index;
            int childIndex = ReturnMinIndexInChildren(index);

            while (childIndex < _count && _items[childIndex].CompareTo(_items[currentIndex]) < 0)
            {
                Swap(childIndex, currentIndex);

                if (childIndex > GetLastIndexHavingChild()) break; // �ڽ��� �������� �ʴ� ��� Ż��

                currentIndex = childIndex;
                childIndex = ReturnMinIndexInChildren(childIndex); // ���� ���ֱ�
            }
        }

        int ReturnMinIndexInChildren(int index)
        {
            int leftChildIndex = GetChild(index);
            int rightChildIndex = GetChild(index, false);

            T leftChild = _items[leftChildIndex];
            T rightChild = _items[rightChildIndex];

            // right�� �� ���� ���
            if (rightChild != null && rightChild.CompareTo(leftChild) < 0) return rightChildIndex;
            return leftChildIndex;
        }

        public void Clear()
        {
            Array.Clear(_items, 0, Count);
            _count = 0;
        }

        public void Insert(T item)
        {
            _items[_count] = item;
            PercolateUp(_count);
            _count++;
        }

        public void DeleteMin()
        {
            if (_count == 0) return;
            else if (_count == 1)
            {
                _count--;
                return;
            }

            T lastItem = _items[_count - 1];
            _count--;
            _items[0] = lastItem;

            PercolateDown(0);
        }

        public T ReturnMin() { return _items[0]; }

        T[] _items;
        int _count = 0;
        public int Count { get { return _count; } }
    }
}