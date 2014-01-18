// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;

namespace Microsoft.TestCommon
{
    /// <summary>
    /// An flags enum that can be used to indicate different variations of a given 
    /// <see cref="TestData"/> instance.
    /// </summary>
    [Flags]
    public enum TestDataVariations
    {
        /// <summary>
        /// An individual instance of a given <see cref="TestData"/> type.
        /// </summary>
        AsInstance = 0x1,

        /// <summary>
        /// An individual instance of a type that derives from a given <see cref="TestData"/> type.
        /// </summary>
        AsDerivedType = 0x2,

        /// <summary>
        /// An individual instance of a given <see cref="TestData"/> type that has a property value 
        /// that is a known type of the declared property type.
        /// </summary>
        AsKnownType = 0x4,

        /// <summary>
        /// A <see cref="Nullable<>"/> instance of a given <see cref="TestData"/> type.  Only applies to
        /// instances of <see cref="ValueTypeTestData"/>.
        /// </summary>
        AsNullable = 0x8,

        /// <summary>
        /// An instance of a <see cref="System.Collections.Generic.List<>"/> of a given <see cref="TestData"/> type.
        /// </summary>
        AsList = 0x10,

        /// <summary>
        /// An instance of a array of the <see cref="TestData"/> type.
        /// </summary>
        AsArray = 0x20,

        /// <summary>
        /// An instance of an <see cref="System.Collections.Generic.IEnumerable<>"/> of a given <see cref="TestData"/> type.
        /// </summary>
        AsIEnumerable = 0x40,

        /// <summary>
        /// An instance of an <see cref="System.Linq.IQueryable<>"/> of a given <see cref="TestData"/> type.
        /// </summary>
        AsIQueryable = 0x80,

        /// <summary>
        /// An instance of a DataContract type in which a given <see cref="TestData"/> type is a member.
        /// </summary>
        AsDataMember = 0x100,

        /// <summary>
        /// An instance of a type in which a given <see cref="TestData"/> type is decorated with a 
        /// <see cref="System.Xml.Serialization.XmlElementAttribute"/>.
        /// </summary>
        AsXmlElementProperty = 0x200,

        /// <summary>
        /// An instance of a <see cref="System.Collections.Generic.IDictionary{string,TValue}"/> of a given
        /// <see cref="TestData"/> type.
        /// </summary>
        AsDictionary = 0x400,

        /// <summary>
        /// Add a <c>null</c> instance of the given <see cref="TestData"/> type to the data set.  This variation is
        /// not included in <see cref="All"/> or other variation masks.
        /// </summary>
        WithNull = 0x800,

        /// <summary>
        /// Individual instances of <see cref="TestDataHolder{T}"/> containing the given <see cref="TestData"/>.  This
        /// variation is not included in <see cref="All"/> or other variation masks.
        /// </summary>
        AsClassMember = 0x1000,

        /// <summary>
        /// All of the flags for single instance variations of a given <see cref="TestData"/> type.
        /// </summary>
        AllSingleInstances = AsInstance | AsDerivedType | AsKnownType | AsNullable,

        /// <summary>
        /// All of the flags for collection variations of a given <see cref="TestData"/> type.
        /// </summary>
        AllCollections = AsList | AsArray | AsIEnumerable | AsIQueryable | AsDictionary,

        /// <summary>
        /// All of the flags for variations in which a given <see cref="TestData"/> type is a property on another type.
        /// </summary>
        AllProperties = AsDataMember | AsXmlElementProperty,

        /// <summary>
        /// All of the flags for interface collection variations of a given <see cref="TestData"/> type.
        /// </summary>
        AllInterfaces = AsIEnumerable | AsIQueryable,

        /// <summary>
        /// All of the flags except for the interface collection variations of a given <see cref="TestData"/> type.
        /// </summary>
        AllNonInterfaces = All & ~AllInterfaces,

        /// <summary>
        /// All of the flags for all of the variations of a given <see cref="TestData"/> type.
        /// </summary>
        All = AllSingleInstances | AllCollections | AllProperties
    }
}
