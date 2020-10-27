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
        public void File_CanSupplySingle()
        {
            var file = new BrowserFile();
            var instance = new InputFileChangeEventArgs(new[] { file });
            Assert.Same(file, instance.File);
        }

        [Fact]
        public void File_ThrowsIfZeroFiles()
        {
            var instance = new InputFileChangeEventArgs(Array.Empty<IBrowserFile>());
            var ex = Assert.Throws<InvalidOperationException>(() => instance.File);
            Assert.StartsWith("No file was supplied", ex.Message);
        }

        [Fact]
        public void File_ThrowsIfMultipleFiles()
        {
            var instance = new InputFileChangeEventArgs(new[] { new BrowserFile(), new BrowserFile() });
            var ex = Assert.Throws<InvalidOperationException>(() => instance.File);
            Assert.StartsWith("More than one file was supplied", ex.Message);
        }

        [Fact]
        public void GetMultipleFiles_CanSupplyEmpty()
        {
            var instance = new InputFileChangeEventArgs(Array.Empty<IBrowserFile>());
            Assert.Empty(instance.GetMultipleFiles());
        }

        [Fact]
        public void GetMultipleFiles_CanSupplyFiles()
        {
            var files = new[] { new BrowserFile(), new BrowserFile() };
            var instance = new InputFileChangeEventArgs(files);
            Assert.Same(files, instance.GetMultipleFiles());
        }

        [Fact]
        public void GetMultipleFiles_ThrowsIfTooManyFiles()
        {
            var files = new[] { new BrowserFile(), new BrowserFile() };
            var instance = new InputFileChangeEventArgs(files);
            var ex = Assert.Throws<InvalidOperationException>(() => instance.GetMultipleFiles(1));
            Assert.Equal($"The maximum number of files accepted is 1, but 2 were supplied.", ex.Message);
        }

        [Fact]
        public void ReadRequestTypeCanBeLoaded()
        {
            // Represents https://github.com/dotnet/aspnetcore/issues/26882
            // Even though the ReadRequest type is only ever used on WebAssembly, developers might
            // do something that causes the type to be loaded on other environments, for example
            // using reflection. It's just a DTO with no behaviors so there's nothing to test
            // except that loading the type doesn't trigger an exception.
            GC.KeepAlive(new ReadRequest());
        }
    }
}
