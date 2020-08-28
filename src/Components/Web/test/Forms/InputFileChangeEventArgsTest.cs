// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Components.Forms
{
    public class InputFileChangeEventArgsTest
    {
        [Fact]
        public void SuppliesNumberOfFiles()
        {
            var emptySet = new InputFileChangeEventArgs(Array.Empty<IBrowserFile>());
            Assert.Equal(0, emptySet.FileCount);

            var twoItemSet = new InputFileChangeEventArgs(new[] { new BrowserFile(), new BrowserFile() });
            Assert.Equal(2, twoItemSet.FileCount);
        }

        [Fact]
        public void File_CanSupplyNull()
        {
            var instance = new InputFileChangeEventArgs(Array.Empty<IBrowserFile>());
            Assert.Null(instance.File);
        }

        [Fact]
        public void File_CanSupplySingle()
        {
            var file = new BrowserFile();
            var instance = new InputFileChangeEventArgs(new[] { file });
            Assert.Same(file, instance.File);
        }

        [Fact]
        public void File_ThrowsIfMultipleFiles()
        {
            var instance = new InputFileChangeEventArgs(new[] { new BrowserFile(), new BrowserFile() });
            var ex = Assert.Throws<InvalidOperationException>(() => instance.File);
            Assert.StartsWith("More than one file was supplied", ex.Message);
        }

        [Fact]
        public void AcceptMultipleFiles_CanSupplyEmpty()
        {
            var instance = new InputFileChangeEventArgs(Array.Empty<IBrowserFile>());
            Assert.Empty(instance.AcceptMultipleFiles());
        }

        [Fact]
        public void AcceptMultipleFiles_CanSupplyFiles()
        {
            var files = new[] { new BrowserFile(), new BrowserFile() };
            var instance = new InputFileChangeEventArgs(files);
            Assert.Same(files, instance.AcceptMultipleFiles());
        }

        [Fact]
        public void AcceptMultipleFiles_ThrowsIfTooManyFiles()
        {
            var files = new[] { new BrowserFile(), new BrowserFile() };
            var instance = new InputFileChangeEventArgs(files);
            var ex = Assert.Throws<InvalidOperationException>(() => instance.AcceptMultipleFiles(1));
            Assert.Equal($"The maximum number of files accepted is 1, but 2 were supplied.", ex.Message);
        }
    }
}
