// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Text.RegularExpressions;

namespace Microsoft.TestCommon
{
    public class RegexReplacement
    {
        Regex regex;
        string replacement;

        public RegexReplacement(Regex regex, string replacement)
        {
            this.regex = regex;
            this.replacement = replacement;
        }

        public RegexReplacement(string regex, string replacement)
        {
            this.regex = new Regex(regex);
            this.replacement = replacement;
        }

        public Regex Regex
        {
            get
            {
                return this.regex;
            }
        }

        public string Replacement
        {
            get
            {
                return this.replacement;
            }
        }
    }
}
