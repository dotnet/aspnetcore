// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml;
using Microsoft.AspNetCore.Shared;
using Microsoft.Web.XmlTransform;

namespace Microsoft.Web.Xdt.Extensions;

/// <summary>
/// Insert or append to the given attribute
/// </summary>
public class InsertOrAppendAttribute : Transform
{
    /// <summary>
    ///
    /// </summary>
    public InsertOrAppendAttribute()
        : base(TransformFlags.UseParentAsTargetNode, MissingTargetMessage.Error)
    {
    }

    private string _attributeName;

    /// <summary>
    ///
    /// </summary>
    protected string AttributeName
    {
        get
        {
            if (_attributeName == null)
            {
                _attributeName = GetArgumentValue("Attribute");
            }
            return _attributeName;
        }
    }

    /// <summary>
    /// Extracts a value from the arguments provided
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    protected string GetArgumentValue(string name)
    {
        ArgumentThrowHelper.ThrowIfNullOrEmpty(name);

        string result = null;
        if (Arguments != null && Arguments.Count > 0)
        {
            foreach (var arg in Arguments)
            {
                if (!string.IsNullOrEmpty(arg))
                {
                    var trimmedArg = arg.Trim();
                    if (trimmedArg.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                    {
                        var start = arg.IndexOf('\'');
                        var last = arg.LastIndexOf('\'');
                        if (start <= 0 || last <= 0 || last <= start)
                        {
                            throw new ArgumentException("Expected two ['] characters");
                        }

                        var value = trimmedArg.Substring(start, last - start);

                        // remove any leading or trailing '
                        value = value.Trim().TrimStart('\'').TrimStart('\'');
                        result = value;
                    }
                }
            }
        }
        return result;
    }

    /// <summary>
    ///
    /// </summary>
    protected override void Apply()
    {
        if (TargetChildNodes == null || TargetChildNodes.Count == 0)
        {
            TargetNode.AppendChild(TransformNode);
        }
        else
        {
            XmlAttribute transformAtt = null;

            foreach (XmlAttribute att in TransformNode.Attributes)
            {
                if (string.Equals(att.Name, AttributeName, StringComparison.OrdinalIgnoreCase))
                {
                    transformAtt = att;
                    break;
                }
            }

            if (transformAtt == null)
            {
                throw new InvalidOperationException("No target attribute to append");
            }

            foreach (XmlNode targetNode in TargetChildNodes)
            {
                var foundAttribute = false;
                foreach (XmlAttribute att in targetNode.Attributes)
                {
                    if (string.Equals(att.Name, AttributeName, StringComparison.OrdinalIgnoreCase))
                    {
                        foundAttribute = true;
                        if (string.IsNullOrEmpty(att.Value))
                        {
                            att.Value = transformAtt.Value;
                        }
                        else
                        {
                            // TODO: This doesn't compose well with insertOrAppend being applied on the TargetNode.
                            // The target node is created with the children it has in the transform, which means we would
                            // duplicate the value here.
                            if (att.Value == transformAtt.Value)
                            {
                                return;
                            }
                            att.Value = $"{att.Value};{transformAtt.Value}";
                        }
                    }
                }

                if (!foundAttribute)
                {
                    var attribute = targetNode.OwnerDocument.CreateAttribute(AttributeName);
                    attribute.Value = transformAtt.Value;
                    targetNode.Attributes.Append(attribute);
                }
            }
        }
    }
}
