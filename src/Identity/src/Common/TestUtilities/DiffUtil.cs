// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TestUtilities
{
    public class DiffUtil
    {
        private enum EditKind
        {
            /// <summary>
            /// No change.
            /// </summary>
            None = 0,

            /// <summary>
            /// Node value was updated.
            /// </summary>
            Update = 1,

            /// <summary>
            /// Node was inserted.
            /// </summary>
            Insert = 2,

            /// <summary>
            /// Node was deleted.
            /// </summary>
            Delete = 3,
        }

        private class LCS<T> : LongestCommonSubsequence<IList<T>>
        {
            public static readonly LCS<T> Default = new LCS<T>((left, right) => EqualityComparer<T>.Default.Equals(left, right));

            private readonly Func<T, T, bool> _comparer;

            public LCS(Func<T, T, bool> comparer)
            {
                _comparer = comparer;
            }

            protected override bool ItemsEqual(IList<T> sequenceA, int indexA, IList<T> sequenceB, int indexB)
            {
                return _comparer(sequenceA[indexA], sequenceB[indexB]);
            }

            public IEnumerable<string> CalculateDiff(IList<T> sequenceA, IList<T> sequenceB, Func<T, string> toString)
            {
                foreach (var edit in GetEdits(sequenceA, sequenceA.Count, sequenceB, sequenceB.Count).Reverse())
                {
                    switch (edit.Kind)
                    {
                        case EditKind.Delete:
                            yield return "--> " + toString(sequenceA[edit.IndexA]);
                            break;

                        case EditKind.Insert:
                            yield return "++> " + toString(sequenceB[edit.IndexB]);
                            break;

                        case EditKind.Update:
                            yield return "    " + toString(sequenceB[edit.IndexB]);
                            break;
                    }
                }
            }
        }

        public static string DiffReport<T>(IEnumerable<T> expected, IEnumerable<T> actual, Func<T, T, bool> comparer = null, Func<T, string> toString = null, string separator = ",\r\n")
        {
            var lcs = (comparer != null) ? new LCS<T>(comparer) : LCS<T>.Default;
            toString = toString ?? new Func<T, string>(obj => obj.ToString());

            IList<T> expectedList = expected as IList<T> ?? new List<T>(expected);
            IList<T> actualList = actual as IList<T> ?? new List<T>(actual);

            return string.Join(separator, lcs.CalculateDiff(expectedList, actualList, toString));
        }

        private static readonly char[] s_lineSplitChars = new[] { '\r', '\n' };

        public static string[] Lines(string s)
        {
            return s.Split(s_lineSplitChars, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string DiffReport(string expected, string actual)
        {
            var exlines = Lines(expected);
            var aclines = Lines(actual);
            return DiffReport(exlines, aclines, separator: "\r\n");
        }

        /// <summary>
        /// Calculates Longest Common Subsequence.
        /// </summary>
        private abstract class LongestCommonSubsequence<TSequence>
        {
            protected struct Edit
            {
                public readonly EditKind Kind;
                public readonly int IndexA;
                public readonly int IndexB;

                internal Edit(EditKind kind, int indexA, int indexB)
                {
                    this.Kind = kind;
                    this.IndexA = indexA;
                    this.IndexB = indexB;
                }
            }

            private const int DeleteCost = 1;
            private const int InsertCost = 1;
            private const int UpdateCost = 2;

            protected abstract bool ItemsEqual(TSequence sequenceA, int indexA, TSequence sequenceB, int indexB);

            protected IEnumerable<KeyValuePair<int, int>> GetMatchingPairs(TSequence sequenceA, int lengthA, TSequence sequenceB, int lengthB)
            {
                int[,] d = ComputeCostMatrix(sequenceA, lengthA, sequenceB, lengthB);
                int i = lengthA;
                int j = lengthB;

                while (i != 0 && j != 0)
                {
                    if (d[i, j] == d[i - 1, j] + DeleteCost)
                    {
                        i--;
                    }
                    else if (d[i, j] == d[i, j - 1] + InsertCost)
                    {
                        j--;
                    }
                    else
                    {
                        i--;
                        j--;
                        yield return new KeyValuePair<int, int>(i, j);
                    }
                }
            }

            protected IEnumerable<Edit> GetEdits(TSequence sequenceA, int lengthA, TSequence sequenceB, int lengthB)
            {
                int[,] d = ComputeCostMatrix(sequenceA, lengthA, sequenceB, lengthB);
                int i = lengthA;
                int j = lengthB;

                while (i != 0 && j != 0)
                {
                    if (d[i, j] == d[i - 1, j] + DeleteCost)
                    {
                        i--;
                        yield return new Edit(EditKind.Delete, i, -1);
                    }
                    else if (d[i, j] == d[i, j - 1] + InsertCost)
                    {
                        j--;
                        yield return new Edit(EditKind.Insert, -1, j);
                    }
                    else
                    {
                        i--;
                        j--;
                        yield return new Edit(EditKind.Update, i, j);
                    }
                }

                while (i > 0)
                {
                    i--;
                    yield return new Edit(EditKind.Delete, i, -1);
                }

                while (j > 0)
                {
                    j--;
                    yield return new Edit(EditKind.Insert, -1, j);
                }
            }

            /// <summary>
            /// Returns a distance [0..1] of the specified sequences.
            /// The smaller distance the more of their elements match.
            /// </summary>
            /// <summary>
            /// Returns a distance [0..1] of the specified sequences.
            /// The smaller distance the more of their elements match.
            /// </summary>
            protected double ComputeDistance(TSequence sequenceA, int lengthA, TSequence sequenceB, int lengthB)
            {
                Debug.Assert(lengthA >= 0 && lengthB >= 0);

                if (lengthA == 0 || lengthB == 0)
                {
                    return (lengthA == lengthB) ? 0.0 : 1.0;
                }

                int lcsLength = 0;
                foreach (var pair in GetMatchingPairs(sequenceA, lengthA, sequenceB, lengthB))
                {
                    lcsLength++;
                }

                int max = Math.Max(lengthA, lengthB);
                Debug.Assert(lcsLength <= max);
                return 1.0 - (double)lcsLength / (double)max;
            }

            /// <summary>
            /// Calculates costs of all paths in an edit graph starting from vertex (0,0) and ending in vertex (lengthA, lengthB). 
            /// </summary>
            /// <remarks>
            /// The edit graph for A and B has a vertex at each point in the grid (i,j), i in [0, lengthA] and j in [0, lengthB].
            /// 
            /// The vertices of the edit graph are connected by horizontal, vertical, and diagonal directed edges to form a directed acyclic graph.
            /// Horizontal edges connect each vertex to its right neighbor. 
            /// Vertical edges connect each vertex to the neighbor below it.
            /// Diagonal edges connect vertex (i,j) to vertex (i-1,j-1) if <see cref="ItemsEqual"/>(sequenceA[i-1],sequenceB[j-1]) is true.
            /// 
            /// Editing starts with S = []. 
            /// Move along horizontal edge (i-1,j)-(i,j) represents the fact that sequenceA[i-1] is not added to S.
            /// Move along vertical edge (i,j-1)-(i,j) represents an insert of sequenceB[j-1] to S.
            /// Move along diagonal edge (i-1,j-1)-(i,j) represents an addition of sequenceB[j-1] to S via an acceptable 
            /// change of sequenceA[i-1] to sequenceB[j-1].
            /// 
            /// In every vertex the cheapest outgoing edge is selected. 
            /// The number of diagonal edges on the path from (0,0) to (lengthA, lengthB) is the length of the longest common subsequence.
            /// </remarks>
            private int[,] ComputeCostMatrix(TSequence sequenceA, int lengthA, TSequence sequenceB, int lengthB)
            {
                var la = lengthA + 1;
                var lb = lengthB + 1;

                // TODO: Optimization possible: O(ND) time, O(N) space
                // EUGENE W. MYERS: An O(ND) Difference Algorithm and Its Variations
                var d = new int[la, lb];

                d[0, 0] = 0;
                for (int i = 1; i <= lengthA; i++)
                {
                    d[i, 0] = d[i - 1, 0] + DeleteCost;
                }

                for (int j = 1; j <= lengthB; j++)
                {
                    d[0, j] = d[0, j - 1] + InsertCost;
                }

                for (int i = 1; i <= lengthA; i++)
                {
                    for (int j = 1; j <= lengthB; j++)
                    {
                        int m1 = d[i - 1, j - 1] + (ItemsEqual(sequenceA, i - 1, sequenceB, j - 1) ? 0 : UpdateCost);
                        int m2 = d[i - 1, j] + DeleteCost;
                        int m3 = d[i, j - 1] + InsertCost;
                        d[i, j] = Math.Min(Math.Min(m1, m2), m3);
                    }
                }

                return d;
            }
        }
    }
}
