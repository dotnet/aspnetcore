// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Diagnostics.Views
{
    /// <summary>
    /// Infrastructure
    /// </summary>
    public abstract class BaseView
    {
        /// <summary>
        /// The request context
        /// </summary>
        protected HttpContext Context { get; private set; }

        /// <summary>
        /// The request
        /// </summary>
        protected HttpRequest Request { get; private set; }

        /// <summary>
        /// The response
        /// </summary>
        protected HttpResponse Response { get; private set; }

        /// <summary>
        /// The output stream
        /// </summary>
        protected StreamWriter Output { get; private set; }

        /// <summary>
        /// Execute an individual request
        /// </summary>
        /// <param name="context"></param>
        public async Task ExecuteAsync(HttpContext context)
        {
            Context = context;
            Request = Context.Request;
            Response = Context.Response;
            Output = new StreamWriter(Response.Body);
            await ExecuteAsync();
            Output.Dispose();
        }

        /// <summary>
        /// Execute an individual request
        /// </summary>
        public abstract Task ExecuteAsync();

        /// <summary>
        /// Write the given value directly to the output
        /// </summary>
        /// <param name="value"></param>
        protected void WriteLiteral(string value)
        {
            Output.Write(value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <param name="name"></param>
        /// <param name="leader"></param>
        /// <param name="trailer"></param>
        /// <param name="part1"></param>
        protected void WriteAttribute<T1>(
            string name,
            Tuple<string, int> leader,
            Tuple<string, int> trailer,
            Tuple<Tuple<string, int>, Tuple<T1, int>, bool> part1)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (leader == null)
            {
                throw new ArgumentNullException("leader");
            }
            if (trailer == null)
            {
                throw new ArgumentNullException("trailer");
            }
            if (part1 == null)
            {
                throw new ArgumentNullException("part1");
            }
            WriteLiteral(leader.Item1);
            WriteLiteral(part1.Item1.Item1);
            Write(part1.Item2.Item1);
            WriteLiteral(trailer.Item1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="name"></param>
        /// <param name="leader"></param>
        /// <param name="trailer"></param>
        /// <param name="part1"></param>
        /// <param name="part2"></param>
        protected void WriteAttribute<T1, T2>(
            string name,
            Tuple<string, int> leader,
            Tuple<string, int> trailer,
            Tuple<Tuple<string, int>, Tuple<T1, int>, bool> part1,
            Tuple<Tuple<string, int>, Tuple<T2, int>, bool> part2)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (leader == null)
            {
                throw new ArgumentNullException("leader");
            }
            if (trailer == null)
            {
                throw new ArgumentNullException("trailer");
            }
            if (part1 == null)
            {
                throw new ArgumentNullException("part1");
            }
            if (part2 == null)
            {
                throw new ArgumentNullException("part2");
            }
            WriteLiteral(leader.Item1);
            WriteLiteral(part1.Item1.Item1);
            Write(part1.Item2.Item1);
            WriteLiteral(part2.Item1.Item1);
            Write(part2.Item2.Item1);
            WriteLiteral(trailer.Item1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <param name="name"></param>
        /// <param name="leader"></param>
        /// <param name="trailer"></param>
        /// <param name="part1"></param>
        /// <param name="part2"></param>
        /// <param name="part3"></param>
        protected void WriteAttribute<T1, T2, T3>(
            string name,
            Tuple<string, int> leader,
            Tuple<string, int> trailer,
            Tuple<Tuple<string, int>, Tuple<T1, int>, bool> part1,
            Tuple<Tuple<string, int>, Tuple<T2, int>, bool> part2,
            Tuple<Tuple<string, int>, Tuple<T3, int>, bool> part3)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (leader == null)
            {
                throw new ArgumentNullException("leader");
            }
            if (trailer == null)
            {
                throw new ArgumentNullException("trailer");
            }
            if (part1 == null)
            {
                throw new ArgumentNullException("part1");
            }
            if (part2 == null)
            {
                throw new ArgumentNullException("part2");
            }
            if (part3 == null)
            {
                throw new ArgumentNullException("part3");
            }
            WriteLiteral(leader.Item1);
            WriteLiteral(part1.Item1.Item1);
            Write(part1.Item2.Item1);
            WriteLiteral(part2.Item1.Item1);
            Write(part2.Item2.Item1);
            WriteLiteral(part3.Item1.Item1);
            Write(part3.Item2.Item1);
            WriteLiteral(trailer.Item1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <param name="name"></param>
        /// <param name="leader"></param>
        /// <param name="trailer"></param>
        /// <param name="part1"></param>
        /// <param name="part2"></param>
        /// <param name="part3"></param>
        /// <param name="part4"></param>
        protected void WriteAttribute<T1, T2, T3, T4>(
            string name,
            Tuple<string, int> leader,
            Tuple<string, int> trailer,
            Tuple<Tuple<string, int>, Tuple<T1, int>, bool> part1,
            Tuple<Tuple<string, int>, Tuple<T2, int>, bool> part2,
            Tuple<Tuple<string, int>, Tuple<T3, int>, bool> part3,
            Tuple<Tuple<string, int>, Tuple<T4, int>, bool> part4)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (leader == null)
            {
                throw new ArgumentNullException("leader");
            }
            if (trailer == null)
            {
                throw new ArgumentNullException("trailer");
            }
            if (part1 == null)
            {
                throw new ArgumentNullException("part1");
            }
            if (part2 == null)
            {
                throw new ArgumentNullException("part2");
            }
            if (part3 == null)
            {
                throw new ArgumentNullException("part3");
            }
            if (part4 == null)
            {
                throw new ArgumentNullException("part4");
            }
            WriteLiteral(leader.Item1);
            WriteLiteral(part1.Item1.Item1);
            Write(part1.Item2.Item1);
            WriteLiteral(part2.Item1.Item1);
            Write(part2.Item2.Item1);
            WriteLiteral(part3.Item1.Item1);
            Write(part3.Item2.Item1);
            WriteLiteral(part4.Item1.Item1);
            Write(part4.Item2.Item1);
            WriteLiteral(trailer.Item1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <param name="name"></param>
        /// <param name="leader"></param>
        /// <param name="trailer"></param>
        /// <param name="part1"></param>
        /// <param name="part2"></param>
        /// <param name="part3"></param>
        /// <param name="part4"></param>
        /// <param name="part5"></param>
        protected void WriteAttribute<T1, T2, T3, T4, T5>(
            string name,
            Tuple<string, int> leader,
            Tuple<string, int> trailer,
            Tuple<Tuple<string, int>, Tuple<T1, int>, bool> part1,
            Tuple<Tuple<string, int>, Tuple<T2, int>, bool> part2,
            Tuple<Tuple<string, int>, Tuple<T3, int>, bool> part3,
            Tuple<Tuple<string, int>, Tuple<T4, int>, bool> part4,
            Tuple<Tuple<string, int>, Tuple<T5, int>, bool> part5)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (leader == null)
            {
                throw new ArgumentNullException("leader");
            }
            if (trailer == null)
            {
                throw new ArgumentNullException("trailer");
            }
            if (part1 == null)
            {
                throw new ArgumentNullException("part1");
            }
            if (part2 == null)
            {
                throw new ArgumentNullException("part2");
            }
            if (part3 == null)
            {
                throw new ArgumentNullException("part3");
            }
            if (part4 == null)
            {
                throw new ArgumentNullException("part4");
            }
            if (part5 == null)
            {
                throw new ArgumentNullException("part5");
            }            
            WriteLiteral(leader.Item1);
            WriteLiteral(part1.Item1.Item1);
            Write(part1.Item2.Item1);
            WriteLiteral(part2.Item1.Item1);
            Write(part2.Item2.Item1);
            WriteLiteral(part3.Item1.Item1);
            Write(part3.Item2.Item1);
            WriteLiteral(part4.Item1.Item1);
            Write(part4.Item2.Item1);
            WriteLiteral(part5.Item1.Item1);
            Write(part5.Item2.Item1);
            WriteLiteral(trailer.Item1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <typeparam name="T6"></typeparam>
        /// <param name="name"></param>
        /// <param name="leader"></param>
        /// <param name="trailer"></param>
        /// <param name="part1"></param>
        /// <param name="part2"></param>
        /// <param name="part3"></param>
        /// <param name="part4"></param>
        /// <param name="part5"></param>
        /// <param name="part6"></param>
        protected void WriteAttribute<T1, T2, T3, T4, T5, T6>(
            string name,
            Tuple<string, int> leader,
            Tuple<string, int> trailer,
            Tuple<Tuple<string, int>, Tuple<T1, int>, bool> part1,
            Tuple<Tuple<string, int>, Tuple<T2, int>, bool> part2,
            Tuple<Tuple<string, int>, Tuple<T3, int>, bool> part3,
            Tuple<Tuple<string, int>, Tuple<T4, int>, bool> part4,
            Tuple<Tuple<string, int>, Tuple<T5, int>, bool> part5,
            Tuple<Tuple<string, int>, Tuple<T6, int>, bool> part6)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (leader == null)
            {
                throw new ArgumentNullException("leader");
            }
            if (trailer == null)
            {
                throw new ArgumentNullException("trailer");
            }
            if (part1 == null)
            {
                throw new ArgumentNullException("part1");
            }
            if (part2 == null)
            {
                throw new ArgumentNullException("part2");
            }
            if (part3 == null)
            {
                throw new ArgumentNullException("part3");
            }
            if (part4 == null)
            {
                throw new ArgumentNullException("part4");
            }
            if (part5 == null)
            {
                throw new ArgumentNullException("part5");
            }
            if (part6 == null)
            {
                throw new ArgumentNullException("part6");
            }
            WriteLiteral(leader.Item1);
            WriteLiteral(part1.Item1.Item1);
            Write(part1.Item2.Item1);
            WriteLiteral(part2.Item1.Item1);
            Write(part2.Item2.Item1);
            WriteLiteral(part3.Item1.Item1);
            Write(part3.Item2.Item1);
            WriteLiteral(part4.Item1.Item1);
            Write(part4.Item2.Item1);
            WriteLiteral(part5.Item1.Item1);
            Write(part5.Item2.Item1);
            WriteLiteral(part6.Item1.Item1);
            Write(part6.Item2.Item1);
            WriteLiteral(trailer.Item1);
        }

        /// <summary>
        /// Html encode and write
        /// </summary>
        /// <param name="value"></param>
        private void WriteEncoded(string value)
        {
            Output.Write(WebUtility.HtmlEncode(value));
        }

        /// <summary>
        /// Convert to string and html encode
        /// </summary>
        /// <param name="value"></param>
        protected void Write(object value)
        {
            WriteEncoded(Convert.ToString(value, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Html encode and write
        /// </summary>
        /// <param name="value"></param>
        protected void Write(string value)
        {
            WriteEncoded(value);
        }
    }
}
