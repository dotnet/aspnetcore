// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Xml;
using Microsoft.Web.XmlTransform;
using Xunit;

namespace Microsoft.Web.Xdt.Extensions;

public class InsertOrAppendAttributeTests
{
    [Fact]
    public void InsertOrAppend_NoExesitingLine_InsertsLine()
    {
        var transform = new XmlTransformation(Path.GetFullPath("transform.xdt"));
        var doc = new XmlDocument();
        doc.Load("config_empty.xml");
        Assert.True(transform.Apply(doc));

        Assert.Equal(2, doc.ChildNodes.Count);
        var configurationNode = doc["configuration"];

        Assert.Equal(2, configurationNode.ChildNodes.Count);

        var firstChild = configurationNode.FirstChild;
        Assert.Equal("add", firstChild.Name);
        Assert.Equal("KeyName1", firstChild.Attributes["name"].Value);
        Assert.Equal("InsertValue1", firstChild.Attributes["value"].Value);

        var secondChild = firstChild.NextSibling;
        Assert.Equal("add", secondChild.Name);
        Assert.Equal("KeyName2", secondChild.Attributes["name"].Value);
        Assert.Equal("InsertValue2", secondChild.Attributes["value"].Value);
    }

    [Fact]
    public void InsertOrAppend_LineExistsButNoValueField_FieldInserted()
    {
        var transform = new XmlTransformation(Path.GetFullPath("transform.xdt"));
        var doc = new XmlDocument();
        doc.Load("config_existingline.xml");
        Assert.True(transform.Apply(doc));

        Assert.Equal(2, doc.ChildNodes.Count);
        var configurationNode = doc["configuration"];

        Assert.Equal(2, configurationNode.ChildNodes.Count);

        var firstChild = configurationNode.FirstChild;
        Assert.Equal("add", firstChild.Name);
        Assert.Equal("KeyName1", firstChild.Attributes["name"].Value);
        Assert.Equal("InsertValue1", firstChild.Attributes["value"].Value);

        var secondChild = firstChild.NextSibling;
        Assert.Equal("add", secondChild.Name);
        Assert.Equal("KeyName2", secondChild.Attributes["name"].Value);
        Assert.Equal("InsertValue2", secondChild.Attributes["value"].Value);
    }

    [Fact]
    public void InsertOrAppend_ExistingEmptyValue_InsertsValue()
    {
        var transform = new XmlTransformation(Path.GetFullPath("transform.xdt"));
        var doc = new XmlDocument();
        doc.Load("config_existingemptyvalue.xml");
        Assert.True(transform.Apply(doc));

        Assert.Equal(2, doc.ChildNodes.Count);
        var configurationNode = doc["configuration"];

        Assert.Equal(2, configurationNode.ChildNodes.Count);

        var firstChild = configurationNode.FirstChild;
        Assert.Equal("add", firstChild.Name);
        Assert.Equal("KeyName1", firstChild.Attributes["name"].Value);
        Assert.Equal("InsertValue1", firstChild.Attributes["value"].Value);

        var secondChild = firstChild.NextSibling;
        Assert.Equal("add", secondChild.Name);
        Assert.Equal("KeyName2", secondChild.Attributes["name"].Value);
        Assert.Equal("InsertValue2", secondChild.Attributes["value"].Value);
    }

    [Fact]
    public void InsertOrAppend_ExistingValue_AppendsValue()
    {
        var transform = new XmlTransformation(Path.GetFullPath("transform.xdt"));
        var doc = new XmlDocument();
        doc.Load("config_existingvalue.xml");
        Assert.True(transform.Apply(doc));

        Assert.Equal(2, doc.ChildNodes.Count);
        var configurationNode = doc["configuration"];

        Assert.Equal(2, configurationNode.ChildNodes.Count);

        var firstChild = configurationNode.FirstChild;
        Assert.Equal("add", firstChild.Name);
        Assert.Equal("KeyName1", firstChild.Attributes["name"].Value);
        Assert.Equal("ExistingValue1;InsertValue1", firstChild.Attributes["value"].Value);

        var secondChild = firstChild.NextSibling;
        Assert.Equal("add", secondChild.Name);
        Assert.Equal("KeyName2", secondChild.Attributes["name"].Value);
        Assert.Equal("ExistingValue2;InsertValue2", secondChild.Attributes["value"].Value);
    }
}
