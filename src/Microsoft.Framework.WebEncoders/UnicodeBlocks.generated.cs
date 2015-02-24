// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.Framework.WebEncoders
{
    public static partial class UnicodeBlocks
    {
        /// <summary>
        /// Represents the 'Basic Latin' Unicode block (U+0000..U+007F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0000.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock BasicLatin
        {
            get
            {
                return Volatile.Read(ref _basicLatin) ?? CreateBlock(ref _basicLatin, first: '\u0000', last: '\u007F');
            }
        }
        private static UnicodeBlock _basicLatin;

        /// <summary>
        /// Represents the 'Latin-1 Supplement' Unicode block (U+0080..U+00FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0080.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Latin1Supplement
        {
            get
            {
                return Volatile.Read(ref _latin1Supplement) ?? CreateBlock(ref _latin1Supplement, first: '\u0080', last: '\u00FF');
            }
        }
        private static UnicodeBlock _latin1Supplement;

        /// <summary>
        /// Represents the 'Latin Extended-A' Unicode block (U+0100..U+017F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0100.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock LatinExtendedA
        {
            get
            {
                return Volatile.Read(ref _latinExtendedA) ?? CreateBlock(ref _latinExtendedA, first: '\u0100', last: '\u017F');
            }
        }
        private static UnicodeBlock _latinExtendedA;

        /// <summary>
        /// Represents the 'Latin Extended-B' Unicode block (U+0180..U+024F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0180.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock LatinExtendedB
        {
            get
            {
                return Volatile.Read(ref _latinExtendedB) ?? CreateBlock(ref _latinExtendedB, first: '\u0180', last: '\u024F');
            }
        }
        private static UnicodeBlock _latinExtendedB;

        /// <summary>
        /// Represents the 'IPA Extensions' Unicode block (U+0250..U+02AF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0250.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock IPAExtensions
        {
            get
            {
                return Volatile.Read(ref _ipaExtensions) ?? CreateBlock(ref _ipaExtensions, first: '\u0250', last: '\u02AF');
            }
        }
        private static UnicodeBlock _ipaExtensions;

        /// <summary>
        /// Represents the 'Spacing Modifier Letters' Unicode block (U+02B0..U+02FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U02B0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock SpacingModifierLetters
        {
            get
            {
                return Volatile.Read(ref _spacingModifierLetters) ?? CreateBlock(ref _spacingModifierLetters, first: '\u02B0', last: '\u02FF');
            }
        }
        private static UnicodeBlock _spacingModifierLetters;

        /// <summary>
        /// Represents the 'Combining Diacritical Marks' Unicode block (U+0300..U+036F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0300.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock CombiningDiacriticalMarks
        {
            get
            {
                return Volatile.Read(ref _combiningDiacriticalMarks) ?? CreateBlock(ref _combiningDiacriticalMarks, first: '\u0300', last: '\u036F');
            }
        }
        private static UnicodeBlock _combiningDiacriticalMarks;

        /// <summary>
        /// Represents the 'Greek and Coptic' Unicode block (U+0370..U+03FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0370.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock GreekandCoptic
        {
            get
            {
                return Volatile.Read(ref _greekandCoptic) ?? CreateBlock(ref _greekandCoptic, first: '\u0370', last: '\u03FF');
            }
        }
        private static UnicodeBlock _greekandCoptic;

        /// <summary>
        /// Represents the 'Cyrillic' Unicode block (U+0400..U+04FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0400.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Cyrillic
        {
            get
            {
                return Volatile.Read(ref _cyrillic) ?? CreateBlock(ref _cyrillic, first: '\u0400', last: '\u04FF');
            }
        }
        private static UnicodeBlock _cyrillic;

        /// <summary>
        /// Represents the 'Cyrillic Supplement' Unicode block (U+0500..U+052F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0500.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock CyrillicSupplement
        {
            get
            {
                return Volatile.Read(ref _cyrillicSupplement) ?? CreateBlock(ref _cyrillicSupplement, first: '\u0500', last: '\u052F');
            }
        }
        private static UnicodeBlock _cyrillicSupplement;

        /// <summary>
        /// Represents the 'Armenian' Unicode block (U+0530..U+058F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0530.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Armenian
        {
            get
            {
                return Volatile.Read(ref _armenian) ?? CreateBlock(ref _armenian, first: '\u0530', last: '\u058F');
            }
        }
        private static UnicodeBlock _armenian;

        /// <summary>
        /// Represents the 'Hebrew' Unicode block (U+0590..U+05FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0590.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Hebrew
        {
            get
            {
                return Volatile.Read(ref _hebrew) ?? CreateBlock(ref _hebrew, first: '\u0590', last: '\u05FF');
            }
        }
        private static UnicodeBlock _hebrew;

        /// <summary>
        /// Represents the 'Arabic' Unicode block (U+0600..U+06FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0600.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Arabic
        {
            get
            {
                return Volatile.Read(ref _arabic) ?? CreateBlock(ref _arabic, first: '\u0600', last: '\u06FF');
            }
        }
        private static UnicodeBlock _arabic;

        /// <summary>
        /// Represents the 'Syriac' Unicode block (U+0700..U+074F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0700.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Syriac
        {
            get
            {
                return Volatile.Read(ref _syriac) ?? CreateBlock(ref _syriac, first: '\u0700', last: '\u074F');
            }
        }
        private static UnicodeBlock _syriac;

        /// <summary>
        /// Represents the 'Arabic Supplement' Unicode block (U+0750..U+077F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0750.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock ArabicSupplement
        {
            get
            {
                return Volatile.Read(ref _arabicSupplement) ?? CreateBlock(ref _arabicSupplement, first: '\u0750', last: '\u077F');
            }
        }
        private static UnicodeBlock _arabicSupplement;

        /// <summary>
        /// Represents the 'Thaana' Unicode block (U+0780..U+07BF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0780.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Thaana
        {
            get
            {
                return Volatile.Read(ref _thaana) ?? CreateBlock(ref _thaana, first: '\u0780', last: '\u07BF');
            }
        }
        private static UnicodeBlock _thaana;

        /// <summary>
        /// Represents the 'NKo' Unicode block (U+07C0..U+07FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U07C0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock NKo
        {
            get
            {
                return Volatile.Read(ref _nKo) ?? CreateBlock(ref _nKo, first: '\u07C0', last: '\u07FF');
            }
        }
        private static UnicodeBlock _nKo;

        /// <summary>
        /// Represents the 'Samaritan' Unicode block (U+0800..U+083F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0800.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Samaritan
        {
            get
            {
                return Volatile.Read(ref _samaritan) ?? CreateBlock(ref _samaritan, first: '\u0800', last: '\u083F');
            }
        }
        private static UnicodeBlock _samaritan;

        /// <summary>
        /// Represents the 'Mandaic' Unicode block (U+0840..U+085F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0840.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Mandaic
        {
            get
            {
                return Volatile.Read(ref _mandaic) ?? CreateBlock(ref _mandaic, first: '\u0840', last: '\u085F');
            }
        }
        private static UnicodeBlock _mandaic;

        /// <summary>
        /// Represents the 'Arabic Extended-A' Unicode block (U+08A0..U+08FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U08A0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock ArabicExtendedA
        {
            get
            {
                return Volatile.Read(ref _arabicExtendedA) ?? CreateBlock(ref _arabicExtendedA, first: '\u08A0', last: '\u08FF');
            }
        }
        private static UnicodeBlock _arabicExtendedA;

        /// <summary>
        /// Represents the 'Devanagari' Unicode block (U+0900..U+097F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0900.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Devanagari
        {
            get
            {
                return Volatile.Read(ref _devanagari) ?? CreateBlock(ref _devanagari, first: '\u0900', last: '\u097F');
            }
        }
        private static UnicodeBlock _devanagari;

        /// <summary>
        /// Represents the 'Bengali' Unicode block (U+0980..U+09FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0980.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Bengali
        {
            get
            {
                return Volatile.Read(ref _bengali) ?? CreateBlock(ref _bengali, first: '\u0980', last: '\u09FF');
            }
        }
        private static UnicodeBlock _bengali;

        /// <summary>
        /// Represents the 'Gurmukhi' Unicode block (U+0A00..U+0A7F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0A00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Gurmukhi
        {
            get
            {
                return Volatile.Read(ref _gurmukhi) ?? CreateBlock(ref _gurmukhi, first: '\u0A00', last: '\u0A7F');
            }
        }
        private static UnicodeBlock _gurmukhi;

        /// <summary>
        /// Represents the 'Gujarati' Unicode block (U+0A80..U+0AFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0A80.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Gujarati
        {
            get
            {
                return Volatile.Read(ref _gujarati) ?? CreateBlock(ref _gujarati, first: '\u0A80', last: '\u0AFF');
            }
        }
        private static UnicodeBlock _gujarati;

        /// <summary>
        /// Represents the 'Oriya' Unicode block (U+0B00..U+0B7F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0B00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Oriya
        {
            get
            {
                return Volatile.Read(ref _oriya) ?? CreateBlock(ref _oriya, first: '\u0B00', last: '\u0B7F');
            }
        }
        private static UnicodeBlock _oriya;

        /// <summary>
        /// Represents the 'Tamil' Unicode block (U+0B80..U+0BFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0B80.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Tamil
        {
            get
            {
                return Volatile.Read(ref _tamil) ?? CreateBlock(ref _tamil, first: '\u0B80', last: '\u0BFF');
            }
        }
        private static UnicodeBlock _tamil;

        /// <summary>
        /// Represents the 'Telugu' Unicode block (U+0C00..U+0C7F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0C00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Telugu
        {
            get
            {
                return Volatile.Read(ref _telugu) ?? CreateBlock(ref _telugu, first: '\u0C00', last: '\u0C7F');
            }
        }
        private static UnicodeBlock _telugu;

        /// <summary>
        /// Represents the 'Kannada' Unicode block (U+0C80..U+0CFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0C80.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Kannada
        {
            get
            {
                return Volatile.Read(ref _kannada) ?? CreateBlock(ref _kannada, first: '\u0C80', last: '\u0CFF');
            }
        }
        private static UnicodeBlock _kannada;

        /// <summary>
        /// Represents the 'Malayalam' Unicode block (U+0D00..U+0D7F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0D00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Malayalam
        {
            get
            {
                return Volatile.Read(ref _malayalam) ?? CreateBlock(ref _malayalam, first: '\u0D00', last: '\u0D7F');
            }
        }
        private static UnicodeBlock _malayalam;

        /// <summary>
        /// Represents the 'Sinhala' Unicode block (U+0D80..U+0DFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0D80.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Sinhala
        {
            get
            {
                return Volatile.Read(ref _sinhala) ?? CreateBlock(ref _sinhala, first: '\u0D80', last: '\u0DFF');
            }
        }
        private static UnicodeBlock _sinhala;

        /// <summary>
        /// Represents the 'Thai' Unicode block (U+0E00..U+0E7F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0E00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Thai
        {
            get
            {
                return Volatile.Read(ref _thai) ?? CreateBlock(ref _thai, first: '\u0E00', last: '\u0E7F');
            }
        }
        private static UnicodeBlock _thai;

        /// <summary>
        /// Represents the 'Lao' Unicode block (U+0E80..U+0EFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0E80.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Lao
        {
            get
            {
                return Volatile.Read(ref _lao) ?? CreateBlock(ref _lao, first: '\u0E80', last: '\u0EFF');
            }
        }
        private static UnicodeBlock _lao;

        /// <summary>
        /// Represents the 'Tibetan' Unicode block (U+0F00..U+0FFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0F00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Tibetan
        {
            get
            {
                return Volatile.Read(ref _tibetan) ?? CreateBlock(ref _tibetan, first: '\u0F00', last: '\u0FFF');
            }
        }
        private static UnicodeBlock _tibetan;

        /// <summary>
        /// Represents the 'Myanmar' Unicode block (U+1000..U+109F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1000.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Myanmar
        {
            get
            {
                return Volatile.Read(ref _myanmar) ?? CreateBlock(ref _myanmar, first: '\u1000', last: '\u109F');
            }
        }
        private static UnicodeBlock _myanmar;

        /// <summary>
        /// Represents the 'Georgian' Unicode block (U+10A0..U+10FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U10A0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Georgian
        {
            get
            {
                return Volatile.Read(ref _georgian) ?? CreateBlock(ref _georgian, first: '\u10A0', last: '\u10FF');
            }
        }
        private static UnicodeBlock _georgian;

        /// <summary>
        /// Represents the 'Hangul Jamo' Unicode block (U+1100..U+11FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1100.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock HangulJamo
        {
            get
            {
                return Volatile.Read(ref _hangulJamo) ?? CreateBlock(ref _hangulJamo, first: '\u1100', last: '\u11FF');
            }
        }
        private static UnicodeBlock _hangulJamo;

        /// <summary>
        /// Represents the 'Ethiopic' Unicode block (U+1200..U+137F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1200.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Ethiopic
        {
            get
            {
                return Volatile.Read(ref _ethiopic) ?? CreateBlock(ref _ethiopic, first: '\u1200', last: '\u137F');
            }
        }
        private static UnicodeBlock _ethiopic;

        /// <summary>
        /// Represents the 'Ethiopic Supplement' Unicode block (U+1380..U+139F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1380.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock EthiopicSupplement
        {
            get
            {
                return Volatile.Read(ref _ethiopicSupplement) ?? CreateBlock(ref _ethiopicSupplement, first: '\u1380', last: '\u139F');
            }
        }
        private static UnicodeBlock _ethiopicSupplement;

        /// <summary>
        /// Represents the 'Cherokee' Unicode block (U+13A0..U+13FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U13A0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Cherokee
        {
            get
            {
                return Volatile.Read(ref _cherokee) ?? CreateBlock(ref _cherokee, first: '\u13A0', last: '\u13FF');
            }
        }
        private static UnicodeBlock _cherokee;

        /// <summary>
        /// Represents the 'Unified Canadian Aboriginal Syllabics' Unicode block (U+1400..U+167F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1400.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock UnifiedCanadianAboriginalSyllabics
        {
            get
            {
                return Volatile.Read(ref _unifiedCanadianAboriginalSyllabics) ?? CreateBlock(ref _unifiedCanadianAboriginalSyllabics, first: '\u1400', last: '\u167F');
            }
        }
        private static UnicodeBlock _unifiedCanadianAboriginalSyllabics;

        /// <summary>
        /// Represents the 'Ogham' Unicode block (U+1680..U+169F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1680.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Ogham
        {
            get
            {
                return Volatile.Read(ref _ogham) ?? CreateBlock(ref _ogham, first: '\u1680', last: '\u169F');
            }
        }
        private static UnicodeBlock _ogham;

        /// <summary>
        /// Represents the 'Runic' Unicode block (U+16A0..U+16FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U16A0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Runic
        {
            get
            {
                return Volatile.Read(ref _runic) ?? CreateBlock(ref _runic, first: '\u16A0', last: '\u16FF');
            }
        }
        private static UnicodeBlock _runic;

        /// <summary>
        /// Represents the 'Tagalog' Unicode block (U+1700..U+171F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1700.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Tagalog
        {
            get
            {
                return Volatile.Read(ref _tagalog) ?? CreateBlock(ref _tagalog, first: '\u1700', last: '\u171F');
            }
        }
        private static UnicodeBlock _tagalog;

        /// <summary>
        /// Represents the 'Hanunoo' Unicode block (U+1720..U+173F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1720.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Hanunoo
        {
            get
            {
                return Volatile.Read(ref _hanunoo) ?? CreateBlock(ref _hanunoo, first: '\u1720', last: '\u173F');
            }
        }
        private static UnicodeBlock _hanunoo;

        /// <summary>
        /// Represents the 'Buhid' Unicode block (U+1740..U+175F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1740.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Buhid
        {
            get
            {
                return Volatile.Read(ref _buhid) ?? CreateBlock(ref _buhid, first: '\u1740', last: '\u175F');
            }
        }
        private static UnicodeBlock _buhid;

        /// <summary>
        /// Represents the 'Tagbanwa' Unicode block (U+1760..U+177F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1760.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Tagbanwa
        {
            get
            {
                return Volatile.Read(ref _tagbanwa) ?? CreateBlock(ref _tagbanwa, first: '\u1760', last: '\u177F');
            }
        }
        private static UnicodeBlock _tagbanwa;

        /// <summary>
        /// Represents the 'Khmer' Unicode block (U+1780..U+17FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1780.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Khmer
        {
            get
            {
                return Volatile.Read(ref _khmer) ?? CreateBlock(ref _khmer, first: '\u1780', last: '\u17FF');
            }
        }
        private static UnicodeBlock _khmer;

        /// <summary>
        /// Represents the 'Mongolian' Unicode block (U+1800..U+18AF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1800.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Mongolian
        {
            get
            {
                return Volatile.Read(ref _mongolian) ?? CreateBlock(ref _mongolian, first: '\u1800', last: '\u18AF');
            }
        }
        private static UnicodeBlock _mongolian;

        /// <summary>
        /// Represents the 'Unified Canadian Aboriginal Syllabics Extended' Unicode block (U+18B0..U+18FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U18B0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock UnifiedCanadianAboriginalSyllabicsExtended
        {
            get
            {
                return Volatile.Read(ref _unifiedCanadianAboriginalSyllabicsExtended) ?? CreateBlock(ref _unifiedCanadianAboriginalSyllabicsExtended, first: '\u18B0', last: '\u18FF');
            }
        }
        private static UnicodeBlock _unifiedCanadianAboriginalSyllabicsExtended;

        /// <summary>
        /// Represents the 'Limbu' Unicode block (U+1900..U+194F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1900.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Limbu
        {
            get
            {
                return Volatile.Read(ref _limbu) ?? CreateBlock(ref _limbu, first: '\u1900', last: '\u194F');
            }
        }
        private static UnicodeBlock _limbu;

        /// <summary>
        /// Represents the 'Tai Le' Unicode block (U+1950..U+197F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1950.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock TaiLe
        {
            get
            {
                return Volatile.Read(ref _taiLe) ?? CreateBlock(ref _taiLe, first: '\u1950', last: '\u197F');
            }
        }
        private static UnicodeBlock _taiLe;

        /// <summary>
        /// Represents the 'New Tai Lue' Unicode block (U+1980..U+19DF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1980.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock NewTaiLue
        {
            get
            {
                return Volatile.Read(ref _newTaiLue) ?? CreateBlock(ref _newTaiLue, first: '\u1980', last: '\u19DF');
            }
        }
        private static UnicodeBlock _newTaiLue;

        /// <summary>
        /// Represents the 'Khmer Symbols' Unicode block (U+19E0..U+19FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U19E0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock KhmerSymbols
        {
            get
            {
                return Volatile.Read(ref _khmerSymbols) ?? CreateBlock(ref _khmerSymbols, first: '\u19E0', last: '\u19FF');
            }
        }
        private static UnicodeBlock _khmerSymbols;

        /// <summary>
        /// Represents the 'Buginese' Unicode block (U+1A00..U+1A1F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1A00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Buginese
        {
            get
            {
                return Volatile.Read(ref _buginese) ?? CreateBlock(ref _buginese, first: '\u1A00', last: '\u1A1F');
            }
        }
        private static UnicodeBlock _buginese;

        /// <summary>
        /// Represents the 'Tai Tham' Unicode block (U+1A20..U+1AAF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1A20.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock TaiTham
        {
            get
            {
                return Volatile.Read(ref _taiTham) ?? CreateBlock(ref _taiTham, first: '\u1A20', last: '\u1AAF');
            }
        }
        private static UnicodeBlock _taiTham;

        /// <summary>
        /// Represents the 'Combining Diacritical Marks Extended' Unicode block (U+1AB0..U+1AFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1AB0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock CombiningDiacriticalMarksExtended
        {
            get
            {
                return Volatile.Read(ref _combiningDiacriticalMarksExtended) ?? CreateBlock(ref _combiningDiacriticalMarksExtended, first: '\u1AB0', last: '\u1AFF');
            }
        }
        private static UnicodeBlock _combiningDiacriticalMarksExtended;

        /// <summary>
        /// Represents the 'Balinese' Unicode block (U+1B00..U+1B7F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1B00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Balinese
        {
            get
            {
                return Volatile.Read(ref _balinese) ?? CreateBlock(ref _balinese, first: '\u1B00', last: '\u1B7F');
            }
        }
        private static UnicodeBlock _balinese;

        /// <summary>
        /// Represents the 'Sundanese' Unicode block (U+1B80..U+1BBF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1B80.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Sundanese
        {
            get
            {
                return Volatile.Read(ref _sundanese) ?? CreateBlock(ref _sundanese, first: '\u1B80', last: '\u1BBF');
            }
        }
        private static UnicodeBlock _sundanese;

        /// <summary>
        /// Represents the 'Batak' Unicode block (U+1BC0..U+1BFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1BC0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Batak
        {
            get
            {
                return Volatile.Read(ref _batak) ?? CreateBlock(ref _batak, first: '\u1BC0', last: '\u1BFF');
            }
        }
        private static UnicodeBlock _batak;

        /// <summary>
        /// Represents the 'Lepcha' Unicode block (U+1C00..U+1C4F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1C00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Lepcha
        {
            get
            {
                return Volatile.Read(ref _lepcha) ?? CreateBlock(ref _lepcha, first: '\u1C00', last: '\u1C4F');
            }
        }
        private static UnicodeBlock _lepcha;

        /// <summary>
        /// Represents the 'Ol Chiki' Unicode block (U+1C50..U+1C7F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1C50.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock OlChiki
        {
            get
            {
                return Volatile.Read(ref _olChiki) ?? CreateBlock(ref _olChiki, first: '\u1C50', last: '\u1C7F');
            }
        }
        private static UnicodeBlock _olChiki;

        /// <summary>
        /// Represents the 'Sundanese Supplement' Unicode block (U+1CC0..U+1CCF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1CC0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock SundaneseSupplement
        {
            get
            {
                return Volatile.Read(ref _sundaneseSupplement) ?? CreateBlock(ref _sundaneseSupplement, first: '\u1CC0', last: '\u1CCF');
            }
        }
        private static UnicodeBlock _sundaneseSupplement;

        /// <summary>
        /// Represents the 'Vedic Extensions' Unicode block (U+1CD0..U+1CFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1CD0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock VedicExtensions
        {
            get
            {
                return Volatile.Read(ref _vedicExtensions) ?? CreateBlock(ref _vedicExtensions, first: '\u1CD0', last: '\u1CFF');
            }
        }
        private static UnicodeBlock _vedicExtensions;

        /// <summary>
        /// Represents the 'Phonetic Extensions' Unicode block (U+1D00..U+1D7F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1D00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock PhoneticExtensions
        {
            get
            {
                return Volatile.Read(ref _phoneticExtensions) ?? CreateBlock(ref _phoneticExtensions, first: '\u1D00', last: '\u1D7F');
            }
        }
        private static UnicodeBlock _phoneticExtensions;

        /// <summary>
        /// Represents the 'Phonetic Extensions Supplement' Unicode block (U+1D80..U+1DBF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1D80.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock PhoneticExtensionsSupplement
        {
            get
            {
                return Volatile.Read(ref _phoneticExtensionsSupplement) ?? CreateBlock(ref _phoneticExtensionsSupplement, first: '\u1D80', last: '\u1DBF');
            }
        }
        private static UnicodeBlock _phoneticExtensionsSupplement;

        /// <summary>
        /// Represents the 'Combining Diacritical Marks Supplement' Unicode block (U+1DC0..U+1DFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1DC0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock CombiningDiacriticalMarksSupplement
        {
            get
            {
                return Volatile.Read(ref _combiningDiacriticalMarksSupplement) ?? CreateBlock(ref _combiningDiacriticalMarksSupplement, first: '\u1DC0', last: '\u1DFF');
            }
        }
        private static UnicodeBlock _combiningDiacriticalMarksSupplement;

        /// <summary>
        /// Represents the 'Latin Extended Additional' Unicode block (U+1E00..U+1EFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1E00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock LatinExtendedAdditional
        {
            get
            {
                return Volatile.Read(ref _latinExtendedAdditional) ?? CreateBlock(ref _latinExtendedAdditional, first: '\u1E00', last: '\u1EFF');
            }
        }
        private static UnicodeBlock _latinExtendedAdditional;

        /// <summary>
        /// Represents the 'Greek Extended' Unicode block (U+1F00..U+1FFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1F00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock GreekExtended
        {
            get
            {
                return Volatile.Read(ref _greekExtended) ?? CreateBlock(ref _greekExtended, first: '\u1F00', last: '\u1FFF');
            }
        }
        private static UnicodeBlock _greekExtended;

        /// <summary>
        /// Represents the 'General Punctuation' Unicode block (U+2000..U+206F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2000.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock GeneralPunctuation
        {
            get
            {
                return Volatile.Read(ref _generalPunctuation) ?? CreateBlock(ref _generalPunctuation, first: '\u2000', last: '\u206F');
            }
        }
        private static UnicodeBlock _generalPunctuation;

        /// <summary>
        /// Represents the 'Superscripts and Subscripts' Unicode block (U+2070..U+209F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2070.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock SuperscriptsandSubscripts
        {
            get
            {
                return Volatile.Read(ref _superscriptsandSubscripts) ?? CreateBlock(ref _superscriptsandSubscripts, first: '\u2070', last: '\u209F');
            }
        }
        private static UnicodeBlock _superscriptsandSubscripts;

        /// <summary>
        /// Represents the 'Currency Symbols' Unicode block (U+20A0..U+20CF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U20A0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock CurrencySymbols
        {
            get
            {
                return Volatile.Read(ref _currencySymbols) ?? CreateBlock(ref _currencySymbols, first: '\u20A0', last: '\u20CF');
            }
        }
        private static UnicodeBlock _currencySymbols;

        /// <summary>
        /// Represents the 'Combining Diacritical Marks for Symbols' Unicode block (U+20D0..U+20FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U20D0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock CombiningDiacriticalMarksforSymbols
        {
            get
            {
                return Volatile.Read(ref _combiningDiacriticalMarksforSymbols) ?? CreateBlock(ref _combiningDiacriticalMarksforSymbols, first: '\u20D0', last: '\u20FF');
            }
        }
        private static UnicodeBlock _combiningDiacriticalMarksforSymbols;

        /// <summary>
        /// Represents the 'Letterlike Symbols' Unicode block (U+2100..U+214F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2100.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock LetterlikeSymbols
        {
            get
            {
                return Volatile.Read(ref _letterlikeSymbols) ?? CreateBlock(ref _letterlikeSymbols, first: '\u2100', last: '\u214F');
            }
        }
        private static UnicodeBlock _letterlikeSymbols;

        /// <summary>
        /// Represents the 'Number Forms' Unicode block (U+2150..U+218F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2150.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock NumberForms
        {
            get
            {
                return Volatile.Read(ref _numberForms) ?? CreateBlock(ref _numberForms, first: '\u2150', last: '\u218F');
            }
        }
        private static UnicodeBlock _numberForms;

        /// <summary>
        /// Represents the 'Arrows' Unicode block (U+2190..U+21FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2190.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Arrows
        {
            get
            {
                return Volatile.Read(ref _arrows) ?? CreateBlock(ref _arrows, first: '\u2190', last: '\u21FF');
            }
        }
        private static UnicodeBlock _arrows;

        /// <summary>
        /// Represents the 'Mathematical Operators' Unicode block (U+2200..U+22FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2200.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock MathematicalOperators
        {
            get
            {
                return Volatile.Read(ref _mathematicalOperators) ?? CreateBlock(ref _mathematicalOperators, first: '\u2200', last: '\u22FF');
            }
        }
        private static UnicodeBlock _mathematicalOperators;

        /// <summary>
        /// Represents the 'Miscellaneous Technical' Unicode block (U+2300..U+23FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2300.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock MiscellaneousTechnical
        {
            get
            {
                return Volatile.Read(ref _miscellaneousTechnical) ?? CreateBlock(ref _miscellaneousTechnical, first: '\u2300', last: '\u23FF');
            }
        }
        private static UnicodeBlock _miscellaneousTechnical;

        /// <summary>
        /// Represents the 'Control Pictures' Unicode block (U+2400..U+243F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2400.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock ControlPictures
        {
            get
            {
                return Volatile.Read(ref _controlPictures) ?? CreateBlock(ref _controlPictures, first: '\u2400', last: '\u243F');
            }
        }
        private static UnicodeBlock _controlPictures;

        /// <summary>
        /// Represents the 'Optical Character Recognition' Unicode block (U+2440..U+245F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2440.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock OpticalCharacterRecognition
        {
            get
            {
                return Volatile.Read(ref _opticalCharacterRecognition) ?? CreateBlock(ref _opticalCharacterRecognition, first: '\u2440', last: '\u245F');
            }
        }
        private static UnicodeBlock _opticalCharacterRecognition;

        /// <summary>
        /// Represents the 'Enclosed Alphanumerics' Unicode block (U+2460..U+24FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2460.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock EnclosedAlphanumerics
        {
            get
            {
                return Volatile.Read(ref _enclosedAlphanumerics) ?? CreateBlock(ref _enclosedAlphanumerics, first: '\u2460', last: '\u24FF');
            }
        }
        private static UnicodeBlock _enclosedAlphanumerics;

        /// <summary>
        /// Represents the 'Box Drawing' Unicode block (U+2500..U+257F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2500.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock BoxDrawing
        {
            get
            {
                return Volatile.Read(ref _boxDrawing) ?? CreateBlock(ref _boxDrawing, first: '\u2500', last: '\u257F');
            }
        }
        private static UnicodeBlock _boxDrawing;

        /// <summary>
        /// Represents the 'Block Elements' Unicode block (U+2580..U+259F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2580.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock BlockElements
        {
            get
            {
                return Volatile.Read(ref _blockElements) ?? CreateBlock(ref _blockElements, first: '\u2580', last: '\u259F');
            }
        }
        private static UnicodeBlock _blockElements;

        /// <summary>
        /// Represents the 'Geometric Shapes' Unicode block (U+25A0..U+25FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U25A0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock GeometricShapes
        {
            get
            {
                return Volatile.Read(ref _geometricShapes) ?? CreateBlock(ref _geometricShapes, first: '\u25A0', last: '\u25FF');
            }
        }
        private static UnicodeBlock _geometricShapes;

        /// <summary>
        /// Represents the 'Miscellaneous Symbols' Unicode block (U+2600..U+26FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2600.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock MiscellaneousSymbols
        {
            get
            {
                return Volatile.Read(ref _miscellaneousSymbols) ?? CreateBlock(ref _miscellaneousSymbols, first: '\u2600', last: '\u26FF');
            }
        }
        private static UnicodeBlock _miscellaneousSymbols;

        /// <summary>
        /// Represents the 'Dingbats' Unicode block (U+2700..U+27BF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2700.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Dingbats
        {
            get
            {
                return Volatile.Read(ref _dingbats) ?? CreateBlock(ref _dingbats, first: '\u2700', last: '\u27BF');
            }
        }
        private static UnicodeBlock _dingbats;

        /// <summary>
        /// Represents the 'Miscellaneous Mathematical Symbols-A' Unicode block (U+27C0..U+27EF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U27C0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock MiscellaneousMathematicalSymbolsA
        {
            get
            {
                return Volatile.Read(ref _miscellaneousMathematicalSymbolsA) ?? CreateBlock(ref _miscellaneousMathematicalSymbolsA, first: '\u27C0', last: '\u27EF');
            }
        }
        private static UnicodeBlock _miscellaneousMathematicalSymbolsA;

        /// <summary>
        /// Represents the 'Supplemental Arrows-A' Unicode block (U+27F0..U+27FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U27F0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock SupplementalArrowsA
        {
            get
            {
                return Volatile.Read(ref _supplementalArrowsA) ?? CreateBlock(ref _supplementalArrowsA, first: '\u27F0', last: '\u27FF');
            }
        }
        private static UnicodeBlock _supplementalArrowsA;

        /// <summary>
        /// Represents the 'Braille Patterns' Unicode block (U+2800..U+28FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2800.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock BraillePatterns
        {
            get
            {
                return Volatile.Read(ref _braillePatterns) ?? CreateBlock(ref _braillePatterns, first: '\u2800', last: '\u28FF');
            }
        }
        private static UnicodeBlock _braillePatterns;

        /// <summary>
        /// Represents the 'Supplemental Arrows-B' Unicode block (U+2900..U+297F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2900.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock SupplementalArrowsB
        {
            get
            {
                return Volatile.Read(ref _supplementalArrowsB) ?? CreateBlock(ref _supplementalArrowsB, first: '\u2900', last: '\u297F');
            }
        }
        private static UnicodeBlock _supplementalArrowsB;

        /// <summary>
        /// Represents the 'Miscellaneous Mathematical Symbols-B' Unicode block (U+2980..U+29FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2980.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock MiscellaneousMathematicalSymbolsB
        {
            get
            {
                return Volatile.Read(ref _miscellaneousMathematicalSymbolsB) ?? CreateBlock(ref _miscellaneousMathematicalSymbolsB, first: '\u2980', last: '\u29FF');
            }
        }
        private static UnicodeBlock _miscellaneousMathematicalSymbolsB;

        /// <summary>
        /// Represents the 'Supplemental Mathematical Operators' Unicode block (U+2A00..U+2AFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2A00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock SupplementalMathematicalOperators
        {
            get
            {
                return Volatile.Read(ref _supplementalMathematicalOperators) ?? CreateBlock(ref _supplementalMathematicalOperators, first: '\u2A00', last: '\u2AFF');
            }
        }
        private static UnicodeBlock _supplementalMathematicalOperators;

        /// <summary>
        /// Represents the 'Miscellaneous Symbols and Arrows' Unicode block (U+2B00..U+2BFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2B00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock MiscellaneousSymbolsandArrows
        {
            get
            {
                return Volatile.Read(ref _miscellaneousSymbolsandArrows) ?? CreateBlock(ref _miscellaneousSymbolsandArrows, first: '\u2B00', last: '\u2BFF');
            }
        }
        private static UnicodeBlock _miscellaneousSymbolsandArrows;

        /// <summary>
        /// Represents the 'Glagolitic' Unicode block (U+2C00..U+2C5F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2C00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Glagolitic
        {
            get
            {
                return Volatile.Read(ref _glagolitic) ?? CreateBlock(ref _glagolitic, first: '\u2C00', last: '\u2C5F');
            }
        }
        private static UnicodeBlock _glagolitic;

        /// <summary>
        /// Represents the 'Latin Extended-C' Unicode block (U+2C60..U+2C7F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2C60.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock LatinExtendedC
        {
            get
            {
                return Volatile.Read(ref _latinExtendedC) ?? CreateBlock(ref _latinExtendedC, first: '\u2C60', last: '\u2C7F');
            }
        }
        private static UnicodeBlock _latinExtendedC;

        /// <summary>
        /// Represents the 'Coptic' Unicode block (U+2C80..U+2CFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2C80.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Coptic
        {
            get
            {
                return Volatile.Read(ref _coptic) ?? CreateBlock(ref _coptic, first: '\u2C80', last: '\u2CFF');
            }
        }
        private static UnicodeBlock _coptic;

        /// <summary>
        /// Represents the 'Georgian Supplement' Unicode block (U+2D00..U+2D2F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2D00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock GeorgianSupplement
        {
            get
            {
                return Volatile.Read(ref _georgianSupplement) ?? CreateBlock(ref _georgianSupplement, first: '\u2D00', last: '\u2D2F');
            }
        }
        private static UnicodeBlock _georgianSupplement;

        /// <summary>
        /// Represents the 'Tifinagh' Unicode block (U+2D30..U+2D7F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2D30.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Tifinagh
        {
            get
            {
                return Volatile.Read(ref _tifinagh) ?? CreateBlock(ref _tifinagh, first: '\u2D30', last: '\u2D7F');
            }
        }
        private static UnicodeBlock _tifinagh;

        /// <summary>
        /// Represents the 'Ethiopic Extended' Unicode block (U+2D80..U+2DDF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2D80.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock EthiopicExtended
        {
            get
            {
                return Volatile.Read(ref _ethiopicExtended) ?? CreateBlock(ref _ethiopicExtended, first: '\u2D80', last: '\u2DDF');
            }
        }
        private static UnicodeBlock _ethiopicExtended;

        /// <summary>
        /// Represents the 'Cyrillic Extended-A' Unicode block (U+2DE0..U+2DFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2DE0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock CyrillicExtendedA
        {
            get
            {
                return Volatile.Read(ref _cyrillicExtendedA) ?? CreateBlock(ref _cyrillicExtendedA, first: '\u2DE0', last: '\u2DFF');
            }
        }
        private static UnicodeBlock _cyrillicExtendedA;

        /// <summary>
        /// Represents the 'Supplemental Punctuation' Unicode block (U+2E00..U+2E7F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2E00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock SupplementalPunctuation
        {
            get
            {
                return Volatile.Read(ref _supplementalPunctuation) ?? CreateBlock(ref _supplementalPunctuation, first: '\u2E00', last: '\u2E7F');
            }
        }
        private static UnicodeBlock _supplementalPunctuation;

        /// <summary>
        /// Represents the 'CJK Radicals Supplement' Unicode block (U+2E80..U+2EFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2E80.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock CJKRadicalsSupplement
        {
            get
            {
                return Volatile.Read(ref _cjkRadicalsSupplement) ?? CreateBlock(ref _cjkRadicalsSupplement, first: '\u2E80', last: '\u2EFF');
            }
        }
        private static UnicodeBlock _cjkRadicalsSupplement;

        /// <summary>
        /// Represents the 'Kangxi Radicals' Unicode block (U+2F00..U+2FDF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2F00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock KangxiRadicals
        {
            get
            {
                return Volatile.Read(ref _kangxiRadicals) ?? CreateBlock(ref _kangxiRadicals, first: '\u2F00', last: '\u2FDF');
            }
        }
        private static UnicodeBlock _kangxiRadicals;

        /// <summary>
        /// Represents the 'Ideographic Description Characters' Unicode block (U+2FF0..U+2FFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2FF0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock IdeographicDescriptionCharacters
        {
            get
            {
                return Volatile.Read(ref _ideographicDescriptionCharacters) ?? CreateBlock(ref _ideographicDescriptionCharacters, first: '\u2FF0', last: '\u2FFF');
            }
        }
        private static UnicodeBlock _ideographicDescriptionCharacters;

        /// <summary>
        /// Represents the 'CJK Symbols and Punctuation' Unicode block (U+3000..U+303F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U3000.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock CJKSymbolsandPunctuation
        {
            get
            {
                return Volatile.Read(ref _cjkSymbolsandPunctuation) ?? CreateBlock(ref _cjkSymbolsandPunctuation, first: '\u3000', last: '\u303F');
            }
        }
        private static UnicodeBlock _cjkSymbolsandPunctuation;

        /// <summary>
        /// Represents the 'Hiragana' Unicode block (U+3040..U+309F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U3040.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Hiragana
        {
            get
            {
                return Volatile.Read(ref _hiragana) ?? CreateBlock(ref _hiragana, first: '\u3040', last: '\u309F');
            }
        }
        private static UnicodeBlock _hiragana;

        /// <summary>
        /// Represents the 'Katakana' Unicode block (U+30A0..U+30FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U30A0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Katakana
        {
            get
            {
                return Volatile.Read(ref _katakana) ?? CreateBlock(ref _katakana, first: '\u30A0', last: '\u30FF');
            }
        }
        private static UnicodeBlock _katakana;

        /// <summary>
        /// Represents the 'Bopomofo' Unicode block (U+3100..U+312F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U3100.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Bopomofo
        {
            get
            {
                return Volatile.Read(ref _bopomofo) ?? CreateBlock(ref _bopomofo, first: '\u3100', last: '\u312F');
            }
        }
        private static UnicodeBlock _bopomofo;

        /// <summary>
        /// Represents the 'Hangul Compatibility Jamo' Unicode block (U+3130..U+318F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U3130.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock HangulCompatibilityJamo
        {
            get
            {
                return Volatile.Read(ref _hangulCompatibilityJamo) ?? CreateBlock(ref _hangulCompatibilityJamo, first: '\u3130', last: '\u318F');
            }
        }
        private static UnicodeBlock _hangulCompatibilityJamo;

        /// <summary>
        /// Represents the 'Kanbun' Unicode block (U+3190..U+319F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U3190.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Kanbun
        {
            get
            {
                return Volatile.Read(ref _kanbun) ?? CreateBlock(ref _kanbun, first: '\u3190', last: '\u319F');
            }
        }
        private static UnicodeBlock _kanbun;

        /// <summary>
        /// Represents the 'Bopomofo Extended' Unicode block (U+31A0..U+31BF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U31A0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock BopomofoExtended
        {
            get
            {
                return Volatile.Read(ref _bopomofoExtended) ?? CreateBlock(ref _bopomofoExtended, first: '\u31A0', last: '\u31BF');
            }
        }
        private static UnicodeBlock _bopomofoExtended;

        /// <summary>
        /// Represents the 'CJK Strokes' Unicode block (U+31C0..U+31EF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U31C0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock CJKStrokes
        {
            get
            {
                return Volatile.Read(ref _cjkStrokes) ?? CreateBlock(ref _cjkStrokes, first: '\u31C0', last: '\u31EF');
            }
        }
        private static UnicodeBlock _cjkStrokes;

        /// <summary>
        /// Represents the 'Katakana Phonetic Extensions' Unicode block (U+31F0..U+31FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U31F0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock KatakanaPhoneticExtensions
        {
            get
            {
                return Volatile.Read(ref _katakanaPhoneticExtensions) ?? CreateBlock(ref _katakanaPhoneticExtensions, first: '\u31F0', last: '\u31FF');
            }
        }
        private static UnicodeBlock _katakanaPhoneticExtensions;

        /// <summary>
        /// Represents the 'Enclosed CJK Letters and Months' Unicode block (U+3200..U+32FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U3200.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock EnclosedCJKLettersandMonths
        {
            get
            {
                return Volatile.Read(ref _enclosedCJKLettersandMonths) ?? CreateBlock(ref _enclosedCJKLettersandMonths, first: '\u3200', last: '\u32FF');
            }
        }
        private static UnicodeBlock _enclosedCJKLettersandMonths;

        /// <summary>
        /// Represents the 'CJK Compatibility' Unicode block (U+3300..U+33FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U3300.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock CJKCompatibility
        {
            get
            {
                return Volatile.Read(ref _cjkCompatibility) ?? CreateBlock(ref _cjkCompatibility, first: '\u3300', last: '\u33FF');
            }
        }
        private static UnicodeBlock _cjkCompatibility;

        /// <summary>
        /// Represents the 'CJK Unified Ideographs Extension A' Unicode block (U+3400..U+4DBF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U3400.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock CJKUnifiedIdeographsExtensionA
        {
            get
            {
                return Volatile.Read(ref _cjkUnifiedIdeographsExtensionA) ?? CreateBlock(ref _cjkUnifiedIdeographsExtensionA, first: '\u3400', last: '\u4DBF');
            }
        }
        private static UnicodeBlock _cjkUnifiedIdeographsExtensionA;

        /// <summary>
        /// Represents the 'Yijing Hexagram Symbols' Unicode block (U+4DC0..U+4DFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U4DC0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock YijingHexagramSymbols
        {
            get
            {
                return Volatile.Read(ref _yijingHexagramSymbols) ?? CreateBlock(ref _yijingHexagramSymbols, first: '\u4DC0', last: '\u4DFF');
            }
        }
        private static UnicodeBlock _yijingHexagramSymbols;

        /// <summary>
        /// Represents the 'CJK Unified Ideographs' Unicode block (U+4E00..U+9FFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U4E00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock CJKUnifiedIdeographs
        {
            get
            {
                return Volatile.Read(ref _cjkUnifiedIdeographs) ?? CreateBlock(ref _cjkUnifiedIdeographs, first: '\u4E00', last: '\u9FFF');
            }
        }
        private static UnicodeBlock _cjkUnifiedIdeographs;

        /// <summary>
        /// Represents the 'Yi Syllables' Unicode block (U+A000..U+A48F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA000.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock YiSyllables
        {
            get
            {
                return Volatile.Read(ref _yiSyllables) ?? CreateBlock(ref _yiSyllables, first: '\uA000', last: '\uA48F');
            }
        }
        private static UnicodeBlock _yiSyllables;

        /// <summary>
        /// Represents the 'Yi Radicals' Unicode block (U+A490..U+A4CF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA490.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock YiRadicals
        {
            get
            {
                return Volatile.Read(ref _yiRadicals) ?? CreateBlock(ref _yiRadicals, first: '\uA490', last: '\uA4CF');
            }
        }
        private static UnicodeBlock _yiRadicals;

        /// <summary>
        /// Represents the 'Lisu' Unicode block (U+A4D0..U+A4FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA4D0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Lisu
        {
            get
            {
                return Volatile.Read(ref _lisu) ?? CreateBlock(ref _lisu, first: '\uA4D0', last: '\uA4FF');
            }
        }
        private static UnicodeBlock _lisu;

        /// <summary>
        /// Represents the 'Vai' Unicode block (U+A500..U+A63F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA500.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Vai
        {
            get
            {
                return Volatile.Read(ref _vai) ?? CreateBlock(ref _vai, first: '\uA500', last: '\uA63F');
            }
        }
        private static UnicodeBlock _vai;

        /// <summary>
        /// Represents the 'Cyrillic Extended-B' Unicode block (U+A640..U+A69F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA640.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock CyrillicExtendedB
        {
            get
            {
                return Volatile.Read(ref _cyrillicExtendedB) ?? CreateBlock(ref _cyrillicExtendedB, first: '\uA640', last: '\uA69F');
            }
        }
        private static UnicodeBlock _cyrillicExtendedB;

        /// <summary>
        /// Represents the 'Bamum' Unicode block (U+A6A0..U+A6FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA6A0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Bamum
        {
            get
            {
                return Volatile.Read(ref _bamum) ?? CreateBlock(ref _bamum, first: '\uA6A0', last: '\uA6FF');
            }
        }
        private static UnicodeBlock _bamum;

        /// <summary>
        /// Represents the 'Modifier Tone Letters' Unicode block (U+A700..U+A71F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA700.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock ModifierToneLetters
        {
            get
            {
                return Volatile.Read(ref _modifierToneLetters) ?? CreateBlock(ref _modifierToneLetters, first: '\uA700', last: '\uA71F');
            }
        }
        private static UnicodeBlock _modifierToneLetters;

        /// <summary>
        /// Represents the 'Latin Extended-D' Unicode block (U+A720..U+A7FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA720.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock LatinExtendedD
        {
            get
            {
                return Volatile.Read(ref _latinExtendedD) ?? CreateBlock(ref _latinExtendedD, first: '\uA720', last: '\uA7FF');
            }
        }
        private static UnicodeBlock _latinExtendedD;

        /// <summary>
        /// Represents the 'Syloti Nagri' Unicode block (U+A800..U+A82F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA800.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock SylotiNagri
        {
            get
            {
                return Volatile.Read(ref _sylotiNagri) ?? CreateBlock(ref _sylotiNagri, first: '\uA800', last: '\uA82F');
            }
        }
        private static UnicodeBlock _sylotiNagri;

        /// <summary>
        /// Represents the 'Common Indic Number Forms' Unicode block (U+A830..U+A83F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA830.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock CommonIndicNumberForms
        {
            get
            {
                return Volatile.Read(ref _commonIndicNumberForms) ?? CreateBlock(ref _commonIndicNumberForms, first: '\uA830', last: '\uA83F');
            }
        }
        private static UnicodeBlock _commonIndicNumberForms;

        /// <summary>
        /// Represents the 'Phags-pa' Unicode block (U+A840..U+A87F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA840.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Phagspa
        {
            get
            {
                return Volatile.Read(ref _phagspa) ?? CreateBlock(ref _phagspa, first: '\uA840', last: '\uA87F');
            }
        }
        private static UnicodeBlock _phagspa;

        /// <summary>
        /// Represents the 'Saurashtra' Unicode block (U+A880..U+A8DF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA880.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Saurashtra
        {
            get
            {
                return Volatile.Read(ref _saurashtra) ?? CreateBlock(ref _saurashtra, first: '\uA880', last: '\uA8DF');
            }
        }
        private static UnicodeBlock _saurashtra;

        /// <summary>
        /// Represents the 'Devanagari Extended' Unicode block (U+A8E0..U+A8FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA8E0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock DevanagariExtended
        {
            get
            {
                return Volatile.Read(ref _devanagariExtended) ?? CreateBlock(ref _devanagariExtended, first: '\uA8E0', last: '\uA8FF');
            }
        }
        private static UnicodeBlock _devanagariExtended;

        /// <summary>
        /// Represents the 'Kayah Li' Unicode block (U+A900..U+A92F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA900.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock KayahLi
        {
            get
            {
                return Volatile.Read(ref _kayahLi) ?? CreateBlock(ref _kayahLi, first: '\uA900', last: '\uA92F');
            }
        }
        private static UnicodeBlock _kayahLi;

        /// <summary>
        /// Represents the 'Rejang' Unicode block (U+A930..U+A95F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA930.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Rejang
        {
            get
            {
                return Volatile.Read(ref _rejang) ?? CreateBlock(ref _rejang, first: '\uA930', last: '\uA95F');
            }
        }
        private static UnicodeBlock _rejang;

        /// <summary>
        /// Represents the 'Hangul Jamo Extended-A' Unicode block (U+A960..U+A97F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA960.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock HangulJamoExtendedA
        {
            get
            {
                return Volatile.Read(ref _hangulJamoExtendedA) ?? CreateBlock(ref _hangulJamoExtendedA, first: '\uA960', last: '\uA97F');
            }
        }
        private static UnicodeBlock _hangulJamoExtendedA;

        /// <summary>
        /// Represents the 'Javanese' Unicode block (U+A980..U+A9DF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA980.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Javanese
        {
            get
            {
                return Volatile.Read(ref _javanese) ?? CreateBlock(ref _javanese, first: '\uA980', last: '\uA9DF');
            }
        }
        private static UnicodeBlock _javanese;

        /// <summary>
        /// Represents the 'Myanmar Extended-B' Unicode block (U+A9E0..U+A9FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA9E0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock MyanmarExtendedB
        {
            get
            {
                return Volatile.Read(ref _myanmarExtendedB) ?? CreateBlock(ref _myanmarExtendedB, first: '\uA9E0', last: '\uA9FF');
            }
        }
        private static UnicodeBlock _myanmarExtendedB;

        /// <summary>
        /// Represents the 'Cham' Unicode block (U+AA00..U+AA5F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UAA00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Cham
        {
            get
            {
                return Volatile.Read(ref _cham) ?? CreateBlock(ref _cham, first: '\uAA00', last: '\uAA5F');
            }
        }
        private static UnicodeBlock _cham;

        /// <summary>
        /// Represents the 'Myanmar Extended-A' Unicode block (U+AA60..U+AA7F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UAA60.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock MyanmarExtendedA
        {
            get
            {
                return Volatile.Read(ref _myanmarExtendedA) ?? CreateBlock(ref _myanmarExtendedA, first: '\uAA60', last: '\uAA7F');
            }
        }
        private static UnicodeBlock _myanmarExtendedA;

        /// <summary>
        /// Represents the 'Tai Viet' Unicode block (U+AA80..U+AADF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UAA80.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock TaiViet
        {
            get
            {
                return Volatile.Read(ref _taiViet) ?? CreateBlock(ref _taiViet, first: '\uAA80', last: '\uAADF');
            }
        }
        private static UnicodeBlock _taiViet;

        /// <summary>
        /// Represents the 'Meetei Mayek Extensions' Unicode block (U+AAE0..U+AAFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UAAE0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock MeeteiMayekExtensions
        {
            get
            {
                return Volatile.Read(ref _meeteiMayekExtensions) ?? CreateBlock(ref _meeteiMayekExtensions, first: '\uAAE0', last: '\uAAFF');
            }
        }
        private static UnicodeBlock _meeteiMayekExtensions;

        /// <summary>
        /// Represents the 'Ethiopic Extended-A' Unicode block (U+AB00..U+AB2F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UAB00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock EthiopicExtendedA
        {
            get
            {
                return Volatile.Read(ref _ethiopicExtendedA) ?? CreateBlock(ref _ethiopicExtendedA, first: '\uAB00', last: '\uAB2F');
            }
        }
        private static UnicodeBlock _ethiopicExtendedA;

        /// <summary>
        /// Represents the 'Latin Extended-E' Unicode block (U+AB30..U+AB6F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UAB30.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock LatinExtendedE
        {
            get
            {
                return Volatile.Read(ref _latinExtendedE) ?? CreateBlock(ref _latinExtendedE, first: '\uAB30', last: '\uAB6F');
            }
        }
        private static UnicodeBlock _latinExtendedE;

        /// <summary>
        /// Represents the 'Meetei Mayek' Unicode block (U+ABC0..U+ABFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UABC0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock MeeteiMayek
        {
            get
            {
                return Volatile.Read(ref _meeteiMayek) ?? CreateBlock(ref _meeteiMayek, first: '\uABC0', last: '\uABFF');
            }
        }
        private static UnicodeBlock _meeteiMayek;

        /// <summary>
        /// Represents the 'Hangul Syllables' Unicode block (U+AC00..U+D7AF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UAC00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock HangulSyllables
        {
            get
            {
                return Volatile.Read(ref _hangulSyllables) ?? CreateBlock(ref _hangulSyllables, first: '\uAC00', last: '\uD7AF');
            }
        }
        private static UnicodeBlock _hangulSyllables;

        /// <summary>
        /// Represents the 'Hangul Jamo Extended-B' Unicode block (U+D7B0..U+D7FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UD7B0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock HangulJamoExtendedB
        {
            get
            {
                return Volatile.Read(ref _hangulJamoExtendedB) ?? CreateBlock(ref _hangulJamoExtendedB, first: '\uD7B0', last: '\uD7FF');
            }
        }
        private static UnicodeBlock _hangulJamoExtendedB;
        
        /// <summary>
        /// Represents the 'CJK Compatibility Ideographs' Unicode block (U+F900..U+FAFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UF900.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock CJKCompatibilityIdeographs
        {
            get
            {
                return Volatile.Read(ref _cjkCompatibilityIdeographs) ?? CreateBlock(ref _cjkCompatibilityIdeographs, first: '\uF900', last: '\uFAFF');
            }
        }
        private static UnicodeBlock _cjkCompatibilityIdeographs;

        /// <summary>
        /// Represents the 'Alphabetic Presentation Forms' Unicode block (U+FB00..U+FB4F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UFB00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock AlphabeticPresentationForms
        {
            get
            {
                return Volatile.Read(ref _alphabeticPresentationForms) ?? CreateBlock(ref _alphabeticPresentationForms, first: '\uFB00', last: '\uFB4F');
            }
        }
        private static UnicodeBlock _alphabeticPresentationForms;

        /// <summary>
        /// Represents the 'Arabic Presentation Forms-A' Unicode block (U+FB50..U+FDFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UFB50.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock ArabicPresentationFormsA
        {
            get
            {
                return Volatile.Read(ref _arabicPresentationFormsA) ?? CreateBlock(ref _arabicPresentationFormsA, first: '\uFB50', last: '\uFDFF');
            }
        }
        private static UnicodeBlock _arabicPresentationFormsA;

        /// <summary>
        /// Represents the 'Variation Selectors' Unicode block (U+FE00..U+FE0F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UFE00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock VariationSelectors
        {
            get
            {
                return Volatile.Read(ref _variationSelectors) ?? CreateBlock(ref _variationSelectors, first: '\uFE00', last: '\uFE0F');
            }
        }
        private static UnicodeBlock _variationSelectors;

        /// <summary>
        /// Represents the 'Vertical Forms' Unicode block (U+FE10..U+FE1F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UFE10.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock VerticalForms
        {
            get
            {
                return Volatile.Read(ref _verticalForms) ?? CreateBlock(ref _verticalForms, first: '\uFE10', last: '\uFE1F');
            }
        }
        private static UnicodeBlock _verticalForms;

        /// <summary>
        /// Represents the 'Combining Half Marks' Unicode block (U+FE20..U+FE2F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UFE20.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock CombiningHalfMarks
        {
            get
            {
                return Volatile.Read(ref _combiningHalfMarks) ?? CreateBlock(ref _combiningHalfMarks, first: '\uFE20', last: '\uFE2F');
            }
        }
        private static UnicodeBlock _combiningHalfMarks;

        /// <summary>
        /// Represents the 'CJK Compatibility Forms' Unicode block (U+FE30..U+FE4F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UFE30.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock CJKCompatibilityForms
        {
            get
            {
                return Volatile.Read(ref _cjkCompatibilityForms) ?? CreateBlock(ref _cjkCompatibilityForms, first: '\uFE30', last: '\uFE4F');
            }
        }
        private static UnicodeBlock _cjkCompatibilityForms;

        /// <summary>
        /// Represents the 'Small Form Variants' Unicode block (U+FE50..U+FE6F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UFE50.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock SmallFormVariants
        {
            get
            {
                return Volatile.Read(ref _smallFormVariants) ?? CreateBlock(ref _smallFormVariants, first: '\uFE50', last: '\uFE6F');
            }
        }
        private static UnicodeBlock _smallFormVariants;

        /// <summary>
        /// Represents the 'Arabic Presentation Forms-B' Unicode block (U+FE70..U+FEFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UFE70.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock ArabicPresentationFormsB
        {
            get
            {
                return Volatile.Read(ref _arabicPresentationFormsB) ?? CreateBlock(ref _arabicPresentationFormsB, first: '\uFE70', last: '\uFEFF');
            }
        }
        private static UnicodeBlock _arabicPresentationFormsB;

        /// <summary>
        /// Represents the 'Halfwidth and Fullwidth Forms' Unicode block (U+FF00..U+FFEF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UFF00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock HalfwidthandFullwidthForms
        {
            get
            {
                return Volatile.Read(ref _halfwidthandFullwidthForms) ?? CreateBlock(ref _halfwidthandFullwidthForms, first: '\uFF00', last: '\uFFEF');
            }
        }
        private static UnicodeBlock _halfwidthandFullwidthForms;

        /// <summary>
        /// Represents the 'Specials' Unicode block (U+FFF0..U+FFFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UFFF0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeBlock Specials
        {
            get
            {
                return Volatile.Read(ref _specials) ?? CreateBlock(ref _specials, first: '\uFFF0', last: '\uFFFF');
            }
        }
        private static UnicodeBlock _specials;
    }
}
