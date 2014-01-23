using System;
using Shouldly;
using Xunit;

namespace Microsoft.AspNet.FeatureModel.Tests
{
    public interface IThing
    {
        string Hello();
    }

    public class Thing : IThing
    {
        public string Hello()
        {
            return "World";
        }
    }

    public class InterfaceDictionaryTests
    {
        [Fact]
        public void AddedInterfaceIsReturned()
        {
            var interfaces = new InterfaceDictionary();
            var thing = new Thing();

            interfaces.Add(typeof(IThing), thing);

            interfaces[typeof(IThing)].ShouldBe(thing);

            object thing2;
            interfaces.TryGetValue(typeof(IThing), out thing2).ShouldBe(true);
            thing2.ShouldBe(thing);
        }

        [Fact]
        public void IndexerAlsoAddsItems()
        {
            var interfaces = new InterfaceDictionary();
            var thing = new Thing();

            interfaces[typeof(IThing)] = thing;

            interfaces[typeof(IThing)].ShouldBe(thing);

            object thing2;
            interfaces.TryGetValue(typeof(IThing), out thing2).ShouldBe(true);
            thing2.ShouldBe(thing);
        }

        [Fact]
        public void SecondCallToAddThrowsException()
        {
            var interfaces = new InterfaceDictionary();
            var thing = new Thing();

            interfaces.Add(typeof(IThing), thing);

            Should.Throw<ArgumentException>(() => interfaces.Add(typeof(IThing), thing));
        }
    }
}
