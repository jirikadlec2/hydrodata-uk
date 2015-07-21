using System;
using System.Collections.Generic;
using System.Text;
using Fiedler.Interfaces;
using ZedGraph;

namespace Fiedler
{
    /// <summary>
    /// Represents a 'time series' to be displayed
    /// by the graphs
    /// </summary>
    public class TimeSeries : IObservationList, ITimeSeries
    {
        #region Declarations

        private PointPairList _list;
        private double _maxVal = Double.MinValue;
        private double _minVal = Double.MaxValue;
        private DateTime _maxTime = DateTime.MinValue;
        private DateTime _minTime = DateTime.MaxValue;
        private DateTime _startTime = DateTime.MaxValue;
        private DateTime _endTime = DateTime.MinValue;
        private int _numValid = 0;

        #endregion

        #region Constructor

        public TimeSeries()
        {
            _list = new PointPairList();
        }

        public TimeSeries(DateTime start, DateTime end)
        {
            _startTime = start;
            _endTime = end;
            _list = new PointPairList();
        }

        #endregion

        #region IObservationList Members

        public void AddObservation(DateTime time, double value)
        {
            _list.Add(XDate.DateTimeToXLDate(time), value);
            if (value > _maxVal) _maxVal = value;
            if (value < _minVal) _minVal = value;
            if (time > _maxTime) _maxTime = time;
            if (time < _minTime) _minTime = time;
            _numValid++;
        }

        public void AddUnknownValue(DateTime time)
        {
            _list.Add(XDate.DateTimeToXLDate(time), PointPair.Missing);
        }

        public void Clear()
        {
            _list.Clear();
        }

        #endregion

        #region ITimeSeries Members

        public double MaxValue
        {
            get { return _maxVal; }
        }

        public double MinValue
        {
            get { return _minVal; }
        }

        public DateTime MaxTime
        {
            get { return _maxTime; }
        }

        public DateTime MinTime
        {
            get { return _minTime; }
        }

        public DateTime Start
        {
            get 
            {
                if (_startTime < _minTime)
                {
                    return _startTime;
                }
                else
                {
                    return _minTime;
                }
            }
        }

        public DateTime End
        {
            get
            {
                if (_endTime > _maxTime)
                {
                    return _endTime;
                }
                else
                {
                    return _maxTime;
                }
            }
        }

        public int NumValid
        {
            get { return _numValid; }
        }

        public ITimeSeries ShowCumulative()
        {
            TimeSeries ts2 = (TimeSeries)this.Clone();

            ts2._list.Sort();
            for (int i = 1; i < ts2.Count; ++i)
            {
                ts2[i].Y = ts2[i - 1].Y + ts2[i].Y;
            }
            return ts2;
        }

        public ITimeSeries AggregateHourly()
        {
            TimeSeries ts2 = (TimeSeries)this.Clone();
            ts2.Clear();

            bool isWholeHour = false;
            double hourSum = 0;

            ts2._list.Sort();
            for (int i = 1; i < Count; ++i)
            {
                DateTime curDate = XDate.XLDateToDateTime(this[i].X);
                isWholeHour = (curDate.Minute == 0);

                hourSum += this[i].Y;

                if (isWholeHour)
                {
                    ts2.AddObservation(curDate, hourSum);
                    isWholeHour = false;
                    hourSum = 0;
                }
            }
            return ts2;
        }

        public ITimeSeries ShowUnknown()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

        #region IPointList Members

        public int Count
        {
            get { return _list.Count; }
        }

        public ZedGraph.PointPair this[int index]
        {
            get { return _list[index]; }
        }

        #endregion

        #region ICloneable Members

        public object Clone()
        {
            TimeSeries ts2 = new TimeSeries();
            ts2._list = this._list.Clone();
            ts2._maxTime = this.MaxTime;
            ts2._maxVal = this.MaxValue;
            ts2._minTime = this.MinTime;
            ts2._minVal = this.MinValue;
            ts2._numValid = this.NumValid;
            return ts2;
        }

        #endregion
    }
}
