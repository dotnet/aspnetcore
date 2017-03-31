// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Authentication
{
    public class TokenExtensionTests
    {
        [Fact]
        public void CanStoreMultipleTokens()
        {
            var props = new AuthenticationProperties();
            var tokens = new List<AuthenticationToken>();
            var tok1 = new AuthenticationToken { Name = "One", Value = "1" };
            var tok2 = new AuthenticationToken { Name = "Two", Value = "2" };
            var tok3 = new AuthenticationToken { Name = "Three", Value = "3" };
            tokens.Add(tok1);
            tokens.Add(tok2);
            tokens.Add(tok3);
            props.StoreTokens(tokens);

            Assert.Equal("1", props.GetTokenValue("One"));
            Assert.Equal("2", props.GetTokenValue("Two"));
            Assert.Equal("3", props.GetTokenValue("Three"));
            Assert.Equal(3, props.GetTokens().Count());
        }

        [Fact]
        public void SubsequentStoreTokenDeletesPreviousTokens()
        {
            var props = new AuthenticationProperties();
            var tokens = new List<AuthenticationToken>();
            var tok1 = new AuthenticationToken { Name = "One", Value = "1" };
            var tok2 = new AuthenticationToken { Name = "Two", Value = "2" };
            var tok3 = new AuthenticationToken { Name = "Three", Value = "3" };
            tokens.Add(tok1);
            tokens.Add(tok2);
            tokens.Add(tok3);

            props.StoreTokens(tokens);

            props.StoreTokens(new[] { new AuthenticationToken { Name = "Zero", Value = "0" } });

            Assert.Equal("0", props.GetTokenValue("Zero"));
            Assert.Equal(null, props.GetTokenValue("One"));
            Assert.Equal(null, props.GetTokenValue("Two"));
            Assert.Equal(null, props.GetTokenValue("Three"));
            Assert.Equal(1, props.GetTokens().Count());
        }

        [Fact]
        public void CanUpdateTokens()
        {
            var props = new AuthenticationProperties();
            var tokens = new List<AuthenticationToken>();
            var tok1 = new AuthenticationToken { Name = "One", Value = "1" };
            var tok2 = new AuthenticationToken { Name = "Two", Value = "2" };
            var tok3 = new AuthenticationToken { Name = "Three", Value = "3" };
            tokens.Add(tok1);
            tokens.Add(tok2);
            tokens.Add(tok3);
            props.StoreTokens(tokens);

            tok1.Value = ".1";
            tok2.Value = ".2";
            tok3.Value = ".3";
            props.StoreTokens(tokens);

            Assert.Equal(".1", props.GetTokenValue("One"));
            Assert.Equal(".2", props.GetTokenValue("Two"));
            Assert.Equal(".3", props.GetTokenValue("Three"));
            Assert.Equal(3, props.GetTokens().Count());
        }

        [Fact]
        public void CanUpdateTokenValues()
        {
            var props = new AuthenticationProperties();
            var tokens = new List<AuthenticationToken>();
            var tok1 = new AuthenticationToken { Name = "One", Value = "1" };
            var tok2 = new AuthenticationToken { Name = "Two", Value = "2" };
            var tok3 = new AuthenticationToken { Name = "Three", Value = "3" };
            tokens.Add(tok1);
            tokens.Add(tok2);
            tokens.Add(tok3);
            props.StoreTokens(tokens);

            Assert.True(props.UpdateTokenValue("One", ".11"));
            Assert.True(props.UpdateTokenValue("Two", ".22"));
            Assert.True(props.UpdateTokenValue("Three", ".33"));

            Assert.Equal(".11", props.GetTokenValue("One"));
            Assert.Equal(".22", props.GetTokenValue("Two"));
            Assert.Equal(".33", props.GetTokenValue("Three"));
            Assert.Equal(3, props.GetTokens().Count());
        }

        [Fact]
        public void UpdateTokenValueReturnsFalseForUnknownToken()
        {
            var props = new AuthenticationProperties();
            var tokens = new List<AuthenticationToken>();
            var tok1 = new AuthenticationToken { Name = "One", Value = "1" };
            var tok2 = new AuthenticationToken { Name = "Two", Value = "2" };
            var tok3 = new AuthenticationToken { Name = "Three", Value = "3" };
            tokens.Add(tok1);
            tokens.Add(tok2);
            tokens.Add(tok3);
            props.StoreTokens(tokens);

            Assert.False(props.UpdateTokenValue("ONE", ".11"));
            Assert.False(props.UpdateTokenValue("Jigglypuff", ".11"));

            Assert.Null(props.GetTokenValue("ONE"));
            Assert.Null(props.GetTokenValue("Jigglypuff"));
            Assert.Equal(3, props.GetTokens().Count());

        }
    }
}
