// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.HttpRepl.OpenApi
{
    public static class PointerUtil
    {
        public static Task<JToken> ResolvePointersAsync(Uri loadLocation, JToken root, HttpClient client)
        {
            return ResolvePointersAsync(loadLocation, root, root, client);
        }

        private static async Task<JToken> ResolvePointersAsync(Uri loadLocation, JToken root, JToken toResolve, HttpClient client)
        {
            JToken cursor = root;

            if (toResolve is JArray arr)
            {
                for (int i = 0; i < arr.Count; ++i)
                {
                    arr[i] = await ResolvePointersAsync(loadLocation, root, arr[i], client).ConfigureAwait(false);
                }
            }
            else if (toResolve is JObject obj)
            {
                if (obj["$ref"] is JValue refVal && refVal.Type == JTokenType.String)
                {
                    if (!Uri.TryCreate((string)refVal.Value, UriKind.RelativeOrAbsolute, out Uri loadTarget))
                    {
                        //TODO: Error resolving pointer (pointer must be a valid URI)
                        return new JValue((object)null);
                    }
                    
                    if (!loadTarget.IsAbsoluteUri)
                    {
                        if (!Uri.TryCreate(loadLocation, loadTarget, out loadTarget))
                        {
                            //TODO: Error resolving pointer (could not combine with base path)
                            return new JValue((object)null);
                        }
                    }

                    //Check to see if we're changing source documents, if we are, get it
                    if (!string.Equals(loadLocation.Host, loadTarget.Host, StringComparison.OrdinalIgnoreCase) || !string.Equals(loadLocation.AbsolutePath, loadTarget.AbsolutePath, StringComparison.OrdinalIgnoreCase))
                    {
                        HttpResponseMessage responseMessage = await client.GetAsync(loadTarget).ConfigureAwait(false);

                        if (!responseMessage.IsSuccessStatusCode)
                        {
                            //TODO: Error resolving pointer (could not get referenced document)
                            return new JValue((object)null);
                        }

                        string responseString = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                        JToken newRoot;

                        try
                        {
                            newRoot = JToken.Parse(responseString);
                        }
                        catch
                        {
                            //TODO: Error resolving pointer (referenced document is not valid JSON)
                            return new JValue((object)null);
                        }

                        cursor = await ResolvePointersAsync(loadTarget, newRoot, newRoot, client).ConfigureAwait(false);
                    }

                    //We're in the right document, grab the bookmark (fragment) of the URI and get the element at that path
                    string fragment = loadTarget.Fragment;

                    if (fragment.StartsWith('#'))
                    {
                        fragment = fragment.Substring(1);
                    }

                    string[] parts = fragment.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                    for (int i = 0; i < parts.Length; ++i)
                    {
                        if (cursor is JArray ca)
                        {
                            if (!int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
                            {
                                //TODO: Error resolving pointer, array index is non-integral
                                return new JValue((object)null);
                            }

                            if (index < 0 || index >= ca.Count)
                            {
                                //TODO: Error resolving pointer, array index is out of bounds
                                return new JValue((object)null);
                            }

                            JToken val = ca[index];
                            if (val is JObject vo && vo.TryGetValue("$ref", out JToken vor) && vor is JValue vorv && vorv.Type == JTokenType.String)
                            {
                                cursor = await ResolvePointersAsync(loadLocation, root, val, client).ConfigureAwait(false);
                            }
                            else
                            {
                                cursor = val;
                            }
                        }
                        else if (cursor is JObject co)
                        {
                            if (!co.TryGetValue(parts[i], out JToken val))
                            {
                                //TODO: Error resolving pointer, no such property on object
                                return new JValue((object)null);
                            }

                            if (val is JObject vo && vo.TryGetValue("$ref", out JToken vor) && vor is JValue vorv && vorv.Type == JTokenType.String)
                            {
                                cursor = await ResolvePointersAsync(loadLocation, root, val, client).ConfigureAwait(false);
                            }
                            else
                            {
                                cursor = val;
                            }
                        }
                        else
                        {
                            //TODO: Error resolving pointer, cannot index into literal
                            return new JValue((object)null);
                        }
                    }

                    cursor = await ResolvePointersAsync(loadLocation, root, cursor, client);
                    return cursor.DeepClone();
                }

                foreach (JProperty property in obj.Properties().ToList())
                {
                    obj[property.Name] = await ResolvePointersAsync(loadLocation, root, property.Value, client).ConfigureAwait(false);
                }
            }

            return toResolve;
        }
    }
}
