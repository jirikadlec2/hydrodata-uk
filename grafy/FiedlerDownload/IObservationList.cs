using System;

namespace Fiedler.Interfaces
{
    /// <summary>
    /// represents a sorted list of time, value pairs
    /// </summary>
    public interface IObservationList
    {
        /// <summary>
        /// Adds an observation data point to the list
        /// </summary>
        /// <param name="time">time of observation</param>
        /// <param name="value">the observation value</param>
        void AddObservation(DateTime time, double value);

        /// <summary>
        /// Adds an observation with unknown value to the list
        /// </summary>
        /// <param name="time">the time of observation</param>
        void AddUnknownValue(DateTime time);

        /// <summary>
        /// Clears all observations in the list
        /// </summary>
        void Clear();
    }
}
