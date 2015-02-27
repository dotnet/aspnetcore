// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Xunit;

namespace Microsoft.Framework.WebEncoders
{
    public class UnicodeBlocksTests
    {
        [Fact]
        public void Block_None()
        {
            UnicodeBlock block = UnicodeBlocks.None;
            Assert.NotNull(block);

            // Test 1: the block should be empty
            Assert.Equal(0, block.FirstCodePoint);
            Assert.Equal(0, block.BlockSize);

            // Test 2: calling the property multiple times should cache and return the same block instance
            UnicodeBlock block2 = UnicodeBlocks.None;
            Assert.Same(block, block2);
        }

        [Fact]
        public void Block_All()
        {
            Block_Unicode('\u0000', '\uFFFF', nameof(UnicodeBlocks.All));
        }

        [Theory]
        [InlineData('\u0000', '\u007F', nameof(UnicodeBlocks.BasicLatin))]
        [InlineData('\u0080', '\u00FF', nameof(UnicodeBlocks.Latin1Supplement))]
        [InlineData('\u0100', '\u017F', nameof(UnicodeBlocks.LatinExtendedA))]
        [InlineData('\u0180', '\u024F', nameof(UnicodeBlocks.LatinExtendedB))]
        [InlineData('\u0250', '\u02AF', nameof(UnicodeBlocks.IPAExtensions))]
        [InlineData('\u02B0', '\u02FF', nameof(UnicodeBlocks.SpacingModifierLetters))]
        [InlineData('\u0300', '\u036F', nameof(UnicodeBlocks.CombiningDiacriticalMarks))]
        [InlineData('\u0370', '\u03FF', nameof(UnicodeBlocks.GreekandCoptic))]
        [InlineData('\u0400', '\u04FF', nameof(UnicodeBlocks.Cyrillic))]
        [InlineData('\u0500', '\u052F', nameof(UnicodeBlocks.CyrillicSupplement))]
        [InlineData('\u0530', '\u058F', nameof(UnicodeBlocks.Armenian))]
        [InlineData('\u0590', '\u05FF', nameof(UnicodeBlocks.Hebrew))]
        [InlineData('\u0600', '\u06FF', nameof(UnicodeBlocks.Arabic))]
        [InlineData('\u0700', '\u074F', nameof(UnicodeBlocks.Syriac))]
        [InlineData('\u0750', '\u077F', nameof(UnicodeBlocks.ArabicSupplement))]
        [InlineData('\u0780', '\u07BF', nameof(UnicodeBlocks.Thaana))]
        [InlineData('\u07C0', '\u07FF', nameof(UnicodeBlocks.NKo))]
        [InlineData('\u0800', '\u083F', nameof(UnicodeBlocks.Samaritan))]
        [InlineData('\u0840', '\u085F', nameof(UnicodeBlocks.Mandaic))]
        [InlineData('\u08A0', '\u08FF', nameof(UnicodeBlocks.ArabicExtendedA))]
        [InlineData('\u0900', '\u097F', nameof(UnicodeBlocks.Devanagari))]
        [InlineData('\u0980', '\u09FF', nameof(UnicodeBlocks.Bengali))]
        [InlineData('\u0A00', '\u0A7F', nameof(UnicodeBlocks.Gurmukhi))]
        [InlineData('\u0A80', '\u0AFF', nameof(UnicodeBlocks.Gujarati))]
        [InlineData('\u0B00', '\u0B7F', nameof(UnicodeBlocks.Oriya))]
        [InlineData('\u0B80', '\u0BFF', nameof(UnicodeBlocks.Tamil))]
        [InlineData('\u0C00', '\u0C7F', nameof(UnicodeBlocks.Telugu))]
        [InlineData('\u0C80', '\u0CFF', nameof(UnicodeBlocks.Kannada))]
        [InlineData('\u0D00', '\u0D7F', nameof(UnicodeBlocks.Malayalam))]
        [InlineData('\u0D80', '\u0DFF', nameof(UnicodeBlocks.Sinhala))]
        [InlineData('\u0E00', '\u0E7F', nameof(UnicodeBlocks.Thai))]
        [InlineData('\u0E80', '\u0EFF', nameof(UnicodeBlocks.Lao))]
        [InlineData('\u0F00', '\u0FFF', nameof(UnicodeBlocks.Tibetan))]
        [InlineData('\u1000', '\u109F', nameof(UnicodeBlocks.Myanmar))]
        [InlineData('\u10A0', '\u10FF', nameof(UnicodeBlocks.Georgian))]
        [InlineData('\u1100', '\u11FF', nameof(UnicodeBlocks.HangulJamo))]
        [InlineData('\u1200', '\u137F', nameof(UnicodeBlocks.Ethiopic))]
        [InlineData('\u1380', '\u139F', nameof(UnicodeBlocks.EthiopicSupplement))]
        [InlineData('\u13A0', '\u13FF', nameof(UnicodeBlocks.Cherokee))]
        [InlineData('\u1400', '\u167F', nameof(UnicodeBlocks.UnifiedCanadianAboriginalSyllabics))]
        [InlineData('\u1680', '\u169F', nameof(UnicodeBlocks.Ogham))]
        [InlineData('\u16A0', '\u16FF', nameof(UnicodeBlocks.Runic))]
        [InlineData('\u1700', '\u171F', nameof(UnicodeBlocks.Tagalog))]
        [InlineData('\u1720', '\u173F', nameof(UnicodeBlocks.Hanunoo))]
        [InlineData('\u1740', '\u175F', nameof(UnicodeBlocks.Buhid))]
        [InlineData('\u1760', '\u177F', nameof(UnicodeBlocks.Tagbanwa))]
        [InlineData('\u1780', '\u17FF', nameof(UnicodeBlocks.Khmer))]
        [InlineData('\u1800', '\u18AF', nameof(UnicodeBlocks.Mongolian))]
        [InlineData('\u18B0', '\u18FF', nameof(UnicodeBlocks.UnifiedCanadianAboriginalSyllabicsExtended))]
        [InlineData('\u1900', '\u194F', nameof(UnicodeBlocks.Limbu))]
        [InlineData('\u1950', '\u197F', nameof(UnicodeBlocks.TaiLe))]
        [InlineData('\u1980', '\u19DF', nameof(UnicodeBlocks.NewTaiLue))]
        [InlineData('\u19E0', '\u19FF', nameof(UnicodeBlocks.KhmerSymbols))]
        [InlineData('\u1A00', '\u1A1F', nameof(UnicodeBlocks.Buginese))]
        [InlineData('\u1A20', '\u1AAF', nameof(UnicodeBlocks.TaiTham))]
        [InlineData('\u1AB0', '\u1AFF', nameof(UnicodeBlocks.CombiningDiacriticalMarksExtended))]
        [InlineData('\u1B00', '\u1B7F', nameof(UnicodeBlocks.Balinese))]
        [InlineData('\u1B80', '\u1BBF', nameof(UnicodeBlocks.Sundanese))]
        [InlineData('\u1BC0', '\u1BFF', nameof(UnicodeBlocks.Batak))]
        [InlineData('\u1C00', '\u1C4F', nameof(UnicodeBlocks.Lepcha))]
        [InlineData('\u1C50', '\u1C7F', nameof(UnicodeBlocks.OlChiki))]
        [InlineData('\u1CC0', '\u1CCF', nameof(UnicodeBlocks.SundaneseSupplement))]
        [InlineData('\u1CD0', '\u1CFF', nameof(UnicodeBlocks.VedicExtensions))]
        [InlineData('\u1D00', '\u1D7F', nameof(UnicodeBlocks.PhoneticExtensions))]
        [InlineData('\u1D80', '\u1DBF', nameof(UnicodeBlocks.PhoneticExtensionsSupplement))]
        [InlineData('\u1DC0', '\u1DFF', nameof(UnicodeBlocks.CombiningDiacriticalMarksSupplement))]
        [InlineData('\u1E00', '\u1EFF', nameof(UnicodeBlocks.LatinExtendedAdditional))]
        [InlineData('\u1F00', '\u1FFF', nameof(UnicodeBlocks.GreekExtended))]
        [InlineData('\u2000', '\u206F', nameof(UnicodeBlocks.GeneralPunctuation))]
        [InlineData('\u2070', '\u209F', nameof(UnicodeBlocks.SuperscriptsandSubscripts))]
        [InlineData('\u20A0', '\u20CF', nameof(UnicodeBlocks.CurrencySymbols))]
        [InlineData('\u20D0', '\u20FF', nameof(UnicodeBlocks.CombiningDiacriticalMarksforSymbols))]
        [InlineData('\u2100', '\u214F', nameof(UnicodeBlocks.LetterlikeSymbols))]
        [InlineData('\u2150', '\u218F', nameof(UnicodeBlocks.NumberForms))]
        [InlineData('\u2190', '\u21FF', nameof(UnicodeBlocks.Arrows))]
        [InlineData('\u2200', '\u22FF', nameof(UnicodeBlocks.MathematicalOperators))]
        [InlineData('\u2300', '\u23FF', nameof(UnicodeBlocks.MiscellaneousTechnical))]
        [InlineData('\u2400', '\u243F', nameof(UnicodeBlocks.ControlPictures))]
        [InlineData('\u2440', '\u245F', nameof(UnicodeBlocks.OpticalCharacterRecognition))]
        [InlineData('\u2460', '\u24FF', nameof(UnicodeBlocks.EnclosedAlphanumerics))]
        [InlineData('\u2500', '\u257F', nameof(UnicodeBlocks.BoxDrawing))]
        [InlineData('\u2580', '\u259F', nameof(UnicodeBlocks.BlockElements))]
        [InlineData('\u25A0', '\u25FF', nameof(UnicodeBlocks.GeometricShapes))]
        [InlineData('\u2600', '\u26FF', nameof(UnicodeBlocks.MiscellaneousSymbols))]
        [InlineData('\u2700', '\u27BF', nameof(UnicodeBlocks.Dingbats))]
        [InlineData('\u27C0', '\u27EF', nameof(UnicodeBlocks.MiscellaneousMathematicalSymbolsA))]
        [InlineData('\u27F0', '\u27FF', nameof(UnicodeBlocks.SupplementalArrowsA))]
        [InlineData('\u2800', '\u28FF', nameof(UnicodeBlocks.BraillePatterns))]
        [InlineData('\u2900', '\u297F', nameof(UnicodeBlocks.SupplementalArrowsB))]
        [InlineData('\u2980', '\u29FF', nameof(UnicodeBlocks.MiscellaneousMathematicalSymbolsB))]
        [InlineData('\u2A00', '\u2AFF', nameof(UnicodeBlocks.SupplementalMathematicalOperators))]
        [InlineData('\u2B00', '\u2BFF', nameof(UnicodeBlocks.MiscellaneousSymbolsandArrows))]
        [InlineData('\u2C00', '\u2C5F', nameof(UnicodeBlocks.Glagolitic))]
        [InlineData('\u2C60', '\u2C7F', nameof(UnicodeBlocks.LatinExtendedC))]
        [InlineData('\u2C80', '\u2CFF', nameof(UnicodeBlocks.Coptic))]
        [InlineData('\u2D00', '\u2D2F', nameof(UnicodeBlocks.GeorgianSupplement))]
        [InlineData('\u2D30', '\u2D7F', nameof(UnicodeBlocks.Tifinagh))]
        [InlineData('\u2D80', '\u2DDF', nameof(UnicodeBlocks.EthiopicExtended))]
        [InlineData('\u2DE0', '\u2DFF', nameof(UnicodeBlocks.CyrillicExtendedA))]
        [InlineData('\u2E00', '\u2E7F', nameof(UnicodeBlocks.SupplementalPunctuation))]
        [InlineData('\u2E80', '\u2EFF', nameof(UnicodeBlocks.CJKRadicalsSupplement))]
        [InlineData('\u2F00', '\u2FDF', nameof(UnicodeBlocks.KangxiRadicals))]
        [InlineData('\u2FF0', '\u2FFF', nameof(UnicodeBlocks.IdeographicDescriptionCharacters))]
        [InlineData('\u3000', '\u303F', nameof(UnicodeBlocks.CJKSymbolsandPunctuation))]
        [InlineData('\u3040', '\u309F', nameof(UnicodeBlocks.Hiragana))]
        [InlineData('\u30A0', '\u30FF', nameof(UnicodeBlocks.Katakana))]
        [InlineData('\u3100', '\u312F', nameof(UnicodeBlocks.Bopomofo))]
        [InlineData('\u3130', '\u318F', nameof(UnicodeBlocks.HangulCompatibilityJamo))]
        [InlineData('\u3190', '\u319F', nameof(UnicodeBlocks.Kanbun))]
        [InlineData('\u31A0', '\u31BF', nameof(UnicodeBlocks.BopomofoExtended))]
        [InlineData('\u31C0', '\u31EF', nameof(UnicodeBlocks.CJKStrokes))]
        [InlineData('\u31F0', '\u31FF', nameof(UnicodeBlocks.KatakanaPhoneticExtensions))]
        [InlineData('\u3200', '\u32FF', nameof(UnicodeBlocks.EnclosedCJKLettersandMonths))]
        [InlineData('\u3300', '\u33FF', nameof(UnicodeBlocks.CJKCompatibility))]
        [InlineData('\u3400', '\u4DBF', nameof(UnicodeBlocks.CJKUnifiedIdeographsExtensionA))]
        [InlineData('\u4DC0', '\u4DFF', nameof(UnicodeBlocks.YijingHexagramSymbols))]
        [InlineData('\u4E00', '\u9FFF', nameof(UnicodeBlocks.CJKUnifiedIdeographs))]
        [InlineData('\uA000', '\uA48F', nameof(UnicodeBlocks.YiSyllables))]
        [InlineData('\uA490', '\uA4CF', nameof(UnicodeBlocks.YiRadicals))]
        [InlineData('\uA4D0', '\uA4FF', nameof(UnicodeBlocks.Lisu))]
        [InlineData('\uA500', '\uA63F', nameof(UnicodeBlocks.Vai))]
        [InlineData('\uA640', '\uA69F', nameof(UnicodeBlocks.CyrillicExtendedB))]
        [InlineData('\uA6A0', '\uA6FF', nameof(UnicodeBlocks.Bamum))]
        [InlineData('\uA700', '\uA71F', nameof(UnicodeBlocks.ModifierToneLetters))]
        [InlineData('\uA720', '\uA7FF', nameof(UnicodeBlocks.LatinExtendedD))]
        [InlineData('\uA800', '\uA82F', nameof(UnicodeBlocks.SylotiNagri))]
        [InlineData('\uA830', '\uA83F', nameof(UnicodeBlocks.CommonIndicNumberForms))]
        [InlineData('\uA840', '\uA87F', nameof(UnicodeBlocks.Phagspa))]
        [InlineData('\uA880', '\uA8DF', nameof(UnicodeBlocks.Saurashtra))]
        [InlineData('\uA8E0', '\uA8FF', nameof(UnicodeBlocks.DevanagariExtended))]
        [InlineData('\uA900', '\uA92F', nameof(UnicodeBlocks.KayahLi))]
        [InlineData('\uA930', '\uA95F', nameof(UnicodeBlocks.Rejang))]
        [InlineData('\uA960', '\uA97F', nameof(UnicodeBlocks.HangulJamoExtendedA))]
        [InlineData('\uA980', '\uA9DF', nameof(UnicodeBlocks.Javanese))]
        [InlineData('\uA9E0', '\uA9FF', nameof(UnicodeBlocks.MyanmarExtendedB))]
        [InlineData('\uAA00', '\uAA5F', nameof(UnicodeBlocks.Cham))]
        [InlineData('\uAA60', '\uAA7F', nameof(UnicodeBlocks.MyanmarExtendedA))]
        [InlineData('\uAA80', '\uAADF', nameof(UnicodeBlocks.TaiViet))]
        [InlineData('\uAAE0', '\uAAFF', nameof(UnicodeBlocks.MeeteiMayekExtensions))]
        [InlineData('\uAB00', '\uAB2F', nameof(UnicodeBlocks.EthiopicExtendedA))]
        [InlineData('\uAB30', '\uAB6F', nameof(UnicodeBlocks.LatinExtendedE))]
        [InlineData('\uABC0', '\uABFF', nameof(UnicodeBlocks.MeeteiMayek))]
        [InlineData('\uAC00', '\uD7AF', nameof(UnicodeBlocks.HangulSyllables))]
        [InlineData('\uD7B0', '\uD7FF', nameof(UnicodeBlocks.HangulJamoExtendedB))]
        [InlineData('\uF900', '\uFAFF', nameof(UnicodeBlocks.CJKCompatibilityIdeographs))]
        [InlineData('\uFB00', '\uFB4F', nameof(UnicodeBlocks.AlphabeticPresentationForms))]
        [InlineData('\uFB50', '\uFDFF', nameof(UnicodeBlocks.ArabicPresentationFormsA))]
        [InlineData('\uFE00', '\uFE0F', nameof(UnicodeBlocks.VariationSelectors))]
        [InlineData('\uFE10', '\uFE1F', nameof(UnicodeBlocks.VerticalForms))]
        [InlineData('\uFE20', '\uFE2F', nameof(UnicodeBlocks.CombiningHalfMarks))]
        [InlineData('\uFE30', '\uFE4F', nameof(UnicodeBlocks.CJKCompatibilityForms))]
        [InlineData('\uFE50', '\uFE6F', nameof(UnicodeBlocks.SmallFormVariants))]
        [InlineData('\uFE70', '\uFEFF', nameof(UnicodeBlocks.ArabicPresentationFormsB))]
        [InlineData('\uFF00', '\uFFEF', nameof(UnicodeBlocks.HalfwidthandFullwidthForms))]
        [InlineData('\uFFF0', '\uFFFF', nameof(UnicodeBlocks.Specials))]
        public void Block_Unicode(char first, char last, string blockName)
        {
            Assert.Equal(0x0, first & 0xF); // first char in any block should be U+nnn0
            Assert.Equal(0xF, last & 0xF); // last char in any block should be U+nnnF
            Assert.True(first < last); // code point ranges should be ordered

            var propInfo = typeof(UnicodeBlocks).GetProperty(blockName, BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(propInfo);

            UnicodeBlock block = (UnicodeBlock)propInfo.GetValue(null);
            Assert.NotNull(block);

            // Test 1: the block should span the range first..last
            Assert.Equal(first, block.FirstCodePoint);
            Assert.Equal(last, block.FirstCodePoint + block.BlockSize - 1);

            // Test 2: calling the property multiple times should cache and return the same block instance
            UnicodeBlock block2 = (UnicodeBlock)propInfo.GetValue(null);
            Assert.Same(block, block2);
        }
    }
}
