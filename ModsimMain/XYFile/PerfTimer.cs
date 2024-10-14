using System;

namespace Csu.Modsim.ModsimIO
{
    public class PerfTimer
    {
        DateTime startTime;
        DateTime lapStartTime;
        DateTime endTime;
        public PerfTimer()
        {
            startTime = DateTime.Now;
            lapStartTime = startTime;
        }

        public void startlap()
        {
            lapStartTime = DateTime.Now;
        }

        public void PrintLaptime(string msg)
        {
            endTime = DateTime.Now;
            double seconds = 0;
            seconds = (endTime.Ticks - lapStartTime.Ticks) / 1E+07f;
            Console.WriteLine(msg + seconds);
        }

        public void PrintElapsed(string msg)
        {
            endTime = DateTime.Now;
            double seconds = 0;
            seconds = (endTime.Ticks - startTime.Ticks) / 1E+07f;
            Console.WriteLine(msg + seconds);
        }
    }
}
