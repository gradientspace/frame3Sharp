using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace f3
{
    public class UniqueNames
    {
        protected static Dictionary<string, int> uniqueNameCounters = new Dictionary<string, int>();

        // allocate next unique integer ID for given basename
        static public string GetNext(string sBaseName)
        {
            int nNum = 1;
            lock (uniqueNameCounters) {
                if (uniqueNameCounters.ContainsKey(sBaseName)) {
                    nNum = ++uniqueNameCounters[sBaseName];
                } else {
                    uniqueNameCounters[sBaseName] = nNum;
                }
            }
            return string.Format("{0}{1}", sBaseName, nNum);
        }
    }
}
