using System;

namespace Csu.Modsim.ModsimModel
{
    /// <summary>ModsimTimer is used to time internal prosseses</summary>
    public class ModsimTimer
    {
        public DateTime Start;
        /// <summary>Constructor to create a new instance and start the timer</summary>
        public ModsimTimer()
        {
            Start = DateTime.Now;
        }
        /// <summary>Report time elasped in seconds from timer start</summary>
        public double ElapsedMinutes()
        {
            DateTime t = DateTime.Now;
            TimeSpan diff = t.Subtract(Start);
            return diff.TotalMinutes;
        }
        /// <summary>Report a message of elapsed time to the console</summary>
        public void Report(string msg)
        {
            Console.WriteLine(string.Format("{0} (elapsed: {1:0.000} min)", msg, ElapsedMinutes()));
        }
        public string GetReport(string msg)
        {
            return string.Format("{0} (elapsed: {1:0.000} min)", msg, ElapsedMinutes());
        }

    }
}
