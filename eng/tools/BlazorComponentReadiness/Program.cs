// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BlazorComponentReadiness;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine(
                "Usage: BlazorComponentReadiness " +
                "<scorecard|tracker|ledger|receipt|revision|validate-agent> [options]");
            return 1;
        }

        return args[0] switch
        {
            "scorecard" => ScorecardCommand.Run(args[1..], Console.Out, Console.Error),
            "tracker" => TrackerCommand.Run(args[1..], Console.Out, Console.Error),
            "ledger" => EvidenceLedgerCommand.Run(args[1..], Console.Out, Console.Error),
            "receipt" => ReceiptCommand.Run(args[1..], Console.Out, Console.Error),
            "revision" => RevisionCommand.Run(args[1..], Console.Out, Console.Error),
            "validate-agent" => AgentValidationCommand.Run(args[1..], Console.Out, Console.Error),
            _ => UnknownCommand(args[0]),
        };
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine(
            $"Unknown command '{command}'. Expected 'scorecard', 'tracker', 'ledger', " +
            "'receipt', 'revision', or 'validate-agent'.");
        return 1;
    }
}
