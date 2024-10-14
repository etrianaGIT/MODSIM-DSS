using System;
using System.Collections.Generic;

namespace Csu.Modsim.ModsimModel
{
    public enum ModsimCollectionType : int { powerEff, hydroUnit, hydroTarget }

    /// <summary>Holds identification numbers and names for any type of list specified in <c>ModsimCollectionType</c>.</summary>
    public class ModsimCollection // private to ensure all lists are synchronized between HydropowerController and this (and other classes that are added to utilize IDs)
    {
        // The lists
        private List<List<int>> IDLists;
        private List<List<string>> NameLists;
        private List<List<object>> ObjectLists;

        // Events
        public event EventHandler<ModsimCollectionEventArgs> Creating;
        public event EventHandler<ModsimCollectionEventArgs> Created;
        public event EventHandler<ModsimCollectionEventArgs> Removing;
        public event EventHandler<ModsimCollectionEventArgs> Removed;

        // Constructor
        public ModsimCollection()
        {
            // Build the array lists.
            int NumOfTypes = Enum.GetValues(typeof(ModsimCollectionType)).Length;
            IDLists = new List<List<int>>();
            NameLists = new List<List<string>>();
            ObjectLists = new List<List<object>>();
            for (int i = 0; i < NumOfTypes; i++)
            {
                IDLists.Add(new List<int>());
                NameLists.Add(new List<string>());
                ObjectLists.Add(new List<object>());
            }
        }

        // Copy
        /// <summary>Copies all the elements into a new ModsimCollection</summary>
        public void Copy(Model newModelReference)
        {
            newModelReference.PowerObjects = (ModsimCollection)this.MemberwiseClone();
            int numOfTypes = Enum.GetValues(typeof(ModsimCollectionType)).Length;
            for (int i = 0; i < numOfTypes; i++)
            {
                // IDs
                newModelReference.PowerObjects.IDLists[i] = new List<int>(this.IDLists[i]);

                // Names
                newModelReference.PowerObjects.NameLists[i] = new List<string>(this.NameLists[i]);

                // Objects
                switch ((ModsimCollectionType)i)
                {
                    case ModsimCollectionType.powerEff:
                        newModelReference.PowerObjects.ObjectLists[i] = new List<object>(this.ObjectLists[i]);
                        break;

                    case ModsimCollectionType.hydroUnit:
                        newModelReference.PowerObjects.ObjectLists[i] = new List<object>(this.ObjectLists[i]);
                        break;

                    case ModsimCollectionType.hydroTarget:
                        newModelReference.PowerObjects.ObjectLists[i] = new List<object>(this.ObjectLists[i]);
                        break;

                    default:
                        throw new Exception("Number of lists is not implemented in code yet.");
                }
            }
        }

        // Retrieve the lists.
        /// <summary>Gets the list of IDs for a specified type of object.</summary>
        /// <param name="type">The type of list desired.</param>
        public List<int> GetIDList(ModsimCollectionType type)
        {
            return this.IDLists[(int)type];
        }
        /// <summary>Gets the list of names for a specified type of object.</summary>
        /// <param name="type">The type of list desired.</param>
        public List<string> GetNameList(ModsimCollectionType type)
        {
            return this.NameLists[(int)type];
        }
        /// <summary>Gets the list of object of a specified type.</summary>
        /// <param name="type">The type of list desired.</param>
        public List<object> GetObjectList(ModsimCollectionType type)
        {
            return this.ObjectLists[(int)type];
        }

        // Count the items
        /// <summary>Gets the item count for a specified type of object.</summary>
        /// <param name="type">The type of object for which to retrieve the count.</param>
        public int Count(ModsimCollectionType type)
        {
            return GetIDList(type).Count;
        }

        // Retrieve an item or set an item
        /// <summary>Retrieves the index of an object of a specified type and identification.</summary>
        /// <param name="type">The type of object.</param>
        /// <param name="id">The ID of the object.</param>
        public int IndexOf(ModsimCollectionType type, int id)
        {
            return this.GetIDList(type).IndexOf(id);
        }
        /// <summary>Retrieves the index of an object of a specified type and identification.</summary>
        /// <param name="type">The type of object.</param>
        /// <param name="id">The ID of the object.</param>
        public int IndexOf(ModsimCollectionType type, string name)
        {
            return this.GetNameList(type).IndexOf(name);
        }
        /// <summary>Retrieves an object of a specified type and identification (ID number or name).</summary>
        /// <param name="type">The type of object.</param>
        /// <param name="id">The ID of the object.</param>
        public object this[ModsimCollectionType type, int id]
        {
            get { return ItemAt(type, this.IndexOf(type, id)); }
            set
            {
                int i = this.IndexOf(type, id);
                if (i >= 0)
                    GetObjectList(type)[i] = value;
            }
        }
        /// <summary>Retrieves an object of a specified type and identification (ID number or name).</summary>
        /// <param name="type">The type of object.</param>
        /// <param name="name">The name of the object.</param>
        public object this[ModsimCollectionType type, string name]
        {
            get { return ItemAt(type, this.IndexOf(type, name)); }
            set
            {
                int i = this.IndexOf(type, name);
                if (i >= 0)
                    GetObjectList(type)[i] = value;
            }
        }
        /// <summary>Retrieves an object of a specified type and indexed location.</summary>
        /// <param name="type">The type of object.</param>
        /// <param name="index">The index where the object is located.</param>
        public object ItemAt(ModsimCollectionType type, int index)
        {
            if (index >= 0)
                return GetObjectList(type)[index];
            else
                return null;
        }
        /// <summary>Sets the name of the object within this collection.</summary>
        /// <param name="type">The type of object.</param>
        /// <param name="id">The ID of the object.</param>
        /// <param name="newname">The new name for the object. This name may change if it is not already unique.</param>
        public void SetName(ModsimCollectionType type, int id, ref string newname)
        {
            int index = this.IndexOf(type, id);
            if (index < 0) return;
            if (GetNameList(type)[index] != newname) // Only get a unique name if the name is changing.
                newname = this.GetUniqueName(type, newname, id);
            GetNameList(type)[index] = newname;
        }

        // Add a new object. 
        /// <summary>Retrieves a new ID for a specified type of object and adds the new ID a list.</summary>
        /// <param name="type">The type of object for which to retrieve an ID.</param>
        /// <param name="obj">The object to store in the collection.</param>
        /// <param name="name">The name of the object that is receiving an ID. If name == null or name == "", then a generic name is assigned.</param>
        /// <param name="id">The ID of the object stored in the MODSIM collection.</param>
        public void Add(ModsimCollectionType type, object obj, ref string name, out int id)
        {
            EventHandler<ModsimCollectionEventArgs> RaiseCreating = Creating;
            EventHandler<ModsimCollectionEventArgs> RaiseCreated = Created;

            // Get a unique ID
            id = this.GetUniqueID(type);

            // Get a unique name
            name = this.GetUniqueName(type, name, id);

            // Inform the user that the ID and name are about to be created.
            ModsimCollectionEventArgs e = new ModsimCollectionEventArgs(id, name, obj);
            if (RaiseCreating != null) RaiseCreating(this, e);
            if (e.Cancel) return;

            // Add the ID and name to the appropriate lists.
            List<int> idlist = GetIDList(type);
            List<string> namelist = GetNameList(type);
            List<object> objlist = GetObjectList(type);
            idlist.Add(id);
            namelist.Add(name);
            objlist.Add(obj);

            // Inform the user that the IDs have been created.
            if (RaiseCreated != null) RaiseCreated(this, e);
        }

        // Get a unique identification
        /// <summary>Retrieves a unique ID within the list of objects of a specified type.</summary>
        /// <param name="type">The type of object.</param>
        public int GetUniqueID(ModsimCollectionType type)
        {
            List<int> idlist = GetIDList(type);
            int id;
            for (id = 1; id <= idlist.Count; id++)
                if (!idlist.Contains(id))
                    break;
            return id;
        }
        /// <summary>Retrieves a unique name within the list of objects of a specified type.</summary>
        /// <param name="type">The type of objects.</param>
        /// <returns>If tryname = "", then this returns type_id. If tryname exists in the list already, tryname_i is returned where i >= 2. Otherwise, returns tryname.</returns>
        public string GetUniqueName(ModsimCollectionType type)
        {
            return this.GetUniqueName(type, "");
        }
        /// <summary>Retrieves a unique name within the list of objects of a specified type.</summary>
        /// <param name="type">The type of objects.</param>
        /// <param name="tryname">The name to try.</param>
        /// <returns>If tryname exists in the list already, tryname_i is returned where i >= 2. Otherwise, returns tryname.</returns>
        public string GetUniqueName(ModsimCollectionType type, string tryname)
        {
            return this.GetUniqueName(type, tryname, -1);
        }
        /// <summary>Retrieves a unique name within the list of objects of a specified type.</summary>
        /// <param name="type">The type of objects.</param>
        /// <param name="tryname">The name to try.</param>
        /// <param name="id">The ID of the object. If tryname is "", the ID is used to as part of the default name (type_id).</param>
        /// <returns>If tryname exists in the list already, tryname_i is returned where i >= 2. Otherwise, returns tryname.</returns>
        public string GetUniqueName(ModsimCollectionType type, string tryname, int id)
        {
            List<string> namelist = GetNameList(type);
            if (tryname == null || tryname == "")
                if (id == -1)
                    tryname = type.ToString();
                else
                    tryname = type.ToString() + id.ToString();
            int i = 1;
            string basename = tryname;
            while (namelist.Contains(tryname))
            {
                i++;
                tryname = basename + "_" + i.ToString();
            }
            return tryname;
        }

        // Check existence
        /// <summary>Determines whether a specified ID or name exists.</summary>
        /// <param name="type">The type of object that defines the list of IDs or names.</param>
        /// <param name="id">The ID to find.</param>
        public bool Exists(ModsimCollectionType type, int id)
        {
            return GetIDList(type).Contains(id);
        }
        /// <summary>Determines whether a specified ID or name exists.</summary>
        /// <param name="type">The type of object that defines the list of IDs or names.</param>
        /// <param name="name">The name to find.</param>
        public bool Exists(ModsimCollectionType type, string name)
        {
            return GetNameList(type).Contains(name);
        }

        // Remove
        /// <summary>Removes an ID or name from a list of a specified type.</summary>
        /// <param name="type">The type of object that defines the list of IDs or names.</param>
        /// <param name="id">The ID to remove.</param>
        public void Remove(ModsimCollectionType type, int id)
        {
            List<int> idlist = this.GetIDList(type);
            int i = idlist.IndexOf(id);
            this.RemoveAt(type, i);
        }
        /// <summary>Removes an ID or name from a list of a specified type.</summary>
        /// <param name="type">The type of object that defines the list of IDs or names.</param>
        /// <param name="name">The name to remove.</param>
        public void Remove(ModsimCollectionType type, string name)
        {
            List<string> namelist = this.GetNameList(type);
            int i = namelist.IndexOf(name);
            this.RemoveAt(type, i);
        }
        /// <summary>Removes an ID or name from a list of a specified type at a specified index.</summary>
        /// <param name="type">The type of object that defines the list of IDs or names.</param>
        /// <param name="index">The index at which to remove the type.</param>
        public void RemoveAt(ModsimCollectionType type, int index)
        {
            List<int> idlist = this.GetIDList(type);
            if (index >= 0 && index < idlist.Count)
            {
                // Get lists and events
                List<string> namelist = this.GetNameList(type);
                List<object> objectlist = this.GetObjectList(type);
                EventHandler<ModsimCollectionEventArgs> RaiseRemoving = this.Removing;
                EventHandler<ModsimCollectionEventArgs> RaiseRemoved = this.Removed;

                // Inform the subscriber that we'll be removing the ID and name.
                ModsimCollectionEventArgs e = new ModsimCollectionEventArgs(idlist[index], namelist[index], objectlist[index]);
                if (RaiseRemoving != null) RaiseRemoving(this, e);
                if (e.Cancel) return; // Cancel if the subscriber wants.

                // Remove the ids and names and objects
                idlist.RemoveAt(index);
                namelist.RemoveAt(index);
                objectlist.RemoveAt(index);

                // Inform the subscriber that we've removed the ID and name.
                if (RaiseRemoved != null) RaiseRemoved(this, e);
            }
        }
    }

    /// <summary>Holds event information for <c>ModsimCollection</c>.</summary>
    public class ModsimCollectionEventArgs : EventArgs
    {
        // Instance variables
        private int id;
        private string name;
        private object obj;
        private bool cancel = false;

        // Properties
        /// <summary>Gets a value specifying the ID of an object within a <c>ModsimCollection</c>.</summary>
        public int ID
        {
            get { return this.id; }
        }
        /// <summary>Gets a value specifying the name of an object within a <c>ModsimCollection</c>.</summary>
        public string Name
        {
            get { return this.name; }
        }
        /// <summary>Gets a value specifying the object within a <c>ModsimCollection</c>.</summary>
        public object Obj
        {
            get { return this.obj; }
        }
        /// <summary>Gets and sets a value specifying whether to cancel the particular action occurring after an event.</summary>
        public bool Cancel
        {
            get { return this.cancel; }
            set { this.cancel = value; }
        }

        // Constructor
        public ModsimCollectionEventArgs(int id, string name, object obj)
        {
            this.id = id;
            this.name = name;
            this.obj = obj;
        }
    }
}
