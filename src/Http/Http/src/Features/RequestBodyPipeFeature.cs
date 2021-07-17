// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// Default implementation for <see cref="IRequestBodyPipeFeature"/>.
    /// </summary>
    public class RequestBodyPipeFeature : IRequestBodyPipeFeature
    {
        private PipeReader? _internalPipeReader;
        private Stream? _streamInstanceWhenWrapped;
        private readonly HttpContext _context;

        /// <summary>
        /// Initializes a new instance of <see cref="IRequestBodyPipeFeature"/>.
        /// </summary>
        /// <param name="context"></param>
        public RequestBodyPipeFeature(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            _context = context;
        }

        /// <inheritdoc />
        public PipeReader Reader
        {
            get
            {
                if (_internalPipeReader == null ||
                    !ReferenceEquals(_streamInstanceWhenWrapped, _context.Request.Body))
                {
                    _streamInstanceWhenWrapped = _context.Request.Body;
                    _internalPipeReader = PipeReader.Create(_context.Request.Body);

                    _context.Response.OnCompleted((self) =>
                    {
                        ((PipeReader)self).Complete();
                        return Task.CompletedTask;
                    }, _internalPipeReader);
                }

                return _internalPipeReader;
            }
        }
    }
}
