// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class DynamicTableTests
    {
        private readonly HeaderField _header1 = new HeaderField(Encoding.ASCII.GetBytes("header-1"), Encoding.ASCII.GetBytes("value1"));
        private readonly HeaderField _header2 = new HeaderField(Encoding.ASCII.GetBytes("header-02"), Encoding.ASCII.GetBytes("value_2"));

        [Fact]
        public void DynamicTableIsInitiallyEmpty()
        {
            var dynamicTable = new DynamicTable(4096);
            Assert.Equal(0, dynamicTable.Count);
            Assert.Equal(0, dynamicTable.Size);
            Assert.Equal(4096, dynamicTable.MaxSize);
        }

        [Fact]
        public void CountIsNumberOfEntriesInDynamicTable()
        {
            var dynamicTable = new DynamicTable(4096);

            dynamicTable.Insert(_header1.Name, _header1.Value);
            Assert.Equal(1, dynamicTable.Count);

            dynamicTable.Insert(_header2.Name, _header2.Value);
            Assert.Equal(2, dynamicTable.Count);
        }

        [Fact]
        public void SizeIsCurrentDynamicTableSize()
        {
            var dynamicTable = new DynamicTable(4096);
            Assert.Equal(0, dynamicTable.Size);

            dynamicTable.Insert(_header1.Name, _header1.Value);
            Assert.Equal(_header1.Length, dynamicTable.Size);

            dynamicTable.Insert(_header2.Name, _header2.Value);
            Assert.Equal(_header1.Length + _header2.Length, dynamicTable.Size);
        }

        [Fact]
        public void FirstEntryIsMostRecentEntry()
        {
            var dynamicTable = new DynamicTable(4096);
            dynamicTable.Insert(_header1.Name, _header1.Value);
            dynamicTable.Insert(_header2.Name, _header2.Value);

            VerifyTableEntries(dynamicTable, _header2, _header1);
        }

        [Fact]
        public void ThrowsIndexOutOfRangeException()
        {
            var dynamicTable = new DynamicTable(4096);
            Assert.Throws<IndexOutOfRangeException>(() => dynamicTable[0]);

            dynamicTable.Insert(_header1.Name, _header1.Value);
            Assert.Throws<IndexOutOfRangeException>(() => dynamicTable[1]);
        }

        [Fact]
        public void NoOpWhenInsertingEntryLargerThanMaxSize()
        {
            var dynamicTable = new DynamicTable(_header1.Length - 1);
            dynamicTable.Insert(_header1.Name, _header1.Value);

            Assert.Equal(0, dynamicTable.Count);
            Assert.Equal(0, dynamicTable.Size);
        }

        [Fact]
        public void NoOpWhenInsertingEntryLargerThanRemainingSpace()
        {
            var dynamicTable = new DynamicTable(_header1.Length);
            dynamicTable.Insert(_header1.Name, _header1.Value);

            VerifyTableEntries(dynamicTable, _header1);

            dynamicTable.Insert(_header2.Name, _header2.Value);

            Assert.Equal(0, dynamicTable.Count);
            Assert.Equal(0, dynamicTable.Size);
        }

        [Fact]
        public void ResizingEvictsOldestEntries()
        {
            var dynamicTable = new DynamicTable(4096);
            dynamicTable.Insert(_header1.Name, _header1.Value);
            dynamicTable.Insert(_header2.Name, _header2.Value);

            VerifyTableEntries(dynamicTable, _header2, _header1);

            dynamicTable.Resize(_header2.Length);

            VerifyTableEntries(dynamicTable, _header2);
        }

        [Fact]
        public void ResizingToZeroEvictsAllEntries()
        {
            var dynamicTable = new DynamicTable(4096);
            dynamicTable.Insert(_header1.Name, _header1.Value);
            dynamicTable.Insert(_header2.Name, _header2.Value);

            dynamicTable.Resize(0);

            Assert.Equal(0, dynamicTable.Count);
            Assert.Equal(0, dynamicTable.Size);
        }

        [Fact]
        public void CanBeResizedToLargerMaxSize()
        {
            var dynamicTable = new DynamicTable(_header1.Length);
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

        private void VerifyTableEntries(DynamicTable dynamicTable, params HeaderField[] entries)
        {
            Assert.Equal(entries.Length, dynamicTable.Count);
            Assert.Equal(entries.Sum(e => e.Length), dynamicTable.Size);

            for (var i = 0; i < entries.Length; i++)
            {
                var headerField = dynamicTable[i];

                Assert.NotSame(entries[i].Name, headerField.Name);
                Assert.Equal(entries[i].Name, headerField.Name);

                Assert.NotSame(entries[i].Value, headerField.Value);
                Assert.Equal(entries[i].Value, headerField.Value);
            }
        }
    }
}
