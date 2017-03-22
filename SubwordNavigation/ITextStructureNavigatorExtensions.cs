using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace VisualStudio.SubwordNavigation {
    public static class ITextStructureNavigatorExtensions {
        enum Category {
            kLetterUpper,
            kLetterLower,
            kDigit,
            kWhitespace,
            kOther,
        }

        static Category Categorize(char c) {
            if (char.IsUpper(c))
                return Category.kLetterUpper;

            if (char.IsLower(c))
                return Category.kLetterLower;

            if (char.IsDigit(c))
                return Category.kDigit;

            if (char.IsWhiteSpace(c))
                return Category.kWhitespace;

            return Category.kOther;
        }

        static bool IsSubwordBoundary(string span_text, int offset, bool forward) {
            if (offset >= span_text.Length)
                return true;

            if (offset == 0)
                return false;

            char chLeft = span_text[offset - 1];
            char chRight = span_text[offset];

            if (chLeft == '\n' || chRight == '\n')
                return true;

            var catLeft = Categorize(chLeft);
            var catRight = Categorize(chRight);

            if (catLeft == catRight) {
                if (catLeft != Category.kLetterUpper)
                    return false;

                if (offset == span_text.Length - 1)
                    return false;

                char chRight2 = span_text[offset + 1];
                var catRight2 = Categorize(chRight2);
                if (catRight2 == Category.kLetterLower)
                    return true;

                return false;
            }

            switch (catLeft) {
                case Category.kLetterUpper:
                    switch (catRight) {
                        case Category.kLetterLower:
                            return false;
                        case Category.kDigit:
                            return true;
                        case Category.kWhitespace:
                        case Category.kOther:
                            return forward;
                    }
                    break;

                case Category.kLetterLower:
                    switch (catRight) {
                        case Category.kLetterUpper:
                        case Category.kDigit:
                            return true;
                        case Category.kWhitespace:
                        case Category.kOther:
                            return forward;
                    }
                    break;

                case Category.kDigit:
                    switch (catRight) {
                        case Category.kLetterUpper:
                        case Category.kLetterLower:
                            return true;
                        case Category.kWhitespace:
                        case Category.kOther:
                            return forward;
                    }
                    break;

                case Category.kOther:
                    switch (catRight) {
                        case Category.kLetterUpper:
                        case Category.kLetterLower:
                        case Category.kDigit:
                        case Category.kWhitespace:
                            return !forward;
                    }
                    break;

                case Category.kWhitespace:
                    switch (catRight) {
                        case Category.kLetterUpper:
                        case Category.kLetterLower:
                        case Category.kDigit:
                            return !forward;
                        case Category.kOther:
                            return forward;
                    }
                    break;
            }

            return false;
        }

        public static int? GetSubwordBoundary(this ITextStructureNavigator navigator, SnapshotPoint point, bool forward) {
            var wordSpan = navigator.GetExtentOfWord(point).Span;
            wordSpan = new SnapshotSpan(navigator.GetSpanOfPreviousSibling(wordSpan).Start,
                                        navigator.GetSpanOfNextSibling(wordSpan).End);
            if (wordSpan.Length == 0)
                return null;

            int step = forward ? 1 : -1;
            SnapshotSpan wordSpanPrev;

            do {
                var word = wordSpan.GetText();
                int i = point.Position - wordSpan.Start;

                while (0 < i && i < word.Length) {
                    if (IsSubwordBoundary(word, i, forward))
                        return wordSpan.Start + i;
                    i += step;
                }

                point = wordSpan.Start + i;

                wordSpanPrev = wordSpan;
                wordSpan = navigator.GetSpanOfEnclosing(wordSpan);
            } while (wordSpan != wordSpanPrev);

            return null;
        }
    }
}
