using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Fiedler
{
    /// <summary>
    /// this class contains data 
    /// about observation. 
    /// it also makes it possible to read observations
    /// from the DTA file.
    /// </summary>
    public class Observation
    {
        private int _chUid;
        private DateTime _time;
        private double _val;

        public Observation(int st, int ch, DateTime time, double val)
        {
            _chUid = (st - 28500) * 100 + ch;
            _time = time;
            _val = val;
        }

        public Observation(int chUid, DateTime time, double val)
        {
            _chUid = chUid;
            _time = time;
            _val = val;
        }

        public int ChannelUid
        {
            get { return _chUid; }
        }

        public int StationId
        {
            get { return 28500 + (int)((double)_chUid / 100.0); }
        }

        public int ChannelId
        {
            get { return _chUid % 100; }
        }

        public DateTime Time
        {
            get { return _time; }
        }

        public double Value
        {
            get { return _val; }
        }
    }
}

