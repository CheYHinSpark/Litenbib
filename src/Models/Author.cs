using System;
using System.Collections.Generic;
using System.Linq;

namespace Litenbib.Models
{
    public class Author(string familyName, string givenName)
    {
        public string FamilyName { get; set; } = familyName;
        public string GivenName { get; set; } = givenName;

        /// <summary> Return fullname as "GivenName FamilyName" </summary>
        public string FullName { get => GivenName + " " + FamilyName; }

        /// <summary> Return fullname as "G. FamilyName" </summary>
        public string FullNameShort
        {
            get
            {
                string result = "";
                foreach (var g in GivenName.Split(' '))
                { result += g[0] + ". "; }
                result += FamilyName;
                return result;
            }
        }

        /// <summary> Return fullname as "FamilyName, GivenName" </summary>
        public string FullNameFamilyFirst { get => FamilyName + ", " + GivenName; }

        /// <summary> Return fullname as "FamilyName, G." </summary>
        public string FullNameFamilyFirstShort
        {
            get
            {
                string result = FamilyName + ",";
                foreach (var g in GivenName.Split(' '))
                { result += " " + g[0] + "."; }
                return result;
            }
        }

        public Author(string fullName) : this("", "")
        {
            if (fullName.Contains(','))
            {
                var parts = fullName.Split(',', 2);
                FamilyName = parts[0].Trim();
                GivenName = parts[1].Trim();
            }
            else
            {
                var parts = fullName.Split(' ', 2);
                if (parts.Length < 2)
                {
                    FamilyName = parts[0].Trim();
                    return;
                }
                FamilyName = parts[1].Trim();
                GivenName = parts[0].Trim();
            }
        }

        public static List<Author> Authors(string authors)
        { return [.. authors.Split(" and ", StringSplitOptions.RemoveEmptyEntries).Select(a => new Author(a.Trim()))]; }

        public static (string, string) GetFamilyGiven(string fullname, int abbreviation)
        {
            string family, given;
            if (fullname.LastIndexOf(',') is int i && i > -1)
            {
                family = fullname[..i];
                given = fullname[(i + 1)..];
            }
            else if (fullname.LastIndexOf(' ') is int e && e > -1)
            {
                given = fullname[..e];
                family = fullname[(e + 1)..];
            }
            else
            {
                family = fullname;
                given = "";
            }
            if (abbreviation == 1)
            {
                var parts = given.Trim().Replace("{", "").Replace("}", "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
                for (int j = 0; j < parts.Length; j++)
                {
                    var sub_parts = parts[j].Split('-', StringSplitOptions.RemoveEmptyEntries);
                    for (int k = 0; k < sub_parts.Length; k++)
                    { sub_parts[k] = sub_parts[k][0] + "."; }
                    parts[j] = string.Join("{-}", sub_parts);
                }
                given = string.Join(" ", parts);
            }
            return (family.Trim(), given);
        }
    }
}
