using System;
using System.Collections;
using System.Collections.Generic;

namespace Csu.Modsim.ModsimModel
{
    /// <summary>Linked list of link pointers.</summary>
    public class LinkList
    {
        // Instance variables
        /// <summary>points to the next item in the list, null if at the end</summary>
        public LinkList next;
        /// <summary>Link pointed to for the current element in the list</summary>
        public Link link;

        // Constructor
        /// <summary>Constructor sets link and next to zero</summary>
        public LinkList()
        {
            link = null;
            next = null;
        }

        // Local methods
        /// <summary>Returns the number of elments in the list</summary>
        public int Count()
        {
            LinkList ll = null;
            int rval = 0;
            if (this.link != null)
                for (ll = this; ll != null; ll = ll.next)
                    rval++;
            return rval;
        }
        /// <summary>Returns the <c>Link</c> at the specified index in the list</summary>
        public Link Item(int index)
        {
            if (index >= Count() || index < 0)
                return null;
            LinkList ll = this;
            for (int i = 0; i <= index; i++)
            {
                if (i == index)
                    return ll.link;
                ll = ll.next;
            }
            return null;
        }
        /// <summary>Add a link to the list</summary>
        public void Add(Link l)
        {

            if (this.link == null)
            {
                this.link = l;
            }
            else
            {
                LinkList nll = new LinkList();
                LinkList ll = null;
                nll.link = l;
                /// Allocate a new LinkList item.
                for (ll = this; ll != null; ll = ll.next)
                {
                    if (ll.next == null)
                    {
                        ll.next = nll;
                        break;
                    }
                }
            }
        }
        /// <summary>Removes the specified link from this linked list.</summary>
        /// <param name="l">The link object to remove.</param>
        public void Remove(Link l)
        {
            LinkList llprev = null;
            if (this.link != null)
            {
                for (LinkList ll = this; ll != null; ll = ll.next)
                {
                    if (ll.link == l)
                    {
                        if (llprev == null)
                        {
                            this.link = this.next.link;
                            this.next = this.next.next;
                        }
                        else
                        {
                            llprev.next = ll.next;
                        }
                    }
                    else
                    {
                        llprev = ll;
                    }
                }
            }
        }
        /// <summary>Creates an array of links from the link list.</summary>
        public Link[] ToArray()
        {
            List<Link> list = new List<Link>();
            if (this.link != null)
            {
                for (LinkList ll = this; ll != null; ll = ll.next)
                {
                    if (ll.link != null)
                    {
                        list.Add(ll.link);
                    }
                }
            }
            return list.ToArray();
        }
    }

}
