/*
The MIT License (MIT)
Copyright (c) 2016 Jason Wall

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace JMW.Collections;

public class RingBuffer<T> : IEnumerable<T>
{
    private readonly T?[] internalArray;
    private int index;

    public RingBuffer(int size)
    {
        internalArray = new T[size];
        index = 0;
        Count = 0;
    }

    public RingBuffer(int size, bool reverse) : this(size)
    {
        ReverseIteration = reverse;
    }

    public RingBuffer(T[] array)
    {
        internalArray = new T[array.Length];
        Array.Copy(array, internalArray, array.Length);
        index = 0;
        Count = array.Length;
    }

    public RingBuffer(T[] array, bool reverse) : this(array)
    {
        ReverseIteration = reverse;
    }

    public RingBuffer(int size, T[] array)
    {
        if (size < array.Length)
            size = array.Length;

        internalArray = new T[size];
        Array.Copy(array, internalArray, array.Length);
        index = size == 0 ? 0 : array.Length % size;
        Count = array.Length;
    }

    public RingBuffer(int size, T[] array, bool reverse) : this(size, array)
    {
        ReverseIteration = reverse;
    }

    public int Count { get; private set; }

    public bool IsReadOnly => false;
    public bool ReverseIteration { get; set; } = false;

    public IEnumerator<T> GetEnumerator()
    {
        return ReverseIteration
            ? new RingReverseEnumerator<T>(index, internalArray, Count)
            : new RingEnumerator<T>(index, internalArray, Count);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(T item)
    {
        // a zero-capacity buffer holds nothing
        if (internalArray.Length == 0)
            return;

        internalArray[index] = item;

        Count = Count >= internalArray.Length ? internalArray.Length : ++Count;
        index = index < internalArray.Length - 1 ? ++index : 0;
    }

    /// <summary>
    /// The most recently added item, or the oldest item when <see cref="ReverseIteration"/> is set.
    /// </summary>
    public T Last()
    {
        if (Count == 0)
            throw new InvalidOperationException("The buffer is empty.");

        return ReverseIteration ? this[0] : this[Count - 1];
    }

    /// <summary>
    /// The oldest item in the buffer.
    /// </summary>
    public T First()
    {
        if (Count == 0)
            throw new InvalidOperationException("The buffer is empty.");

        return this[0];
    }

    public void Clear()
    {
        for (var i = 0; i < internalArray.Length; i++)
            internalArray[i] = default;

        Count = 0;
        index = 0;
    }

    public bool Contains(T item)
    {
        var comparer = EqualityComparer<T?>.Default;
        for (var i = 0; i < Count; i++)
        {
            if (comparer.Equals(internalArray[slot(i)], item))
                return true;
        }

        return false;
    }

    public T[] ToArray()
    {
        var arr = new T[Count];

        var j = 0;
        foreach (var i in this)
        {
            arr[j++] = i;
        }

        return arr;
    }

    public List<T> ToList()
    {
        return new List<T>(this);
    }

    /// <summary>
    /// Maps a position counted from the oldest item to its slot in the internal array.
    /// </summary>
    private int slot(int position)
    {
        // the oldest item sits Count slots behind the next write position
        var oldest = index - Count;
        if (oldest < 0)
            oldest += internalArray.Length;

        var idx = oldest + position;
        return idx < internalArray.Length ? idx : idx - internalArray.Length;
    }

    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= Count)
                throw new IndexOutOfRangeException("Index: " + index + " is not between 0 and " + (Count - 1));

            return internalArray[slot(index)] ?? throw new InvalidOperationException();
        }

        set
        {
            if (index < 0 || index >= Count)
                throw new IndexOutOfRangeException("Index: " + index + " is not between 0 and " + (Count - 1));

            internalArray[slot(index)] = value;
        }
    }
}

public class RingReverseEnumerator<T> : IEnumerator<T>
{
    private int count = 0;
    private int index;
    private readonly int origin;
    private readonly int length;
    private readonly T?[] internalArray;

    public RingReverseEnumerator(int idx, T?[] arr, int length)
    {
        index = idx; origin = idx;
        internalArray = arr;
        this.length = length;
    }

    public bool MoveNext()
    {
        count++;
        if (index > 0)
            index--;
        else
            index = length - 1;
        return count <= length;
    }

    public void Reset()
    {
        index = origin;
        count = 0;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    T IEnumerator<T>.Current => internalArray[index] ?? throw new InvalidOperationException();
    public object Current => internalArray[index] ?? throw new InvalidOperationException();
}

public class RingEnumerator<T> : IEnumerator<T>
{
    private int count = 0;
    private int index;
    private readonly int origin;
    private readonly int length;
    private readonly T?[] internalArray;

    public RingEnumerator(int idx, T?[] arr, int length)
    {
        index = idx - 1; origin = idx - 1;
        internalArray = arr;
        this.length = length;
    }

    public bool MoveNext()
    {
        count++;
        if (index < length - 1)
            index++;
        else
            index = 0;

        return count <= length;
    }

    public void Reset()
    {
        index = origin;
        count = 0;
    }

    T IEnumerator<T>.Current => internalArray[index] ?? throw new InvalidOperationException();
    public object Current => internalArray[index] ?? throw new InvalidOperationException();

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}