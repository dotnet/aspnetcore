// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Microsoft.Web.Management.PInvoke.MLang
{
    internal enum HRESULT : uint
    {
        S_OK = 0x0,
        S_FALSE = 0x1,
        E_FAIL = 0x80004005,
    }

    internal static class NativeMethods
    {
        [StructLayout(LayoutKind.Explicit, Pack = 4)]
        public struct __MIDL_IWinTypes_0009
        {
            // Fields
            [FieldOffset(0)]
            public int hInproc;
            [FieldOffset(0)]
            public int hRemote;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct _RemotableHandle
        {
            public int fContext;
            public __MIDL_IWinTypes_0009 u;
        }

        [ComImport, CoClass(typeof(CMLangConvertCharsetClass)), Guid("D66D6F98-CDAA-11D0-B822-00C04FC9B31F")]
        public interface CMLangConvertCharset : IMLangConvertCharset
        {
        }

        [ComImport, TypeLibType((short)2), Guid("D66D6F99-CDAA-11D0-B822-00C04FC9B31F"), ClassInterface((short)0)]
        public class CMLangConvertCharsetClass : IMLangConvertCharset, CMLangConvertCharset
        {
            // Methods
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void DoConversion([In] ref byte pSrcStr, [In, Out] ref uint pcSrcSize, out byte pDstStr, [In, Out] ref uint pcDstSize);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void DoConversionFromUnicode([In] ref ushort pSrcStr, [In, Out] ref uint pcSrcSize, out sbyte pDstStr, [In, Out] ref uint pcDstSize);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void DoConversionToUnicode([In] ref sbyte pSrcStr, [In, Out] ref uint pcSrcSize, out ushort pDstStr, [In, Out] ref uint pcDstSize);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void GetDestinationCodePage(out uint puiDstCodePage);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void GetProperty(out uint pdwProperty);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void GetSourceCodePage(out uint puiSrcCodePage);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void Initialize([In] uint uiSrcCodePage, [In] uint uiDstCodePage, [In] uint dwProperty);
        }

        [ComImport, Guid("C04D65CE-B70D-11D0-B188-00AA0038C969"), CoClass(typeof(CMLangStringClass))]
        public interface CMLangString : IMLangString
        {
        }

        [ComImport, TypeLibType((short)2), Guid("C04D65CF-B70D-11D0-B188-00AA0038C969"), ClassInterface((short)0)]
        public class CMLangStringClass : IMLangString, CMLangString, IMLangStringWStr, IMLangStringAStr
        {
            // Methods
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void GetAStr([In] int lSrcPos, [In] int lSrcLen, [In] uint uCodePageIn, out uint puCodePageOut, [Out, MarshalAs(UnmanagedType.LPStr)] string pszDest, [In] int cchDest, out int pcchActual, out int plActualLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern int GetLength();
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void GetLocale([In] int lSrcPos, [In] int lSrcMaxLen, out uint plocale, out int plLocalePos, out int plLocaleLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void GetMLStr([In] int lSrcPos, [In] int lSrcLen, [In, MarshalAs(UnmanagedType.IUnknown)] object pUnkOuter, [In] uint dwClsContext, [In] ref Guid piid, [MarshalAs(UnmanagedType.IUnknown)] out object ppDestMLStr, out int plDestPos, out int plDestLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void GetStrBufA([In] int lSrcPos, [In] int lSrcMaxLen, out uint puDestCodePage, [MarshalAs(UnmanagedType.Interface)] out IMLangStringBufA ppDestBuf, out int plDestLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void GetStrBufW([In] int lSrcPos, [In] int lSrcMaxLen, [MarshalAs(UnmanagedType.Interface)] out IMLangStringBufW ppDestBuf, out int plDestLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void GetWStr([In] int lSrcPos, [In] int lSrcLen, [Out, MarshalAs(UnmanagedType.LPWStr)] string pszDest, [In] int cchDest, out int pcchActual, out int plActualLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern int IMLangStringAStr_GetLength();
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMLangStringAStr_GetLocale([In] int lSrcPos, [In] int lSrcMaxLen, out uint plocale, out int plLocalePos, out int plLocaleLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMLangStringAStr_GetMLStr([In] int lSrcPos, [In] int lSrcLen, [In, MarshalAs(UnmanagedType.IUnknown)] object pUnkOuter, [In] uint dwClsContext, [In] ref Guid piid, [MarshalAs(UnmanagedType.IUnknown)] out object ppDestMLStr, out int plDestPos, out int plDestLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMLangStringAStr_SetLocale([In] int lDestPos, [In] int lDestLen, [In] uint locale);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMLangStringAStr_SetMLStr([In] int lDestPos, [In] int lDestLen, [In, MarshalAs(UnmanagedType.IUnknown)] object pSrcMLStr, [In] int lSrcPos, [In] int lSrcLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMLangStringAStr_Sync([In] int fNoAccess);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern int IMLangStringWStr_GetLength();
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMLangStringWStr_GetMLStr([In] int lSrcPos, [In] int lSrcLen, [In, MarshalAs(UnmanagedType.IUnknown)] object pUnkOuter, [In] uint dwClsContext, [In] ref Guid piid, [MarshalAs(UnmanagedType.IUnknown)] out object ppDestMLStr, out int plDestPos, out int plDestLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMLangStringWStr_SetMLStr([In] int lDestPos, [In] int lDestLen, [In, MarshalAs(UnmanagedType.IUnknown)] object pSrcMLStr, [In] int lSrcPos, [In] int lSrcLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMLangStringWStr_Sync([In] int fNoAccess);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void LockAStr([In] int lSrcPos, [In] int lSrcLen, [In] int lFlags, [In] uint uCodePageIn, [In] int cchRequest, out uint puCodePageOut, [MarshalAs(UnmanagedType.LPStr)] out string ppszDest, out int pcchDest, out int plDestLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void LockWStr([In] int lSrcPos, [In] int lSrcLen, [In] int lFlags, [In] int cchRequest, [MarshalAs(UnmanagedType.LPWStr)] out string ppszDest, out int pcchDest, out int plDestLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void SetAStr([In] int lDestPos, [In] int lDestLen, [In] uint uCodePage, [In, MarshalAs(UnmanagedType.LPStr)] string pszSrc, [In] int cchSrc, out int pcchActual, out int plActualLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void SetLocale([In] int lDestPos, [In] int lDestLen, [In] uint locale);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void SetMLStr([In] int lDestPos, [In] int lDestLen, [In, MarshalAs(UnmanagedType.IUnknown)] object pSrcMLStr, [In] int lSrcPos, [In] int lSrcLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void SetStrBufA([In] int lDestPos, [In] int lDestLen, [In] uint uCodePage, [In, MarshalAs(UnmanagedType.Interface)] IMLangStringBufA pSrcBuf, out int pcchActual, out int plActualLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void SetStrBufW([In] int lDestPos, [In] int lDestLen, [In, MarshalAs(UnmanagedType.Interface)] IMLangStringBufW pSrcBuf, out int pcchActual, out int plActualLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void SetWStr([In] int lDestPos, [In] int lDestLen, [In, MarshalAs(UnmanagedType.LPWStr)] string pszSrc, [In] int cchSrc, out int pcchActual, out int plActualLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void Sync([In] int fNoAccess);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void UnlockAStr([In, MarshalAs(UnmanagedType.LPStr)] string pszSrc, [In] int cchSrc, out int pcchActual, out int plActualLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void UnlockWStr([In, MarshalAs(UnmanagedType.LPWStr)] string pszSrc, [In] int cchSrc, out int pcchActual, out int plActualLen);
        }

        [ComImport, Guid("275C23E1-3747-11D0-9FEA-00AA003F8646"), CoClass(typeof(CMultiLanguageClass))]
        public interface CMultiLanguage : IMultiLanguage
        {
        }

        [ComImport, TypeLibType(TypeLibTypeFlags.FCanCreate), ClassInterface(ClassInterfaceType.None), Guid("275C23E2-3747-11D0-9FEA-00AA003F8646")]
        public class CMultiLanguageClass : IMultiLanguage, CMultiLanguage, IMLangCodePages, IMLangFontLink, IMLangLineBreakConsole, IMultiLanguage2, IMLangFontLink2, IMultiLanguage3
        {
            // Methods
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void BreakLineA([In] uint locale, [In] uint uCodePage, [In] ref sbyte pszSrc, [In] int cchSrc, [In] int cMaxColumns, out int pcchLine, out int pcchSkip);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void BreakLineML([In, MarshalAs(UnmanagedType.Interface)] CMLangString pSrcMLStr, [In] int lSrcPos, [In] int lSrcLen, [In] int cMinColumns, [In] int cMaxColumns, out int plLineLen, out int plSkipLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void BreakLineW([In] uint locale, [In] ref ushort pszSrc, [In] int cchSrc, [In] int cMaxColumns, out int pcchLine, out int pcchSkip);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void CodePagesToCodePage([In] uint dwCodePages, [In] uint uDefaultCodePage, out uint puCodePage);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void CodePageToCodePages([In] uint uCodePage, out uint pdwCodePages);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void CodePageToScriptID([In] uint uiCodePage, out byte pSid);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void ConvertString([In, Out] ref uint pdwMode, [In] uint dwSrcEncoding, [In] uint dwDstEncoding, [In] ref byte pSrcStr, [In, Out] ref uint pcSrcSize, out byte pDstStr, [In, Out] ref uint pcDstSize);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void ConvertStringFromUnicode([In, Out] ref uint pdwMode, [In] uint dwEncoding, [In] ref ushort pSrcStr, [In, Out] ref uint pcSrcSize, out sbyte pDstStr, [In, Out] ref uint pcDstSize);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void ConvertStringFromUnicodeEx([In, Out] ref uint pdwMode, [In] uint dwEncoding, [In] ref ushort pSrcStr, [In, Out] ref uint pcSrcSize, out sbyte pDstStr, [In, Out] ref uint pcDstSize, [In] uint dwFlag, [In] ref ushort lpFallBack);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void ConvertStringInIStream([In, Out] ref uint pdwMode, [In] uint dwFlag, [In] ref ushort lpFallBack, [In] uint dwSrcEncoding, [In] uint dwDstEncoding, [In, MarshalAs(UnmanagedType.Interface)] IStream pstmIn, [In, MarshalAs(UnmanagedType.Interface)] IStream pstmOut);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void ConvertStringReset();
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void ConvertStringToUnicode([In, Out] ref uint pdwMode, [In] uint dwEncoding, [In] ref sbyte pSrcStr, [In, Out] ref uint pcSrcSize, out ushort pDstStr, [In, Out] ref uint pcDstSize);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void ConvertStringToUnicodeEx([In, Out] ref uint pdwMode, [In] uint dwEncoding, [In] ref sbyte pSrcStr, [In, Out] ref uint pcSrcSize, out ushort pDstStr, [In, Out] ref uint pcDstSize, [In] uint dwFlag, [In] ref ushort lpFallBack);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void CreateConvertCharset([In] uint uiSrcCodePage, [In] uint uiDstCodePage, [In] uint dwProperty, [MarshalAs(UnmanagedType.Interface)] out CMLangConvertCharset ppMLangConvertCharset);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void DetectCodepageInIStream([In] uint dwFlag, [In] uint dwPrefWinCodePage, [In, MarshalAs(UnmanagedType.Interface)] IStream pstmIn, out DetectEncodingInfo lpEncoding, [In, Out] ref int pnScores);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            [PreserveSig]
            public virtual extern HRESULT DetectInputCodepage([In] MLDETECTCP dwFlag, [In] uint dwPrefWinCodePage, [In] ref byte pSrcStr, [In, Out] ref int pcSrcSize, ref DetectEncodingInfo lpEncoding, [In, Out] ref int pnScores);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void DetectOutboundCodePage([In] uint dwFlags, [In, MarshalAs(UnmanagedType.LPWStr)] string lpWideCharStr, [In] uint cchWideChar, [In] ref uint puiPreferredCodePages, [In] uint nPreferredCodePages, out uint puiDetectedCodePages, [In, Out] ref uint pnDetectedCodePages, [In, MarshalAs(UnmanagedType.LPWStr)] string lpSpecialChar);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void DetectOutboundCodePageInIStream([In] uint dwFlags, [In, MarshalAs(UnmanagedType.Interface)] IStream pStrIn, [In] ref uint puiPreferredCodePages, [In] uint nPreferredCodePages, out uint puiDetectedCodePages, [In, Out] ref uint pnDetectedCodePages, [In, MarshalAs(UnmanagedType.LPWStr)] string lpSpecialChar);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            [return: MarshalAs(UnmanagedType.Interface)]
            public virtual extern IEnumCodePage EnumCodePages([In] NativeMethods.MIMECONTF grfFlags);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void EnumCodePages([In] uint grfFlags, [In] ushort LangId, [MarshalAs(UnmanagedType.Interface)] out IEnumCodePage ppEnumCodePage);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void EnumRfc1766([MarshalAs(UnmanagedType.Interface)] out IEnumRfc1766 ppEnumRfc1766);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void EnumRfc1766([In] ushort LangId, [MarshalAs(UnmanagedType.Interface)] out IEnumRfc1766 ppEnumRfc1766);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void EnumScripts([In] uint dwFlags, [In] ushort LangId, [MarshalAs(UnmanagedType.Interface)] out IEnumScript ppEnumScript);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void GetCharCodePages([In] ushort chSrc, out uint pdwCodePages);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void GetCharsetInfo([In, MarshalAs(UnmanagedType.BStr)] string Charset, out MIMECSETINFO pCharsetInfo);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void GetCodePageDescription([In] uint uiCodePage, [In] uint lcid, [Out, MarshalAs(UnmanagedType.LPWStr)] string lpWideCharStr, [In] int cchWideChar);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void GetCodePageInfo([In] uint uiCodePage, out MIMECPINFO pCodePageInfo);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void GetCodePageInfo([In] uint uiCodePage, [In] ushort LangId, out MIMECPINFO pCodePageInfo);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void GetFamilyCodePage([In] uint uiCodePage, out uint puiFamilyCodePage);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void GetFontCodePages([In, ComAliasName("MultiLanguage.wireHDC")] ref _RemotableHandle hDC, [In, ComAliasName("MultiLanguage.wireHFONT")] ref _RemotableHandle hFont, out uint pdwCodePages);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void GetFontUnicodeRanges([In, ComAliasName("MultiLanguage.wireHDC")] ref _RemotableHandle hDC, [In, Out] ref uint puiRanges, out UNICODERANGE pUranges);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void GetLcidFromRfc1766(out uint plocale, [In, MarshalAs(UnmanagedType.BStr)] string bstrRfc1766);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void GetNumberOfCodePageInfo(out uint pcCodePage);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void GetNumberOfScripts(out uint pnScripts);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void GetRfc1766FromLcid([In] uint locale, [MarshalAs(UnmanagedType.BStr)] out string pbstrRfc1766);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void GetRfc1766Info([In] uint locale, out RFC1766INFO pRfc1766Info);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void GetRfc1766Info([In] uint locale, [In] ushort LangId, out RFC1766INFO pRfc1766Info);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void GetScriptFontInfo([In] byte sid, [In] uint dwFlags, [In, Out] ref uint puiFonts, out SCRIPFONTINFO pScriptFont);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void GetStrCodePages([In] ref ushort pszSrc, [In] int cchSrc, [In] uint dwPriorityCodePages, out uint pdwCodePages, out int pcchCodePages);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMLangFontLink_CodePagesToCodePage([In] uint dwCodePages, [In] uint uDefaultCodePage, out uint puCodePage);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMLangFontLink_CodePageToCodePages([In] uint uCodePage, out uint pdwCodePages);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMLangFontLink_GetCharCodePages([In] ushort chSrc, out uint pdwCodePages);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMLangFontLink_GetStrCodePages([In] ref ushort pszSrc, [In] int cchSrc, [In] uint dwPriorityCodePages, out uint pdwCodePages, out int pcchCodePages);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMLangFontLink2_CodePagesToCodePage([In] uint dwCodePages, [In] uint uDefaultCodePage, out uint puCodePage);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMLangFontLink2_CodePageToCodePages([In] uint uCodePage, out uint pdwCodePages);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMLangFontLink2_GetCharCodePages([In] ushort chSrc, out uint pdwCodePages);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMLangFontLink2_GetFontCodePages([In, ComAliasName("MultiLanguage.wireHDC")] ref _RemotableHandle hDC, [In, ComAliasName("MultiLanguage.wireHFONT")] ref _RemotableHandle hFont, out uint pdwCodePages);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMLangFontLink2_GetStrCodePages([In] ref ushort pszSrc, [In] int cchSrc, [In] uint dwPriorityCodePages, out uint pdwCodePages, out int pcchCodePages);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMLangFontLink2_ReleaseFont([In, ComAliasName("MultiLanguage.wireHFONT")] ref _RemotableHandle hFont);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMLangFontLink2_ResetFontMapping();
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage2_ConvertString([In, Out] ref uint pdwMode, [In] uint dwSrcEncoding, [In] uint dwDstEncoding, [In] ref byte pSrcStr, [In, Out] ref uint pcSrcSize, out byte pDstStr, [In, Out] ref uint pcDstSize);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage2_ConvertStringFromUnicode([In, Out] ref uint pdwMode, [In] uint dwEncoding, [In] ref ushort pSrcStr, [In, Out] ref uint pcSrcSize, out sbyte pDstStr, [In, Out] ref uint pcDstSize);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage2_ConvertStringReset();
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage2_ConvertStringToUnicode([In, Out] ref uint pdwMode, [In] uint dwEncoding, [In] ref sbyte pSrcStr, [In, Out] ref uint pcSrcSize, out ushort pDstStr, [In, Out] ref uint pcDstSize);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage2_CreateConvertCharset([In] uint uiSrcCodePage, [In] uint uiDstCodePage, [In] uint dwProperty, [MarshalAs(UnmanagedType.Interface)] out CMLangConvertCharset ppMLangConvertCharset);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage2_GetCharsetInfo([In, MarshalAs(UnmanagedType.BStr)] string Charset, out MIMECSETINFO pCharsetInfo);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage2_GetFamilyCodePage([In] uint uiCodePage, out uint puiFamilyCodePage);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage2_GetLcidFromRfc1766(out uint plocale, [In, MarshalAs(UnmanagedType.BStr)] string bstrRfc1766);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage2_GetNumberOfCodePageInfo(out uint pcCodePage);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage2_GetRfc1766FromLcid([In] uint locale, [MarshalAs(UnmanagedType.BStr)] out string pbstrRfc1766);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage2_IsConvertible([In] uint dwSrcEncoding, [In] uint dwDstEncoding);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage3_ConvertString([In, Out] ref uint pdwMode, [In] uint dwSrcEncoding, [In] uint dwDstEncoding, [In] ref byte pSrcStr, [In, Out] ref uint pcSrcSize, out byte pDstStr, [In, Out] ref uint pcDstSize);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage3_ConvertStringFromUnicode([In, Out] ref uint pdwMode, [In] uint dwEncoding, [In] ref ushort pSrcStr, [In, Out] ref uint pcSrcSize, out sbyte pDstStr, [In, Out] ref uint pcDstSize);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage3_ConvertStringFromUnicodeEx([In, Out] ref uint pdwMode, [In] uint dwEncoding, [In] ref ushort pSrcStr, [In, Out] ref uint pcSrcSize, out sbyte pDstStr, [In, Out] ref uint pcDstSize, [In] uint dwFlag, [In] ref ushort lpFallBack);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage3_ConvertStringInIStream([In, Out] ref uint pdwMode, [In] uint dwFlag, [In] ref ushort lpFallBack, [In] uint dwSrcEncoding, [In] uint dwDstEncoding, [In, MarshalAs(UnmanagedType.Interface)] IStream pstmIn, [In, MarshalAs(UnmanagedType.Interface)] IStream pstmOut);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage3_ConvertStringReset();
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage3_ConvertStringToUnicode([In, Out] ref uint pdwMode, [In] uint dwEncoding, [In] ref sbyte pSrcStr, [In, Out] ref uint pcSrcSize, out ushort pDstStr, [In, Out] ref uint pcDstSize);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage3_ConvertStringToUnicodeEx([In, Out] ref uint pdwMode, [In] uint dwEncoding, [In] ref sbyte pSrcStr, [In, Out] ref uint pcSrcSize, out ushort pDstStr, [In, Out] ref uint pcDstSize, [In] uint dwFlag, [In] ref ushort lpFallBack);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage3_CreateConvertCharset([In] uint uiSrcCodePage, [In] uint uiDstCodePage, [In] uint dwProperty, [MarshalAs(UnmanagedType.Interface)] out CMLangConvertCharset ppMLangConvertCharset);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage3_DetectCodepageInIStream([In] uint dwFlag, [In] uint dwPrefWinCodePage, [In, MarshalAs(UnmanagedType.Interface)] IStream pstmIn, out DetectEncodingInfo lpEncoding, [In, Out] ref int pnScores);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage3_DetectInputCodepage([In] uint dwFlag, [In] uint dwPrefWinCodePage, [In] ref sbyte pSrcStr, [In, Out] ref int pcSrcSize, out DetectEncodingInfo lpEncoding, [In, Out] ref int pnScores);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage3_EnumCodePages([In] NativeMethods.MIMECONTF grfFlags, [In] ushort LangId, [MarshalAs(UnmanagedType.Interface)] out IEnumCodePage ppEnumCodePage);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage3_EnumRfc1766([In] ushort LangId, [MarshalAs(UnmanagedType.Interface)] out IEnumRfc1766 ppEnumRfc1766);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage3_EnumScripts([In] uint dwFlags, [In] ushort LangId, [MarshalAs(UnmanagedType.Interface)] out IEnumScript ppEnumScript);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage3_GetCharsetInfo([In, MarshalAs(UnmanagedType.BStr)] string Charset, out MIMECSETINFO pCharsetInfo);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage3_GetCodePageDescription([In] uint uiCodePage, [In] uint lcid, [Out, MarshalAs(UnmanagedType.LPWStr)] string lpWideCharStr, [In] int cchWideChar);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage3_GetCodePageInfo([In] uint uiCodePage, [In] ushort LangId, out MIMECPINFO pCodePageInfo);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage3_GetFamilyCodePage([In] uint uiCodePage, out uint puiFamilyCodePage);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage3_GetLcidFromRfc1766(out uint plocale, [In, MarshalAs(UnmanagedType.BStr)] string bstrRfc1766);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage3_GetNumberOfCodePageInfo(out uint pcCodePage);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage3_GetNumberOfScripts(out uint pnScripts);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage3_GetRfc1766FromLcid([In] uint locale, [MarshalAs(UnmanagedType.BStr)] out string pbstrRfc1766);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage3_GetRfc1766Info([In] uint locale, [In] ushort LangId, out RFC1766INFO pRfc1766Info);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage3_IsCodePageInstallable([In] uint uiCodePage);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage3_IsConvertible([In] uint dwSrcEncoding, [In] uint dwDstEncoding);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage3_SetMimeDBSource([In] MIMECONTF dwSource);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage3_ValidateCodePage([In] uint uiCodePage, [In, ComAliasName("MultiLanguage.wireHWND")] ref _RemotableHandle hwnd);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IMultiLanguage3_ValidateCodePageEx([In] uint uiCodePage, [In, ComAliasName("MultiLanguage.wireHWND")] ref _RemotableHandle hwnd, [In] uint dwfIODControl);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IsCodePageInstallable([In] uint uiCodePage);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void IsConvertible([In] uint dwSrcEncoding, [In] uint dwDstEncoding);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void MapFont([In, ComAliasName("MultiLanguage.wireHDC")] ref _RemotableHandle hDC, [In] uint dwCodePages, [In] ushort chSrc, [Out, ComAliasName("MultiLanguage.wireHFONT")] IntPtr pFont);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void MapFont([In, ComAliasName("MultiLanguage.wireHDC")] ref _RemotableHandle hDC, [In] uint dwCodePages, [In, ComAliasName("MultiLanguage.wireHFONT")] ref _RemotableHandle hSrcFont, [Out, ComAliasName("MultiLanguage.wireHFONT")] IntPtr phDestFont);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void ReleaseFont([In, ComAliasName("MultiLanguage.wireHFONT")] ref _RemotableHandle hFont);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void ResetFontMapping();
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void SetMimeDBSource([In] MIMECONTF dwSource);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void ValidateCodePage([In] uint uiCodePage, [In, ComAliasName("MultiLanguage.wireHWND")] ref _RemotableHandle hwnd);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public virtual extern void ValidateCodePageEx([In] uint uiCodePage, [In, ComAliasName("MultiLanguage.wireHWND")] ref _RemotableHandle hwnd, [In] uint dwfIODControl);
        }

        [ComImport, Guid("275C23E3-3747-11D0-9FEA-00AA003F8646"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IEnumCodePage
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void Clone([MarshalAs(UnmanagedType.Interface)] out IEnumCodePage ppEnum);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            [PreserveSig]
            HRESULT Next([In] uint celt, out MIMECPINFO rgelt, out uint pceltFetched);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void Reset();
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void Skip([In] uint celt);
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("3DC39D1D-C030-11D0-B81B-00C04FC9B31F")]
        public interface IEnumRfc1766
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void Clone([MarshalAs(UnmanagedType.Interface)] out IEnumRfc1766 ppEnum);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void Next([In] uint celt, out RFC1766INFO rgelt, out uint pceltFetched);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void Reset();
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void Skip([In] uint celt);
        }

        [ComImport, Guid("AE5F1430-388B-11D2-8380-00C04F8F5DA1"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IEnumScript
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void Clone([MarshalAs(UnmanagedType.Interface)] out IEnumScript ppEnum);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void Next([In] uint celt, out SCRIPTINFO rgelt, out uint pceltFetched);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void Reset();
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void Skip([In] uint celt);
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("359F3443-BD4A-11D0-B188-00AA0038C969")]
        public interface IMLangCodePages
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetCharCodePages([In] ushort chSrc, out uint pdwCodePages);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetStrCodePages([In] ref ushort pszSrc, [In] int cchSrc, [In] uint dwPriorityCodePages, out uint pdwCodePages, out int pcchCodePages);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void CodePageToCodePages([In] uint uCodePage, out uint pdwCodePages);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void CodePagesToCodePage([In] uint dwCodePages, [In] uint uDefaultCodePage, out uint puCodePage);
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("D66D6F98-CDAA-11D0-B822-00C04FC9B31F")]
        public interface IMLangConvertCharset
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void Initialize([In] uint uiSrcCodePage, [In] uint uiDstCodePage, [In] uint dwProperty);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetSourceCodePage(out uint puiSrcCodePage);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetDestinationCodePage(out uint puiDstCodePage);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetProperty(out uint pdwProperty);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void DoConversion([In] ref byte pSrcStr, [In, Out] ref uint pcSrcSize, out byte pDstStr, [In, Out] ref uint pcDstSize);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void DoConversionToUnicode([In] ref sbyte pSrcStr, [In, Out] ref uint pcSrcSize, out ushort pDstStr, [In, Out] ref uint pcDstSize);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void DoConversionFromUnicode([In] ref ushort pSrcStr, [In, Out] ref uint pcSrcSize, out sbyte pDstStr, [In, Out] ref uint pcDstSize);
        }

        [ComImport, Guid("359F3441-BD4A-11D0-B188-00AA0038C969"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComConversionLoss]
        public interface IMLangFontLink : IMLangCodePages
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetFontCodePages([In, ComAliasName("MultiLanguage.wireHDC")] ref _RemotableHandle hDC, [In, ComAliasName("MultiLanguage.wireHFONT")] ref _RemotableHandle hFont, out uint pdwCodePages);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void MapFont([In, ComAliasName("MultiLanguage.wireHDC")] ref _RemotableHandle hDC, [In] uint dwCodePages, [In, ComAliasName("MultiLanguage.wireHFONT")] ref _RemotableHandle hSrcFont, [Out, ComAliasName("MultiLanguage.wireHFONT")] IntPtr phDestFont);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void ReleaseFont([In, ComAliasName("MultiLanguage.wireHFONT")] ref _RemotableHandle hFont);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void ResetFontMapping();
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("DCCFC162-2B38-11D2-B7EC-00C04F8F5D9A"), ComConversionLoss]
        public interface IMLangFontLink2 : IMLangCodePages
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetFontCodePages([In, ComAliasName("MultiLanguage.wireHDC")] ref _RemotableHandle hDC, [In, ComAliasName("MultiLanguage.wireHFONT")] ref _RemotableHandle hFont, out uint pdwCodePages);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void ReleaseFont([In, ComAliasName("MultiLanguage.wireHFONT")] ref _RemotableHandle hFont);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void ResetFontMapping();
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void MapFont([In, ComAliasName("MultiLanguage.wireHDC")] ref _RemotableHandle hDC, [In] uint dwCodePages, [In] ushort chSrc, [Out, ComAliasName("MultiLanguage.wireHFONT")] IntPtr pFont);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetFontUnicodeRanges([In, ComAliasName("MultiLanguage.wireHDC")] ref _RemotableHandle hDC, [In, Out] ref uint puiRanges, out UNICODERANGE pUranges);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetScriptFontInfo([In] byte sid, [In] uint dwFlags, [In, Out] ref uint puiFonts, out SCRIPFONTINFO pScriptFont);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void CodePageToScriptID([In] uint uiCodePage, out byte pSid);
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("F5BE2EE1-BFD7-11D0-B188-00AA0038C969")]
        public interface IMLangLineBreakConsole
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void BreakLineML([In, MarshalAs(UnmanagedType.Interface)] CMLangString pSrcMLStr, [In] int lSrcPos, [In] int lSrcLen, [In] int cMinColumns, [In] int cMaxColumns, out int plLineLen, out int plSkipLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void BreakLineW([In] uint locale, [In] ref ushort pszSrc, [In] int cchSrc, [In] int cMaxColumns, out int pcchLine, out int pcchSkip);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void BreakLineA([In] uint locale, [In] uint uCodePage, [In] ref sbyte pszSrc, [In] int cchSrc, [In] int cMaxColumns, out int pcchLine, out int pcchSkip);
        }

        [ComImport, Guid("C04D65CE-B70D-11D0-B188-00AA0038C969"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IMLangString
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void Sync([In] int fNoAccess);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            int GetLength();
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void SetMLStr([In] int lDestPos, [In] int lDestLen, [In, MarshalAs(UnmanagedType.IUnknown)] object pSrcMLStr, [In] int lSrcPos, [In] int lSrcLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetMLStr([In] int lSrcPos, [In] int lSrcLen, [In, MarshalAs(UnmanagedType.IUnknown)] object pUnkOuter, [In] uint dwClsContext, [In] ref Guid piid, [MarshalAs(UnmanagedType.IUnknown)] out object ppDestMLStr, out int plDestPos, out int plDestLen);
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("C04D65D2-B70D-11D0-B188-00AA0038C969")]
        public interface IMLangStringAStr : IMLangString
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void SetAStr([In] int lDestPos, [In] int lDestLen, [In] uint uCodePage, [In, MarshalAs(UnmanagedType.LPStr)] string pszSrc, [In] int cchSrc, out int pcchActual, out int plActualLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void SetStrBufA([In] int lDestPos, [In] int lDestLen, [In] uint uCodePage, [In, MarshalAs(UnmanagedType.Interface)] IMLangStringBufA pSrcBuf, out int pcchActual, out int plActualLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetAStr([In] int lSrcPos, [In] int lSrcLen, [In] uint uCodePageIn, out uint puCodePageOut, [Out, MarshalAs(UnmanagedType.LPStr)] string pszDest, [In] int cchDest, out int pcchActual, out int plActualLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetStrBufA([In] int lSrcPos, [In] int lSrcMaxLen, out uint puDestCodePage, [MarshalAs(UnmanagedType.Interface)] out IMLangStringBufA ppDestBuf, out int plDestLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void LockAStr([In] int lSrcPos, [In] int lSrcLen, [In] int lFlags, [In] uint uCodePageIn, [In] int cchRequest, out uint puCodePageOut, [MarshalAs(UnmanagedType.LPStr)] out string ppszDest, out int pcchDest, out int plDestLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void UnlockAStr([In, MarshalAs(UnmanagedType.LPStr)] string pszSrc, [In] int cchSrc, out int pcchActual, out int plActualLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void SetLocale([In] int lDestPos, [In] int lDestLen, [In] uint locale);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetLocale([In] int lSrcPos, [In] int lSrcMaxLen, out uint plocale, out int plLocalePos, out int plLocaleLen);
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComConversionLoss, Guid("D24ACD23-BA72-11D0-B188-00AA0038C969")]
        public interface IMLangStringBufA
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetStatus(out int plFlags, out int pcchBuf);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void LockBuf([In] int cchOffset, [In] int cchMaxLock, [Out] IntPtr ppszBuf, out int pcchBuf);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void UnlockBuf([In] ref sbyte pszBuf, [In] int cchOffset, [In] int cchWrite);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void Insert([In] int cchOffset, [In] int cchMaxInsert, out int pcchActual);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void Delete([In] int cchOffset, [In] int cchDelete);
        }

        [ComImport, ComConversionLoss, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("D24ACD21-BA72-11D0-B188-00AA0038C969")]
        public interface IMLangStringBufW
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetStatus(out int plFlags, out int pcchBuf);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void LockBuf([In] int cchOffset, [In] int cchMaxLock, [Out] IntPtr ppszBuf, out int pcchBuf);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void UnlockBuf([In] ref ushort pszBuf, [In] int cchOffset, [In] int cchWrite);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void Insert([In] int cchOffset, [In] int cchMaxInsert, out int pcchActual);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void Delete([In] int cchOffset, [In] int cchDelete);
        }

        [ComImport, Guid("C04D65D0-B70D-11D0-B188-00AA0038C969"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IMLangStringWStr : IMLangString
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void SetWStr([In] int lDestPos, [In] int lDestLen, [In, MarshalAs(UnmanagedType.LPWStr)] string pszSrc, [In] int cchSrc, out int pcchActual, out int plActualLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void SetStrBufW([In] int lDestPos, [In] int lDestLen, [In, MarshalAs(UnmanagedType.Interface)] IMLangStringBufW pSrcBuf, out int pcchActual, out int plActualLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetWStr([In] int lSrcPos, [In] int lSrcLen, [Out, MarshalAs(UnmanagedType.LPWStr)] string pszDest, [In] int cchDest, out int pcchActual, out int plActualLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetStrBufW([In] int lSrcPos, [In] int lSrcMaxLen, [MarshalAs(UnmanagedType.Interface)] out IMLangStringBufW ppDestBuf, out int plDestLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void LockWStr([In] int lSrcPos, [In] int lSrcLen, [In] int lFlags, [In] int cchRequest, [MarshalAs(UnmanagedType.LPWStr)] out string ppszDest, out int pcchDest, out int plDestLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void UnlockWStr([In, MarshalAs(UnmanagedType.LPWStr)] string pszSrc, [In] int cchSrc, out int pcchActual, out int plActualLen);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void SetLocale([In] int lDestPos, [In] int lDestLen, [In] uint locale);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetLocale([In] int lSrcPos, [In] int lSrcMaxLen, out uint plocale, out int plLocalePos, out int plLocaleLen);
        }

        [ComImport, Guid("275C23E1-3747-11D0-9FEA-00AA003F8646"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IMultiLanguage
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetNumberOfCodePageInfo(out uint pcCodePage);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetCodePageInfo([In] uint uiCodePage, out MIMECPINFO pCodePageInfo);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetFamilyCodePage([In] uint uiCodePage, out uint puiFamilyCodePage);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            [return: MarshalAs(UnmanagedType.Interface)]
            IEnumCodePage EnumCodePages([In] NativeMethods.MIMECONTF grfFlags);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetCharsetInfo([In, MarshalAs(UnmanagedType.BStr)] string Charset, out MIMECSETINFO pCharsetInfo);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void IsConvertible([In] uint dwSrcEncoding, [In] uint dwDstEncoding);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void ConvertString([In, Out] ref uint pdwMode, [In] uint dwSrcEncoding, [In] uint dwDstEncoding, [In] ref byte pSrcStr, [In, Out] ref uint pcSrcSize, out byte pDstStr, [In, Out] ref uint pcDstSize);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void ConvertStringToUnicode([In, Out] ref uint pdwMode, [In] uint dwEncoding, [In] ref sbyte pSrcStr, [In, Out] ref uint pcSrcSize, out ushort pDstStr, [In, Out] ref uint pcDstSize);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void ConvertStringFromUnicode([In, Out] ref uint pdwMode, [In] uint dwEncoding, [In] ref ushort pSrcStr, [In, Out] ref uint pcSrcSize, out sbyte pDstStr, [In, Out] ref uint pcDstSize);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void ConvertStringReset();
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetRfc1766FromLcid([In] uint locale, [MarshalAs(UnmanagedType.BStr)] out string pbstrRfc1766);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetLcidFromRfc1766(out uint plocale, [In, MarshalAs(UnmanagedType.BStr)] string bstrRfc1766);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void EnumRfc1766([MarshalAs(UnmanagedType.Interface)] out IEnumRfc1766 ppEnumRfc1766);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetRfc1766Info([In] uint locale, out RFC1766INFO pRfc1766Info);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void CreateConvertCharset([In] uint uiSrcCodePage, [In] uint uiDstCodePage, [In] uint dwProperty, [MarshalAs(UnmanagedType.Interface)] out CMLangConvertCharset ppMLangConvertCharset);
        }

        [ComImport, Guid("DCCFC164-2B38-11D2-B7EC-00C04F8F5D9A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IMultiLanguage2
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetNumberOfCodePageInfo(out uint pcCodePage);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetCodePageInfo([In] uint uiCodePage, [In] ushort LangId, out MIMECPINFO pCodePageInfo);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetFamilyCodePage([In] uint uiCodePage, out uint puiFamilyCodePage);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void EnumCodePages([In] uint grfFlags, [In] ushort LangId, [MarshalAs(UnmanagedType.Interface)] out IEnumCodePage ppEnumCodePage);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetCharsetInfo([In, MarshalAs(UnmanagedType.BStr)] string Charset, out MIMECSETINFO pCharsetInfo);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void IsConvertible([In] uint dwSrcEncoding, [In] uint dwDstEncoding);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void ConvertString([In, Out] ref uint pdwMode, [In] uint dwSrcEncoding, [In] uint dwDstEncoding, [In] ref byte pSrcStr, [In, Out] ref uint pcSrcSize, out byte pDstStr, [In, Out] ref uint pcDstSize);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void ConvertStringToUnicode([In, Out] ref uint pdwMode, [In] uint dwEncoding, [In] ref sbyte pSrcStr, [In, Out] ref uint pcSrcSize, out ushort pDstStr, [In, Out] ref uint pcDstSize);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void ConvertStringFromUnicode([In, Out] ref uint pdwMode, [In] uint dwEncoding, [In] ref ushort pSrcStr, [In, Out] ref uint pcSrcSize, out sbyte pDstStr, [In, Out] ref uint pcDstSize);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void ConvertStringReset();
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetRfc1766FromLcid([In] uint locale, [MarshalAs(UnmanagedType.BStr)] out string pbstrRfc1766);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetLcidFromRfc1766(out uint plocale, [In, MarshalAs(UnmanagedType.BStr)] string bstrRfc1766);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void EnumRfc1766([In] ushort LangId, [MarshalAs(UnmanagedType.Interface)] out IEnumRfc1766 ppEnumRfc1766);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetRfc1766Info([In] uint locale, [In] ushort LangId, out RFC1766INFO pRfc1766Info);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void CreateConvertCharset([In] uint uiSrcCodePage, [In] uint uiDstCodePage, [In] uint dwProperty, [MarshalAs(UnmanagedType.Interface)] out CMLangConvertCharset ppMLangConvertCharset);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void ConvertStringInIStream([In, Out] ref uint pdwMode, [In] uint dwFlag, [In] ref ushort lpFallBack, [In] uint dwSrcEncoding, [In] uint dwDstEncoding, [In, MarshalAs(UnmanagedType.Interface)] IStream pstmIn, [In, MarshalAs(UnmanagedType.Interface)] IStream pstmOut);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void ConvertStringToUnicodeEx([In, Out] ref uint pdwMode, [In] uint dwEncoding, [In] ref sbyte pSrcStr, [In, Out] ref uint pcSrcSize, out ushort pDstStr, [In, Out] ref uint pcDstSize, [In] uint dwFlag, [In] ref ushort lpFallBack);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void ConvertStringFromUnicodeEx([In, Out] ref uint pdwMode, [In] uint dwEncoding, [In] ref ushort pSrcStr, [In, Out] ref uint pcSrcSize, out sbyte pDstStr, [In, Out] ref uint pcDstSize, [In] uint dwFlag, [In] ref ushort lpFallBack);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void DetectCodepageInIStream([In] uint dwFlag, [In] uint dwPrefWinCodePage, [In, MarshalAs(UnmanagedType.Interface)] IStream pstmIn, out DetectEncodingInfo lpEncoding, [In, Out] ref int pnScores);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            [PreserveSig]
            HRESULT DetectInputCodepage([In] MLDETECTCP dwFlag, [In] uint dwPrefWinCodePage, [In] ref byte pSrcStr, [In, Out] ref int pcSrcSize, [In, Out] ref DetectEncodingInfo lpEncoding, [In, Out] ref int pnScores);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void ValidateCodePage([In] uint uiCodePage, [In, ComAliasName("MultiLanguage.wireHWND")] ref _RemotableHandle hwnd);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetCodePageDescription([In] uint uiCodePage, [In] uint lcid, [Out, MarshalAs(UnmanagedType.LPWStr)] string lpWideCharStr, [In] int cchWideChar);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void IsCodePageInstallable([In] uint uiCodePage);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void SetMimeDBSource([In] MIMECONTF dwSource);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetNumberOfScripts(out uint pnScripts);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void EnumScripts([In] uint dwFlags, [In] ushort LangId, [MarshalAs(UnmanagedType.Interface)] out IEnumScript ppEnumScript);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void ValidateCodePageEx([In] uint uiCodePage, [In, ComAliasName("MultiLanguage.wireHWND")] ref _RemotableHandle hwnd, [In] uint dwfIODControl);
        }

        [ComImport, Guid("4E5868AB-B157-4623-9ACC-6A1D9CAEBE04"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IMultiLanguage3 : IMultiLanguage2
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void DetectOutboundCodePage([In] uint dwFlags, [In, MarshalAs(UnmanagedType.LPWStr)] string lpWideCharStr, [In] uint cchWideChar, [In] ref uint puiPreferredCodePages, [In] uint nPreferredCodePages, out uint puiDetectedCodePages, [In, Out] ref uint pnDetectedCodePages, [In, MarshalAs(UnmanagedType.LPWStr)] string lpSpecialChar);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void DetectOutboundCodePageInIStream([In] uint dwFlags, [In, MarshalAs(UnmanagedType.Interface)] IStream pStrIn, [In] ref uint puiPreferredCodePages, [In] uint nPreferredCodePages, out uint puiDetectedCodePages, [In, Out] ref uint pnDetectedCodePages, [In, MarshalAs(UnmanagedType.LPWStr)] string lpSpecialChar);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct DetectEncodingInfo
        {
            public uint nLangID;
            public uint nCodePage;
            public int nDocPercent;
            public int nConfidence;
        }

        public enum MIMECONTF
        {
            MIMECONTF_BROWSER = 2,
            MIMECONTF_EXPORT = 0x400,
            MIMECONTF_IMPORT = 8,
            MIMECONTF_MAILNEWS = 1,
            MIMECONTF_MIME_IE4 = 0x10000000,
            MIMECONTF_MIME_LATEST = 0x20000000,
            MIMECONTF_MIME_REGISTRY = 0x40000000,
            MIMECONTF_MINIMAL = 4,
            MIMECONTF_PRIVCONVERTER = 0x10000,
            MIMECONTF_SAVABLE_BROWSER = 0x200,
            MIMECONTF_SAVABLE_MAILNEWS = 0x100,
            MIMECONTF_VALID = 0x20000,
            MIMECONTF_VALID_NLS = 0x40000
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
        public struct MIMECPINFO
        {
            public uint dwFlags;
            public uint uiCodePage;
            public uint uiFamilyCodePage;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x40)]
            public string wszDescription;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
            public string wszWebCharset;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
            public string wszHeaderCharset;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
            public string wszBodyCharset;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string wszFixedWidthFont;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string wszProportionalFont;
            public byte bGDICharset;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct MIMECSETINFO
        {
            public uint uiCodePage;
            public uint uiInternetEncoding;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 50)]
            public ushort[] wszCharset;
        }

        public enum MLSTR_FLAGS
        {
            MLSTR_READ = 1,
            MLSTR_WRITE = 2
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct RFC1766INFO
        {
            public uint lcid;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public ushort[] wszRfc1766;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
            public ushort[] wszLocaleName;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct SCRIPFONTINFO
        {
            public long scripts;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
            public ushort[] wszFont;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct SCRIPTINFO
        {
            public byte ScriptId;
            public uint uiCodePage;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x30)]
            public ushort[] wszDescription;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
            public ushort[] wszFixedWidthFont;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
            public ushort[] wszProportionalFont;
        }

        public enum MLDETECTCP
        {
            MLDETECTCP_NONE = 0,
            MLDETECTCP_7BIT = 1,
            MLDETECTCP_8BIT = 2,
            MLDETECTCP_DBCS = 4,
            MLDETECTCP_HTML = 8,
            MLDETECTCP_NUMBER = 16
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct STATSTG
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pwcsName;
            public uint type;
            public ulong cbSize;
            public System.Runtime.InteropServices.ComTypes.FILETIME mtime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ctime;
            public System.Runtime.InteropServices.ComTypes.FILETIME atime;
            public uint grfMode;
            public uint grfLocksSupported;
            public Guid clsid;
            public uint grfStateBits;
            public uint reserved;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        public struct UNICODERANGE
        {
            public ushort wcFrom;
            public ushort wcTo;
        }
    }
}
