// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;

namespace Cli.FunctionalTests.Util
{
    public class ConcurrentStringBuilder
    {
        private StringBuilder _stringBuilder = new StringBuilder();
        private object _lock = new object();

        public void AppendLine()
        {
            lock (_lock)
            {
                _stringBuilder.AppendLine();
            }
        }

        public void AppendLine(string data)
        {
            lock (_lock)
            {
                _stringBuilder.AppendLine(data);
            }
        }

        public override string ToString()
        {
            lock (_lock)
            {
                return _stringBuilder.ToString();
            }
        }
    }
}
