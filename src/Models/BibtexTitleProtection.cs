using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Litenbib.Models
{
    public sealed record TitleProtectionTerm(string Canonical, IReadOnlyList<string> Aliases);

    public static class TitleProtectionTerms
    {
        public const string DefaultContent =
            "# Title protection terms\n" +
            "# Format: Canonical=alias1,alias2\n" +
            "# Without \"=\" means exact match only.\n" +
            "# Lines starting with # are ignored.\n" +
            "LaTeX=LaTeX,latex,LATEX\n" +
            "TeX=TeX,tex,TEX\n" +
            "BibTeX=BibTeX,bibtex,BIBTEX\n" +
            "arXiv=arXiv,arxiv,ARXIV\n" +
            "LLM=LLM,llm\n" +
            "AI=AI,ai\n" +
            "ML=ML,ml\n" +
            "NLP=NLP,nlp\n" +
            "GPU=GPU,gpu\n" +
            "CPU=CPU,cpu\n" +
            "PDE=PDE,pde\n" +
            "ODE=ODE,ode\n" +
            "BERT=BERT,bert\n" +
            "GPT=GPT,gpt\n";

        public static void EnsureFileExists()
        {
            Directory.CreateDirectory(AppPaths.ConfigDirectory);
            if (!File.Exists(AppPaths.TitleProtectionTermsPath))
            {
                File.WriteAllText(AppPaths.TitleProtectionTermsPath, DefaultContent);
            }
        }

        public static List<TitleProtectionTerm> Load()
        {
            EnsureFileExists();
            List<TitleProtectionTerm> terms = [];
            foreach (string line in File.ReadLines(AppPaths.TitleProtectionTermsPath))
            {
                TitleProtectionTerm? term = ParseLine(line);
                if (term != null)
                {
                    terms.Add(term);
                }
            }

            return terms
                .DistinctBy(term => term.Canonical, StringComparer.Ordinal)
                .ToList();
        }

        private static TitleProtectionTerm? ParseLine(string line)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
            {
                return null;
            }

            int separatorIndex = trimmed.IndexOf('=');
            string canonical = separatorIndex < 0
                ? trimmed
                : trimmed[..separatorIndex].Trim();
            if (string.IsNullOrWhiteSpace(canonical))
            {
                return null;
            }

            List<string> aliases = [canonical];
            if (separatorIndex >= 0 && separatorIndex < trimmed.Length - 1)
            {
                aliases.AddRange(trimmed[(separatorIndex + 1)..]
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
            }

            return new TitleProtectionTerm(
                canonical,
                aliases.Distinct(StringComparer.Ordinal).ToList());
        }
    }

    public static class BibtexTitleProtection
    {
        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "a",
            "an",
            "the",
            "and",
            "or",
            "but",
            "nor",
            "for",
            "so",
            "yet",
            "of",
            "in",
            "on",
            "at",
            "by",
            "to",
            "from",
            "with",
            "without",
            "as",
            "into",
            "onto",
            "over",
            "under",
            "via",
            "per",
        };

        public static string Protect(
            string title,
            IReadOnlyList<TitleProtectionTerm> terms,
            bool protectTerms,
            bool protectTitleCase)
        {
            string result = title ?? string.Empty;
            if (protectTerms && terms.Count > 0)
            {
                result = ProtectTerms(result, terms);
            }

            if (protectTitleCase)
            {
                result = ProtectTitleCaseInitials(result);
            }

            return result;
        }

        private static string ProtectTerms(string title, IReadOnlyList<TitleProtectionTerm> terms)
        {
            List<(string Alias, string Canonical)> aliases = terms
                .SelectMany(term => term.Aliases.Select(alias => (Alias: alias, term.Canonical)))
                .Where(item => !string.IsNullOrWhiteSpace(item.Alias))
                .OrderByDescending(item => item.Alias.Length)
                .ToList();

            return TransformUnprotected(title, segment => ProtectTermsInSegment(segment, aliases));
        }

        private static string ProtectTermsInSegment(
            string segment,
            IReadOnlyList<(string Alias, string Canonical)> aliases)
        {
            StringBuilder builder = new(segment.Length);
            int index = 0;
            while (index < segment.Length)
            {
                bool matched = false;
                foreach (var (alias, canonical) in aliases)
                {
                    if (index + alias.Length > segment.Length
                        || !segment.AsSpan(index, alias.Length).SequenceEqual(alias))
                    {
                        continue;
                    }

                    if (!HasTermBoundary(segment, index, alias.Length))
                    {
                        continue;
                    }

                    builder.Append('{').Append(canonical).Append('}');
                    index += alias.Length;
                    matched = true;
                    break;
                }

                if (!matched)
                {
                    builder.Append(segment[index]);
                    index++;
                }
            }

            return builder.ToString();
        }

        private static string ProtectTitleCaseInitials(string title)
        {
            return TransformUnprotected(title, ProtectTitleCaseInitialsInSegment);
        }

        private static string ProtectTitleCaseInitialsInSegment(string segment)
        {
            StringBuilder builder = new(segment.Length);
            int index = 0;
            while (index < segment.Length)
            {
                if (!char.IsLetter(segment[index]))
                {
                    builder.Append(segment[index]);
                    index++;
                    continue;
                }

                int start = index;
                while (index < segment.Length && (char.IsLetterOrDigit(segment[index]) || segment[index] == '\''))
                {
                    index++;
                }

                string word = segment[start..index];
                if (ShouldProtectInitial(word))
                {
                    builder.Append('{').Append(word[0]).Append('}').Append(word[1..]);
                }
                else
                {
                    builder.Append(word);
                }
            }

            return builder.ToString();
        }

        private static bool ShouldProtectInitial(string word)
        {
            return word.Length > 0
                && char.IsUpper(word[0])
                && !StopWords.Contains(word);
        }

        private static string TransformUnprotected(string value, Func<string, string> transform)
        {
            StringBuilder builder = new(value.Length);
            StringBuilder segment = new();
            int braceDepth = 0;

            for (int index = 0; index < value.Length; index++)
            {
                char current = value[index];
                if (current == '{' && !IsEscaped(value, index))
                {
                    if (braceDepth == 0)
                    {
                        builder.Append(transform(segment.ToString()));
                        segment.Clear();
                    }

                    braceDepth++;
                    builder.Append(current);
                    continue;
                }

                if (current == '}' && !IsEscaped(value, index) && braceDepth > 0)
                {
                    braceDepth--;
                    builder.Append(current);
                    continue;
                }

                if (braceDepth == 0)
                {
                    segment.Append(current);
                }
                else
                {
                    builder.Append(current);
                }
            }

            builder.Append(transform(segment.ToString()));
            return builder.ToString();
        }

        private static bool HasTermBoundary(string value, int index, int length)
        {
            char first = value[index];
            char last = value[index + length - 1];
            bool hasLeftBoundary = !IsTokenChar(first)
                || index == 0
                || !IsTokenChar(value[index - 1]);
            bool hasRightBoundary = !IsTokenChar(last)
                || index + length >= value.Length
                || !IsTokenChar(value[index + length]);
            return hasLeftBoundary && hasRightBoundary;
        }

        private static bool IsTokenChar(char value)
        {
            return char.IsLetterOrDigit(value);
        }

        private static bool IsEscaped(string value, int index)
        {
            int slashCount = 0;
            for (int cursor = index - 1; cursor >= 0 && value[cursor] == '\\'; cursor--)
            {
                slashCount++;
            }

            return slashCount % 2 == 1;
        }
    }
}
