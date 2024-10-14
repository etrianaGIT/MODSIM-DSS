namespace Csu.Modsim.ModsimModel
{
    /// <summary>An interface defining a MODSIM object that lies within the "controller" or the ModsimCollection object in the Model class.</summary>
    public interface IModsimObject
    {
        /// <summary>Gets the unique ID number for this instance. If -1, then this instance is not within the controller.</summary>
        int ID { get; }
        /// <summary>Gets and sets the name of this instance. When setting name for an object already within the controller, the name needs to be unique, so it may change slightly.</summary>
        string Name { get; set; }
        /// <summary>Gets whether this instance is in the controller.</summary>
        bool IsInController { get; }
        /// <summary>Gets the ModsimCollectionType of this instance.</summary>
        ModsimCollectionType ModsimObjectType { get; }
        /// <summary>Compares this instance to another instance of the same object for sorting purposes.</summary>
        /// <param name="obj">The object to compare this instance to.</param>
        /// <returns>Returns a negative number if this instance precedes obj in the sort order. Returns 0 if this instance occurs in the same position in the sort order as obj. Returns a positive number if this instance follows obj in the sort order.</returns>
        int CompareTo(object obj);
        /// <summary>Adds this instance to the ModsimCollection controller.</summary>
        void AddToController();
        /// <summary>Removes this instance from the ModsimCollection controller.</summary>
        void RemoveFromController();
    }
}
