using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.Extensions.Configuration.KeyPerFile.Test
{
    public class KeyPerFileTests
    {
        [Fact]
        public void DoesNotThrowWhenOptionalAndNoSecrets()
        {
            new ConfigurationBuilder().AddKeyPerFile(o => o.Optional = true).Build();
        }

        [Fact]
        public void DoesNotThrowWhenOptionalAndDirectoryDoesntExist()
        {
            new ConfigurationBuilder().AddKeyPerFile("nonexistent", true).Build();
        }

        [Fact]
        public void ThrowsWhenNotOptionalAndDirectoryDoesntExist()
        {
            var e = Assert.Throws<ArgumentException>(() => new ConfigurationBuilder().AddKeyPerFile("nonexistent", false).Build());
            Assert.Contains("The directory name", e.Message);
        }

        [Fact]
        public void CanLoadMultipleSecrets()
        {
            var testFileProvider = new TestFileProvider(
                new TestFile("Secret1", "SecretValue1"),
                new TestFile("Secret2", "SecretValue2"));

            var config = new ConfigurationBuilder()
                .AddKeyPerFile(o => o.FileProvider = testFileProvider)
                .Build();

            Assert.Equal("SecretValue1", config["Secret1"]);
            Assert.Equal("SecretValue2", config["Secret2"]);
        }

        [Fact]
        public void CanLoadMultipleSecretsWithDirectory()
        {
            var testFileProvider = new TestFileProvider(
                new TestFile("Secret1", "SecretValue1"),
                new TestFile("Secret2", "SecretValue2"),
                new TestFile("directory"));

            var config = new ConfigurationBuilder()
                .AddKeyPerFile(o => o.FileProvider = testFileProvider)
                .Build();

            Assert.Equal("SecretValue1", config["Secret1"]);
            Assert.Equal("SecretValue2", config["Secret2"]);
        }

        [Fact]
        public void CanLoadNestedKeys()
        {
            var testFileProvider = new TestFileProvider(
                new TestFile("Secret0__Secret1__Secret2__Key", "SecretValue2"),
                new TestFile("Secret0__Secret1__Key", "SecretValue1"),
                new TestFile("Secret0__Key", "SecretValue0"));

            var config = new ConfigurationBuilder()
                .AddKeyPerFile(o => o.FileProvider = testFileProvider)
                .Build();

            Assert.Equal("SecretValue0", config["Secret0:Key"]);
            Assert.Equal("SecretValue1", config["Secret0:Secret1:Key"]);
            Assert.Equal("SecretValue2", config["Secret0:Secret1:Secret2:Key"]);
        }

        [Fact]
        public void CanIgnoreFilesWithDefault()
        {
            var testFileProvider = new TestFileProvider(
                new TestFile("ignore.Secret0", "SecretValue0"),
                new TestFile("ignore.Secret1", "SecretValue1"),
                new TestFile("Secret2", "SecretValue2"));

            var config = new ConfigurationBuilder()
                .AddKeyPerFile(o => o.FileProvider = testFileProvider)
                .Build();

            Assert.Null(config["ignore.Secret0"]);
            Assert.Null(config["ignore.Secret1"]);
            Assert.Equal("SecretValue2", config["Secret2"]);
        }

        [Fact]
        public void CanTurnOffDefaultIgnorePrefixWithCondition()
        {
            var testFileProvider = new TestFileProvider(
                new TestFile("ignore.Secret0", "SecretValue0"),
                new TestFile("ignore.Secret1", "SecretValue1"),
                new TestFile("Secret2", "SecretValue2"));

            var config = new ConfigurationBuilder()
                .AddKeyPerFile(o =>
                {
                    o.FileProvider = testFileProvider;
                    o.IgnoreCondition = null;
                })
                .Build();

            Assert.Equal("SecretValue0", config["ignore.Secret0"]);
            Assert.Equal("SecretValue1", config["ignore.Secret1"]);
            Assert.Equal("SecretValue2", config["Secret2"]);
        }

        [Fact]
        public void CanIgnoreAllWithCondition()
        {
            var testFileProvider = new TestFileProvider(
                new TestFile("Secret0", "SecretValue0"),
                new TestFile("Secret1", "SecretValue1"),
                new TestFile("Secret2", "SecretValue2"));

            var config = new ConfigurationBuilder()
                .AddKeyPerFile(o =>
                {
                    o.FileProvider = testFileProvider;
                    o.IgnoreCondition = s => true;
                })
                .Build();

            Assert.Empty(config.AsEnumerable());
        }

        [Fact]
        public void CanIgnoreFilesWithCustomIgnore()
        {
            var testFileProvider = new TestFileProvider(
                new TestFile("meSecret0", "SecretValue0"),
                new TestFile("meSecret1", "SecretValue1"),
                new TestFile("Secret2", "SecretValue2"));

            var config = new ConfigurationBuilder()
                .AddKeyPerFile(o =>
                {
                    o.FileProvider = testFileProvider;
                    o.IgnorePrefix = "me";
                })
                .Build();

            Assert.Null(config["meSecret0"]);
            Assert.Null(config["meSecret1"]);
            Assert.Equal("SecretValue2", config["Secret2"]);
        }

        [Fact]
        public void CanUnIgnoreDefaultFiles()
        {
            var testFileProvider = new TestFileProvider(
                new TestFile("ignore.Secret0", "SecretValue0"),
                new TestFile("ignore.Secret1", "SecretValue1"),
                new TestFile("Secret2", "SecretValue2"));

            var config = new ConfigurationBuilder()
                .AddKeyPerFile(o =>
                {
                    o.FileProvider = testFileProvider;
                    o.IgnorePrefix = null;
                })
                .Build();

            Assert.Equal("SecretValue0", config["ignore.Secret0"]);
            Assert.Equal("SecretValue1", config["ignore.Secret1"]);
            Assert.Equal("SecretValue2", config["Secret2"]);
        }
    }

    class TestFileProvider : IFileProvider
    {
        IDirectoryContents _contents;
        
        public TestFileProvider(params IFileInfo[] files)
        {
            _contents = new TestDirectoryContents(files);
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            return _contents;
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            throw new NotImplementedException();
        }

        public IChangeToken Watch(string filter)
        {
            throw new NotImplementedException();
        }
    }

    class TestDirectoryContents : IDirectoryContents
    {
        List<IFileInfo> _list;

        public TestDirectoryContents(params IFileInfo[] files)
        {
            _list = new List<IFileInfo>(files);
        }

        public bool Exists
        {
            get
            {
                return true;
            }
        }

        public IEnumerator<IFileInfo> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    //TODO: Probably need a directory and file type.
    class TestFile : IFileInfo
    {
        private string _name;
        private string _contents;

        public bool Exists
        {
            get
            {
                return true;
            }
        }

        public bool IsDirectory
        {
            get;
        }

        public DateTimeOffset LastModified
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public long Length
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
        }

        public string PhysicalPath
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public TestFile(string name)
        {
            _name = name;
            IsDirectory = true;
        }

        public TestFile(string name, string contents)
        {
            _name = name;
            _contents = contents;
        }

        public Stream CreateReadStream()
        {
            if(IsDirectory)
            {
                throw new InvalidOperationException("Cannot create stream from directory");
            }

            return new MemoryStream(Encoding.UTF8.GetBytes(_contents));
        }
    }
}