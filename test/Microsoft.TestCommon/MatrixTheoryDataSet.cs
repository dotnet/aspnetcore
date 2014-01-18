// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Microsoft.TestCommon
{
    public class MatrixTheoryDataSet<T1, T2> : TheoryDataSet<T1, T2>
    {
        public MatrixTheoryDataSet(IEnumerable<T1> data1, IEnumerable<T2> data2)
        {
            Contract.Assert(data1 != null && data1.Any());
            Contract.Assert(data2 != null && data2.Any());

            foreach (T1 t1 in data1)
            {
                foreach (T2 t2 in data2)
                {
                    Add(t1, t2);
                }
            }
        }
    }
}
