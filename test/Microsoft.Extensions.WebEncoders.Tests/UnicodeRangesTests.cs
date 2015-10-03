// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Xunit;

namespace Microsoft.Extensions.WebEncoders
{
    public class UnicodeRangesTests
    {
        [Fact]
        public void Range_None()
        {
            UnicodeRange range = UnicodeRanges.None;
            Assert.NotNull(range);

            // Test 1: the range should be empty
            Assert.Equal(0, range.FirstCodePoint);
            Assert.Equal(0, range.RangeSize);

            // Test 2: calling the property multiple times should cache and return the same range instance
            UnicodeRange range2 = UnicodeRanges.None;
            Assert.Same(range, range2);
        }

        [Fact]
        public void Range_All()
        {
            Range_Unicode('\u0000', '\uFFFF', nameof(UnicodeRanges.All));
        }

        [Theory]
        [InlineData('\u0000', '\u007F', nameof(UnicodeRanges.BasicLatin))]
        [InlineData('\u0080', '\u00FF', nameof(UnicodeRanges.Latin1Supplement))]
        [InlineData('\u0100', '\u017F', nameof(UnicodeRanges.LatinExtendedA))]
        [InlineData('\u0180', '\u024F', nameof(UnicodeRanges.LatinExtendedB))]
        [InlineData('\u0250', '\u02AF', nameof(UnicodeRanges.IPAExtensions))]
        [InlineData('\u02B0', '\u02FF', nameof(UnicodeRanges.SpacingModifierLetters))]
        [InlineData('\u0300', '\u036F', nameof(UnicodeRanges.CombiningDiacriticalMarks))]
        [InlineData('\u0370', '\u03FF', nameof(UnicodeRanges.GreekandCoptic))]
        [InlineData('\u0400', '\u04FF', nameof(UnicodeRanges.Cyrillic))]
        [InlineData('\u0500', '\u052F', nameof(UnicodeRanges.CyrillicSupplement))]
        [InlineData('\u0530', '\u058F', nameof(UnicodeRanges.Armenian))]
        [InlineData('\u0590', '\u05FF', nameof(UnicodeRanges.Hebrew))]
        [InlineData('\u0600', '\u06FF', nameof(UnicodeRanges.Arabic))]
        [InlineData('\u0700', '\u074F', nameof(UnicodeRanges.Syriac))]
        [InlineData('\u0750', '\u077F', nameof(UnicodeRanges.ArabicSupplement))]
        [InlineData('\u0780', '\u07BF', nameof(UnicodeRanges.Thaana))]
        [InlineData('\u07C0', '\u07FF', nameof(UnicodeRanges.NKo))]
        [InlineData('\u0800', '\u083F', nameof(UnicodeRanges.Samaritan))]
        [InlineData('\u0840', '\u085F', nameof(UnicodeRanges.Mandaic))]
        [InlineData('\u08A0', '\u08FF', nameof(UnicodeRanges.ArabicExtendedA))]
        [InlineData('\u0900', '\u097F', nameof(UnicodeRanges.Devanagari))]
        [InlineData('\u0980', '\u09FF', nameof(UnicodeRanges.Bengali))]
        [InlineData('\u0A00', '\u0A7F', nameof(UnicodeRanges.Gurmukhi))]
        [InlineData('\u0A80', '\u0AFF', nameof(UnicodeRanges.Gujarati))]
        [InlineData('\u0B00', '\u0B7F', nameof(UnicodeRanges.Oriya))]
        [InlineData('\u0B80', '\u0BFF', nameof(UnicodeRanges.Tamil))]
        [InlineData('\u0C00', '\u0C7F', nameof(UnicodeRanges.Telugu))]
        [InlineData('\u0C80', '\u0CFF', nameof(UnicodeRanges.Kannada))]
        [InlineData('\u0D00', '\u0D7F', nameof(UnicodeRanges.Malayalam))]
        [InlineData('\u0D80', '\u0DFF', nameof(UnicodeRanges.Sinhala))]
        [InlineData('\u0E00', '\u0E7F', nameof(UnicodeRanges.Thai))]
        [InlineData('\u0E80', '\u0EFF', nameof(UnicodeRanges.Lao))]
        [InlineData('\u0F00', '\u0FFF', nameof(UnicodeRanges.Tibetan))]
        [InlineData('\u1000', '\u109F', nameof(UnicodeRanges.Myanmar))]
        [InlineData('\u10A0', '\u10FF', nameof(UnicodeRanges.Georgian))]
        [InlineData('\u1100', '\u11FF', nameof(UnicodeRanges.HangulJamo))]
        [InlineData('\u1200', '\u137F', nameof(UnicodeRanges.Ethiopic))]
        [InlineData('\u1380', '\u139F', nameof(UnicodeRanges.EthiopicSupplement))]
        [InlineData('\u13A0', '\u13FF', nameof(UnicodeRanges.Cherokee))]
        [InlineData('\u1400', '\u167F', nameof(UnicodeRanges.UnifiedCanadianAboriginalSyllabics))]
        [InlineData('\u1680', '\u169F', nameof(UnicodeRanges.Ogham))]
        [InlineData('\u16A0', '\u16FF', nameof(UnicodeRanges.Runic))]
        [InlineData('\u1700', '\u171F', nameof(UnicodeRanges.Tagalog))]
        [InlineData('\u1720', '\u173F', nameof(UnicodeRanges.Hanunoo))]
        [InlineData('\u1740', '\u175F', nameof(UnicodeRanges.Buhid))]
        [InlineData('\u1760', '\u177F', nameof(UnicodeRanges.Tagbanwa))]
        [InlineData('\u1780', '\u17FF', nameof(UnicodeRanges.Khmer))]
        [InlineData('\u1800', '\u18AF', nameof(UnicodeRanges.Mongolian))]
        [InlineData('\u18B0', '\u18FF', nameof(UnicodeRanges.UnifiedCanadianAboriginalSyllabicsExtended))]
        [InlineData('\u1900', '\u194F', nameof(UnicodeRanges.Limbu))]
        [InlineData('\u1950', '\u197F', nameof(UnicodeRanges.TaiLe))]
        [InlineData('\u1980', '\u19DF', nameof(UnicodeRanges.NewTaiLue))]
        [InlineData('\u19E0', '\u19FF', nameof(UnicodeRanges.KhmerSymbols))]
        [InlineData('\u1A00', '\u1A1F', nameof(UnicodeRanges.Buginese))]
        [InlineData('\u1A20', '\u1AAF', nameof(UnicodeRanges.TaiTham))]
        [InlineData('\u1AB0', '\u1AFF', nameof(UnicodeRanges.CombiningDiacriticalMarksExtended))]
        [InlineData('\u1B00', '\u1B7F', nameof(UnicodeRanges.Balinese))]
        [InlineData('\u1B80', '\u1BBF', nameof(UnicodeRanges.Sundanese))]
        [InlineData('\u1BC0', '\u1BFF', nameof(UnicodeRanges.Batak))]
        [InlineData('\u1C00', '\u1C4F', nameof(UnicodeRanges.Lepcha))]
        [InlineData('\u1C50', '\u1C7F', nameof(UnicodeRanges.OlChiki))]
        [InlineData('\u1CC0', '\u1CCF', nameof(UnicodeRanges.SundaneseSupplement))]
        [InlineData('\u1CD0', '\u1CFF', nameof(UnicodeRanges.VedicExtensions))]
        [InlineData('\u1D00', '\u1D7F', nameof(UnicodeRanges.PhoneticExtensions))]
        [InlineData('\u1D80', '\u1DBF', nameof(UnicodeRanges.PhoneticExtensionsSupplement))]
        [InlineData('\u1DC0', '\u1DFF', nameof(UnicodeRanges.CombiningDiacriticalMarksSupplement))]
        [InlineData('\u1E00', '\u1EFF', nameof(UnicodeRanges.LatinExtendedAdditional))]
        [InlineData('\u1F00', '\u1FFF', nameof(UnicodeRanges.GreekExtended))]
        [InlineData('\u2000', '\u206F', nameof(UnicodeRanges.GeneralPunctuation))]
        [InlineData('\u2070', '\u209F', nameof(UnicodeRanges.SuperscriptsandSubscripts))]
        [InlineData('\u20A0', '\u20CF', nameof(UnicodeRanges.CurrencySymbols))]
        [InlineData('\u20D0', '\u20FF', nameof(UnicodeRanges.CombiningDiacriticalMarksforSymbols))]
        [InlineData('\u2100', '\u214F', nameof(UnicodeRanges.LetterlikeSymbols))]
        [InlineData('\u2150', '\u218F', nameof(UnicodeRanges.NumberForms))]
        [InlineData('\u2190', '\u21FF', nameof(UnicodeRanges.Arrows))]
        [InlineData('\u2200', '\u22FF', nameof(UnicodeRanges.MathematicalOperators))]
        [InlineData('\u2300', '\u23FF', nameof(UnicodeRanges.MiscellaneousTechnical))]
        [InlineData('\u2400', '\u243F', nameof(UnicodeRanges.ControlPictures))]
        [InlineData('\u2440', '\u245F', nameof(UnicodeRanges.OpticalCharacterRecognition))]
        [InlineData('\u2460', '\u24FF', nameof(UnicodeRanges.EnclosedAlphanumerics))]
        [InlineData('\u2500', '\u257F', nameof(UnicodeRanges.BoxDrawing))]
        [InlineData('\u2580', '\u259F', nameof(UnicodeRanges.BlockElements))]
        [InlineData('\u25A0', '\u25FF', nameof(UnicodeRanges.GeometricShapes))]
        [InlineData('\u2600', '\u26FF', nameof(UnicodeRanges.MiscellaneousSymbols))]
        [InlineData('\u2700', '\u27BF', nameof(UnicodeRanges.Dingbats))]
        [InlineData('\u27C0', '\u27EF', nameof(UnicodeRanges.MiscellaneousMathematicalSymbolsA))]
        [InlineData('\u27F0', '\u27FF', nameof(UnicodeRanges.SupplementalArrowsA))]
        [InlineData('\u2800', '\u28FF', nameof(UnicodeRanges.BraillePatterns))]
        [InlineData('\u2900', '\u297F', nameof(UnicodeRanges.SupplementalArrowsB))]
        [InlineData('\u2980', '\u29FF', nameof(UnicodeRanges.MiscellaneousMathematicalSymbolsB))]
        [InlineData('\u2A00', '\u2AFF', nameof(UnicodeRanges.SupplementalMathematicalOperators))]
        [InlineData('\u2B00', '\u2BFF', nameof(UnicodeRanges.MiscellaneousSymbolsandArrows))]
        [InlineData('\u2C00', '\u2C5F', nameof(UnicodeRanges.Glagolitic))]
        [InlineData('\u2C60', '\u2C7F', nameof(UnicodeRanges.LatinExtendedC))]
        [InlineData('\u2C80', '\u2CFF', nameof(UnicodeRanges.Coptic))]
        [InlineData('\u2D00', '\u2D2F', nameof(UnicodeRanges.GeorgianSupplement))]
        [InlineData('\u2D30', '\u2D7F', nameof(UnicodeRanges.Tifinagh))]
        [InlineData('\u2D80', '\u2DDF', nameof(UnicodeRanges.EthiopicExtended))]
        [InlineData('\u2DE0', '\u2DFF', nameof(UnicodeRanges.CyrillicExtendedA))]
        [InlineData('\u2E00', '\u2E7F', nameof(UnicodeRanges.SupplementalPunctuation))]
        [InlineData('\u2E80', '\u2EFF', nameof(UnicodeRanges.CJKRadicalsSupplement))]
        [InlineData('\u2F00', '\u2FDF', nameof(UnicodeRanges.KangxiRadicals))]
        [InlineData('\u2FF0', '\u2FFF', nameof(UnicodeRanges.IdeographicDescriptionCharacters))]
        [InlineData('\u3000', '\u303F', nameof(UnicodeRanges.CJKSymbolsandPunctuation))]
        [InlineData('\u3040', '\u309F', nameof(UnicodeRanges.Hiragana))]
        [InlineData('\u30A0', '\u30FF', nameof(UnicodeRanges.Katakana))]
        [InlineData('\u3100', '\u312F', nameof(UnicodeRanges.Bopomofo))]
        [InlineData('\u3130', '\u318F', nameof(UnicodeRanges.HangulCompatibilityJamo))]
        [InlineData('\u3190', '\u319F', nameof(UnicodeRanges.Kanbun))]
        [InlineData('\u31A0', '\u31BF', nameof(UnicodeRanges.BopomofoExtended))]
        [InlineData('\u31C0', '\u31EF', nameof(UnicodeRanges.CJKStrokes))]
        [InlineData('\u31F0', '\u31FF', nameof(UnicodeRanges.KatakanaPhoneticExtensions))]
        [InlineData('\u3200', '\u32FF', nameof(UnicodeRanges.EnclosedCJKLettersandMonths))]
        [InlineData('\u3300', '\u33FF', nameof(UnicodeRanges.CJKCompatibility))]
        [InlineData('\u3400', '\u4DBF', nameof(UnicodeRanges.CJKUnifiedIdeographsExtensionA))]
        [InlineData('\u4DC0', '\u4DFF', nameof(UnicodeRanges.YijingHexagramSymbols))]
        [InlineData('\u4E00', '\u9FFF', nameof(UnicodeRanges.CJKUnifiedIdeographs))]
        [InlineData('\uA000', '\uA48F', nameof(UnicodeRanges.YiSyllables))]
        [InlineData('\uA490', '\uA4CF', nameof(UnicodeRanges.YiRadicals))]
        [InlineData('\uA4D0', '\uA4FF', nameof(UnicodeRanges.Lisu))]
        [InlineData('\uA500', '\uA63F', nameof(UnicodeRanges.Vai))]
        [InlineData('\uA640', '\uA69F', nameof(UnicodeRanges.CyrillicExtendedB))]
        [InlineData('\uA6A0', '\uA6FF', nameof(UnicodeRanges.Bamum))]
        [InlineData('\uA700', '\uA71F', nameof(UnicodeRanges.ModifierToneLetters))]
        [InlineData('\uA720', '\uA7FF', nameof(UnicodeRanges.LatinExtendedD))]
        [InlineData('\uA800', '\uA82F', nameof(UnicodeRanges.SylotiNagri))]
        [InlineData('\uA830', '\uA83F', nameof(UnicodeRanges.CommonIndicNumberForms))]
        [InlineData('\uA840', '\uA87F', nameof(UnicodeRanges.Phagspa))]
        [InlineData('\uA880', '\uA8DF', nameof(UnicodeRanges.Saurashtra))]
        [InlineData('\uA8E0', '\uA8FF', nameof(UnicodeRanges.DevanagariExtended))]
        [InlineData('\uA900', '\uA92F', nameof(UnicodeRanges.KayahLi))]
        [InlineData('\uA930', '\uA95F', nameof(UnicodeRanges.Rejang))]
        [InlineData('\uA960', '\uA97F', nameof(UnicodeRanges.HangulJamoExtendedA))]
        [InlineData('\uA980', '\uA9DF', nameof(UnicodeRanges.Javanese))]
        [InlineData('\uA9E0', '\uA9FF', nameof(UnicodeRanges.MyanmarExtendedB))]
        [InlineData('\uAA00', '\uAA5F', nameof(UnicodeRanges.Cham))]
        [InlineData('\uAA60', '\uAA7F', nameof(UnicodeRanges.MyanmarExtendedA))]
        [InlineData('\uAA80', '\uAADF', nameof(UnicodeRanges.TaiViet))]
        [InlineData('\uAAE0', '\uAAFF', nameof(UnicodeRanges.MeeteiMayekExtensions))]
        [InlineData('\uAB00', '\uAB2F', nameof(UnicodeRanges.EthiopicExtendedA))]
        [InlineData('\uAB30', '\uAB6F', nameof(UnicodeRanges.LatinExtendedE))]
        [InlineData('\uAB70', '\uABBF', nameof(UnicodeRanges.CherokeeSupplement))]
        [InlineData('\uABC0', '\uABFF', nameof(UnicodeRanges.MeeteiMayek))]
        [InlineData('\uAC00', '\uD7AF', nameof(UnicodeRanges.HangulSyllables))]
        [InlineData('\uD7B0', '\uD7FF', nameof(UnicodeRanges.HangulJamoExtendedB))]
        [InlineData('\uF900', '\uFAFF', nameof(UnicodeRanges.CJKCompatibilityIdeographs))]
        [InlineData('\uFB00', '\uFB4F', nameof(UnicodeRanges.AlphabeticPresentationForms))]
        [InlineData('\uFB50', '\uFDFF', nameof(UnicodeRanges.ArabicPresentationFormsA))]
        [InlineData('\uFE00', '\uFE0F', nameof(UnicodeRanges.VariationSelectors))]
        [InlineData('\uFE10', '\uFE1F', nameof(UnicodeRanges.VerticalForms))]
        [InlineData('\uFE20', '\uFE2F', nameof(UnicodeRanges.CombiningHalfMarks))]
        [InlineData('\uFE30', '\uFE4F', nameof(UnicodeRanges.CJKCompatibilityForms))]
        [InlineData('\uFE50', '\uFE6F', nameof(UnicodeRanges.SmallFormVariants))]
        [InlineData('\uFE70', '\uFEFF', nameof(UnicodeRanges.ArabicPresentationFormsB))]
        [InlineData('\uFF00', '\uFFEF', nameof(UnicodeRanges.HalfwidthandFullwidthForms))]
        [InlineData('\uFFF0', '\uFFFF', nameof(UnicodeRanges.Specials))]
        public void Range_Unicode(char first, char last, string blockName)
        {
            Assert.Equal(0x0, first & 0xF); // first char in any block should be U+nnn0
            Assert.Equal(0xF, last & 0xF); // last char in any block should be U+nnnF
            Assert.True(first < last); // code point ranges should be ordered

            var propInfo = typeof(UnicodeRanges).GetProperty(blockName, BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(propInfo);

            UnicodeRange range = (UnicodeRange)propInfo.GetValue(null);
            Assert.NotNull(range);

            // Test 1: the range should span the range first..last
            Assert.Equal(first, range.FirstCodePoint);
            Assert.Equal(last, range.FirstCodePoint + range.RangeSize - 1);

            // Test 2: calling the property multiple times should cache and return the same range instance
            UnicodeRange range2 = (UnicodeRange)propInfo.GetValue(null);
            Assert.Same(range, range2);
        }
    }
}
