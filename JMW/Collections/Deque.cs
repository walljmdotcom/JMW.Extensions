using System;
using System.Collections;
using System.Collections.Generic;

namespace JMW.Collections;

/// <summary>
/// An indexed list in a circular buffer. Adding or removing at either end is O(1), and inserting or removing in the
/// middle moves only the shorter side, where <see cref="List{T}"/> always moves everything after the position.
/// </summary>
internal sealed class Deque<T> : IReadOnlyList<T>
{
    private T[] items = [];
    private int head;
    private int count;
    private int version;

    public int Count => count;

    public T this[int index]
    {
        get
        {
            CheckIndex(index);
            return items[Physical(index)];
        }

        set
        {
            CheckIndex(index);
            items[Physical(index)] = value;
            version++;
        }
    }

    public void Add(T item)
    {
        EnsureRoom();
        items[Physical(count)] = item;
        count++;
        version++;
    }

    public void Insert(int index, T item)
    {
        if ((uint)index > (uint)count)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be within the bounds of the list.");

        EnsureRoom();
        if (index < count / 2)
        {
            // open a slot at the front and move the first index items down into it
            head = head == 0 ? items.Length - 1 : head - 1;
            for (var i = 0; i < index; i++)
                items[Physical(i)] = items[Physical(i + 1)];
        }
        else
        {
            for (var i = count; i > index; i--)
                items[Physical(i)] = items[Physical(i - 1)];
        }

        items[Physical(index)] = item;
        count++;
        version++;
    }

    public void RemoveAt(int index)
    {
        CheckIndex(index);
        if (index < count / 2)
        {
            for (var i = index; i > 0; i--)
                items[Physical(i)] = items[Physical(i - 1)];

            // cleared so the buffer does not keep a removed item alive
            Array.Clear(items, head, 1);
            head = head + 1 == items.Length ? 0 : head + 1;
        }
        else
        {
            for (var i = index; i < count - 1; i++)
                items[Physical(i)] = items[Physical(i + 1)];

            Array.Clear(items, Physical(count - 1), 1);
        }

        count--;
        version++;
    }

    /// <summary>
    /// Drops every item from <paramref name="newCount"/> to the end.
    /// </summary>
    public void Truncate(int newCount)
    {
        if ((uint)newCount > (uint)count)
            throw new ArgumentOutOfRangeException(nameof(newCount), newCount, "The new count must be within the list.");

        for (var i = newCount; i < count; i++)
            Array.Clear(items, Physical(i), 1);

        count = newCount;
        version++;
    }

    public void Clear()
    {
        Array.Clear(items, 0, items.Length);
        head = 0;
        count = 0;
        version++;
    }

    /// <summary>
    /// Position of <paramref name="item"/> by reference, scanning inward from both ends, so items near either end
    /// are found at once. -1 if absent.
    /// </summary>
    public int IndexOfReference(T item)
    {
        for (int lo = 0, hi = count - 1; lo <= hi; lo++, hi--)
        {
            if (ReferenceEquals(items[Physical(lo)], item)) return lo;
            if (ReferenceEquals(items[Physical(hi)], item)) return hi;
        }

        return -1;
    }

    /// <summary>
    /// Position of the first item equal to <paramref name="item"/> by <see cref="EqualityComparer{T}.Default"/>, the
    /// same rule as <see cref="List{T}.IndexOf(T)"/>. -1 if absent.
    /// </summary>
    public int IndexOf(T item)
    {
        var comparer = EqualityComparer<T>.Default;
        for (var i = 0; i < count; i++)
        {
            if (comparer.Equals(items[Physical(i)], item)) return i;
        }

        return -1;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        if (array is null) throw new ArgumentNullException(nameof(array));
        if (arrayIndex < 0 || arrayIndex > array.Length - count)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex), arrayIndex, "The array is too small.");

        var firstPart = Math.Min(count, items.Length - head);
        Array.Copy(items, head, array, arrayIndex, firstPart);
        Array.Copy(items, 0, array, arrayIndex + firstPart, count - firstPart);
    }

    public Enumerator GetEnumerator() => new(this);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private int Physical(int index)
    {
        var p = head + index;
        return p >= items.Length ? p - items.Length : p;
    }

    private void CheckIndex(int index)
    {
        if ((uint)index >= (uint)count)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be within the bounds of the list.");
    }

    private void EnsureRoom()
    {
        if (count < items.Length)
            return;

        var grown = new T[items.Length == 0 ? 4 : items.Length * 2];
        CopyTo(grown, 0);
        items = grown;
        head = 0;
    }

    public struct Enumerator : IEnumerator<T>
    {
        private readonly Deque<T> deque;
        private readonly int version;
        private int index;

        internal Enumerator(Deque<T> deque)
        {
            this.deque = deque;
            version = deque.version;
            index = -1;
        }

        public readonly T Current => deque.items[deque.Physical(index)];

        readonly object? IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (version != deque.version)
                throw new InvalidOperationException("The collection was modified while it was being enumerated.");

            if (index + 1 >= deque.count)
            {
                index = deque.count;
                return false;
            }

            index++;
            return true;
        }

        public void Reset()
        {
            if (version != deque.version)
                throw new InvalidOperationException("The collection was modified while it was being enumerated.");

            index = -1;
        }

        public readonly void Dispose()
        {
        }
    }
}
