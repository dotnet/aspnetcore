#region Copyright notice and license

// Copyright 2015 gRPC authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using Grpc.Core;

namespace InteropTestsWebsite;

// Implementation copied from https://github.com/grpc/grpc/blob/master/src/csharp/Grpc.Core/Utils/AsyncStreamExtensions.cs
internal static class AsyncStreamExtensions
{
    /// <summary>
    /// Reads the entire stream and executes an async action for each element.
    /// </summary>
    public static async Task ForEachAsync<T>(this IAsyncStreamReader<T> streamReader, Func<T, Task> asyncAction)
        where T : class
    {
        while (await streamReader.MoveNext().ConfigureAwait(false))
        {
            await asyncAction(streamReader.Current).ConfigureAwait(false);
        }
    }
}
