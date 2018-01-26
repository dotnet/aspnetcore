// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace RepoTasks
{
    public class JoinItems : Task
    {
        [Required]
        public ITaskItem[] Left { get; set; }

        [Required]
        public ITaskItem[] Right { get; set; }

        // The metadata to use as the new item spec. If not specified, LeftKey is used.
        public string LeftItemSpec { get; set; }

        //  LeftKey and RightKey: The metadata to join on.  If not set, then use the ItemSpec
        public string LeftKey { get; set; }

        public string RightKey { get; set; }


        //  LeftMetadata and RightMetadata: The metadata names to include in the result.  Specify "*" to include all metadata
        public string[] LeftMetadata { get; set; }

        public string[] RightMetadata { get; set; }


        [Output]
        public ITaskItem[] JoinResult { get; private set; }

        public override bool Execute()
        {
            bool useAllLeftMetadata = LeftMetadata != null && LeftMetadata.Length == 1 && LeftMetadata[0] == "*";
            bool useAllRightMetadata = RightMetadata != null && RightMetadata.Length == 1 && RightMetadata[0] == "*";
            var newItemSpec = string.IsNullOrEmpty(LeftItemSpec)
                ? LeftKey
                : LeftItemSpec;

            JoinResult = Left.Join(Right,
                item => GetKeyValue(LeftKey, item),
                item => GetKeyValue(RightKey, item),
                (left, right) =>
                {
                    //  If including all metadata from left items and none from right items, just return left items directly
                    if (useAllLeftMetadata &&
                        string.IsNullOrEmpty(LeftKey) &&
                        string.IsNullOrEmpty(LeftItemSpec) &&
                        (RightMetadata == null || RightMetadata.Length == 0))
                    {
                        return left;
                    }

                    //  If including all metadata from right items and none from left items, just return the right items directly
                    if (useAllRightMetadata &&
                        string.IsNullOrEmpty(RightKey) &&
                        string.IsNullOrEmpty(LeftItemSpec) &&
                        (LeftMetadata == null || LeftMetadata.Length == 0))
                    {
                        return right;
                    }

                    var ret = new TaskItem(GetKeyValue(newItemSpec, left));

                    //  Weird ordering here is to prefer left metadata in all cases, as CopyToMetadata doesn't overwrite any existing metadata
                    if (useAllLeftMetadata)
                    {
                        //  CopyMetadata adds an OriginalItemSpec, which we don't want.  So we subsequently remove it
                        left.CopyMetadataTo(ret);
                        ret.RemoveMetadata("OriginalItemSpec");
                    }

                    if (!useAllRightMetadata && RightMetadata != null)
                    {
                        foreach (string name in RightMetadata)
                        {
                            ret.SetMetadata(name, right.GetMetadata(name));
                        }
                    }

                    if (!useAllLeftMetadata && LeftMetadata != null)
                    {
                        foreach (string name in LeftMetadata)
                        {
                            ret.SetMetadata(name, left.GetMetadata(name));
                        }
                    }

                    if (useAllRightMetadata)
                    {
                        //  CopyMetadata adds an OriginalItemSpec, which we don't want.  So we subsequently remove it
                        right.CopyMetadataTo(ret);
                        ret.RemoveMetadata("OriginalItemSpec");
                    }

                    return (ITaskItem)ret;
                },
                StringComparer.OrdinalIgnoreCase).ToArray();

            return true;
        }

        static string GetKeyValue(string key, ITaskItem item)
        {
            if (string.IsNullOrEmpty(key))
            {
                return item.ItemSpec;
            }
            else
            {
                return item.GetMetadata(key);
            }
        }
    }
}
