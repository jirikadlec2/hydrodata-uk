using System;
using System.Collections.Generic;
using System.Text;

namespace Fiedler.Interfaces
{
    /// <summary>
    /// represents a sorted list of
    /// <time, value> pairs with basic functionality
    /// for time series data manipulation.
    /// </summary>
    public interface ITimeSeries : ZedGraph.IPointList
    {
        double MaxValue { get; }
        double MinValue { get; }
        DateTime Start { get; }
        DateTime End { get; }
        DateTime MaxTime { get; }
        DateTime MinTime { get; }

        int NumValid { get; }

        ITimeSeries ShowCumulative();
        ITimeSeries ShowUnknown();
        ITimeSeries AggregateHourly();
    }
}
