// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.Net.Http.Headers
{
    public class ContentDispositionHeaderValueTest
    {
        [Fact]
        public void Ctor_ContentDispositionNull_Throw()
        {
            Assert.Throws<ArgumentException>(() => new ContentDispositionHeaderValue(null));
        }

        [Fact]
        public void Ctor_ContentDispositionEmpty_Throw()
        {
            // null and empty should be treated the same. So we also throw for empty strings.
            Assert.Throws<ArgumentException>(() => new ContentDispositionHeaderValue(string.Empty));
        }

        [Fact]
        public void Ctor_ContentDispositionInvalidFormat_ThrowFormatException()
        {
            // When adding values using strongly typed objects, no leading/trailing LWS (whitespaces) are allowed.
            AssertFormatException(" inline ");
            AssertFormatException(" inline");
            AssertFormatException("inline ");
            AssertFormatException("\"inline\"");
            AssertFormatException("te xt");
            AssertFormatException("te=xt");
            AssertFormatException("teäxt");
            AssertFormatException("text;");
            AssertFormatException("te/xt;");
            AssertFormatException("inline; name=someName; ");
            AssertFormatException("text;name=someName"); // ctor takes only disposition-type name, no parameters
        }

        [Fact]
        public void Ctor_ContentDispositionValidFormat_SuccessfullyCreated()
        {
            var contentDisposition = new ContentDispositionHeaderValue("inline");
            Assert.Equal("inline", contentDisposition.DispositionType);
            Assert.Equal(0, contentDisposition.Parameters.Count);
            Assert.Null(contentDisposition.Name.Value);
            Assert.Null(contentDisposition.FileName.Value);
            Assert.Null(contentDisposition.CreationDate);
            Assert.Null(contentDisposition.ModificationDate);
            Assert.Null(contentDisposition.ReadDate);
            Assert.Null(contentDisposition.Size);
        }

        [Fact]
        public void Parameters_AddNull_Throw()
        {
            var contentDisposition = new ContentDispositionHeaderValue("inline");
            Assert.Throws<ArgumentNullException>(() => contentDisposition.Parameters.Add(null));
        }

        [Fact]
        public void ContentDisposition_SetAndGetContentDisposition_MatchExpectations()
        {
            var contentDisposition = new ContentDispositionHeaderValue("inline");
            Assert.Equal("inline", contentDisposition.DispositionType);

            contentDisposition.DispositionType = "attachment";
            Assert.Equal("attachment", contentDisposition.DispositionType);
        }

        [Fact]
        public void Name_SetNameAndValidateObject_ParametersEntryForNameAdded()
        {
            var contentDisposition = new ContentDispositionHeaderValue("inline");
            contentDisposition.Name = "myname";
            Assert.Equal("myname", contentDisposition.Name);
            Assert.Equal(1, contentDisposition.Parameters.Count);
            Assert.Equal("name", contentDisposition.Parameters.First().Name);

            contentDisposition.Name = null;
            Assert.Null(contentDisposition.Name.Value);
            Assert.Equal(0, contentDisposition.Parameters.Count);
            contentDisposition.Name = null; // It's OK to set it again to null; no exception.
        }

        [Fact]
        public void Name_AddNameParameterThenUseProperty_ParametersEntryIsOverwritten()
        {
            var contentDisposition = new ContentDispositionHeaderValue("inline");

            // Note that uppercase letters are used. Comparison should happen case-insensitive.
            NameValueHeaderValue name = new NameValueHeaderValue("NAME", "old_name");
            contentDisposition.Parameters.Add(name);
            Assert.Equal(1, contentDisposition.Parameters.Count);
            Assert.Equal("NAME", contentDisposition.Parameters.First().Name);

            contentDisposition.Name = "new_name";
            Assert.Equal("new_name", contentDisposition.Name);
            Assert.Equal(1, contentDisposition.Parameters.Count);
            Assert.Equal("NAME", contentDisposition.Parameters.First().Name);

            contentDisposition.Parameters.Remove(name);
            Assert.Null(contentDisposition.Name.Value);
        }

        [Fact]
        public void FileName_AddNameParameterThenUseProperty_ParametersEntryIsOverwritten()
        {
            var contentDisposition = new ContentDispositionHeaderValue("inline");

            // Note that uppercase letters are used. Comparison should happen case-insensitive.
            var fileName = new NameValueHeaderValue("FILENAME", "old_name");
            contentDisposition.Parameters.Add(fileName);
            Assert.Equal(1, contentDisposition.Parameters.Count);
            Assert.Equal("FILENAME", contentDisposition.Parameters.First().Name);

            contentDisposition.FileName = "new_name";
            Assert.Equal("new_name", contentDisposition.FileName);
            Assert.Equal(1, contentDisposition.Parameters.Count);
            Assert.Equal("FILENAME", contentDisposition.Parameters.First().Name);

            contentDisposition.Parameters.Remove(fileName);
            Assert.Null(contentDisposition.FileName.Value);
        }

        [Fact]
        public void FileName_NeedsEncoding_EncodedAndDecodedCorrectly()
        {
            var contentDisposition = new ContentDispositionHeaderValue("inline");

            contentDisposition.FileName = "FileÃName.bat";
            Assert.Equal("FileÃName.bat", contentDisposition.FileName);
            Assert.Equal(1, contentDisposition.Parameters.Count);
            Assert.Equal("filename", contentDisposition.Parameters.First().Name);
            Assert.Equal("\"=?utf-8?B?RmlsZcODTmFtZS5iYXQ=?=\"", contentDisposition.Parameters.First().Value);

            contentDisposition.Parameters.Remove(contentDisposition.Parameters.First());
            Assert.Null(contentDisposition.FileName.Value);
        }

        [Fact]
        public void FileName_UnknownOrBadEncoding_PropertyFails()
        {
            var contentDisposition = new ContentDispositionHeaderValue("inline");

            // Note that uppercase letters are used. Comparison should happen case-insensitive.
            var fileName = new NameValueHeaderValue("FILENAME", "\"=?utf-99?Q?R=mlsZcODTmFtZS5iYXQ=?=\"");
            contentDisposition.Parameters.Add(fileName);
            Assert.Equal(1, contentDisposition.Parameters.Count);
            Assert.Equal("FILENAME", contentDisposition.Parameters.First().Name);
            Assert.Equal("\"=?utf-99?Q?R=mlsZcODTmFtZS5iYXQ=?=\"", contentDisposition.Parameters.First().Value);
            Assert.Equal("=?utf-99?Q?R=mlsZcODTmFtZS5iYXQ=?=", contentDisposition.FileName);

            contentDisposition.FileName = "new_name";
            Assert.Equal("new_name", contentDisposition.FileName);
            Assert.Equal(1, contentDisposition.Parameters.Count);
            Assert.Equal("FILENAME", contentDisposition.Parameters.First().Name);

            contentDisposition.Parameters.Remove(fileName);
            Assert.Null(contentDisposition.FileName.Value);
        }

        [Fact]
        public void FileNameStar_AddNameParameterThenUseProperty_ParametersEntryIsOverwritten()
        {
            var contentDisposition = new ContentDispositionHeaderValue("inline");

            // Note that uppercase letters are used. Comparison should happen case-insensitive.
            var fileNameStar = new NameValueHeaderValue("FILENAME*", "old_name");
            contentDisposition.Parameters.Add(fileNameStar);
            Assert.Equal(1, contentDisposition.Parameters.Count);
            Assert.Equal("FILENAME*", contentDisposition.Parameters.First().Name);
            Assert.Null(contentDisposition.FileNameStar.Value); // Decode failure

            contentDisposition.FileNameStar = "new_name";
            Assert.Equal("new_name", contentDisposition.FileNameStar);
            Assert.Equal(1, contentDisposition.Parameters.Count);
            Assert.Equal("FILENAME*", contentDisposition.Parameters.First().Name);
            Assert.Equal("UTF-8\'\'new_name", contentDisposition.Parameters.First().Value);

            contentDisposition.Parameters.Remove(fileNameStar);
            Assert.Null(contentDisposition.FileNameStar.Value);
        }

        [Fact]
        public void FileNameStar_NeedsEncoding_EncodedAndDecodedCorrectly()
        {
            var contentDisposition = new ContentDispositionHeaderValue("inline");

            contentDisposition.FileNameStar = "FileÃName.bat";
            Assert.Equal("FileÃName.bat", contentDisposition.FileNameStar);
            Assert.Equal(1, contentDisposition.Parameters.Count);
            Assert.Equal("filename*", contentDisposition.Parameters.First().Name);
            Assert.Equal("UTF-8\'\'File%C3%83Name.bat", contentDisposition.Parameters.First().Value);

            contentDisposition.Parameters.Remove(contentDisposition.Parameters.First());
            Assert.Null(contentDisposition.FileNameStar.Value);
        }

        [Fact]
        public void FileNameStar_UnknownOrBadEncoding_PropertyFails()
        {
            var contentDisposition = new ContentDispositionHeaderValue("inline");

            // Note that uppercase letters are used. Comparison should happen case-insensitive.
            var fileNameStar = new NameValueHeaderValue("FILENAME*", "utf-99'lang'File%CZName.bat");
            contentDisposition.Parameters.Add(fileNameStar);
            Assert.Equal(1, contentDisposition.Parameters.Count);
            Assert.Equal("FILENAME*", contentDisposition.Parameters.First().Name);
            Assert.Equal("utf-99'lang'File%CZName.bat", contentDisposition.Parameters.First().Value);
            Assert.Null(contentDisposition.FileNameStar.Value); // Decode failure

            contentDisposition.FileNameStar = "new_name";
            Assert.Equal("new_name", contentDisposition.FileNameStar);
            Assert.Equal(1, contentDisposition.Parameters.Count);
            Assert.Equal("FILENAME*", contentDisposition.Parameters.First().Name);

            contentDisposition.Parameters.Remove(fileNameStar);
            Assert.Null(contentDisposition.FileNameStar.Value);
        }

        [Fact]
        public void Dates_AddDateParameterThenUseProperty_ParametersEntryIsOverwritten()
        {
            string validDateString = "\"Tue, 15 Nov 1994 08:12:31 GMT\"";
            DateTimeOffset validDate = DateTimeOffset.Parse("Tue, 15 Nov 1994 08:12:31 GMT");

            var contentDisposition = new ContentDispositionHeaderValue("inline");

            // Note that uppercase letters are used. Comparison should happen case-insensitive.
            var dateParameter = new NameValueHeaderValue("Creation-DATE", validDateString);
            contentDisposition.Parameters.Add(dateParameter);
            Assert.Equal(1, contentDisposition.Parameters.Count);
            Assert.Equal("Creation-DATE", contentDisposition.Parameters.First().Name);

            Assert.Equal(validDate, contentDisposition.CreationDate);

            var newDate = validDate.AddSeconds(1);
            contentDisposition.CreationDate = newDate;
            Assert.Equal(newDate, contentDisposition.CreationDate);
            Assert.Equal(1, contentDisposition.Parameters.Count);
            Assert.Equal("Creation-DATE", contentDisposition.Parameters.First().Name);
            Assert.Equal("\"Tue, 15 Nov 1994 08:12:32 GMT\"", contentDisposition.Parameters.First().Value);

            contentDisposition.Parameters.Remove(dateParameter);
            Assert.Null(contentDisposition.CreationDate);
        }

        [Fact]
        public void Dates_InvalidDates_PropertyFails()
        {
            string invalidDateString = "\"Tue, 15 Nov 94 08:12 GMT\"";

            var contentDisposition = new ContentDispositionHeaderValue("inline");

            // Note that uppercase letters are used. Comparison should happen case-insensitive.
            var dateParameter = new NameValueHeaderValue("read-DATE", invalidDateString);
            contentDisposition.Parameters.Add(dateParameter);
            Assert.Equal(1, contentDisposition.Parameters.Count);
            Assert.Equal("read-DATE", contentDisposition.Parameters.First().Name);

            Assert.Null(contentDisposition.ReadDate);

            contentDisposition.ReadDate = null;
            Assert.Null(contentDisposition.ReadDate);
            Assert.Equal(0, contentDisposition.Parameters.Count);
        }

        [Fact]
        public void Size_AddSizeParameterThenUseProperty_ParametersEntryIsOverwritten()
        {
            var contentDisposition = new ContentDispositionHeaderValue("inline");

            // Note that uppercase letters are used. Comparison should happen case-insensitive.
            var sizeParameter = new NameValueHeaderValue("SIZE", "279172874239");
            contentDisposition.Parameters.Add(sizeParameter);
            Assert.Equal(1, contentDisposition.Parameters.Count);
            Assert.Equal("SIZE", contentDisposition.Parameters.First().Name);
            Assert.Equal(279172874239, contentDisposition.Size);

            contentDisposition.Size = 279172874240;
            Assert.Equal(279172874240, contentDisposition.Size);
            Assert.Equal(1, contentDisposition.Parameters.Count);
            Assert.Equal("SIZE", contentDisposition.Parameters.First().Name);

            contentDisposition.Parameters.Remove(sizeParameter);
            Assert.Null(contentDisposition.Size);
        }

        [Fact]
        public void Size_InvalidSizes_PropertyFails()
        {
            var contentDisposition = new ContentDispositionHeaderValue("inline");

            // Note that uppercase letters are used. Comparison should happen case-insensitive.
            var sizeParameter = new NameValueHeaderValue("SIZE", "-279172874239");
            contentDisposition.Parameters.Add(sizeParameter);
            Assert.Equal(1, contentDisposition.Parameters.Count);
            Assert.Equal("SIZE", contentDisposition.Parameters.First().Name);
            Assert.Null(contentDisposition.Size);

            // Negatives not allowed
            Assert.Throws<ArgumentOutOfRangeException>(() => contentDisposition.Size = -279172874240);
            Assert.Null(contentDisposition.Size);
            Assert.Equal(1, contentDisposition.Parameters.Count);
            Assert.Equal("SIZE", contentDisposition.Parameters.First().Name);

            contentDisposition.Parameters.Remove(sizeParameter);
            Assert.Null(contentDisposition.Size);
        }

        [Fact]
        public void ToString_UseDifferentContentDispositions_AllSerializedCorrectly()
        {
            var contentDisposition = new ContentDispositionHeaderValue("inline");
            Assert.Equal("inline", contentDisposition.ToString());

            contentDisposition.Name = "myname";
            Assert.Equal("inline; name=myname", contentDisposition.ToString());

            contentDisposition.FileName = "my File Name";
            Assert.Equal("inline; name=myname; filename=\"my File Name\"", contentDisposition.ToString());

            contentDisposition.CreationDate = new DateTimeOffset(new DateTime(2011, 2, 15), new TimeSpan(-8, 0, 0));
            Assert.Equal("inline; name=myname; filename=\"my File Name\"; creation-date="
                + "\"Tue, 15 Feb 2011 08:00:00 GMT\"", contentDisposition.ToString());

            contentDisposition.Parameters.Add(new NameValueHeaderValue("custom", "\"custom value\""));
            Assert.Equal("inline; name=myname; filename=\"my File Name\"; creation-date="
                + "\"Tue, 15 Feb 2011 08:00:00 GMT\"; custom=\"custom value\"",  contentDisposition.ToString());

            contentDisposition.Name = null;
            Assert.Equal("inline; filename=\"my File Name\"; creation-date="
                + "\"Tue, 15 Feb 2011 08:00:00 GMT\"; custom=\"custom value\"", contentDisposition.ToString());

            contentDisposition.FileNameStar = "File%Name";
            Assert.Equal("inline; filename=\"my File Name\"; creation-date="
                + "\"Tue, 15 Feb 2011 08:00:00 GMT\"; custom=\"custom value\"; filename*=UTF-8\'\'File%25Name",
                contentDisposition.ToString());

            contentDisposition.FileName = null;
            Assert.Equal("inline; creation-date=\"Tue, 15 Feb 2011 08:00:00 GMT\"; custom=\"custom value\";"
                + " filename*=UTF-8\'\'File%25Name", contentDisposition.ToString());

            contentDisposition.CreationDate = null;
            Assert.Equal("inline; custom=\"custom value\"; filename*=UTF-8\'\'File%25Name",
                contentDisposition.ToString());
        }

        [Fact]
        public void GetHashCode_UseContentDispositionWithAndWithoutParameters_SameOrDifferentHashCodes()
        {
            var contentDisposition1 = new ContentDispositionHeaderValue("inline");
            var contentDisposition2 = new ContentDispositionHeaderValue("inline");
            contentDisposition2.Name = "myname";
            var contentDisposition3 = new ContentDispositionHeaderValue("inline");
            contentDisposition3.Parameters.Add(new NameValueHeaderValue("name", "value"));
            var contentDisposition4 = new ContentDispositionHeaderValue("INLINE");
            var contentDisposition5 = new ContentDispositionHeaderValue("INLINE");
            contentDisposition5.Parameters.Add(new NameValueHeaderValue("NAME", "MYNAME"));

            Assert.NotEqual(contentDisposition1.GetHashCode(), contentDisposition2.GetHashCode());
            Assert.NotEqual(contentDisposition1.GetHashCode(), contentDisposition3.GetHashCode());
            Assert.NotEqual(contentDisposition2.GetHashCode(), contentDisposition3.GetHashCode());
            Assert.Equal(contentDisposition1.GetHashCode(), contentDisposition4.GetHashCode());
            Assert.Equal(contentDisposition2.GetHashCode(), contentDisposition5.GetHashCode());
        }

        [Fact]
        public void Equals_UseContentDispositionWithAndWithoutParameters_EqualOrNotEqualNoExceptions()
        {
            var contentDisposition1 = new ContentDispositionHeaderValue("inline");
            var contentDisposition2 = new ContentDispositionHeaderValue("inline");
            contentDisposition2.Name = "myName";
            var contentDisposition3 = new ContentDispositionHeaderValue("inline");
            contentDisposition3.Parameters.Add(new NameValueHeaderValue("name", "value"));
            var contentDisposition4 = new ContentDispositionHeaderValue("INLINE");
            var contentDisposition5 = new ContentDispositionHeaderValue("INLINE");
            contentDisposition5.Parameters.Add(new NameValueHeaderValue("NAME", "MYNAME"));
            var contentDisposition6 = new ContentDispositionHeaderValue("INLINE");
            contentDisposition6.Parameters.Add(new NameValueHeaderValue("NAME", "MYNAME"));
            contentDisposition6.Parameters.Add(new NameValueHeaderValue("custom", "value"));
            var contentDisposition7 = new ContentDispositionHeaderValue("attachment");

            Assert.False(contentDisposition1.Equals(contentDisposition2), "No params vs. name.");
            Assert.False(contentDisposition2.Equals(contentDisposition1), "name vs. no params.");
            Assert.False(contentDisposition1.Equals(null), "No params vs. <null>.");
            Assert.False(contentDisposition1.Equals(contentDisposition3), "No params vs. custom param.");
            Assert.False(contentDisposition2.Equals(contentDisposition3), "name vs. custom param.");
            Assert.True(contentDisposition1.Equals(contentDisposition4), "Different casing.");
            Assert.True(contentDisposition2.Equals(contentDisposition5), "Different casing in name.");
            Assert.False(contentDisposition5.Equals(contentDisposition6), "name vs. custom param.");
            Assert.False(contentDisposition1.Equals(contentDisposition7), "inline vs. text/other.");
        }

        [Fact]
        public void Parse_SetOfValidValueStrings_ParsedCorrectly()
        {
            var expected = new ContentDispositionHeaderValue("inline");
            CheckValidParse("\r\n inline  ", expected);
            CheckValidParse("inline", expected);

            // We don't have to test all possible input strings, since most of the pieces are handled by other parsers.
            // The purpose of this test is to verify that these other parsers are combined correctly to build a
            // Content-Disposition parser.
            expected.Name = "myName";
            CheckValidParse("\r\n inline  ;  name =   myName ", expected);
            CheckValidParse("  inline;name=myName", expected);

            expected.Name = null;
            expected.DispositionType = "attachment";
            expected.FileName = "foo-ae.html";
            expected.Parameters.Add(new NameValueHeaderValue("filename*", "UTF-8''foo-%c3%a4.html"));
            CheckValidParse(@"attachment; filename*=UTF-8''foo-%c3%a4.html; filename=foo-ae.html", expected);
        }

        [Fact]
        public void Parse_SetOfInvalidValueStrings_Throws()
        {
            CheckInvalidParse("");
            CheckInvalidParse("  ");
            CheckInvalidParse(null);
            CheckInvalidParse("inline会");
            CheckInvalidParse("inline ,");
            CheckInvalidParse("inline,");
            CheckInvalidParse("inline; name=myName ,");
            CheckInvalidParse("inline; name=myName,");
            CheckInvalidParse("inline; name=my会Name");
            CheckInvalidParse("inline/");
        }

        [Fact]
        public void TryParse_SetOfValidValueStrings_ParsedCorrectly()
        {
            var expected = new ContentDispositionHeaderValue("inline");
            CheckValidTryParse("\r\n inline  ", expected);
            CheckValidTryParse("inline", expected);

            // We don't have to test all possible input strings, since most of the pieces are handled by other parsers.
            // The purpose of this test is to verify that these other parsers are combined correctly to build a
            // Content-Disposition parser.
            expected.Name = "myName";
            CheckValidTryParse("\r\n inline  ;  name =   myName ", expected);
            CheckValidTryParse("  inline;name=myName", expected);
        }

        [Fact]
        public void TryParse_SetOfInvalidValueStrings_ReturnsFalse()
        {
            CheckInvalidTryParse("");
            CheckInvalidTryParse("  ");
            CheckInvalidTryParse(null);
            CheckInvalidTryParse("inline会");
            CheckInvalidTryParse("inline ,");
            CheckInvalidTryParse("inline,");
            CheckInvalidTryParse("inline; name=myName ,");
            CheckInvalidTryParse("inline; name=myName,");
            CheckInvalidTryParse("text/");
        }

        public static TheoryData<string, ContentDispositionHeaderValue> ValidContentDispositionTestCases = new TheoryData<string, ContentDispositionHeaderValue>()
        {
            { "inline", new ContentDispositionHeaderValue("inline") }, // @"This should be equivalent to not including the header at all."
            { "inline;", new ContentDispositionHeaderValue("inline") },
            { "inline;name=", new ContentDispositionHeaderValue("inline") { Parameters = { new NameValueHeaderValue("name", "") } } }, // TODO: passing in a null value causes a strange assert on CoreCLR before the test even starts. Not reproducable in the body of a test.
            { "inline;name=value", new ContentDispositionHeaderValue("inline") { Name  = "value" } },
            { "inline;name=value;", new ContentDispositionHeaderValue("inline") { Name  = "value" } },
            { "inline;name=value;", new ContentDispositionHeaderValue("inline") { Name  = "value" } },
            { @"inline; filename=""foo.html""", new ContentDispositionHeaderValue("inline") { FileName = @"""foo.html""" } },
            { @"inline; filename=""Not an attachment!""", new ContentDispositionHeaderValue("inline") { FileName = @"""Not an attachment!""" } }, // 'inline', specifying a filename of Not an attachment! - this checks for proper parsing for disposition types.
            { @"inline; filename=""foo.pdf""", new ContentDispositionHeaderValue("inline") { FileName = @"""foo.pdf""" } },
            { "attachment", new ContentDispositionHeaderValue("attachment") },
            { "ATTACHMENT", new ContentDispositionHeaderValue("ATTACHMENT") },
            { @"attachment; filename=""foo.html""", new ContentDispositionHeaderValue("attachment") { FileName = @"""foo.html""" } },
            { @"attachment; filename=""\""quoting\"" tested.html""", new ContentDispositionHeaderValue("attachment") { FileName = "\"\"quoting\" tested.html\"" } }, // 'attachment', specifying a filename of \"quoting\" tested.html (using double quotes around "quoting" to test... quoting)
            { @"attachment; filename=""Here's a semicolon;.html""", new ContentDispositionHeaderValue("attachment") { FileName = @"""Here's a semicolon;.html""" } }, // , 'attachment', specifying a filename of Here's a semicolon;.html - this checks for proper parsing for parameters.
            { @"attachment; foo=""bar""; filename=""foo.html""", new ContentDispositionHeaderValue(@"attachment") { FileName = @"""foo.html""", Parameters = { new NameValueHeaderValue("foo", @"""bar""") } } }, // 'attachment', specifying a filename of foo.html and an extension parameter "foo" which should be ignored (see <a href="http://greenbytes.de/tech/webdav/rfc2183.html#rfc.section.2.8">Section 2.8 of RFC 2183</a>.).
            { @"attachment; foo=""\""\\"";filename=""foo.html""", new ContentDispositionHeaderValue(@"attachment") { FileName = @"""foo.html""", Parameters = { new NameValueHeaderValue("foo", @"""\""\\""") } } }, // 'attachment', specifying a filename of foo.html and an extension parameter "foo" which should be ignored (see <a href="http://greenbytes.de/tech/webdav/rfc2183.html#rfc.section.2.8">Section 2.8 of RFC 2183</a>.). The extension parameter actually uses backslash-escapes. This tests whether the UA properly skips the parameter.
            { @"attachment; FILENAME=""foo.html""", new ContentDispositionHeaderValue("attachment") { FileName = @"""foo.html""" } },
            { @"attachment; filename=foo.html", new ContentDispositionHeaderValue("attachment") { FileName = "foo.html" } }, // 'attachment', specifying a filename of foo.html using a token instead of a quoted-string.
            { @"attachment; filename='foo.bar'", new ContentDispositionHeaderValue("attachment") { FileName = "'foo.bar'" } }, // 'attachment', specifying a filename of 'foo.bar' using single quotes.
            { @"attachment; filename=""foo-ä.html""", new ContentDispositionHeaderValue("attachment" ) { Parameters = { new NameValueHeaderValue("filename", @"""foo-ä.html""") } } }, // 'attachment', specifying a filename of foo-ä.html, using plain ISO-8859-1
            { @"attachment; filename=""foo-&#xc3;&#xa4;.html""", new ContentDispositionHeaderValue("attachment") { FileName = @"""foo-&#xc3;&#xa4;.html""" } }, // 'attachment', specifying a filename of foo-&#xc3;&#xa4;.html, which happens to be foo-ä.html using UTF-8 encoding.
            { @"attachment; filename=""foo-%41.html""", new ContentDispositionHeaderValue("attachment") {  Parameters = { new NameValueHeaderValue("filename", @"""foo-%41.html""") } } },
            { @"attachment; filename=""50%.html""", new ContentDispositionHeaderValue("attachment") { Parameters = { new NameValueHeaderValue("filename", @"""50%.html""") } } },
            { @"attachment; filename=""foo-%\41.html""", new ContentDispositionHeaderValue("attachment") { Parameters = { new NameValueHeaderValue("filename", @"""foo-%\41.html""") } } }, // 'attachment', specifying a filename of foo-%41.html, using an escape character (this tests whether adding an escape character inside a %xx sequence can be used to disable the non-conformant %xx-unescaping).
            { @"attachment; name=""foo-%41.html""", new ContentDispositionHeaderValue("attachment") { Name = @"""foo-%41.html""" } }, // 'attachment', specifying a <i>name</i> parameter of foo-%41.html. (this test was added to observe the behavior of the (unspecified) treatment of ""name"" as synonym for ""filename""; see <a href=""http://www.imc.org/ietf-smtp/mail-archive/msg05023.html"">Ned Freed's summary</a> where this comes from in MIME messages)
            { @"attachment; filename=""ä-%41.html""", new ContentDispositionHeaderValue("attachment") { Parameters = { new NameValueHeaderValue("filename", @"""ä-%41.html""") } } }, // 'attachment', specifying a filename parameter of ä-%41.html. (this test was added to observe the behavior when non-ASCII characters and percent-hexdig sequences are combined)
            { @"attachment; filename=""foo-%c3%a4-%e2%82%ac.html""", new ContentDispositionHeaderValue("attachment") { FileName = @"""foo-%c3%a4-%e2%82%ac.html""" } }, // 'attachment', specifying a filename of foo-%c3%a4-%e2%82%ac.html, using raw percent encoded UTF-8 to represent foo-ä-&#x20ac;.html
            { @"attachment; filename =""foo.html""", new ContentDispositionHeaderValue("attachment") { FileName = @"""foo.html""" } },
            { @"attachment; xfilename=foo.html", new ContentDispositionHeaderValue("attachment" ) { Parameters = { new NameValueHeaderValue("xfilename", "foo.html") } } },
            { @"attachment; filename=""/foo.html""", new ContentDispositionHeaderValue("attachment") { FileName = @"""/foo.html""" } },
            { @"attachment; creation-date=""Wed, 12 Feb 1997 16:29:51 -0500""", new ContentDispositionHeaderValue("attachment") { Parameters = { new NameValueHeaderValue("creation-date", @"""Wed, 12 Feb 1997 16:29:51 -0500""") } } },
            { @"attachment; modification-date=""Wed, 12 Feb 1997 16:29:51 -0500""", new ContentDispositionHeaderValue("attachment") { Parameters = { new NameValueHeaderValue("modification-date", @"""Wed, 12 Feb 1997 16:29:51 -0500""") } } },
            { @"foobar", new ContentDispositionHeaderValue("foobar") }, //  @"This should be equivalent to using ""attachment""."
            { @"attachment; example=""filename=example.txt""", new ContentDispositionHeaderValue("attachment") { Parameters = { new NameValueHeaderValue("example", @"""filename=example.txt""") } } },
            { @"attachment; filename*=iso-8859-1''foo-%E4.html", new ContentDispositionHeaderValue("attachment") { Parameters = { new NameValueHeaderValue("filename*", "iso-8859-1''foo-%E4.html") } } }, // 'attachment', specifying a filename of foo-ä.html, using RFC2231 encoded ISO-8859-1
            { @"attachment; filename*=UTF-8''foo-%c3%a4-%e2%82%ac.html", new ContentDispositionHeaderValue("attachment") {  Parameters = { new NameValueHeaderValue("filename*", "UTF-8''foo-%c3%a4-%e2%82%ac.html") } } }, // 'attachment', specifying a filename of foo-ä-&#x20ac;.html, using RFC2231 encoded UTF-8
            { @"attachment; filename*=''foo-%c3%a4-%e2%82%ac.html", new ContentDispositionHeaderValue("attachment") { Parameters = { new NameValueHeaderValue("filename*", "''foo-%c3%a4-%e2%82%ac.html") } } }, // Behavior is undefined in RFC 2231, the charset part is missing, although UTF-8 was used.
            { @"attachment; filename*=UTF-8''foo-a%22.html", new ContentDispositionHeaderValue("attachment") { FileNameStar = @"foo-a"".html" } },
            { @"attachment; filename*= UTF-8''foo-%c3%a4.html", new ContentDispositionHeaderValue("attachment") { FileNameStar = "foo-ä.html" } },
            { @"attachment; filename* =UTF-8''foo-%c3%a4.html", new ContentDispositionHeaderValue("attachment") { FileNameStar = "foo-ä.html" } },
            { @"attachment; filename*=UTF-8''A-%2541.html", new ContentDispositionHeaderValue("attachment") { FileNameStar = "A-%41.html" } },
            { @"attachment; filename*=UTF-8''%5cfoo.html", new ContentDispositionHeaderValue("attachment") { FileNameStar = @"\foo.html" } },
            { @"attachment; filename=""foo-ae.html""; filename*=UTF-8''foo-%c3%a4.html", new ContentDispositionHeaderValue("attachment") { FileName = @"""foo-ae.html""", FileNameStar = "foo-ä.html" } },
            { @"attachment; filename*=UTF-8''foo-%c3%a4.html; filename=""foo-ae.html""", new ContentDispositionHeaderValue("attachment") { FileNameStar = "foo-ä.html", FileName = @"""foo-ae.html""" } },
            { @"attachment; foobar=x; filename=""foo.html""", new ContentDispositionHeaderValue("attachment") { FileName = @"""foo.html""", Parameters = { new NameValueHeaderValue("foobar", "x") } } },
            { @"attachment; filename=""=?ISO-8859-1?Q?foo-=E4.html?=""", new ContentDispositionHeaderValue("attachment") { FileName = @"""=?ISO-8859-1?Q?foo-=E4.html?=""" } }, // attachment; filename="=?ISO-8859-1?Q?foo-=E4.html?="
            { @"attachment; filename=""=?utf-8?B?Zm9vLeQuaHRtbA==?=""", new ContentDispositionHeaderValue("attachment") { FileName = @"""=?utf-8?B?Zm9vLeQuaHRtbA==?=""" } }, // attachment; filename="=?utf-8?B?Zm9vLeQuaHRtbA==?="
            { @"attachment; filename=foo.html ;", new ContentDispositionHeaderValue("attachment") { FileName="foo.html" } }, // 'attachment', specifying a filename of foo.html using a token instead of a quoted-string, and adding a trailing semicolon.,
        };

        [Theory]
        [MemberData(nameof(ValidContentDispositionTestCases))]
        public void ContentDispositionHeaderValue_ParseValid_Success(string input, ContentDispositionHeaderValue expected)
        {
            // System.Diagnostics.Debugger.Launch();
            var result = ContentDispositionHeaderValue.Parse(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        // Invalid values
        [InlineData(@"""inline""")] // @"'inline' only, using double quotes", false) },
        [InlineData(@"""attachment""")] // @"'attachment' only, using double quotes", false) },
        [InlineData(@"attachment; filename=foo bar.html")] // @"'attachment', specifying a filename of foo bar.html without using quoting.", false) },
        // Duplicate file name parameter
        // @"attachment; filename=""foo.html""; // filename=""bar.html""", @"'attachment', specifying two filename parameters. This is invalid syntax.", false) },
        [InlineData(@"attachment; filename=foo[1](2).html")] // @"'attachment', specifying a filename of foo[1](2).html, but missing the quotes. Also, ""["", ""]"", ""("" and "")"" are not allowed in the HTTP <a href=""http://greenbytes.de/tech/webdav/draft-ietf-httpbis-p1-messaging-latest.html#rfc.section.1.2.2"">token</a> production.", false) },
        [InlineData(@"attachment; filename=foo-ä.html")] // @"'attachment', specifying a filename of foo-ä.html, but missing the quotes.", false) },
        // HTML escaping, not supported
        // @"attachment; filename=foo-&#xc3;&#xa4;.html", // "'attachment', specifying a filename of foo-&#xc3;&#xa4;.html (which happens to be foo-ä.html using UTF-8 encoding) but missing the quotes.", false) },
        [InlineData(@"filename=foo.html")] // @"Disposition type missing, filename specified.", false) },
        [InlineData(@"x=y; filename=foo.html")] // @"Disposition type missing, filename specified after extension parameter.", false) },
        [InlineData(@"""foo; filename=bar;baz""; filename=qux")] // @"Disposition type missing, filename ""qux"". Can it be more broken? (Probably)", false) },
        [InlineData(@"filename=foo.html, filename=bar.html")] // @"Disposition type missing, two filenames specified separated by a comma (this is syntactically equivalent to have two instances of the header with one filename parameter each).", false) },
        [InlineData(@"; filename=foo.html")] // @"Disposition type missing (but delimiter present), filename specified.", false) },
        // This is permitted as a parameter without a value
        // @"inline; attachment; filename=foo.html", // @"Both disposition types specified.", false) },
        // This is permitted as a parameter without a value
        // @"inline; attachment; filename=foo.html", // @"Both disposition types specified.", false) },
        [InlineData(@"attachment; filename=""foo.html"".txt")] // @"'attachment', specifying a filename parameter that is broken (quoted-string followed by more characters). This is invalid syntax. ", false) },
        [InlineData(@"attachment; filename=""bar")] // @"'attachment', specifying a filename parameter that is broken (missing ending double quote). This is invalid syntax.", false) },
        [InlineData(@"attachment; filename=foo""bar;baz""qux")] // @"'attachment', specifying a filename parameter that is broken (disallowed characters in token syntax). This is invalid syntax.", false) },
        [InlineData(@"attachment; filename=foo.html, attachment; filename=bar.html")] // @"'attachment', two comma-separated instances of the header field. As Content-Disposition doesn't use a list-style syntax, this is invalid syntax and, according to <a href=""http://greenbytes.de/tech/webdav/rfc2616.html#rfc.section.4.2.p.5"">RFC 2616, Section 4.2</a>, roughly equivalent to having two separate header field instances.", false) },
        [InlineData(@"filename=foo.html; attachment")] // @"filename parameter and disposition type reversed.", false) },
        // Escaping is not verified
        // @"attachment; filename*=iso-8859-1''foo-%c3%a4-%e2%82%ac.html", // @"'attachment', specifying a filename of foo-ä-&#x20ac;.html, using RFC2231 encoded UTF-8, but declaring ISO-8859-1", false) },
        // Escaping is not verified
        // @"attachment; filename *=UTF-8''foo-%c3%a4.html", // @"'attachment', specifying a filename of foo-ä.html, using RFC2231 encoded UTF-8, with whitespace before ""*=""", false) },
        // Escaping is not verified
        // @"attachment; filename*=""UTF-8''foo-%c3%a4.html""", // @"'attachment', specifying a filename of foo-ä.html, using RFC2231 encoded UTF-8, with double quotes around the parameter value.", false) },
        [InlineData(@"attachment; filename==?ISO-8859-1?Q?foo-=E4.html?=")] // @"Uses RFC 2047 style encoded word. ""="" is invalid inside the token production, so this is invalid.", false) },
        [InlineData(@"attachment; filename==?utf-8?B?Zm9vLeQuaHRtbA==?=")] // @"Uses RFC 2047 style encoded word. ""="" is invalid inside the token production, so this is invalid.", false) },
        public void ContentDispositionHeaderValue_ParseInvalid_Throws(string input)
        {
            Assert.Throws<FormatException>(() => ContentDispositionHeaderValue.Parse(input));
        }

        [Fact]
        public void HeaderNamesWithQuotes_ExpectNamesToNotHaveQuotes()
        {
            var contentDispositionLine = "form-data; name =\"dotnet\"; filename=\"example.png\"";
            var expectedName = "dotnet";
            var expectedFileName = "example.png";

            var result = ContentDispositionHeaderValue.Parse(contentDispositionLine);

            Assert.Equal(expectedName, result.Name);
            Assert.Equal(expectedFileName, result.FileName);
        }

        public class ContentDispositionValue
        {
            public ContentDispositionValue(string value, string description, bool valid)
            {
                Value = value;
                Description = description;
                Valid = valid;
            }

            public string Value { get; }

            public string Description { get; }

            public bool Valid { get; }
        }

        private void CheckValidParse(string input, ContentDispositionHeaderValue expectedResult)
        {
            var result = ContentDispositionHeaderValue.Parse(input);
            Assert.Equal(expectedResult, result);
        }

        private void CheckInvalidParse(string input)
        {
            Assert.Throws<FormatException>(() => ContentDispositionHeaderValue.Parse(input));
        }

        private void CheckValidTryParse(string input, ContentDispositionHeaderValue expectedResult)
        {
            ContentDispositionHeaderValue result = null;
            Assert.True(ContentDispositionHeaderValue.TryParse(input, out result), input);
            Assert.Equal(expectedResult, result);
        }

        private void CheckInvalidTryParse(string input)
        {
            ContentDispositionHeaderValue result = null;
            Assert.False(ContentDispositionHeaderValue.TryParse(input, out result), input);
            Assert.Null(result);
        }

        private static void AssertFormatException(string contentDisposition)
        {
            Assert.Throws<FormatException>(() => new ContentDispositionHeaderValue(contentDisposition));
        }
    }
}
