using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace f3
{
    public static class StringValidators
    {
        //
        // only allows signed real-valued input
        // valid characters are 0 to 9, . and - , strings with other characters are rejected
        // "." is replaced with "0."
        // "-." is replaced with "-0."
        public static string SignedRealEdit(string oldStr, string newStr)
        {
            string separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            char decimalSep = separator[0];

            int N = newStr.Length;
            if (N == 0)
                return newStr;
            int num_dots = 0;
            for ( int i = 0; i < N; ++i ) {
                char c = newStr[i];
                if (char.IsDigit(c) == false && c != '-' && c != decimalSep)
                    return oldStr;
                if (c == '-' && i != 0)
                    return oldStr;
                if (c == decimalSep) {
                    num_dots++;
                    if (num_dots > 1)
                        return oldStr;
                }
            }
            if (N == 1 && newStr[0] == decimalSep)
                return "0" + decimalSep.ToString();
            if (N == 2 && newStr[0] == '-' && newStr[1] == decimalSep)
                return "-0" + decimalSep.ToString();

            return newStr;
        }


        public static string BasicAlphaNumericText(string oldStr, string newStr)
        {
            string cleanString = Regex.Replace(newStr, @"[^a-zA-Z0-9\-\_]", "");
            return cleanString;
        }
        public static string KeyboardSymbolsText(string oldStr, string newStr)
        {
            string cleanString = Regex.Replace(newStr, @"[^a-zA-Z0-9\-\_\,\.\?\*\#\(\)\[\]\<\>\\\/ ]", "");
            return cleanString;
        }
    }

}
