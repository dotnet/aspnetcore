using System;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.WebUtilities
{
    /// <summary>
    /// A helper class for constructing encoded Uris for use in headers and other Uris.
    /// </summary>
    public class UriHelper
    {
        public UriHelper()
        {
        }

        public UriHelper(HttpRequest request)
        {
            Scheme = request.Scheme;
            Host = request.Host;
            PathBase = request.PathBase;
            Path = request.Path;
            Query = request.QueryString;
            // Fragment is not a valid request field.
        }

        public UriHelper(Uri uri)
        {
            Scheme = uri.Scheme;
            Host = HostString.FromUriComponent(uri);
            // Assume nothing is being put in PathBase
            Path = PathString.FromUriComponent(uri);
            Query = QueryString.FromUriComponent(uri);
            Fragment = FragmentString.FromUriComponent(uri);
        }

        public string Scheme { get; set; }

        public HostString Host { get; set; }

        public PathString PathBase { get; set; }

        public PathString Path { get; set; }

        public QueryString Query { get; set; }

        public FragmentString Fragment { get; set; }

        // Always returns at least '/'
        public string GetPartialUri()
        {
            string path = (PathBase.HasValue || Path.HasValue) ? (PathBase + Path).ToString() : "/";
            return path + Query + Fragment;
        }

        // Always returns at least 'scheme://host/'
        public string GetFullUri()
        {
            if (string.IsNullOrEmpty(Scheme))
            {
                throw new InvalidOperationException("Missing Scheme");
            }
            if (!Host.HasValue)
            {
                throw new InvalidOperationException("Missing Host");
            }

            string path = (PathBase.HasValue || Path.HasValue) ? (PathBase + Path).ToString() : "/";
            return Scheme + "://" + Host + path + Query + Fragment;
        }

        public static string Create(PathString pathBase,
            PathString path = new PathString(),
            QueryString query = new QueryString(),
            FragmentString fragment = new FragmentString())
        {
            return new UriHelper()
            {
                PathBase = pathBase,
                Path = path,
                Query = query,
                Fragment = fragment
            }.GetPartialUri();
        }

        public static string Create(string scheme,
            HostString host,
            PathString pathBase = new PathString(),
            PathString path = new PathString(),
            QueryString query = new QueryString(),
            FragmentString fragment = new FragmentString())
        {
            return new UriHelper()
            {
                Scheme = scheme,
                Host = host,
                PathBase = pathBase,
                Path = path,
                Query = query,
                Fragment = fragment
            }.GetFullUri();
        }
    }
}