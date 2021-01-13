// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Routing.Tree
{
    internal struct TreeEnumerator : IEnumerator<UrlMatchingNode>
    {
        private readonly Stack<UrlMatchingNode> _stack;
        private readonly PathTokenizer _tokenizer;

        public TreeEnumerator(UrlMatchingNode root, PathTokenizer tokenizer)
        {
            _stack = new Stack<UrlMatchingNode>();
            _tokenizer = tokenizer;
            Current = null;

            _stack.Push(root);
        }

        public UrlMatchingNode Current { get; private set; }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (_stack == null)
            {
                return false;
            }

            while (_stack.Count > 0)
            {
                var next = _stack.Pop();

                // In case of wild card segment, the request path segment length can be greater
                // Example:
                // Template:    a/{*path}
                // Request Url: a/b/c/d
                if (next.IsCatchAll && next.Matches.Count > 0)
                {
                    Current = next;
                    return true;
                }
                // Next template has the same length as the url we are trying to match
                // The only possible matching segments are either our current matches or
                // any catch-all segment after this segment in which the catch all is empty.
                else if (next.Depth == _tokenizer.Count)
                {
                    if (next.Matches.Count > 0)
                    {
                        Current = next;
                        return true;
                    }
                    else
                    {
                        // We can stop looking as any other child node from this node will be
                        // either a literal, a constrained parameter or a parameter.
                        // (Catch alls and constrained catch alls will show up as candidate matches).
                        continue;
                    }
                }

                if (next.CatchAlls != null)
                {
                    _stack.Push(next.CatchAlls);
                }

                if (next.ConstrainedCatchAlls != null)
                {
                    _stack.Push(next.ConstrainedCatchAlls);
                }

                if (next.Parameters != null)
                {
                    _stack.Push(next.Parameters);
                }

                if (next.ConstrainedParameters != null)
                {
                    _stack.Push(next.ConstrainedParameters);
                }

                if (next.Literals.Count > 0)
                {
                    Debug.Assert(next.Depth < _tokenizer.Count);
                    if (next.Literals.TryGetValue(_tokenizer[next.Depth].Value, out var node))
                    {
                        _stack.Push(node);
                    }
                }
            }

            return false;
        }

        public void Reset()
        {
            _stack.Clear();
            Current = null;
        }
    }
}
