using System;
using System.IO;

namespace Csu.Modsim.ModsimIO
{
    // TextFile class handles reading an arrray of strings
    // from disk.
    // Also has helper functions to extract strings,integers,and floating point values
    public class TextFile
    {
        string _filename;
        string[] _lines;

        public TextFile(string filename)
        {
            _filename = filename;
            _lines = File.ReadAllLines(_filename);
        }

        public Int32 Count
        {
            get { return _lines.Length; }
        }
        public string this[int index]
        {
            get { return _lines[index]; }
        }

        public string fname
        {
            get { return _filename; }
        }

        // Returns index in file that begins with specified string 
        // return -1 if not found (or bad start index)
        public int FindBeginningWith(string findMe, int startIndex, int endIndex)
        {
            int i = 0;
            if (startIndex < 0)
                return -1;
            if (startIndex > _lines.Length - 1)
                return -1;
            if (startIndex > endIndex)
                return -1;
            if (endIndex > _lines.Length - 1)
                return -1;

            for (i = startIndex; i <= endIndex; i++)
            {
                if (_lines[i].IndexOf(findMe) == 0)
                    return i;
            }
            return -1;

        }

        // Returns index to line in file that has specified string 
        // return -1 if not found (or bad start index)
        public int Find(string findMe, int startIndex, int endIndex)
        {
            int i = 0;
            if (startIndex < 0)
                return -1;
            if (startIndex > _lines.Length - 1)
                return -1;

            //_lines.Length - 1
            for (i = startIndex; i <= endIndex; i++)
            {
                if (_lines[i] == findMe)
                    return i;
            }
            return -1;

        }
        // Returns index to line in file that has any of the specified strings
        // return -1 if not found (or bad start index)
        public int FindAny(string[] findMe, int startIndex)
        {
            int i = 0;
            int j = 0;
            if (startIndex < 0)
                return -1;
            if (startIndex > _lines.Length - 1)
                return -1;
            string currentLine = null;

            for (i = startIndex; i < _lines.Length; i++)
            {
                currentLine = _lines[i];
                for (j = 0; j < findMe.Length; j++)
                {
                    if (currentLine == findMe[j])
                        return i;
                }
            }
            return -1;
        }

    }
}
