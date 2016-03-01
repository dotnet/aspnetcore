using System.Linq;
using System.Text.RegularExpressions;

namespace MusicStore.Models
{
    // Obviously this is not a serious sentiment analyser. It is only here to provide an amusing demonstration of cross-property
    // validation in AlbumsApiController.
    public static class SentimentAnalysis
    {
        private static string[] positiveSentimentWords = new[] { "happy", "fun", "joy", "love", "delight", "bunny", "bunnies", "asp.net" };

        private static string[] negativeSentimentWords = new[] { "sad", "pain", "despair", "hate", "scorn", "death", "package management" };

        public static SentimentResult GetSentiment(string text) {
            var numPositiveWords = CountWordOccurrences(text, positiveSentimentWords);
            var numNegativeWords = CountWordOccurrences(text, negativeSentimentWords);
            if (numPositiveWords > numNegativeWords) {
                return SentimentResult.Positive;
            } else if (numNegativeWords > numPositiveWords) {
                return SentimentResult.Negative;
            } else {
                return SentimentResult.Neutral;
            }
        }

        private static int CountWordOccurrences(string text, string[] words)
        {
            // Very simplistic matching technique for this sample. Not scalable and not really even correct.
            return new Regex(string.Join("|", words), RegexOptions.IgnoreCase).Matches(text).Count;
        }

        public enum SentimentResult {
            Negative,
            Neutral,
            Positive,
        }
    }
}
