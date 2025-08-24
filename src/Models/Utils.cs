using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Litenbib.Models
{
    internal class BibFile
    {
        public static string Read(string path)
        {
            return File.ReadAllText(path);
        }

        public static void Write(string path, string content)
        {

        }
    }
}
