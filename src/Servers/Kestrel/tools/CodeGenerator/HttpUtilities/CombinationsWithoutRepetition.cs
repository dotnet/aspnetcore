// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace CodeGenerator.HttpUtilities;

// C code for Algorithm L (Lexicographic combinations) in Section 7.2.1.3 of The Art of Computer Programming, Volume 4A: Combinatorial Algorithms, Part 1 :
internal sealed class CombinationsWithoutRepetition<T> : IEnumerator<T[]>
{
    private bool _firstElement;
    private int[] _pointers;
    private T[] _nElements;
    private readonly int _p;

    public CombinationsWithoutRepetition(T[] nElements, int p)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(p, nElements.Length);

        _nElements = nElements;
        _p = p;
        Current = new T[p];
        ResetCurrent();
    }

    public T[] Current { get; private set; }
    object IEnumerator.Current => Current;

    public bool MoveNext()
    {
        if (_firstElement)
        {
            _firstElement = false;
            return true;
        }

        var p = _p;
        var pointers = _pointers;
        var current = Current;
        var nElements = _nElements;
        var index = 1;

        while (pointers[index] + 1 == pointers[index + 1])
        {
            var j1 = index - 1;

            pointers[index] = j1;
            current[j1] = nElements[j1];
            ++index;
        }

        if (index > p)
        {
            return false;
        }

        current[index - 1] = nElements[++pointers[index]];

        return true;
    }

    private void ResetCurrent()
    {
        var p = _p;
        if (_pointers == null)
        {
            _pointers = new int[p + 3];
        }

        var pointers = _pointers;
        var current = Current;
        var nElements = _nElements;

        pointers[0] = 0;
        for (int j = 1; j <= _p; j++)
        {
            pointers[j] = j - 1;
        }
        pointers[_p + 1] = nElements.Length;
        pointers[_p + 2] = 0;

        for (int j = _p; j > 0; j--)
        {
            current[j - 1] = nElements[pointers[j]];
        }
        _firstElement = true;
    }

    public void Reset()
    {
        Array.Clear(Current, 0, Current.Length);
        Current = null;
        ResetCurrent();
    }

    public void Dispose()
    {
        _nElements = null;
        Current = null;
        _pointers = null;
    }
}
