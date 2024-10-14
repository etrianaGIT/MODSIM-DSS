namespace Csu.Modsim.ModsimModel
{
    public class DistConstraint
    {
        private constraintliststr member; // Don't charge losses
        private constraintliststr memberCharge; // Losses are charged
        private constraintliststr memberCredit; // Losses are credited
        private static constraintliststr heap = null;
        private DistConstraint next = null;
        private long hi;

        // Constructor
        public DistConstraint()
        {
            hi = 0;
            member = null;
            memberCharge = null;
            memberCredit = null;
            next = null;
            if (heap == null) // Quick 9
            {
                heap = new constraintliststr();
                heap.next = new constraintliststr();
                heap.next.next = new constraintliststr();
                heap.next.next.next = new constraintliststr();
                heap.next.next.next.next = new constraintliststr();
                heap.next.next.next.next.next = new constraintliststr();
                heap.next.next.next.next.next.next = new constraintliststr();
                heap.next.next.next.next.next.next.next = new constraintliststr();
                heap.next.next.next.next.next.next.next.next = new constraintliststr();
                heap.next.next.next.next.next.next.next.next.next = null;
            }
        }

        private void AddMemberGen(ref constraintliststr memb)
        {
            if (heap == null) // Quick 9
            {
                heap = new constraintliststr();
                heap.next = new constraintliststr();
                heap.next.next = new constraintliststr();
                heap.next.next.next = new constraintliststr();
                heap.next.next.next.next = new constraintliststr();
                heap.next.next.next.next.next = new constraintliststr();
                heap.next.next.next.next.next.next = new constraintliststr();
                heap.next.next.next.next.next.next.next = new constraintliststr();
                heap.next.next.next.next.next.next.next.next = new constraintliststr();
                heap.next.next.next.next.next.next.next.next.next = null;
            }
            constraintliststr hold = heap;
            heap = heap.next;
            hold.next = memb;
            memb = hold;
        }

        public static sortstr freeListSortStr = null;
        public static diststr freeListDistStr = null;
        public static void CleanUp()
        {
            for (; heap != null; )
            {
                constraintliststr hold = heap;
                heap = heap.next;
            }
        }

        public static void Initialize()
        {
            heap = null;
            for (int i = 0; i < 5; i++)
            {
                constraintliststr hold = new constraintliststr();
                hold.next = heap;
                heap = hold;
            }
        }

        public void AddMember()
        {
            AddMemberGen(ref member);
        }

        public void AddMemberCharge()
        {
            AddMemberGen(ref memberCharge);
        }

        public void AddMemberCredit()
        {
            AddMemberGen(ref memberCredit);
        }

        public void DeleteAllMembers()
        {
            constraintliststr hold;

            for (; member != null; )
            {
                hold = member;
                member = member.next;
                hold.next = heap;
                heap = hold;
            }

            for (; memberCharge != null; )
            {
                hold = memberCharge;
                memberCharge = memberCharge.next;
                hold.next = heap;
                heap = hold;
            }

            for (; memberCredit != null; )
            {
                hold = memberCredit;
                memberCredit = memberCredit.next;
                hold.next = heap;
                heap = hold;
            }
        }

        public void SetHi(long hi_in)
        {
            hi = hi_in;
        }

        public long GetHi()
        {
            return hi;
        }

        // Return the head to the member linked list.  Not totally protected.
        // Might improve with a cursor into the list?
        public constraintliststr GetMemberList()
        {
            return member;
        }

        public constraintliststr GetMemberListCharge()
        {
            return memberCharge;
        }

        public constraintliststr GetMemberListCredit()
        {
            return memberCredit;
        }

        // Cursors
        public DistConstraint GetNext()
        {
            return next;
        }
        public void SetNext(DistConstraint newNext)
        {
            next = newNext;
        }
    }
}
