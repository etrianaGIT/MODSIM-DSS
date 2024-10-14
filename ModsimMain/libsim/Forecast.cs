namespace Csu.Modsim.ModsimModel
{
    /// <summary>Linked List of forecast values (forecasted volumes of flow).</summary>
    public class Forecast
    {
        /// <summary>Constructs a <c>Forecast</c> class.</summary>
        public Forecast()
        {
            forecastData = new TimeSeries(TimeSeriesType.Forecast);
        }
        /// <summary>TimeSeries of forecast data</summary>
        public TimeSeries forecastData;
        /// <summary>Identification name for the forecast</summary>
        public string forecastName;
        /// <summary>Pointer to the previous forecast in the list</summary>
        public Forecast prev;
        /// <summary>Pointer to the next forecast in the list</summary>
        public Forecast next;
    }

}
