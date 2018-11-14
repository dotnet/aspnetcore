using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Server.IntegrationTesting;

namespace Microsoft.AspNetCore.WebSockets.ConformanceTest.Autobahn
{
    public class AutobahnExpectations
    {
        private Dictionary<string, Expectation> _expectations = new Dictionary<string, Expectation>();
        public bool Ssl { get; }
        public ServerType Server { get; }
        public string Environment { get; }

        public AutobahnExpectations(ServerType server, bool ssl, string environment)
        {
            Server = server;
            Ssl = ssl;
            Environment = environment;
        }

        public AutobahnExpectations Fail(params string[] caseSpecs) => Expect(Expectation.Fail, caseSpecs);
        public AutobahnExpectations NonStrict(params string[] caseSpecs) => Expect(Expectation.NonStrict, caseSpecs);
        public AutobahnExpectations OkOrFail(params string[] caseSpecs) => Expect(Expectation.OkOrFail, caseSpecs);

        public AutobahnExpectations Expect(Expectation expectation, params string[] caseSpecs)
        {
            foreach (var caseSpec in caseSpecs)
            {
                _expectations[caseSpec] = expectation;
            }
            return this;
        }

        internal void Verify(AutobahnServerResult serverResult, StringBuilder failures)
        {
            foreach (var caseResult in serverResult.Cases)
            {
                // If this is an informational test result, we can't compare it to anything
                if (!string.Equals(caseResult.ActualBehavior, "INFORMATIONAL", StringComparison.Ordinal))
                {
                    Expectation expectation;
                    if (!_expectations.TryGetValue(caseResult.Name, out expectation))
                    {
                        expectation = Expectation.Ok;
                    }

                    switch (expectation)
                    {
                        case Expectation.Fail:
                            if (!caseResult.BehaviorIs("FAILED"))
                            {
                                failures.AppendLine($"Case {serverResult.Name}:{caseResult.Name}. Expected 'FAILED', but got '{caseResult.ActualBehavior}'");
                            }
                            break;
                        case Expectation.NonStrict:
                            if (!caseResult.BehaviorIs("NON-STRICT"))
                            {
                                failures.AppendLine($"Case {serverResult.Name}:{caseResult.Name}. Expected 'NON-STRICT', but got '{caseResult.ActualBehavior}'");
                            }
                            break;
                        case Expectation.Ok:
                            if (!caseResult.BehaviorIs("NON-STRICT") && !caseResult.BehaviorIs("OK"))
                            {
                                failures.AppendLine($"Case {serverResult.Name}:{caseResult.Name}. Expected 'NON-STRICT' or 'OK', but got '{caseResult.ActualBehavior}'");
                            }
                            break;
                        case Expectation.OkOrFail:
                            if (!caseResult.BehaviorIs("NON-STRICT") && !caseResult.BehaviorIs("FAILED") && !caseResult.BehaviorIs("OK"))
                            {
                                failures.AppendLine($"Case {serverResult.Name}:{caseResult.Name}. Expected 'FAILED', 'NON-STRICT' or 'OK', but got '{caseResult.ActualBehavior}'");
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }
}