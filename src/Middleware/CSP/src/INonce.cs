using System;

namespace Microsoft.AspNetCore.Csp
{
    public interface INonce
    {
        string GetValue();
    }

    public class Nonce : INonce
    {
        private readonly string _value;
        // TODO: Make sure we use an actually crypto-safe random generator.
        private static readonly Lazy<Random> _gen = new Lazy<Random>(() => new Random());

        public Nonce()
        {
            // TODO: Actually come up with an alphanumeric string of enough entropy rather than casting a random int to string.
            _value = _gen.Value.Next().ToString();
        }

        public string GetValue()
        {
            return _value;
        }
    }
}
