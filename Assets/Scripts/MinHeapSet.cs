using System.Collections.Generic;
using System;

public class MinHeapSet<T> where T : IComparable<T>
{
    private HashSet<T> set = new HashSet<T>();
    private SortedSet<T> heap = new SortedSet<T>();

    public bool Contains(T item)
    {
        // O(1) time complexity
        return set.Contains(item);
    }

    public bool Add(T item)
    {
        // O(log n) for the SortedSet and O(1) for the HashSet
        if (set.Add(item))
        {
            heap.Add(item);
            return true;
        }
        return false;
    }

    public bool Remove(T item)
    {
        // O(log n) for the SortedSet and O(1) for the HashSet
        if (set.Remove(item))
        {
            heap.Remove(item);
            return true;
        }
        return false;
    }

    public T ExtractMin()
    {
        T minItem = heap.Min;
        heap.Remove(minItem);
        set.Remove(minItem);
        return minItem;
    }

    public int Count => set.Count;
}
