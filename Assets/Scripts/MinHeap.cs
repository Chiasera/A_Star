using System;
using System.Collections.Generic;
using System.Diagnostics;

public class MinHeap<T> where T : IComparable<T>
{
    private List<T> heap = new List<T>();

    public int Count => heap.Count;

    public void Add(T value)
    {
        heap.Add(value);
        HeapifyUp(heap.Count - 1);
    }

    public T ExtractMin()
    {
        if (heap.Count == 0) throw new InvalidOperationException("Heap is empty");
        T min = heap[0];
        heap[0] = heap[heap.Count - 1];
        heap.RemoveAt(heap.Count - 1);
        HeapifyDown(0);
        return min;
    }

    public void Update(T value)
    {
        int index = heap.IndexOf(value);
        if (index == -1)
        {
            UnityEngine.Debug.Log("ITEM HAS BEEN REMOVED FROM HEAP, IGNORING...");
            return;
        }
        HeapifyUp(index);
        HeapifyDown(index);
    }

    public T Peek()
    {
        if (heap.Count == 0) throw new InvalidOperationException("Heap is empty");
        return heap[0];
    }

    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (heap[index].CompareTo(heap[parent]) >= 0) break;
            Swap(index, parent);
            index = parent;
        }
    }

    private void HeapifyDown(int index)
    {
        int smallest = index;
        int leftChild = index * 2 + 1;
        int rightChild = index * 2 + 2;

        if (leftChild < heap.Count && heap[leftChild].CompareTo(heap[smallest]) < 0)
        {
            smallest = leftChild;
        }

        if (rightChild < heap.Count && heap[rightChild].CompareTo(heap[smallest]) < 0)
        {
            smallest = rightChild;
        }

        if (smallest != index)
        {
            Swap(index, smallest);
            HeapifyDown(smallest);
        }
    }

    private void Swap(int first, int second)
    {
        T temp = heap[first];
        heap[first] = heap[second];
        heap[second] = temp;
    }
}
