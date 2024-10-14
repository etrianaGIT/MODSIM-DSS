namespace Csu.Modsim.ModsimModel
{
    public static class GlobalMembersMss
    {
        public static void CalcLinkFlows(Model mi, int mon)
        {
            Link l;
            Node n;

            for (l = mi.firstLink; l != null; l = l.next)
            {
                if (l.mlInfo.isArtificial)
                    continue;
                l.mrlInfo.link_flow[mon] = l.mlInfo.flow;
                l.mrlInfo.link_closs[mon] = l.mrlInfo.closs;

                /* add in losses to link flow output */
                // RKL I don't see why we want to add the channel loss here; it is confusing
                if (l.m.loss_coef < 1.0)
                {
                    l.mrlInfo.link_flow[mon] += (long)(l.mrlInfo.closs + DefineConstants.ROFF);
                }
                /* for flow-through demand, link flow output should include
                ** the flow-through demand
                */
                //RKL this is for the case where we have a flothru demand "onstream" and we want
                //  the link directly downstream of the flothru demand to LOOK like it carried the
                //  total flow upstream of the flothu deamnd node
                //   (unless part of the flothru return is split to another location, that part is not shown)
                //  this seems confusing and should go away
                n = l.from;
                // change the array size limit from 10 to idstrmx->length
                for (int j = 0; j < 10; j++)
                {
                    if (n.m.idstrmx[j] != null)
                    {
                        if (n.m.idstrmx[j] == l.to)
                        {
                            l.mrlInfo.link_flow[mon] += (long)(n.mnInfo.demLink.mlInfo.flow * n.m.idstrmfraction[j] + DefineConstants.ROFF);
                        }
                    }
                }
            }
        }
    }
}
