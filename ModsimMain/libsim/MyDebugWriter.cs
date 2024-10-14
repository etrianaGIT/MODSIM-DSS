using System;
using System.IO;
using System.Data;
using Csu.Modsim.ModsimModel;

namespace MyDebugHelper
{
    public static class MyDebugWriter
    {
        private static string filepath = Path.GetDirectoryName(typeof(MyDebugWriter).Assembly.Location) + "\\MyDebugFile.txt";
        private static FileStream fs = new FileStream(filepath, FileMode.Create, FileAccess.Write);
        private static StreamWriter sw = new StreamWriter(fs);
        public static string separator = "|||";
        public static int ctr = 0;
        public static int maxctr = 100000;
        public static bool KeepGoing()
        {
            ctr++;
            return ctr <= maxctr;
        }
        public static void Write(string variable, object value)
        {
            if (!KeepGoing())
                return;
            if (!fs.CanWrite)
            {
                fs = new FileStream(filepath, FileMode.Append, FileAccess.Write);
                sw = new StreamWriter(fs);
            }
            if (value != null)
                sw.WriteLine(variable + separator + value.ToString());
        }
        public static void Close()
        {
            sw.Close();
        }
        public static void WriteModelComponents(Model mi, int tstep)
        {
            if (!KeepGoing())
                return;

            // Header...
            sw.WriteLine("Model Components Output for Timestep: " + tstep.ToString());

            // Real Links
            foreach (Link l in mi.mInfo.lList)
            {
                if (l != null)
                {
                    ctr = ctr + 5;
                    sw.WriteLine("Link Output for " + l.name + " (number " + l.number.ToString() + "):");
                    sw.WriteLine("\thi:           " + l.mlInfo.hi.ToString());
                    sw.WriteLine("\tlo:           " + l.mlInfo.lo.ToString());
                    sw.WriteLine("\tcost:         " + l.mlInfo.cost.ToString());
                    sw.WriteLine("\tflow:         " + l.mlInfo.flow.ToString());
                }
            }
        }
    }
}
