namespace Microsoft.AspNetCore.WebSockets.Server.Test.Autobahn
{
    public enum Expectation
    {
        Fail,
        NonStrict,
        OkOrFail,
        Ok,
        OkOrNonStrict
    }
}