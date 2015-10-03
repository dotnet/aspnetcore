// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.Extensions.WebEncoders
{
    public static partial class UnicodeRanges
    {
        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Basic Latin' Unicode block (U+0000..U+007F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0000.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange BasicLatin => Volatile.Read(ref _basicLatin) ?? CreateRange(ref _basicLatin, first: '\u0000', last: '\u007F');
        private static UnicodeRange _basicLatin;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Latin-1 Supplement' Unicode block (U+0080..U+00FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0080.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Latin1Supplement => Volatile.Read(ref _latin1Supplement) ?? CreateRange(ref _latin1Supplement, first: '\u0080', last: '\u00FF');
        private static UnicodeRange _latin1Supplement;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Latin Extended-A' Unicode block (U+0100..U+017F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0100.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange LatinExtendedA => Volatile.Read(ref _latinExtendedA) ?? CreateRange(ref _latinExtendedA, first: '\u0100', last: '\u017F');
        private static UnicodeRange _latinExtendedA;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Latin Extended-B' Unicode block (U+0180..U+024F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0180.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange LatinExtendedB => Volatile.Read(ref _latinExtendedB) ?? CreateRange(ref _latinExtendedB, first: '\u0180', last: '\u024F');
        private static UnicodeRange _latinExtendedB;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'IPA Extensions' Unicode block (U+0250..U+02AF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0250.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange IPAExtensions => Volatile.Read(ref _ipaExtensions) ?? CreateRange(ref _ipaExtensions, first: '\u0250', last: '\u02AF');
        private static UnicodeRange _ipaExtensions;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Spacing Modifier Letters' Unicode block (U+02B0..U+02FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U02B0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange SpacingModifierLetters => Volatile.Read(ref _spacingModifierLetters) ?? CreateRange(ref _spacingModifierLetters, first: '\u02B0', last: '\u02FF');
        private static UnicodeRange _spacingModifierLetters;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Combining Diacritical Marks' Unicode block (U+0300..U+036F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0300.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange CombiningDiacriticalMarks => Volatile.Read(ref _combiningDiacriticalMarks) ?? CreateRange(ref _combiningDiacriticalMarks, first: '\u0300', last: '\u036F');
        private static UnicodeRange _combiningDiacriticalMarks;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Greek and Coptic' Unicode block (U+0370..U+03FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0370.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange GreekandCoptic => Volatile.Read(ref _greekandCoptic) ?? CreateRange(ref _greekandCoptic, first: '\u0370', last: '\u03FF');
        private static UnicodeRange _greekandCoptic;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Cyrillic' Unicode block (U+0400..U+04FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0400.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Cyrillic => Volatile.Read(ref _cyrillic) ?? CreateRange(ref _cyrillic, first: '\u0400', last: '\u04FF');
        private static UnicodeRange _cyrillic;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Cyrillic Supplement' Unicode block (U+0500..U+052F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0500.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange CyrillicSupplement => Volatile.Read(ref _cyrillicSupplement) ?? CreateRange(ref _cyrillicSupplement, first: '\u0500', last: '\u052F');
        private static UnicodeRange _cyrillicSupplement;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Armenian' Unicode block (U+0530..U+058F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0530.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Armenian => Volatile.Read(ref _armenian) ?? CreateRange(ref _armenian, first: '\u0530', last: '\u058F');
        private static UnicodeRange _armenian;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Hebrew' Unicode block (U+0590..U+05FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0590.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Hebrew => Volatile.Read(ref _hebrew) ?? CreateRange(ref _hebrew, first: '\u0590', last: '\u05FF');
        private static UnicodeRange _hebrew;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Arabic' Unicode block (U+0600..U+06FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0600.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Arabic => Volatile.Read(ref _arabic) ?? CreateRange(ref _arabic, first: '\u0600', last: '\u06FF');
        private static UnicodeRange _arabic;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Syriac' Unicode block (U+0700..U+074F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0700.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Syriac => Volatile.Read(ref _syriac) ?? CreateRange(ref _syriac, first: '\u0700', last: '\u074F');
        private static UnicodeRange _syriac;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Arabic Supplement' Unicode block (U+0750..U+077F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0750.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange ArabicSupplement => Volatile.Read(ref _arabicSupplement) ?? CreateRange(ref _arabicSupplement, first: '\u0750', last: '\u077F');
        private static UnicodeRange _arabicSupplement;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Thaana' Unicode block (U+0780..U+07BF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0780.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Thaana => Volatile.Read(ref _thaana) ?? CreateRange(ref _thaana, first: '\u0780', last: '\u07BF');
        private static UnicodeRange _thaana;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'NKo' Unicode block (U+07C0..U+07FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U07C0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange NKo => Volatile.Read(ref _nKo) ?? CreateRange(ref _nKo, first: '\u07C0', last: '\u07FF');
        private static UnicodeRange _nKo;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Samaritan' Unicode block (U+0800..U+083F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0800.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Samaritan => Volatile.Read(ref _samaritan) ?? CreateRange(ref _samaritan, first: '\u0800', last: '\u083F');
        private static UnicodeRange _samaritan;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Mandaic' Unicode block (U+0840..U+085F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0840.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Mandaic => Volatile.Read(ref _mandaic) ?? CreateRange(ref _mandaic, first: '\u0840', last: '\u085F');
        private static UnicodeRange _mandaic;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Arabic Extended-A' Unicode block (U+08A0..U+08FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U08A0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange ArabicExtendedA => Volatile.Read(ref _arabicExtendedA) ?? CreateRange(ref _arabicExtendedA, first: '\u08A0', last: '\u08FF');
        private static UnicodeRange _arabicExtendedA;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Devanagari' Unicode block (U+0900..U+097F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0900.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Devanagari => Volatile.Read(ref _devanagari) ?? CreateRange(ref _devanagari, first: '\u0900', last: '\u097F');
        private static UnicodeRange _devanagari;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Bengali' Unicode block (U+0980..U+09FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0980.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Bengali => Volatile.Read(ref _bengali) ?? CreateRange(ref _bengali, first: '\u0980', last: '\u09FF');
        private static UnicodeRange _bengali;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Gurmukhi' Unicode block (U+0A00..U+0A7F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0A00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Gurmukhi => Volatile.Read(ref _gurmukhi) ?? CreateRange(ref _gurmukhi, first: '\u0A00', last: '\u0A7F');
        private static UnicodeRange _gurmukhi;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Gujarati' Unicode block (U+0A80..U+0AFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0A80.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Gujarati => Volatile.Read(ref _gujarati) ?? CreateRange(ref _gujarati, first: '\u0A80', last: '\u0AFF');
        private static UnicodeRange _gujarati;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Oriya' Unicode block (U+0B00..U+0B7F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0B00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Oriya => Volatile.Read(ref _oriya) ?? CreateRange(ref _oriya, first: '\u0B00', last: '\u0B7F');
        private static UnicodeRange _oriya;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Tamil' Unicode block (U+0B80..U+0BFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0B80.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Tamil => Volatile.Read(ref _tamil) ?? CreateRange(ref _tamil, first: '\u0B80', last: '\u0BFF');
        private static UnicodeRange _tamil;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Telugu' Unicode block (U+0C00..U+0C7F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0C00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Telugu => Volatile.Read(ref _telugu) ?? CreateRange(ref _telugu, first: '\u0C00', last: '\u0C7F');
        private static UnicodeRange _telugu;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Kannada' Unicode block (U+0C80..U+0CFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0C80.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Kannada => Volatile.Read(ref _kannada) ?? CreateRange(ref _kannada, first: '\u0C80', last: '\u0CFF');
        private static UnicodeRange _kannada;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Malayalam' Unicode block (U+0D00..U+0D7F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0D00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Malayalam => Volatile.Read(ref _malayalam) ?? CreateRange(ref _malayalam, first: '\u0D00', last: '\u0D7F');
        private static UnicodeRange _malayalam;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Sinhala' Unicode block (U+0D80..U+0DFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0D80.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Sinhala => Volatile.Read(ref _sinhala) ?? CreateRange(ref _sinhala, first: '\u0D80', last: '\u0DFF');
        private static UnicodeRange _sinhala;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Thai' Unicode block (U+0E00..U+0E7F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0E00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Thai => Volatile.Read(ref _thai) ?? CreateRange(ref _thai, first: '\u0E00', last: '\u0E7F');
        private static UnicodeRange _thai;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Lao' Unicode block (U+0E80..U+0EFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0E80.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Lao => Volatile.Read(ref _lao) ?? CreateRange(ref _lao, first: '\u0E80', last: '\u0EFF');
        private static UnicodeRange _lao;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Tibetan' Unicode block (U+0F00..U+0FFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U0F00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Tibetan => Volatile.Read(ref _tibetan) ?? CreateRange(ref _tibetan, first: '\u0F00', last: '\u0FFF');
        private static UnicodeRange _tibetan;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Myanmar' Unicode block (U+1000..U+109F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1000.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Myanmar => Volatile.Read(ref _myanmar) ?? CreateRange(ref _myanmar, first: '\u1000', last: '\u109F');
        private static UnicodeRange _myanmar;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Georgian' Unicode block (U+10A0..U+10FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U10A0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Georgian => Volatile.Read(ref _georgian) ?? CreateRange(ref _georgian, first: '\u10A0', last: '\u10FF');
        private static UnicodeRange _georgian;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Hangul Jamo' Unicode block (U+1100..U+11FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1100.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange HangulJamo => Volatile.Read(ref _hangulJamo) ?? CreateRange(ref _hangulJamo, first: '\u1100', last: '\u11FF');
        private static UnicodeRange _hangulJamo;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Ethiopic' Unicode block (U+1200..U+137F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1200.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Ethiopic => Volatile.Read(ref _ethiopic) ?? CreateRange(ref _ethiopic, first: '\u1200', last: '\u137F');
        private static UnicodeRange _ethiopic;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Ethiopic Supplement' Unicode block (U+1380..U+139F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1380.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange EthiopicSupplement => Volatile.Read(ref _ethiopicSupplement) ?? CreateRange(ref _ethiopicSupplement, first: '\u1380', last: '\u139F');
        private static UnicodeRange _ethiopicSupplement;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Cherokee' Unicode block (U+13A0..U+13FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U13A0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Cherokee => Volatile.Read(ref _cherokee) ?? CreateRange(ref _cherokee, first: '\u13A0', last: '\u13FF');
        private static UnicodeRange _cherokee;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Unified Canadian Aboriginal Syllabics' Unicode block (U+1400..U+167F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1400.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange UnifiedCanadianAboriginalSyllabics => Volatile.Read(ref _unifiedCanadianAboriginalSyllabics) ?? CreateRange(ref _unifiedCanadianAboriginalSyllabics, first: '\u1400', last: '\u167F');
        private static UnicodeRange _unifiedCanadianAboriginalSyllabics;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Ogham' Unicode block (U+1680..U+169F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1680.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Ogham => Volatile.Read(ref _ogham) ?? CreateRange(ref _ogham, first: '\u1680', last: '\u169F');
        private static UnicodeRange _ogham;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Runic' Unicode block (U+16A0..U+16FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U16A0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Runic => Volatile.Read(ref _runic) ?? CreateRange(ref _runic, first: '\u16A0', last: '\u16FF');
        private static UnicodeRange _runic;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Tagalog' Unicode block (U+1700..U+171F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1700.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Tagalog => Volatile.Read(ref _tagalog) ?? CreateRange(ref _tagalog, first: '\u1700', last: '\u171F');
        private static UnicodeRange _tagalog;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Hanunoo' Unicode block (U+1720..U+173F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1720.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Hanunoo => Volatile.Read(ref _hanunoo) ?? CreateRange(ref _hanunoo, first: '\u1720', last: '\u173F');
        private static UnicodeRange _hanunoo;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Buhid' Unicode block (U+1740..U+175F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1740.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Buhid => Volatile.Read(ref _buhid) ?? CreateRange(ref _buhid, first: '\u1740', last: '\u175F');
        private static UnicodeRange _buhid;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Tagbanwa' Unicode block (U+1760..U+177F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1760.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Tagbanwa => Volatile.Read(ref _tagbanwa) ?? CreateRange(ref _tagbanwa, first: '\u1760', last: '\u177F');
        private static UnicodeRange _tagbanwa;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Khmer' Unicode block (U+1780..U+17FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1780.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Khmer => Volatile.Read(ref _khmer) ?? CreateRange(ref _khmer, first: '\u1780', last: '\u17FF');
        private static UnicodeRange _khmer;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Mongolian' Unicode block (U+1800..U+18AF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1800.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Mongolian => Volatile.Read(ref _mongolian) ?? CreateRange(ref _mongolian, first: '\u1800', last: '\u18AF');
        private static UnicodeRange _mongolian;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Unified Canadian Aboriginal Syllabics Extended' Unicode block (U+18B0..U+18FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U18B0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange UnifiedCanadianAboriginalSyllabicsExtended => Volatile.Read(ref _unifiedCanadianAboriginalSyllabicsExtended) ?? CreateRange(ref _unifiedCanadianAboriginalSyllabicsExtended, first: '\u18B0', last: '\u18FF');
        private static UnicodeRange _unifiedCanadianAboriginalSyllabicsExtended;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Limbu' Unicode block (U+1900..U+194F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1900.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Limbu => Volatile.Read(ref _limbu) ?? CreateRange(ref _limbu, first: '\u1900', last: '\u194F');
        private static UnicodeRange _limbu;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Tai Le' Unicode block (U+1950..U+197F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1950.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange TaiLe => Volatile.Read(ref _taiLe) ?? CreateRange(ref _taiLe, first: '\u1950', last: '\u197F');
        private static UnicodeRange _taiLe;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'New Tai Lue' Unicode block (U+1980..U+19DF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1980.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange NewTaiLue => Volatile.Read(ref _newTaiLue) ?? CreateRange(ref _newTaiLue, first: '\u1980', last: '\u19DF');
        private static UnicodeRange _newTaiLue;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Khmer Symbols' Unicode block (U+19E0..U+19FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U19E0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange KhmerSymbols => Volatile.Read(ref _khmerSymbols) ?? CreateRange(ref _khmerSymbols, first: '\u19E0', last: '\u19FF');
        private static UnicodeRange _khmerSymbols;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Buginese' Unicode block (U+1A00..U+1A1F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1A00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Buginese => Volatile.Read(ref _buginese) ?? CreateRange(ref _buginese, first: '\u1A00', last: '\u1A1F');
        private static UnicodeRange _buginese;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Tai Tham' Unicode block (U+1A20..U+1AAF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1A20.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange TaiTham => Volatile.Read(ref _taiTham) ?? CreateRange(ref _taiTham, first: '\u1A20', last: '\u1AAF');
        private static UnicodeRange _taiTham;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Combining Diacritical Marks Extended' Unicode block (U+1AB0..U+1AFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1AB0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange CombiningDiacriticalMarksExtended => Volatile.Read(ref _combiningDiacriticalMarksExtended) ?? CreateRange(ref _combiningDiacriticalMarksExtended, first: '\u1AB0', last: '\u1AFF');
        private static UnicodeRange _combiningDiacriticalMarksExtended;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Balinese' Unicode block (U+1B00..U+1B7F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1B00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Balinese => Volatile.Read(ref _balinese) ?? CreateRange(ref _balinese, first: '\u1B00', last: '\u1B7F');
        private static UnicodeRange _balinese;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Sundanese' Unicode block (U+1B80..U+1BBF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1B80.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Sundanese => Volatile.Read(ref _sundanese) ?? CreateRange(ref _sundanese, first: '\u1B80', last: '\u1BBF');
        private static UnicodeRange _sundanese;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Batak' Unicode block (U+1BC0..U+1BFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1BC0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Batak => Volatile.Read(ref _batak) ?? CreateRange(ref _batak, first: '\u1BC0', last: '\u1BFF');
        private static UnicodeRange _batak;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Lepcha' Unicode block (U+1C00..U+1C4F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1C00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Lepcha => Volatile.Read(ref _lepcha) ?? CreateRange(ref _lepcha, first: '\u1C00', last: '\u1C4F');
        private static UnicodeRange _lepcha;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Ol Chiki' Unicode block (U+1C50..U+1C7F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1C50.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange OlChiki => Volatile.Read(ref _olChiki) ?? CreateRange(ref _olChiki, first: '\u1C50', last: '\u1C7F');
        private static UnicodeRange _olChiki;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Sundanese Supplement' Unicode block (U+1CC0..U+1CCF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1CC0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange SundaneseSupplement => Volatile.Read(ref _sundaneseSupplement) ?? CreateRange(ref _sundaneseSupplement, first: '\u1CC0', last: '\u1CCF');
        private static UnicodeRange _sundaneseSupplement;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Vedic Extensions' Unicode block (U+1CD0..U+1CFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1CD0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange VedicExtensions => Volatile.Read(ref _vedicExtensions) ?? CreateRange(ref _vedicExtensions, first: '\u1CD0', last: '\u1CFF');
        private static UnicodeRange _vedicExtensions;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Phonetic Extensions' Unicode block (U+1D00..U+1D7F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1D00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange PhoneticExtensions => Volatile.Read(ref _phoneticExtensions) ?? CreateRange(ref _phoneticExtensions, first: '\u1D00', last: '\u1D7F');
        private static UnicodeRange _phoneticExtensions;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Phonetic Extensions Supplement' Unicode block (U+1D80..U+1DBF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1D80.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange PhoneticExtensionsSupplement => Volatile.Read(ref _phoneticExtensionsSupplement) ?? CreateRange(ref _phoneticExtensionsSupplement, first: '\u1D80', last: '\u1DBF');
        private static UnicodeRange _phoneticExtensionsSupplement;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Combining Diacritical Marks Supplement' Unicode block (U+1DC0..U+1DFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1DC0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange CombiningDiacriticalMarksSupplement => Volatile.Read(ref _combiningDiacriticalMarksSupplement) ?? CreateRange(ref _combiningDiacriticalMarksSupplement, first: '\u1DC0', last: '\u1DFF');
        private static UnicodeRange _combiningDiacriticalMarksSupplement;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Latin Extended Additional' Unicode block (U+1E00..U+1EFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1E00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange LatinExtendedAdditional => Volatile.Read(ref _latinExtendedAdditional) ?? CreateRange(ref _latinExtendedAdditional, first: '\u1E00', last: '\u1EFF');
        private static UnicodeRange _latinExtendedAdditional;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Greek Extended' Unicode block (U+1F00..U+1FFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U1F00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange GreekExtended => Volatile.Read(ref _greekExtended) ?? CreateRange(ref _greekExtended, first: '\u1F00', last: '\u1FFF');
        private static UnicodeRange _greekExtended;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'General Punctuation' Unicode block (U+2000..U+206F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2000.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange GeneralPunctuation => Volatile.Read(ref _generalPunctuation) ?? CreateRange(ref _generalPunctuation, first: '\u2000', last: '\u206F');
        private static UnicodeRange _generalPunctuation;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Superscripts and Subscripts' Unicode block (U+2070..U+209F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2070.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange SuperscriptsandSubscripts => Volatile.Read(ref _superscriptsandSubscripts) ?? CreateRange(ref _superscriptsandSubscripts, first: '\u2070', last: '\u209F');
        private static UnicodeRange _superscriptsandSubscripts;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Currency Symbols' Unicode block (U+20A0..U+20CF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U20A0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange CurrencySymbols => Volatile.Read(ref _currencySymbols) ?? CreateRange(ref _currencySymbols, first: '\u20A0', last: '\u20CF');
        private static UnicodeRange _currencySymbols;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Combining Diacritical Marks for Symbols' Unicode block (U+20D0..U+20FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U20D0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange CombiningDiacriticalMarksforSymbols => Volatile.Read(ref _combiningDiacriticalMarksforSymbols) ?? CreateRange(ref _combiningDiacriticalMarksforSymbols, first: '\u20D0', last: '\u20FF');
        private static UnicodeRange _combiningDiacriticalMarksforSymbols;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Letterlike Symbols' Unicode block (U+2100..U+214F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2100.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange LetterlikeSymbols => Volatile.Read(ref _letterlikeSymbols) ?? CreateRange(ref _letterlikeSymbols, first: '\u2100', last: '\u214F');
        private static UnicodeRange _letterlikeSymbols;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Number Forms' Unicode block (U+2150..U+218F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2150.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange NumberForms => Volatile.Read(ref _numberForms) ?? CreateRange(ref _numberForms, first: '\u2150', last: '\u218F');
        private static UnicodeRange _numberForms;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Arrows' Unicode block (U+2190..U+21FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2190.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Arrows => Volatile.Read(ref _arrows) ?? CreateRange(ref _arrows, first: '\u2190', last: '\u21FF');
        private static UnicodeRange _arrows;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Mathematical Operators' Unicode block (U+2200..U+22FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2200.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange MathematicalOperators => Volatile.Read(ref _mathematicalOperators) ?? CreateRange(ref _mathematicalOperators, first: '\u2200', last: '\u22FF');
        private static UnicodeRange _mathematicalOperators;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Miscellaneous Technical' Unicode block (U+2300..U+23FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2300.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange MiscellaneousTechnical => Volatile.Read(ref _miscellaneousTechnical) ?? CreateRange(ref _miscellaneousTechnical, first: '\u2300', last: '\u23FF');
        private static UnicodeRange _miscellaneousTechnical;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Control Pictures' Unicode block (U+2400..U+243F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2400.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange ControlPictures => Volatile.Read(ref _controlPictures) ?? CreateRange(ref _controlPictures, first: '\u2400', last: '\u243F');
        private static UnicodeRange _controlPictures;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Optical Character Recognition' Unicode block (U+2440..U+245F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2440.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange OpticalCharacterRecognition => Volatile.Read(ref _opticalCharacterRecognition) ?? CreateRange(ref _opticalCharacterRecognition, first: '\u2440', last: '\u245F');
        private static UnicodeRange _opticalCharacterRecognition;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Enclosed Alphanumerics' Unicode block (U+2460..U+24FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2460.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange EnclosedAlphanumerics => Volatile.Read(ref _enclosedAlphanumerics) ?? CreateRange(ref _enclosedAlphanumerics, first: '\u2460', last: '\u24FF');
        private static UnicodeRange _enclosedAlphanumerics;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Box Drawing' Unicode block (U+2500..U+257F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2500.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange BoxDrawing => Volatile.Read(ref _boxDrawing) ?? CreateRange(ref _boxDrawing, first: '\u2500', last: '\u257F');
        private static UnicodeRange _boxDrawing;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Block Elements' Unicode block (U+2580..U+259F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2580.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange BlockElements => Volatile.Read(ref _blockElements) ?? CreateRange(ref _blockElements, first: '\u2580', last: '\u259F');
        private static UnicodeRange _blockElements;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Geometric Shapes' Unicode block (U+25A0..U+25FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U25A0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange GeometricShapes => Volatile.Read(ref _geometricShapes) ?? CreateRange(ref _geometricShapes, first: '\u25A0', last: '\u25FF');
        private static UnicodeRange _geometricShapes;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Miscellaneous Symbols' Unicode block (U+2600..U+26FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2600.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange MiscellaneousSymbols => Volatile.Read(ref _miscellaneousSymbols) ?? CreateRange(ref _miscellaneousSymbols, first: '\u2600', last: '\u26FF');
        private static UnicodeRange _miscellaneousSymbols;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Dingbats' Unicode block (U+2700..U+27BF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2700.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Dingbats => Volatile.Read(ref _dingbats) ?? CreateRange(ref _dingbats, first: '\u2700', last: '\u27BF');
        private static UnicodeRange _dingbats;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Miscellaneous Mathematical Symbols-A' Unicode block (U+27C0..U+27EF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U27C0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange MiscellaneousMathematicalSymbolsA => Volatile.Read(ref _miscellaneousMathematicalSymbolsA) ?? CreateRange(ref _miscellaneousMathematicalSymbolsA, first: '\u27C0', last: '\u27EF');
        private static UnicodeRange _miscellaneousMathematicalSymbolsA;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Supplemental Arrows-A' Unicode block (U+27F0..U+27FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U27F0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange SupplementalArrowsA => Volatile.Read(ref _supplementalArrowsA) ?? CreateRange(ref _supplementalArrowsA, first: '\u27F0', last: '\u27FF');
        private static UnicodeRange _supplementalArrowsA;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Braille Patterns' Unicode block (U+2800..U+28FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2800.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange BraillePatterns => Volatile.Read(ref _braillePatterns) ?? CreateRange(ref _braillePatterns, first: '\u2800', last: '\u28FF');
        private static UnicodeRange _braillePatterns;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Supplemental Arrows-B' Unicode block (U+2900..U+297F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2900.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange SupplementalArrowsB => Volatile.Read(ref _supplementalArrowsB) ?? CreateRange(ref _supplementalArrowsB, first: '\u2900', last: '\u297F');
        private static UnicodeRange _supplementalArrowsB;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Miscellaneous Mathematical Symbols-B' Unicode block (U+2980..U+29FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2980.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange MiscellaneousMathematicalSymbolsB => Volatile.Read(ref _miscellaneousMathematicalSymbolsB) ?? CreateRange(ref _miscellaneousMathematicalSymbolsB, first: '\u2980', last: '\u29FF');
        private static UnicodeRange _miscellaneousMathematicalSymbolsB;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Supplemental Mathematical Operators' Unicode block (U+2A00..U+2AFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2A00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange SupplementalMathematicalOperators => Volatile.Read(ref _supplementalMathematicalOperators) ?? CreateRange(ref _supplementalMathematicalOperators, first: '\u2A00', last: '\u2AFF');
        private static UnicodeRange _supplementalMathematicalOperators;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Miscellaneous Symbols and Arrows' Unicode block (U+2B00..U+2BFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2B00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange MiscellaneousSymbolsandArrows => Volatile.Read(ref _miscellaneousSymbolsandArrows) ?? CreateRange(ref _miscellaneousSymbolsandArrows, first: '\u2B00', last: '\u2BFF');
        private static UnicodeRange _miscellaneousSymbolsandArrows;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Glagolitic' Unicode block (U+2C00..U+2C5F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2C00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Glagolitic => Volatile.Read(ref _glagolitic) ?? CreateRange(ref _glagolitic, first: '\u2C00', last: '\u2C5F');
        private static UnicodeRange _glagolitic;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Latin Extended-C' Unicode block (U+2C60..U+2C7F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2C60.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange LatinExtendedC => Volatile.Read(ref _latinExtendedC) ?? CreateRange(ref _latinExtendedC, first: '\u2C60', last: '\u2C7F');
        private static UnicodeRange _latinExtendedC;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Coptic' Unicode block (U+2C80..U+2CFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2C80.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Coptic => Volatile.Read(ref _coptic) ?? CreateRange(ref _coptic, first: '\u2C80', last: '\u2CFF');
        private static UnicodeRange _coptic;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Georgian Supplement' Unicode block (U+2D00..U+2D2F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2D00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange GeorgianSupplement => Volatile.Read(ref _georgianSupplement) ?? CreateRange(ref _georgianSupplement, first: '\u2D00', last: '\u2D2F');
        private static UnicodeRange _georgianSupplement;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Tifinagh' Unicode block (U+2D30..U+2D7F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2D30.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Tifinagh => Volatile.Read(ref _tifinagh) ?? CreateRange(ref _tifinagh, first: '\u2D30', last: '\u2D7F');
        private static UnicodeRange _tifinagh;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Ethiopic Extended' Unicode block (U+2D80..U+2DDF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2D80.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange EthiopicExtended => Volatile.Read(ref _ethiopicExtended) ?? CreateRange(ref _ethiopicExtended, first: '\u2D80', last: '\u2DDF');
        private static UnicodeRange _ethiopicExtended;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Cyrillic Extended-A' Unicode block (U+2DE0..U+2DFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2DE0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange CyrillicExtendedA => Volatile.Read(ref _cyrillicExtendedA) ?? CreateRange(ref _cyrillicExtendedA, first: '\u2DE0', last: '\u2DFF');
        private static UnicodeRange _cyrillicExtendedA;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Supplemental Punctuation' Unicode block (U+2E00..U+2E7F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2E00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange SupplementalPunctuation => Volatile.Read(ref _supplementalPunctuation) ?? CreateRange(ref _supplementalPunctuation, first: '\u2E00', last: '\u2E7F');
        private static UnicodeRange _supplementalPunctuation;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'CJK Radicals Supplement' Unicode block (U+2E80..U+2EFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2E80.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange CJKRadicalsSupplement => Volatile.Read(ref _cjkRadicalsSupplement) ?? CreateRange(ref _cjkRadicalsSupplement, first: '\u2E80', last: '\u2EFF');
        private static UnicodeRange _cjkRadicalsSupplement;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Kangxi Radicals' Unicode block (U+2F00..U+2FDF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2F00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange KangxiRadicals => Volatile.Read(ref _kangxiRadicals) ?? CreateRange(ref _kangxiRadicals, first: '\u2F00', last: '\u2FDF');
        private static UnicodeRange _kangxiRadicals;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Ideographic Description Characters' Unicode block (U+2FF0..U+2FFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U2FF0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange IdeographicDescriptionCharacters => Volatile.Read(ref _ideographicDescriptionCharacters) ?? CreateRange(ref _ideographicDescriptionCharacters, first: '\u2FF0', last: '\u2FFF');
        private static UnicodeRange _ideographicDescriptionCharacters;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'CJK Symbols and Punctuation' Unicode block (U+3000..U+303F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U3000.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange CJKSymbolsandPunctuation => Volatile.Read(ref _cjkSymbolsandPunctuation) ?? CreateRange(ref _cjkSymbolsandPunctuation, first: '\u3000', last: '\u303F');
        private static UnicodeRange _cjkSymbolsandPunctuation;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Hiragana' Unicode block (U+3040..U+309F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U3040.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Hiragana => Volatile.Read(ref _hiragana) ?? CreateRange(ref _hiragana, first: '\u3040', last: '\u309F');
        private static UnicodeRange _hiragana;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Katakana' Unicode block (U+30A0..U+30FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U30A0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Katakana => Volatile.Read(ref _katakana) ?? CreateRange(ref _katakana, first: '\u30A0', last: '\u30FF');
        private static UnicodeRange _katakana;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Bopomofo' Unicode block (U+3100..U+312F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U3100.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Bopomofo => Volatile.Read(ref _bopomofo) ?? CreateRange(ref _bopomofo, first: '\u3100', last: '\u312F');
        private static UnicodeRange _bopomofo;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Hangul Compatibility Jamo' Unicode block (U+3130..U+318F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U3130.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange HangulCompatibilityJamo => Volatile.Read(ref _hangulCompatibilityJamo) ?? CreateRange(ref _hangulCompatibilityJamo, first: '\u3130', last: '\u318F');
        private static UnicodeRange _hangulCompatibilityJamo;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Kanbun' Unicode block (U+3190..U+319F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U3190.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Kanbun => Volatile.Read(ref _kanbun) ?? CreateRange(ref _kanbun, first: '\u3190', last: '\u319F');
        private static UnicodeRange _kanbun;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Bopomofo Extended' Unicode block (U+31A0..U+31BF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U31A0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange BopomofoExtended => Volatile.Read(ref _bopomofoExtended) ?? CreateRange(ref _bopomofoExtended, first: '\u31A0', last: '\u31BF');
        private static UnicodeRange _bopomofoExtended;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'CJK Strokes' Unicode block (U+31C0..U+31EF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U31C0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange CJKStrokes => Volatile.Read(ref _cjkStrokes) ?? CreateRange(ref _cjkStrokes, first: '\u31C0', last: '\u31EF');
        private static UnicodeRange _cjkStrokes;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Katakana Phonetic Extensions' Unicode block (U+31F0..U+31FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U31F0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange KatakanaPhoneticExtensions => Volatile.Read(ref _katakanaPhoneticExtensions) ?? CreateRange(ref _katakanaPhoneticExtensions, first: '\u31F0', last: '\u31FF');
        private static UnicodeRange _katakanaPhoneticExtensions;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Enclosed CJK Letters and Months' Unicode block (U+3200..U+32FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U3200.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange EnclosedCJKLettersandMonths => Volatile.Read(ref _enclosedCJKLettersandMonths) ?? CreateRange(ref _enclosedCJKLettersandMonths, first: '\u3200', last: '\u32FF');
        private static UnicodeRange _enclosedCJKLettersandMonths;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'CJK Compatibility' Unicode block (U+3300..U+33FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U3300.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange CJKCompatibility => Volatile.Read(ref _cjkCompatibility) ?? CreateRange(ref _cjkCompatibility, first: '\u3300', last: '\u33FF');
        private static UnicodeRange _cjkCompatibility;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'CJK Unified Ideographs Extension A' Unicode block (U+3400..U+4DBF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U3400.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange CJKUnifiedIdeographsExtensionA => Volatile.Read(ref _cjkUnifiedIdeographsExtensionA) ?? CreateRange(ref _cjkUnifiedIdeographsExtensionA, first: '\u3400', last: '\u4DBF');
        private static UnicodeRange _cjkUnifiedIdeographsExtensionA;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Yijing Hexagram Symbols' Unicode block (U+4DC0..U+4DFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U4DC0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange YijingHexagramSymbols => Volatile.Read(ref _yijingHexagramSymbols) ?? CreateRange(ref _yijingHexagramSymbols, first: '\u4DC0', last: '\u4DFF');
        private static UnicodeRange _yijingHexagramSymbols;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'CJK Unified Ideographs' Unicode block (U+4E00..U+9FFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/U4E00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange CJKUnifiedIdeographs => Volatile.Read(ref _cjkUnifiedIdeographs) ?? CreateRange(ref _cjkUnifiedIdeographs, first: '\u4E00', last: '\u9FFF');
        private static UnicodeRange _cjkUnifiedIdeographs;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Yi Syllables' Unicode block (U+A000..U+A48F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA000.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange YiSyllables => Volatile.Read(ref _yiSyllables) ?? CreateRange(ref _yiSyllables, first: '\uA000', last: '\uA48F');
        private static UnicodeRange _yiSyllables;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Yi Radicals' Unicode block (U+A490..U+A4CF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA490.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange YiRadicals => Volatile.Read(ref _yiRadicals) ?? CreateRange(ref _yiRadicals, first: '\uA490', last: '\uA4CF');
        private static UnicodeRange _yiRadicals;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Lisu' Unicode block (U+A4D0..U+A4FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA4D0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Lisu => Volatile.Read(ref _lisu) ?? CreateRange(ref _lisu, first: '\uA4D0', last: '\uA4FF');
        private static UnicodeRange _lisu;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Vai' Unicode block (U+A500..U+A63F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA500.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Vai => Volatile.Read(ref _vai) ?? CreateRange(ref _vai, first: '\uA500', last: '\uA63F');
        private static UnicodeRange _vai;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Cyrillic Extended-B' Unicode block (U+A640..U+A69F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA640.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange CyrillicExtendedB => Volatile.Read(ref _cyrillicExtendedB) ?? CreateRange(ref _cyrillicExtendedB, first: '\uA640', last: '\uA69F');
        private static UnicodeRange _cyrillicExtendedB;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Bamum' Unicode block (U+A6A0..U+A6FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA6A0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Bamum => Volatile.Read(ref _bamum) ?? CreateRange(ref _bamum, first: '\uA6A0', last: '\uA6FF');
        private static UnicodeRange _bamum;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Modifier Tone Letters' Unicode block (U+A700..U+A71F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA700.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange ModifierToneLetters => Volatile.Read(ref _modifierToneLetters) ?? CreateRange(ref _modifierToneLetters, first: '\uA700', last: '\uA71F');
        private static UnicodeRange _modifierToneLetters;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Latin Extended-D' Unicode block (U+A720..U+A7FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA720.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange LatinExtendedD => Volatile.Read(ref _latinExtendedD) ?? CreateRange(ref _latinExtendedD, first: '\uA720', last: '\uA7FF');
        private static UnicodeRange _latinExtendedD;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Syloti Nagri' Unicode block (U+A800..U+A82F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA800.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange SylotiNagri => Volatile.Read(ref _sylotiNagri) ?? CreateRange(ref _sylotiNagri, first: '\uA800', last: '\uA82F');
        private static UnicodeRange _sylotiNagri;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Common Indic Number Forms' Unicode block (U+A830..U+A83F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA830.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange CommonIndicNumberForms => Volatile.Read(ref _commonIndicNumberForms) ?? CreateRange(ref _commonIndicNumberForms, first: '\uA830', last: '\uA83F');
        private static UnicodeRange _commonIndicNumberForms;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Phags-pa' Unicode block (U+A840..U+A87F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA840.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Phagspa => Volatile.Read(ref _phagspa) ?? CreateRange(ref _phagspa, first: '\uA840', last: '\uA87F');
        private static UnicodeRange _phagspa;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Saurashtra' Unicode block (U+A880..U+A8DF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA880.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Saurashtra => Volatile.Read(ref _saurashtra) ?? CreateRange(ref _saurashtra, first: '\uA880', last: '\uA8DF');
        private static UnicodeRange _saurashtra;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Devanagari Extended' Unicode block (U+A8E0..U+A8FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA8E0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange DevanagariExtended => Volatile.Read(ref _devanagariExtended) ?? CreateRange(ref _devanagariExtended, first: '\uA8E0', last: '\uA8FF');
        private static UnicodeRange _devanagariExtended;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Kayah Li' Unicode block (U+A900..U+A92F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA900.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange KayahLi => Volatile.Read(ref _kayahLi) ?? CreateRange(ref _kayahLi, first: '\uA900', last: '\uA92F');
        private static UnicodeRange _kayahLi;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Rejang' Unicode block (U+A930..U+A95F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA930.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Rejang => Volatile.Read(ref _rejang) ?? CreateRange(ref _rejang, first: '\uA930', last: '\uA95F');
        private static UnicodeRange _rejang;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Hangul Jamo Extended-A' Unicode block (U+A960..U+A97F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA960.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange HangulJamoExtendedA => Volatile.Read(ref _hangulJamoExtendedA) ?? CreateRange(ref _hangulJamoExtendedA, first: '\uA960', last: '\uA97F');
        private static UnicodeRange _hangulJamoExtendedA;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Javanese' Unicode block (U+A980..U+A9DF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA980.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Javanese => Volatile.Read(ref _javanese) ?? CreateRange(ref _javanese, first: '\uA980', last: '\uA9DF');
        private static UnicodeRange _javanese;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Myanmar Extended-B' Unicode block (U+A9E0..U+A9FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UA9E0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange MyanmarExtendedB => Volatile.Read(ref _myanmarExtendedB) ?? CreateRange(ref _myanmarExtendedB, first: '\uA9E0', last: '\uA9FF');
        private static UnicodeRange _myanmarExtendedB;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Cham' Unicode block (U+AA00..U+AA5F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UAA00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Cham => Volatile.Read(ref _cham) ?? CreateRange(ref _cham, first: '\uAA00', last: '\uAA5F');
        private static UnicodeRange _cham;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Myanmar Extended-A' Unicode block (U+AA60..U+AA7F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UAA60.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange MyanmarExtendedA => Volatile.Read(ref _myanmarExtendedA) ?? CreateRange(ref _myanmarExtendedA, first: '\uAA60', last: '\uAA7F');
        private static UnicodeRange _myanmarExtendedA;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Tai Viet' Unicode block (U+AA80..U+AADF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UAA80.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange TaiViet => Volatile.Read(ref _taiViet) ?? CreateRange(ref _taiViet, first: '\uAA80', last: '\uAADF');
        private static UnicodeRange _taiViet;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Meetei Mayek Extensions' Unicode block (U+AAE0..U+AAFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UAAE0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange MeeteiMayekExtensions => Volatile.Read(ref _meeteiMayekExtensions) ?? CreateRange(ref _meeteiMayekExtensions, first: '\uAAE0', last: '\uAAFF');
        private static UnicodeRange _meeteiMayekExtensions;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Ethiopic Extended-A' Unicode block (U+AB00..U+AB2F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UAB00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange EthiopicExtendedA => Volatile.Read(ref _ethiopicExtendedA) ?? CreateRange(ref _ethiopicExtendedA, first: '\uAB00', last: '\uAB2F');
        private static UnicodeRange _ethiopicExtendedA;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Latin Extended-E' Unicode block (U+AB30..U+AB6F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UAB30.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange LatinExtendedE => Volatile.Read(ref _latinExtendedE) ?? CreateRange(ref _latinExtendedE, first: '\uAB30', last: '\uAB6F');
        private static UnicodeRange _latinExtendedE;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Cherokee Supplement' Unicode block (U+AB70..U+ABBF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UAB70.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange CherokeeSupplement => Volatile.Read(ref _cherokeeSupplement) ?? CreateRange(ref _cherokeeSupplement, first: '\uAB70', last: '\uABBF');
        private static UnicodeRange _cherokeeSupplement;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Meetei Mayek' Unicode block (U+ABC0..U+ABFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UABC0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange MeeteiMayek => Volatile.Read(ref _meeteiMayek) ?? CreateRange(ref _meeteiMayek, first: '\uABC0', last: '\uABFF');
        private static UnicodeRange _meeteiMayek;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Hangul Syllables' Unicode block (U+AC00..U+D7AF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UAC00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange HangulSyllables => Volatile.Read(ref _hangulSyllables) ?? CreateRange(ref _hangulSyllables, first: '\uAC00', last: '\uD7AF');
        private static UnicodeRange _hangulSyllables;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Hangul Jamo Extended-B' Unicode block (U+D7B0..U+D7FF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UD7B0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange HangulJamoExtendedB => Volatile.Read(ref _hangulJamoExtendedB) ?? CreateRange(ref _hangulJamoExtendedB, first: '\uD7B0', last: '\uD7FF');
        private static UnicodeRange _hangulJamoExtendedB;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'CJK Compatibility Ideographs' Unicode block (U+F900..U+FAFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UF900.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange CJKCompatibilityIdeographs => Volatile.Read(ref _cjkCompatibilityIdeographs) ?? CreateRange(ref _cjkCompatibilityIdeographs, first: '\uF900', last: '\uFAFF');
        private static UnicodeRange _cjkCompatibilityIdeographs;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Alphabetic Presentation Forms' Unicode block (U+FB00..U+FB4F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UFB00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange AlphabeticPresentationForms => Volatile.Read(ref _alphabeticPresentationForms) ?? CreateRange(ref _alphabeticPresentationForms, first: '\uFB00', last: '\uFB4F');
        private static UnicodeRange _alphabeticPresentationForms;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Arabic Presentation Forms-A' Unicode block (U+FB50..U+FDFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UFB50.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange ArabicPresentationFormsA => Volatile.Read(ref _arabicPresentationFormsA) ?? CreateRange(ref _arabicPresentationFormsA, first: '\uFB50', last: '\uFDFF');
        private static UnicodeRange _arabicPresentationFormsA;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Variation Selectors' Unicode block (U+FE00..U+FE0F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UFE00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange VariationSelectors => Volatile.Read(ref _variationSelectors) ?? CreateRange(ref _variationSelectors, first: '\uFE00', last: '\uFE0F');
        private static UnicodeRange _variationSelectors;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Vertical Forms' Unicode block (U+FE10..U+FE1F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UFE10.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange VerticalForms => Volatile.Read(ref _verticalForms) ?? CreateRange(ref _verticalForms, first: '\uFE10', last: '\uFE1F');
        private static UnicodeRange _verticalForms;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Combining Half Marks' Unicode block (U+FE20..U+FE2F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UFE20.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange CombiningHalfMarks => Volatile.Read(ref _combiningHalfMarks) ?? CreateRange(ref _combiningHalfMarks, first: '\uFE20', last: '\uFE2F');
        private static UnicodeRange _combiningHalfMarks;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'CJK Compatibility Forms' Unicode block (U+FE30..U+FE4F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UFE30.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange CJKCompatibilityForms => Volatile.Read(ref _cjkCompatibilityForms) ?? CreateRange(ref _cjkCompatibilityForms, first: '\uFE30', last: '\uFE4F');
        private static UnicodeRange _cjkCompatibilityForms;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Small Form Variants' Unicode block (U+FE50..U+FE6F).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UFE50.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange SmallFormVariants => Volatile.Read(ref _smallFormVariants) ?? CreateRange(ref _smallFormVariants, first: '\uFE50', last: '\uFE6F');
        private static UnicodeRange _smallFormVariants;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Arabic Presentation Forms-B' Unicode block (U+FE70..U+FEFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UFE70.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange ArabicPresentationFormsB => Volatile.Read(ref _arabicPresentationFormsB) ?? CreateRange(ref _arabicPresentationFormsB, first: '\uFE70', last: '\uFEFF');
        private static UnicodeRange _arabicPresentationFormsB;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Halfwidth and Fullwidth Forms' Unicode block (U+FF00..U+FFEF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UFF00.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange HalfwidthandFullwidthForms => Volatile.Read(ref _halfwidthandFullwidthForms) ?? CreateRange(ref _halfwidthandFullwidthForms, first: '\uFF00', last: '\uFFEF');
        private static UnicodeRange _halfwidthandFullwidthForms;

        /// <summary>
        /// A <see cref="UnicodeRange"/> corresponding to the 'Specials' Unicode block (U+FFF0..U+FFFF).
        /// </summary>
        /// <remarks>
        /// See http://www.unicode.org/charts/PDF/UFFF0.pdf for the full set of characters in this block.
        /// </remarks>
        public static UnicodeRange Specials => Volatile.Read(ref _specials) ?? CreateRange(ref _specials, first: '\uFFF0', last: '\uFFFF');
        private static UnicodeRange _specials;
    }
}
