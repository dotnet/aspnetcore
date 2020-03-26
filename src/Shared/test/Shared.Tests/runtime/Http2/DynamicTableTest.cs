// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.HPack;
using System.Reflection;
using System.Text;
using Xunit;

namespace System.Net.Http.Unit.Tests.HPack
{
    public class DynamicTableTest
    {
        private readonly HeaderField _header1 = new HeaderField(Encoding.ASCII.GetBytes("header-1"), Encoding.ASCII.GetBytes("value1"));
        private readonly HeaderField _header2 = new HeaderField(Encoding.ASCII.GetBytes("header-02"), Encoding.ASCII.GetBytes("value_2"));

        [Fact]
        public void DynamicTable_IsInitiallyEmpty()
        {
            DynamicTable dynamicTable = new DynamicTable(4096);
            Assert.Equal(0, dynamicTable.Count);
            Assert.Equal(0, dynamicTable.Size);
            Assert.Equal(4096, dynamicTable.MaxSize);
        }

        [Fact]
        public void DynamicTable_Count_IsNumberOfEntriesInDynamicTable()
        {
            DynamicTable dynamicTable = new DynamicTable(4096);

            dynamicTable.Insert(_header1.Name, _header1.Value);
            Assert.Equal(1, dynamicTable.Count);

            dynamicTable.Insert(_header2.Name, _header2.Value);
            Assert.Equal(2, dynamicTable.Count);
        }

        [Fact]
        public void DynamicTable_Size_IsCurrentDynamicTableSize()
        {
            DynamicTable dynamicTable = new DynamicTable(4096);
            Assert.Equal(0, dynamicTable.Size);

            dynamicTable.Insert(_header1.Name, _header1.Value);
            Assert.Equal(_header1.Length, dynamicTable.Size);

            dynamicTable.Insert(_header2.Name, _header2.Value);
            Assert.Equal(_header1.Length + _header2.Length, dynamicTable.Size);
        }

        [Fact]
        public void DynamicTable_FirstEntry_IsMostRecentEntry()
        {
            DynamicTable dynamicTable = new DynamicTable(4096);
            dynamicTable.Insert(_header1.Name, _header1.Value);
            dynamicTable.Insert(_header2.Name, _header2.Value);

            VerifyTableEntries(dynamicTable, _header2, _header1);
        }

        [Fact]
        public void BoundsCheck_ThrowsIndexOutOfRangeException()
        {
            DynamicTable dynamicTable = new DynamicTable(4096);
            Assert.Throws<IndexOutOfRangeException>(() => dynamicTable[0]);

            dynamicTable.Insert(_header1.Name, _header1.Value);
            Assert.Throws<IndexOutOfRangeException>(() => dynamicTable[1]);
        }

        [Fact]
        public void DynamicTable_InsertEntryLargerThanMaxSize_NoOp()
        {
            DynamicTable dynamicTable = new DynamicTable(_header1.Length - 1);
            dynamicTable.Insert(_header1.Name, _header1.Value);

            Assert.Equal(0, dynamicTable.Count);
            Assert.Equal(0, dynamicTable.Size);
        }

        [Fact]
        public void DynamicTable_InsertEntryLargerThanRemainingSpace_NoOp()
        {
            DynamicTable dynamicTable = new DynamicTable(_header1.Length);
            dynamicTable.Insert(_header1.Name, _header1.Value);

            VerifyTableEntries(dynamicTable, _header1);

            dynamicTable.Insert(_header2.Name, _header2.Value);

            Assert.Equal(0, dynamicTable.Count);
            Assert.Equal(0, dynamicTable.Size);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void DynamicTable_WrapsRingBuffer_Success(int targetInsertIndex)
        {
            FieldInfo insertIndexField = typeof(DynamicTable).GetField("_insertIndex", BindingFlags.NonPublic | BindingFlags.Instance);
            DynamicTable table = new DynamicTable(maxSize: 256);
            Stack<byte[]> insertedHeaders = new Stack<byte[]>();

            // Insert into dynamic table until its insert index into its ring buffer loops back to 0.
            do
            {
                InsertOne();
            }
            while ((int)insertIndexField.GetValue(table) != 0);

            // Finally loop until the insert index reaches the target.
            while ((int)insertIndexField.GetValue(table) != targetInsertIndex)
            {
                InsertOne();
            }

            void InsertOne()
            {
                byte[] data = Encoding.ASCII.GetBytes($"header-{insertedHeaders.Count}");

                insertedHeaders.Push(data);
                table.Insert(data, data);
            }

            // Now check to see that we can retrieve the remaining headers.
            // Some headers will have been evacuated from the table during this process, so we don't exhaust the entire insertedHeaders stack.
            Assert.True(table.Count > 0);
            Assert.True(table.Count < insertedHeaders.Count);

            for (int i = 0; i < table.Count; ++i)
            {
                HeaderField dynamicField = table[i];
                byte[] expectedData = insertedHeaders.Pop();

                Assert.True(expectedData.AsSpan().SequenceEqual(dynamicField.Name));
                Assert.True(expectedData.AsSpan().SequenceEqual(dynamicField.Value));
            }
        }

        [Theory]
        [MemberData(nameof(CreateResizeData))]
        public void DynamicTable_Resize_Success(int initialMaxSize, int finalMaxSize, int insertSize)
        {
            // This is purely to make it simple to perfectly reach our initial max size to test growing a full but non-wrapping buffer.
            Debug.Assert((insertSize % 64) == 0, $"{nameof(insertSize)} must be a multiple of 64 ({nameof(HeaderField)}.{nameof(HeaderField.RfcOverhead)} * 2)");

            DynamicTable dynamicTable = new DynamicTable(maxSize: initialMaxSize);
            int insertedSize = 0;

            while (insertedSize != insertSize)
            {
                byte[] data = Encoding.ASCII.GetBytes($"header-{dynamicTable.Size}".PadRight(16, ' '));
                Debug.Assert(data.Length == 16);

                dynamicTable.Insert(data, data);
                insertedSize += data.Length * 2 + HeaderField.RfcOverhead;
            }

            List<HeaderField> headers = new List<HeaderField>();

            for (int i = 0; i < dynamicTable.Count; ++i)
            {
                headers.Add(dynamicTable[i]);
            }

            dynamicTable.Resize(finalMaxSize);

            int expectedCount = Math.Min(finalMaxSize / 64, headers.Count);
            Assert.Equal(expectedCount, dynamicTable.Count);

            for (int i = 0; i < dynamicTable.Count; ++i)
            {
                Assert.True(headers[i].Name.AsSpan().SequenceEqual(dynamicTable[i].Name));
                Assert.True(headers[i].Value.AsSpan().SequenceEqual(dynamicTable[i].Value));
            }
        }

        [Fact]
        public void DynamicTable_ResizingEvictsOldestEntries()
        {
            DynamicTable dynamicTable = new DynamicTable(4096);
            dynamicTable.Insert(_header1.Name, _header1.Value);
            dynamicTable.Insert(_header2.Name, _header2.Value);

            VerifyTableEntries(dynamicTable, _header2, _header1);

            dynamicTable.Resize(_header2.Length);

            VerifyTableEntries(dynamicTable, _header2);
        }

        [Fact]
        public void DynamicTable_ResizingToZeroEvictsAllEntries()
        {
            DynamicTable dynamicTable = new DynamicTable(4096);
            dynamicTable.Insert(_header1.Name, _header1.Value);
            dynamicTable.Insert(_header2.Name, _header2.Value);

            dynamicTable.Resize(0);

            Assert.Equal(0, dynamicTable.Count);
            Assert.Equal(0, dynamicTable.Size);
        }

        [Fact]
        public void DynamicTable_CanBeResizedToLargerMaxSize()
        {
            DynamicTable dynamicTable = new DynamicTable(_header1.Length);
            dynamicTable.Insert(_header1.Name, _header1.Value);
            dynamicTable.Insert(_header2.Name, _header2.Value);

            // _header2 is larger than _header1, so an attempt at inserting it
            // would first clear the table then return without actually inserting it,
            // given it is larger than the current max size.
            Assert.Equal(0, dynamicTable.Count);
            Assert.Equal(0, dynamicTable.Size);

            dynamicTable.Resize(dynamicTable.MaxSize + _header2.Length);
            dynamicTable.Insert(_header2.Name, _header2.Value);

            VerifyTableEntries(dynamicTable, _header2);
        }

        public static IEnumerable<object[]> CreateResizeData()
        {
            int[] values = new[] { 128, 256, 384, 512 };
            return from initialMaxSize in values
                   from finalMaxSize in values
                   from insertSize in values
                   select new object[] { initialMaxSize, finalMaxSize, insertSize };
        }

        private void VerifyTableEntries(DynamicTable dynamicTable, params HeaderField[] entries)
        {
            Assert.Equal(entries.Length, dynamicTable.Count);
            Assert.Equal(entries.Sum(e => e.Length), dynamicTable.Size);

            for (int i = 0; i < entries.Length; i++)
            {
                HeaderField headerField = dynamicTable[i];

                Assert.NotSame(entries[i].Name, headerField.Name);
                Assert.Equal(entries[i].Name, headerField.Name);

                Assert.NotSame(entries[i].Value, headerField.Value);
                Assert.Equal(entries[i].Value, headerField.Value);
            }
        }
    }
}
