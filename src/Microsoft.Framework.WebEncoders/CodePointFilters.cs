// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.Framework.WebEncoders
{
    /// <summary>
    /// Contains predefined Unicode code point filters.
    /// </summary>
    public static class CodePointFilters
    {
        /// <summary>
        /// A filter which allows no characters.
        /// </summary>
        public static ICodePointFilter None
        {
            get
            {
                return LazyInitializer.EnsureInitialized(ref _none);
            }
        }
        private static EmptyCodePointFilter _none;

        /// <summary>
        /// A filter which allows all Unicode Basic Multilingual Plane characters.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+0000 .. U+FFFF.
        /// </remarks>
        public static ICodePointFilter All
        {
            get
            {
                return GetFilter(ref _all, first: '\u0000', last: '\uFFFF');
            }
        }
        private static DefinedCharacterCodePointFilter _all;

        /// <summary>
        /// A filter which allows characters in the 'Basic Latin' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+0000 .. U+007F.
        /// See http://www.unicode.org/charts/PDF/U0000.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter BasicLatin
        {
            get
            {
                return GetFilter(ref _basicLatin, first: '\u0000', last: '\u007F');
            }
        }
        private static DefinedCharacterCodePointFilter _basicLatin;

        /// <summary>
        /// A filter which allows characters in the 'Latin-1 Supplement' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+0080 .. U+00FF.
        /// See http://www.unicode.org/charts/PDF/U0080.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Latin1Supplement
        {
            get
            {
                return GetFilter(ref _latin1Supplement, first: '\u0080', last: '\u00FF');
            }
        }
        private static DefinedCharacterCodePointFilter _latin1Supplement;

        /// <summary>
        /// A filter which allows characters in the 'Latin Extended-A' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+0100 .. U+017F.
        /// See http://www.unicode.org/charts/PDF/U0100.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter LatinExtendedA
        {
            get
            {
                return GetFilter(ref _latinExtendedA, first: '\u0100', last: '\u017F');
            }
        }
        private static DefinedCharacterCodePointFilter _latinExtendedA;

        /// <summary>
        /// A filter which allows characters in the 'Latin Extended-B' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+0180 .. U+024F.
        /// See http://www.unicode.org/charts/PDF/U0180.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter LatinExtendedB
        {
            get
            {
                return GetFilter(ref _latinExtendedB, first: '\u0180', last: '\u024F');
            }
        }
        private static DefinedCharacterCodePointFilter _latinExtendedB;

        /// <summary>
        /// A filter which allows characters in the 'IPA Extensions' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+0250 .. U+02AF.
        /// See http://www.unicode.org/charts/PDF/U0250.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter IPAExtensions
        {
            get
            {
                return GetFilter(ref _ipaExtensions, first: '\u0250', last: '\u02AF');
            }
        }
        private static DefinedCharacterCodePointFilter _ipaExtensions;

        /// <summary>
        /// A filter which allows characters in the 'Spacing Modifier Letters' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+02B0 .. U+02FF.
        /// See http://www.unicode.org/charts/PDF/U02B0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter SpacingModifierLetters
        {
            get
            {
                return GetFilter(ref _spacingModifierLetters, first: '\u02B0', last: '\u02FF');
            }
        }
        private static DefinedCharacterCodePointFilter _spacingModifierLetters;

        /// <summary>
        /// A filter which allows characters in the 'Combining Diacritical Marks' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+0300 .. U+036F.
        /// See http://www.unicode.org/charts/PDF/U0300.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter CombiningDiacriticalMarks
        {
            get
            {
                return GetFilter(ref _combiningDiacriticalMarks, first: '\u0300', last: '\u036F');
            }
        }
        private static DefinedCharacterCodePointFilter _combiningDiacriticalMarks;

        /// <summary>
        /// A filter which allows characters in the 'Greek and Coptic' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+0370 .. U+03FF.
        /// See http://www.unicode.org/charts/PDF/U0370.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter GreekandCoptic
        {
            get
            {
                return GetFilter(ref _greekandCoptic, first: '\u0370', last: '\u03FF');
            }
        }
        private static DefinedCharacterCodePointFilter _greekandCoptic;

        /// <summary>
        /// A filter which allows characters in the 'Cyrillic' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+0400 .. U+04FF.
        /// See http://www.unicode.org/charts/PDF/U0400.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Cyrillic
        {
            get
            {
                return GetFilter(ref _cyrillic, first: '\u0400', last: '\u04FF');
            }
        }
        private static DefinedCharacterCodePointFilter _cyrillic;

        /// <summary>
        /// A filter which allows characters in the 'Cyrillic Supplement' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+0500 .. U+052F.
        /// See http://www.unicode.org/charts/PDF/U0500.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter CyrillicSupplement
        {
            get
            {
                return GetFilter(ref _cyrillicSupplement, first: '\u0500', last: '\u052F');
            }
        }
        private static DefinedCharacterCodePointFilter _cyrillicSupplement;

        /// <summary>
        /// A filter which allows characters in the 'Armenian' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+0530 .. U+058F.
        /// See http://www.unicode.org/charts/PDF/U0530.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Armenian
        {
            get
            {
                return GetFilter(ref _armenian, first: '\u0530', last: '\u058F');
            }
        }
        private static DefinedCharacterCodePointFilter _armenian;

        /// <summary>
        /// A filter which allows characters in the 'Hebrew' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+0590 .. U+05FF.
        /// See http://www.unicode.org/charts/PDF/U0590.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Hebrew
        {
            get
            {
                return GetFilter(ref _hebrew, first: '\u0590', last: '\u05FF');
            }
        }
        private static DefinedCharacterCodePointFilter _hebrew;

        /// <summary>
        /// A filter which allows characters in the 'Arabic' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+0600 .. U+06FF.
        /// See http://www.unicode.org/charts/PDF/U0600.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Arabic
        {
            get
            {
                return GetFilter(ref _arabic, first: '\u0600', last: '\u06FF');
            }
        }
        private static DefinedCharacterCodePointFilter _arabic;

        /// <summary>
        /// A filter which allows characters in the 'Syriac' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+0700 .. U+074F.
        /// See http://www.unicode.org/charts/PDF/U0700.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Syriac
        {
            get
            {
                return GetFilter(ref _syriac, first: '\u0700', last: '\u074F');
            }
        }
        private static DefinedCharacterCodePointFilter _syriac;

        /// <summary>
        /// A filter which allows characters in the 'Arabic Supplement' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+0750 .. U+077F.
        /// See http://www.unicode.org/charts/PDF/U0750.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter ArabicSupplement
        {
            get
            {
                return GetFilter(ref _arabicSupplement, first: '\u0750', last: '\u077F');
            }
        }
        private static DefinedCharacterCodePointFilter _arabicSupplement;

        /// <summary>
        /// A filter which allows characters in the 'Thaana' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+0780 .. U+07BF.
        /// See http://www.unicode.org/charts/PDF/U0780.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Thaana
        {
            get
            {
                return GetFilter(ref _thaana, first: '\u0780', last: '\u07BF');
            }
        }
        private static DefinedCharacterCodePointFilter _thaana;

        /// <summary>
        /// A filter which allows characters in the 'NKo' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+07C0 .. U+07FF.
        /// See http://www.unicode.org/charts/PDF/U07C0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter NKo
        {
            get
            {
                return GetFilter(ref _nKo, first: '\u07C0', last: '\u07FF');
            }
        }
        private static DefinedCharacterCodePointFilter _nKo;

        /// <summary>
        /// A filter which allows characters in the 'Samaritan' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+0800 .. U+083F.
        /// See http://www.unicode.org/charts/PDF/U0800.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Samaritan
        {
            get
            {
                return GetFilter(ref _samaritan, first: '\u0800', last: '\u083F');
            }
        }
        private static DefinedCharacterCodePointFilter _samaritan;

        /// <summary>
        /// A filter which allows characters in the 'Mandaic' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+0840 .. U+085F.
        /// See http://www.unicode.org/charts/PDF/U0840.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Mandaic
        {
            get
            {
                return GetFilter(ref _mandaic, first: '\u0840', last: '\u085F');
            }
        }
        private static DefinedCharacterCodePointFilter _mandaic;

        /// <summary>
        /// A filter which allows characters in the 'Arabic Extended-A' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+08A0 .. U+08FF.
        /// See http://www.unicode.org/charts/PDF/U08A0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter ArabicExtendedA
        {
            get
            {
                return GetFilter(ref _arabicExtendedA, first: '\u08A0', last: '\u08FF');
            }
        }
        private static DefinedCharacterCodePointFilter _arabicExtendedA;

        /// <summary>
        /// A filter which allows characters in the 'Devanagari' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+0900 .. U+097F.
        /// See http://www.unicode.org/charts/PDF/U0900.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Devanagari
        {
            get
            {
                return GetFilter(ref _devanagari, first: '\u0900', last: '\u097F');
            }
        }
        private static DefinedCharacterCodePointFilter _devanagari;

        /// <summary>
        /// A filter which allows characters in the 'Bengali' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+0980 .. U+09FF.
        /// See http://www.unicode.org/charts/PDF/U0980.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Bengali
        {
            get
            {
                return GetFilter(ref _bengali, first: '\u0980', last: '\u09FF');
            }
        }
        private static DefinedCharacterCodePointFilter _bengali;

        /// <summary>
        /// A filter which allows characters in the 'Gurmukhi' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+0A00 .. U+0A7F.
        /// See http://www.unicode.org/charts/PDF/U0A00.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Gurmukhi
        {
            get
            {
                return GetFilter(ref _gurmukhi, first: '\u0A00', last: '\u0A7F');
            }
        }
        private static DefinedCharacterCodePointFilter _gurmukhi;

        /// <summary>
        /// A filter which allows characters in the 'Gujarati' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+0A80 .. U+0AFF.
        /// See http://www.unicode.org/charts/PDF/U0A80.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Gujarati
        {
            get
            {
                return GetFilter(ref _gujarati, first: '\u0A80', last: '\u0AFF');
            }
        }
        private static DefinedCharacterCodePointFilter _gujarati;

        /// <summary>
        /// A filter which allows characters in the 'Oriya' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+0B00 .. U+0B7F.
        /// See http://www.unicode.org/charts/PDF/U0B00.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Oriya
        {
            get
            {
                return GetFilter(ref _oriya, first: '\u0B00', last: '\u0B7F');
            }
        }
        private static DefinedCharacterCodePointFilter _oriya;

        /// <summary>
        /// A filter which allows characters in the 'Tamil' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+0B80 .. U+0BFF.
        /// See http://www.unicode.org/charts/PDF/U0B80.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Tamil
        {
            get
            {
                return GetFilter(ref _tamil, first: '\u0B80', last: '\u0BFF');
            }
        }
        private static DefinedCharacterCodePointFilter _tamil;

        /// <summary>
        /// A filter which allows characters in the 'Telugu' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+0C00 .. U+0C7F.
        /// See http://www.unicode.org/charts/PDF/U0C00.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Telugu
        {
            get
            {
                return GetFilter(ref _telugu, first: '\u0C00', last: '\u0C7F');
            }
        }
        private static DefinedCharacterCodePointFilter _telugu;

        /// <summary>
        /// A filter which allows characters in the 'Kannada' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+0C80 .. U+0CFF.
        /// See http://www.unicode.org/charts/PDF/U0C80.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Kannada
        {
            get
            {
                return GetFilter(ref _kannada, first: '\u0C80', last: '\u0CFF');
            }
        }
        private static DefinedCharacterCodePointFilter _kannada;

        /// <summary>
        /// A filter which allows characters in the 'Malayalam' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+0D00 .. U+0D7F.
        /// See http://www.unicode.org/charts/PDF/U0D00.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Malayalam
        {
            get
            {
                return GetFilter(ref _malayalam, first: '\u0D00', last: '\u0D7F');
            }
        }
        private static DefinedCharacterCodePointFilter _malayalam;

        /// <summary>
        /// A filter which allows characters in the 'Sinhala' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+0D80 .. U+0DFF.
        /// See http://www.unicode.org/charts/PDF/U0D80.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Sinhala
        {
            get
            {
                return GetFilter(ref _sinhala, first: '\u0D80', last: '\u0DFF');
            }
        }
        private static DefinedCharacterCodePointFilter _sinhala;

        /// <summary>
        /// A filter which allows characters in the 'Thai' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+0E00 .. U+0E7F.
        /// See http://www.unicode.org/charts/PDF/U0E00.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Thai
        {
            get
            {
                return GetFilter(ref _thai, first: '\u0E00', last: '\u0E7F');
            }
        }
        private static DefinedCharacterCodePointFilter _thai;

        /// <summary>
        /// A filter which allows characters in the 'Lao' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+0E80 .. U+0EFF.
        /// See http://www.unicode.org/charts/PDF/U0E80.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Lao
        {
            get
            {
                return GetFilter(ref _lao, first: '\u0E80', last: '\u0EFF');
            }
        }
        private static DefinedCharacterCodePointFilter _lao;

        /// <summary>
        /// A filter which allows characters in the 'Tibetan' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+0F00 .. U+0FFF.
        /// See http://www.unicode.org/charts/PDF/U0F00.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Tibetan
        {
            get
            {
                return GetFilter(ref _tibetan, first: '\u0F00', last: '\u0FFF');
            }
        }
        private static DefinedCharacterCodePointFilter _tibetan;

        /// <summary>
        /// A filter which allows characters in the 'Myanmar' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+1000 .. U+109F.
        /// See http://www.unicode.org/charts/PDF/U1000.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Myanmar
        {
            get
            {
                return GetFilter(ref _myanmar, first: '\u1000', last: '\u109F');
            }
        }
        private static DefinedCharacterCodePointFilter _myanmar;

        /// <summary>
        /// A filter which allows characters in the 'Georgian' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+10A0 .. U+10FF.
        /// See http://www.unicode.org/charts/PDF/U10A0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Georgian
        {
            get
            {
                return GetFilter(ref _georgian, first: '\u10A0', last: '\u10FF');
            }
        }
        private static DefinedCharacterCodePointFilter _georgian;

        /// <summary>
        /// A filter which allows characters in the 'Hangul Jamo' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+1100 .. U+11FF.
        /// See http://www.unicode.org/charts/PDF/U1100.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter HangulJamo
        {
            get
            {
                return GetFilter(ref _hangulJamo, first: '\u1100', last: '\u11FF');
            }
        }
        private static DefinedCharacterCodePointFilter _hangulJamo;

        /// <summary>
        /// A filter which allows characters in the 'Ethiopic' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+1200 .. U+137F.
        /// See http://www.unicode.org/charts/PDF/U1200.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Ethiopic
        {
            get
            {
                return GetFilter(ref _ethiopic, first: '\u1200', last: '\u137F');
            }
        }
        private static DefinedCharacterCodePointFilter _ethiopic;

        /// <summary>
        /// A filter which allows characters in the 'Ethiopic Supplement' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+1380 .. U+139F.
        /// See http://www.unicode.org/charts/PDF/U1380.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter EthiopicSupplement
        {
            get
            {
                return GetFilter(ref _ethiopicSupplement, first: '\u1380', last: '\u139F');
            }
        }
        private static DefinedCharacterCodePointFilter _ethiopicSupplement;

        /// <summary>
        /// A filter which allows characters in the 'Cherokee' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+13A0 .. U+13FF.
        /// See http://www.unicode.org/charts/PDF/U13A0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Cherokee
        {
            get
            {
                return GetFilter(ref _cherokee, first: '\u13A0', last: '\u13FF');
            }
        }
        private static DefinedCharacterCodePointFilter _cherokee;

        /// <summary>
        /// A filter which allows characters in the 'Unified Canadian Aboriginal Syllabics' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+1400 .. U+167F.
        /// See http://www.unicode.org/charts/PDF/U1400.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter UnifiedCanadianAboriginalSyllabics
        {
            get
            {
                return GetFilter(ref _unifiedCanadianAboriginalSyllabics, first: '\u1400', last: '\u167F');
            }
        }
        private static DefinedCharacterCodePointFilter _unifiedCanadianAboriginalSyllabics;

        /// <summary>
        /// A filter which allows characters in the 'Ogham' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+1680 .. U+169F.
        /// See http://www.unicode.org/charts/PDF/U1680.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Ogham
        {
            get
            {
                return GetFilter(ref _ogham, first: '\u1680', last: '\u169F');
            }
        }
        private static DefinedCharacterCodePointFilter _ogham;

        /// <summary>
        /// A filter which allows characters in the 'Runic' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+16A0 .. U+16FF.
        /// See http://www.unicode.org/charts/PDF/U16A0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Runic
        {
            get
            {
                return GetFilter(ref _runic, first: '\u16A0', last: '\u16FF');
            }
        }
        private static DefinedCharacterCodePointFilter _runic;

        /// <summary>
        /// A filter which allows characters in the 'Tagalog' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+1700 .. U+171F.
        /// See http://www.unicode.org/charts/PDF/U1700.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Tagalog
        {
            get
            {
                return GetFilter(ref _tagalog, first: '\u1700', last: '\u171F');
            }
        }
        private static DefinedCharacterCodePointFilter _tagalog;

        /// <summary>
        /// A filter which allows characters in the 'Hanunoo' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+1720 .. U+173F.
        /// See http://www.unicode.org/charts/PDF/U1720.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Hanunoo
        {
            get
            {
                return GetFilter(ref _hanunoo, first: '\u1720', last: '\u173F');
            }
        }
        private static DefinedCharacterCodePointFilter _hanunoo;

        /// <summary>
        /// A filter which allows characters in the 'Buhid' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+1740 .. U+175F.
        /// See http://www.unicode.org/charts/PDF/U1740.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Buhid
        {
            get
            {
                return GetFilter(ref _buhid, first: '\u1740', last: '\u175F');
            }
        }
        private static DefinedCharacterCodePointFilter _buhid;

        /// <summary>
        /// A filter which allows characters in the 'Tagbanwa' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+1760 .. U+177F.
        /// See http://www.unicode.org/charts/PDF/U1760.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Tagbanwa
        {
            get
            {
                return GetFilter(ref _tagbanwa, first: '\u1760', last: '\u177F');
            }
        }
        private static DefinedCharacterCodePointFilter _tagbanwa;

        /// <summary>
        /// A filter which allows characters in the 'Khmer' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+1780 .. U+17FF.
        /// See http://www.unicode.org/charts/PDF/U1780.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Khmer
        {
            get
            {
                return GetFilter(ref _khmer, first: '\u1780', last: '\u17FF');
            }
        }
        private static DefinedCharacterCodePointFilter _khmer;

        /// <summary>
        /// A filter which allows characters in the 'Mongolian' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+1800 .. U+18AF.
        /// See http://www.unicode.org/charts/PDF/U1800.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Mongolian
        {
            get
            {
                return GetFilter(ref _mongolian, first: '\u1800', last: '\u18AF');
            }
        }
        private static DefinedCharacterCodePointFilter _mongolian;

        /// <summary>
        /// A filter which allows characters in the 'Unified Canadian Aboriginal Syllabics Extended' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+18B0 .. U+18FF.
        /// See http://www.unicode.org/charts/PDF/U18B0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter UnifiedCanadianAboriginalSyllabicsExtended
        {
            get
            {
                return GetFilter(ref _unifiedCanadianAboriginalSyllabicsExtended, first: '\u18B0', last: '\u18FF');
            }
        }
        private static DefinedCharacterCodePointFilter _unifiedCanadianAboriginalSyllabicsExtended;

        /// <summary>
        /// A filter which allows characters in the 'Limbu' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+1900 .. U+194F.
        /// See http://www.unicode.org/charts/PDF/U1900.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Limbu
        {
            get
            {
                return GetFilter(ref _limbu, first: '\u1900', last: '\u194F');
            }
        }
        private static DefinedCharacterCodePointFilter _limbu;

        /// <summary>
        /// A filter which allows characters in the 'Tai Le' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+1950 .. U+197F.
        /// See http://www.unicode.org/charts/PDF/U1950.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter TaiLe
        {
            get
            {
                return GetFilter(ref _taiLe, first: '\u1950', last: '\u197F');
            }
        }
        private static DefinedCharacterCodePointFilter _taiLe;

        /// <summary>
        /// A filter which allows characters in the 'New Tai Lue' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+1980 .. U+19DF.
        /// See http://www.unicode.org/charts/PDF/U1980.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter NewTaiLue
        {
            get
            {
                return GetFilter(ref _newTaiLue, first: '\u1980', last: '\u19DF');
            }
        }
        private static DefinedCharacterCodePointFilter _newTaiLue;

        /// <summary>
        /// A filter which allows characters in the 'Khmer Symbols' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+19E0 .. U+19FF.
        /// See http://www.unicode.org/charts/PDF/U19E0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter KhmerSymbols
        {
            get
            {
                return GetFilter(ref _khmerSymbols, first: '\u19E0', last: '\u19FF');
            }
        }
        private static DefinedCharacterCodePointFilter _khmerSymbols;

        /// <summary>
        /// A filter which allows characters in the 'Buginese' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+1A00 .. U+1A1F.
        /// See http://www.unicode.org/charts/PDF/U1A00.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Buginese
        {
            get
            {
                return GetFilter(ref _buginese, first: '\u1A00', last: '\u1A1F');
            }
        }
        private static DefinedCharacterCodePointFilter _buginese;

        /// <summary>
        /// A filter which allows characters in the 'Tai Tham' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+1A20 .. U+1AAF.
        /// See http://www.unicode.org/charts/PDF/U1A20.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter TaiTham
        {
            get
            {
                return GetFilter(ref _taiTham, first: '\u1A20', last: '\u1AAF');
            }
        }
        private static DefinedCharacterCodePointFilter _taiTham;

        /// <summary>
        /// A filter which allows characters in the 'Combining Diacritical Marks Extended' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+1AB0 .. U+1AFF.
        /// See http://www.unicode.org/charts/PDF/U1AB0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter CombiningDiacriticalMarksExtended
        {
            get
            {
                return GetFilter(ref _combiningDiacriticalMarksExtended, first: '\u1AB0', last: '\u1AFF');
            }
        }
        private static DefinedCharacterCodePointFilter _combiningDiacriticalMarksExtended;

        /// <summary>
        /// A filter which allows characters in the 'Balinese' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+1B00 .. U+1B7F.
        /// See http://www.unicode.org/charts/PDF/U1B00.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Balinese
        {
            get
            {
                return GetFilter(ref _balinese, first: '\u1B00', last: '\u1B7F');
            }
        }
        private static DefinedCharacterCodePointFilter _balinese;

        /// <summary>
        /// A filter which allows characters in the 'Sundanese' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+1B80 .. U+1BBF.
        /// See http://www.unicode.org/charts/PDF/U1B80.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Sundanese
        {
            get
            {
                return GetFilter(ref _sundanese, first: '\u1B80', last: '\u1BBF');
            }
        }
        private static DefinedCharacterCodePointFilter _sundanese;

        /// <summary>
        /// A filter which allows characters in the 'Batak' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+1BC0 .. U+1BFF.
        /// See http://www.unicode.org/charts/PDF/U1BC0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Batak
        {
            get
            {
                return GetFilter(ref _batak, first: '\u1BC0', last: '\u1BFF');
            }
        }
        private static DefinedCharacterCodePointFilter _batak;

        /// <summary>
        /// A filter which allows characters in the 'Lepcha' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+1C00 .. U+1C4F.
        /// See http://www.unicode.org/charts/PDF/U1C00.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Lepcha
        {
            get
            {
                return GetFilter(ref _lepcha, first: '\u1C00', last: '\u1C4F');
            }
        }
        private static DefinedCharacterCodePointFilter _lepcha;

        /// <summary>
        /// A filter which allows characters in the 'Ol Chiki' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+1C50 .. U+1C7F.
        /// See http://www.unicode.org/charts/PDF/U1C50.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter OlChiki
        {
            get
            {
                return GetFilter(ref _olChiki, first: '\u1C50', last: '\u1C7F');
            }
        }
        private static DefinedCharacterCodePointFilter _olChiki;

        /// <summary>
        /// A filter which allows characters in the 'Sundanese Supplement' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+1CC0 .. U+1CCF.
        /// See http://www.unicode.org/charts/PDF/U1CC0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter SundaneseSupplement
        {
            get
            {
                return GetFilter(ref _sundaneseSupplement, first: '\u1CC0', last: '\u1CCF');
            }
        }
        private static DefinedCharacterCodePointFilter _sundaneseSupplement;

        /// <summary>
        /// A filter which allows characters in the 'Vedic Extensions' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+1CD0 .. U+1CFF.
        /// See http://www.unicode.org/charts/PDF/U1CD0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter VedicExtensions
        {
            get
            {
                return GetFilter(ref _vedicExtensions, first: '\u1CD0', last: '\u1CFF');
            }
        }
        private static DefinedCharacterCodePointFilter _vedicExtensions;

        /// <summary>
        /// A filter which allows characters in the 'Phonetic Extensions' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+1D00 .. U+1D7F.
        /// See http://www.unicode.org/charts/PDF/U1D00.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter PhoneticExtensions
        {
            get
            {
                return GetFilter(ref _phoneticExtensions, first: '\u1D00', last: '\u1D7F');
            }
        }
        private static DefinedCharacterCodePointFilter _phoneticExtensions;

        /// <summary>
        /// A filter which allows characters in the 'Phonetic Extensions Supplement' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+1D80 .. U+1DBF.
        /// See http://www.unicode.org/charts/PDF/U1D80.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter PhoneticExtensionsSupplement
        {
            get
            {
                return GetFilter(ref _phoneticExtensionsSupplement, first: '\u1D80', last: '\u1DBF');
            }
        }
        private static DefinedCharacterCodePointFilter _phoneticExtensionsSupplement;

        /// <summary>
        /// A filter which allows characters in the 'Combining Diacritical Marks Supplement' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+1DC0 .. U+1DFF.
        /// See http://www.unicode.org/charts/PDF/U1DC0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter CombiningDiacriticalMarksSupplement
        {
            get
            {
                return GetFilter(ref _combiningDiacriticalMarksSupplement, first: '\u1DC0', last: '\u1DFF');
            }
        }
        private static DefinedCharacterCodePointFilter _combiningDiacriticalMarksSupplement;

        /// <summary>
        /// A filter which allows characters in the 'Latin Extended Additional' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+1E00 .. U+1EFF.
        /// See http://www.unicode.org/charts/PDF/U1E00.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter LatinExtendedAdditional
        {
            get
            {
                return GetFilter(ref _latinExtendedAdditional, first: '\u1E00', last: '\u1EFF');
            }
        }
        private static DefinedCharacterCodePointFilter _latinExtendedAdditional;

        /// <summary>
        /// A filter which allows characters in the 'Greek Extended' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+1F00 .. U+1FFF.
        /// See http://www.unicode.org/charts/PDF/U1F00.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter GreekExtended
        {
            get
            {
                return GetFilter(ref _greekExtended, first: '\u1F00', last: '\u1FFF');
            }
        }
        private static DefinedCharacterCodePointFilter _greekExtended;

        /// <summary>
        /// A filter which allows characters in the 'General Punctuation' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+2000 .. U+206F.
        /// See http://www.unicode.org/charts/PDF/U2000.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter GeneralPunctuation
        {
            get
            {
                return GetFilter(ref _generalPunctuation, first: '\u2000', last: '\u206F');
            }
        }
        private static DefinedCharacterCodePointFilter _generalPunctuation;

        /// <summary>
        /// A filter which allows characters in the 'Superscripts and Subscripts' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+2070 .. U+209F.
        /// See http://www.unicode.org/charts/PDF/U2070.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter SuperscriptsandSubscripts
        {
            get
            {
                return GetFilter(ref _superscriptsandSubscripts, first: '\u2070', last: '\u209F');
            }
        }
        private static DefinedCharacterCodePointFilter _superscriptsandSubscripts;

        /// <summary>
        /// A filter which allows characters in the 'Currency Symbols' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+20A0 .. U+20CF.
        /// See http://www.unicode.org/charts/PDF/U20A0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter CurrencySymbols
        {
            get
            {
                return GetFilter(ref _currencySymbols, first: '\u20A0', last: '\u20CF');
            }
        }
        private static DefinedCharacterCodePointFilter _currencySymbols;

        /// <summary>
        /// A filter which allows characters in the 'Combining Diacritical Marks for Symbols' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+20D0 .. U+20FF.
        /// See http://www.unicode.org/charts/PDF/U20D0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter CombiningDiacriticalMarksforSymbols
        {
            get
            {
                return GetFilter(ref _combiningDiacriticalMarksforSymbols, first: '\u20D0', last: '\u20FF');
            }
        }
        private static DefinedCharacterCodePointFilter _combiningDiacriticalMarksforSymbols;

        /// <summary>
        /// A filter which allows characters in the 'Letterlike Symbols' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+2100 .. U+214F.
        /// See http://www.unicode.org/charts/PDF/U2100.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter LetterlikeSymbols
        {
            get
            {
                return GetFilter(ref _letterlikeSymbols, first: '\u2100', last: '\u214F');
            }
        }
        private static DefinedCharacterCodePointFilter _letterlikeSymbols;

        /// <summary>
        /// A filter which allows characters in the 'Number Forms' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+2150 .. U+218F.
        /// See http://www.unicode.org/charts/PDF/U2150.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter NumberForms
        {
            get
            {
                return GetFilter(ref _numberForms, first: '\u2150', last: '\u218F');
            }
        }
        private static DefinedCharacterCodePointFilter _numberForms;

        /// <summary>
        /// A filter which allows characters in the 'Arrows' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+2190 .. U+21FF.
        /// See http://www.unicode.org/charts/PDF/U2190.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Arrows
        {
            get
            {
                return GetFilter(ref _arrows, first: '\u2190', last: '\u21FF');
            }
        }
        private static DefinedCharacterCodePointFilter _arrows;

        /// <summary>
        /// A filter which allows characters in the 'Mathematical Operators' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+2200 .. U+22FF.
        /// See http://www.unicode.org/charts/PDF/U2200.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter MathematicalOperators
        {
            get
            {
                return GetFilter(ref _mathematicalOperators, first: '\u2200', last: '\u22FF');
            }
        }
        private static DefinedCharacterCodePointFilter _mathematicalOperators;

        /// <summary>
        /// A filter which allows characters in the 'Miscellaneous Technical' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+2300 .. U+23FF.
        /// See http://www.unicode.org/charts/PDF/U2300.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter MiscellaneousTechnical
        {
            get
            {
                return GetFilter(ref _miscellaneousTechnical, first: '\u2300', last: '\u23FF');
            }
        }
        private static DefinedCharacterCodePointFilter _miscellaneousTechnical;

        /// <summary>
        /// A filter which allows characters in the 'Control Pictures' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+2400 .. U+243F.
        /// See http://www.unicode.org/charts/PDF/U2400.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter ControlPictures
        {
            get
            {
                return GetFilter(ref _controlPictures, first: '\u2400', last: '\u243F');
            }
        }
        private static DefinedCharacterCodePointFilter _controlPictures;

        /// <summary>
        /// A filter which allows characters in the 'Optical Character Recognition' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+2440 .. U+245F.
        /// See http://www.unicode.org/charts/PDF/U2440.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter OpticalCharacterRecognition
        {
            get
            {
                return GetFilter(ref _opticalCharacterRecognition, first: '\u2440', last: '\u245F');
            }
        }
        private static DefinedCharacterCodePointFilter _opticalCharacterRecognition;

        /// <summary>
        /// A filter which allows characters in the 'Enclosed Alphanumerics' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+2460 .. U+24FF.
        /// See http://www.unicode.org/charts/PDF/U2460.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter EnclosedAlphanumerics
        {
            get
            {
                return GetFilter(ref _enclosedAlphanumerics, first: '\u2460', last: '\u24FF');
            }
        }
        private static DefinedCharacterCodePointFilter _enclosedAlphanumerics;

        /// <summary>
        /// A filter which allows characters in the 'Box Drawing' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+2500 .. U+257F.
        /// See http://www.unicode.org/charts/PDF/U2500.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter BoxDrawing
        {
            get
            {
                return GetFilter(ref _boxDrawing, first: '\u2500', last: '\u257F');
            }
        }
        private static DefinedCharacterCodePointFilter _boxDrawing;

        /// <summary>
        /// A filter which allows characters in the 'Block Elements' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+2580 .. U+259F.
        /// See http://www.unicode.org/charts/PDF/U2580.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter BlockElements
        {
            get
            {
                return GetFilter(ref _blockElements, first: '\u2580', last: '\u259F');
            }
        }
        private static DefinedCharacterCodePointFilter _blockElements;

        /// <summary>
        /// A filter which allows characters in the 'Geometric Shapes' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+25A0 .. U+25FF.
        /// See http://www.unicode.org/charts/PDF/U25A0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter GeometricShapes
        {
            get
            {
                return GetFilter(ref _geometricShapes, first: '\u25A0', last: '\u25FF');
            }
        }
        private static DefinedCharacterCodePointFilter _geometricShapes;

        /// <summary>
        /// A filter which allows characters in the 'Miscellaneous Symbols' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+2600 .. U+26FF.
        /// See http://www.unicode.org/charts/PDF/U2600.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter MiscellaneousSymbols
        {
            get
            {
                return GetFilter(ref _miscellaneousSymbols, first: '\u2600', last: '\u26FF');
            }
        }
        private static DefinedCharacterCodePointFilter _miscellaneousSymbols;

        /// <summary>
        /// A filter which allows characters in the 'Dingbats' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+2700 .. U+27BF.
        /// See http://www.unicode.org/charts/PDF/U2700.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Dingbats
        {
            get
            {
                return GetFilter(ref _dingbats, first: '\u2700', last: '\u27BF');
            }
        }
        private static DefinedCharacterCodePointFilter _dingbats;

        /// <summary>
        /// A filter which allows characters in the 'Miscellaneous Mathematical Symbols-A' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+27C0 .. U+27EF.
        /// See http://www.unicode.org/charts/PDF/U27C0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter MiscellaneousMathematicalSymbolsA
        {
            get
            {
                return GetFilter(ref _miscellaneousMathematicalSymbolsA, first: '\u27C0', last: '\u27EF');
            }
        }
        private static DefinedCharacterCodePointFilter _miscellaneousMathematicalSymbolsA;

        /// <summary>
        /// A filter which allows characters in the 'Supplemental Arrows-A' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+27F0 .. U+27FF.
        /// See http://www.unicode.org/charts/PDF/U27F0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter SupplementalArrowsA
        {
            get
            {
                return GetFilter(ref _supplementalArrowsA, first: '\u27F0', last: '\u27FF');
            }
        }
        private static DefinedCharacterCodePointFilter _supplementalArrowsA;

        /// <summary>
        /// A filter which allows characters in the 'Braille Patterns' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+2800 .. U+28FF.
        /// See http://www.unicode.org/charts/PDF/U2800.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter BraillePatterns
        {
            get
            {
                return GetFilter(ref _braillePatterns, first: '\u2800', last: '\u28FF');
            }
        }
        private static DefinedCharacterCodePointFilter _braillePatterns;

        /// <summary>
        /// A filter which allows characters in the 'Supplemental Arrows-B' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+2900 .. U+297F.
        /// See http://www.unicode.org/charts/PDF/U2900.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter SupplementalArrowsB
        {
            get
            {
                return GetFilter(ref _supplementalArrowsB, first: '\u2900', last: '\u297F');
            }
        }
        private static DefinedCharacterCodePointFilter _supplementalArrowsB;

        /// <summary>
        /// A filter which allows characters in the 'Miscellaneous Mathematical Symbols-B' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+2980 .. U+29FF.
        /// See http://www.unicode.org/charts/PDF/U2980.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter MiscellaneousMathematicalSymbolsB
        {
            get
            {
                return GetFilter(ref _miscellaneousMathematicalSymbolsB, first: '\u2980', last: '\u29FF');
            }
        }
        private static DefinedCharacterCodePointFilter _miscellaneousMathematicalSymbolsB;

        /// <summary>
        /// A filter which allows characters in the 'Supplemental Mathematical Operators' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+2A00 .. U+2AFF.
        /// See http://www.unicode.org/charts/PDF/U2A00.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter SupplementalMathematicalOperators
        {
            get
            {
                return GetFilter(ref _supplementalMathematicalOperators, first: '\u2A00', last: '\u2AFF');
            }
        }
        private static DefinedCharacterCodePointFilter _supplementalMathematicalOperators;

        /// <summary>
        /// A filter which allows characters in the 'Miscellaneous Symbols and Arrows' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+2B00 .. U+2BFF.
        /// See http://www.unicode.org/charts/PDF/U2B00.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter MiscellaneousSymbolsandArrows
        {
            get
            {
                return GetFilter(ref _miscellaneousSymbolsandArrows, first: '\u2B00', last: '\u2BFF');
            }
        }
        private static DefinedCharacterCodePointFilter _miscellaneousSymbolsandArrows;

        /// <summary>
        /// A filter which allows characters in the 'Glagolitic' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+2C00 .. U+2C5F.
        /// See http://www.unicode.org/charts/PDF/U2C00.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Glagolitic
        {
            get
            {
                return GetFilter(ref _glagolitic, first: '\u2C00', last: '\u2C5F');
            }
        }
        private static DefinedCharacterCodePointFilter _glagolitic;

        /// <summary>
        /// A filter which allows characters in the 'Latin Extended-C' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+2C60 .. U+2C7F.
        /// See http://www.unicode.org/charts/PDF/U2C60.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter LatinExtendedC
        {
            get
            {
                return GetFilter(ref _latinExtendedC, first: '\u2C60', last: '\u2C7F');
            }
        }
        private static DefinedCharacterCodePointFilter _latinExtendedC;

        /// <summary>
        /// A filter which allows characters in the 'Coptic' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+2C80 .. U+2CFF.
        /// See http://www.unicode.org/charts/PDF/U2C80.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Coptic
        {
            get
            {
                return GetFilter(ref _coptic, first: '\u2C80', last: '\u2CFF');
            }
        }
        private static DefinedCharacterCodePointFilter _coptic;

        /// <summary>
        /// A filter which allows characters in the 'Georgian Supplement' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+2D00 .. U+2D2F.
        /// See http://www.unicode.org/charts/PDF/U2D00.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter GeorgianSupplement
        {
            get
            {
                return GetFilter(ref _georgianSupplement, first: '\u2D00', last: '\u2D2F');
            }
        }
        private static DefinedCharacterCodePointFilter _georgianSupplement;

        /// <summary>
        /// A filter which allows characters in the 'Tifinagh' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+2D30 .. U+2D7F.
        /// See http://www.unicode.org/charts/PDF/U2D30.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Tifinagh
        {
            get
            {
                return GetFilter(ref _tifinagh, first: '\u2D30', last: '\u2D7F');
            }
        }
        private static DefinedCharacterCodePointFilter _tifinagh;

        /// <summary>
        /// A filter which allows characters in the 'Ethiopic Extended' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+2D80 .. U+2DDF.
        /// See http://www.unicode.org/charts/PDF/U2D80.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter EthiopicExtended
        {
            get
            {
                return GetFilter(ref _ethiopicExtended, first: '\u2D80', last: '\u2DDF');
            }
        }
        private static DefinedCharacterCodePointFilter _ethiopicExtended;

        /// <summary>
        /// A filter which allows characters in the 'Cyrillic Extended-A' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+2DE0 .. U+2DFF.
        /// See http://www.unicode.org/charts/PDF/U2DE0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter CyrillicExtendedA
        {
            get
            {
                return GetFilter(ref _cyrillicExtendedA, first: '\u2DE0', last: '\u2DFF');
            }
        }
        private static DefinedCharacterCodePointFilter _cyrillicExtendedA;

        /// <summary>
        /// A filter which allows characters in the 'Supplemental Punctuation' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+2E00 .. U+2E7F.
        /// See http://www.unicode.org/charts/PDF/U2E00.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter SupplementalPunctuation
        {
            get
            {
                return GetFilter(ref _supplementalPunctuation, first: '\u2E00', last: '\u2E7F');
            }
        }
        private static DefinedCharacterCodePointFilter _supplementalPunctuation;

        /// <summary>
        /// A filter which allows characters in the 'CJK Radicals Supplement' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+2E80 .. U+2EFF.
        /// See http://www.unicode.org/charts/PDF/U2E80.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter CJKRadicalsSupplement
        {
            get
            {
                return GetFilter(ref _cjkRadicalsSupplement, first: '\u2E80', last: '\u2EFF');
            }
        }
        private static DefinedCharacterCodePointFilter _cjkRadicalsSupplement;

        /// <summary>
        /// A filter which allows characters in the 'Kangxi Radicals' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+2F00 .. U+2FDF.
        /// See http://www.unicode.org/charts/PDF/U2F00.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter KangxiRadicals
        {
            get
            {
                return GetFilter(ref _kangxiRadicals, first: '\u2F00', last: '\u2FDF');
            }
        }
        private static DefinedCharacterCodePointFilter _kangxiRadicals;

        /// <summary>
        /// A filter which allows characters in the 'Ideographic Description Characters' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+2FF0 .. U+2FFF.
        /// See http://www.unicode.org/charts/PDF/U2FF0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter IdeographicDescriptionCharacters
        {
            get
            {
                return GetFilter(ref _ideographicDescriptionCharacters, first: '\u2FF0', last: '\u2FFF');
            }
        }
        private static DefinedCharacterCodePointFilter _ideographicDescriptionCharacters;

        /// <summary>
        /// A filter which allows characters in the 'CJK Symbols and Punctuation' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+3000 .. U+303F.
        /// See http://www.unicode.org/charts/PDF/U3000.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter CJKSymbolsandPunctuation
        {
            get
            {
                return GetFilter(ref _cjkSymbolsandPunctuation, first: '\u3000', last: '\u303F');
            }
        }
        private static DefinedCharacterCodePointFilter _cjkSymbolsandPunctuation;

        /// <summary>
        /// A filter which allows characters in the 'Hiragana' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+3040 .. U+309F.
        /// See http://www.unicode.org/charts/PDF/U3040.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Hiragana
        {
            get
            {
                return GetFilter(ref _hiragana, first: '\u3040', last: '\u309F');
            }
        }
        private static DefinedCharacterCodePointFilter _hiragana;

        /// <summary>
        /// A filter which allows characters in the 'Katakana' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+30A0 .. U+30FF.
        /// See http://www.unicode.org/charts/PDF/U30A0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Katakana
        {
            get
            {
                return GetFilter(ref _katakana, first: '\u30A0', last: '\u30FF');
            }
        }
        private static DefinedCharacterCodePointFilter _katakana;

        /// <summary>
        /// A filter which allows characters in the 'Bopomofo' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+3100 .. U+312F.
        /// See http://www.unicode.org/charts/PDF/U3100.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Bopomofo
        {
            get
            {
                return GetFilter(ref _bopomofo, first: '\u3100', last: '\u312F');
            }
        }
        private static DefinedCharacterCodePointFilter _bopomofo;

        /// <summary>
        /// A filter which allows characters in the 'Hangul Compatibility Jamo' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+3130 .. U+318F.
        /// See http://www.unicode.org/charts/PDF/U3130.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter HangulCompatibilityJamo
        {
            get
            {
                return GetFilter(ref _hangulCompatibilityJamo, first: '\u3130', last: '\u318F');
            }
        }
        private static DefinedCharacterCodePointFilter _hangulCompatibilityJamo;

        /// <summary>
        /// A filter which allows characters in the 'Kanbun' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+3190 .. U+319F.
        /// See http://www.unicode.org/charts/PDF/U3190.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Kanbun
        {
            get
            {
                return GetFilter(ref _kanbun, first: '\u3190', last: '\u319F');
            }
        }
        private static DefinedCharacterCodePointFilter _kanbun;

        /// <summary>
        /// A filter which allows characters in the 'Bopomofo Extended' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+31A0 .. U+31BF.
        /// See http://www.unicode.org/charts/PDF/U31A0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter BopomofoExtended
        {
            get
            {
                return GetFilter(ref _bopomofoExtended, first: '\u31A0', last: '\u31BF');
            }
        }
        private static DefinedCharacterCodePointFilter _bopomofoExtended;

        /// <summary>
        /// A filter which allows characters in the 'CJK Strokes' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+31C0 .. U+31EF.
        /// See http://www.unicode.org/charts/PDF/U31C0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter CJKStrokes
        {
            get
            {
                return GetFilter(ref _cjkStrokes, first: '\u31C0', last: '\u31EF');
            }
        }
        private static DefinedCharacterCodePointFilter _cjkStrokes;

        /// <summary>
        /// A filter which allows characters in the 'Katakana Phonetic Extensions' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+31F0 .. U+31FF.
        /// See http://www.unicode.org/charts/PDF/U31F0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter KatakanaPhoneticExtensions
        {
            get
            {
                return GetFilter(ref _katakanaPhoneticExtensions, first: '\u31F0', last: '\u31FF');
            }
        }
        private static DefinedCharacterCodePointFilter _katakanaPhoneticExtensions;

        /// <summary>
        /// A filter which allows characters in the 'Enclosed CJK Letters and Months' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+3200 .. U+32FF.
        /// See http://www.unicode.org/charts/PDF/U3200.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter EnclosedCJKLettersandMonths
        {
            get
            {
                return GetFilter(ref _enclosedCJKLettersandMonths, first: '\u3200', last: '\u32FF');
            }
        }
        private static DefinedCharacterCodePointFilter _enclosedCJKLettersandMonths;

        /// <summary>
        /// A filter which allows characters in the 'CJK Compatibility' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+3300 .. U+33FF.
        /// See http://www.unicode.org/charts/PDF/U3300.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter CJKCompatibility
        {
            get
            {
                return GetFilter(ref _cjkCompatibility, first: '\u3300', last: '\u33FF');
            }
        }
        private static DefinedCharacterCodePointFilter _cjkCompatibility;

        /// <summary>
        /// A filter which allows characters in the 'CJK Unified Ideographs Extension A' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+3400 .. U+4DBF.
        /// See http://www.unicode.org/charts/PDF/U3400.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter CJKUnifiedIdeographsExtensionA
        {
            get
            {
                return GetFilter(ref _cjkUnifiedIdeographsExtensionA, first: '\u3400', last: '\u4DBF');
            }
        }
        private static DefinedCharacterCodePointFilter _cjkUnifiedIdeographsExtensionA;

        /// <summary>
        /// A filter which allows characters in the 'Yijing Hexagram Symbols' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+4DC0 .. U+4DFF.
        /// See http://www.unicode.org/charts/PDF/U4DC0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter YijingHexagramSymbols
        {
            get
            {
                return GetFilter(ref _yijingHexagramSymbols, first: '\u4DC0', last: '\u4DFF');
            }
        }
        private static DefinedCharacterCodePointFilter _yijingHexagramSymbols;

        /// <summary>
        /// A filter which allows characters in the 'CJK Unified Ideographs' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+4E00 .. U+9FFF.
        /// See http://www.unicode.org/charts/PDF/U4E00.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter CJKUnifiedIdeographs
        {
            get
            {
                return GetFilter(ref _cjkUnifiedIdeographs, first: '\u4E00', last: '\u9FFF');
            }
        }
        private static DefinedCharacterCodePointFilter _cjkUnifiedIdeographs;

        /// <summary>
        /// A filter which allows characters in the 'Yi Syllables' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+A000 .. U+A48F.
        /// See http://www.unicode.org/charts/PDF/UA000.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter YiSyllables
        {
            get
            {
                return GetFilter(ref _yiSyllables, first: '\uA000', last: '\uA48F');
            }
        }
        private static DefinedCharacterCodePointFilter _yiSyllables;

        /// <summary>
        /// A filter which allows characters in the 'Yi Radicals' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+A490 .. U+A4CF.
        /// See http://www.unicode.org/charts/PDF/UA490.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter YiRadicals
        {
            get
            {
                return GetFilter(ref _yiRadicals, first: '\uA490', last: '\uA4CF');
            }
        }
        private static DefinedCharacterCodePointFilter _yiRadicals;

        /// <summary>
        /// A filter which allows characters in the 'Lisu' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+A4D0 .. U+A4FF.
        /// See http://www.unicode.org/charts/PDF/UA4D0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Lisu
        {
            get
            {
                return GetFilter(ref _lisu, first: '\uA4D0', last: '\uA4FF');
            }
        }
        private static DefinedCharacterCodePointFilter _lisu;

        /// <summary>
        /// A filter which allows characters in the 'Vai' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+A500 .. U+A63F.
        /// See http://www.unicode.org/charts/PDF/UA500.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Vai
        {
            get
            {
                return GetFilter(ref _vai, first: '\uA500', last: '\uA63F');
            }
        }
        private static DefinedCharacterCodePointFilter _vai;

        /// <summary>
        /// A filter which allows characters in the 'Cyrillic Extended-B' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+A640 .. U+A69F.
        /// See http://www.unicode.org/charts/PDF/UA640.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter CyrillicExtendedB
        {
            get
            {
                return GetFilter(ref _cyrillicExtendedB, first: '\uA640', last: '\uA69F');
            }
        }
        private static DefinedCharacterCodePointFilter _cyrillicExtendedB;

        /// <summary>
        /// A filter which allows characters in the 'Bamum' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+A6A0 .. U+A6FF.
        /// See http://www.unicode.org/charts/PDF/UA6A0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Bamum
        {
            get
            {
                return GetFilter(ref _bamum, first: '\uA6A0', last: '\uA6FF');
            }
        }
        private static DefinedCharacterCodePointFilter _bamum;

        /// <summary>
        /// A filter which allows characters in the 'Modifier Tone Letters' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+A700 .. U+A71F.
        /// See http://www.unicode.org/charts/PDF/UA700.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter ModifierToneLetters
        {
            get
            {
                return GetFilter(ref _modifierToneLetters, first: '\uA700', last: '\uA71F');
            }
        }
        private static DefinedCharacterCodePointFilter _modifierToneLetters;

        /// <summary>
        /// A filter which allows characters in the 'Latin Extended-D' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+A720 .. U+A7FF.
        /// See http://www.unicode.org/charts/PDF/UA720.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter LatinExtendedD
        {
            get
            {
                return GetFilter(ref _latinExtendedD, first: '\uA720', last: '\uA7FF');
            }
        }
        private static DefinedCharacterCodePointFilter _latinExtendedD;

        /// <summary>
        /// A filter which allows characters in the 'Syloti Nagri' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+A800 .. U+A82F.
        /// See http://www.unicode.org/charts/PDF/UA800.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter SylotiNagri
        {
            get
            {
                return GetFilter(ref _sylotiNagri, first: '\uA800', last: '\uA82F');
            }
        }
        private static DefinedCharacterCodePointFilter _sylotiNagri;

        /// <summary>
        /// A filter which allows characters in the 'Common Indic Number Forms' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+A830 .. U+A83F.
        /// See http://www.unicode.org/charts/PDF/UA830.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter CommonIndicNumberForms
        {
            get
            {
                return GetFilter(ref _commonIndicNumberForms, first: '\uA830', last: '\uA83F');
            }
        }
        private static DefinedCharacterCodePointFilter _commonIndicNumberForms;

        /// <summary>
        /// A filter which allows characters in the 'Phags-pa' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+A840 .. U+A87F.
        /// See http://www.unicode.org/charts/PDF/UA840.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Phagspa
        {
            get
            {
                return GetFilter(ref _phagspa, first: '\uA840', last: '\uA87F');
            }
        }
        private static DefinedCharacterCodePointFilter _phagspa;

        /// <summary>
        /// A filter which allows characters in the 'Saurashtra' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+A880 .. U+A8DF.
        /// See http://www.unicode.org/charts/PDF/UA880.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Saurashtra
        {
            get
            {
                return GetFilter(ref _saurashtra, first: '\uA880', last: '\uA8DF');
            }
        }
        private static DefinedCharacterCodePointFilter _saurashtra;

        /// <summary>
        /// A filter which allows characters in the 'Devanagari Extended' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+A8E0 .. U+A8FF.
        /// See http://www.unicode.org/charts/PDF/UA8E0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter DevanagariExtended
        {
            get
            {
                return GetFilter(ref _devanagariExtended, first: '\uA8E0', last: '\uA8FF');
            }
        }
        private static DefinedCharacterCodePointFilter _devanagariExtended;

        /// <summary>
        /// A filter which allows characters in the 'Kayah Li' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+A900 .. U+A92F.
        /// See http://www.unicode.org/charts/PDF/UA900.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter KayahLi
        {
            get
            {
                return GetFilter(ref _kayahLi, first: '\uA900', last: '\uA92F');
            }
        }
        private static DefinedCharacterCodePointFilter _kayahLi;

        /// <summary>
        /// A filter which allows characters in the 'Rejang' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+A930 .. U+A95F.
        /// See http://www.unicode.org/charts/PDF/UA930.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Rejang
        {
            get
            {
                return GetFilter(ref _rejang, first: '\uA930', last: '\uA95F');
            }
        }
        private static DefinedCharacterCodePointFilter _rejang;

        /// <summary>
        /// A filter which allows characters in the 'Hangul Jamo Extended-A' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+A960 .. U+A97F.
        /// See http://www.unicode.org/charts/PDF/UA960.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter HangulJamoExtendedA
        {
            get
            {
                return GetFilter(ref _hangulJamoExtendedA, first: '\uA960', last: '\uA97F');
            }
        }
        private static DefinedCharacterCodePointFilter _hangulJamoExtendedA;

        /// <summary>
        /// A filter which allows characters in the 'Javanese' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+A980 .. U+A9DF.
        /// See http://www.unicode.org/charts/PDF/UA980.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Javanese
        {
            get
            {
                return GetFilter(ref _javanese, first: '\uA980', last: '\uA9DF');
            }
        }
        private static DefinedCharacterCodePointFilter _javanese;

        /// <summary>
        /// A filter which allows characters in the 'Myanmar Extended-B' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+A9E0 .. U+A9FF.
        /// See http://www.unicode.org/charts/PDF/UA9E0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter MyanmarExtendedB
        {
            get
            {
                return GetFilter(ref _myanmarExtendedB, first: '\uA9E0', last: '\uA9FF');
            }
        }
        private static DefinedCharacterCodePointFilter _myanmarExtendedB;

        /// <summary>
        /// A filter which allows characters in the 'Cham' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+AA00 .. U+AA5F.
        /// See http://www.unicode.org/charts/PDF/UAA00.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Cham
        {
            get
            {
                return GetFilter(ref _cham, first: '\uAA00', last: '\uAA5F');
            }
        }
        private static DefinedCharacterCodePointFilter _cham;

        /// <summary>
        /// A filter which allows characters in the 'Myanmar Extended-A' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+AA60 .. U+AA7F.
        /// See http://www.unicode.org/charts/PDF/UAA60.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter MyanmarExtendedA
        {
            get
            {
                return GetFilter(ref _myanmarExtendedA, first: '\uAA60', last: '\uAA7F');
            }
        }
        private static DefinedCharacterCodePointFilter _myanmarExtendedA;

        /// <summary>
        /// A filter which allows characters in the 'Tai Viet' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+AA80 .. U+AADF.
        /// See http://www.unicode.org/charts/PDF/UAA80.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter TaiViet
        {
            get
            {
                return GetFilter(ref _taiViet, first: '\uAA80', last: '\uAADF');
            }
        }
        private static DefinedCharacterCodePointFilter _taiViet;

        /// <summary>
        /// A filter which allows characters in the 'Meetei Mayek Extensions' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+AAE0 .. U+AAFF.
        /// See http://www.unicode.org/charts/PDF/UAAE0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter MeeteiMayekExtensions
        {
            get
            {
                return GetFilter(ref _meeteiMayekExtensions, first: '\uAAE0', last: '\uAAFF');
            }
        }
        private static DefinedCharacterCodePointFilter _meeteiMayekExtensions;

        /// <summary>
        /// A filter which allows characters in the 'Ethiopic Extended-A' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+AB00 .. U+AB2F.
        /// See http://www.unicode.org/charts/PDF/UAB00.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter EthiopicExtendedA
        {
            get
            {
                return GetFilter(ref _ethiopicExtendedA, first: '\uAB00', last: '\uAB2F');
            }
        }
        private static DefinedCharacterCodePointFilter _ethiopicExtendedA;

        /// <summary>
        /// A filter which allows characters in the 'Latin Extended-E' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+AB30 .. U+AB6F.
        /// See http://www.unicode.org/charts/PDF/UAB30.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter LatinExtendedE
        {
            get
            {
                return GetFilter(ref _latinExtendedE, first: '\uAB30', last: '\uAB6F');
            }
        }
        private static DefinedCharacterCodePointFilter _latinExtendedE;

        /// <summary>
        /// A filter which allows characters in the 'Meetei Mayek' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+ABC0 .. U+ABFF.
        /// See http://www.unicode.org/charts/PDF/UABC0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter MeeteiMayek
        {
            get
            {
                return GetFilter(ref _meeteiMayek, first: '\uABC0', last: '\uABFF');
            }
        }
        private static DefinedCharacterCodePointFilter _meeteiMayek;

        /// <summary>
        /// A filter which allows characters in the 'Hangul Syllables' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+AC00 .. U+D7AF.
        /// See http://www.unicode.org/charts/PDF/UAC00.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter HangulSyllables
        {
            get
            {
                return GetFilter(ref _hangulSyllables, first: '\uAC00', last: '\uD7AF');
            }
        }
        private static DefinedCharacterCodePointFilter _hangulSyllables;

        /// <summary>
        /// A filter which allows characters in the 'Hangul Jamo Extended-B' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+D7B0 .. U+D7FF.
        /// See http://www.unicode.org/charts/PDF/UD7B0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter HangulJamoExtendedB
        {
            get
            {
                return GetFilter(ref _hangulJamoExtendedB, first: '\uD7B0', last: '\uD7FF');
            }
        }
        private static DefinedCharacterCodePointFilter _hangulJamoExtendedB;

        /// <summary>
        /// A filter which allows characters in the 'CJK Compatibility Ideographs' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+F900 .. U+FAFF.
        /// See http://www.unicode.org/charts/PDF/UF900.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter CJKCompatibilityIdeographs
        {
            get
            {
                return GetFilter(ref _cjkCompatibilityIdeographs, first: '\uF900', last: '\uFAFF');
            }
        }
        private static DefinedCharacterCodePointFilter _cjkCompatibilityIdeographs;

        /// <summary>
        /// A filter which allows characters in the 'Alphabetic Presentation Forms' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+FB00 .. U+FB4F.
        /// See http://www.unicode.org/charts/PDF/UFB00.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter AlphabeticPresentationForms
        {
            get
            {
                return GetFilter(ref _alphabeticPresentationForms, first: '\uFB00', last: '\uFB4F');
            }
        }
        private static DefinedCharacterCodePointFilter _alphabeticPresentationForms;

        /// <summary>
        /// A filter which allows characters in the 'Arabic Presentation Forms-A' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+FB50 .. U+FDFF.
        /// See http://www.unicode.org/charts/PDF/UFB50.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter ArabicPresentationFormsA
        {
            get
            {
                return GetFilter(ref _arabicPresentationFormsA, first: '\uFB50', last: '\uFDFF');
            }
        }
        private static DefinedCharacterCodePointFilter _arabicPresentationFormsA;

        /// <summary>
        /// A filter which allows characters in the 'Variation Selectors' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+FE00 .. U+FE0F.
        /// See http://www.unicode.org/charts/PDF/UFE00.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter VariationSelectors
        {
            get
            {
                return GetFilter(ref _variationSelectors, first: '\uFE00', last: '\uFE0F');
            }
        }
        private static DefinedCharacterCodePointFilter _variationSelectors;

        /// <summary>
        /// A filter which allows characters in the 'Vertical Forms' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+FE10 .. U+FE1F.
        /// See http://www.unicode.org/charts/PDF/UFE10.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter VerticalForms
        {
            get
            {
                return GetFilter(ref _verticalForms, first: '\uFE10', last: '\uFE1F');
            }
        }
        private static DefinedCharacterCodePointFilter _verticalForms;

        /// <summary>
        /// A filter which allows characters in the 'Combining Half Marks' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+FE20 .. U+FE2F.
        /// See http://www.unicode.org/charts/PDF/UFE20.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter CombiningHalfMarks
        {
            get
            {
                return GetFilter(ref _combiningHalfMarks, first: '\uFE20', last: '\uFE2F');
            }
        }
        private static DefinedCharacterCodePointFilter _combiningHalfMarks;

        /// <summary>
        /// A filter which allows characters in the 'CJK Compatibility Forms' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+FE30 .. U+FE4F.
        /// See http://www.unicode.org/charts/PDF/UFE30.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter CJKCompatibilityForms
        {
            get
            {
                return GetFilter(ref _cjkCompatibilityForms, first: '\uFE30', last: '\uFE4F');
            }
        }
        private static DefinedCharacterCodePointFilter _cjkCompatibilityForms;

        /// <summary>
        /// A filter which allows characters in the 'Small Form Variants' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+FE50 .. U+FE6F.
        /// See http://www.unicode.org/charts/PDF/UFE50.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter SmallFormVariants
        {
            get
            {
                return GetFilter(ref _smallFormVariants, first: '\uFE50', last: '\uFE6F');
            }
        }
        private static DefinedCharacterCodePointFilter _smallFormVariants;

        /// <summary>
        /// A filter which allows characters in the 'Arabic Presentation Forms-B' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+FE70 .. U+FEFF.
        /// See http://www.unicode.org/charts/PDF/UFE70.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter ArabicPresentationFormsB
        {
            get
            {
                return GetFilter(ref _arabicPresentationFormsB, first: '\uFE70', last: '\uFEFF');
            }
        }
        private static DefinedCharacterCodePointFilter _arabicPresentationFormsB;

        /// <summary>
        /// A filter which allows characters in the 'Halfwidth and Fullwidth Forms' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+FF00 .. U+FFEF.
        /// See http://www.unicode.org/charts/PDF/UFF00.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter HalfwidthandFullwidthForms
        {
            get
            {
                return GetFilter(ref _halfwidthandFullwidthForms, first: '\uFF00', last: '\uFFEF');
            }
        }
        private static DefinedCharacterCodePointFilter _halfwidthandFullwidthForms;

        /// <summary>
        /// A filter which allows characters in the 'Specials' Unicode range.
        /// </summary>
        /// <remarks>
        /// This range spans the code points U+FFF0 .. U+FFFF.
        /// See http://www.unicode.org/charts/PDF/UFFF0.pdf for the full set of characters in this range.
        /// </remarks>
        public static ICodePointFilter Specials
        {
            get
            {
                return GetFilter(ref _specials, first: '\uFFF0', last: '\uFFFF');
            }
        }
        private static DefinedCharacterCodePointFilter _specials;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ICodePointFilter GetFilter(ref DefinedCharacterCodePointFilter filter, char first, char last)
        {
            // Return an existing filter if it has already been created, otherwise
            // create a new filter on-demand.
            return Volatile.Read(ref filter) ?? GetFilterSlow(ref filter, first, last);
        }

        private static ICodePointFilter GetFilterSlow(ref DefinedCharacterCodePointFilter filter, char first, char last)
        {
            // If the filter hasn't been created, create it now.
            // It's ok if two threads race and one overwrites the other's 'filter' value.
            DefinedCharacterCodePointFilter newFilter = new DefinedCharacterCodePointFilter(first, last);
            Volatile.Write(ref filter, newFilter);
            return newFilter;
        }

        /// <summary>
        /// A code point filter which returns only defined characters within a certain
        /// range of the Unicode specification.
        /// </summary>
        private sealed class DefinedCharacterCodePointFilter : ICodePointFilter
        {
            private readonly int _count;
            private readonly int _first;

            public DefinedCharacterCodePointFilter(int first, int last)
            {
                Debug.Assert(0 <= first);
                Debug.Assert(first <= last);
                Debug.Assert(last <= 0xFFFF);

                _first = first;
                _count = last - first + 1;
            }

            public IEnumerable<int> GetAllowedCodePoints()
            {
                for (int i = 0; i < _count; i++)
                {
                    int thisCodePoint = _first + i;
                    if (UnicodeHelpers.IsCharacterDefined((char)thisCodePoint))
                    {
                        yield return thisCodePoint;
                    }
                }
            }
        }

        /// <summary>
        /// A filter that allows no code points.
        /// </summary>
        private sealed class EmptyCodePointFilter : ICodePointFilter
        {
            private static readonly int[] _emptyArray = new int[0]; // immutable since empty

            public IEnumerable<int> GetAllowedCodePoints()
            {
                return _emptyArray;
            }
        }
    }
}
