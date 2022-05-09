// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.CommandLineUtils;

internal sealed class CommandOption
{
    public CommandOption(string template, CommandOptionType optionType)
    {
        Template = template;
        OptionType = optionType;
        Values = new List<string>();

        foreach (var part in Template.Split(new[] { ' ', '|' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.StartsWith("--", StringComparison.Ordinal))
            {
                LongName = part.Substring(2);
            }
            else if (part.StartsWith("-", StringComparison.Ordinal))
            {
                var optName = part.Substring(1);

                // If there is only one char and it is not an English letter, it is a symbol option (e.g. "-?")
                if (optName.Length == 1 && !IsEnglishLetter(optName[0]))
                {
                    SymbolName = optName;
                }
                else
                {
                    ShortName = optName;
                }
            }
            else if (part.StartsWith("<", StringComparison.Ordinal) && part.EndsWith(">", StringComparison.Ordinal))
            {
                ValueName = part.Substring(1, part.Length - 2);
            }
            else
            {
                throw new ArgumentException($"Invalid template pattern '{template}'", nameof(template));
            }
        }

        if (string.IsNullOrEmpty(LongName) && string.IsNullOrEmpty(ShortName) && string.IsNullOrEmpty(SymbolName))
        {
            throw new ArgumentException($"Invalid template pattern '{template}'", nameof(template));
        }
    }

    public string Template { get; set; }
    public string ShortName { get; set; }
    public string LongName { get; set; }
    public string SymbolName { get; set; }
    public string ValueName { get; set; }
    public string Description { get; set; }
    public List<string> Values { get; private set; }
    public CommandOptionType OptionType { get; private set; }
    public bool ShowInHelpText { get; set; } = true;
    public bool Inherited { get; set; }

    public bool TryParse(string value)
    {
        switch (OptionType)
        {
            case CommandOptionType.MultipleValue:
                Values.Add(value);
                break;
            case CommandOptionType.SingleValue:
                if (Values.Any())
                {
                    return false;
                }
                Values.Add(value);
                break;
            case CommandOptionType.NoValue:
                if (value != null)
                {
                    return false;
                }
                // Add a value to indicate that this option was specified
                Values.Add("on");
                break;
            default:
                break;
        }
        return true;
    }

    public bool HasValue()
    {
        return Values.Any();
    }

    public string Value()
    {
        return HasValue() ? Values[0] : null;
    }

    private static bool IsEnglishLetter(char c)
    {
        return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
    }
}
