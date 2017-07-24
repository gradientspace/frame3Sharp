using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
            int N = newStr.Length;
            if (N == 0)
                return newStr;
            int num_dots = 0;
            for ( int i = 0; i < N; ++i ) {
                char c = newStr[i];
                if (char.IsDigit(c) == false && c != '-' && c != '.')
                    return oldStr;
                if (c == '-' && i != 0)
                    return oldStr;
                if (c == '.') {
                    num_dots++;
                    if (num_dots > 1)
                        return oldStr;
                }
            }
            if (N == 1 && newStr[0] == '.')
                return "0.";
            if (N == 2 && newStr[0] == '-' && newStr[1] == '.')
                return "-0.";

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
