using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Common.Tests.Internal.Protocol
{
    public class JsonHubProtocolTests
    {
        public static IEnumerable<object[]> ProtocolTestData => new[]
        {
            new object[] { new InvocationMessage("123", true, "Target", 1, "Foo", 2.0f), true, NullValueHandling.Ignore, "{\"invocationId\":\"123\",\"type\":1,\"target\":\"Target\",\"nonBlocking\":true,\"arguments\":[1,\"Foo\",2.0]}" },
            new object[] { new InvocationMessage("123", false, "Target", 1, "Foo", 2.0f), true, NullValueHandling.Ignore, "{\"invocationId\":\"123\",\"type\":1,\"target\":\"Target\",\"arguments\":[1,\"Foo\",2.0]}" },
            new object[] { new InvocationMessage("123", false, "Target", true), true, NullValueHandling.Ignore, "{\"invocationId\":\"123\",\"type\":1,\"target\":\"Target\",\"arguments\":[true]}" },
            new object[] { new InvocationMessage("123", false, "Target", new object[] { null }), true, NullValueHandling.Ignore, "{\"invocationId\":\"123\",\"type\":1,\"target\":\"Target\",\"arguments\":[null]}" },
            new object[] { new InvocationMessage("123", false, "Target", new CustomObject()), false, NullValueHandling.Ignore, "{\"invocationId\":\"123\",\"type\":1,\"target\":\"Target\",\"arguments\":[{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00\"}]}" },
            new object[] { new InvocationMessage("123", false, "Target", new CustomObject()), true, NullValueHandling.Ignore, "{\"invocationId\":\"123\",\"type\":1,\"target\":\"Target\",\"arguments\":[{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00\"}]}" },
            new object[] { new InvocationMessage("123", false, "Target", new CustomObject()), false, NullValueHandling.Include, "{\"invocationId\":\"123\",\"type\":1,\"target\":\"Target\",\"arguments\":[{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00\",\"NullProp\":null}]}" },
            new object[] { new InvocationMessage("123", false, "Target", new CustomObject()), true, NullValueHandling.Include, "{\"invocationId\":\"123\",\"type\":1,\"target\":\"Target\",\"arguments\":[{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00\",\"nullProp\":null}]}" },

            new object[] { new StreamItemMessage("123", 1), true, NullValueHandling.Ignore, "{\"invocationId\":\"123\",\"type\":2,\"item\":1}" },
            new object[] { new StreamItemMessage("123", "Foo"), true, NullValueHandling.Ignore, "{\"invocationId\":\"123\",\"type\":2,\"item\":\"Foo\"}" },
            new object[] { new StreamItemMessage("123", 2.0f), true, NullValueHandling.Ignore, "{\"invocationId\":\"123\",\"type\":2,\"item\":2.0}" },
            new object[] { new StreamItemMessage("123", true), true, NullValueHandling.Ignore, "{\"invocationId\":\"123\",\"type\":2,\"item\":true}" },
            new object[] { new StreamItemMessage("123", null), true, NullValueHandling.Ignore, "{\"invocationId\":\"123\",\"type\":2,\"item\":null}" },
            new object[] { new StreamItemMessage("123", new CustomObject()), false, NullValueHandling.Ignore, "{\"invocationId\":\"123\",\"type\":2,\"item\":{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00\"}}" },
            new object[] { new StreamItemMessage("123", new CustomObject()), true, NullValueHandling.Ignore, "{\"invocationId\":\"123\",\"type\":2,\"item\":{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00\"}}" },
            new object[] { new StreamItemMessage("123", new CustomObject()), false, NullValueHandling.Include, "{\"invocationId\":\"123\",\"type\":2,\"item\":{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00\",\"NullProp\":null}}" },
            new object[] { new StreamItemMessage("123", new CustomObject()), true, NullValueHandling.Include, "{\"invocationId\":\"123\",\"type\":2,\"item\":{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00\",\"nullProp\":null}}" },

            new object[] { CompletionMessage.WithResult("123", 1), true, NullValueHandling.Ignore, "{\"invocationId\":\"123\",\"type\":3,\"result\":1}" },
            new object[] { CompletionMessage.WithResult("123", "Foo"), true, NullValueHandling.Ignore, "{\"invocationId\":\"123\",\"type\":3,\"result\":\"Foo\"}" },
            new object[] { CompletionMessage.WithResult("123", 2.0f), true, NullValueHandling.Ignore, "{\"invocationId\":\"123\",\"type\":3,\"result\":2.0}" },
            new object[] { CompletionMessage.WithResult("123", true), true, NullValueHandling.Ignore, "{\"invocationId\":\"123\",\"type\":3,\"result\":true}" },
            new object[] { CompletionMessage.WithResult("123", null), true, NullValueHandling.Ignore, "{\"invocationId\":\"123\",\"type\":3,\"result\":null}" },
            new object[] { CompletionMessage.WithError("123", "Whoops!"), false, NullValueHandling.Ignore, "{\"invocationId\":\"123\",\"type\":3,\"error\":\"Whoops!\"}" },
            new object[] { CompletionMessage.WithResult("123", new CustomObject()), false, NullValueHandling.Ignore, "{\"invocationId\":\"123\",\"type\":3,\"result\":{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00\"}}" },
            new object[] { CompletionMessage.WithResult("123", new CustomObject()), true, NullValueHandling.Ignore, "{\"invocationId\":\"123\",\"type\":3,\"result\":{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00\"}}" },
            new object[] { CompletionMessage.WithResult("123", new CustomObject()), false, NullValueHandling.Include, "{\"invocationId\":\"123\",\"type\":3,\"result\":{\"StringProp\":\"SignalR!\",\"DoubleProp\":6.2831853071,\"IntProp\":42,\"DateTimeProp\":\"2017-04-11T00:00:00\",\"NullProp\":null}}" },
            new object[] { CompletionMessage.WithResult("123", new CustomObject()), true, NullValueHandling.Include, "{\"invocationId\":\"123\",\"type\":3,\"result\":{\"stringProp\":\"SignalR!\",\"doubleProp\":6.2831853071,\"intProp\":42,\"dateTimeProp\":\"2017-04-11T00:00:00\",\"nullProp\":null}}" },
        };

        [Theory]
        [MemberData(nameof(ProtocolTestData))]
        public async Task WriteMessage(HubMessage message, bool camelCase, NullValueHandling nullValueHandling, string expectedOutput)
        {
            var jsonSerializer = new JsonSerializer
            {
                NullValueHandling = nullValueHandling,
                ContractResolver = camelCase ? new CamelCasePropertyNamesContractResolver() : new DefaultContractResolver()
            };

            var protocol = new JsonHubProtocol(jsonSerializer);
            var encoded = await protocol.WriteToArrayAsync(message);
            var json = Encoding.UTF8.GetString(encoded);

            Assert.Equal(expectedOutput, json);
        }

        [Theory]
        [MemberData(nameof(ProtocolTestData))]
        public void ParseMessage(HubMessage expectedMessage, bool camelCase, NullValueHandling nullValueHandling, string input)
        {
            var jsonSerializer = new JsonSerializer
            {
                NullValueHandling = nullValueHandling,
                ContractResolver = camelCase ? new CamelCasePropertyNamesContractResolver() : new DefaultContractResolver()
            };

            var binder = new TestBinder(expectedMessage);
            var protocol = new JsonHubProtocol(jsonSerializer);
            var message = protocol.ParseMessage(Encoding.UTF8.GetBytes(input), binder);

            Assert.Equal(expectedMessage, message, TestEqualityComparer.Instance);
        }

        [Theory]
        [InlineData("", "Error reading JSON.")]
        [InlineData("null", "Unexpected JSON Token Type 'Null'. Expected a JSON Object.")]
        [InlineData("42", "Unexpected JSON Token Type 'Integer'. Expected a JSON Object.")]
        [InlineData("'foo'", "Unexpected JSON Token Type 'String'. Expected a JSON Object.")]
        [InlineData("[42]", "Unexpected JSON Token Type 'Array'. Expected a JSON Object.")]
        [InlineData("{}", "Missing required property 'type'.")]

        [InlineData("{'type':1}", "Missing required property 'invocationId'.")]
        [InlineData("{'type':1,'invocationId':42}", "Expected 'invocationId' to be of type String.")]
        [InlineData("{'type':1,'invocationId':'42','target':42}", "Expected 'target' to be of type String.")]
        [InlineData("{'type':1,'invocationId':'42','target':'foo'}", "Missing required property 'arguments'.")]
        [InlineData("{'type':1,'invocationId':'42','target':'foo','arguments':{}}", "Expected 'arguments' to be of type Array.")]

        [InlineData("{'type':2}", "Missing required property 'invocationId'.")]
        [InlineData("{'type':2,'invocationId':42}", "Expected 'invocationId' to be of type String.")]
        [InlineData("{'type':2,'invocationId':'42'}", "Missing required property 'item'.")]

        [InlineData("{'type':3}", "Missing required property 'invocationId'.")]
        [InlineData("{'type':3,'invocationId':42}", "Expected 'invocationId' to be of type String.")]
        [InlineData("{'type':3,'invocationId':'42','error':[]}", "Expected 'error' to be of type String.")]

        [InlineData("{'type':4}", "Unknown message type: 4")]
        [InlineData("{'type':'foo'}", "Expected 'type' to be of type Integer.")]
        public void InvalidMessages(string input, string expectedMessage)
        {
            var binder = new TestBinder();
            var protocol = new JsonHubProtocol(new JsonSerializer());
            var ex = Assert.Throws<FormatException>(() => protocol.ParseMessage(Encoding.UTF8.GetBytes(input), binder));
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Theory]
        [InlineData("{'type':1,'invocationId':'42','target':'foo','arguments':[]}", "Invocation provides 0 argument(s) but target expects 2.")]
        [InlineData("{'type':1,'invocationId':'42','target':'foo','arguments':[42, 'foo'],'nonBlocking':42}", "Expected 'nonBlocking' to be of type Boolean.")]
        [InlineData("{'type':3,'invocationId':'42','error':'foo','result':true}", "The 'error' and 'result' properties are mutually exclusive.")]
        public void InvalidMessagesWithBinder(string input, string expectedMessage)
        {
            var binder = new TestBinder(paramTypes: new[] { typeof(int), typeof(string) }, returnType: typeof(bool));
            var protocol = new JsonHubProtocol(new JsonSerializer());
            var ex = Assert.Throws<FormatException>(() => protocol.ParseMessage(Encoding.UTF8.GetBytes(input), binder));
            Assert.Equal(expectedMessage, ex.Message);
        }

        private class CustomObject : IEquatable<CustomObject>
        {
            // Not intended to be a full set of things, just a smattering of sample serializations
            public string StringProp => "SignalR!";

            public double DoubleProp => 6.2831853071;

            public int IntProp => 42;

            public DateTime DateTimeProp => new DateTime(2017, 4, 11);

            public object NullProp => null;

            public override bool Equals(object obj)
            {
                return obj is CustomObject o && Equals(o);
            }

            public override int GetHashCode()
            {
                // This is never used in a hash table
                return 0;
            }

            public bool Equals(CustomObject right)
            {
                // This allows the comparer below to properly compare the object in the test.
                return string.Equals(StringProp, right.StringProp, StringComparison.Ordinal) &&
                    DoubleProp == right.DoubleProp &&
                    IntProp == right.IntProp &&
                    DateTime.Equals(DateTimeProp, right.DateTimeProp) &&
                    NullProp == right.NullProp;
            }
        }

        // Binder that works based on the expected message argument/result types :)
        private class TestBinder : IInvocationBinder
        {
            private readonly Type[] _paramTypes;
            private readonly Type _returnType;

            public TestBinder(HubMessage expectedMessage)
            {
                switch(expectedMessage)
                {
                    case InvocationMessage i:
                        _paramTypes = i.Arguments?.Select(a => a?.GetType() ?? typeof(object))?.ToArray();
                        break;
                    case StreamItemMessage s:
                        _returnType = s.Item?.GetType() ?? typeof(object);
                        break;
                    case CompletionMessage c:
                        _returnType = c.Result?.GetType() ?? typeof(object);
                        break;
                }
            }

            public TestBinder() : this(null, null) { }
            public TestBinder(Type[] paramTypes) : this(paramTypes, null) { }
            public TestBinder(Type returnType) : this(null, returnType) {}
            public TestBinder(Type[] paramTypes, Type returnType)
            {
                _paramTypes = paramTypes;
                _returnType = returnType;
            }

            public Type[] GetParameterTypes(string methodName)
            {
                if (_paramTypes != null)
                {
                    return _paramTypes;
                }
                throw new InvalidOperationException("Unexpected binder call");
            }

            public Type GetReturnType(string invocationId)
            {
                if (_returnType != null)
                {
                    return _returnType;
                }
                throw new InvalidOperationException("Unexpected binder call");
            }
        }

        private class TestEqualityComparer : IEqualityComparer<HubMessage>
        {
            public static readonly TestEqualityComparer Instance = new TestEqualityComparer();

            private TestEqualityComparer() { }

            public bool Equals(HubMessage x, HubMessage y)
            {
                if (!string.Equals(x.InvocationId, y.InvocationId, StringComparison.Ordinal))
                {
                    return false;
                }

                return InvocationMessagesEqual(x, y) || StreamItemMessagesEqual(x, y) || CompletionMessagesEqual(x, y);
            }

            private bool CompletionMessagesEqual(HubMessage x, HubMessage y)
            {
                return x is CompletionMessage left && y is CompletionMessage right &&
                    string.Equals(left.Error, right.Error, StringComparison.Ordinal) &&
                    Equals(left.Result, right.Result) &&
                    left.HasResult == right.HasResult;
            }

            private bool StreamItemMessagesEqual(HubMessage x, HubMessage y)
            {
                return x is StreamItemMessage left && y is StreamItemMessage right &&
                    Equals(left.Item, right.Item);
            }

            private bool InvocationMessagesEqual(HubMessage x, HubMessage y)
            {
                return x is InvocationMessage left && y is InvocationMessage right &&
                    string.Equals(left.Target, right.Target, StringComparison.Ordinal) &&
                    Enumerable.SequenceEqual(left.Arguments, right.Arguments) &&
                    left.NonBlocking == right.NonBlocking;
            }

            public int GetHashCode(HubMessage obj)
            {
                // We never use these in a hash-table
                return 0;
            }
        }
    }
}
