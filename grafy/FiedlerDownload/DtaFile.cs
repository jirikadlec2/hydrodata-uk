// a special class for reading a [DTA] file for a specific station
// 

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace Fiedler
{
    public class DtaFile
    {
        #region Declarations

        private int _stId;
        private List<Channel> _channels;
        private List<Observation> _obsList;
        private string _file;

        #endregion

        #region Constructor

        public DtaFile(int StationId, List<Channel> AllChannels, string DtaDir)
        {
            _stId = StationId;
            _channels = Channel.FindByStation(AllChannels, _stId);
            _file = System.IO.Path.Combine(DtaDir, (_stId.ToString() + ".txt"));
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
            StringBuilder sb = new StringBuilder("(?<time>[^\\t]+)");

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
                while (usedIndex < chanIndex)
                {
                    //append regex for unused channels
                    sb.Append("\\t[^\\t]+");
                    ++usedIndex;
                }

                //append regex for a used channel
                sb.Append("\\t(?<" + (chanIndex + 1).ToString() + ">[^\\t]+)");
                ++usedIndex;
            }

            return new Regex(sb.ToString(), RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        #endregion

        #region Public Methods

        public void ReadDtaLine(Regex reg, List<Channel> chans, string dta)
        {
            Match m = reg.Match(dta);

            if (m.Success)
            {
                string curStr = m.Value;
                double curValue;
                string timeStr = m.Groups["time"].Value;
                int chIndex = 0;
                string groupName;
                DateTime curTime;

                //parse the time
                curTime = Parser.ParseTime(timeStr);

                foreach (Channel ch in chans)
                {
                    chIndex = ch.ChId; //must be increased by one because regex
                    //doesn't allow zero group capture values
                    groupName = (chIndex + 1).ToString();
                    curValue = Parser.String2Double(m.Groups[groupName].Value);
                    _obsList.Add(new Observation(ch.StId, chIndex, curTime, curValue));
                }
            }
        }

        /// <summary>
        /// reads a list of observations from the DTA file
        /// </summary>
        /// <returns>the list of observations that were read</returns>
        public List<Observation> Read()
        {
            //initialize observation internal list
            _obsList.Clear();
            
            //initialize the regular expression
            Regex reg = CreateRegex(_channels);
            
            //open and read the file
            string line;
            using (StreamReader sr = new StreamReader(_file))
            {
                {
                    while((line = sr.ReadLine()) != null)
                    {
                        if (line.IndexOf("<data>") >= 0)
                        {
                            //read all lines that contain data
                            sr.ReadLine();
                            while ((line = sr.ReadLine()).Length > 1)
                            {
                                ReadDtaLine(reg, _channels, line);
                            }
                        }
                    }
                }
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

            public static double String2Double(string input)
            {
                double result = -9999.0; //indicates 'unknown value'

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
