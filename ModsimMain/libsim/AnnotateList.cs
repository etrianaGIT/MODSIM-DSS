using System;
using System.Collections;
namespace Csu.Modsim.ModsimModel
{
    /// <summary>List (collection) of canvas annotations</summary>
    public class AnnotateList : CollectionBase
    {

        /// <summary>Get (return the address of) the specifed (by index number) annotation</summary>
        public Annotate Item1(int index)
        {
            return (Annotate)(List[index]);
        }
        /// <summary>Set the text of the specifed (by index) annotation to that of the specified (pointer to an annotation) annotation</summary>
        public void Item1(int index, Annotate value)
        {
            List[index] = (value);
        }
        //<summary>Add a specified annotation to the list</summary>
        public int Add(Annotate value)
        {
            return (List.Add(value));
        }
        /// <summary>Return the index of the specified annotation</summary>
        public int IndexOf(Annotate value)
        {
            return (List.IndexOf(value));
        }
        /// <summary>Remove the specified annotation for the list</summary>
        public void Remove(Annotate value)
        {
            List.Remove(value);
        }
    }
}
