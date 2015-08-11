// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using System.Text;
using System.Threading.Tasks;

namespace LargeResponseApp
{
    public class Startup
    {
        private const int _chunkSize = 4096;
        private const int _defaultNumChunks = 16;
        private static byte[] _chunk = Encoding.UTF8.GetBytes(new string('a', _chunkSize));
        private static Task _emptyTask = Task.FromResult<object>(null);

        public void Configure(IApplicationBuilder app)
        {
            app.Run(async (context) =>
            {
                int numChunks;
                var path = context.Request.Path;
                if (!path.HasValue || !int.TryParse(path.Value.Substring(1), out numChunks))
                {
                    numChunks = _defaultNumChunks;
                }

                context.Response.ContentLength = _chunkSize * numChunks;
                context.Response.ContentType = "text/plain";

                for (int i = 0; i < numChunks; i++)
                {
                    await context.Response.Body.WriteAsync(_chunk, 0, _chunkSize).ConfigureAwait(false);
                }
            });
        }
    }
}
