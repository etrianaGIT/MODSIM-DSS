using System;

namespace Csu.Modsim.ModsimIO
{

    public class XYFileVersionException : ApplicationException
    {

        public XYFileVersionException(string message)
        {
        }
    }
}
namespace Csu.Modsim.ModsimIO
{
    public class XYFileReadingException : ApplicationException
    {
        string msg;
        public XYFileReadingException()
        {
        }

        public XYFileReadingException(string message, int linenumber = -1)
        {
            if (linenumber < 0)
            {
                msg = "XYFileReadError:  " + message;
            }
            else
            {
                msg = "XYFileReadError:  line number " + linenumber + ". " + message;
            }
        }

        public override string Message
        {
            get { return msg; }
        }

    }
}
namespace Csu.Modsim.ModsimIO
{
    public class XYFileWritingException : ApplicationException
    {

        public XYFileWritingException()
        {
            // Implementation code goes here.
        }

        public XYFileWritingException(string message)
        {
            // Implementation code goes here.
        }

        public XYFileWritingException(string message, Exception inner)
        {
            // Implementation code goes here.
        }

    }
}
