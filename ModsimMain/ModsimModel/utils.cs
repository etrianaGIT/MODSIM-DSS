using System;

namespace Csu.Modsim.ModsimModel
{
    public static class Utils
    {
        // This routine only valid for artificial links in the model.
        public static void ConnectFromNode(Link l, Node n)
        {
            l.from = n;

            var ll = new LinkList();
            ll.next = n.OutflowLinks;
            ll.link = l;
            n.OutflowLinks = ll;
        }

        // This routine only valid for artificial links in the model.
        public static void ConnectToNode(Link l, Node n)
        {
            l.to = n;

            var ll = new LinkList();
            ll.next = n.InflowLinks;
            ll.link = l;
            n.InflowLinks = ll;
        }

        // This routine only valid for artificial links in the model.
        public static void DisConnectToNode(Link l)
        {
            LinkList ll = null;
            LinkList ll2 = null;

            // Search ...
            for (ll = l.to.InflowLinks; ll != null; ll = ll.next)
            {
                if (ll.link == l)
                {
                    ll2 = ll;
                }
            }
            // Find previous pointer to ll2 - and destroy connection
            for (ll = l.to.InflowLinks; ll != null; ll = ll.next)
            {
                if (ll == l.to.InflowLinks && ll == ll2)
                {
                    l.to.InflowLinks = ll.next;
                    ll = null;
                    break;
                }
                else if (ll.next == ll2)
                {
                    ll.next = ll2.next;
                    ll = null;
                    break;
                }
            }
        }

        // This routine only valid for artificial links in the model.
        public static void DisConnectFromNode(Link l)
        {
            LinkList ll = null;
            LinkList ll2 = null;

            // Search ...
            for (ll = l.from.OutflowLinks; ll != null; ll = ll.next)
            {
                if (ll.link == l)
                {
                    ll2 = ll;
                }
            }
            // Find previous pointer to ll2 - and destroy connection
            for (ll = l.from.OutflowLinks; ll != null; ll = ll.next)
            {
                if (ll == l.from.OutflowLinks && ll == ll2)
                {
                    l.from.OutflowLinks = ll.next;
                    ll = null;
                    break;
                }
                else if (ll.next == ll2)
                {
                    ll.next = ll2.next;
                    ll = null;
                    break;
                }
            }
        }

        /// <summary>
        /// Relative difference between two real numbers.
        /// </summary>
        /// <param name="a">first real number</param>
        /// <param name="b">second real number</param>
        /// <returns>The relative difference of two real numbers: 0.0 if they are
        /// exactly the same, otherwise the ratio of the difference to the larger
        /// of the two.</returns>
        public static double RelativeDiff(double a, double b)
        {
            /* Taken from here: http://c-faq.com/fp/fpequal.html */
            double c = Math.Abs(a);
            double d = Math.Abs(b);

            d = Math.Max(c, d);

            return d == 0.0 ? 0.0 : Math.Abs(a - b) / d;
        }

        public static bool NearlyEqual(double a, double b, double epsilon)
        {
            /* Taken from here: http://floating-point-gui.de/errors/comparison/ */

            double absA = Math.Abs(a);
            double absB = Math.Abs(b);
            double diff = Math.Abs(a - b);

            if (a == b)
            {
                // shortcut, handles infinities
                return true;
            }
            else if (a == 0 || b == 0 || diff < double.Epsilon)
            {
                // a or b is zero or both are extremely close to it
                // relative error is less meaningful here
                return diff < (epsilon * double.Epsilon);
            }
            else
            {
                // use relative error
                return diff / (absA + absB) <= epsilon;
            }
        }

    }
}
