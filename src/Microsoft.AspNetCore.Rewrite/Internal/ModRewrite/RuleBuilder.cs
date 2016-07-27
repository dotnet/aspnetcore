// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Rewrite.Internal.ModRewrite
{
    public class RuleBuilder
    {
        private ParsedModRewriteExpression _pce;
        private List<Condition> _conditions;
        private RuleFlags _flags;
        private Pattern _patterns;
        public ModRewriteRule Build()
        {
            var ruleExpression = ExpressionCreator.CreateRuleExpression(_pce, _flags);
            return new ModRewriteRule(_conditions, ruleExpression, _patterns, _flags);
        }

        public RuleBuilder(string initialRule, string transformation) : 
            this(initialRule, transformation, flags: null)
        {
        }
        public RuleBuilder(string rule)
        {
            var tokens = Tokenizer.Tokenize(rule);
            if (tokens.Count == 3)
            {
                CreateRule(tokens[1], tokens[2], flags: null);
            }
            else if (tokens.Count == 4)
            {
                CreateRule(tokens[1], tokens[2], tokens[3]);
            }
            else
            {
                throw new ArgumentException();
            }
        }

        public RuleBuilder(string initialRule, string transformation, string flags)
        {
            CreateRule(initialRule, transformation, flags);
        }

        public void CreateRule(string initialRule, string transformation, string flags)
        {
            _pce = RuleRegexParser.ParseRuleRegex(initialRule);
            _patterns = ConditionTestStringParser.ParseConditionTestString(transformation);
            _flags = FlagParser.ParseRuleFlags(flags);
        }

        public void AddCondition(string condition)
        {
            if (_conditions == null)
            {
                _conditions = new List<Condition>();
            }
            var condBuilder = new ConditionBuilder(condition);
            _conditions.Add(condBuilder.Build());
        }

        public void AddCondition(Condition condition)
        {
            if (_conditions == null)
            {
                _conditions = new List<Condition>();
            }
            _conditions.Add(condition);
        }
        
        public void AddConditions(List<Condition> conditions)
        {
            if (_conditions == null)
            {
                _conditions = new List<Condition>();
            }
            _conditions.AddRange(conditions);
        }

        public void SetFlag(string flag)
        {
            SetFlag(flag, value: null);
        }
        
        public void SetFlag(RuleFlagType flag)
        {
            SetFlag(flag, value: null);
        }

        public void SetFlag(string flag, string value)
        {
            if (_flags == null)
            {
                _flags = new RuleFlags();
            }
            _flags.SetFlag(flag, value);
        }

        public void SetFlag(RuleFlagType flag, string value)
        {
            if (_flags == null)
            {
                _flags = new RuleFlags();
            }
            _flags.SetFlag(flag, value);
        }

        public void SetFlags(string flags)
        {
            if (_flags == null)
            {
                _flags = FlagParser.ParseRuleFlags(flags);
            }
            else
            {
                FlagParser.ParseRuleFlags(flags, _flags);
            }
        }
    }
}
