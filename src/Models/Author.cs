using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Litenbib.Models
{
    public class Author(string lastName, string firstName)
    {
        public string LastName { get; set; } = lastName;
        public string FirstName { get; set; } = firstName;

        /// <summary> Return fullname as "Firstname Lastname" </summary>
        public string FullName { get => FirstName + " " + LastName; }

        /// <summary> Return fullname as "F. Lastname" </summary>
        public string FullNameShort
        {
            get 
            {
                var fl = FirstName.Split(' ');
                string result = "";
                foreach (var f in fl)
                { result += f[0] + ". "; }
                result += LastName;
                return result;
            }
        }

        /// <summary> Return fullname as "Lastname, Firstname" </summary>
        public string FullNameSurFirst { get => LastName + ", " + FirstName; }

        /// <summary> Return fullname as "Lastname, F." </summary>
        public string FullNameSurFirstShort
        {
            get
            {
                var fl = FirstName.Split(' ');
                string result = LastName + ",";
                foreach (var f in fl)
                { result += " " + f[0] + "."; }
                return result;
            }
        }
    }
}
