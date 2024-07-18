using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TC.Functions
{
    // Random Functions (14.11.2022, SRM)
    public static class RandomFC
    {
        #region Random Object

        // Random Object (31.10.2022, SRM)
        private static Random _Random = new Random();

        #endregion

        #region Random Number

        // Get Random Number (31.10.2022, SRM)
        public static int GetRandomNumber()
        {
            return _Random.Next();
        }

        // Get Random Number between Min and Max (31.10.2022, SRM)
        public static int GetRandomNumber(int min, int max)
        {
            // for some reason, max-number will never be reached => that's why max + 1
            return _Random.Next(min, max + 1);
        }

        #endregion

        #region Random Entry

        // Get Random Entry of IEnumerable (31.10.2022, SRM)
        public static TType GetRandomEntry<TType>(IEnumerable<TType> list)
        {
            if (list == null)
                return default(TType);
            else if (!list.Any())
                return default(TType);
            else
                return list.ElementAt(GetRandomNumber(0, list.Count() - 1));
        }

        #endregion

        // Get Random-Number-String (04.12.2022, SME)
        public static string GetRandomNumberString(int length, bool allowStartWithZero = false)
        {
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
            if (length == 0) return string.Empty;

            var sb = new StringBuilder();
            var index = 0;
            while (index < length)
            {
                var number = GetRandomEntry<char>(Constants.CoreConstants.Numbers);
                if (index == 0 && !allowStartWithZero && number.ToString().Equals("0"))
                    continue;
                sb.Append(number);
                index++;
            }
            return sb.ToString();
        }

        // Get Random-String (04.12.2022, SME)
        public static string GetRandomString(int length, bool includeABCToUpper = true, bool includeABCToLower = true, bool includeNumbers = true)
        {
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
            if (length == 0) return string.Empty;
            if (!includeABCToUpper && !includeABCToLower && !includeNumbers)
                throw new Exception("Ungültige Parameter-Auswahl: Kein Flag gesetzt");

            string characterString = string.Empty;
            if (includeABCToUpper) characterString += Constants.CoreConstants.ABC_ToUpperString;
            if (includeABCToLower) characterString += Constants.CoreConstants.ABC_ToLower;
            if (includeNumbers) characterString += Constants.CoreConstants.NumberString;
            var characters = characterString.ToCharArray();

            var sb = new StringBuilder();
            var index = 0;
            while (index < length)
            {
                var number = GetRandomEntry<char>(characters);
                sb.Append(number);
                index++;
            }
            return sb.ToString();
        }
    }
}
