namespace Litenbib.Models
{
    internal static class AiPrompts
    {
        private const int MaxPdfFirstPageTextLength = 12000;
        private const int MaxReferenceTextLength = 30000;

        public const string PdfFirstPageToBibtexSystem =
            "You are a careful bibliography extraction assistant. Extract exactly one BibTeX entry from paper first-page text. Return only a valid BibTeX entry, with no markdown fences and no explanation. Prefer @article, @inproceedings, or @misc when the venue type is unclear. Use only information supported by the text. Use BibTeX author formatting with names separated by ' and '. Preserve DOI, arXiv ID, URL, title, authors, year, journal, booktitle, volume, number, pages, and publisher when present. If there is not enough information to create a useful entry, return EMPTY.";

        public const string ReferenceTextToBibtexSystem =
            "You are a careful bibliography extraction assistant. Convert pasted reference text into BibTeX entries. The text may contain one reference, multiple references, or noisy prose copied from papers or websites. Return only valid BibTeX entries, with no markdown fences and no explanation. Create one entry per distinct reference. Prefer @article, @inproceedings, @book, or @misc when the venue type is unclear. Use only information supported by the text. Use BibTeX author formatting with names separated by ' and '. Preserve DOI, arXiv ID, URL, title, authors, year, journal, booktitle, volume, number, pages, and publisher when present. If there are no recognizable references, return EMPTY.";

        public static string BuildPdfFirstPageToBibtexUserPrompt(string firstPageText)
        {
            string text = firstPageText?.Trim() ?? string.Empty;
            if (text.Length > MaxPdfFirstPageTextLength)
            {
                text = text[..MaxPdfFirstPageTextLength];
            }

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
            {
                text = text[..MaxReferenceTextLength];
            }

            return
                "Convert the following pasted reference text into BibTeX entries.\n\n" +
                "REFERENCE_TEXT_BEGIN\n" +
                text +
                "\nREFERENCE_TEXT_END";
        }
    }
}
