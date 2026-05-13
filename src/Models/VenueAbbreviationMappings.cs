using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Litenbib.Models
{
    public sealed record VenueAbbreviationMapping(string Abbreviation, string FullName);

    public static class VenueAbbreviationMappings
    {
        public const string DefaultContent = "NeurIPS=Advances in Neural Information Processing Systems\n";

        public static void EnsureFileExists()
        {
            Directory.CreateDirectory(AppPaths.ConfigDirectory);
            if (!File.Exists(AppPaths.AbbreviationMappingsPath))
            {
                File.WriteAllText(AppPaths.AbbreviationMappingsPath, DefaultContent);
            }
        }

        public static List<VenueAbbreviationMapping> Load()
        {
            EnsureFileExists();
            List<VenueAbbreviationMapping> mappings = [];
            foreach (string line in File.ReadLines(AppPaths.AbbreviationMappingsPath))
            {
                string trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
                {
                    continue;
                }

                int separatorIndex = trimmed.IndexOf('=');
                if (separatorIndex <= 0 || separatorIndex >= trimmed.Length - 1)
                {
                    continue;
                }

                string abbreviation = trimmed[..separatorIndex].Trim();
                string fullName = trimmed[(separatorIndex + 1)..].Trim();
                if (string.IsNullOrWhiteSpace(abbreviation) || string.IsNullOrWhiteSpace(fullName))
                {
                    continue;
                }

                mappings.Add(new VenueAbbreviationMapping(abbreviation, fullName));
            }

            return mappings
                .DistinctBy(mapping => mapping.Abbreviation, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static string ToPromptReferenceTable(IEnumerable<VenueAbbreviationMapping> mappings)
        {
            return string.Join('\n', mappings.Select(mapping => $"{mapping.Abbreviation}={mapping.FullName}"));
        }
    }
}
