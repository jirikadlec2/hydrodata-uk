// a special class for reading a [CHMI_1] file for a specific station
// 

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace Fiedler
{
    public class ChmiFile
    {
        #region Declarations

        private int _stId;
        private List<Channel> _channels;
        private List<Observation> _obsList;
        private string _file;

        #endregion

        #region Constructor

        public ChmiFile(int StationId, List<Channel> AllChannels, string DtaDir)
        {
            _stId = StationId;
            _channels = Channel.FindByStation(AllChannels, _stId);
            _file = System.IO.Path.Combine(DtaDir, (_stId.ToString() + "_chm.txt"));
            _obsList = new List<Observation>(128);
        }

        #endregion

        #region Private Methods
        /// <summary>
        /// this will prepare the regular expression for extracting the data
        /// </summary>
        /// <param name="channels">list of channel indexes to be extracted</param>
        /// <returns></returns>
        private Regex CreateRegex(List<Channel> channels)
        {
            StringBuilder sb = new StringBuilder("[0-9]+[\\t]+(?<chan>");

            if (channels.Count == 0)
            {
                throw new ArgumentException("number of channels cannot be zero");
            }
            int maxIndex = channels[channels.Count - 1].ChId;
            int usedIndex = 0;
            int chanIndex = 0;

            //loop all channels
            foreach (Channel ch in channels)
            {
                chanIndex = ch.ChId;
                sb.Append(chanIndex.ToString());
                sb.Append("|");
                ++usedIndex;
            }
            sb.Remove(sb.Length - 1, 1);
            sb.Append(")[\\t]+(?<time>[^\\t]+)[\\t]+(?<val>[^\\t]+)\\r\\n");

            return new Regex(sb.ToString(), RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// reads a list of observations from the CHMI_1 file
        /// </summary>
        /// <returns>the list of observations that were read</returns>
        public List<Observation> Read()
        {
            //initialize observation internal list
            _obsList.Clear();

            //initialize the regular expression
            Regex reg = CreateRegex(_channels);
            
            //information about channels
            System.Collections.Hashtable ht = new System.Collections.Hashtable();
            foreach (Channel ch in _channels)
            {
                ht.Add(ch.ChId, ch);
            }

            //open and read the file
            string str = "";
            string chanStr, timeStr, valStr;
            int curChan;
            DateTime curTime;
            double curVal;

            using (StreamReader sr = new StreamReader(_file))
            {
                {
                    str = sr.ReadToEnd();
                }
            }

            Match m = reg.Match(str);
            while (m.Success)
            {
                chanStr = m.Groups["chan"].Value;
                timeStr = m.Groups["time"].Value;
                valStr = m.Groups["val"].Value;

                curChan = int.Parse(chanStr);
                curTime = Parser.ParseChmiTime(timeStr);
                curVal = Parser.String2Double(valStr);
                _obsList.Add(new Observation(_stId, curChan, curTime, curVal));

                m = m.NextMatch();
            }

            return _obsList;
        }

        #endregion Public Methods

        #region Parser
        /// <summary>
        /// special utility class for parsing the text from the file
        /// </summary>
        private class Parser
        {
            public static DateTime ParseTime(string input)
            {
                string day = input.Substring(0, 2);
                string mon = input.Substring(3, 2);
                string year = input.Substring(6, 4);
                string hour = input.Substring(11, 2);
                string minute = input.Substring(14, 2);
                string second = input.Substring(17, 2);
                return new DateTime(int.Parse(year), int.Parse(mon), int.Parse(day),
                    int.Parse(hour), int.Parse(minute), int.Parse(second));
            }

            public static DateTime ParseChmiTime(string input)
            {
                string day = input.Substring(8, 2);
                string mon = input.Substring(5, 2);
                string year = input.Substring(0,4);
                string hour = input.Substring(11, 2);
                string minute = input.Substring(14, 2);
                string second = input.Substring(17, 2);
                return new DateTime(int.Parse(year), int.Parse(mon), int.Parse(day),
                    int.Parse(hour), int.Parse(minute), int.Parse(second));
            }

            public static double String2Double(string input)
            {
                double result = -9999.0; //indicates 'unknown value'

                //replace "," in input with "."
                input = input.Replace(",", ".");

                //first, try reading number using current culture specification
                if (double.TryParse(input, out result) == true)
                {
                    return result;
                }
                else
                {
                    //try reading number assuming decimal point
                    System.Globalization.CultureInfo cult = new System.Globalization.CultureInfo("en-US");
                    System.Globalization.NumberFormatInfo decimalPointFormat = cult.NumberFormat;
                    double.TryParse(input, System.Globalization.NumberStyles.Number, decimalPointFormat, out result);
                    return result;
                }
            }
        }

        #endregion Parser
    }
}
