using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http.HPack;
using System.Reflection;
using System.Text;
using System.Linq;
using Xunit;

namespace System.Net.Http.Unit.Tests.HPack
{
    public class DynamicTableTest
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void DynamicTable_WrapsRingBuffer_Success(int targetInsertIndex)
        {
            FieldInfo insertIndexField = typeof(DynamicTable).GetField("_insertIndex", BindingFlags.NonPublic | BindingFlags.Instance);
            var table = new DynamicTable(maxSize: 256);
            var insertedHeaders = new Stack<byte[]>();

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

            var dynamicTable = new DynamicTable(maxSize: initialMaxSize);
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

        public static IEnumerable<object[]> CreateResizeData()
        {
            var values = new[] { 128, 256, 384, 512 };
            return from initialMaxSize in values
                   from finalMaxSize in values
                   from insertSize in values
                   select new object[] { initialMaxSize, finalMaxSize, insertSize };
        }
    }
}
