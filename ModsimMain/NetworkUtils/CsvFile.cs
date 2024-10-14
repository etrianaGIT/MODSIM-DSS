using System.Text.RegularExpressions;

//********************************
// *
// * Csv.cs
// *
// * Karl Tarbet  May 30, 2002
// * April 2007 (csv bug fixes)
// * May 2007 data type estimation
// * A DataTable class that reads and writes
// * to comma seperated files.
// *
// *******************************
namespace Csu.Modsim.NetworkUtils
{
    /// <summary>
    /// Class for encoding/decoding comma seperated text files
    /// </summary>
    public class CsvFile
    {
        /// <summary>
        /// encode csv data for a single
        /// Excel cell.
        /// </summary>
        public static string EncodeCSVCell(string data)
        {
            string rval = data;
            if (data.IndexOf('"') >= 0)
            {
                rval = data.Replace("\"", "\"\"");
            }

            if (rval.IndexOfAny(new char[] { ',', '\"' }) >= 0)
            {
                rval = "\"" + rval + "\"";
            }
            return rval;
        }

        static Regex s_csvRegex = null;
        private static Regex CsvRegex
        {
            get
            {
                if (s_csvRegex == null)
                {
                    string strPattern = ("^" + "(?:\"(?<value>(?:\"\"|[^\"\\f\\r])*)\"|(?<value>[^,\\f\\r\"]*))") + "(?:,(?:[ \\t]*\"(?<value>(?:\"\"|[^\"\\f\\r])*)\"|(?<value>[^,\\f\\r\"]*)))*" + "$";
                    //Match objMatch = Regex.Match(strLine, strPattern);
                    s_csvRegex = new Regex(strPattern, RegexOptions.Compiled);
                }
                return s_csvRegex;
            }
        }

        /// <summary>
        /// Decodes CSV string
        /// </summary>
        public static string[] DecodeCSVLine(string strLine)
        {
            // from http://www.nonhostile.com/page000029.asp
            Match objMatch = CsvRegex.Match(strLine);
            if (!objMatch.Success)
            {
                return new string[] { };
            }
            Group objGroup = objMatch.Groups["value"];
            int intCount = objGroup.Captures.Count;
            string[] arrOutput = new string[(intCount - 1) + 1];
            for (int i = 0; i < intCount; i++)
            {
                Capture objCapture = objGroup.Captures[i];
                arrOutput[i] = objCapture.Value;
                arrOutput[i] = arrOutput[i].Replace("\"\"", "\"").Trim();
            }
            return arrOutput;
        }
    }
}

