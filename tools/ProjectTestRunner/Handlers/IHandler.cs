using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using ProjectTestRunner.HandlerResults;

namespace ProjectTestRunner.Handlers
{
    public interface IHandler
    {
        string HandlerName { get; }

        IHandlerResult Execute(IReadOnlyDictionary<string, string> tokens, IReadOnlyList<IHandlerResult> results, JObject json);

        string Summarize(IReadOnlyDictionary<string, string> tokens, JObject json);
    }
}
