using System.Runtime.ExceptionServices;

namespace Microsoft.AspNetCore.SignalR.Protocol
{
    /// <summary>
    /// Represents a failure to bind arguments for an invocation. This does not represent an actual
    /// message that is sent on the wire, it is returned by <see cref="IHubProtocol.TryParseMessage"/>
    /// to indicate that a binding failure occurred when parsing an invocation. The invocation ID is associated
    /// so that the error can be sent back to the client, associated with the appropriate invocation ID.
    /// </summary>
    public class InvocationBindingFailureMessage : HubInvocationMessage
    {
        public ExceptionDispatchInfo BindingFailure { get; }
        public string Target { get; }

        public InvocationBindingFailureMessage(string invocationId, string target, ExceptionDispatchInfo bindingFailure) : base(invocationId)
        {
            Target = target;
            BindingFailure = bindingFailure;
        }
    }
}
