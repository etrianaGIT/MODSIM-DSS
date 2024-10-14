using System;
using System.Collections;

namespace Csu.Modsim.ModsimModel
{
    /// <summary>DateList contains a list of dates</summary>
    public class DateList : CollectionBase
    {
        /// <summary>Return the date of the specified item (by index) in the list</summary>
        public DateTime Item(int index)
        {
            return (DateTime)(List[index]);
        }
        /// <summary>Set the specified (by index) item to the specified value</summary>
        public void Item(int index, DateTime value)
        {
            List[index] = value;
        }
        /// <summary>Add a new item to the list</summary>
        public int Add(DateTime value)
        {
            return (List.Add(value));
        }
        public int IndexOf(DateTime value)
        {
            /// <summary>Return the index of the item with the specified value</summary>
            return (List.IndexOf(value));
        }
        /// <summary>Remove the item with the specified value from the list</summary>
        public void Remove(DateTime value)
        {
            List.Remove(value);
        }
    }
}
