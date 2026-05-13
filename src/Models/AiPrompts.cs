using System.Collections.Generic;
using System.Linq;

namespace Litenbib.Models
{
    internal static class AiPrompts
    {
        private const int MaxPdfFirstPageTextLength = 15000;
        private const int MaxReferenceTextLength = 30000;
        private const int MaxVenuePromptTextLength = 30000;

        public const string PdfFirstPageToBibtexSystem =
            "You are a careful bibliography extraction assistant. Extract exactly one BibTeX entry from paper first-page text. Return only a valid BibTeX entry, with no markdown fences and no explanation. Prefer @article, @inproceedings, or @misc when the venue type is unclear. Use only information supported by the text. Use BibTeX author formatting with names separated by ' and '. Preserve DOI, arXiv ID, URL, title, authors, year, journal, booktitle, volume, number, pages, and publisher when present. If there is not enough information to create a useful entry, return EMPTY.";

        public const string ReferenceTextToBibtexSystem =
            "You are a careful bibliography extraction assistant. Convert pasted reference text into BibTeX entries. The text may contain one reference, multiple references, or noisy prose copied from papers or websites. Return only valid BibTeX entries, with no markdown fences and no explanation. Create one entry per distinct reference. Prefer @article, @inproceedings, @book, or @misc when the venue type is unclear. Use only information supported by the text. Use BibTeX author formatting with names separated by ' and '. Preserve DOI, arXiv ID, URL, title, authors, year, journal, booktitle, volume, number, pages, and publisher when present. If there are no recognizable references, return EMPTY.";

        public const string VenueNameNormalizationSystem =
            "You normalize journal and conference venue names using only the user's abbreviation mapping table. Return only a numbered list. Do not add explanations, markdown, code fences, headings, or extra lines. The output must contain exactly one line for each input item, in the same order, using the format \"1. value\". If an item has no confident mapping in the table, return the original item unchanged. Never invent abbreviations or full names that are not in the table.";

        public static string BuildPdfFirstPageToBibtexUserPrompt(string firstPageText)
        {
            string text = firstPageText?.Trim() ?? string.Empty;
            if (text.Length > MaxPdfFirstPageTextLength)
            { text = text[..MaxPdfFirstPageTextLength]; }

            return
                "Extract a single BibTeX entry from the following first-page text.\n\n" +
                "PDF_FIRST_PAGE_TEXT_BEGIN\n" +
                text +
                "\nPDF_FIRST_PAGE_TEXT_END";
        }

        public static string BuildReferenceTextToBibtexUserPrompt(string referenceText)
        {
            string text = referenceText?.Trim() ?? string.Empty;
            if (text.Length > MaxReferenceTextLength)
            { text = text[..MaxReferenceTextLength]; }

            return
                "Convert the following pasted reference text into BibTeX entries.\n\n" +
                "REFERENCE_TEXT_BEGIN\n" +
                text +
                "\nREFERENCE_TEXT_END";
        }

        public static string BuildVenueNameNormalizationUserPrompt(
            string mode,
            string referenceTable,
            IReadOnlyList<string> venueNames)
        {
            string table = referenceTable?.Trim() ?? string.Empty;
            string items = string.Join('\n', venueNames.Select((name, index) => $"{index + 1}. {name}"));
            string prompt =
                "Normalize the following venue names.\n\n" +
                $"MODE: {mode}\n\n" +
                "REFERENCE_TABLE_BEGIN\n" +
                table +
                "\nREFERENCE_TABLE_END\n\n" +
                "INPUT_LIST_BEGIN\n" +
                items +
                "\nINPUT_LIST_END\n\n" +
                "Rules:\n" +
                "- For EXPAND, convert abbreviations to full names using REFERENCE_TABLE.\n" +
                "- For ABBREVIATE, convert full names to abbreviations using REFERENCE_TABLE.\n" +
                "- Return exactly the same number of numbered lines as INPUT_LIST.\n" +
                "- Preserve input order.\n" +
                "- Return the original item unchanged when there is no confident mapping.\n" +
                "- Return only the numbered list.";

            return prompt.Length > MaxVenuePromptTextLength
                ? prompt[..MaxVenuePromptTextLength]
                : prompt;
        }
    }
}
