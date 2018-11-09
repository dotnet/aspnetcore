// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "precomp.hxx"
#include "buffer.h"
#include "stringu.h"
#include "stringa.h"

//
// Cannot support mixed native/managed code for BUFFER class
// because of alignment. We need to run the test as native.
//
#include <mstest.h>

void TestBuffer()
{
    //
    // 104 == 8 byte size rounded, why is this needed?
    //
    STACK_BUFFER(   bufStack,   104 );
    BUFFER          bufReg;
    BUFFER*         pBuf = new BUFFER;

    //
    // QueryPtr
    //
    Assert::IsNotNull( bufStack.QueryPtr( ) );
    Assert::IsNotNull( bufReg.QueryPtr( ) );
    Assert::IsNotNull( pBuf->QueryPtr( ) );

    //
    // QuerySize
    //
    Assert::IsTrue( 104 == bufStack.QuerySize( ) );
    Assert::IsTrue( INLINED_BUFFER_LEN == bufReg.QuerySize( ) );
    Assert::IsTrue( INLINED_BUFFER_LEN == pBuf->QuerySize( ) );

    //
    // Resize
    //
    Assert::IsTrue( bufStack.Resize( 64 ) );
    Assert::IsTrue( bufReg.Resize( 128) );
    Assert::IsTrue( pBuf->Resize( 256 ) );

    //
    // Resize again
    //
    Assert::IsTrue( bufStack.Resize( 512, true ) );
    Assert::IsTrue( bufReg.Resize( 512, true ) );
    Assert::IsTrue( pBuf->Resize( 512, true ) );

    //
    // Resize again
    //
    Assert::IsTrue( bufStack.Resize( 1024, false ) );
    Assert::IsTrue( bufReg.Resize( 1024, false ) );
    Assert::IsTrue( pBuf->Resize( 1024, false ) );

    //
    // write to mem
    //
    ZeroMemory( bufStack.QueryPtr( ), bufStack.QuerySize( ) );
    ZeroMemory( bufReg.QueryPtr( ), bufReg.QuerySize( ) );
    ZeroMemory( pBuf->QueryPtr( ), pBuf->QuerySize( ) );

    delete pBuf;
}

void TestStraOverrun()
{
   STACK_STRA( straStack, 3 );
   wchar_t Input[] = {0x65f6, 0x0};
   HRESULT hr;

   hr = straStack.CopyW(Input);
   Assert::IsTrue( SUCCEEDED(hr) );
   Assert::AreEqual( 3, straStack.QueryCCH(), L"Invalid string length." );
   Assert::AreEqual( 4, straStack.QuerySizeCCH(), L"Invalid buffer length." );
}

#define LOWER_A_THING L"ä"
#define UPPER_A_THING L"Ä"

void TestStru()
{
    STACK_STRU( struStack, 104 );
    STRU        struReg;
    wchar_t    buf[100];
    DWORD       cbBuf = sizeof( buf );

    //
    // IsEmpty
    //
    Assert::IsTrue( struStack.IsEmpty( ) );
    Assert::IsTrue( L'\0' == struStack.QueryStr()[0] );
    Assert::IsTrue( struReg.IsEmpty( ) );

    //
    // Copy psz
    // CopyA psz
    //
    Assert::IsTrue( SUCCEEDED( struStack.Copy( L"hello" ) ) );
    Assert::IsTrue( SUCCEEDED( struReg.CopyA( "hello" ) ) );

    //
    // Equal
    //
    Assert::IsTrue( struStack.Equals( L"hello" ) );
    Assert::IsTrue( !struStack.Equals( L"goodbye" ) );
    Assert::IsTrue( !struStack.Equals( L"" ) );

    STRU strHELLO;
    Assert::IsTrue( SUCCEEDED( strHELLO.Copy( L"HELLO" ) ) );

    Assert::IsTrue( struStack.Equals( &struReg ) );
    Assert::IsTrue( struStack.Equals( struReg ) );

    Assert::IsTrue( !struStack.Equals( &strHELLO ) );
    Assert::IsTrue( !struStack.Equals( strHELLO ) );

    Assert::IsTrue( struStack.Equals( &strHELLO, TRUE ) );
    Assert::IsTrue( struStack.Equals( strHELLO, TRUE ) );

    Assert::IsTrue( struStack.Equals( L"helLO", TRUE ) );


    Assert::IsTrue(  STRU::Equals( L"Hello", L"Hello" ) );
    Assert::IsTrue(  STRU::Equals( L"Hello", L"Hello", FALSE ) );
    Assert::IsTrue(  STRU::Equals( L"Hello", L"Hello", TRUE ) );

    Assert::IsFalse( STRU::Equals( L"hello", L"Hello" ) );
    Assert::IsFalse( STRU::Equals( L"hello", L"Hello", FALSE ) );
    Assert::IsTrue(  STRU::Equals( L"hello", L"Hello", TRUE ) );

    Assert::IsFalse( STRU::Equals( L"hello", L"goodbye" ) );
    Assert::IsFalse( STRU::Equals( L"hello", L"goodbye", FALSE ) );
    Assert::IsFalse( STRU::Equals( L"hello", L"goodbye", TRUE ) );

    Assert::IsFalse( STRU::Equals( (PCWSTR)NULL, (PCWSTR)NULL ) );
    Assert::IsFalse( STRU::Equals( L"hello", (PCWSTR)NULL ) );
    Assert::IsFalse( STRU::Equals( (PCWSTR)NULL, L"hello" ) );



    //
    // Query*
    //
    Assert::IsTrue( 5 * sizeof( wchar_t ) == struStack.QueryCB( ) );
    Assert::IsTrue( 5 == struStack.QueryCCH( ) );
    Assert::IsTrue( 6 <= struStack.QuerySizeCCH( ) );
    Assert::IsTrue( L'h' == *( struStack.QueryStr( ) ) );

    //
    // Resize
    //
    Assert::IsTrue( SUCCEEDED( struReg.Resize( 7 ) ) );
    Assert::IsTrue( 7 == struReg.QuerySizeCCH( ) );

    //
    // SyncWithBuffer
    //
    *(struStack.QueryStr() + 5) = L'\0';
    Assert::AreEqual(S_OK, struStack.SyncWithBuffer( ));
    Assert::IsTrue( 5 == struStack.QueryCCH( ) );

    //
    // Reset
    //
    struStack.Reset( );
    Assert::IsTrue( 0 == wcslen( struStack.QueryStr( ) ) );

    //
    // Append*
    //
    Assert::IsTrue( SUCCEEDED( struStack.Append( L"hell" ) ) );
    Assert::IsTrue( SUCCEEDED( struStack.Append( L"o", 1 ) ) );
    Assert::IsTrue( SUCCEEDED( struStack.Append( &struReg ) ) );
    Assert::IsTrue( SUCCEEDED( struStack.AppendA( "hell" ) ) );
    Assert::IsTrue( SUCCEEDED( struStack.AppendA( "0", 1, CP_ACP ) ) );
    Assert::IsTrue( 15 == wcslen( struStack.QueryStr( ) ) );

    //
    // CopyToBuffer
    //
    Assert::IsTrue( SUCCEEDED( struStack.CopyToBuffer( buf, &cbBuf ) ) );
    Assert::IsTrue( 15 == wcslen( buf ) );
    Assert::IsTrue( 16 * sizeof( wchar_t ) == cbBuf );

    //
    // Trim
    //
    Assert::IsTrue( SUCCEEDED( struStack.Copy(L"              \n\tHello World! \n\t             ") ) );
    struStack.Trim();
    Assert::IsTrue( struStack.Equals(L"Hello World!"));

    Assert::IsTrue( SUCCEEDED( struStack.Copy(L" Test test") ) );
    struStack.Trim();
    Assert::IsTrue( struStack.Equals(L"Test test"));

    Assert::IsTrue( SUCCEEDED( struStack.Copy(L"Test test ") ) );
    struStack.Trim();
    Assert::IsTrue( struStack.Equals(L"Test test"));

    Assert::IsTrue( SUCCEEDED( struStack.Copy(L" Test test ") ) );
    struStack.Trim();
    Assert::IsTrue( struStack.Equals(L"Test test"));

    Assert::IsTrue( SUCCEEDED( struStack.Copy(L" ") ) );
    struStack.Trim();
    Assert::IsTrue( struStack.Equals(L""));

    Assert::IsTrue( SUCCEEDED( struStack.Copy(L"                                          ") ) );
    struStack.Trim();
    Assert::IsTrue( struStack.Equals(L""));

    Assert::IsTrue( SUCCEEDED( struStack.Copy(L"") ) );
    struStack.Trim();
    Assert::IsTrue( struStack.Equals(L""));

    //
    // StartsWith
    //
    Assert::IsTrue( SUCCEEDED( struStack.Copy(L"Just the facts, please.") ) );
    Assert::IsTrue( struStack.StartsWith(L"Just the facts, please.") );
    Assert::IsTrue( struStack.StartsWith(L"Just") );
    Assert::IsTrue( struStack.StartsWith(L"Just the") );
    Assert::IsTrue( !struStack.StartsWith(L"just the") );
    Assert::IsTrue( struStack.StartsWith(L"just The", TRUE) );
    Assert::IsTrue( !struStack.StartsWith((LPCWSTR) NULL, TRUE) );
    Assert::IsTrue( !struStack.StartsWith(L"Just the facts, please...") );

    //
    // EndsWith
    //
    Assert::IsTrue( SUCCEEDED( struStack.Copy(L"The beginning of the end of the beginning.") ) );
    Assert::IsTrue( struStack.EndsWith(L"The beginning of the end of the beginning.") );
    Assert::IsTrue( struStack.EndsWith(L".") );
    Assert::IsTrue( struStack.EndsWith(L"of the beginning.") );
    Assert::IsTrue( !struStack.EndsWith(L"Beginning.") );
    Assert::IsTrue( struStack.EndsWith(L"Beginning.", TRUE) );
    Assert::IsTrue( struStack.EndsWith(L"tHe BeGiNnIng.", TRUE) );
    Assert::IsTrue( !struStack.EndsWith((LPCWSTR) NULL, TRUE) );
    Assert::IsTrue( !struStack.EndsWith(L" The beginning of the end of the beginning.") );

    //
    // IndexOf
    //
    Assert::IsTrue( SUCCEEDED( struStack.Copy(L"01234567890") ) );
    Assert::IsTrue( 0 == struStack.IndexOf( L'0' ) );
    Assert::IsTrue( 1 == struStack.IndexOf( L'1' ) );
    Assert::IsTrue( 2 == struStack.IndexOf( L'2', 1 ) );
    Assert::IsTrue( 10 == struStack.IndexOf( L'0', 1 ) );
    Assert::IsTrue( -1 == struStack.IndexOf( L'A' ) );
    Assert::IsTrue( -1 == struStack.IndexOf( L'0', 20 ) );

    Assert::IsTrue( 0 == struStack.IndexOf( L"0123" ) );
    Assert::IsTrue( -1 == struStack.IndexOf( L"0123", 1 ) );
    Assert::IsTrue( 0 == struStack.IndexOf( L"01234567890" ) );
    Assert::IsTrue( -1 == struStack.IndexOf( L"012345678901" ) );
    Assert::IsTrue( 1 == struStack.IndexOf( L"1234" ) );
    Assert::IsTrue( 1 == struStack.IndexOf( L"1234", 1 ) );
    Assert::IsTrue( -1 == struStack.IndexOf( (PCWSTR)NULL ) );
    Assert::IsTrue( 0 == struStack.IndexOf( L"" ) );
    Assert::IsTrue( -1 == struStack.IndexOf( L"", 20 ) );

    //
    // LastIndexOf
    //
    Assert::IsTrue( 10 == struStack.LastIndexOf( L'0' ) );
    Assert::IsTrue( 1 == struStack.LastIndexOf( L'1' ) );
    Assert::IsTrue( 2 == struStack.LastIndexOf( L'2', 1 ) );
    Assert::IsTrue( 10 == struStack.LastIndexOf( L'0', 1 ) );
    Assert::IsTrue( -1 == struStack.LastIndexOf( L'A' ) );
    Assert::IsTrue( -1 == struStack.LastIndexOf( L'0', 20 ) );

    //
    // SetLen
    //
    Assert::IsTrue( SUCCEEDED( struStack.SetLen( 2 ) ) );
    Assert::IsTrue( 2 == struStack.QueryCCH( ) );

#if defined( NTDDI_VERSION ) && NTDDI_VERSION >= NTDDI_LONGHORN

    //
    // OS-locale case-insensitive compare
    // Note how the two case-insensitive comparisons have different expected results
    //
    Assert::IsTrue( SUCCEEDED( struStack.Copy( LOWER_A_THING ) ) );
    Assert::IsTrue( SUCCEEDED( struReg.Copy( UPPER_A_THING ) ) );
    Assert::IsTrue( !struStack.Equals( &struReg ) );
    Assert::IsTrue( struStack.Equals( &struReg, TRUE ) );
    Assert::IsTrue( 0 != _wcsicmp( LOWER_A_THING, UPPER_A_THING ) );

#endif

    Assert::IsTrue( SUCCEEDED( struReg.SafeSnwprintf( L"%s%d", L"Hello", 10 ) ) );

    //
    // Fail since there is no null-terminating char.
    //
    struStack.Reset();
    struStack.Resize(200);
    memset(struStack.QueryStr(), 'x', 200 * sizeof(WCHAR));
    Assert::AreNotEqual(S_OK, struStack.SyncWithBuffer());
}

void TestStra()
{
    STACK_STRA( straStack, 104 );
    STRA        straReg;
    char        buf[100];
    DWORD       cbBuf = sizeof( buf );

    //
    // IsEmpty
    //
    Assert::IsTrue( straStack.IsEmpty( ) );
    Assert::IsTrue( '\0' == straStack.QueryStr()[0] );
    Assert::IsTrue( straReg.IsEmpty( ) );

    //
    // Copy psz
    // CopyW psz
    //
    Assert::IsTrue( SUCCEEDED( straStack.Copy( "hello" ) ) );
    Assert::IsTrue( SUCCEEDED( straReg.CopyW( L"hello" ) ) );

    //
    // Equal
    //
    Assert::IsTrue( straStack.Equals( "hello" ) );
    Assert::IsTrue( straStack.Equals( &straReg ) );
    Assert::IsTrue( straStack.Equals( "helLO", TRUE ) );


    Assert::IsTrue(  STRA::Equals( "Hello", "Hello" ) );
    Assert::IsTrue(  STRA::Equals( "Hello", "Hello", FALSE ) );
    Assert::IsTrue(  STRA::Equals( "Hello", "Hello", TRUE ) );

    Assert::IsFalse( STRA::Equals( "hello", "Hello" ) );
    Assert::IsFalse( STRA::Equals( "hello", "Hello", FALSE ) );
    Assert::IsTrue(  STRA::Equals( "hello", "Hello", TRUE ) );

    Assert::IsFalse( STRA::Equals( "hello", "goodbye" ) );
    Assert::IsFalse( STRA::Equals( "hello", "goodbye", FALSE ) );
    Assert::IsFalse( STRA::Equals( "hello", "goodbye", TRUE ) );

    Assert::IsFalse( STRA::Equals( (PCSTR)NULL, (PCSTR)NULL ) );
    Assert::IsFalse( STRA::Equals( "hello", (PCSTR)NULL ) );
    Assert::IsFalse( STRA::Equals( (PCSTR)NULL, "hello" ) );

    //
    // Query*
    //
    Assert::IsTrue( 5 * sizeof( char ) == straStack.QueryCB( ) );
    Assert::IsTrue( 5 == straStack.QueryCCH( ) );
    Assert::IsTrue( 6 <= straStack.QuerySizeCCH( ) );
    Assert::IsTrue( 'h' == *( straStack.QueryStr( ) ) );

    //
    // Resize
    //
    Assert::IsTrue( SUCCEEDED( straReg.Resize( 7 ) ) );
    Assert::IsTrue( 7 == straReg.QuerySizeCCH( ) );

    //
    // SyncWithBuffer
    //
    *(straStack.QueryStr() + 5) = L'\0';
    Assert::AreEqual(S_OK, straStack.SyncWithBuffer( ));
    Assert::IsTrue( 5 == straStack.QueryCCH( ) );

    //
    // Reset
    //
    straStack.Reset( );
    Assert::IsTrue( 0 == strlen( straStack.QueryStr( ) ) );

    //
    // Append*
    //
    Assert::IsTrue( SUCCEEDED( straStack.Append( "hell" ) ) );
    Assert::IsTrue( SUCCEEDED( straStack.Append( "o", 1 ) ) );
    Assert::IsTrue( SUCCEEDED( straStack.Append( &straReg ) ) );
    Assert::IsTrue( SUCCEEDED( straStack.AppendW( L"hell" ) ) );
    Assert::IsTrue( SUCCEEDED( straStack.AppendW( L"0", 1, CP_ACP ) ) );
    Assert::IsTrue( 15 == strlen( straStack.QueryStr( ) ) );

    //
    // CopyToBuffer
    //
    Assert::IsTrue( SUCCEEDED( straStack.CopyToBuffer( buf, &cbBuf ) ) );
    Assert::IsTrue( 15 == strlen( buf ) );
    Assert::IsTrue( 16 * sizeof( char ) == cbBuf );

    //
    // Trim
    //
    Assert::IsTrue( SUCCEEDED( straStack.Copy("              \n\tHello World! \n\t             ") ) );
    straStack.Trim();
    Assert::IsTrue( straStack.Equals("Hello World!"));

    Assert::IsTrue( SUCCEEDED( straStack.Copy(" Test test") ) );
    straStack.Trim();
    Assert::IsTrue( straStack.Equals("Test test"));

    Assert::IsTrue( SUCCEEDED( straStack.Copy("Test test ") ) );
    straStack.Trim();
    Assert::IsTrue( straStack.Equals("Test test"));

    Assert::IsTrue( SUCCEEDED( straStack.Copy(" Test test ") ) );
    straStack.Trim();
    Assert::IsTrue( straStack.Equals("Test test"));

    Assert::IsTrue( SUCCEEDED( straStack.Copy(" ") ) );
    straStack.Trim();
    Assert::IsTrue( straStack.Equals(""));

    Assert::IsTrue( SUCCEEDED( straStack.Copy("                                          ") ) );
    straStack.Trim();
    Assert::IsTrue( straStack.Equals(""));

    Assert::IsTrue( SUCCEEDED( straStack.Copy("") ) );
    straStack.Trim();
    Assert::IsTrue( straStack.Equals(""));

    //
    // StartsWith
    //
    Assert::IsTrue( SUCCEEDED( straStack.Copy("Just the facts, please.") ) );
    Assert::IsTrue( straStack.StartsWith("Just the facts, please.") );
    Assert::IsTrue( straStack.StartsWith("Just") );
    Assert::IsTrue( straStack.StartsWith("Just the") );
    Assert::IsTrue( !straStack.StartsWith("just the") );
    Assert::IsTrue( straStack.StartsWith("just The", TRUE) );
    Assert::IsTrue( !straStack.StartsWith((LPCSTR) NULL, TRUE) );
    Assert::IsTrue( !straStack.StartsWith("Just the facts, please...") );

    //
    // EndsWith
    //
    Assert::IsTrue( SUCCEEDED( straStack.Copy("The beginning of the end of the beginning.") ) );
    Assert::IsTrue( straStack.EndsWith("The beginning of the end of the beginning.") );
    Assert::IsTrue( straStack.EndsWith(".") );
    Assert::IsTrue( straStack.EndsWith("of the beginning.") );
    Assert::IsTrue( !straStack.EndsWith("Beginning.") );
    Assert::IsTrue( straStack.EndsWith("Beginning.", TRUE) );
    Assert::IsTrue( straStack.EndsWith("tHe BeGiNnIng.", TRUE) );
    Assert::IsTrue( !straStack.EndsWith((LPCSTR) NULL, TRUE) );
    Assert::IsTrue( !straStack.EndsWith(" The beginning of the end of the beginning.") );

    //
    // IndexOf
    //
    Assert::IsTrue( SUCCEEDED( straStack.Copy("01234567890") ) );
    Assert::IsTrue( 0 == straStack.IndexOf( '0' ) );
    Assert::IsTrue( 1 == straStack.IndexOf( '1' ) );
    Assert::IsTrue( 2 == straStack.IndexOf( '2', 1 ) );
    Assert::IsTrue( 10 == straStack.IndexOf( '0', 1 ) );
    Assert::IsTrue( -1 == straStack.IndexOf( 'A' ) );
    Assert::IsTrue( -1 == straStack.IndexOf( '0', 20 ) );

    Assert::IsTrue( 0 == straStack.IndexOf( "0123" ) );
    Assert::IsTrue( -1 == straStack.IndexOf( "0123", 1 ) );
    Assert::IsTrue( 0 == straStack.IndexOf( "01234567890" ) );
    Assert::IsTrue( -1 == straStack.IndexOf( "012345678901" ) );
    Assert::IsTrue( 1 == straStack.IndexOf( "1234" ) );
    Assert::IsTrue( 1 == straStack.IndexOf( "1234", 1 ) );
    Assert::IsTrue( -1 == straStack.IndexOf( (PCSTR)NULL ) );
    Assert::IsTrue( 0 == straStack.IndexOf( "" ) );
    Assert::IsTrue( -1 == straStack.IndexOf( "", 20 ) );

    //
    // LastIndexOf
    //
    Assert::IsTrue( 10 == straStack.LastIndexOf( '0' ) );
    Assert::IsTrue( 1 == straStack.LastIndexOf( '1' ) );
    Assert::IsTrue( 2 == straStack.LastIndexOf( '2', 1 ) );
    Assert::IsTrue( 10 == straStack.LastIndexOf( '0', 1 ) );
    Assert::IsTrue( -1 == straStack.LastIndexOf( 'A' ) );
    Assert::IsTrue( -1 == straStack.LastIndexOf( '0', 20 ) );

    //
    // SetLen
    //
    Assert::IsTrue( SUCCEEDED( straStack.SetLen( 2 ) ) );
    Assert::IsTrue( 2 == straStack.QueryCCH( ) );


    //
    // Convert.
    //
    {
        STRA str;
        wchar_t psz[] = {0x41, L'Ã', 0x0};
        char pszA[] = {0x41, 'Ã', 0x0};
        Assert::IsTrue( SUCCEEDED(str.CopyW((LPCWSTR)psz, 2, CP_ACP )) );
        Assert::IsTrue( 0 == strcmp( pszA, str.QueryStr() ) );
    }
    //
    // Empty
    //
    {
        STRA str;
        wchar_t psz[] = {0x0};
        char pszA[] = {0x0};
        Assert::IsTrue( SUCCEEDED(str.CopyW((LPCWSTR)psz, 0, CP_ACP )) );
        Assert::IsTrue( 0 == strcmp( pszA, str.QueryStr() ) );
    }

    //
    // Fail since there is no null-terminating char.
    //
    straStack.Reset();
    straStack.Resize(200);
    memset(straStack.QueryStr(), 'x', 200);
    Assert::AreNotEqual(S_OK, straStack.SyncWithBuffer());
}

VOID
AsciiAssert(char * str1, char * str2, size_t length)
{
    for ( size_t index = 0; index < length; ++index )
    {
        Assert::AreEqual(str1[index], str2[index]);
    }
}

void
TestStraUnicode()
{
    STRA str;
    HRESULT hr = S_OK;

    //
    // Tool used to convert unicode to UTF-8 code points and hexadecimal code points:
    // http://rishida.net/scripts/uniview/conversion.php
    //

    //
    // Input values to play with.
    //

    // Real unicode string.
    LPCWSTR InputRealUnicode = L"?q=世加";

    // This is the same value than InputRealUnicode, but represented as an array.
    wchar_t InputRealUnicodeArray[] =
    {
        0x3F,   // ?
        0x71,   // q
        0x3D,   // =
        0x4E16, // 世
        0x52A0, // 加
        0x00    // L'\0'
    };

    wchar_t InputAscii[] =
    {
        0x3F,   // ?
        0x71,   // q
        0x3D,   // =
        0x7F,   // 127
        0x00    // L'\0'
    };

    // Fake unicode
    // UTF-8 code units in 'wchar_t' chars instead of 'char' chars.
    // This is how WinHttp returns the query string.
    wchar_t InputFakeUnicode[] =
    {
        0x3F, // ?
        0x71, // q
        0x3D, // =
        0xE4, // 1st code unit for '世'
        0xB8, // 2nd code unit for '世'
        0x96, // 3rd code unit for '世'
        0xE5, // 1st code unit for '加'
        0x8A, // 2nd code unit for '加'
        0xA0, // 3rd code unit for '加'
        0x00  // L'\0'
    };

    //
    // Expected values after translation.
    //

    unsigned char ExpectedAsciiCodeUnits[] =
    {
        0x3F, // ?
        0x71, // q
        0x3D, // =
        0xE4, // 1st code unit for '世'
        0xB8, // 2nd code unit for '世'
        0x96, // 3rd code unit for '世'
        0xE5, // 1st code unit for '加'
        0x8A, // 2nd code unit for '加'
        0xA0, // 3rd code unit for '加'
        0x00  // L'\0'
    };

    char ExpectedAscii[] =
    {
        0x3F,   // ?
        0x71,   // q
        0x3D,   // =
        0x7F,   // 127
        0x00    // L'\0'
    };

    //
    // Act and Assert.
    //

    hr = str.CopyW(InputRealUnicode);
    Assert::AreEqual(S_OK, hr);
    Assert::AreEqual(9UL, str.QueryCCH(), L"Invalid real unicode query string length.");
    AsciiAssert( (char*)ExpectedAsciiCodeUnits, str.QueryStr(), str.QueryCCH() );

    hr = str.CopyW(InputRealUnicodeArray);
    Assert::AreEqual(S_OK, hr);
    Assert::AreEqual(9UL, str.QueryCCH(), L"Invalid real unicode query string length.");
    AsciiAssert( (char*)ExpectedAsciiCodeUnits, str.QueryStr(), str.QueryCCH() );

    hr = str.CopyWTruncate(InputFakeUnicode);
    Assert::AreEqual(S_OK, hr);
    Assert::AreEqual(9UL, str.QueryCCH(), L"Invalid truncated fake unicode query string length.");
    AsciiAssert( (char*)ExpectedAsciiCodeUnits, str.QueryStr(), str.QueryCCH() );

    hr = str.CopyWTruncate(InputAscii);
    Assert::AreEqual(S_OK, hr);
    Assert::AreEqual(4UL, str.QueryCCH(), L"Invalid truncated ASCII query string length.");
    AsciiAssert( ExpectedAscii, str.QueryStr(), str.QueryCCH() );

    hr = str.CopyW(InputAscii);
    Assert::AreEqual(S_OK, hr);
    Assert::AreEqual(4UL, str.QueryCCH(), L"Invalid CopyW ASCII query string length.");
    AsciiAssert( ExpectedAscii, str.QueryStr(), str.QueryCCH() );

}

#pragma managed

using namespace Microsoft::VisualStudio::TestTools::UnitTesting;

[TestClass]
public ref class StringTests
{
public:

    [TestMethod]
    void BufferTest()
    {
        ::TestBuffer();
    }

    [TestMethod]
    void StruTest()
    {
        ::TestStru();
    }

    [TestMethod]
    void StraTest()
    {
        ::TestStra();
    }

    [TestMethod]
    void TestStraOverrun()
    {
        ::TestStraOverrun();
    }

    [TestMethod]
    void StraUnicodeTest()
    {
        ::TestStraUnicode();
    }
};
