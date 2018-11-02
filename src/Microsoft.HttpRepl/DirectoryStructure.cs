// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.HttpRepl
{
    public class DirectoryStructure : IDirectoryStructure
    {
        private readonly Dictionary<string, DirectoryStructure> _childDirectories = new Dictionary<string, DirectoryStructure>(StringComparer.OrdinalIgnoreCase);

        public DirectoryStructure(IDirectoryStructure parent)
        {
            Parent = parent;
        }

        public IEnumerable<string> DirectoryNames => _childDirectories.Keys;

        public IDirectoryStructure Parent { get; }

        public DirectoryStructure DeclareDirectory(string name)
        {
            if (_childDirectories.TryGetValue(name, out DirectoryStructure existing))
            {
                return existing;
            }

            return _childDirectories[name] = new DirectoryStructure(this);
        }

        public IDirectoryStructure GetChildDirectory(string name)
        {
            if (_childDirectories.TryGetValue(name, out DirectoryStructure result))
            {
                return result;
            }

            IDirectoryStructure parameterizedTarget = _childDirectories.FirstOrDefault(x => x.Key.StartsWith('{') && x.Key.EndsWith('}')).Value;

            if (!(parameterizedTarget is null))
            {
                return parameterizedTarget;
            }

            return new DirectoryStructure(this);
        }

        public IRequestInfo RequestInfo { get; set;  }
    }

    public class RequestInfo : IRequestInfo
    {
        private readonly HashSet<string> _methods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Dictionary<string, string>> _requestBodiesByMethodByContentType = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _fallbackBodyStringsByMethod = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _fallbackContentTypeStringsByMethod = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, IReadOnlyList<string>> _contentTypesByMethod = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<string> Methods => _methods.ToList();

        public IReadOnlyDictionary<string, IReadOnlyList<string>> ContentTypesByMethod => _contentTypesByMethod;

        public string GetRequestBodyForContentType(ref string contentType, string method)
        {
            if (_requestBodiesByMethodByContentType.TryGetValue(method, out Dictionary<string, string> bodiesByContentType)
                && bodiesByContentType.TryGetValue(contentType, out string body))
            {
                return body;
            }

            if (_fallbackBodyStringsByMethod.TryGetValue(method, out body))
            {
                if (_fallbackContentTypeStringsByMethod.TryGetValue(method, out string newContentType))
                {
                    contentType = newContentType;
                }

                return body;
            }

            return null;
        }

        public void SetRequestBody(string method, string contentType, string body)
        {
            if (!_requestBodiesByMethodByContentType.TryGetValue(method, out Dictionary<string, string> bodiesByContentType))
            {
                _requestBodiesByMethodByContentType[method] = bodiesByContentType = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            if (!_contentTypesByMethod.TryGetValue(method, out IReadOnlyList<string> contentTypesRaw))
            {
                _contentTypesByMethod[method] = contentTypesRaw = new List<string>();
            }

            List<string> contentTypes = (List<string>)contentTypesRaw;
            contentTypes.Add(contentType);

            bodiesByContentType[contentType] = body;
        }

        public void AddMethod(string method)
        {
            _methods.Add(method);
        }

        public void SetFallbackRequestBody(string method, string contentType, string fallbackBodyString)
        {
            _fallbackBodyStringsByMethod[method] = fallbackBodyString;
            _fallbackContentTypeStringsByMethod[method] = contentType;
        }
    }
}
