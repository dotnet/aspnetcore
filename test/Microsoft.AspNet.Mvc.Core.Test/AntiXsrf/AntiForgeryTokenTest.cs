// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test
{
    public class AntiForgeryTokenTest
    {
        [Fact]
        public void AdditionalDataProperty()
        {
            // Arrange
            var token = new AntiForgeryToken();

            // Act & assert - 1
            Assert.Equal("", token.AdditionalData);

            // Act & assert - 2
            token.AdditionalData = "additional data";
            Assert.Equal("additional data", token.AdditionalData);

            // Act & assert - 3
            token.AdditionalData = null;
            Assert.Equal("", token.AdditionalData);
        }

        [Fact]
        public void ClaimUidProperty()
        {
            // Arrange
            var token = new AntiForgeryToken();

            // Act & assert - 1
            Assert.Null(token.ClaimUid);

            // Act & assert - 2
            BinaryBlob blob = new BinaryBlob(32);
            token.ClaimUid = blob;
            Assert.Equal(blob, token.ClaimUid);

            // Act & assert - 3
            token.ClaimUid = null;
            Assert.Null(token.ClaimUid);
        }

        [Fact]
        public void IsSessionTokenProperty()
        {
            // Arrange
            var token = new AntiForgeryToken();

            // Act & assert - 1
            Assert.False(token.IsSessionToken);

            // Act & assert - 2
            token.IsSessionToken = true;
            Assert.True(token.IsSessionToken);

            // Act & assert - 3
            token.IsSessionToken = false;
            Assert.False(token.IsSessionToken);
        }

        [Fact]
        public void UsernameProperty()
        {
            // Arrange
            var token = new AntiForgeryToken();

            // Act & assert - 1
            Assert.Equal("", token.Username);

            // Act & assert - 2
            token.Username = "my username";
            Assert.Equal("my username", token.Username);

            // Act & assert - 3
            token.Username = null;
            Assert.Equal("", token.Username);
        }

        [Fact]
        public void SecurityTokenProperty_GetsAutopopulated()
        {
            // Arrange
            var token = new AntiForgeryToken();

            // Act
            var securityToken = token.SecurityToken;

            // Assert
            Assert.NotNull(securityToken);
            Assert.Equal(AntiForgeryToken.SecurityTokenBitLength, securityToken.BitLength);

            // check that we're not making a new one each property call
            Assert.Equal(securityToken, token.SecurityToken);
        }

        [Fact]
        public void SecurityTokenProperty_PropertySetter_DoesNotUseDefaults()
        {
            // Arrange
            var token = new AntiForgeryToken();

            // Act
            var securityToken = new BinaryBlob(64);
            token.SecurityToken = securityToken;

            // Assert
            Assert.Equal(securityToken, token.SecurityToken);
        }

        [Fact]
        public void SecurityTokenProperty_PropertySetter_DoesNotAllowNulls()
        {
            // Arrange
            var token = new AntiForgeryToken();

            // Act
            token.SecurityToken = null;
            var securityToken = token.SecurityToken;

            // Assert
            Assert.NotNull(securityToken);
            Assert.Equal(AntiForgeryToken.SecurityTokenBitLength, securityToken.BitLength);

            // check that we're not making a new one each property call
            Assert.Equal(securityToken, token.SecurityToken);
        }
    }
}