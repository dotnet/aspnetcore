// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Rewrite.Internal.ModRewrite
{
    public class ConditionBuilder
    {
        private Pattern _testString;
        private ParsedModRewriteExpression _pce;
        private ConditionFlags _flags;

        public ConditionBuilder(string conditionString)
        {
            var tokens = Tokenizer.Tokenize(conditionString);
            if (tokens.Count == 3)
            {
                CreateCondition(tokens[1], tokens[2], flagsString: null);
            }
            else if (tokens.Count == 4)
            {
                CreateCondition(tokens[1], tokens[2], tokens[3]);
            }
            else
            {
                throw new FormatException("Invalid number of tokens.");
            }
        }

        public ConditionBuilder(string testString, string condition)
        {
            CreateCondition(testString, condition, flagsString: null);
        }

        public ConditionBuilder(string testString, string condition, string flags)
        {
            CreateCondition(testString, condition, flags);
        }

        public Condition Build()
        {
            var expression = ExpressionCreator.CreateConditionExpression(_pce, _flags);
            return new Condition(_testString, expression, _flags);
        }

        private void CreateCondition(string testString, string condition, string flagsString)
        {
            _testString = ConditionTestStringParser.ParseConditionTestString(testString);
            _pce = ConditionPatternParser.ParseActionCondition(condition);
            _flags = FlagParser.ParseConditionFlags(flagsString);
        }

        public void SetFlag(string flag)
        {
            SetFlag(flag, value: null);
        }

        public void SetFlag(ConditionFlagType flag)
        {
            SetFlag(flag, value: null);
        }

        public void SetFlag(string flag, string value)
        {
            if (_flags == null)
            {
                _flags = new ConditionFlags();
            }
            _flags.SetFlag(flag, value);
        }

        public void SetFlag(ConditionFlagType flag, string value)
        {
            if (_flags == null)
            {
                _flags = new ConditionFlags();
            }
            _flags.SetFlag(flag, value);
        }

        public void SetFlags(string flags)
        {
            if (_flags == null)
            {
                _flags = FlagParser.ParseConditionFlags(flags);
            }
            else
            {
                FlagParser.ParseConditionFlags(flags, _flags);
            }
        }
    }
}
