using System;

namespace Csu.Modsim.ModsimModel
{

    public static class GlobalMembersRelax4
    {
        public static arrayit_ arrayit_1 = new arrayit_();
        public static arraysave_ arraysave_1 = new arraysave_();
        public static auctionsave_ auctionsave_1 = new auctionsave_();
        public static blks_ blks_1 = new blks_();
        public static blks2_ blks2_1 = new blks2_();
        public static blks3_ blks3_1 = new blks3_();
        public static input_ input_1 = new input_();
        public static output_ output_1 = new output_();
        public static bool silent = false;

        public static int relaxcallfortran(Model mi)
        {
            return GlobalMembersRelax4.relaxcallfortraninternal(mi, 0);
        }
        public static int relaxcallfortranincremental(Model mi)
        {
            return GlobalMembersRelax4.relaxcallfortraninternal(mi, DefineConstants.USEINCREMENT);
        }
        public static int relaxcallfortraninternal(Model mi, int incremental)
        {
            /*****************************************************************************************
            relaxcllfortraninternal -	TRANSFERS ARC DATA FROM THE PARENT PROGRAM
                                        OPERATE FOR USE WITH RELAX AND THEN CALLS RELAX.
            -------------------------------------------------------------------------------------------
            Note This routine is the MAIN routine driving the process.
            \*****************************************************************************************/

            /*  INITIALIZE CORRESPONDING VARIABLES FROM MODSIM/KILTER FOR USE WITH RELAX. */
            //GlobalMembersRelax4.InitializeValues();
            GlobalMembersRelax4.InitializeValues(mi);

            /*  SET UP CORRESPONDING VARIABLES FROM MODSIM/KILTER FOR USE WITH RELAX,
                TRANSFORMATION OF DATA FOR LOWER BOUND CONDITION, AND
                SET UP REDUCED COST TO LINK COST SO THAT DUAL PRICE EQUAL TO ZERO. */
            GlobalMembersRelax4.SetupValues(mi);

            /*  CHECK CURRENT NETWORK TO VERIFY THE CONNECTIVITY HAS NOT CHANGED FROM PREVIOUS.*/
            if (incremental != 0)
                GlobalMembersRelax4.CheckNETWORK(mi); // Check to set value of input_1.repeat
            else
                input_1.repeat = false;

            /*	SET UP DATA QUEUES (TWO LINKED LISTS FOR THE NETWORK TOPOLOGY),
                UPDATE DUAL PRICE, PREPARE BEFORE RUN, ETC. */
            GlobalMembersRelax4.inidat_();
            if (input_1.repeat == false)
                input_1.crash = DefineConstants.USEAUCTION; // Do NOT need to update dual price; use zero.
            else if (input_1.repeat == true)
            {
                GlobalMembersRelax4.UpdateValues();
                input_1.crash = 0;
            }

            /*  CALL relax4 TO SOLVE THE NETWORK.*/
            output_1.time0 = DateTime.Now; // clock();
            if (GlobalMembersRelax4.relax4_(mi) != 0 && !silent)
            {
                mi.FireOnError("Error solving the network... exiting.");
                return 1;
            }
            output_1.time2 = (double)DateTime.Now.Subtract(output_1.time0).TotalMilliseconds;

            /*  CHECK THE SOLUTION FROM RELAX4. */
            if (blks_1.feasbl == true)
            {
                GlobalMembersRelax4.CheckOUTPUT();
                GlobalMembersRelax4.SaveValues(mi);
                GlobalMembersRelax4.SaveValues_DualPrice();

                //if (DefineConstants.SHOWLOG != 0)
                //{
                //    GlobalMembersRelax4.DisplayInfo_1();
                //    GlobalMembersRelax4.DisplayInfo_2(); // DISPLAY RELAX4 EXECUTION TIME.
                //}

                if (!output_1.feasbl || arraysave_1.tcost != arraysave_1.tprice)
                {
                    // The solution either has dfct <> 0 or violate complementary slackness, or
                    // The total primal cost is not equal to total dual price.
                    GlobalMembersRelax4.ClearValues(); // Disable recall.
                }
            }
            else
            {
                GlobalMembersRelax4.ClearValues(); // Disable recall.
            }

            /*  RETRANSFORM VARIABLES x(), u() TO ACCOUNT FOR LOWER BOUND > 0 */
            GlobalMembersRelax4.RetranValues(mi);

            return 0;
        }
        public static int ascnt2_(ref long dm, ref long delx, ref long nlabel, ref bool feasbl, ref bool switch__, ref long nscan, ref long curnode, ref long prevnode)
        {
            /* --------------------------------------------------------------- */

            /*  PURPOSE - THIS ROUTINE IS ANALOGOUS TO ASCNT BUT FOR */
            /*     THE CASE WHERE THE SCANNED NODES HAVE NEGATIVE DEFICIT. */

            /* --------------------------------------------------------------- */

            /*     MAXNN = DIMENSION OF NODE-LENGTH ARRAYS */
            /*     MAXNA = DIMENSION OF ARC-LENGTH ARRAYS */


            /* ^^                                     B                     ^^ */
            /* ^^          TEMPIN I1 P,TEMPOU PRICE I2,I3,I4,I5,I6,I7        ^^ */
            /* ^^                      MARK   TFSTOU  TNXTOU  TFSTIN TNXTIN ^^ */
            /* ^^                  I14      I15        I16     I17          ^^ */


            /*     STORE THE ARCS BETWEEN THE SET OF SCANNED NODES AND */
            /*     ITS COMPLEMENT IN SAVE AND COMPUTE DELPRC, THE STEPSIZE */
            /*     TO THE NEXT BREAKPOINT IN THE DUAL COST IN THE DIRECTION */
            /*     OF INCREASING PRICES OF THE SCANNED NODES. */

            /* System generated locals */
            /* static */
            long i__1;

            /* Local variables */
            /* static */
            long node;
            long node2;
            long i__;
            long j;
            long nsave;
            long t1;
            long t2;
            long t3;
            long nb;
            long delprc;
            long rdcost;
            long arc;
            long dlx;

            delprc = input_1.large;
            dlx = 0;
            nsave = 0;

            if (nscan <= input_1.n / 2)
            {
                i__1 = nscan;
                for (i__ = 1; i__ <= i__1; ++i__)
                {
                    node = blks_1.label[i__ - 1];

                    arc = blks_1.fin[node - 1];
                    while (arc > 0) //L500:
                    {
                        node2 = arrayit_1.startn[arc - 1];
                        if (!blks2_1.scan[node2 - 1])
                        {
                            ++nsave;
                            blks_1.save[nsave - 1] = arc;
                            rdcost = arrayit_1.rc[arc - 1];
                            if (rdcost == 0 && blks_1.prdcsr[node2 - 1] != arc)
                            {
                                dlx += arrayit_1.x[arc - 1];
                            }
                            if (rdcost < 0 && rdcost > -delprc)
                            {
                                delprc = -rdcost;
                            }
                        }
                        arc = blks_1.nxtin[arc - 1];
                    } //goto L500;


                    arc = blks_1.fou[node - 1];
                    while (arc > 0) //L501:
                    {
                        node2 = arrayit_1.endn[arc - 1];
                        if (!blks2_1.scan[node2 - 1])
                        {
                            ++nsave;
                            blks_1.save[nsave - 1] = -arc;
                            rdcost = arrayit_1.rc[arc - 1];
                            if (rdcost == 0 && blks_1.prdcsr[node2 - 1] != -arc)
                            {
                                dlx += arrayit_1.u[arc - 1];
                            }
                            if (rdcost > 0 && rdcost < delprc)
                            {
                                delprc = rdcost;
                            }
                        }
                        arc = blks_1.nxtou[arc - 1];
                    } //goto L501;
                } // L1:
            }
            else
            {
                i__1 = input_1.n;
                for (node = 1; node <= i__1; ++node)
                {
                    if (blks2_1.scan[node - 1])
                    {
                        continue; //goto L2;
                    }

                    arc = blks_1.fou[node - 1];
                    while (arc > 0) //L502:
                    {
                        node2 = arrayit_1.endn[arc - 1];
                        if (blks2_1.scan[node2 - 1])
                        {
                            ++nsave;
                            blks_1.save[nsave - 1] = arc;
                            rdcost = arrayit_1.rc[arc - 1];
                            if (rdcost == 0 && blks_1.prdcsr[node - 1] != arc)
                            {
                                dlx += arrayit_1.x[arc - 1];
                            }
                            if (rdcost < 0 && rdcost > -delprc)
                            {
                                delprc = -rdcost;
                            }
                        }
                        arc = blks_1.nxtou[arc - 1];
                    } //goto L502;


                    arc = blks_1.fin[node - 1];
                    while (arc > 0) //L503:
                    {
                        node2 = arrayit_1.startn[arc - 1];
                        if (blks2_1.scan[node2 - 1])
                        {
                            ++nsave;
                            blks_1.save[nsave - 1] = -arc;
                            rdcost = arrayit_1.rc[arc - 1];
                            if (rdcost == 0 && blks_1.prdcsr[node - 1] != -arc)
                            {
                                dlx += arrayit_1.u[arc - 1];
                            }
                            if (rdcost > 0 && rdcost < delprc)
                            {
                                delprc = rdcost;
                            }
                        }
                        arc = blks_1.nxtin[arc - 1];
                    } //goto L503;
                } //L2:            ;
            }

            if (!(switch__) && delx + dlx >= -(dm))
            {
                switch__ = true;
                return 0;
            }
            delx += dlx;

        /*     CHECK THAT THE PROBLEM IS FEASIBLE. */

        L4:
            if (delprc == input_1.large)
            {
                feasbl = false;
                return 0;
            }


            if (switch__)
            {
                /*	INCREASE THE PRICES OF THE SCANNED NODES, ADD MORE */
                /*  NODES TO THE LABELED SET AND CHECK IF A NEWLY LABELED NODE */
                /*  HAS POSITIVE DEFICIT. */

                i__1 = nsave;
                for (i__ = 1; i__ <= i__1; ++i__)
                {
                    arc = blks_1.save[i__ - 1];
                    if (arc > 0)
                    {
                        arrayit_1.rc[arc - 1] += delprc;
                        if (arrayit_1.rc[arc - 1] == 0)
                        {
                            node2 = arrayit_1.startn[arc - 1];
                            if (blks2_1.nxtpushf[arc - 1] < 0)
                            {
                                blks2_1.nxtpushf[arc - 1] = blks2_1.fpushf[node2 - 1];
                                blks2_1.fpushf[node2 - 1] = arc;
                            }
                            if (blks2_1.nxtpushb[arc - 1] < 0)
                            {
                                blks2_1.nxtpushb[arc - 1] = blks2_1.fpushb[arrayit_1.endn[arc - 1] - 1];
                                blks2_1.fpushb[arrayit_1.endn[arc - 1] - 1] = arc;
                            }
                            if (!blks2_1.path_id__[node2 - 1])
                            {
                                blks_1.prdcsr[node2 - 1] = arc;
                                ++(nlabel);
                                blks_1.label[nlabel - 1] = node2;
                                blks2_1.path_id__[node2 - 1] = true;
                            }
                        }
                    }
                    else
                    {
                        arc = -arc;
                        arrayit_1.rc[arc - 1] -= delprc;
                        if (arrayit_1.rc[arc - 1] == 0)
                        {
                            node2 = arrayit_1.endn[arc - 1];
                            if (blks2_1.nxtpushf[arc - 1] < 0)
                            {
                                blks2_1.nxtpushf[arc - 1] = blks2_1.fpushf[arrayit_1.startn[arc - 1] - 1];
                                blks2_1.fpushf[arrayit_1.startn[arc - 1] - 1] = arc;
                            }
                            if (blks2_1.nxtpushb[arc - 1] < 0)
                            {
                                blks2_1.nxtpushb[arc - 1] = blks2_1.fpushb[node2 - 1];
                                blks2_1.fpushb[node2 - 1] = arc;
                            }
                            if (!blks2_1.path_id__[node2 - 1])
                            {
                                blks_1.prdcsr[node2 - 1] = -arc;
                                ++(nlabel);
                                blks_1.label[nlabel - 1] = node2;
                                blks2_1.path_id__[node2 - 1] = true;
                            }
                        }
                    }
                } // L7:
                return 0;
            }
            else
            {
                nb = 0;
                i__1 = nsave;
                for (i__ = 1; i__ <= i__1; ++i__)
                {
                    arc = blks_1.save[i__ - 1];
                    if (arc > 0)
                    {
                        t1 = arrayit_1.rc[arc - 1];
                        if (t1 == 0)
                        {
                            t2 = arrayit_1.x[arc - 1];
                            t3 = arrayit_1.startn[arc - 1];
                            arrayit_1.dfct[t3 - 1] -= t2;
                            if (blks3_1.nxtqueue[t3 - 1] == 0)
                            {
                                blks3_1.nxtqueue[prevnode - 1] = t3;
                                blks3_1.nxtqueue[t3 - 1] = curnode;
                                prevnode = t3;
                            }
                            t3 = arrayit_1.endn[arc - 1];
                            arrayit_1.dfct[t3 - 1] += t2;
                            if (blks3_1.nxtqueue[t3 - 1] == 0)
                            {
                                blks3_1.nxtqueue[prevnode - 1] = t3;
                                blks3_1.nxtqueue[t3 - 1] = curnode;
                                prevnode = t3;
                            }
                            arrayit_1.u[arc - 1] += t2;
                            arrayit_1.x[arc - 1] = 0;
                        }
                        arrayit_1.rc[arc - 1] = t1 + delprc;
                        if (arrayit_1.rc[arc - 1] == 0)
                        {
                            delx += arrayit_1.x[arc - 1];
                            ++nb;
                            blks_1.prdcsr[nb - 1] = arc;
                        }
                    }
                    else
                    {
                        arc = -arc;
                        t1 = arrayit_1.rc[arc - 1];
                        if (t1 == 0)
                        {
                            t2 = arrayit_1.u[arc - 1];
                            t3 = arrayit_1.startn[arc - 1];
                            arrayit_1.dfct[t3 - 1] += t2;
                            if (blks3_1.nxtqueue[t3 - 1] == 0)
                            {
                                blks3_1.nxtqueue[prevnode - 1] = t3;
                                blks3_1.nxtqueue[t3 - 1] = curnode;
                                prevnode = t3;
                            }
                            t3 = arrayit_1.endn[arc - 1];
                            arrayit_1.dfct[t3 - 1] -= t2;
                            if (blks3_1.nxtqueue[t3 - 1] == 0)
                            {
                                blks3_1.nxtqueue[prevnode - 1] = t3;
                                blks3_1.nxtqueue[t3 - 1] = curnode;
                                prevnode = t3;
                            }
                            arrayit_1.x[arc - 1] += t2;
                            arrayit_1.u[arc - 1] = 0;
                        }
                        arrayit_1.rc[arc - 1] = t1 - delprc;
                        if (arrayit_1.rc[arc - 1] == 0)
                        {
                            delx += arrayit_1.u[arc - 1];
                            ++nb;
                            blks_1.prdcsr[nb - 1] = arc;
                        }
                    }
                } // L6:
            }

            if (delx <= -(dm))
            {
                delprc = input_1.large;
                i__1 = nsave;
                for (i__ = 1; i__ <= i__1; ++i__)
                {
                    arc = blks_1.save[i__ - 1];
                    if (arc > 0)
                    {
                        rdcost = arrayit_1.rc[arc - 1];
                        if (rdcost < 0 && rdcost > -delprc)
                        {
                            delprc = -rdcost;
                        }
                    }
                    else
                    {
                        arc = -arc;
                        rdcost = arrayit_1.rc[arc - 1];
                        if (rdcost > 0 && rdcost < delprc)
                        {
                            delprc = rdcost;
                        }
                    }
                } // L10:

                if (delprc != input_1.large || delx < -(dm))
                {
                    goto L4;
                }
            }

            /*     ADD NEW BALANCED ARCS TO THE SUPERSET OF BALANCED ARCS. */

            i__1 = nb;
            for (i__ = 1; i__ <= i__1; ++i__)
            {
                arc = blks_1.prdcsr[i__ - 1];
                if (blks2_1.nxtpushb[arc - 1] == -1)
                {
                    j = arrayit_1.endn[arc - 1];
                    blks2_1.nxtpushb[arc - 1] = blks2_1.fpushb[j - 1];
                    blks2_1.fpushb[j - 1] = arc;
                }
                if (blks2_1.nxtpushf[arc - 1] == -1)
                {
                    j = arrayit_1.startn[arc - 1];
                    blks2_1.nxtpushf[arc - 1] = blks2_1.fpushf[j - 1];
                    blks2_1.fpushf[j - 1] = arc;
                }

            } // L9:
            return 0;
        } // ascnt2_
        public static int ascnt1_(ref long dm, ref long delx, ref long nlabel, ref bool feasbl, ref bool switch__, ref long nscan, ref long curnode, ref long prevnode)
        {
            /* --------------------------------------------------------------- */

            /*  PURPOSE - THIS SUBROUTINE PERFORMS THE MULTI-NODE PRICE */
            /*     ADJUSTMENT STEP FOR THE CASE WHERE THE SCANNED NODES */
            /*     HAVE POSITIVE DEFICIT.  IT FIRST CHECKS IF DECREASING */
            /*     THE PRICE OF THE SCANNED NODES INCREASES THE DUAL COST. */
            /*     IF YES, THEN IT DECREASES THE PRICE OF ALL SCANNED NODES. */
            /*     THERE ARE TWO POSSIBILITIES FOR PRICE DECREASE: */
            /*     IF SWITCH=.TRUE., THEN THE SET OF SCANNED NODES */
            /*     CORRESPONDS TO AN ELEMENTARY DIRECTION OF MAXIMAL */
            /*     RATE OF ASCENT, IN WHICH CASE THE PRICE OF ALL SCANNED */
            /*     NODES ARE DECREASED UNTIL THE NEXT BREAKPOINT IN THE */
            /*     DUAL COST IS ENCOUNTERED.  AT THIS POINT, SOME ARC */
            /*     BECOMES BALANCED AND MORE NODE(S) ARE ADDED TO THE */
            /*     LABELED SET AND THE SUBROUTINE IS EXITED. */
            /*     IF SWITCH=.FALSE., THEN THE PRICE OF ALL SCANNED NODES */
            /*     ARE DECREASED UNTIL THE RATE OF ASCENT BECOMES */
            /*     NEGATIVE (THIS CORRESPONDS TO THE PRICE ADJUSTMENT */
            /*     STEP IN WHICH BOTH THE LINE SEARCH AND THE DEGENERATE */
            /*     ASCENT ITERATION ARE IMPLEMENTED). */

            /* --------------------------------------------------------------- */

            /*     MAXNN = DIMENSION OF NODE-LENGTH ARRAYS */
            /*     MAXNA = DIMENSION OF ARC-LENGTH ARRAYS */


            /* ^^                                     B                     ^^ */
            /* ^^          TEMPIN I1 P,TEMPOU PRICE I2,I3,I4,I5,I6,I7        ^^ */
            /* ^^                      MARK   TFSTOU  TNXTOU  TFSTIN TNXTIN ^^ */
            /* ^^                  I14      I15        I16     I17          ^^ */

            /*  INPUT PARAMETERS */

            /*     DM        = TOTAL DEFICIT OF SCANNED NODES */
            /*     SWITCH    = .TRUE. IF LABELING IS TO CONTINUE AFTER PRICE CHANGE */
            /*     NSCAN     = NUMBER OF SCANNED NODES */
            /*     CURNODE   = MOST RECENTLY SCANNED NODE */
            /*     N         = NUMBER OF NODES */
            /*     NA        = NUMBER OF ARCS */
            /*     LARGE     = A VERY LARGE INTEGER TO REPRESENT INFINITY */
            /*                 (SEE NOTE 3) */
            /*     STARTN(I) = STARTING NODE FOR THE I-TH ARC,    I = 1,...,NA */
            /*     ENDN(I)   = ENDING NODE FOR THE I-TH ARC,      I = 1,...,NA */
            /*     FOU(I)    = FIRST ARC LEAVING I-TH NODE,       I = 1,...,N */
            /*     NXTOU(I)  = NEXT ARC LEAVING THE STARTING NODE OF J-TH ARC, */
            /*                                                    I = 1,...,NA */
            /*     FIN(I)    = FIRST ARC ENTERING I-TH NODE,      I = 1,...,N */
            /*     NXTIN(I)  = NEXT ARC ENTERING THE ENDING NODE OF J-TH ARC, */
            /*                                                    I = 1,...,NA */


            /*  UPDATED PARAMETERS */

            /*     DELX      = A LOWER ESTIMATE OF THE TOTAL FLOW ON BALANCED ARCS */
            /*                 IN THE SCANNED-NODES CUT */
            /*     NLABEL    = NUMBER OF LABELED NODES */
            /*     FEASBL    = .FALSE. IF PROBLEM IS FOUND TO BE INFEASIBLE */
            /*     PREVNODE  = THE NODE BEFORE CURNODE IN QUEUE */
            /*     RC(J)     = REDUCED COST OF ARC J,             J = 1,...,NA */
            /*     U(J)      = RESIDUAL CAPACITY OF ARC J, */
            /*                                                    J = 1,...,NA */
            /*     X(J)      = FLOW ON ARC J,                     J = 1,...,NA */
            /*     DFCT(I)   = DEFICIT AT NODE I,                 I = 1,...,N */
            /*     LABEL(K)  = K-TH NODE LABELED,                 K = 1,NLABEL */
            /*     PRDCSR(I) = PREDECESSOR OF NODE I IN TREE OF LABELED NODES */
            /*                 (O IF I IS UNLABELED),             I = 1,...,N */
            /*     FPUSHF(I) = FIRST BALANCED ARC OUT OF NODE I,  I = 1,...,N */
            /*     NXTPUSHF(J) = NEXT BALANCED ARC OUT OF THE STARTING NODE OF ARC J, */
            /*                                                    J = 1,...,NA */
            /*     FPUSHB(I) = FIRST BALANCED ARC INTO NODE I,  I = 1,...,N */
            /*     NXTPUSHB(J) = NEXT BALANCED ARC INTO THE ENDING NODE OF ARC J, */
            /*                                                    J = 1,...,NA */
            /*     NXTQUEUE(I) = NODE FOLLOWING NODE I IN THE FIFO QUEUE */
            /*                   (0 IF NODE IS NOT IN THE QUEUE), I = 1,...,N */
            /*     SCAN(I)   = .TRUE. IF NODE I IS SCANNED,       I = 1,...,N */
            /*     PATH_ID(I)   = .TRUE. IF NODE I IS LABELED,       I = 1,...,N */


            /*  WORKING PARAMETERS */


            /*     STORE THE ARCS BETWEEN THE SET OF SCANNED NODES AND */
            /*     ITS COMPLEMENT IN SAVE AND COMPUTE DELPRC, THE STEPSIZE */
            /*     TO THE NEXT BREAKPOINT IN THE DUAL COST IN THE DIRECTION */
            /*     OF DECREASING PRICES OF THE SCANNED NODES. */
            /*     [THE ARCS ARE STORED INTO SAVE BY LOOKING AT THE ARCS */
            /*     INCIDENT TO EITHER THE SET OF SCANNED NODES OR ITS */
            /*     COMPLEMENT, DEPENDING ON WHETHER NSCAN>N/2 OR NOT. */
            /*     THIS IMPROVES THE EFFICIENCY OF STORING.] */

            /* System generated locals */
            /* static */
            long i__1;

            /* Local variables */
            /* static */
            long node;
            long node2;
            long i__;
            long j;
            long nsave;
            long t1;
            long t2;
            long t3;
            long nb;
            long delprc;
            long rdcost;
            long arc;
            long dlx;

            delprc = input_1.large;
            dlx = 0;
            nsave = 0;

            if (nscan <= input_1.n / 2)
            {
                i__1 = nscan;
                for (i__ = 1; i__ <= i__1; ++i__)
                {
                    node = blks_1.label[i__ - 1];

                    arc = blks_1.fou[node - 1];
                    while (arc > 0) //L500:
                    {

                        /*	ARC POINTS FROM SCANNED NODE TO AN UNSCANNED NODE. */

                        node2 = arrayit_1.endn[arc - 1];
                        if (!blks2_1.scan[node2 - 1])
                        {
                            ++nsave;
                            blks_1.save[nsave - 1] = arc;
                            rdcost = arrayit_1.rc[arc - 1];
                            if (rdcost == 0 && blks_1.prdcsr[node2 - 1] != arc)
                            {
                                dlx += arrayit_1.x[arc - 1];
                            }
                            if (rdcost < 0 && rdcost > -delprc)
                            {
                                delprc = -rdcost;
                            }
                        }
                        arc = blks_1.nxtou[arc - 1];
                    } //goto L500;

                    arc = blks_1.fin[node - 1];
                    while (arc > 0) //L501:
                    {

                        /*	ARC POINTS FROM UNSCANNED NODE TO SCANNED NODE. */

                        node2 = arrayit_1.startn[arc - 1];
                        if (!blks2_1.scan[node2 - 1])
                        {
                            ++nsave;
                            blks_1.save[nsave - 1] = -arc;
                            rdcost = arrayit_1.rc[arc - 1];
                            if (rdcost == 0 && blks_1.prdcsr[node2 - 1] != -arc)
                            {
                                dlx += arrayit_1.u[arc - 1];
                            }
                            if (rdcost > 0 && rdcost < delprc)
                            {
                                delprc = rdcost;
                            }
                        }
                        arc = blks_1.nxtin[arc - 1];

                    } //goto L501;
                } // L1:
            }
            else
            {
                i__1 = input_1.n;
                for (node = 1; node <= i__1; ++node)
                {
                    if (blks2_1.scan[node - 1])
                    {
                        continue; //goto L2;
                    }

                    arc = blks_1.fin[node - 1];
                    while (arc > 0) //L502:
                    {
                        node2 = arrayit_1.startn[arc - 1];
                        if (blks2_1.scan[node2 - 1])
                        {
                            ++nsave;
                            blks_1.save[nsave - 1] = arc;
                            rdcost = arrayit_1.rc[arc - 1];
                            if (rdcost == 0 && blks_1.prdcsr[node - 1] != arc)
                            {
                                dlx += arrayit_1.x[arc - 1];
                            }
                            if (rdcost < 0 && rdcost > -delprc)
                            {
                                delprc = -rdcost;
                            }
                        }
                        arc = blks_1.nxtin[arc - 1];
                    } //goto L502;


                    arc = blks_1.fou[node - 1];
                    while (arc > 0) //L503:
                    {
                        node2 = arrayit_1.endn[arc - 1];
                        if (blks2_1.scan[node2 - 1])
                        {
                            ++nsave;
                            blks_1.save[nsave - 1] = -arc;
                            rdcost = arrayit_1.rc[arc - 1];
                            if (rdcost == 0 && blks_1.prdcsr[node - 1] != -arc)
                            {
                                dlx += arrayit_1.u[arc - 1];
                            }
                            if (rdcost > 0 && rdcost < delprc)
                            {
                                delprc = rdcost;
                            }
                        }
                        arc = blks_1.nxtou[arc - 1];
                    } //goto L503;
                } //L2:            ;
            }

            /*     CHECK IF THE SET OF SCANNED NODES TRULY CORRESPONDS */
            /*     TO A DUAL ASCENT DIRECTION.  [HERE DELX+DLX IS THE EXACT */
            /*     SUM OF THE FLOW ON ARCS FROM THE SCANNED SET TO THE */
            /*     UNSCANNED SET PLUS THE (CAPACITY - FLOW) ON ARCS FROM */
            /*     THE UNSCANNED SET TO THE SCANNED SET.] */
            /*     IF THIS WERE NOT THE CASE, SET SWITCH TO .TRUE. */
            /*     AND EXIT SUBROUTINE. */

            if (!(switch__) && delx + dlx >= dm)
            {
                switch__ = true;
                return 0;
            }
            delx += dlx;

        /*     CHECK THAT THE PROBLEM IS FEASIBLE. */
        L4:
            if (delprc == input_1.large)
            {

                /*	WE CAN INCREASE THE DUAL COST WITHOUT BOUND, SO */
                /*  THE PRIMAL PROBLEM IS INFEASIBLE. */

                feasbl = false;
                return 0;
            }


            if (switch__)
            {
                /*	DECREASE THE PRICES OF THE SCANNED NODES, ADD MORE */
                /*  NODES TO THE LABELED SET AND CHECK IF A NEWLY LABELED NODE */
                /*  HAS NEGATIVE DEFICIT. */

                i__1 = nsave;
                for (i__ = 1; i__ <= i__1; ++i__)
                {
                    arc = blks_1.save[i__ - 1];
                    if (arc > 0)
                    {
                        arrayit_1.rc[arc - 1] += delprc;
                        if (arrayit_1.rc[arc - 1] == 0)
                        {
                            node2 = arrayit_1.endn[arc - 1];
                            if (blks2_1.nxtpushf[arc - 1] < 0)
                            {
                                blks2_1.nxtpushf[arc - 1] = blks2_1.fpushf[arrayit_1.startn[arc - 1] - 1];
                                blks2_1.fpushf[arrayit_1.startn[arc - 1] - 1] = arc;
                            }
                            if (blks2_1.nxtpushb[arc - 1] < 0)
                            {
                                blks2_1.nxtpushb[arc - 1] = blks2_1.fpushb[node2 - 1];
                                blks2_1.fpushb[node2 - 1] = arc;
                            }
                            if (!blks2_1.path_id__[node2 - 1])
                            {
                                blks_1.prdcsr[node2 - 1] = arc;
                                ++(nlabel);
                                blks_1.label[nlabel - 1] = node2;
                                blks2_1.path_id__[node2 - 1] = true;
                            }
                        }
                    }
                    else
                    {
                        arc = -arc;
                        arrayit_1.rc[arc - 1] -= delprc;
                        if (arrayit_1.rc[arc - 1] == 0)
                        {
                            node2 = arrayit_1.startn[arc - 1];
                            if (blks2_1.nxtpushf[arc - 1] < 0)
                            {
                                blks2_1.nxtpushf[arc - 1] = blks2_1.fpushf[node2 - 1];
                                blks2_1.fpushf[node2 - 1] = arc;
                            }
                            if (blks2_1.nxtpushb[arc - 1] < 0)
                            {
                                blks2_1.nxtpushb[arc - 1] = blks2_1.fpushb[arrayit_1.endn[arc - 1] - 1];
                                blks2_1.fpushb[arrayit_1.endn[arc - 1] - 1] = arc;
                            }
                            if (!blks2_1.path_id__[node2 - 1])
                            {
                                blks_1.prdcsr[node2 - 1] = -arc;
                                ++(nlabel);
                                blks_1.label[nlabel - 1] = node2;
                                blks2_1.path_id__[node2 - 1] = true;
                            }
                        }
                    }
                } // L7:
                return 0;
            }
            else
            {

                /*	DECREASE THE PRICES OF THE SCANNED NODES BY DELPRC. */
                /*  ADJUST FLOW TO MAINTAIN COMPLEMENTARY SLACKNESS WITH */
                /*  THE PRICES. */

                nb = 0;
                i__1 = nsave;
                for (i__ = 1; i__ <= i__1; ++i__)
                {
                    arc = blks_1.save[i__ - 1];
                    if (arc > 0)
                    {
                        t1 = arrayit_1.rc[arc - 1];
                        if (t1 == 0)
                        {
                            t2 = arrayit_1.x[arc - 1];
                            t3 = arrayit_1.startn[arc - 1];
                            arrayit_1.dfct[t3 - 1] -= t2;
                            if (blks3_1.nxtqueue[t3 - 1] == 0)
                            {
                                blks3_1.nxtqueue[prevnode - 1] = t3;
                                blks3_1.nxtqueue[t3 - 1] = curnode;
                                prevnode = t3;
                            }
                            t3 = arrayit_1.endn[arc - 1];
                            arrayit_1.dfct[t3 - 1] += t2;
                            if (blks3_1.nxtqueue[t3 - 1] == 0)
                            {
                                blks3_1.nxtqueue[prevnode - 1] = t3;
                                blks3_1.nxtqueue[t3 - 1] = curnode;
                                prevnode = t3;
                            }
                            arrayit_1.u[arc - 1] += t2;
                            arrayit_1.x[arc - 1] = 0;
                        }
                        arrayit_1.rc[arc - 1] = t1 + delprc;
                        if (arrayit_1.rc[arc - 1] == 0)
                        {
                            delx += arrayit_1.x[arc - 1];
                            ++nb;
                            blks_1.prdcsr[nb - 1] = arc;
                        }
                    }
                    else
                    {
                        arc = -arc;
                        t1 = arrayit_1.rc[arc - 1];
                        if (t1 == 0)
                        {
                            t2 = arrayit_1.u[arc - 1];
                            t3 = arrayit_1.startn[arc - 1];
                            arrayit_1.dfct[t3 - 1] += t2;
                            if (blks3_1.nxtqueue[t3 - 1] == 0)
                            {
                                blks3_1.nxtqueue[prevnode - 1] = t3;
                                blks3_1.nxtqueue[t3 - 1] = curnode;
                                prevnode = t3;
                            }
                            t3 = arrayit_1.endn[arc - 1];
                            arrayit_1.dfct[t3 - 1] -= t2;
                            if (blks3_1.nxtqueue[t3 - 1] == 0)
                            {
                                blks3_1.nxtqueue[prevnode - 1] = t3;
                                blks3_1.nxtqueue[t3 - 1] = curnode;
                                prevnode = t3;
                            }
                            arrayit_1.x[arc - 1] += t2;
                            arrayit_1.u[arc - 1] = 0;
                        }
                        arrayit_1.rc[arc - 1] = t1 - delprc;
                        if (arrayit_1.rc[arc - 1] == 0)
                        {
                            delx += arrayit_1.u[arc - 1];
                            ++nb;
                            blks_1.prdcsr[nb - 1] = arc;
                        }
                    }
                } // L6:
            }

            if (delx <= dm)
            {

                /*	THE SET OF SCANNED NODES STILL CORRESPONDS TO A */
                /*  DUAL (POSSIBLY DEGENERATE) ASCENT DIRECTON.  COMPUTE */
                /*  THE STEPSIZE DELPRC TO THE NEXT BREAKPOINT IN THE */
                /*  DUAL COST. */

                delprc = input_1.large;
                i__1 = nsave;
                for (i__ = 1; i__ <= i__1; ++i__)
                {
                    arc = blks_1.save[i__ - 1];
                    if (arc > 0)
                    {
                        rdcost = arrayit_1.rc[arc - 1];
                        if (rdcost < 0 && rdcost > -delprc)
                        {
                            delprc = -rdcost;
                        }
                    }
                    else
                    {
                        arc = -arc;
                        rdcost = arrayit_1.rc[arc - 1];
                        if (rdcost > 0 && rdcost < delprc)
                        {
                            delprc = rdcost;
                        }
                    }
                } // L10:

                if (delprc != input_1.large || delx < dm)
                {
                    goto L4;
                }
            }

            /*     ADD NEW BALANCED ARCS TO THE SUPERSET OF BALANCED ARCS. */

            i__1 = nb;
            for (i__ = 1; i__ <= i__1; ++i__)
            {
                arc = blks_1.prdcsr[i__ - 1];
                if (blks2_1.nxtpushb[arc - 1] == -1)
                {
                    j = arrayit_1.endn[arc - 1];
                    blks2_1.nxtpushb[arc - 1] = blks2_1.fpushb[j - 1];
                    blks2_1.fpushb[j - 1] = arc;
                }
                if (blks2_1.nxtpushf[arc - 1] == -1)
                {
                    j = arrayit_1.startn[arc - 1];
                    blks2_1.nxtpushf[arc - 1] = blks2_1.fpushf[j - 1];
                    blks2_1.fpushf[j - 1] = arc;
                }
            } // L9:

            return 0;
        } // ascnt1_

        /* Subroutine */
        public static int inidat_()
        {
            /* --------------------------------------------------------------- */

            /*  PURPOSE - THIS ROUTINE CONSTRUCTS TWO LINKED LISTS FOR */
            /*     THE NETWORK TOPOLOGY: ONE LIST (GIVEN BY FOU, NXTOU) FOR */
            /*     THE OUTGOING ARCS OF NODES AND ONE LIST (GIVEN BY FIN, */
            /*     NXTIN) FOR THE INCOMING ARCS OF NODES.  THESE TWO LISTS */
            /*     ARE REQUIRED BY RELAX4. */
            /* --------------------------------------------------------------- */

            /*     MAXNN = DIMENSION OF NODE-LENGTH ARRAYS */
            /*     MAXNA = DIMENSION OF ARC-LENGTH ARRAYS */


            /*  INPUT PARAMETERS */

            /*     N         = NUMBER OF NODES */
            /*     NA        = NUMBER OF ARCS */
            /*     STARTN(J) = STARTING NODE FOR ARC J,           J = 1,...,NA */
            /*     ENDN(J)   = ENDING NODE FOR ARC J,             J = 1,...,NA */

            /* ^^                                     B                     ^^ */
            /* ^^          TEMPIN I1 P,TEMPOU PRICE I2,I3,I4,I5,I6,I7        ^^ */
            /* ^^                      MARK   TFSTOU  TNXTOU  TFSTIN TNXTIN ^^ */
            /* ^^                  I14      I15        I16     I17          ^^ */

            /*  OUTPUT PARAMETERS */

            /*     FOU(I)    = FIRST ARC OUT OF NODE I,           I = 1,...,N */
            /*     NXTOU(J)  = NEXT ARC OUT OF THE STARTING NODE OF ARC J, */
            /*                                                    J = 1,...,NA */
            /*     FIN(I)    = FIRST ARC INTO NODE I,             I = 1,...,N */
            /*     NXTIN(J)  = NEXT ARC INTO THE ENDING NODE OF ARC J, */
            /*                                                    J = 1,...,NA */

            /*  WORKING PARAMETERS */
            /*    "TEMPIN", "TEMPOU" */

            /* --------------------------------------------------------------- */
            int i;
            long n;
            long na;
            long node_1;
            long node_2;

            n = input_1.n;
            na = input_1.na;

            //#pragma omp parallel for
            for (i = 1; i <= n; ++i)
            {
                //int ithread = omp_get_thread_num();

                blks_1.fin[i - 1] = 0;
                blks_1.fou[i - 1] = 0;
                blks_1.label[i - 1] = 0;
                blks_1.prdcsr[i - 1] = 0;
            } // L10:

            //#pragma omp parallel for
            for (i = 1; i <= na; ++i)
            {
                //int ithread = omp_get_thread_num();

                blks_1.nxtin[i - 1] = 0;
                blks_1.nxtou[i - 1] = 0;

                node_1 = arrayit_1.startn[i - 1];
                node_2 = arrayit_1.endn[i - 1];

                //#pragma omp critical (fou)
                {
                    if (blks_1.fou[node_1 - 1] != 0)
                    {
                        blks_1.nxtou[blks_1.prdcsr[node_1 - 1] - 1] = i;
                    }
                    else
                    {
                        blks_1.fou[node_1 - 1] = i;
                    }
                    blks_1.prdcsr[node_1 - 1] = i;
                }

                //#pragma omp critical (fin)
                {
                    if (blks_1.fin[node_2 - 1] != 0)
                    {
                        blks_1.nxtin[blks_1.label[node_2 - 1] - 1] = i;
                    }
                    else
                    {
                        blks_1.fin[node_2 - 1] = i;
                    }
                    blks_1.label[node_2 - 1] = i;
                }
            } // L20:
            return 0;
        } // inidat_
        /* Subroutine */
        public static int relax4_(Model mi)
        {
            /* --------------------------------------------------------------- */

            /*                 RELAX-IV  (VERSION OF OCTOBER 1994) */

            /*  RELEASE NOTE - THIS VERSION OF RELAXATION CODE HAS OPTION FOR */
            /*     A SPECIAL CRASH PROCEDURE FOR */
            /*     THE INITIAL PRICE-FLOW PAIR. THIS IS RECOMMENDED FOR DIFFICULT */
            /*     PROBLEMS WHERE THE DEFAULT INITIALIZATION */
            /*     RESULTS IN LONG RUNNING TIMES. */
            /*     CRASH =1 CORRESPONDS TO AN AUCTION/SHORTEST PATH METHOD */

            /*     THESE INITIALIZATIONS ARE RECOMMENDED IN THE ABSENCE OF ANY */
            /*     PRIOR INFORMATION ON A FAVORABLE INITIAL FLOW-PRICE VECTOR PAIR */
            /*     THAT SATISFIES COMPLEMENTARY SLACKNESS */

            /*     THE RELAXATION PORTION OF THE CODE DIFFERS FROM THE CODE RELAXT-III */
            /*     AND OTHER EARLIER RELAXATION CODES IN THAT IT MAINTAINS */
            /*     THE SET OF NODES WITH NONZERO DEFICIT IN A FIFO QUEUE. */
            /*     LIKE ITS PREDECESSOR RELAXT-III, THIS CODE MAINTAINS A LINKED LIST */
            /*     OF BALANCED (I.E., OF ZERO REDUCED COST) ARCS SO TO REDUCE */
            /*     THE WORK IN LABELING AND SCANNING. */
            /*     UNLIKE RELAXT-III, IT DOES NOT USE SELECTIVELY */
            /*     SHORTEST PATH ITERATIONS FOR INITIALIZATION. */

            /* --------------------------------------------------------------- */

            /*  PURPOSE - THIS ROUTINE IMPLEMENTS THE RELAXATION METHOD */
            /*     OF BERTSEKAS AND TSENG (SEE [1], [2]) FOR LINEAR */
            /*     COST ORDINARY NETWORK FLOW PROBLEMS. */

            /*  [1] BERTSEKAS, D. P., "A UNIFIED FRAMEWORK FOR PRIMAL-DUAL METHODS ..." */
            /*      MATHEMATICAL PROGRAMMING, VOL. 32, 1985, PP. 125-145. */
            /*  [2] BERTSEKAS, D. P., AND TSENG, P., "RELAXATION METHODS FOR */
            /*      MINIMUM COST ..." OPERATIONS RESEARCH, VOL. 26, 1988, PP. 93-114. */

            /*     THE RELAXATION METHOD IS ALSO DESCRIBED IN THE BOOKS: */

            /*  [3] BERTSEKAS, D. P., "LINEAR NETWORK OPTIMIZATION: ALGORITHMS AND CODES" */
            /*      MIT PRESS, 1991. */
            /*  [4] BERTSEKAS, D. P. AND TSITSIKLIS, J. N., "PARALLEL AND DISTRIBUTED */
            /*      COMPUTATION: NUMERICAL METHODS", PRENTICE-HALL, 1989. */



            /* --------------------------------------------------------------- */

            /*  SOURCE -  THIS CODE WAS WRITTEN BY DIMITRI P. BERTSEKAS */
            /*     AND PAUL TSENG, WITH A CONTRIBUTION BY JONATHAN ECKSTEIN */
            /*     IN THE PHASE II INITIALIZATION.  THE ROUTINE AUCTION WAS WRITTEN */
            /*     BY DIMITRI P. BERTSEKAS AND IS BASED ON THE METHOD DESCRIBED IN */
            /*     THE PAPER: */

            /*  [5] BERTSEKAS, D. P., "AN AUCTION/SEQUENTIAL SHORTEST PATH ALGORITHM */
            /*      FOR THE MINIMUM COST FLOW PROBLEM", LIDS REPORT P-2146, MIT, NOV. 1992. */

            /*     FOR INQUIRIES ABOUT THE CODE, PLEASE CONTACT: */

            /*     DIMITRI P. BERTSEKAS */
            /*     LABORATORY FOR INFORMATION AND DECISION SYSTEMS */
            /*     MASSACHUSETTS INSTITUTE OF TECHNOLOGY */
            /*     CAMBRIDGE, MA 02139 */
            /*     (617) 253-7267, DIMITRIB@MIT.EDU */

            /* --------------------------------------------------------------- */

            /*  USER GUIDELINES - */

            /*     THIS ROUTINE IS IN THE PUBLIC DOMAIN TO BE USED ONLY FOR RESEARCH */
            /*     PURPOSES.  IT CANNOT BE USED AS PART OF A COMMERCIAL PRODUCT, OR */
            /*     TO SATISFY IN ANY PART COMMERCIAL DELIVERY REQUIREMENTS TO */
            /*     GOVERNMENT OR INDUSTRY, WITHOUT PRIOR AGREEMENT WITH THE AUTHORS. */
            /*     USERS ARE REQUESTED TO ACKNOWLEDGE THE AUTHORSHIP OF THE CODE, */
            /*     AND THE RELAXATION METHOD.  THEY SHOULD ALSO REGISTER WITH THE */
            /*     AUTHORS TO RECEIVE UPDATES AND SUBSEQUENT RELEASES. */

            /*     NO MODIFICATION SHOULD BE MADE TO THIS CODE OTHER */
            /*     THAN THE MINIMAL NECESSARY */
            /*     TO MAKE IT COMPATIBLE WITH THE FORTRAN COMPILERS OF SPECIFIC */
            /*     MACHINES.  WHEN REPORTING COMPUTATIONAL RESULTS PLEASE BE SURE */
            /*     TO DESCRIBE THE MEMORY LIMITATIONS OF YOUR MACHINE. GENERALLY */
            /*     RELAX4 REQUIRES MORE MEMORY THAN PRIMAL SIMPLEX CODES AND MAY */
            /*     BE PENALIZED SEVERELY BY LIMITED MACHINE MEMORY. */

            /* --------------------------------------------------------------- */

            /*     MAXNN = DIMENSION OF NODE-LENGTH ARRAYS */
            /*     MAXNA = DIMENSION OF ARC-LENGTH ARRAYS */


            /*  INPUT PARAMETERS (SEE NOTES 1, 2, 4) */

            /*     N         = NUMBER OF NODES */
            /*     NA        = NUMBER OF ARCS */
            /*     LARGE     = A VERY LARGE INTEGER TO REPRESENT INFINITY */
            /*                 (SEE NOTE 3) */
            /*     REPEAT    = .TRUE. IF INITIALIZATION IS TO BE SKIPPED */
            /*                 (.FALSE. OTHERWISE) */
            /*     CRASH     = 0 IF DEFAULT INITIALIZATION IS USED */
            /*                 1 IF AUCTION INITIALIZATION IS USED */
            /*     STARTN(J) = STARTING NODE FOR ARC J,           J = 1,...,NA */
            /*     ENDN(J)   = ENDING NODE FOR ARC J,             J = 1,...,NA */
            /*     FOU(I)    = FIRST ARC OUT OF NODE I,          I = 1,...,N */
            /*     NXTOU(J)  = NEXT ARC OUT OF THE STARTING NODE OF ARC J, */
            /*                                                    J = 1,...,NA */
            /*     FIN(I)    = FIRST ARC INTO NODE I,             I = 1,...,N */
            /*     NXTIN(J)  = NEXT ARC INTO THE ENDING NODE OF ARC J, */
            /*                                                    J = 1,...,NA */

            /* ^^                                     B                     ^^ */
            /* ^^          TEMPIN I1 P,TEMPOU PRICE I2,I3,I4,I5,I6,I7        ^^ */
            /* ^^                      MARK   TFSTOU  TNXTOU  TFSTIN TNXTIN ^^ */
            /* ^^                  I14      I15        I16     I17          ^^ */

            /*  UPDATED PARAMETERS (SEE NOTES 1, 3, 4) */

            /*     RC(J)     = REDUCED COST OF ARC J,             J = 1,...,NA */
            /*     U(J)      = CAPACITY OF ARC J ON INPUT */
            /*                 AND (CAPACITY OF ARC J) - X(J) ON OUTPUT, */
            /*                                                    J = 1,...,NA */
            /*     DFCT(I)   = DEMAND AT NODE I ON INPUT */
            /*                 AND ZERO ON OUTPUT,                I = 1,...,N */


            /*  OUTPUT PARAMETERS (SEE NOTES 1, 3, 4) */

            /*     X(J)      = FLOW ON ARC J,                     J = 1,...,NA */
            /*     NMULTINODE = NUMBER OF MULTINODE RELAXATION ITERATIONS IN RELAX4 */
            /*     ITER       = NUMBER OF RELAXATION ITERATIONS IN RELAX4 */
            /*     NUM_AUGM   = NUMBER OF FLOW AUGMENTATION STEPS IN RELAX4 */
            /*     NUM_ASCNT  = NUMBER OF MULTINODE ASCENT STEPS IN RELAX4 */
            /*     NSP        = NUMBER OF AUCTION/SHORTEST PATH ITERATIONS */


            /*  WORKING PARAMETERS (SEE NOTES 1, 4, 5) */


            /*  TIMING PARAMETERS */


            /*  NOTE 1 - */
            /*     TO RUN IN LIMITED MEMORY SYSTEMS, DECLARE THE ARRAYS */
            /*     STARTN, ENDN, NXTIN, NXTOU, FIN, FOU, LABEL, */
            /*     PRDCSR, SAVE, TFSTOU, TNXTOU, TFSTIN, TNXTIN, */
            /*     DDPOS,DDNEG,NXTQUEUE AS INTEGER*2 INSTEAD. */

            /*  NOTE 2 - */
            /*     THIS ROUTINE MAKES NO EFFORT TO INITIALIZE WITH A FAVORABLE X */
            /*     FROM AMONGST THOSE FLOW VECTORS THAT SATISFY COMPLEMENTARY SLACKNESS */
            /*     WITH THE INITIAL REDUCED COST VECTOR RC. */
            /*     IF A FAVORABLE X IS KNOWN, THEN IT CAN BE PASSED, TOGETHER */
            /*     WITH THE CORRESPONDING ARRAYS U AND DFCT, TO THIS ROUTINE */
            /*     DIRECTLY.  THIS, HOWEVER, REQUIRES THAT THE CAPACITY */
            /*     TIGHTENING PORTION AND THE FLOW INITIALIZATION PORTION */
            /*     OF THIS ROUTINE (UP TO LINE LABELED 90) BE SKIPPED. */

            /*  NOTE 3 - */
            /*     ALL PROBLEM DATA SHOULD BE LESS THAN LARGE IN MAGNITUDE, */
            /*     AND LARGE SHOULD BE LESS THAN, SAY, 1/4 THE LARGEST INTEGER*4 */
            /*     OF THE MACHINE USED.  THIS WILL GUARD PRIMARILY AGAINST */
            /*     OVERFLOW IN UNCAPACITATED PROBLEMS WHERE THE ARC CAPACITIES */
            /*     ARE TAKEN FINITE BUT VERY LARGE.  NOTE, HOWEVER, THAT AS IN */
            /*     ALL CODES OPERATING WITH INTEGERS, OVERFLOW MAY OCCUR IF SOME */
            /*     OF THE PROBLEM DATA TAKES VERY LARGE VALUES. */

            /*  NOTE 4 - */
            /*     EACH COMMON BLOCK CONTAINS JUST ONE ARRAY, SO THE ARRAYS IN RELAX4 */
            /*     CAN BE DIMENSIONED TO 1 AND TAKE THEIR DIMENSION FROM THE */
            /*     MAIN CALLING ROUTINE.  WITH THIS TRICK, RELAX4 NEED NOT BE RECOMPILED */
            /*     IF THE ARRAY DIMENSIONS IN THE CALLING ROUTINE CHANGE. */
            /*     IF YOUR FORTRAN COMPILER DOES NOT SUPPORT THIS FEATURE, THEN */
            /*     CHANGE THE DIMENSION OF ALL THE ARRAYS TO BE THE SAME AS THE ONES */
            /*     DECLARED IN THE MAIN CALLING PROGRAM. */

            /*  NOTE 5 - */
            /*       Note :  EQUIVALENCE(DDPOS,TFSTOU),(DDNEG,TFSTIN)  : */
            /*     DDPOS AND DDNEG ARE ARRAYS THAT GIVE THE DIRECTIONAL DERIVATIVES FOR */
            /*     ALL POSITIVE AND NEGATIVE SINGLE-NODE PRICE CHANGES.  THESE ARE USED */
            /*     ONLY IN PHASE II OF THE INITIALIZATION PROCEDURE, BEFORE THE */
            /*     LINKED LIST OF BALANCED ARCS COMES TO PLAY.  THEREFORE, TO REDUCE */
            /*     STORAGE, THEY ARE EQUIVALENCE TO TFSTOU AND TFSTIN, */
            /*     WHICH ARE OF THE SAME SIZE (NUMBER OF NODES) AND ARE USED */
            /*     ONLY AFTER THE TREE COMES INTO USE. */

            /* --------------------------------------------------------------- */

            /*  INITIALIZATION PHASE I */

            /*     IN THIS PHASE, WE REDUCE THE ARC CAPACITIES BY AS MUCH AS */
            /*     POSSIBLE WITHOUT CHANGING THE PROBLEM; */
            /*     THEN WE SET THE INITIAL FLOW ARRAY X, TOGETHER WITH */
            /*     THE CORRESPONDING ARRAYS U AND DFCT. */

            /*     THIS PHASE AND PHASE II (FROM HERE UP TO LINE LABELED 90) */
            /*     CAN BE SKIPPED (BY SETTING REPEAT TO .TRUE.) IF THE CALLING PROGRAM */
            /*     PLACES IN COMMON USER-CHOSEN VALUES FOR THE ARC FLOWS, THE RESIDUAL ARC */
            /*     CAPACITIES, AND THE NODAL DEFICITS.  WHEN THIS IS DONE, */
            /*     IT IS CRITICAL THAT THE FLOW AND THE REDUCED COST FOR EACH ARC */
            /*     SATISFY COMPLEMENTARY SLACKNESS */
            /*     AND THE DFCT ARRAY PROPERLY CORRESPOND TO THE INITIAL ARC/FLOWS. */
            /* --------------------------------------------------------------- */

            /* System generated locals */
            long i__2 = 0;
            long i__3 = 0;


            /* Local variables */
            long narc = 0;
            long node = 0;
            long delx = 0;
            // node_def__,
            long prevnode = 0;
            long node2 = 0;
            long i__ = 0;
            long j = 0;
            long t = 0;
            long indef = 0;
            long nscan = 0;
            // capin,
            long t1 = 0;
            long t2 = 0;
            long numnz = 0;
            long lastqueue = 0;
            long numpasses = 0;
            long numnz_new__ = 0;
            long ib = 0;
            long nb = 0;
            long dm = 0;
            long dp = 0;
            long nlabel = 0;
            long defcit = 0;
            long dx = 0;
            long tp = 0;
            long ts = 0;
            long delprc = 0;
            long augnod = 0;
            long tmparc = 0;
            long passes = 0;
            long rdcost = 0;
            // scapou, capout, maxcap, scapin,
            long nxtarc = 0;
            long prvarc = 0;
            long nxtbrk = 0;
            long num_passes__ = 0;
            long arc = 0;
            long trc = 0;
            long naugnod = 0;
            long nxtnode = 0;

            bool quit = false;
            bool posit = false;
            bool switch__ = false;
            bool pchange = false;

            bool fg_doloop = false;
            bool fg_aug = false;
            bool fg_pchange = false;


            if (input_1.repeat)
            {
                goto L90;
            }

            //#pragma omp parallel for private(arc) num_threads(NOMP)
            for (node = 1; node <= input_1.n; ++node)
            {
                //int ithread = omp_get_thread_num();

                long scapou = 0; //scapou = 0;
                long capout = 0;
                long maxcap = 0; //maxcap = 0;
                long scapin = 0; //scapin = 0;
                long capin = 0;
                long node_def__ = arrayit_1.dfct[node - 1];


                blks2_1.fpushf[node - 1] = node_def__;
                blks2_1.fpushb[node - 1] = -node_def__;

                arc = blks_1.fou[node - 1];
                while (arc > 0) // L11:
                {
                    if (scapou <= input_1.large - arrayit_1.u[arc - 1])
                    {
                        scapou += arrayit_1.u[arc - 1];
                    }
                    else
                    {
                        goto L10; //continue to next node.
                    }
                    arc = blks_1.nxtou[arc - 1]; //goto L11;
                }

                if (scapou <= input_1.large - node_def__)
                {
                    capout = scapou + node_def__;
                }
                else
                {
                    goto L10; //continue to next node.
                }

                if (capout < 0)
                {
                    /* PROBLEM IS INFEASIBLE - EXIT */
                    /* PRINT*,'EXIT DURING CAPACITY ADJUSTMENT' */
                    /* PRINT*,'EXOGENOUS FLOW INTO NODE',NODE, */
                    /* $    ' EXCEEDS OUT CAPACITY' */
                    /* CALL PRINTFLOWS(NODE) */
                    //goto L4400;
                    if (!silent) mi.FireOnError(string.Concat("EXOGENOUS FLOW INTO NODE ", node.ToString(), "\nEXIT DURING CAPACITY ADJUSTMENT.\nPROBLEM IS FOUND TO BE INFEASIBLE."));
                    return 1;
                }

                arc = blks_1.fin[node - 1];
                while (arc > 0) //L12:
                {
                    /* Computing MIN */
                    //arrayit_1.u[arc - 1] = min(arrayit_1.u[arc - 1],capout);
                    if (maxcap < arrayit_1.u[arc - 1])
                    {
                        maxcap = arrayit_1.u[arc - 1];
                    }
                    if (scapin <= input_1.large - arrayit_1.u[arc - 1])
                    {
                        scapin += arrayit_1.u[arc - 1];
                    }
                    else
                    {
                        goto L10; //continue to next node.
                    }
                    arc = blks_1.nxtin[arc - 1]; //goto L12;
                }

                if (scapin <= input_1.large + node_def__)
                {
                    capin = scapin - node_def__;
                }
                else
                {
                    goto L10; //continue to next node.
                }

                if (capin < 0)
                {
                    if (!silent) mi.FireOnError(string.Concat("EXOGENOUS FLOW INTO NODE ", node.ToString(), "\nEXIT DURING CAPACITY ADJUSTMENT.\nPROBLEM IS FOUND TO BE INFEASIBLE."));
                    return 1;
                }

                arc = blks_1.fou[node - 1];
                while (arc > 0) //L15:
                {
                    /* Computing MIN */
                    //arrayit_1.u[arc - 1] = min(arrayit_1.u[arc - 1],capin);
                    arc = blks_1.nxtou[arc - 1]; //goto L15;
                }
            L10:
                ;
            }

            /* --------------------------------------------------------------- */

            /*  INITIALIZATION PHASE II */

            /*     IN THIS PHASE, WE INITIALIZE THE PRICES AND FLOWS BY EITHER CALLING */
            /*     THE ROUTINE AUCTION OR BY PERFORMING ONLY SINGLE NODE (COORDINATE) */
            /*     RELAXATION ITERATIONS. */
            /* --------------------------------------------------------------- */

            if (input_1.crash == 1)
            {
                output_1.nsp = 0;
                GlobalMembersRelax4.auction_();
                GlobalMembersRelax4.inidat_();
                if (input_1.crash == 1)
                {
                    // Skip some initialization if input_1.crash is 1 still.
                    goto L70;
                }
            }

            /*     INITIALIZE THE ARC FLOWS TO SATISFY COMPLEMENTARY SLACKNESS WITH THE */
            /*     PRICES.  U(ARC) IS THE RESIDUAL CAPACITY OF ARC, AND X(ARC) IS THE FLOW. */
            /*     THESE TWO ALWAYS ADD UP TO THE TOTAL CAPACITY FOR ARC. */
            /*     ALSO COMPUTE THE DIRECTIONAL DERIVATIVES FOR EACH COORDINATE */
            /*     AND COMPUTE THE ACTUAL DEFICITS. */

            //#pragma omp parallel for private(t, t1, t2) num_threads(NOMP)
            for (arc = 1; arc <= input_1.na; ++arc)
            {
                //int ithread = omp_get_thread_num();

                arrayit_1.x[arc - 1] = 0;
                if (arrayit_1.rc[arc - 1] <= 0)
                {
                    // ARC is active or balanced.
                    t = arrayit_1.u[arc - 1];
                    t1 = arrayit_1.startn[arc - 1];
                    t2 = arrayit_1.endn[arc - 1];

                    //#pragma omp atomic
                    blks2_1.fpushf[t1 - 1] += t;
                    //#pragma omp atomic
                    blks2_1.fpushb[t2 - 1] += t;

                    if (arrayit_1.rc[arc - 1] < 0)
                    {
                        // ARC is active.
                        arrayit_1.x[arc - 1] = t;
                        arrayit_1.u[arc - 1] = 0;

                        //#pragma omp atomic
                        arrayit_1.dfct[t1 - 1] += t;
                        //#pragma omp atomic
                        arrayit_1.dfct[t2 - 1] -= t;

                        //#pragma omp atomic
                        blks2_1.fpushb[t1 - 1] -= t;
                        //#pragma omp atomic
                        blks2_1.fpushf[t2 - 1] -= t;
                    }
                }
            } // L20:

            #region
            /* NOTE by ST:
		This section of codes could be removed, and relax4 can still solve the problem.
		The solution is optimal as a result of primal objection is equal to dual objective.
		However, the solution could be sligtly different from the case where this section is excuted.
		Since this section has no detrimental effect on speeding time of problem solving,
		it is set as an active section still.
	
		Also, blks2_1.fpushb and blk2_1.fpushf in this seciton of code are used in different
		meanning from others. They contains the value of flow or deficit,
		instead of the index of arcs or links as seen in other sections.
	*/


            /*     MAKE 2 OR 3 PASSES THROUGH ALL NODES, PERFORMING ONLY */
            /*     SINGLE NODE RELAXATION ITERATIONS.  THE NUMBER OF */
            /*     PASSES DEPENDS ON THE DENSITY OF THE NETWORK */

            if (input_1.na > input_1.n * 10)
            {
                numpasses = 2;
            }
            else
            {
                numpasses = 3;
            }

            for (passes = 1; passes <= numpasses; ++passes)
            {
                for (node = 1; node <= input_1.n; ++node)
                {
                    if (arrayit_1.dfct[node - 1] == 0)
                    {
                        continue; //goto L40; go to next 'node'
                    }

                    if (blks2_1.fpushf[node - 1] <= 0)
                    {
                        /*     COMPUTE DELPRC, THE STEPSIZE TO THE NEXT BREAKPOINT */
                        /*     IN THE DUAL COST AS THE PRICE OF NODE IS INCREASED. */
                        /*     [SINCE THE REDUCED COST OF ALL OUTGOING (RESP., */
                        /*     INCOMING) ARCS WILL DECREASE (RESP., INCREASE) AS */
                        /*     THE PRICE OF NODE IS INCREASED, THE NEXT BREAKPOINT IS */
                        /*     THE MINIMUM OF THE POSITIVE REDUCED COST ON OUTGOING */
                        /*     ARCS AND OF THE NEGATIVE REDUCED COST ON INCOMING ARCS.] */

                        delprc = input_1.large;

                        arc = blks_1.fou[node - 1]; //L51:
                        while (arc > 0)
                        {
                            trc = arrayit_1.rc[arc - 1];
                            if (trc > 0) // && trc < delprc) {
                            {
                                delprc = ((delprc) < (trc)) ? (delprc) : (trc);
                            }
                            arc = blks_1.nxtou[arc - 1]; //goto L51;
                        }

                        arc = blks_1.fin[node - 1]; //L52:
                        while (arc > 0)
                        {
                            trc = arrayit_1.rc[arc - 1];
                            if (trc < 0) // && trc > -delprc) {
                            {
                                delprc = ((delprc) < (-trc)) ? (delprc) : (-trc);
                            }
                            arc = blks_1.nxtin[arc - 1]; //goto L52;
                        }

                        /* IF NO BREAKPOINT IS LEFT AND DUAL ASCENT IS STILL */
                        /* POSSIBLE, THE PROBLEM IS INFEASIBLE. */

                        if (delprc >= input_1.large)
                        {
                            if (blks2_1.fpushf[node - 1] == 0)
                            {
                                continue; //goto L40; go to next 'node'
                            }
                            //goto L4400;
                            if (!silent) mi.FireOnError("NO BREAKPOINT IS LEFT AND DUAL ASCENT IS STILL POSSIBLE.\nEXIT DURING PERFORMING SINGLE NODE RELAXATION ITERATIONS.\nPROBLEM IS FOUND TO BE INFEASIBLE.");
                            return 1;
                        }

                        /*	DELPRC IS THE STEPSIZE TO NEXT BREAKPOINT.  INCREASE */
                        /*	PRICE OF NODE BY DELPRC AND COMPUTE THE STEPSIZE TO */
                        /*	THE NEXT BREAKPOINT IN THE DUAL COST. */
                        do //L53:
                        {
                            fg_doloop = false;

                            nxtbrk = input_1.large;

                            /*	LOOK AT ALL ARCS OUT OF NODE. */

                            arc = blks_1.fou[node - 1]; //L54:
                            while (arc > 0)
                            {
                                trc = arrayit_1.rc[arc - 1];
                                if (trc == 0)
                                {
                                    t1 = arrayit_1.endn[arc - 1];
                                    t = arrayit_1.u[arc - 1];
                                    if (t > 0)
                                    {
                                        arrayit_1.dfct[node - 1] += t;
                                        arrayit_1.dfct[t1 - 1] -= t;
                                        arrayit_1.x[arc - 1] = t;
                                        arrayit_1.u[arc - 1] = 0;
                                    }
                                    else
                                    {
                                        t = arrayit_1.x[arc - 1];
                                    }
                                    blks2_1.fpushb[node - 1] -= t;
                                    blks2_1.fpushf[t1 - 1] -= t;
                                }

                                /*	DECREASE THE REDUCED COST ON ALL OUTGOING ARCS. */

                                trc -= delprc;
                                if (trc > 0) // && trc < nxtbrk) {
                                {
                                    nxtbrk = ((nxtbrk) < (trc)) ? (nxtbrk) : (trc);
                                }
                                else if (trc == 0)
                                {

                                    /*	ARC GOES FROM INACTIVE TO BALANCED.  UPDATE THE */
                                    /*	RATE OF DUAL ASCENT AT NODE AND AT ITS NEIGHBOR. */

                                    blks2_1.fpushf[node - 1] += arrayit_1.u[arc - 1];
                                    blks2_1.fpushb[arrayit_1.endn[arc - 1] - 1] += arrayit_1.u[arc - 1];
                                }
                                arrayit_1.rc[arc - 1] = trc;
                                arc = blks_1.nxtou[arc - 1]; //goto L54;
                            }

                            /*	LOOK AT ALL ARCS INTO NODE. */

                            arc = blks_1.fin[node - 1]; //L55:
                            while (arc > 0)
                            {
                                trc = arrayit_1.rc[arc - 1];
                                if (trc == 0)
                                {
                                    t1 = arrayit_1.startn[arc - 1];
                                    t = arrayit_1.x[arc - 1];
                                    if (t > 0)
                                    {
                                        arrayit_1.dfct[node - 1] += t;
                                        arrayit_1.dfct[t1 - 1] -= t;
                                        arrayit_1.u[arc - 1] = t;
                                        arrayit_1.x[arc - 1] = 0;
                                    }
                                    else
                                    {
                                        t = arrayit_1.u[arc - 1];
                                    }
                                    blks2_1.fpushf[t1 - 1] -= t;
                                    blks2_1.fpushb[node - 1] -= t;
                                }

                                /*	INCREASE THE REDUCED COST ON ALL INCOMING ARCS. */

                                trc += delprc;
                                if (trc < 0) // && trc > -nxtbrk) {
                                {
                                    nxtbrk = ((nxtbrk) < (-trc)) ? (nxtbrk) : (-trc);
                                }
                                else if (trc == 0)
                                {

                                    /*	ARC GOES FROM ACTIVE TO BALANCED.  UPDATE THE */
                                    /*	RATE OF DUAL ASCENT AT NODE AND AT ITS NEIGHBOR. */

                                    blks2_1.fpushb[arrayit_1.startn[arc - 1] - 1] += arrayit_1.x[arc - 1];
                                    blks2_1.fpushf[node - 1] += arrayit_1.x[arc - 1];
                                }
                                arrayit_1.rc[arc - 1] = trc;
                                arc = blks_1.nxtin[arc - 1]; //goto L55;

                            }

                            /*	IF PRICE OF NODE CAN BE INCREASED FURTHER WITHOUT DECREASING */
                            /*	THE DUAL COST (EVEN IF THE DUAL COST DOESN'T INCREASE), */
                            /*	RETURN TO INCREASE THE PRICE FURTHER. */

                            if (blks2_1.fpushf[node - 1] <= 0 && nxtbrk < input_1.large)
                            {
                                delprc = nxtbrk;
                                fg_doloop = true; //goto L53;
                            }
                        } while (fg_doloop);

                    }
                    else if (blks2_1.fpushb[node - 1] <= 0)
                    {
                        /*     COMPUTE DELPRC, THE STEPSIZE TO THE NEXT BREAKPOINT */
                        /*     IN THE DUAL COST AS THE PRICE OF NODE IS DECREASED. */
                        /*     [SINCE THE REDUCED COST OF ALL OUTGOING (RESP., */
                        /*     INCOMING) ARCS WILL INCREASE (RESP., DECREASE) AS */
                        /*     THE PRICE OF NODE IS DECREASED, THE NEXT BREAKPOINT IS */
                        /*     THE MINIMUM OF THE NEGATIVE REDUCED COST ON OUTGOING */
                        /*     ARCS AND OF THE POSITIVE REDUCED COST ON INCOMING ARCS.] */

                        delprc = input_1.large;

                        arc = blks_1.fou[node - 1]; //L61:
                        while (arc > 0)
                        {
                            trc = arrayit_1.rc[arc - 1];
                            if (trc < 0) // && trc > -delprc) {
                            {
                                delprc = ((delprc) < (-trc)) ? (delprc) : (-trc);
                            }
                            arc = blks_1.nxtou[arc - 1]; //goto L61;
                        }

                        arc = blks_1.fin[node - 1]; //L62:
                        while (arc > 0)
                        {
                            trc = arrayit_1.rc[arc - 1];
                            if (trc > 0) // && trc < delprc) {
                            {
                                delprc = ((delprc) < (trc)) ? (delprc) : (trc);
                            }
                            arc = blks_1.nxtin[arc - 1]; //goto L62;
                        }

                        /*	IF NO BREAKPOINT IS LEFT AND DUAL ASCENT IS STILL */
                        /*	POSSIBLE, THE PROBLEM IS INFEASIBLE. */

                        if (delprc == input_1.large)
                        {
                            if (blks2_1.fpushb[node - 1] == 0)
                            {
                                continue; //goto L40; //go to next 'node'
                            }
                            //goto L4400;
                            if (!silent) mi.FireOnError("NO BREAKPOINT IS LEFT AND DUAL ASCENT IS STILL POSSIBLE.\nEXIT DURING PERFORMING SINGLE NODE RELAXATION ITERATIONS.\nPROBLEM IS FOUND TO BE INFEASIBLE.");
                            return 1;
                        }

                        /*	DELPRC IS THE STEPSIZE TO NEXT BREAKPOINT.  DECREASE */
                        /*	PRICE OF NODE BY DELPRC AND COMPUTE THE STEPSIZE TO */
                        /*	THE NEXT BREAKPOINT IN THE DUAL COST. */
                        do //L63:
                        {
                            fg_doloop = false;

                            nxtbrk = input_1.large;

                            /*	LOOK AT ALL ARCS OUT OF NODE. */

                            arc = blks_1.fou[node - 1]; //L64:
                            while (arc > 0)
                            {
                                trc = arrayit_1.rc[arc - 1];
                                if (trc == 0)
                                {
                                    t1 = arrayit_1.endn[arc - 1];
                                    t = arrayit_1.x[arc - 1];
                                    if (t > 0)
                                    {
                                        arrayit_1.dfct[node - 1] -= t;
                                        arrayit_1.dfct[t1 - 1] += t;
                                        arrayit_1.u[arc - 1] = t;
                                        arrayit_1.x[arc - 1] = 0;
                                    }
                                    else
                                    {
                                        t = arrayit_1.u[arc - 1];
                                    }
                                    blks2_1.fpushf[node - 1] -= t;
                                    blks2_1.fpushb[t1 - 1] -= t;
                                }

                                /*	INCREASE THE REDUCED COST ON ALL OUTGOING ARCS. */

                                trc += delprc;
                                if (trc < 0) // && trc > -nxtbrk) {
                                {
                                    nxtbrk = ((nxtbrk) < (-trc)) ? (nxtbrk) : (-trc);
                                }
                                else if (trc == 0)
                                {

                                    /*	ARC GOES FROM ACTIVE TO BALANCED.  UPDATE THE */
                                    /*	RATE OF DUAL ASCENT AT NODE AND AT ITS NEIGHBOR. */

                                    blks2_1.fpushb[node - 1] += arrayit_1.x[arc - 1];
                                    blks2_1.fpushf[arrayit_1.endn[arc - 1] - 1] += arrayit_1.x[arc - 1];
                                }
                                arrayit_1.rc[arc - 1] = trc;
                                arc = blks_1.nxtou[arc - 1]; //goto L64;
                            }

                            /*	LOOK AT ALL ARCS INTO NODE. */

                            arc = blks_1.fin[node - 1]; //L65:
                            while (arc > 0)
                            {
                                trc = arrayit_1.rc[arc - 1];
                                if (trc == 0)
                                {
                                    t1 = arrayit_1.startn[arc - 1];
                                    t = arrayit_1.u[arc - 1];
                                    if (t > 0)
                                    {
                                        arrayit_1.dfct[node - 1] -= t;
                                        arrayit_1.dfct[t1 - 1] += t;
                                        arrayit_1.x[arc - 1] = t;
                                        arrayit_1.u[arc - 1] = 0;
                                    }
                                    else
                                    {
                                        t = arrayit_1.x[arc - 1];
                                    }
                                    blks2_1.fpushb[t1 - 1] -= t;
                                    blks2_1.fpushf[node - 1] -= t;
                                }

                                /*	DECREASE THE REDUCED COST ON ALL INCOMING ARCS. */

                                trc -= delprc;
                                if (trc > 0) // && trc < nxtbrk) {
                                {
                                    nxtbrk = ((nxtbrk) < (trc)) ? (nxtbrk) : (trc);
                                }
                                else if (trc == 0)
                                {

                                    /*	ARC GOES FROM INACTIVE TO BALANCED.  UPDATE THE */
                                    /*	RATE OF DUAL ASCENT AT NODE AND AT ITS NEIGHBOR. */

                                    blks2_1.fpushf[arrayit_1.startn[arc - 1] - 1] += arrayit_1.u[arc - 1];
                                    blks2_1.fpushb[node - 1] += arrayit_1.u[arc - 1];
                                }
                                arrayit_1.rc[arc - 1] = trc;
                                arc = blks_1.nxtin[arc - 1]; //goto L65;
                            }

                            /*	IF PRICE OF NODE CAN BE DECREASED FURTHER WITHOUT DECREASING */
                            /*	THE DUAL COST (EVEN IF THE DUAL COST DOESN'T INCREASE), */
                            /*	RETURN TO DECREASE THE PRICE FURTHER. */

                            if (blks2_1.fpushb[node - 1] <= 0 && nxtbrk < input_1.large)
                            {
                                delprc = nxtbrk;
                                fg_doloop = true; //goto L63;
                            }
                        } while (fg_doloop);
                    }
                } // L40: ;  Next node for-loop.
            } // L30: Next passes for-loop
            #endregion

        /* --------------------------------------------------------------- */
        /*     INITIALIZE TREE DATA STRUCTURE. */
        /* --------------------------------------------------------------- */
        L70:
            //#pragma omp parallel num_threads(NOMP)
            {
                //#pragma omp for
                for (i__ = 1; i__ <= input_1.n; ++i__)
                {
                    blks2_1.fpushf[i__ - 1] = 0;
                    blks2_1.fpushb[i__ - 1] = 0;
                } // L80:

                //#pragma omp master

                {
                    /*	Do NOT make this block parallel.
                         Topology of the network could be messed. */
                    for (i__ = 1; i__ <= input_1.na; ++i__)
                    {
                        blks2_1.nxtpushb[i__ - 1] = -1;
                        blks2_1.nxtpushf[i__ - 1] = -1;

                        if (arrayit_1.rc[i__ - 1] == 0)
                        {
                            // If BALANCED link

                            blks2_1.nxtpushf[i__ - 1] = blks2_1.fpushf[arrayit_1.startn[i__ - 1] - 1];
                            blks2_1.nxtpushb[i__ - 1] = blks2_1.fpushb[arrayit_1.endn[i__ - 1] - 1];

                            //#pragma omp critical (fpush)
                            {
                                blks2_1.fpushf[arrayit_1.startn[i__ - 1] - 1] = i__;
                                blks2_1.fpushb[arrayit_1.endn[i__ - 1] - 1] = i__;
                            }
                        }
                    } // L81:
                }
            }

            /* --------------------------------------------------------------- */
        /*     INITIALIZE OTHER VARIABLES. */
        /* --------------------------------------------------------------- */
        L90:
            blks_1.feasbl = true;
            output_1.iter = 0;
            output_1.nmultinode = 0;
            output_1.num_augm__ = 0;
            output_1.num_ascnt__ = 0;
            num_passes__ = 0;
            numnz = input_1.n;
            numnz_new__ = 0;
            switch__ = false;

            for (i__ = 1; i__ <= input_1.n; ++i__)
            {
                blks2_1.path_id__[i__ - 1] = false;
                blks2_1.scan[i__ - 1] = false;
            } // L91:

            nlabel = 0;

            /*     RELAX4 USES AN ADAPTIVE STRATEGY TO DECIDE WHETHER TO */
            /*     CONTINUE THE SCANNING PROCESS AFTER A MULTINODE PRICE CHANGE. */
            /*     THE THRESHOLD PARAMETER TP AND TS THAT CONTROL */
            /*     THIS STRATEGY ARE SET IN THE NEXT TWO LINES. */

            tp = 10;
            ts = input_1.n / 15;

            /*     INITIALIZE THE QUEUE OF NODES WITH NONZERO DEFICIT */

            for (node = 1; node <= input_1.n - 1; ++node)
            {
                blks3_1.nxtqueue[node - 1] = node + 1;
            } // L92:

            blks3_1.nxtqueue[input_1.n - 1] = 1;

            node = input_1.n;
            lastqueue = input_1.n;

            /*    READ TIME FOR INITIALIZATION */
            output_1.time1 = (double)DateTime.Now.Subtract(output_1.time0).TotalMilliseconds;


            /* --------------------------------------------------------------- */
        /*     START THE RELAXATION ALGORITHM. */
        /* --------------------------------------------------------------- */
        L100:

            /* CODE FOR ADVANCING THE QUEUE OF NONZERO DEFICIT NODES */

            prevnode = node;

            node = blks3_1.nxtqueue[node - 1];

            defcit = arrayit_1.dfct[node - 1];

            if (node == lastqueue)
            {
                numnz = numnz_new__;
                numnz_new__ = 0;
                lastqueue = prevnode;
                ++num_passes__;
            }

            /* CODE FOR DELETING A NODE FROM THE QUEUE */

            if (defcit == 0)
            {
                nxtnode = blks3_1.nxtqueue[node - 1];
                if (node == nxtnode)
                {
                    return 0;
                }
                else
                {
                    blks3_1.nxtqueue[prevnode - 1] = nxtnode;
                    blks3_1.nxtqueue[node - 1] = 0;
                    node = nxtnode;
                    goto L100;
                }
            }
            else
            {
                posit = defcit > 0;
            }

            ++output_1.iter;
            ++numnz_new__;

            if (posit)
            {
                /*		ATTEMPT A SINGLE NODE ITERATION FROM NODE WITH POSITIVE DEFICIT */

                pchange = false;
                indef = defcit;
                delx = 0;
                nb = 0;


                //#pragma omp parallel private(arc) num_threads(NOMP) //reduction(+: delx)
                {
                    //#pragma omp single nowait
                    {
                        /* CHECK OUTGOING (PROBABLY) BALANCED ARCS FROM NODE. */
                        arc = blks2_1.fpushf[node - 1];
                        while (arc > 0) //L4500:
                        {
                            if (arrayit_1.rc[arc - 1] == 0 && arrayit_1.x[arc - 1] > 0)
                            {
                                //#pragma omp atomic
                                delx += arrayit_1.x[arc - 1];

                                //#pragma omp critical(nb)
                                {
                                    ++nb;
                                    blks_1.save[nb - 1] = arc;
                                }
                            }
                            arc = blks2_1.nxtpushf[arc - 1];
                        } //goto L4500;
                    }

                    //#pragma omp single
                    {
                        /* CHECK INCOMING ARCS. */
                        arc = blks2_1.fpushb[node - 1];
                        while (arc > 0) //L4501:
                        {
                            if (arrayit_1.rc[arc - 1] == 0 && arrayit_1.u[arc - 1] > 0)
                            {
                                //#pragma omp atomic
                                delx += arrayit_1.u[arc - 1];
                                //#pragma omp critical(nb)
                                {
                                    ++nb;
                                    blks_1.save[nb - 1] = -arc;
                                }
                            }
                            arc = blks2_1.nxtpushb[arc - 1];
                        } //goto L4501;
                    }
                }

                /*     END OF INITIAL NODE SCAN. */

                do //L4018:
                {
                    fg_aug = true;
                    fg_doloop = false;

                    if (delx > defcit)
                    {
                        /*	IF NO PRICE CHANGE IS POSSIBLE, EXIT. */
                        quit = defcit < indef;
                        fg_aug = true;
                        break; //goto L4016; exit do loop, and then do flow augmentation.
                    }

                    /*  RELAX4 SEARCHES ALONG THE ASCENT DIRECTION FOR THE */
                    /*  BEST PRICE BY CHECKING THE SLOPE OF THE DUAL COST */
                    /*  AT SUCCESSIVE BREAK POINTS.  FIRST, WE */
                    /*  COMPUTE THE DISTANCE TO THE NEXT BREAK POINT. */

                    delprc = input_1.large;

                    //#pragma omp parallel private(arc, rdcost) num_threads(NOMP)
                    {
                        //#pragma omp single nowait
                        {
                            arc = blks_1.fou[node - 1];
                            while (arc > 0) //L4502:
                            {
                                rdcost = arrayit_1.rc[arc - 1];
                                //#pragma omp critical (delprc)
                                {
                                    if (rdcost < 0) // && rdcost > -delprc) {
                                    {
                                        delprc = ((delprc) < (-rdcost)) ? (delprc) : (-rdcost);
                                    }
                                }
                                arc = blks_1.nxtou[arc - 1];
                            } //goto L4502;
                        }

                        //#pragma omp single
                        {
                            arc = blks_1.fin[node - 1];
                            while (arc > 0) //L4503:
                            {
                                rdcost = arrayit_1.rc[arc - 1];
                                //#pragma omp critical (delprc)
                                {
                                    if (rdcost > 0) // && rdcost < delprc) {
                                    {
                                        delprc = ((delprc) < (rdcost)) ? (delprc) : (rdcost);
                                    }
                                }
                                arc = blks_1.nxtin[arc - 1];
                            } //goto L4503;
                        }
                    }

                    /*  CHECK IF PROBLEM IS INFEASIBLE. */

                    if (delx < defcit && delprc == input_1.large)
                    {
                        /* THE DUAL COST CAN BE DECREASED WITHOUT BOUND. */
                        //goto L4400;
                        if (!silent) mi.FireOnError(string.Concat("THE DUAL COST CAN BE DECREASED WITHOUT BOUND.\nEXIT DURING A SINGLE NODE ITERATION AT NODE ", node.ToString(), "\nPROBLEM IS FOUND TO BE INFEASIBLE."));
                        return 1;
                    }


                    /* SKIP FLOW ADJUSTEMT IF THERE IS NO FLOW TO MODIFY. */

                    if (delx == 0)
                    {
                        //goto L4014;
                        if (delprc == input_1.large) //L4014:
                        {
                            quit = true;
                            fg_aug = false;
                            break; //goto L4019; exit do loop, and NOT doing flow augmentation.
                        }
                    }
                    else
                    {
                        defcit -= delx;

                        /* ADJUST THE FLOW ON THE BALANCED ARCS INCIDENT TO NODE TO */
                        /* MAINTAIN COMPLEMENTARY SLACKNESS AFTER THE PRICE CHANGE. */

                        //#pragma omp parallel for private(arc, narc, node2, t1) num_threads(NOMP)
                        for (j = 1; j <= nb; ++j)
                        {
                            arc = blks_1.save[j - 1];
                            if (arc > 0)
                            {
                                // OUTGOING ARC from 'node'.
                                node2 = arrayit_1.endn[arc - 1];
                                t1 = arrayit_1.x[arc - 1];

                                //#pragma omp atomic
                                arrayit_1.dfct[node2 - 1] += t1;

                                //#pragma omp critical(prevnode)
                                {
                                    if (blks3_1.nxtqueue[node2 - 1] == 0)
                                    {
                                        blks3_1.nxtqueue[prevnode - 1] = node2;
                                        blks3_1.nxtqueue[node2 - 1] = node;
                                        prevnode = node2;
                                    }
                                }

                                arrayit_1.u[arc - 1] += t1;
                                arrayit_1.x[arc - 1] = 0;
                            }
                            else
                            {
                                // INCOMING ARC to 'node'.
                                narc = -arc;
                                node2 = arrayit_1.startn[narc - 1];
                                t1 = arrayit_1.u[narc - 1];

                                //#pragma omp atomic
                                arrayit_1.dfct[node2 - 1] += t1;

                                //#pragma omp critical(prevnode)
                                {
                                    if (blks3_1.nxtqueue[node2 - 1] == 0)
                                    {
                                        blks3_1.nxtqueue[prevnode - 1] = node2;
                                        blks3_1.nxtqueue[node2 - 1] = node;
                                        prevnode = node2;
                                    }
                                    arrayit_1.x[narc - 1] += t1;
                                    arrayit_1.u[narc - 1] = 0;
                                }
                            }
                        } // L4013:

                        if (delprc == input_1.large) //L4014:
                        {
                            quit = true;
                            fg_aug = false;
                            break; //goto L4019; exit do loop, and NOT doing flow augmentation.
                        }
                    }

                    /* NODE CORRESPONDS TO A DUAL ASCENT DIRECTION.  DECREASE */
                    /* THE PRICE OF NODE BY DELPRC AND COMPUTE THE STEPSIZE TO THE */
                    /* NEXT BREAKPOINT IN THE DUAL COST. */

                    nb = 0;
                    pchange = true;
                    dp = delprc;
                    delprc = input_1.large;
                    delx = 0;

                    //#pragma omp parallel private(arc, rdcost) num_threads(NOMP)
                    {
                        //#pragma omp single nowait
                        {
                            arc = blks_1.fou[node - 1];
                            while (arc > 0) //L4504:
                            {
                                rdcost = arrayit_1.rc[arc - 1] + dp;
                                arrayit_1.rc[arc - 1] = rdcost;

                                //#pragma omp critical(nb)
                                {
                                    if (rdcost == 0)
                                    {
                                        ++nb;
                                        blks_1.save[nb - 1] = arc;
                                        delx += arrayit_1.x[arc - 1];
                                    } // && rdcost > -delprc) {
                                    else if (rdcost < 0)
                                    {
                                        delprc = ((delprc) < (-rdcost)) ? (delprc) : (-rdcost);
                                    }
                                }

                                arc = blks_1.nxtou[arc - 1];
                            } //goto L4504;
                        }

                        //#pragma omp single
                        {
                            arc = blks_1.fin[node - 1];
                            while (arc > 0) //L4505:
                            {
                                rdcost = arrayit_1.rc[arc - 1] - dp;
                                arrayit_1.rc[arc - 1] = rdcost;

                                //#pragma omp critical(nb)
                                {
                                    if (rdcost == 0)
                                    {
                                        ++nb;
                                        blks_1.save[nb - 1] = -arc;
                                        delx += arrayit_1.u[arc - 1];
                                    } // && rdcost < delprc) {
                                    else if (rdcost > 0)
                                    {
                                        delprc = ((delprc) < (rdcost)) ? (delprc) : (rdcost);
                                    }
                                }

                                arc = blks_1.nxtin[arc - 1];
                            } //goto L4505;
                        }
                    }

                    if (delx > defcit)
                    {
                        /*	IF NO PRICE CHANGE IS POSSIBLE, EXIT. */
                        quit = defcit < indef;
                        fg_aug = true;
                        fg_doloop = false;
                    }
                    else
                    {
                        /* RETURN TO CHECK IF ANOTHER PRICE CHANGE IS POSSIBLE. */
                        fg_doloop = true; //goto L4018;
                    }
                } while (fg_doloop);


                if (fg_aug) //L4016:
                {

                    /* PERFORM FLOW AUGMENTATION AT NODE. */
                    for (j = 1; j <= nb; ++j)
                    {
                        arc = blks_1.save[j - 1];
                        if (arc > 0)
                        {

                            /* ARC IS AN OUTGOING ARC FROM NODE. */

                            node2 = arrayit_1.endn[arc - 1];
                            t1 = arrayit_1.dfct[node2 - 1];

                            if (t1 < 0)
                            {

                                /* DECREASE THE TOTAL DEFICIT BY DECREASING FLOW OF ARC. */

                                quit = true;
                                t2 = arrayit_1.x[arc - 1];

                                /* Computing MIN */
                                i__2 = ((defcit) < (-t1)) ? (defcit) : (-t1);
                                dx = ((i__2) < (t2)) ? (i__2) : (t2);
                                defcit -= dx;

                                arrayit_1.dfct[node2 - 1] = t1 + dx;

                                if (blks3_1.nxtqueue[node2 - 1] == 0)
                                {
                                    blks3_1.nxtqueue[prevnode - 1] = node2;
                                    blks3_1.nxtqueue[node2 - 1] = node;
                                    prevnode = node2;
                                }

                                arrayit_1.x[arc - 1] = t2 - dx;
                                arrayit_1.u[arc - 1] += dx;

                                if (defcit == 0)
                                {
                                    break; //goto L4019;
                                }
                            }
                        }
                        else
                        {

                            /* -ARC IS AN INCOMING ARC TO NODE. */

                            narc = -arc;
                            node2 = arrayit_1.startn[narc - 1];
                            t1 = arrayit_1.dfct[node2 - 1];

                            if (t1 < 0)
                            {

                                /* DECREASE THE TOTAL DEFICIT BY INCREASING FLOW OF -ARC. */

                                quit = true;
                                t2 = arrayit_1.u[narc - 1];

                                /* Computing MIN */
                                i__2 = ((defcit) < (-t1)) ? (defcit) : (-t1);
                                dx = ((i__2) < (t2)) ? (i__2) : (t2);
                                defcit -= dx;

                                arrayit_1.dfct[node2 - 1] = t1 + dx;

                                if (blks3_1.nxtqueue[node2 - 1] == 0)
                                {
                                    blks3_1.nxtqueue[prevnode - 1] = node2;
                                    blks3_1.nxtqueue[node2 - 1] = node;
                                    prevnode = node2;
                                }

                                arrayit_1.x[narc - 1] += dx;
                                arrayit_1.u[narc - 1] = t2 - dx;

                                if (defcit == 0)
                                {
                                    break; //goto L4019;
                                }
                            }
                        }
                    } // L4011:

                    arrayit_1.dfct[node - 1] = defcit;

                } //L4019:
                else
                {
                    arrayit_1.dfct[node - 1] = defcit;
                }


                /* RECONSTRUCT THE LINKED LIST OF BALANCE ARCS INCIDENT TO THIS NODE. */
                /* FOR EACH ADJACENT NODE, WE ADD ANY NEWLY BLANCED ARCS */
                /* TO THE LIST, BUT DO NOT BOTHER REMOVING FORMERLY BALANCED ONES */
                /* (THEY WILL BE REMOVED THE NEXT TIME EACH ADJACENT NODE IS SCANNED). */

                if (pchange)
                {
                    //#pragma omp parallel private(arc, nxtarc) num_threads(NOMP)
                    {
                        //#pragma omp single nowait
                        {
                            arc = blks2_1.fpushf[node - 1];
                            blks2_1.fpushf[node - 1] = 0;
                            while (arc > 0) //L4506:
                            {
                                nxtarc = blks2_1.nxtpushf[arc - 1];
                                blks2_1.nxtpushf[arc - 1] = -1;
                                arc = nxtarc;
                            } //goto L4506;
                        }

                        //#pragma omp single
                        {
                            arc = blks2_1.fpushb[node - 1];
                            blks2_1.fpushb[node - 1] = 0;
                            while (arc > 0) //L4507:
                            {
                                nxtarc = blks2_1.nxtpushb[arc - 1];
                                blks2_1.nxtpushb[arc - 1] = -1;
                                arc = nxtarc;
                            } //goto L4507;
                        }

                        //#pragma omp barrier

                        /*  NOW ADD THE CURRENTLY BALANCED ARCS TO THE LIST FOR THIS NODE */
                        /*  (WHICH IS NOW EMPTY), AND THE APPROPRIATE ADJACENT ONES. */
                        //#pragma omp for
                        for (j = 1; j <= nb; ++j)
                        {
                            arc = blks_1.save[j - 1];
                            if (arc <= 0)
                            {
                                arc = -arc;
                            }
                            if (blks2_1.nxtpushf[arc - 1] < 0)
                            {
                                blks2_1.nxtpushf[arc - 1] = blks2_1.fpushf[arrayit_1.startn[arc - 1] - 1];
                                blks2_1.fpushf[arrayit_1.startn[arc - 1] - 1] = arc;
                            }
                            if (blks2_1.nxtpushb[arc - 1] < 0)
                            {
                                blks2_1.nxtpushb[arc - 1] = blks2_1.fpushb[arrayit_1.endn[arc - 1] - 1];
                                blks2_1.fpushb[arrayit_1.endn[arc - 1] - 1] = arc;
                            }
                        } // L4508:
                    }
                }
                /*     END OF SINGLE NODE ITERATION FOR POSITIVE DEFICIT NODE. */

            }
            else
            {

                /*     ATTEMPT A SINGLE NODE ITERATION FROM NODE WITH NEGATIVE DEFICIT */

                pchange = false;
                defcit = -defcit;
                indef = defcit;
                delx = 0;
                nb = 0;

                arc = blks2_1.fpushb[node - 1];
                while (arc > 0) //L4509:
                {
                    if (arrayit_1.rc[arc - 1] == 0 && arrayit_1.x[arc - 1] > 0)
                    {
                        delx += arrayit_1.x[arc - 1];
                        ++nb;
                        blks_1.save[nb - 1] = arc;
                    }
                    arc = blks2_1.nxtpushb[arc - 1];
                } //goto L4509;

                arc = blks2_1.fpushf[node - 1];
                while (arc > 0) //L4510:
                {
                    if (arrayit_1.rc[arc - 1] == 0 && arrayit_1.u[arc - 1] > 0)
                    {
                        delx += arrayit_1.u[arc - 1];
                        ++nb;
                        blks_1.save[nb - 1] = -arc;
                    }
                    arc = blks2_1.nxtpushf[arc - 1];
                } //goto L4510;

                do //L4028:
                {
                    fg_aug = true;
                    fg_doloop = false;

                    if (delx >= defcit)
                    {
                        quit = defcit < indef;
                        fg_aug = true;
                        break; //goto L4026;
                    }

                    /*  COMPUTE DISTANCE TO NEXT BREAKPOINT. */

                    delprc = input_1.large;
                    arc = blks_1.fin[node - 1];
                    while (arc > 0) //L4511:
                    {
                        rdcost = arrayit_1.rc[arc - 1];
                        if (rdcost < 0) // && rdcost > -delprc) {
                        {
                            delprc = ((delprc) < (-rdcost)) ? (delprc) : (-rdcost);
                        }
                        arc = blks_1.nxtin[arc - 1];
                    } //goto L4511;

                    arc = blks_1.fou[node - 1];
                    while (arc > 0) //L4512:
                    {
                        rdcost = arrayit_1.rc[arc - 1];
                        if (rdcost > 0) // && rdcost < delprc) {
                        {
                            delprc = ((delprc) < (rdcost)) ? (delprc) : (rdcost);
                        }
                        arc = blks_1.nxtou[arc - 1];
                    } //goto L4512;


                    /*  CHECK IF PROBLEM IS INFEASIBLE. */
                    if (delx < defcit && delprc == input_1.large)
                    {
                        //goto L4400;
                        if (!silent) mi.FireOnError(string.Concat("THE DUAL COST CAN BE DECREASED WITHOUT BOUND.\nEXIT DURING A SINGLE NODE ITERATION AT NODE ", node.ToString(), "\nPROBLEM IS FOUND TO BE INFEASIBLE."));
                        return 1;
                    }

                    /* SKIP FLOW ADJUSTEMT IF THERE IS NO FLOW TO MODIFY. */

                    if (delx == 0)
                    {
                        //goto L4024;
                        if (delprc == input_1.large) //L4024:
                        {
                            quit = true;
                            fg_aug = false;
                            break; //goto L4029;
                        }
                    }
                    else
                    {
                        defcit -= delx;

                        /*  FLOW AUGMENTATION IS POSSIBLE. */

                        for (j = 1; j <= nb; ++j)
                        {
                            arc = blks_1.save[j - 1];
                            if (arc > 0)
                            {
                                node2 = arrayit_1.startn[arc - 1];
                                t1 = arrayit_1.x[arc - 1];
                                arrayit_1.dfct[node2 - 1] -= t1;
                                if (blks3_1.nxtqueue[node2 - 1] == 0)
                                {
                                    blks3_1.nxtqueue[prevnode - 1] = node2;
                                    blks3_1.nxtqueue[node2 - 1] = node;
                                    prevnode = node2;
                                }
                                arrayit_1.u[arc - 1] += t1;
                                arrayit_1.x[arc - 1] = 0;
                            }
                            else
                            {
                                narc = -arc;
                                node2 = arrayit_1.endn[narc - 1];
                                t1 = arrayit_1.u[narc - 1];
                                arrayit_1.dfct[node2 - 1] -= t1;
                                if (blks3_1.nxtqueue[node2 - 1] == 0)
                                {
                                    blks3_1.nxtqueue[prevnode - 1] = node2;
                                    blks3_1.nxtqueue[node2 - 1] = node;
                                    prevnode = node2;
                                }
                                arrayit_1.x[narc - 1] += t1;
                                arrayit_1.u[narc - 1] = 0;
                            }
                        } // L4023:

                        if (delprc == input_1.large) //L4024:
                        {
                            quit = true;
                            fg_aug = false;
                            break; //goto L4029;
                        }
                    }

                    /*  PRICE INCREASE AT NODE IS POSSIBLE. */

                    nb = 0;
                    pchange = true;
                    dp = delprc;
                    delprc = input_1.large;
                    delx = 0;

                    arc = blks_1.fin[node - 1];
                    while (arc > 0) //L4513:
                    {
                        rdcost = arrayit_1.rc[arc - 1] + dp;
                        arrayit_1.rc[arc - 1] = rdcost;
                        if (rdcost == 0)
                        {
                            ++nb;
                            blks_1.save[nb - 1] = arc;
                            delx += arrayit_1.x[arc - 1];
                        } // && rdcost > -delprc) {
                        else if (rdcost < 0)
                        {
                            delprc = ((delprc) < (-rdcost)) ? (delprc) : (-rdcost);
                        }
                        arc = blks_1.nxtin[arc - 1];
                    } //goto L4513;

                    arc = blks_1.fou[node - 1];
                    while (arc > 0) //L4514:
                    {
                        rdcost = arrayit_1.rc[arc - 1] - dp;
                        arrayit_1.rc[arc - 1] = rdcost;
                        if (rdcost == 0)
                        {
                            ++nb;
                            blks_1.save[nb - 1] = -arc;
                            delx += arrayit_1.u[arc - 1];
                        } // && rdcost < delprc) {
                        else if (rdcost > 0)
                        {
                            delprc = ((delprc) < (rdcost)) ? (delprc) : (rdcost);
                        }
                        arc = blks_1.nxtou[arc - 1];
                    } //goto L4514;

                    if (delx >= defcit)
                    {
                        quit = defcit < indef;
                        fg_aug = true;
                        fg_doloop = false; //goto L4026;
                    }
                    else
                    {
                        fg_doloop = true; //goto L4028;
                    }
                } while (fg_doloop);


                if (fg_aug) //L4026:
                {

                    /*  PERFORM FLOW AUGMENTATION AT NODE. */

                    for (j = 1; j <= nb; ++j)
                    {
                        arc = blks_1.save[j - 1];
                        if (arc > 0)
                        {

                            /*  ARC IS AN INCOMING ARC TO NODE. */

                            node2 = arrayit_1.startn[arc - 1];
                            t1 = arrayit_1.dfct[node2 - 1];

                            if (t1 > 0)
                            {
                                quit = true;
                                t2 = arrayit_1.x[arc - 1];

                                /* Computing MIN */
                                i__2 = ((defcit) < (t1)) ? (defcit) : (t1);
                                dx = ((i__2) < (t2)) ? (i__2) : (t2);
                                defcit -= dx;
                                arrayit_1.dfct[node2 - 1] = t1 - dx;

                                if (blks3_1.nxtqueue[node2 - 1] == 0)
                                {
                                    blks3_1.nxtqueue[prevnode - 1] = node2;
                                    blks3_1.nxtqueue[node2 - 1] = node;
                                    prevnode = node2;
                                }

                                arrayit_1.x[arc - 1] = t2 - dx;
                                arrayit_1.u[arc - 1] += dx;
                                if (defcit == 0)
                                {
                                    break; //goto L4029;
                                }
                            }
                        }
                        else
                        {

                            /*	-ARC IS AN OUTGOING ARC FROM NODE. */

                            narc = -arc;
                            node2 = arrayit_1.endn[narc - 1];
                            t1 = arrayit_1.dfct[node2 - 1];

                            if (t1 > 0)
                            {
                                quit = true;
                                t2 = arrayit_1.u[narc - 1];

                                /* Computing MIN */
                                i__2 = ((defcit) < (t1)) ? (defcit) : (t1);
                                dx = ((i__2) < (t2)) ? (i__2) : (t2);
                                defcit -= dx;
                                arrayit_1.dfct[node2 - 1] = t1 - dx;

                                if (blks3_1.nxtqueue[node2 - 1] == 0)
                                {
                                    blks3_1.nxtqueue[prevnode - 1] = node2;
                                    blks3_1.nxtqueue[node2 - 1] = node;
                                    prevnode = node2;
                                }

                                arrayit_1.x[narc - 1] += dx;
                                arrayit_1.u[narc - 1] = t2 - dx;

                                if (defcit == 0)
                                {
                                    break; //goto L4029;
                                }
                            }
                        }
                    } // L4021:

                    arrayit_1.dfct[node - 1] = -defcit;

                } //L4029:
                else
                {
                    arrayit_1.dfct[node - 1] = -defcit;
                }


                /*  RECONSTRUCT THE LIST OF BALANCED ARCS INCIDENT TO NODE. */

                if (pchange)
                {
                    arc = blks2_1.fpushf[node - 1];
                    blks2_1.fpushf[node - 1] = 0;
                    while (arc > 0) //L4515:
                    {
                        nxtarc = blks2_1.nxtpushf[arc - 1];
                        blks2_1.nxtpushf[arc - 1] = -1;
                        arc = nxtarc;
                    } //goto L4515;

                    arc = blks2_1.fpushb[node - 1];
                    blks2_1.fpushb[node - 1] = 0;
                    while (arc > 0) //L4516:
                    {
                        nxtarc = blks2_1.nxtpushb[arc - 1];
                        blks2_1.nxtpushb[arc - 1] = -1;
                        arc = nxtarc;
                    } //goto L4516;

                    /*  NOW ADD THE CURRENTLY BALANCED ARCS TO THE LIST FOR THIS NODE */
                    /*  (WHICH IS NOW EMPTY), AND THE APPROPRIATE ADJACENT ONES. */
                    for (j = 1; j <= nb; ++j)
                    {
                        arc = blks_1.save[j - 1];
                        if (arc <= 0)
                        {
                            arc = -arc;
                        }
                        if (blks2_1.nxtpushf[arc - 1] < 0)
                        {
                            blks2_1.nxtpushf[arc - 1] = blks2_1.fpushf[arrayit_1.startn[arc - 1] - 1];
                            blks2_1.fpushf[arrayit_1.startn[arc - 1] - 1] = arc;
                        }
                        if (blks2_1.nxtpushb[arc - 1] < 0)
                        {
                            blks2_1.nxtpushb[arc - 1] = blks2_1.fpushb[arrayit_1.endn[arc - 1] - 1];
                            blks2_1.fpushb[arrayit_1.endn[arc - 1] - 1] = arc;
                        }
                    } // L4517:
                }
                /*     END OF SINGLE NODE ITERATION FOR A NEGATIVE DEFICIT NODE. */
            }

            if (quit || num_passes__ <= 3)
            {
                goto L100;
            }

            /* --------------------------------------------------------------------
                DO A MULTINODE ITERATION FROM NODE.
            --------------------------------------------------------------------- */

            ++output_1.nmultinode;

            /* UNMARK NODES LABELED EARLIER. */
            for (j = 1; j <= nlabel; ++j)
            {
                node2 = blks_1.label[j - 1];
                blks2_1.path_id__[node2 - 1] = false;
                blks2_1.scan[node2 - 1] = false;
            } // L4090:


            /* INITIALIZE LABELING. */
            nlabel = 1;
            blks_1.label[0] = node;
            blks2_1.path_id__[node - 1] = true;
            blks_1.prdcsr[node - 1] = 0;


            /* SCAN STARTING NODE. */
            blks2_1.scan[node - 1] = true;
            nscan = 1;
            dm = arrayit_1.dfct[node - 1];
            delx = 0;

            for (j = 1; j <= nb; ++j)
            {
                arc = blks_1.save[j - 1];
                if (arc > 0)
                {

                    /* ARC IS AN INCOMING ARC TO NODE. */

                    if (posit)
                    {
                        node2 = arrayit_1.endn[arc - 1];
                    }
                    else
                    {
                        node2 = arrayit_1.startn[arc - 1];
                    }
                    if (!blks2_1.path_id__[node2 - 1])
                    {
                        ++nlabel;
                        blks_1.label[nlabel - 1] = node2;
                        blks_1.prdcsr[node2 - 1] = arc;
                        blks2_1.path_id__[node2 - 1] = true;
                        delx += arrayit_1.x[arc - 1];
                    }
                }
                else
                {

                    /* ARC IS AN OUTGOING ARC FROM NODE. */

                    narc = -arc;
                    if (posit)
                    {
                        node2 = arrayit_1.startn[narc - 1];
                    }
                    else
                    {
                        node2 = arrayit_1.endn[narc - 1];
                    }
                    if (!blks2_1.path_id__[node2 - 1])
                    {
                        ++nlabel;
                        blks_1.label[nlabel - 1] = node2;
                        blks_1.prdcsr[node2 - 1] = arc;
                        blks2_1.path_id__[node2 - 1] = true;
                        delx += arrayit_1.u[narc - 1];
                    }
                }
            } // L4095:


            /*	START SCANNING A LABELED BUT UNSCANNED NODE. */
            /*  IF NUMBER OF NONZERO DEFICIT NODES IS SMALL, CONTINUE */
            /*  LABELING UNTIL A FLOW AUGMENTATION IS DONE. */

            switch__ = numnz < tp;

            do //L4120:
            {
                ++nscan;

                /*	CHECK TO SEE IF SWITCH NEEDS TO BE SET TO TRUE SO TO */
                /*  CONTINUE SCANNING EVEN AFTER A PRICE CHANGE. */

                switch__ = switch__ || nscan > ts && numnz < ts;


                /*  SCANNING WILL CONTINUE UNTIL EITHER AN OVERESTIMATE OF THE RESIDUAL */
                /*  CAPACITY ACROSS THE CUT CORRESPONDING TO THE SCANNED SET OF NODES (CALLED */
                /*  DELX) EXCEEDS THE ABSOLUTE VALUE OF THE TOTAL DEFICIT OF THE SCANNED */
                /*  NODES (CALLED DM), OR ELSE AN AUGMENTING PATH IS FOUND.  ARCS THAT ARE */
                /*  IN THE TREE BUT ARE NOT BALANCED ARE REMOVED AS PART OF THE SCANNING */
                /*  PROCESS. */

                i__ = blks_1.label[nscan - 1];
                blks2_1.scan[i__ - 1] = true;
                naugnod = 0;

                if (posit)
                {
                    /*  SCANNING NODE I IN CASE OF POSITIVE DEFICIT. */

                    prvarc = 0;
                    arc = blks2_1.fpushf[i__ - 1];
                    while (arc > 0) //L4518:
                    {

                        /*  ARC IS AN OUTGOING ARC FROM NODE. */

                        if (arrayit_1.rc[arc - 1] == 0)
                        {
                            if (arrayit_1.x[arc - 1] > 0)
                            {
                                node2 = arrayit_1.endn[arc - 1];
                                if (!blks2_1.path_id__[node2 - 1])
                                {

                                    /*  NODE2 IS NOT LABELED, SO ADD NODE2 TO THE LABELED SET. */

                                    blks_1.prdcsr[node2 - 1] = arc;
                                    if (arrayit_1.dfct[node2 - 1] < 0)
                                    {
                                        ++naugnod;
                                        blks_1.save[naugnod - 1] = node2;
                                    }
                                    ++nlabel;
                                    blks_1.label[nlabel - 1] = node2;
                                    blks2_1.path_id__[node2 - 1] = true;
                                    delx += arrayit_1.x[arc - 1];
                                }
                            }
                            prvarc = arc;
                            arc = blks2_1.nxtpushf[arc - 1];
                        }
                        else
                        {
                            tmparc = arc;
                            arc = blks2_1.nxtpushf[arc - 1];
                            blks2_1.nxtpushf[tmparc - 1] = -1;
                            if (prvarc == 0)
                            {
                                blks2_1.fpushf[i__ - 1] = arc;
                            }
                            else
                            {
                                blks2_1.nxtpushf[prvarc - 1] = arc;
                            }
                        }
                    } //goto L4518;


                    prvarc = 0;
                    arc = blks2_1.fpushb[i__ - 1];
                    while (arc > 0) //L4519:
                    {

                        /*  ARC IS AN INCOMING ARC INTO NODE. */

                        if (arrayit_1.rc[arc - 1] == 0)
                        {
                            if (arrayit_1.u[arc - 1] > 0)
                            {
                                node2 = arrayit_1.startn[arc - 1];
                                if (!blks2_1.path_id__[node2 - 1])
                                {

                                    /*  NODE2 IS NOT LABELED, SO ADD NODE2 TO THE LABELED SET. */

                                    blks_1.prdcsr[node2 - 1] = -arc;
                                    if (arrayit_1.dfct[node2 - 1] < 0)
                                    {
                                        ++naugnod;
                                        blks_1.save[naugnod - 1] = node2;
                                    }
                                    ++nlabel;
                                    blks_1.label[nlabel - 1] = node2;
                                    blks2_1.path_id__[node2 - 1] = true;
                                    delx += arrayit_1.u[arc - 1];
                                }
                            }
                            prvarc = arc;
                            arc = blks2_1.nxtpushb[arc - 1];
                        }
                        else
                        {
                            tmparc = arc;
                            arc = blks2_1.nxtpushb[arc - 1];
                            blks2_1.nxtpushb[tmparc - 1] = -1;
                            if (prvarc == 0)
                            {
                                blks2_1.fpushb[i__ - 1] = arc;
                            }
                            else
                            {
                                blks2_1.nxtpushb[prvarc - 1] = arc;
                            }
                        }
                    } //goto L4519;



                    /*  CORRECT THE RESIDUAL CAPACITY OF THE SCANNED NODE CUT. */

                    arc = blks_1.prdcsr[i__ - 1];
                    if (arc > 0)
                    {
                        delx -= arrayit_1.x[arc - 1];
                    }
                    else
                    {
                        delx -= arrayit_1.u[-arc - 1];
                    }

                    /*	END OF SCANNING OF NODE I FOR POSITIVE DEFICIT CASE. */

                }
                else
                {
                    /*  SCANNING NODE I FOR NEGATIVE DEFICIT CASE. */

                    prvarc = 0;
                    arc = blks2_1.fpushb[i__ - 1];
                    while (arc > 0) //L4520:
                    {
                        if (arrayit_1.rc[arc - 1] == 0)
                        {
                            if (arrayit_1.x[arc - 1] > 0)
                            {
                                node2 = arrayit_1.startn[arc - 1];
                                if (!blks2_1.path_id__[node2 - 1])
                                {
                                    blks_1.prdcsr[node2 - 1] = arc;
                                    if (arrayit_1.dfct[node2 - 1] > 0)
                                    {
                                        ++naugnod;
                                        blks_1.save[naugnod - 1] = node2;
                                    }
                                    ++nlabel;
                                    blks_1.label[nlabel - 1] = node2;
                                    blks2_1.path_id__[node2 - 1] = true;
                                    delx += arrayit_1.x[arc - 1];
                                }
                            }
                            prvarc = arc;
                            arc = blks2_1.nxtpushb[arc - 1];
                        }
                        else
                        {
                            tmparc = arc;
                            arc = blks2_1.nxtpushb[arc - 1];
                            blks2_1.nxtpushb[tmparc - 1] = -1;
                            if (prvarc == 0)
                            {
                                blks2_1.fpushb[i__ - 1] = arc;
                            }
                            else
                            {
                                blks2_1.nxtpushb[prvarc - 1] = arc;
                            }
                        }
                    } //goto L4520;


                    prvarc = 0;
                    arc = blks2_1.fpushf[i__ - 1];
                    while (arc > 0) //L4521:
                    {
                        if (arrayit_1.rc[arc - 1] == 0)
                        {
                            if (arrayit_1.u[arc - 1] > 0)
                            {
                                node2 = arrayit_1.endn[arc - 1];
                                if (!blks2_1.path_id__[node2 - 1])
                                {
                                    blks_1.prdcsr[node2 - 1] = -arc;
                                    if (arrayit_1.dfct[node2 - 1] > 0)
                                    {
                                        ++naugnod;
                                        blks_1.save[naugnod - 1] = node2;
                                    }
                                    ++nlabel;
                                    blks_1.label[nlabel - 1] = node2;
                                    blks2_1.path_id__[node2 - 1] = true;
                                    delx += arrayit_1.u[arc - 1];
                                }
                            }
                            prvarc = arc;
                            arc = blks2_1.nxtpushf[arc - 1];
                        }
                        else
                        {
                            tmparc = arc;
                            arc = blks2_1.nxtpushf[arc - 1];
                            blks2_1.nxtpushf[tmparc - 1] = -1;
                            if (prvarc == 0)
                            {
                                blks2_1.fpushf[i__ - 1] = arc;
                            }
                            else
                            {
                                blks2_1.nxtpushf[prvarc - 1] = arc;
                            }
                        }
                    } //goto L4521;

                    arc = blks_1.prdcsr[i__ - 1];
                    if (arc > 0)
                    {
                        delx -= arrayit_1.x[arc - 1];
                    }
                    else
                    {
                        delx -= arrayit_1.u[-arc - 1];
                    }
                    /*	END OF SCANNING OF NODE I FOR NEGATIVE DEFICIT CASE. */
                }


                /*	ADD DEFICIT OF NODE SCANNED TO DM. */

                dm += arrayit_1.dfct[i__ - 1];


                /*	CHECK IF THE SET OF SCANNED NODES CORRESPOND */
                /*  TO A DUAL ASCENT DIRECTION; IF YES, PERFORM A */
                /*  PRICE ADJUSTMENT STEP, OTHERWISE CONTINUE LABELING. */
                if (nscan < nlabel)
                {
                    if ((switch__) || (delx >= dm && delx >= -dm))
                    {
                        fg_pchange = false; //goto L4210;
                    }
                    else
                    {
                        fg_pchange = true;
                    }
                }
                else
                {
                    fg_pchange = true;
                }

                if (fg_pchange)
                {
                    /*	TRY A PRICE CHANGE. */
                    /*  [NOTE THAT SINCE DELX-ABS(DM) IS AN OVERESTIMATE OF ASCENT SLOPE, WE */
                    /*  MAY OCCASIONALLY TRY A DIRECTION THAT IS NOT AN ASCENT DIRECTION. */
                    /*  IN THIS CASE, THE ASCNT ROUTINES RETURN WITH QUIT=.FALSE., */
                    /*  SO WE CONTINUE LABELING NODES. */

                    if (posit)
                    {
                        GlobalMembersRelax4.ascnt1_(ref dm, ref delx, ref nlabel, ref blks_1.feasbl, ref switch__, ref nscan, ref node, ref prevnode);
                        ++output_1.num_ascnt__;
                    }
                    else
                    {
                        GlobalMembersRelax4.ascnt2_(ref dm, ref delx, ref nlabel, ref blks_1.feasbl, ref switch__, ref nscan, ref node, ref prevnode);
                        ++output_1.num_ascnt__;
                    }

                    if (!blks_1.feasbl)
                    {
                        //goto L4400;
                        if (!silent) mi.FireOnError("EXITED WHILE ATTEMPTING MULTI-NODE PRICE CHANGE.\nPROBLEM IS FOUND TO BE INFEASIBLE.");
                        return 1;
                    }

                    if (!switch__)
                    {
                        goto L100;
                    }


                    /*	STORE THOSE NEWLY LABELED NODES TO WHICH FLOW AUGMENTATION IS POSSIBLE. */
                    naugnod = 0;
                    for (j = nscan + 1; j <= nlabel; ++j)
                    {
                        node2 = blks_1.label[j - 1];
                        if (posit && arrayit_1.dfct[node2 - 1] < 0)
                        {
                            ++naugnod;
                            blks_1.save[naugnod - 1] = node2;
                        }
                        else if (!posit && arrayit_1.dfct[node2 - 1] > 0)
                        {
                            ++naugnod;
                            blks_1.save[naugnod - 1] = node2;
                        }
                    } // L530:
                }


                /*	CHECK IF FLOW AUGMENTATION IS POSSIBLE. */
                /*	IF NOT, RETURN TO SCAN ANOTHER NODE. */

                if (naugnod == 0) //L4210:
                {
                    continue; //goto L4120;
                }
                else
                {
                    for (j = 1; j <= naugnod; ++j)
                    {
                        ++output_1.num_augm__;
                        augnod = blks_1.save[j - 1];

                        if (posit)
                        {

                            /*	DO THE AUGMENTATION FROM NODE WITH POSITIVE DEFICIT. */

                            dx = -arrayit_1.dfct[augnod - 1];

                            ib = augnod;
                            while (ib != node) //L1500:
                            {
                                arc = blks_1.prdcsr[ib - 1];
                                if (arc > 0)
                                {
                                    /* Computing MIN */
                                    i__3 = arrayit_1.x[arc - 1];
                                    dx = ((dx) < (i__3)) ? (dx) : (i__3);
                                    ib = arrayit_1.startn[arc - 1];
                                }
                                else
                                {
                                    /* Computing MIN */
                                    i__3 = arrayit_1.u[-arc - 1];
                                    dx = ((dx) < (i__3)) ? (dx) : (i__3);
                                    ib = arrayit_1.endn[-arc - 1];
                                }
                            } //goto L1500;

                            /* Computing MIN */
                            i__3 = arrayit_1.dfct[node - 1];
                            dx = ((dx) < (i__3)) ? (dx) : (i__3);

                            if (dx > 0)
                            {
                                /*	INCREASE (DECREASE) THE FLOW OF ALL FORWARD (BACKWARD) */
                                /*  ARCS IN THE FLOW AUGMENTING PATH.  ADJUST NODE DEFICIT ACCORDINGLY. */

                                if (blks3_1.nxtqueue[augnod - 1] == 0)
                                {
                                    blks3_1.nxtqueue[prevnode - 1] = augnod;
                                    blks3_1.nxtqueue[augnod - 1] = node;
                                    prevnode = augnod;
                                }
                                arrayit_1.dfct[augnod - 1] += dx;
                                arrayit_1.dfct[node - 1] -= dx;

                                ib = augnod;
                                while (ib != node) //L1501:
                                {
                                    arc = blks_1.prdcsr[ib - 1];
                                    if (arc > 0)
                                    {
                                        arrayit_1.x[arc - 1] -= dx;
                                        arrayit_1.u[arc - 1] += dx;
                                        ib = arrayit_1.startn[arc - 1];
                                    }
                                    else
                                    {
                                        narc = -arc;
                                        arrayit_1.x[narc - 1] += dx;
                                        arrayit_1.u[narc - 1] -= dx;
                                        ib = arrayit_1.endn[narc - 1];
                                    }
                                } //goto L1501;
                            }
                            /*	END OF DOING THE AUGMENTATION FROM NODE WITH POSITIVE DEFICIT. */

                        }
                        else
                        {
                            /*	DO THE AUGMENTATION FROM NODE WITH NEGATIVE DEFICIT. */

                            dx = arrayit_1.dfct[augnod - 1];

                            ib = augnod;

                            while (ib != node) //L1502:
                            {
                                arc = blks_1.prdcsr[ib - 1];
                                if (arc > 0)
                                {
                                    /* Computing MIN */
                                    i__3 = arrayit_1.x[arc - 1];
                                    dx = ((dx) < (i__3)) ? (dx) : (i__3);
                                    ib = arrayit_1.endn[arc - 1];
                                }
                                else
                                {
                                    /* Computing MIN */
                                    i__3 = arrayit_1.u[-arc - 1];
                                    dx = ((dx) < (i__3)) ? (dx) : (i__3);
                                    ib = arrayit_1.startn[-arc - 1];
                                }
                            } //goto L1502;

                            /* Computing MIN */
                            i__3 = -arrayit_1.dfct[node - 1];
                            dx = ((dx) < (i__3)) ? (dx) : (i__3);

                            if (dx > 0)
                            {
                                /*	UPDATE THE FLOW AND DEFICITS. */

                                if (blks3_1.nxtqueue[augnod - 1] == 0)
                                {
                                    blks3_1.nxtqueue[prevnode - 1] = augnod;
                                    blks3_1.nxtqueue[augnod - 1] = node;
                                    prevnode = augnod;
                                }
                                arrayit_1.dfct[augnod - 1] -= dx;
                                arrayit_1.dfct[node - 1] += dx;

                                ib = augnod;
                                while (ib != node) //L1503:
                                {
                                    arc = blks_1.prdcsr[ib - 1];
                                    if (arc > 0)
                                    {
                                        arrayit_1.x[arc - 1] -= dx;
                                        arrayit_1.u[arc - 1] += dx;
                                        ib = arrayit_1.endn[arc - 1];
                                    }
                                    else
                                    {
                                        narc = -arc;
                                        arrayit_1.x[narc - 1] += dx;
                                        arrayit_1.u[narc - 1] -= dx;
                                        ib = arrayit_1.startn[narc - 1];
                                    }
                                } //goto L1503;
                            }
                            /*	END OF DOING THE AUGMENTATION FROM NODE WITH NEGATIVE DEFICIT. */
                        }

                        if (arrayit_1.dfct[node - 1] == 0)
                        {
                            goto L100;
                        }

                        if (arrayit_1.dfct[augnod - 1] != 0)
                        {
                            switch__ = false;
                        }
                    } // L4096:
                }

                /*	IF NODE STILL HAS NONZERO DEFICIT AND ALL NEWLY */
                /*  LABELED NODES HAVE SAME SIGN FOR THEIR DEFICIT AS */
                /*  NODE, WE CAN CONTINUE LABELING.  IN THIS CASE, CONTINUE */
                /*  LABELING ONLY WHEN FLOW AUGMENTATION IS DONE */
                /*  RELATIVELY INFREQUENTLY. */
            } while ((naugnod == 0) || (switch__ && output_1.iter > output_1.num_augm__ << 3));


            /*  RETURN TO DO ANOTHER RELAXATION ITERATION. */
            goto L100;


            /*	PROBLEM IS FOUND TO BE INFEASIBLE */
        //L4400:
        //    /*     PRINT*,' PROBLEM IS FOUND TO BE INFEASIBLE.' */
        //    /*     PRINT*, 'PROGRAM ENDED; PRESS <CR> TO EXIT' */
        //    /*     PAUSE */
        //    if (!silent) mi.FireOnError("PROBLEM IS FOUND TO BE INFEASIBLE.");
        //    return 1;
        } // relax4_
        /* Subroutine */
        public static int auction_()
        {
            /* System generated locals */
            long i__1;

            /* Local variables */
            long node = 0;
            long pend = 0;
            long incr = 0;
            long last = 0;
            long pass = 0;
            long term = 0;
            long flow = 0;
            long seclevel = 0;
            long red_cost__ = 0;
            long root = 0;
            long bstlevel = 0;
            long prevnode = 0;
            long i__ = 0;
            long resid = 0;
            long pterm = 0;
            long start = 0;
            long new_level__ = 0;
            long prevlevel = 0;
            long lastqueue = 0;
            long secarc = 0;
            long factor = 0;
            long extarc = 0;
            long rdcost = 0;
            long nolist = 0;
            long pstart = 0;
            long num_passes__ = 0;
            long arc = 0;
            long end = 0;
            long nas = 0;
            long prd = 0;
            long eps = 0;
            long prevarc = 0;
            long pr_term__ = 0;
            long thresh_dfct__ = 0;
            long mincost = 0;
            long maxcost = 0;
            long nxtnode = 0;

            /* --------------------------------------------------------------- */

            /*  PURPOSE - THIS SUBROUTINE USES A VERSION OF THE AUCTION */
            /*     ALGORITHM FOR MIN COST NETWORK FLOW TO COMPUTE A */
            /*     GOOD INITIAL FLOW AND PRICES FOR THE PROBLEM. */

            /* --------------------------------------------------------------- */

            /*     MAXNN = DIMENSION OF NODE-LENGTH ARRAYS */
            /*     MAXNA = DIMENSION OF ARC-LENGTH ARRAYS */


            /*  INPUT PARAMETERS */

            /*     N         = NUMBER OF NODES */
            /*     NA        = NUMBER OF ARCS */
            /*     LARGE     = A VERY LARGE INTEGER TO REPRESENT INFINITY */
            /*                 (SEE NOTE 3) */
            /*     STARTN(I) = STARTING NODE FOR THE I-TH ARC,    I = 1,...,NA */
            /*     ENDN(I)   = ENDING NODE FOR THE I-TH ARC,      I = 1,...,NA */
            /*     FOU(I)    = FIRST ARC LEAVING I-TH NODE,       I = 1,...,N */
            /*     NXTOU(I)  = NEXT ARC LEAVING THE STARTING NODE OF J-TH ARC, */
            /*                                                    I = 1,...,NA */
            /*     FIN(I)    = FIRST ARC ENTERING I-TH NODE,      I = 1,...,N */
            /*     NXTIN(I)  = NEXT ARC ENTERING THE ENDING NODE OF J-TH ARC, */
            /*                                                    I = 1,...,NA */


            /*  UPDATED PARAMETERS */

            /*     RC(J)     = REDUCED COST OF ARC J,             J = 1,...,NA */
            /*     U(J)      = RESIDUAL CAPACITY OF ARC J, */
            /*                                                    J = 1,...,NA */
            /*     X(J)      = FLOW ON ARC J,                     J = 1,...,NA */
            /*     DFCT(I)   = DEFICIT AT NODE I,                 I = 1,...,N */


            /*  OUTPUT PARAMETERS */

            /*  WORKING PARAMETERS */

            /* ^^                                     B                     ^^ */
            /* ^^          TEMPIN I1 P,TEMPOU PRICE I2,I3,I4,I5,I6,I7        ^^ */
            /* ^^                      MARK   TFSTOU  TNXTOU  TFSTIN TNXTIN ^^ */
            /* ^^                  I14      I15        I16     I17          ^^ */

            /*  START INITIALIZATION USING AUCTION */
            pass = 0;
            thresh_dfct__ = 0;

            /*  FACTOR DETERMINES BY HOW MUCH EPSILON IS REDUCED AT EACH MINIMIZATION */
            factor = DefineConstants.EPSFACTOR;

            /*  NUM_PASSES DETERMINES HOW MANY AUCTION SCALING PHASES ARE PERFORMED */
            num_passes__ = DefineConstants.MAXAUCTIONPASS; //1;

            /*    SET ARC FLOWS TO SATISFY CS AND CALCULATE MAXCOST AND MINCOST */
            maxcost = -input_1.large / 50;
            mincost = input_1.large / 50;

            i__1 = input_1.na;
            for (arc = 1; arc <= i__1; ++arc)
            {
                start = arrayit_1.startn[arc - 1];
                end = arrayit_1.endn[arc - 1];
                rdcost = arrayit_1.rc[arc - 1];

                if (maxcost < rdcost)
                {
                    maxcost = rdcost;
                }

                if (mincost > rdcost)
                {
                    mincost = rdcost;
                }

                if (rdcost < 0)
                {
                    arrayit_1.dfct[start - 1] += arrayit_1.u[arc - 1];
                    arrayit_1.dfct[end - 1] -= arrayit_1.u[arc - 1];
                    arrayit_1.x[arc - 1] = arrayit_1.u[arc - 1];
                    arrayit_1.u[arc - 1] = 0;
                }
                else
                {
                    arrayit_1.x[arc - 1] = 0;
                }
            } // L49:


            /*     SET INITIAL EPSILON */
            if (maxcost - mincost >= 8)
            {
                eps = (maxcost - mincost) / 8;
            }
            else
            {
                eps = 1;
            }

            /*     SET INITIAL PRICES TO ZERO */
            i__1 = input_1.n;
            for (node = 1; node <= i__1; ++node)
            {
                blks_1.label[node - 1] = 0;
            } // L48:


            /*     INITIALIZATION USING AUCTION/SHORTEST PATHS. */
        /*     START OF THE FIRST SCALING PHASE. */
        L100:
            ++pass;
            if (pass == num_passes__ || eps == 1)
            {
                input_1.crash = 0;
            }
            nolist = 0;

            /*     CONSTRUCT LIST OF POSITIVE SURPLUS NODES AND QUEUE OF NEGATIVE SURPLUS */
            /*     NODES */

            i__1 = input_1.n;
            for (node = 1; node <= i__1; ++node)
            {
                blks_1.prdcsr[node - 1] = 0;
                blks2_1.path_id__[node - 1] = false;
                blks3_1.extend_arc__[node - 1] = 0;
                blks3_1.sb_level__[node - 1] = -input_1.large;
                blks3_1.nxtqueue[node - 1] = node + 1;
                if (arrayit_1.dfct[node - 1] > 0)
                {
                    ++nolist;
                    blks_1.save[nolist - 1] = node;
                }
            } // L110:

            blks3_1.nxtqueue[input_1.n - 1] = 1;
            root = 1;
            prevnode = input_1.n;
            lastqueue = input_1.n;


            /*     INITIALIZATION WITH DOWN ITERATIONS FOR NEGATIVE SURPLUS NODES */
            i__1 = nolist;
            for (i__ = 1; i__ <= i__1; ++i__)
            {
                node = blks_1.save[i__ - 1];
                ++output_1.nsp;

                /*	BUILD THE LIST OF ARCS W/ ROOM FOR PUSHING FLOW */
                /*  AND FIND PROPER PRICE FOR DOWN ITERATION */

                bstlevel = -input_1.large;

                blks2_1.fpushf[node - 1] = 0;
                arc = blks_1.fou[node - 1];
                while (arc > 0) //L152:
                {
                    if (arrayit_1.u[arc - 1] > 0)
                    {
                        if (blks2_1.fpushf[node - 1] == 0)
                        {
                            blks2_1.fpushf[node - 1] = arc;
                            blks2_1.nxtpushf[arc - 1] = 0;
                            last = arc;
                        }
                        else
                        {
                            blks2_1.nxtpushf[last - 1] = arc;
                            blks2_1.nxtpushf[arc - 1] = 0;
                            last = arc;
                        }
                    }
                    if (arrayit_1.x[arc - 1] > 0)
                    {
                        new_level__ = blks_1.label[arrayit_1.endn[arc - 1] - 1] + arrayit_1.rc[arc - 1];
                        if (new_level__ > bstlevel)
                        {
                            bstlevel = new_level__;
                            extarc = arc;
                        }
                    }
                    arc = blks_1.nxtou[arc - 1];
                } //goto L152;


                blks2_1.fpushb[node - 1] = 0;
                arc = blks_1.fin[node - 1];
                while (arc > 0) //L154:
                {
                    if (arrayit_1.x[arc - 1] > 0)
                    {
                        if (blks2_1.fpushb[node - 1] == 0)
                        {
                            blks2_1.fpushb[node - 1] = arc;
                            blks2_1.nxtpushb[arc - 1] = 0;
                            last = arc;
                        }
                        else
                        {
                            blks2_1.nxtpushb[last - 1] = arc;
                            blks2_1.nxtpushb[arc - 1] = 0;
                            last = arc;
                        }
                    }
                    if (arrayit_1.u[arc - 1] > 0)
                    {
                        new_level__ = blks_1.label[arrayit_1.startn[arc - 1] - 1] - arrayit_1.rc[arc - 1];
                        if (new_level__ > bstlevel)
                        {
                            bstlevel = new_level__;
                            extarc = -arc;
                        }
                    }
                    arc = blks_1.nxtin[arc - 1];
                } //goto L154;

                blks3_1.extend_arc__[node - 1] = extarc;
                blks_1.label[node - 1] = bstlevel - eps;
            } // L150:


            /*     START THE AUGMENTATION CYCLES OF THE NEW SCALING PHASE. */
        L200:
            if (arrayit_1.dfct[root - 1] >= thresh_dfct__)
            {
                goto L3000;
            }
            term = root;
            blks2_1.path_id__[root - 1] = true;


            /*     MAIN FORWARD ALGORITHM WITH ROOT AS ORIGIN. */
        L500:
            /*     START OF A NEW FORWARD ITERATION */
            pterm = blks_1.label[term - 1];
            extarc = blks3_1.extend_arc__[term - 1];
            if (extarc == 0)
            {

                /*	BUILD THE LIST OF ARCS W/ ROOM FOR PUSHING FLOW */
                blks2_1.fpushf[term - 1] = 0;
                arc = blks_1.fou[term - 1];
                while (arc > 0) //L502:
                {
                    if (arrayit_1.u[arc - 1] > 0)
                    {
                        if (blks2_1.fpushf[term - 1] == 0)
                        {
                            blks2_1.fpushf[term - 1] = arc;
                            blks2_1.nxtpushf[arc - 1] = 0;
                            last = arc;
                        }
                        else
                        {
                            blks2_1.nxtpushf[last - 1] = arc;
                            blks2_1.nxtpushf[arc - 1] = 0;
                            last = arc;
                        }
                    }
                    arc = blks_1.nxtou[arc - 1];
                } //goto L502;


                blks2_1.fpushb[term - 1] = 0;
                arc = blks_1.fin[term - 1];
                while (arc > 0) //L504:
                {
                    if (arrayit_1.x[arc - 1] > 0)
                    {
                        if (blks2_1.fpushb[term - 1] == 0)
                        {
                            blks2_1.fpushb[term - 1] = arc;
                            blks2_1.nxtpushb[arc - 1] = 0;
                            last = arc;
                        }
                        else
                        {
                            blks2_1.nxtpushb[last - 1] = arc;
                            blks2_1.nxtpushb[arc - 1] = 0;
                            last = arc;
                        }
                    }
                    arc = blks_1.nxtin[arc - 1];
                } //goto L504;
                goto L600;
            }

            /*     SPECULATIVE PATH EXTENSION ATTEMPT */
            /*     NOTE: ARC>0 MEANS THAT ARC IS ORIENTED FROM THE ROOT TO THE DESTINATIONS */
            /*     ARC<0 MEANS THAT ARC IS ORIENTED FROM THE DESTINATIONS TO THE ROOT */
            /*     EXTARC=0 OR PRDARC=0, MEANS THE EXTENSION ARC OR THE PREDECESSOR ARC, */
            /*     RESPECTIVELY, HAS NOT BEEN ESTABLISHED */

            /* L510: */
            if (extarc > 0)
            {
                if (arrayit_1.u[extarc - 1] == 0)
                {
                    seclevel = blks3_1.sb_level__[term - 1];
                    goto L580;
                }
                end = arrayit_1.endn[extarc - 1];
                bstlevel = blks_1.label[end - 1] + arrayit_1.rc[extarc - 1];

                if (pterm >= bstlevel)
                {
                    if (blks2_1.path_id__[end - 1])
                    {
                        goto L1200;
                    }
                    term = end;
                    blks_1.prdcsr[term - 1] = extarc;
                    blks2_1.path_id__[term - 1] = true;

                    /*	IF NEGATIVE SURPLUS NODE IS FOUND, DO AN AUGMENTATION */
                    if (arrayit_1.dfct[term - 1] > 0)
                    {
                        goto L2000;
                    }

                    /*	RETURN FOR ANOTHER ITERATION */
                    goto L500;
                }
            }
            else
            {
                extarc = -extarc;
                if (arrayit_1.x[extarc - 1] == 0)
                {
                    seclevel = blks3_1.sb_level__[term - 1];
                    goto L580;
                }
                start = arrayit_1.startn[extarc - 1];
                bstlevel = blks_1.label[start - 1] - arrayit_1.rc[extarc - 1];
                if (pterm >= bstlevel)
                {
                    if (blks2_1.path_id__[start - 1])
                    {
                        goto L1200;
                    }
                    term = start;
                    blks_1.prdcsr[term - 1] = -extarc;
                    blks2_1.path_id__[term - 1] = true;

                    /*	IF NEGATIVE SURPLUS NODE IS FOUND, DO AN AUGMENTATION */
                    if (arrayit_1.dfct[term - 1] > 0)
                    {
                        goto L2000;
                    }

                    /*	RETURN FOR ANOTHER ITERATION */
                    goto L500;
                }
            }

            /*     SECOND BEST LOGIC TEST APPLIED TO SAVE A FULL NODE SCAN */
        /*     IF OLD BEST LEVEL CONTINUES TO BE BEST GO FOR ANOTHER CONTRACTION */
        L550:
            seclevel = blks3_1.sb_level__[term - 1];
            if (bstlevel <= seclevel)
            {
                goto L800;
            }

            /*     IF SECOND BEST CAN BE USED DO EITHER A CONTRACTION */
        /*     OR START OVER WITH A SPECULATIVE EXTENSION */
        L580:
            if (seclevel > -input_1.large)
            {
                extarc = blks3_1.sb_arc__[term - 1];

                if (extarc > 0)
                {
                    if (arrayit_1.u[extarc - 1] == 0)
                    {
                        goto L600;
                    }
                    bstlevel = blks_1.label[arrayit_1.endn[extarc - 1] - 1] + arrayit_1.rc[extarc - 1];
                }
                else
                {
                    if (arrayit_1.x[-extarc - 1] == 0)
                    {
                        goto L600;
                    }
                    bstlevel = blks_1.label[arrayit_1.startn[-extarc - 1] - 1] - arrayit_1.rc[-extarc - 1];
                }

                if (bstlevel == seclevel)
                {
                    blks3_1.sb_level__[term - 1] = -input_1.large;
                    blks3_1.extend_arc__[term - 1] = extarc;
                    goto L800;
                }
            }

            /*     EXTENSION/CONTRACTION ATTEMPT WAS UNSUCCESSFUL, SO SCAN TERMINAL NODE */

            L600:
            ++output_1.nsp;
            bstlevel = input_1.large;
            seclevel = input_1.large;

            arc = blks2_1.fpushf[term - 1];
            while (arc > 0) //L700:
            {
                new_level__ = blks_1.label[arrayit_1.endn[arc - 1] - 1] + arrayit_1.rc[arc - 1];
                if (new_level__ < seclevel)
                {
                    if (new_level__ < bstlevel)
                    {
                        seclevel = bstlevel;
                        bstlevel = new_level__;
                        secarc = extarc;
                        extarc = arc;
                    }
                    else
                    {
                        seclevel = new_level__;
                        secarc = arc;
                    }
                }
                arc = blks2_1.nxtpushf[arc - 1];
            } //goto L700;


            arc = blks2_1.fpushb[term - 1];
            while (arc > 0) //L710:
            {
                new_level__ = blks_1.label[arrayit_1.startn[arc - 1] - 1] - arrayit_1.rc[arc - 1];
                if (new_level__ < seclevel)
                {
                    if (new_level__ < bstlevel)
                    {
                        seclevel = bstlevel;
                        bstlevel = new_level__;
                        secarc = extarc;
                        extarc = -arc;
                    }
                    else
                    {
                        seclevel = new_level__;
                        secarc = -arc;
                    }
                }
                arc = blks2_1.nxtpushb[arc - 1];
            } //goto L710;

            blks3_1.sb_level__[term - 1] = seclevel;
            blks3_1.sb_arc__[term - 1] = secarc;
            blks3_1.extend_arc__[term - 1] = extarc;
        /*     END OF NODE SCAN. */


            /*     IF THE TERMINAL NODE IS THE ROOT, ADJUST ITS PRICE AND CHANGE ROOT */
        L800:
            if (term == root)
            {
                blks_1.label[term - 1] = bstlevel + eps;
                if (pterm >= input_1.large)
                {
                    //printf("NO PATH TO THE DESTINATION\n");
                    //printf("PROBLEM IS FOUND TO BE INFEASIBLE.\n");
                    //printf("PROGRAM ENDED; PRESS <CR> TO EXIT'\n");
                    /*         PRINT*,'NO PATH TO THE DESTINATION' */
                    /*         PRINT*,' PROBLEM IS FOUND TO BE INFEASIBLE.' */
                    /*         PRINT*, 'PROGRAM ENDED; PRESS <CR> TO EXIT' */
                    /*         PAUSE */
                    //exit(0);
                    if (pass == 1)
                    {
                        // Auction failed even for the 1ST pass.
                        // Set the flag to ZERO so that relax4 will do initialization as usual.
                        input_1.crash = 0;
                        Console.WriteLine("NO PATH FOUND TO THE DESTINATION BY AUCTION.");
                        //Console::WriteLine(S"   EPSILON = {0} AT SCALING PHRASE = {1}.",
                        //	eps.ToString(), pass.ToString());
                    }
                    else
                    {
                        input_1.crash = 1;
                        GlobalMembersRelax4.RecallAuctionValues();
                    }
                    return 0;
                }

                blks2_1.path_id__[root - 1] = false;
                prevnode = root;
                root = blks3_1.nxtqueue[root - 1];
                goto L200;
            }


            /*     CHECK WHETHER EXTENSION OR CONTRACTION */
            prd = blks_1.prdcsr[term - 1];
            if (prd > 0)
            {
                pr_term__ = arrayit_1.startn[prd - 1];
                prevlevel = blks_1.label[pr_term__ - 1] - arrayit_1.rc[prd - 1];
            }
            else
            {
                pr_term__ = arrayit_1.endn[-prd - 1];
                prevlevel = blks_1.label[pr_term__ - 1] + arrayit_1.rc[-prd - 1];
            }

            if (prevlevel > bstlevel)
            {

                /*	PATH EXTENSION */
                if (prevlevel >= bstlevel + eps)
                {
                    blks_1.label[term - 1] = bstlevel + eps;
                }
                else
                {
                    blks_1.label[term - 1] = prevlevel;
                }

                if (extarc > 0)
                {
                    end = arrayit_1.endn[extarc - 1];
                    if (blks2_1.path_id__[end - 1])
                    {
                        goto L1200;
                    }
                    term = end;
                }
                else
                {
                    start = arrayit_1.startn[-extarc - 1];
                    if (blks2_1.path_id__[start - 1])
                    {
                        goto L1200;
                    }
                    term = start;
                }

                blks_1.prdcsr[term - 1] = extarc;
                blks2_1.path_id__[term - 1] = true;


                /*	IF NEGATIVE SURPLUS NODE IS FOUND, DO AN AUGMENTATION */
                if (arrayit_1.dfct[term - 1] > 0)
                {
                    goto L2000;
                }

                /*	RETURN FOR ANOTHER ITERATION */
                goto L500;

            }
            else
            {
                /*	PATH CONTRACTION. */
                blks_1.label[term - 1] = bstlevel + eps;
                blks2_1.path_id__[term - 1] = false;
                term = pr_term__;

                if (pr_term__ != root)
                {
                    if (bstlevel <= pterm + eps)
                    {
                        goto L2000;
                    }
                }

                pterm = blks_1.label[term - 1];
                extarc = prd;

                if (prd > 0)
                {
                    bstlevel = bstlevel + eps + arrayit_1.rc[prd - 1];
                }
                else
                {
                    bstlevel = bstlevel + eps - arrayit_1.rc[-prd - 1];
                }

                /*	DO A SECOND BEST TEST AND IF THAT FAILS, DO A FULL NODE SCAN */
                goto L550;
            }


            /*     A CYCLE IS ABOUT TO FORM; DO A RETREAT SEQUENCE. */
        L1200:
            node = term;

        L1600:
            if (node != root)
            {
                blks2_1.path_id__[node - 1] = false;
                prd = blks_1.prdcsr[node - 1];

                if (prd > 0)
                {
                    pr_term__ = arrayit_1.startn[prd - 1];
                    if (blks_1.label[pr_term__ - 1] == blks_1.label[node - 1] + arrayit_1.rc[prd - 1] + eps)
                    {
                        node = pr_term__;
                        goto L1600;
                    }
                }
                else
                {
                    pr_term__ = arrayit_1.endn[-prd - 1];
                    if (blks_1.label[pr_term__ - 1] == blks_1.label[node - 1] - arrayit_1.rc[-prd - 1] + eps)
                    {
                        node = pr_term__;
                        goto L1600;
                    }
                }


                /*	DO A FULL SCAN AND PRICE RISE AT PR_TERM */
                ++output_1.nsp;
                bstlevel = input_1.large;
                seclevel = input_1.large;


                arc = blks2_1.fpushf[pr_term__ - 1];
                while (arc > 0) //L1700:
                {
                    new_level__ = blks_1.label[arrayit_1.endn[arc - 1] - 1] + arrayit_1.rc[arc - 1];
                    if (new_level__ < seclevel)
                    {
                        if (new_level__ < bstlevel)
                        {
                            seclevel = bstlevel;
                            bstlevel = new_level__;
                            secarc = extarc;
                            extarc = arc;
                        }
                        else
                        {
                            seclevel = new_level__;
                            secarc = arc;
                        }
                    }
                    arc = blks2_1.nxtpushf[arc - 1];
                } //goto L1700;


                arc = blks2_1.fpushb[pr_term__ - 1];
                while (arc > 0) //L1710:
                {
                    new_level__ = blks_1.label[arrayit_1.startn[arc - 1] - 1] - arrayit_1.rc[arc - 1];
                    if (new_level__ < seclevel)
                    {
                        if (new_level__ < bstlevel)
                        {
                            seclevel = bstlevel;
                            bstlevel = new_level__;
                            secarc = extarc;
                            extarc = -arc;
                        }
                        else
                        {
                            seclevel = new_level__;
                            secarc = -arc;
                        }
                    }
                    arc = blks2_1.nxtpushb[arc - 1];
                } //goto L1710;

                blks3_1.sb_level__[pr_term__ - 1] = seclevel;
                blks3_1.sb_arc__[pr_term__ - 1] = secarc;
                blks3_1.extend_arc__[pr_term__ - 1] = extarc;
                blks_1.label[pr_term__ - 1] = bstlevel + eps;

                if (pr_term__ == root)
                {
                    prevnode = root;
                    blks2_1.path_id__[root - 1] = false;
                    root = blks3_1.nxtqueue[root - 1];
                    goto L200;
                }

                blks2_1.path_id__[pr_term__ - 1] = false;
                prd = blks_1.prdcsr[pr_term__ - 1];

                if (prd > 0)
                {
                    term = arrayit_1.startn[prd - 1];
                }
                else
                {
                    term = arrayit_1.endn[-prd - 1];
                }

                if (term == root)
                {
                    prevnode = root;
                    blks2_1.path_id__[root - 1] = false;
                    root = blks3_1.nxtqueue[root - 1];
                    goto L200;
                }
                else
                {
                    goto L2000;
                }
            }
        /*     END OF AUCTION/SHORTEST PATH ROUTINE. */


            /*     DO AUGMENTATION FROM ROOT AND CORRECT THE PUSH LISTS */
        L2000:
            incr = -arrayit_1.dfct[root - 1];
            node = root;

            do //L2050:
            {
                extarc = blks3_1.extend_arc__[node - 1];
                blks2_1.path_id__[node - 1] = false;
                if (extarc > 0)
                {
                    node = arrayit_1.endn[extarc - 1];
                    if (incr > arrayit_1.u[extarc - 1])
                    {
                        incr = arrayit_1.u[extarc - 1];
                    }
                }
                else
                {
                    node = arrayit_1.startn[-extarc - 1];
                    if (incr > arrayit_1.x[-extarc - 1])
                    {
                        incr = arrayit_1.x[-extarc - 1];
                    }
                }

                //if (node != term) {
                //	goto L2050;
                //}
            } while (node != term);



            blks2_1.path_id__[term - 1] = false;
            if (arrayit_1.dfct[term - 1] > 0)
            {
                if (incr > arrayit_1.dfct[term - 1])
                {
                    incr = arrayit_1.dfct[term - 1];
                }
            }

            node = root;

        L2100:
            extarc = blks3_1.extend_arc__[node - 1];
            if (extarc > 0)
            {
                end = arrayit_1.endn[extarc - 1];

                /*	ADD ARC TO THE REDUCED GRAPH */

                if (arrayit_1.x[extarc - 1] == 0)
                {
                    blks2_1.nxtpushb[extarc - 1] = blks2_1.fpushb[end - 1];
                    blks2_1.fpushb[end - 1] = extarc;
                    new_level__ = blks_1.label[node - 1] - arrayit_1.rc[extarc - 1];
                    if (blks3_1.sb_level__[end - 1] > new_level__)
                    {
                        blks3_1.sb_level__[end - 1] = new_level__;
                        blks3_1.sb_arc__[end - 1] = -extarc;
                    }
                }
                arrayit_1.x[extarc - 1] += incr;
                arrayit_1.u[extarc - 1] -= incr;

                /*  REMOVE ARC FROM THE REDUCED GRAPH */

                if (arrayit_1.u[extarc - 1] == 0)
                {
                    ++nas;
                    arc = blks2_1.fpushf[node - 1];
                    if (arc == extarc)
                    {
                        blks2_1.fpushf[node - 1] = blks2_1.nxtpushf[arc - 1];
                    }
                    else
                    {
                        prevarc = arc;

                        arc = blks2_1.nxtpushf[arc - 1];
                        while (arc > 0) //L2200:
                        {
                            if (arc == extarc)
                            {
                                blks2_1.nxtpushf[prevarc - 1] = blks2_1.nxtpushf[arc - 1];
                                goto L2250;
                            }
                            prevarc = arc;
                            arc = blks2_1.nxtpushf[arc - 1];
                        } //goto L2200;
                    }
                }
            L2250:
                node = end;

            }
            else
            {
                extarc = -extarc;
                start = arrayit_1.startn[extarc - 1];

                /*	ADD ARC TO THE REDUCED GRAPH */

                if (arrayit_1.u[extarc - 1] == 0)
                {
                    blks2_1.nxtpushf[extarc - 1] = blks2_1.fpushf[start - 1];
                    blks2_1.fpushf[start - 1] = extarc;
                    new_level__ = blks_1.label[node - 1] + arrayit_1.rc[extarc - 1];
                    if (blks3_1.sb_level__[start - 1] > new_level__)
                    {
                        blks3_1.sb_level__[start - 1] = new_level__;
                        blks3_1.sb_arc__[start - 1] = extarc;
                    }
                }
                arrayit_1.u[extarc - 1] += incr;
                arrayit_1.x[extarc - 1] -= incr;

                /*	REMOVE ARC FROM THE REDUCED GRAPH */

                if (arrayit_1.x[extarc - 1] == 0)
                {
                    ++nas;
                    arc = blks2_1.fpushb[node - 1];
                    if (arc == extarc)
                    {
                        blks2_1.fpushb[node - 1] = blks2_1.nxtpushb[arc - 1];
                    }
                    else
                    {
                        prevarc = arc;

                        arc = blks2_1.nxtpushb[arc - 1];
                        while (arc > 0) //L2300:
                        {
                            if (arc == extarc)
                            {
                                blks2_1.nxtpushb[prevarc - 1] = blks2_1.nxtpushb[arc - 1];
                                goto L2350;
                            }
                            prevarc = arc;
                            arc = blks2_1.nxtpushb[arc - 1];
                        } //goto L2300;
                    }
                }
            L2350:
                node = start;
            }

            if (node != term)
            {
                goto L2100;
            }

            arrayit_1.dfct[term - 1] -= incr;
            arrayit_1.dfct[root - 1] += incr;


            /*		INSERT TERM IN THE QUEUE IF IT HAS A LARGE ENOUGH SURPLUS */
            if (arrayit_1.dfct[term - 1] < thresh_dfct__)
            {
                if (blks3_1.nxtqueue[term - 1] == 0)
                {
                    nxtnode = blks3_1.nxtqueue[root - 1];
                    if (blks_1.label[term - 1] >= blks_1.label[nxtnode - 1] && root != nxtnode)
                    {
                        blks3_1.nxtqueue[root - 1] = term;
                        blks3_1.nxtqueue[term - 1] = nxtnode;
                    }
                    else
                    {
                        blks3_1.nxtqueue[prevnode - 1] = term;
                        blks3_1.nxtqueue[term - 1] = root;
                        prevnode = term;
                    }
                }
            }


            /*     IF ROOT HAS A LARGE ENOUGH SURPLUS, KEEP IT */
            /*     IN THE QUEUE AND RETURN FOR ANOTHER ITERATION */
            if (arrayit_1.dfct[root - 1] < thresh_dfct__)
            {
                prevnode = root;
                root = blks3_1.nxtqueue[root - 1];
                goto L200;
            }

            /*     END OF AUGMENTATION CYCLE */

            L3000:
            /*     CHECK FOR TERMINATION OF SCALING PHASE. IF SCALING PHASE IS */
            /*     NOT FINISHED, ADVANCE THE QUEUE AND RETURN TO TAKE ANOTHER NODE. */
            nxtnode = blks3_1.nxtqueue[root - 1];
            if (root != nxtnode)
            {
                blks3_1.nxtqueue[root - 1] = 0;
                blks3_1.nxtqueue[prevnode - 1] = nxtnode;
                root = nxtnode;
                goto L200;
            }

            /*     END OF SUBPROBLEM (SCALING PHASE). */


            /*     REDUCE ALL PRICES TO REDUCE DANGER OF OVERFLOW */
            GlobalMembersRelax4.ReduceAuctionPrice(); //Added by ST on 01/19/2010.

            /* L3600: */

            /*     IF ANOTHER AUCTION SCALING PHASE REMAINS, RESET THE FLOWS & THE PUSH LISTS */
            /*     ELSE RESET ARC FLOWS TO SATISFY CS AND COMPUTE REDUCED COSTS */

            if (input_1.crash == 1)
            {

                // Save auction result before adjusting any value, added by ST on 10/09/2009.
                GlobalMembersRelax4.SaveAuctionValues(pass, eps, true);

                /*     REDUCE EPSILON. */
                eps /= factor;
                if (eps < 1)
                {
                    eps = 1;
                }

                thresh_dfct__ /= factor;
                if (eps == 1)
                {
                    thresh_dfct__ = 0;
                }

                i__1 = input_1.na;
                for (arc = 1; arc <= i__1; ++arc)
                {
                    start = arrayit_1.startn[arc - 1];
                    end = arrayit_1.endn[arc - 1];
                    pstart = blks_1.label[start - 1];
                    pend = blks_1.label[end - 1];
                    red_cost__ = arrayit_1.rc[arc - 1] + pend - pstart;

                    if (0 > red_cost__ + eps)
                    {
                        resid = arrayit_1.u[arc - 1];

                        if (resid > 0)
                        {
                            arrayit_1.dfct[start - 1] += resid;
                            arrayit_1.dfct[end - 1] -= resid;
                            arrayit_1.x[arc - 1] += resid;
                            arrayit_1.u[arc - 1] = 0;
                        }
                    }
                    else if (0 < red_cost__ - eps)
                    {
                        flow = arrayit_1.x[arc - 1];
                        if (flow > 0)
                        {
                            arrayit_1.dfct[start - 1] -= flow;
                            arrayit_1.dfct[end - 1] += flow;
                            arrayit_1.x[arc - 1] = 0;
                            arrayit_1.u[arc - 1] += flow;
                        }
                    }
                } // L3800:

                /*	RETURN FOR ANOTHER PHASE */
                /* L3850: */
                goto L100;

            }
            else
            {
                GlobalMembersRelax4.SaveAuctionValues(pass, eps, false);

                input_1.crash = 1;

                i__1 = input_1.na;
                for (arc = 1; arc <= i__1; ++arc)
                {
                    start = arrayit_1.startn[arc - 1];
                    end = arrayit_1.endn[arc - 1];
                    pstart = blks_1.label[start - 1];
                    pend = blks_1.label[end - 1];

                    red_cost__ = arrayit_1.rc[arc - 1] + pend - pstart;
                    if (red_cost__ < 0)
                    {
                        resid = arrayit_1.u[arc - 1];

                        if (resid > 0)
                        {
                            arrayit_1.dfct[start - 1] += resid;
                            arrayit_1.dfct[end - 1] -= resid;
                            arrayit_1.x[arc - 1] += resid;
                            arrayit_1.u[arc - 1] = 0;
                        }
                    }
                    else if (red_cost__ > 0)
                    {
                        flow = arrayit_1.x[arc - 1];
                        if (flow > 0)
                        {
                            arrayit_1.dfct[start - 1] -= flow;
                            arrayit_1.dfct[end - 1] += flow;
                            arrayit_1.x[arc - 1] = 0;
                            arrayit_1.u[arc - 1] += flow;
                        }
                    }
                    arrayit_1.rc[arc - 1] = red_cost__;
                } // L3900:
            }
            return 0;
        } // auction_

        // Todo:
        //X 1.  Remove capacity constraining portion of relax4.
        //X 2.  Store results at end of iter 0 relax run.
        //X 3.  Use results at iter > 0 for incremental relax run.
        //  4.  Handle comp slack conditions.
        //      -- delta link flows become node supplies.
        //      -- for places that slack is violated, create a pair of node supplies.
        //      -- note that dfct is negative supply
        public static void InitializeValues()
        {
            /*******************************************************************************************
           InitializeValues - INITIALIZE CORRESPONDING VARIABLES FROM MODSIM/KILTER FOR USE WITH RELAX.
           ----------------------------------------------------------------------------------------------
           Note:
           - This rountine was extracted from 'relaxcallfortraninternal' routine by ST on 12/28/2009.
           \********************************************************************************************/
            int i;

            //#pragma omp parallel //num_threads(NOMP)
            {
                //#pragma omp for nowait
                for (i = 1; i <= DefineConstants.MAXLINKSRELAX; ++i)
                {
                    //int ithread = omp_get_thread_num();

                    arrayit_1.startn[i - 1] = DefineConstants.NODATAVALUE; //999999999;
                    arrayit_1.endn[i - 1] = DefineConstants.NODATAVALUE; //999999999;
                    arrayit_1.c__[i - 1] = DefineConstants.NODATAVALUE; //999999999;
                    arrayit_1.rc[i - 1] = DefineConstants.NODATAVALUE;
                    arrayit_1.u[i - 1] = DefineConstants.NODATAVALUE; //999999999;
                    arrayit_1.x[i - 1] = DefineConstants.NODATAVALUE; //999999999;
                    blks_1.nxtin[i - 1] = DefineConstants.NODATAVALUE; //999999999;
                    blks_1.nxtou[i - 1] = DefineConstants.NODATAVALUE; //999999999;
                }

                //#pragma omp for
                for (i = 1; i <= DefineConstants.MAXNODESRELAX; ++i)
                {
                    //int ithread = omp_get_thread_num();

                    arrayit_1.dfct[i - 1] = 0;
                    blks_1.fin[i - 1] = DefineConstants.NODATAVALUE; //999999999;
                    blks_1.fou[i - 1] = DefineConstants.NODATAVALUE; //999999999;
                    blks_1.label[i - 1] = DefineConstants.NODATAVALUE; //999999999;
                    blks_1.prdcsr[i - 1] = DefineConstants.NODATAVALUE; //999999999;
                }
            } //End of parallel region.
        }
        public static void InitializeValues(Model mi)
        {
            {
                for (int i = 0; i < mi.mInfo.lList.Length + mi.mInfo.outputLinkList.Length; ++i)
                {
                    arrayit_1.startn[i] = DefineConstants.NODATAVALUE; //999999999;
                    arrayit_1.endn[i] = DefineConstants.NODATAVALUE; //999999999;
                    arrayit_1.c__[i] = DefineConstants.NODATAVALUE; //999999999;
                    arrayit_1.rc[i] = DefineConstants.NODATAVALUE;
                    arrayit_1.u[i] = DefineConstants.NODATAVALUE; //999999999;
                    arrayit_1.x[i] = DefineConstants.NODATAVALUE; //999999999;
                    blks_1.nxtin[i] = DefineConstants.NODATAVALUE; //999999999;
                    blks_1.nxtou[i] = DefineConstants.NODATAVALUE; //999999999;
                }

                for (int i = 0; i < mi.mInfo.nList.Length + mi.mInfo.outputNodeList.Length; ++i)
                {
                    arrayit_1.dfct[i] = 0;
                    blks_1.fin[i] = DefineConstants.NODATAVALUE; //999999999;
                    blks_1.fou[i] = DefineConstants.NODATAVALUE; //999999999;
                    blks_1.label[i] = DefineConstants.NODATAVALUE; //999999999;
                    blks_1.prdcsr[i] = DefineConstants.NODATAVALUE; //999999999;
                }
            }
        }
        public static void SetupValues(Model mi)
        {
            /*******************************************************************************************
            SetupValues -	SET UP CORRESPONDING VARIABLES FROM MODSIM/KILTER FOR USE WITH RELAX,
                            TRANSFORMATION OF DATA FOR LOWER BOUND CONDITION, AND
                            SET DUAL PRICE TO ZERO (REDUCE COST = COST).
            ----------------------------------------------------------------------------------------------
            Note:
            - This rountine was extracted from 'relaxcallfortraninternal' routine by ST on 12/28/2009.
            - For some reasons, if this routine is in multi-threading, it will provide wired results.
            \********************************************************************************************/
            int i;
            int m;
            long danger_thresh__;

            input_1.large = DefineConstants.LARGESTVALUE; //500000000;
            danger_thresh__ = input_1.large / 50;

            m = mi.NextNodeNum - 1; // The number of NODES, including MODSIM artificial nodes.
            input_1.n = m; // The number of NODES, including MODSIM artificial nodes.

            m = 0; // Link counter; I do not know exactly why it needed.

            for (i = 1; i <= mi.NextLinkNum - 1; ++i)
            {
                // NOTE: It not clear to me why this for-loop could not run parallel.
                arrayit_1.startn[i - 1] = mi.mInfo.lList[i].from.number;
                arrayit_1.endn[i - 1] = mi.mInfo.lList[i].to.number;
                arrayit_1.c__[i - 1] = mi.mInfo.lList[i].mlInfo.cost;
                arrayit_1.x[i - 1] = mi.mInfo.lList[i].mlInfo.lo;
                arrayit_1.u[i - 1] = mi.mInfo.lList[i].mlInfo.hi;

                if (arrayit_1.startn[i - 1] == input_1.n + 1)
                {
                    //#pragma omp critical
                    arrayit_1.dfct[arrayit_1.endn[i - 1] - 1] = -arrayit_1.u[i - 1];
                }
                else if (arrayit_1.endn[i - 1] == input_1.n + 2)
                {
                    //#pragma omp critical
                    arrayit_1.dfct[arrayit_1.startn[i - 1] - 1] = arrayit_1.u[i - 1];
                }
                else
                {

                    ++m;

                    if (arrayit_1.u[i - 1] > danger_thresh__)
                    {
                        Console.WriteLine("Danger u {0}, i = {1}.", arrayit_1.u[i - 1].ToString(), i.ToString());
                        arrayit_1.u[i - 1] = danger_thresh__; //- 1000;
                    }
                    if (arrayit_1.c__[i - 1] > danger_thresh__)
                    {
                        Console.WriteLine("Danger c {0}, i = {1}.", arrayit_1.c__[i - 1].ToString(), i.ToString());
                        arrayit_1.c__[i - 1] = danger_thresh__;
                    }
                    if (arrayit_1.c__[i - 1] < -danger_thresh__)
                    {
                        Console.WriteLine("Danger c {0}, i = {1}.", arrayit_1.c__[i - 1].ToString(), i.ToString());
                        arrayit_1.c__[i - 1] = -danger_thresh__;
                    }

                    arrayit_1.startn[m - 1] = arrayit_1.startn[i - 1];
                    arrayit_1.endn[m - 1] = arrayit_1.endn[i - 1];
                    arrayit_1.c__[m - 1] = arrayit_1.c__[i - 1];
                    arrayit_1.u[m - 1] = arrayit_1.u[i - 1];
                }
            }

            input_1.na = m; // The number of LINKS, including MODSIM artificial links.


            //#pragma omp parallel //num_threads(NOMP)
            {
                /*		TRANSFORMATION OF DATA FOR LOWER BOUND CONDITION. */
                //#pragma omp for nowait
                for (i = 1; i <= input_1.na; ++i)
                {
                    //int ithread = omp_get_thread_num();

                    //#pragma omp critical(dfct)
                    {
                        arrayit_1.dfct[arrayit_1.startn[i - 1] - 1] += mi.mInfo.lList[i].mlInfo.lo;
                        arrayit_1.dfct[arrayit_1.endn[i - 1] - 1] -= mi.mInfo.lList[i].mlInfo.lo;
                    }

                    arrayit_1.x[i - 1] -= mi.mInfo.lList[i].mlInfo.lo;
                    arrayit_1.u[i - 1] -= mi.mInfo.lList[i].mlInfo.lo;
                }

                /*		SET UP REDUCED COST = LINK COST SO THAT DUAL PRICE = 0. */
                //#pragma omp for
                for (i = 1; i <= mi.NextLinkNum; ++i)
                {
                    //int ithread = omp_get_thread_num();

                    arrayit_1.rc[i - 1] = arrayit_1.c__[i - 1];
                }
            } //End of parallel region.
        }
        public static void CheckNETWORK(Model mi)
        {
            /*****************************************************************************
            CheckNETWORK - Compare networks to verify the connectivity has not changed, 
            and also the cost not significantly changed from the network previously solved.
            -------------------------------------------------------------------------------
            \*****************************************************************************/
            int i;
            double mcost;
            double dcost;

            /*  INITIALIZE BEFORE CHECKING. */
            input_1.repeat = true;

            /*  CHECKING PROCESSES. */
            if (arraysave_1.numlinks != input_1.na)
            {
                //The network DOES change.
                input_1.repeat = false;
            }
            else
            {
                for (i = 1; i <= mi.NextLinkNum - 1; ++i)
                {
                    if (arrayit_1.startn[i - 1] != arraysave_1.startn[i - 1] || arrayit_1.endn[i - 1] != arraysave_1.endn[i - 1])
                    {
                        input_1.repeat = false;
                        break; // Network was changed; Exit the loop, added by ST on 09/24/2009.
                    }
                    if (arraysave_1.cost[i - 1] != mi.mInfo.lList[i].mlInfo.cost)
                    {
                        //Relax the condition about cost change, added by ST on 09/24/2009.
                        if ((arraysave_1.cost[i - 1] == 0 && mi.mInfo.lList[i].mlInfo.cost != 0) || (arraysave_1.cost[i - 1] != 0 && mi.mInfo.lList[i].mlInfo.cost == 0) || (arraysave_1.cost[i - 1] * mi.mInfo.lList[i].mlInfo.cost < 0))
                        {
                            // Cost was changed in sign: Negative, Positive, Zero.
                            input_1.repeat = false;
                            break;
                        }
                        else
                        {
                            mcost = (double)(mi.mInfo.lList[i].mlInfo.cost) / (double)(arraysave_1.cost[i - 1]);
                            if (mcost > 1)
                                mcost = 1 / mcost;
                            dcost = 1 - mcost;
                            if (dcost > 0.90)
                            {
                                // Cost was changed in really huge amount.
                                input_1.repeat = false;
                                break;
                            }
                        }
                    }
                }
            }
        }

        public static void UpdateValues()
        {
            /*******************************************************************************************
           UpdateValues - Use arraysave values to prepare run if possible.
           ----------------------------------------------------------------------------------------------
           Note that node supply, arc capacity, arc cost could be changed from the solution 
           of the previous run. But, it is recommended that arc cost should not have a huge change. 
           See logic for filter a huge cost change in 'relaxcallfortraninternal' subroutine.
           This rountine was transformed from the original RELAX4 code in FORTRAN by ST on 10/09/2009.
           \********************************************************************************************/
            int i;
            long del_dem;
            long del_cap;
            long del_cost;

            del_dem = 0;
            del_cap = 0;
            del_cost = 0;

            //#pragma omp parallel //num_threads(NOMP)
            {
                //#pragma omp for private(del_dem)
                for (i = 0; i < input_1.n; i++)
                {
                    //int ithread = omp_get_thread_num();

                    // NODE FLOW DEMAND CHANGED.
                    del_dem = arrayit_1.dfct[i] - arraysave_1.dfct[i];
                    arrayit_1.dfct[i] = del_dem;
                }

                //#pragma omp for private (del_cap, del_cost)
                for (i = 0; i < input_1.na; i++)
                {
                    //int ithread = omp_get_thread_num();

                    // CHECK CHANGE IN ARC FLOW CAPACITY AND/OR ARC COST.

                    del_cap = (arrayit_1.x[i] + arrayit_1.u[i]) - (arraysave_1.x[i] + arraysave_1.u[i]);
                    del_cost = arrayit_1.c__[i] - arraysave_1.cost[i];

                    if (arraysave_1.rc[i] < 0)
                    {
                        // ARC is now active.
                        //#pragma omp atomic
                        arrayit_1.dfct[arrayit_1.startn[i] - 1] += del_cap;
                        //#pragma omp atomic
                        arrayit_1.dfct[arrayit_1.endn[i] - 1] -= del_cap;

                        // Increase flow to (new) capacity due to ARC FLOW CAPACITY CHANGED.
                        arrayit_1.x[i] = arraysave_1.x[i] + del_cap;
                        arrayit_1.u[i] = arraysave_1.u[i];
                        if (arrayit_1.x[i] < 0 || arrayit_1.u[i] != 0)
                            Console.WriteLine("\n x < 0 or u <> 0.");

                        if (arraysave_1.rc[i] > -del_cost)
                        {
                            // ARC then becomes inactive due to ARC COST CHANGED.
                            //#pragma omp atomic
                            arrayit_1.dfct[arrayit_1.startn[i] - 1] -= arrayit_1.x[i];
                            //#pragma omp atomic
                            arrayit_1.dfct[arrayit_1.endn[i] - 1] += arrayit_1.x[i];

                            // Decrease flow to zero.
                            arrayit_1.u[i] = arrayit_1.x[i];
                            arrayit_1.x[i] = 0;
                        }

                        if (arrayit_1.x[i] < 0 || arrayit_1.u[i] < 0)
                        {
                            Console.WriteLine("\n x < 0 or u < 0.");
                            //throw new Exception();
                        }

                    }
                    else if (arraysave_1.rc[i] > 0)
                    {
                        // ARC is now inactive.
                        // Preserve flow as it was at zero.
                        // Maintain (new) capacity due to ARC FLOW CAPACITY CHANGED.
                        arrayit_1.u[i] = arraysave_1.u[i] + del_cap;
                        arrayit_1.x[i] = arraysave_1.x[i];
                        if (arrayit_1.x[i] != 0 || arrayit_1.u[i] < 0)
                        {
                            Console.WriteLine("\n x <> 0 or u < 0.");
                            //throw new Exception();
                        }

                        if (arraysave_1.rc[i] < -del_cost)
                        {
                            // ARC then becomes active, due to ARC COST CHANGED. 
                            //#pragma omp atomic
                            arrayit_1.dfct[arrayit_1.startn[i] - 1] += arrayit_1.u[i];
                            //#pragma omp atomic
                            arrayit_1.dfct[arrayit_1.endn[i] - 1] -= arrayit_1.u[i];

                            // Increase flow to capacity.
                            arrayit_1.x[i] = arrayit_1.u[i];
                            arrayit_1.u[i] = 0;
                        }
                        if (arrayit_1.x[i] < 0 || arrayit_1.u[i] < 0)
                        {
                            Console.WriteLine("\n x < 0 or u < 0.");
                            //throw new Exception();
                        }

                    }
                    else if (arraysave_1.rc[i] == 0)
                    {
                        // ARC is balanced.
                        if (arraysave_1.u[i] >= -del_cap)
                        {
                            // New capacitiy larger than current flow.
                            // Preserve flow as it was.
                            // Maintain (new) capacity due to ARC FLOW CAPACITY CHANGED.
                            arrayit_1.u[i] = arraysave_1.u[i] + del_cap;
                            arrayit_1.x[i] = arraysave_1.x[i];
                        }
                        else if (arraysave_1.u[i] < -del_cap)
                        {
                            // New capacity less than current flow.
                            long del = -del_cap - arraysave_1.u[i];
                            //#pragma omp atomic
                            arrayit_1.dfct[arrayit_1.startn[i] - 1] -= del;
                            //#pragma omp atomic
                            arrayit_1.dfct[arrayit_1.endn[i] - 1] += del;

                            // Decrease flow to new capacity.
                            arrayit_1.x[i] = arraysave_1.x[i] - del;
                            arrayit_1.u[i] = 0;
                        }
                        if (del_cost < 0)
                        {
                            // ARC then becomes active due to ARC COST CHANGED.  
                            //#pragma omp atomic
                            arrayit_1.dfct[arrayit_1.startn[i] - 1] += arrayit_1.u[i];
                            //#pragma omp atomic
                            arrayit_1.dfct[arrayit_1.endn[i] - 1] -= arrayit_1.u[i];

                            // Increase flow to capacity.
                            arrayit_1.x[i] = arrayit_1.x[i] + arrayit_1.u[i];
                            arrayit_1.u[i] = 0;
                        }
                        else if (del_cost > 0)
                        {
                            // ARC then becomes inactive due to ARC COST CHANGED. 
                            //#pragma omp atomic
                            arrayit_1.dfct[arrayit_1.startn[i] - 1] -= arrayit_1.x[i];
                            //#pragma omp atomic
                            arrayit_1.dfct[arrayit_1.endn[i] - 1] += arrayit_1.x[i];

                            // Decrease flow to zero.
                            arrayit_1.u[i] = arrayit_1.x[i] + arrayit_1.u[i];
                            arrayit_1.x[i] = 0;
                        }
                        if (arrayit_1.x[i] < 0 || arrayit_1.u[i] < 0)
                        {
                            Console.WriteLine("\n x < 0 or u < 0.");
                            //throw new Exception();
                        }
                    }

                    arrayit_1.rc[i] = arraysave_1.rc[i] + del_cost;

                    // If ARC becomes balanced, check to add ARC to TFSTOU, TFSTIN,...
                    if (arrayit_1.rc[i] == 0 && del_cost != 0)
                    {
                        long node;
                        long arc;
                        node = arrayit_1.startn[i];
                        arc = blks2_1.fpushf[node];
                        while (arc > 0)
                        {
                            if (arc == i)
                                break;
                            arc = blks2_1.nxtpushf[arc];
                            if (arc <= 0)
                            {
                                blks2_1.nxtpushf[i] = blks2_1.fpushf[node];
                                blks2_1.fpushf[node] = i;
                            }
                        }
                        node = arrayit_1.endn[i];
                        arc = blks2_1.fpushb[node];
                        while (arc > 0)
                        {
                            if (arc == i)
                                break;
                            arc = blks2_1.nxtpushb[arc];
                            if (arc <= 0)
                            {
                                blks2_1.nxtpushb[i] = blks2_1.fpushb[node];
                                blks2_1.fpushb[node] = i;
                            }
                        }
                    }
                }
            } //End of parallel region.
        }
        public static void CheckOUTPUT()
        {
            int i;

            output_1.feasbl = true;

            //#pragma omp parallel
            {
                /*  CHECK CORRECTNESS OF OUTPUT PARAMETERS AT NODES. */
                //#pragma omp for nowait
                for (i = 1; i <= input_1.n; i++)
                {
                    if (arrayit_1.dfct[i - 1] != 0)
                    {
                        Console.WriteLine("NONZERO DEFICIT AT NODE {0} = {1}.", i.ToString(), arrayit_1.dfct[i - 1].ToString());

                        //#pragma omp critical(feasbl)
                        output_1.feasbl = false;
                        //blks_1.feasbl = FALSE;            
                    }
                }

                /*	CHECK CORRECTNESS OF OUTPUT PARAMETERS AT ARCS. */
                //#pragma omp for
                for (i = 1; i <= input_1.na; i++)
                {
                    if ((arrayit_1.rc[i - 1] > 0 && arrayit_1.x[i - 1] != 0) || (arrayit_1.rc[i - 1] < 0 && arrayit_1.u[i - 1] != 0) || (arrayit_1.rc[i - 1] == 0 && (arrayit_1.x[i - 1] < 0 || arrayit_1.u[i - 1] < 0)))
                    {
                        Console.WriteLine("COMPLEMENTARY SLACKNESS VIOLATED AT ARC {0}: {1}, {2}, {3}.", i.ToString(), arrayit_1.rc[i - 1].ToString(), arrayit_1.x[i - 1].ToString(), arrayit_1.u[i - 1].ToString());

                        //#pragma omp critical(feasbl)			
                        output_1.feasbl = false;
                        //blks_1.feasbl = FALSE;
                    }
                }
            }
        }
        public static void SaveValues(Model mi)
        {
            /*****************************************************************************
            SaveCalculatedValues - Save most recently calculated relax values.
            This sub also check the correctness of output parameters.
            -------------------------------------------------------------------------------
            Values:
              cost - costs as passed
              dfct - deficits created by lower bound conditions.
            \*****************************************************************************/
            int i;

            arraysave_1.numlinks = input_1.na;
            arraysave_1.tcost = 0;

            //#pragma omp parallel //num_threads(NOMP)
            {
                /*      INITIALIZE dfct BEFORE USED TO SAVE. */
                //#pragma omp for
                for (i = 0; i < input_1.n; i++)
                {
                    //int ithread = omp_get_thread_num();

                    arraysave_1.dfct[i] = 0;
                }

                /*      SAVE ARRAYS RELATED TO ARC. */
                //#pragma omp for
                for (i = 0; i < input_1.na; i++)
                {
                    //int ithread = omp_get_thread_num();

                    arraysave_1.startn[i] = arrayit_1.startn[i];
                    arraysave_1.endn[i] = arrayit_1.endn[i];
                    arraysave_1.x[i] = arrayit_1.x[i];
                    arraysave_1.u[i] = arrayit_1.u[i];
                    arraysave_1.rc[i] = arrayit_1.rc[i];
                    arraysave_1.cost[i] = mi.mInfo.lList[i + 1].mlInfo.cost; // Original Cost.

                    // SAVE DEFICIT/DEMAND BEFORE THE SOLUTION.
                    //#pragma omp atomic
                    arraysave_1.dfct[arraysave_1.startn[i] - 1] += mi.mInfo.lList[i + 1].mlInfo.lo;

                    //#pragma omp atomic
                    arraysave_1.dfct[arraysave_1.endn[i] - 1] -= mi.mInfo.lList[i + 1].mlInfo.lo;

                    // SAVE TOTAL COST.
                    //#pragma omp atomic
                    arraysave_1.tcost += (arrayit_1.c__[i] * arrayit_1.x[i]); // Total Cost of Flows.
                }
            } //End of parallel region.
        }
        public static void SaveValues_DualPrice()
        {
            /*****************************************************************************
            SaveValues_DualPrice -	Calculate and save most recently dual price values.
                                    Dual prices are relative value.
            -------------------------------------------------------------------------------
            NOTE:
            - Added by ST on 12/29/2009.
            \*****************************************************************************/
            bool init = false;
            bool loop = false;
            long node = 0;
            long arc = 0;
            long pinit = 0;
            long pstart = 0;
            long pend = 0;
            long pmin_1 = 0;
            long pmin_2 = 0;
            long tprice = 0;

            long startn = 0;
            long endn = 0;
            long rc = 0;
            long x = 0;
            long cost = 0;
            long dfct = 0;

            /*  INITIALIZE BEFORE USED TO SAVE. */
            tprice = 0;
            arraysave_1.tprice = 0;
            init = true;
            pinit = input_1.large / 50;
            pmin_1 = 0;
            pmin_2 = 0;


            /*	INITIALIZE BEFORE USED TO SAVE (CONT.) */
            //#pragma omp parallel for //num_threads(NOMP)
            for (node = 1; node <= input_1.n; ++node)
            {
                //int ithread = omp_get_thread_num();

                arraysave_1.dprice[node - 1] = pinit;
            }

            /*		ASSIGN DUAL PRICE TO EACH NODE. 
                    NOTE: Do NOT make this section parallel region for now, it might mess something up.*/

            do
            {
                //int ithread = omp_get_thread_num();

                loop = false;
                for (node = 1; node <= input_1.n; ++node)
                {
                    arc = blks_1.fou[node - 1];
                    while (arc > 0)
                    {
                        startn = arraysave_1.startn[arc - 1];
                        endn = arraysave_1.endn[arc - 1];

                        pstart = arraysave_1.dprice[arraysave_1.startn[arc - 1] - 1];
                        pend = arraysave_1.dprice[arraysave_1.endn[arc - 1] - 1];

                        if (pstart == pinit && pend == pinit)
                        {
                            if (init)
                            {
                                init = false;
                                pstart = 0;
                                pend = pstart - (arraysave_1.cost[arc - 1] - arraysave_1.rc[arc - 1]);
                            }
                            else
                            {
                                if (arraysave_1.startn[arc - 1] != arraysave_1.endn[arc - 1])
                                {
                                    loop = true;
                                }
                            }
                        }
                        else if (pstart == pinit && pend != pinit)
                        {
                            pstart = pend + (arraysave_1.cost[arc - 1] - arraysave_1.rc[arc - 1]);
                        }
                        else if (pstart != pinit && pend == pinit)
                        {
                            pend = pstart - (arraysave_1.cost[arc - 1] - arraysave_1.rc[arc - 1]);
                        }

                        pmin_1 = ((pstart) < (pend)) ? (pstart) : (pend);
                        pmin_2 = ((pmin_1) < (pmin_2)) ? (pmin_1) : (pmin_2);

                        arraysave_1.dprice[arraysave_1.startn[arc - 1] - 1] = pstart;
                        arraysave_1.dprice[arraysave_1.endn[arc - 1] - 1] = pend;

                        arc = blks_1.nxtou[arc - 1];
                    }

                    arc = blks_1.fin[node - 1];
                    while (arc > 0)
                    {
                        startn = arraysave_1.startn[arc - 1];
                        endn = arraysave_1.endn[arc - 1];

                        pstart = arraysave_1.dprice[arraysave_1.startn[arc - 1] - 1];
                        pend = arraysave_1.dprice[arraysave_1.endn[arc - 1] - 1];

                        if (pstart == pinit && pend == pinit)
                        {
                            if (init)
                            {
                                init = false;
                                pstart = 0;
                                pend = pstart - (arraysave_1.cost[arc - 1] - arraysave_1.rc[arc - 1]);
                            }
                            else
                            {
                                if (arraysave_1.startn[arc - 1] != arraysave_1.endn[arc - 1])
                                {
                                    loop = true;
                                }
                            }

                        }
                        else if (pstart == pinit && pend != pinit)
                        {
                            pstart = pend + (arraysave_1.cost[arc - 1] - arraysave_1.rc[arc - 1]);
                        }
                        else if (pstart != pinit && pend == pinit)
                        {
                            pend = pstart - (arraysave_1.cost[arc - 1] - arraysave_1.rc[arc - 1]);
                        }

                        pmin_1 = ((pstart) < (pend)) ? (pstart) : (pend);
                        pmin_2 = ((pmin_1) < (pmin_2)) ? (pmin_1) : (pmin_2);

                        arraysave_1.dprice[arraysave_1.startn[arc - 1] - 1] = pstart;
                        arraysave_1.dprice[arraysave_1.endn[arc - 1] - 1] = pend;

                        arc = blks_1.nxtin[arc - 1];
                    }
                }
            } while (loop == true);

            /*		CHECK IF DUAL PRICES ARE ASSIGNED AT ALL NODES.
                    IF YES, ADJUST THE LOWEST VALUE TO ZERO.
                    CALCULATE AND SAVE TOTAL DUAL PRICE (PART I). */

            //#pragma omp parallel shared(pinit, pmin_2, tprice) //num_threads(NOMP) 
            {
                //#pragma omp for  private(dfct)
                for (node = 1; node <= input_1.n; ++node)
                {
                    //int ithread = omp_get_thread_num();

                    if (arraysave_1.dprice[node - 1] != pinit)
                    {
                        // Adjust the dual price based on the lowest value previously assigned.
                        arraysave_1.dprice[node - 1] -= pmin_2;

                        // Calculate and save total dual price (PART I).
                        //#pragma omp atomic
                        tprice += (-1 * arraysave_1.dfct[node - 1] * arraysave_1.dprice[node - 1]);
                    }
                    else
                    {
                        if (arraysave_1.dfct[node - 1] == 0)
                        {
                            // Set dual price to ZERO since we sure it will not affect total dual price.
                            arraysave_1.dprice[node - 1] = 0;
                        }
                        else
                        {
                            dfct = arraysave_1.dfct[node - 1];
                            Console.WriteLine("ERR: DUAL PRICE AT NODE {0} NOT ASSIGNED: dfct = {1}.", node.ToString(), dfct.ToString());
                        }
                    }
                }

                /*		CHECK CORRECTNESS OF DUAL PRICE AT EACH NODE.
                        IF CORRECT, CALCULATE AND SAVE TOTAL DUAL PRICE. */
                //#pragma omp for private(pstart, pend, cost, rc, x)
                for (arc = 1; arc <= input_1.na; ++arc)
                {
                    //int ithread = omp_get_thread_num();

                    pstart = arraysave_1.dprice[arraysave_1.startn[arc - 1] - 1];
                    pend = arraysave_1.dprice[arraysave_1.endn[arc - 1] - 1];

                    cost = arraysave_1.cost[arc - 1];
                    rc = arraysave_1.rc[arc - 1];
                    x = arraysave_1.x[arc - 1];

                    if (arraysave_1.rc[arc - 1] == arraysave_1.cost[arc - 1] - (pstart - pend))
                    {
                        // Calculate and save total dual price (PART II).
                        //#pragma omp atomic
                        tprice += (arraysave_1.rc[arc - 1] * arraysave_1.x[arc - 1]);
                    }
                    else
                    {
                        Console.WriteLine("ERR: INCORRECT DUAL PRICES AT ARC {0}:", arc.ToString());
                        Console.WriteLine("     x, rc, cost, pstart, pend = {0}, {1}, {2}, {3}, {4}.", x.ToString(), rc.ToString(), cost.ToString(), pstart.ToString(), pend.ToString());
                    }
                }
            } //End of parallel region.
            arraysave_1.tprice = tprice;
        }


        public static void ClearValues()
        {
            /*****************************************************************************
            ClearValues - Clear the static values in arraysave to zero.
            -------------------------------------------------------------------------------
            \*****************************************************************************/
            int arc;
            int node;

            arraysave_1.numlinks = 0;
            arraysave_1.tcost = 0;
            arraysave_1.tprice = 0;

            //#pragma omp parallel //num_threads(NOMP)
            {
                //#pragma omp for nowait
                for (node = 0; node < DefineConstants.MAXNODESRELAX; node++)
                {
                    //int ithread = omp_get_thread_num();

                    arraysave_1.dfct[node] = 0;
                    arraysave_1.dprice[node] = 0;
                }

                //#pragma omp for
                for (arc = 0; arc < DefineConstants.MAXLINKSRELAX; arc++)
                {
                    //int ithread = omp_get_thread_num();

                    arraysave_1.startn[arc] = 0;
                    arraysave_1.endn[arc] = 0;
                    arraysave_1.cost[arc] = 0;
                    arraysave_1.rc[arc] = 0;
                    arraysave_1.u[arc] = 0;
                    arraysave_1.x[arc] = 0;
                }
            } //End of parallel region.
        }

        public static void DisplayInfo_1()
        {
            /*  DISPLAY RELAX4 STATISTICS */
            if (input_1.crash == 1)
            {
                Console.WriteLine("NUMBER OF AUCTION SCALING PHASES = {0} WITH EPSILON = {1}.", auctionsave_1.pass.ToString(), auctionsave_1.eps.ToString());
                Console.WriteLine("NUMBER OF AUCTION/SHORTEST PATH ITERATIONS = {0}.", output_1.nsp.ToString());
            }
            else if (input_1.repeat == true)
            {
                Console.WriteLine("VALID INCREMENT, SOME INITIALIZATION WAS SKIPPED.");
            }
            Console.WriteLine("NUMBER OF ITERATIONS = {0}.", output_1.iter.ToString());
            Console.WriteLine("NUMBER OF MULTINODE ITERATIONS = {0}.", output_1.nmultinode.ToString());
            Console.WriteLine("NUMBER OF MULTINODE ASCENT STEPS = {0}.", output_1.num_ascnt__.ToString());
            Console.WriteLine("NUMBER OF REGULAR AUGMENTATIONS = {0}.", output_1.num_augm__.ToString());

            /*  DISPLAY COST OF FLOWS */
            long diff = arraysave_1.tcost - arraysave_1.tprice;
            Console.WriteLine("TOTAL PRIMAL COST = {0}.", arraysave_1.tcost.ToString());
            Console.WriteLine("TOTAL DUAL PRICE  = {0}.", arraysave_1.tprice.ToString());
            Console.WriteLine("DIFFERENCE = {0}.", diff.ToString());
        }
        public static void DisplayInfo_2()
        {
            Console.WriteLine("TOTAL SOLUTION TIME = {0} Milli-SEC.", output_1.time2.ToString());
            Console.WriteLine("TIME IN INITIALIZATION = {0} Milli-SEC.", output_1.time1.ToString());
            Console.WriteLine("***********************************");
        }
        public static void RetranValues(Model mi)
        {
            /*****************************************************************************
            RetranValues - RETRANSFORM VARIABLES x(), u() TO ACCOUNT FOR LOWER BOUND > 0.
            -------------------------------------------------------------------------------
            NOTE:
            - This rountine was extracted from 'relaxcallfortraninternal' by ST on 12/29/2009.
            \*****************************************************************************/
            int i;

            mi.mInfo.ada_feasible = blks_1.feasbl;

            //#pragma omp parallel for //num_threads(NOMP)
            for (i = 1; i <= input_1.na; ++i)
            {
                //int ithread = omp_get_thread_num();

                arrayit_1.x[i - 1] += mi.mInfo.lList[i].mlInfo.lo;
                mi.mInfo.lList[i].mlInfo.flow = arrayit_1.x[i - 1];
            }
        }
        public static void Horque()
        {
            long i;
            for (i = 1; i <= input_1.na; ++i)
            {
                Console.WriteLine("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}", i.ToString(), arrayit_1.startn[i - 1].ToString(), arrayit_1.endn[i - 1].ToString(), arrayit_1.x[i - 1].ToString(), arrayit_1.u[i - 1].ToString(), arrayit_1.c__[i - 1].ToString(), arrayit_1.rc[i - 1].ToString(), blks_1.nxtou[i - 1].ToString(), blks_1.nxtin[i - 1].ToString());
            }
            for (i = 1; i <= input_1.n; ++i)
            {
                Console.WriteLine("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}", i.ToString(), arrayit_1.dfct[i - 1].ToString(), blks_1.label[i - 1].ToString(), blks_1.fou[i - 1].ToString(), blks_1.fin[i - 1].ToString());
            }
        }


        public static void ReduceAuctionPrice()
        {
            long minp;
            long del;
            int node;

            /*	FIND THE MINIMAL PRICE */
            minp = input_1.large;
            for (node = 1; node <= input_1.n; ++node)
            {
                minp = ((minp) < (blks_1.label[node - 1])) ? (minp) : (blks_1.label[node - 1]);
            }

            /*	REDUCE ALL PRICES TO REDUCE DANGER OF OVERFLOW */
            del = minp + (input_1.large / 10);
            if (del < 0)
            {
                del = 0;
            }
            for (node = 1; node <= input_1.n; ++node)
            {
                blks_1.label[node - 1] -= del;
            }
        }
        public static void SaveAuctionValues(long pass, long eps, bool saveall)
        {
            /*****************************************************************************************
            SaveAuctionValues - Use auctionsave values to store results of each auction scaling phase.
            -------------------------------------------------------------------------------------------
            Note that we need to keep track of the auction result since as epsilon decreasing,
            it could be possible to have infeasible solution from auction algorithm.
            Then, if auction is failed at a scaling phase, the result of the previous scaling phase
            could be further used in relax4 sub by using RecallAuctionValues routine.
            \*****************************************************************************************/
            long node;
            long arc;
            long start;
            long end;
            long red_cost__;
            long resid;
            long flow;

            auctionsave_1.pass = pass;
            auctionsave_1.eps = eps;

            if (!saveall)
            {
                return;
            }

            //#pragma omp parallel if(0)
            {
                //#pragma omp for nowait
                for (arc = 1; arc <= input_1.na; ++arc)
                {
                    auctionsave_1.x[arc - 1] = arrayit_1.x[arc - 1];
                    auctionsave_1.u[arc - 1] = arrayit_1.u[arc - 1];
                }

                //#pragma omp for
                for (node = 1; node <= input_1.n; ++node)
                {
                    auctionsave_1.dfct[node - 1] = arrayit_1.dfct[node - 1];
                    auctionsave_1.label[node - 1] = blks_1.label[node - 1];
                    auctionsave_1.prdcsr[node - 1] = blks_1.prdcsr[node - 1];
                }

                //#pragma omp for private(start, end, red_cost__, resid, flow)
                for (arc = 1; arc <= input_1.na; ++arc)
                {
                    start = arrayit_1.startn[arc - 1];
                    end = arrayit_1.endn[arc - 1];
                    red_cost__ = arrayit_1.rc[arc - 1] + blks_1.label[end - 1] - blks_1.label[start - 1];

                    auctionsave_1.startn[arc - 1] = start;
                    auctionsave_1.endn[arc - 1] = end;
                    auctionsave_1.rc[arc - 1] = red_cost__;

                    if (red_cost__ < 0)
                    {
                        resid = arrayit_1.u[arc - 1];
                        if (resid > 0)
                        {
                            auctionsave_1.u[arc - 1] = 0;
                            //#pragma omp atomic
                            auctionsave_1.x[arc - 1] += resid;
                            //#pragma omp atomic
                            auctionsave_1.dfct[start - 1] += resid;
                            //#pragma omp atomic
                            auctionsave_1.dfct[end - 1] -= resid;
                        }
                    }
                    else if (red_cost__ > 0)
                    {
                        flow = arrayit_1.x[arc - 1];
                        if (flow > 0)
                        {
                            auctionsave_1.x[arc - 1] = 0;
                            //#pragma omp atomic
                            auctionsave_1.u[arc - 1] += flow;
                            //#pragma omp atomic
                            auctionsave_1.dfct[start - 1] -= flow;
                            //#pragma omp atomic
                            auctionsave_1.dfct[end - 1] += flow;
                        }
                    }
                }
            } // End of parallel region.
        }
        public static void RecallAuctionValues()
        {
            /*****************************************************************************************
            RecallAuctionValues - Use auctionsave values to prepare relax4 run if auction failed.
            -------------------------------------------------------------------------------------------
            Note This routine is used together with SaveAuctionValues.
            \*****************************************************************************************/
            long arc;
            long node;

            //#pragma omp parallel
            {
                //#pragma omp for nowait
                for (arc = 1; arc <= input_1.na; ++arc)
                {
                    arrayit_1.startn[arc - 1] = auctionsave_1.startn[arc - 1];
                    arrayit_1.endn[arc - 1] = auctionsave_1.endn[arc - 1];
                    arrayit_1.x[arc - 1] = auctionsave_1.x[arc - 1];
                    arrayit_1.u[arc - 1] = auctionsave_1.u[arc - 1];
                    arrayit_1.rc[arc - 1] = auctionsave_1.rc[arc - 1];
                }

                //#pragma omp for
                for (node = 1; node <= input_1.n; ++node)
                {
                    arrayit_1.dfct[node - 1] = auctionsave_1.dfct[node - 1];

                    blks_1.label[node - 1] = auctionsave_1.label[node - 1];
                    blks_1.prdcsr[node - 1] = auctionsave_1.prdcsr[node - 1];
                }
            } // End of parallel region.
        }
        //private static long marco_node;
        //private static long flag1;
        //private static long flag2;
        //private static long flag3;
        //private static long i__;
        //private static double marco_tcost = new double();
        //private static long marco_danger_thresh__;
        //private static long marco_arc;

#if ORIGINAL
	/* Main program */	 public static int marco()
	 {
	/*  SAMPLE CALLING PROGRAM FOR RELAX-IV */

	/* --------------------------------------------------------------- */

	/*  PURPOSE - THIS PROGRAM READS IN DATA FOR A LINEAR COST */
	/*     ORDINARY NETWORK FLOW PROBLEM FROM THE FILE `RELAX4.INP', */
	/*     CALLS THE ROUTINE INIDAT TO CONSTRUCT LINKED LIST FOR THE PROBLEM, */
	/*     AND THEN CALLS THE ROUTINE RELAX4 TO SOLVE THE PROBLEM. */

	/* --------------------------------------------------------------- */

		/* System generated locals */
		long i__1;
		long i__2;

		long i;


		/* Local variables */
	//C++ TO C# CONVERTER NOTE: This static local variable declaration (not allowed in C#) has been moved just prior to the method:
	//	static long node, flag1, flag2, flag3, i__;
	//C++ TO C# CONVERTER NOTE: This static local variable declaration (not allowed in C#) has been moved just prior to the method:
	//	static double tcost;
	//C++ TO C# CONVERTER NOTE: This static local variable declaration (not allowed in C#) has been moved just prior to the method:
	//	static long danger_thresh__;
	//C++ TO C# CONVERTER NOTE: This static local variable declaration (not allowed in C#) has been moved just prior to the method:
	//	static long arc;
		FILE fp13;


	/*     MAXNN = DIMENSION OF NODE-LENGTH ARRAYS */
	/*     MAXNA = DIMENSION OF ARC-LENGTH ARRAYS */


	/*  INPUT PARAMETERS */

	/*     N         = NUMBER OF NODES */
	/*     NA        = NUMBER OF ARCS */
	/*     LARGE     = A VERY LARGE INTEGER TO REPRESENT INFINITY */
	/*     STARTN(J) = STARTING NODE FOR ARC J,           J = 1,...,NA */
	/*     ENDN(J)   = ENDING NODE FOR ARC J,             J = 1,...,NA */
	/*     C(J)      = COST OF ARC J,                     J = 1,...,NA */

	/*  UPDATED PARAMETERS */

	/*     U(J)      = CAPACITY OF ARC J ON INPUT AND RESIDUAL CAPACITY */
	/*                 ON OUTPUT,                         J = 1,...,NA */
	/*     B(I)      = DEMAND AT NODE I ON INPUT AND ZERO ON OUTPUT, */
	/*                                                    I = 1,...,N */


	/*  OUTPUT PARAMETERS */

	/*     X(J)      = FLOW ON ARC J,                     J = 1,...,NA */
	/*     RC(J)     = REDUCED COST OF ARC J,             J = 1,...,NA */
	/*     NMULTINODE = NUMBER OF MULTINODE RELAXATION ITERATIONS IN RELAX4 */
	/*     ITER       = NUMBER OF RELAXATION ITERATIONS IN RELAX4 */
	/*     NUM_AUGM   = NUMBER OF FLOW AUGMENTATION STEPS IN RELAX4 */
	/*     NUM_ASCNT  = NUMBER OF MULTINODE ASCENT STEPS IN RELAX4 */
	/*     NSP       = NUMBER OF AUCTION/SHORTEST PATH ITERATIONS */
	/*     TCOST     = COST OF FLOW */


	/* ^^                                     B                     ^^ */
	/* ^^          TEMPIN I1 P,TEMPOU PRICE I2,I3,I4,I5,I6,I7        ^^ */
	/* ^^                      MARK   TFSTOU  TNXTOU  TFSTIN TNXTIN ^^ */
	/* ^^                  I14      I15        I16     I17          ^^ */

	/*  WORKING PARAMETERS */


	/*  OPTIONAL WORKING PARAMETERS (FOR SENSITIVITY ANALYSIS ONLY) */


	/*  DECLARE TIMING VARIABLES FOR UNIX SYSTEM */


	/* --------------------------------------------------------------- */

	/*     READ PROBLEM DATA FROM FILE RELAX4.INP */

		   Console.Write("READ PROBLEM DATA FROM RELAX4.INP\n");
		   fp13 = fopen("RELAX4.INP", "r");
	/*     PRINT*,'READ PROBLEM DATA FROM RELAX4.INP' */
	/*     OPEN(13,FILE='RELAX4.INP',STATUS='OLD') */
	/*     REWIND(13) */

	/*     READ NUMBER OF NODES AND ARCS */

	/*     READ(13,1000) N,NA */
		   fscanf(fp13, "%lld%lld", input_1.n, input_1.na);

	/*     READ START NODE, END NODE, COST, AND CAPACITY OF EACH ARC */

	/*     DO 20 I=1,NA */
	/*       READ(13,1000) STARTN(I),ENDN(I),C(I),U(I) */
	/*       WRITE(*,1000) STARTN(I),ENDN(I),C(I),U(I) */
	/* L20: */
		   for (i = 0; i < input_1.na; i++)
		   {
			 fscanf(fp13, "%lld%lld%lld%lld", arrayit_1.startn[i], arrayit_1.endn[i], arrayit_1.c__[i], arrayit_1.u[i]);
			 Console.Write("{0,5:D} {1,5:D} {2,5:D} {3,5:D}\n", arrayit_1.startn[i], arrayit_1.endn[i], arrayit_1.c__[i], arrayit_1.u[i]);
		   }

	/*     READ SUPPLY OF EACH NODE; CONVERT IT TO DEMAND */

	/*     DO 30 I=1,N */
	/*       READ(13,1000) DFCT(I) */
	/*       WRITE(*,1000) DFCT(I) */
	/*       DFCT(I)=-DFCT(I) */
	/* L30: */
		   for (i = 0; i < input_1.n; i++)
		   {
			 fscanf(fp13, "%lld", arrayit_1.dfct[i]);
			 Console.Write("{0:D}\n", arrayit_1.dfct[i]);
			 arrayit_1.dfct[i] = -arrayit_1.dfct[i];
		   }

	/* L1000: */
	/*     REWIND(13) */
	/*     CLOSE(13) */

	/*     PRINT*,'END OF READING' */
	/*     PRINT*,'NUMBER OF NODES =',N,', NUMBER OF ARCS =',NA */

		   Console.Write("END OF READING\n");
		   Console.Write("NUMBER OF NODES ={0:D} NUMBER OF ARCS ={1:D}\n", input_1.n, input_1.na);

	/*     SET LARGE TO A LARGE INTEGER FOR YOUR MACHINE */

		input_1.large = 500000000;
		marco_danger_thresh__ = input_1.large / 10;

	/*     CHECK DATA IS WITHIN RANGE OF MACHINE */

		flag1 = 0;
		flag2 = 0;
		flag3 = 0;
		i__1 = input_1.na;
		for (i__ = 1; i__ <= i__1; ++i__)
		{
		if ((i__2 = arrayit_1.c__[i__ - 1], Math.Abs(i__2)) > input_1.large)
		{
			flag1 = 1;
		}
		if (arrayit_1.u[i__ - 1] > input_1.large)
		{
			flag2 = 1;
		}
		if ((i__2 = arrayit_1.c__[i__ - 1], Math.Abs(i__2)) > marco_danger_thresh__)
		{
			flag3 = 1;
		}
	/* L40: */
		}
		if (flag1 == 1)
		{
			ModsimModel.Model.FireOnErrorGlobal("SOME COSTS EXCEED THE ALLOWABLE RANGE\nPROGRAM CANNOT RUN\n");
			return 1;
		}
		if (flag2 == 1)
		{
			ModsimModel.Model.FireOnErrorGlobal("SOME ARC CAPACITIES EXCEED THE ALLOWABLE RANGE\nPROGRAM CANNOT RUN\n");
			return 1;
		}
		if (flag3 == 1)
		{
			Console.Write("SOME COSTS ARE DANGEROUSLY LARGE\n");
			Console.Write("PROGRAM MAY NOT RUN CORRECTLY\n");
	/*        PRINT*,'SOME COSTS ARE DANGEROUSLY LARGE' */
	/*        PRINT*,'PROGRAM MAY NOT RUN CORRECTLY' */
		}

	/* --------------------------------------------------------------- */

	/*     CONSTRUCT LINKED LISTS FOR THE PROBLEM */

	/*     PRINT*,'CONSTRUCT LINKED LISTS FOR THE PROBLEM' */
		   Console.Write("CONSTRUCT LINKED LISTS FOR THE PROBLEM\n");

		GlobalMembersRelax4.inidat_();

	/* --------------------------------------------------------------- */

	/*     INITIALIZE DUAL PRICES */
	/*     (DEFAULT: ALL DUAL PRICES = 0, SO REDUCED COST IS SET */
	/*     EQUAL TO COST) */

		i__1 = input_1.na;
		for (i__ = 1; i__ <= i__1; ++i__)
		{
	/* L60: */
		arrayit_1.rc[i__ - 1] = arrayit_1.c__[i__ - 1];
		}

	/*     SPECIFY THAT WE ARE SOLVING THE PROBLEM FROM SCRATCH */

		input_1.repeat = FALSE_;

	/* --------------------------------------------------------------- */

	/*     SET CRASH EQUAL TO 1 TO ACTIVATE AN AUCTION/SHORTEST PATH SUBROUTINE FOR */
	/*     GETTING THE INITIAL PRICE-FLOW PAIR. THIS IS RECOMMENDED FOR DIFFICULT */
	/*     PROBLEMS WHERE THE DEFAULT INITIALIZATION YIELDS */
	/*     LONG SOLUTION TIMES. */

	/*     PRINT*,'ENTER THE INITIALIZATION DESIRED' */
	/*     PRINT*,' <0> FOR THE DEFAULT INITIALIZATION' */
	/*     PRINT*,' <1> FOR AUCTION INITIALIZATION' */
	/*     READ*,CRASH */
		   Console.Write("ENTER THE INITIALIZATION DESIRED\n");
		   Console.Write(" <0> FOR THE DEFAULT INITIALIZATION\n");
		   Console.Write(" <1> FOR AUCTION INITIALIZATION\n");
		   scanf("%lld", input_1.crash);

	/*     CALL RELAX4 TO SOLVE THE PROBLEM */

	/* L75: */
	/*     PRINT*,'CALLING RELAX4 TO SOLVE THE PROBLEM' */
	/*     PRINT*,'***********************************' */
		   Console.Write("CALLING RELAX4 TO SOLVE THE PROBLEM\n");
		   Console.Write("***********************************\n");

	/*     INITIALIZE SYSTEM TIMER */

	/*     TIME0 = LONG(362)/60.0 */
	/*     TIME0 = SECNDS(0.0) */

		   GlobalMembersRelax4.relax4(ModsimModel.Model);


	/* --------------------------------------------------------------- */

	/*     CHECK CORRECTNESS OF OUTPUT PARAMETERS */

		i__1 = input_1.n;
		for (marco_node = 1; marco_node <= i__1; ++marco_node)
		{
		if (arrayit_1.dfct[marco_node - 1] != 0)
		{
			   Console.Write("NONZERO SURPLUS AT NODE {0:D}\n", marco_node);
	/*         PRINT*,'NONZERO SURPLUS AT NODE ',NODE */
		}
	/* L80: */
		}
		i__1 = input_1.na;
		for (marco_arc = 1; marco_arc <= i__1; ++marco_arc)
		{
		if (arrayit_1.x[marco_arc - 1] > 0)
		{
			if (arrayit_1.rc[marco_arc - 1] > 0)
			{
				 Console.Write("COMPLEMENTARY SLACKNESS VIOLATED AT ARC {0:D}\n", marco_arc);
	/*           PRINT*,'COMPLEMENTARY SLACKNESS VIOLATED AT ARC ',ARC */
			}
		}
		if (arrayit_1.u[marco_arc - 1] > 0)
		{
			if (arrayit_1.rc[marco_arc - 1] < 0)
			{
				 Console.Write("COMPLEMENTARY SLACKNESS VIOLATED AT ARC {0:D}\n", marco_arc);
	/*           PRINT*,'COMPLEMENTARY SLACKNESS VIOLATED AT ARC ',ARC */
			}
		}
	/* L90: */
		}

	/*     COMPUTE AND DISPLAY COST OF FLOWS (IN DOUBLE PRECISION) */

		marco_tcost = (double)0.0;
		i__1 = input_1.na;
		for (i__ = 1; i__ <= i__1; ++i__)
		{
		marco_tcost += (double)(arrayit_1.x[i__ - 1] * arrayit_1.c__[i__ - 1]);
	/* L100: */
		}
	/*     PRINT*,'OPTIMAL COST = ',TCOST */
		   Console.Write("OPTIMAL COST = {0:f}\n", marco_tcost);

	/*     DISPLAY RELAX4 STATISTICS */

		if (input_1.crash == 1)
		{
			 Console.Write("NUMBER OF AUCTION/SHORTEST PATH ITERATIONS ={0:D}\n", output_1.nsp);
	/*       PRINT*,'NUMBER OF AUCTION/SHORTEST PATH ITERATIONS =',NSP */
		}
		Console.Write("NUMBER OF ITERATIONS = {0:D}\n", output_1.iter);
		Console.Write("NUMBER OF MULTINODE ITERATIONS = {0:D}\n", output_1.nmultinode);
		Console.Write("NUMBER OF MULTINODE ASCENT STEPS = {0:D}\n", output_1.num_ascnt__);
		Console.Write("NUMBER OF REGULAR AUGMENTATIONS = {0:D}\n", output_1.num_augm__);
		Console.Write("***********************************\n");
	/*     PRINT*,'NUMBER OF ITERATIONS = ',ITER */
	/*     PRINT*,'NUMBER OF MULTINODE ITERATIONS = ',NMULTINODE */
	/*     PRINT*,'NUMBER OF MULTINODE ASCENT STEPS = ',NUM_ASCNT */
	/*     PRINT*,'NUMBER OF REGULAR AUGMENTATIONS = ',NUM_AUGM */
	/*     PRINT*,'***********************************' */

	/*     TO ACTIVATE SENSITIVITY ANALYSIS, INSERT THE FOLLOWING */
	/*     THREE LINES HERE. */

	return 0;
	 } // MAIN__
//C++ TO C# CONVERTER NOTE: This was formerly a static local variable declaration (not allowed in C#):
 private static long relax4_i__1;
 long i__2;
 long i__3;

	/* Subroutine */	 public static int relax4(Model mi)
	 {
		/* System generated locals */
	//C++ TO C# CONVERTER NOTE: This static local variable declaration (not allowed in C#) has been moved just prior to the method:
	//	static long i__1, i__2, i__3;


		/* Local variables */
		long node_def__;
		long narc;
		long node;
		long delx;
		logical1 quit = new logical1();
		long prevnode;
		long node2;
		long i__;
		long j;
		long t;
		long indef;
		long capin;
		long nscan;
		logical1 posit = new logical1();
		long t1;
		long t2;
		long numnz;
		long lastqueue;
		long numpasses;
		long numnz_new__;
		long ib;
		long nb;
		long dm;
		long dp;
		long nlabel;
		long defcit;
		long dx;
		long tp;
		long ts;
		long maxcap;
		long delprc;
		long scapin;
		long augnod;
		long tmparc;
		long scapou;
		long capout;
		long passes;
		long rdcost;
		logical1 switch__ = new logical1();
		long nxtarc;
		long prvarc;
		long nxtbrk;
		long num_passes__;
		long arc;
		logical1 pchange = new logical1();
		long trc;
		long naugnod;
		long nxtnode;


	/* --------------------------------------------------------------- */

	/*                 RELAX-IV  (VERSION OF OCTOBER 1994) */

	/*  RELEASE NOTE - THIS VERSION OF RELAXATION CODE HAS OPTION FOR */
	/*     A SPECIAL CRASH PROCEDURE FOR */
	/*     THE INITIAL PRICE-FLOW PAIR. THIS IS RECOMMENDED FOR DIFFICULT */
	/*     PROBLEMS WHERE THE DEFAULT INITIALIZATION */
	/*     RESULTS IN LONG RUNNING TIMES. */
	/*     CRASH =1 CORRESPONDS TO AN AUCTION/SHORTEST PATH METHOD */

	/*     THESE INITIALIZATIONS ARE RECOMMENDED IN THE ABSENCE OF ANY */
	/*     PRIOR INFORMATION ON A FAVORABLE INITIAL FLOW-PRICE VECTOR PAIR */
	/*     THAT SATISFIES COMPLEMENTARY SLACKNESS */

	/*     THE RELAXATION PORTION OF THE CODE DIFFERS FROM THE CODE RELAXT-III */
	/*     AND OTHER EARLIER RELAXATION CODES IN THAT IT MAINTAINS */
	/*     THE SET OF NODES WITH NONZERO DEFICIT IN A FIFO QUEUE. */
	/*     LIKE ITS PREDECESSOR RELAXT-III, THIS CODE MAINTAINS A LINKED LIST */
	/*     OF BALANCED (I.E., OF ZERO REDUCED COST) ARCS SO TO REDUCE */
	/*     THE WORK IN LABELING AND SCANNING. */
	/*     UNLIKE RELAXT-III, IT DOES NOT USE SELECTIVELY */
	/*     SHORTEST PATH ITERATIONS FOR INITIALIZATION. */

	/* --------------------------------------------------------------- */

	/*  PURPOSE - THIS ROUTINE IMPLEMENTS THE RELAXATION METHOD */
	/*     OF BERTSEKAS AND TSENG (SEE [1], [2]) FOR LINEAR */
	/*     COST ORDINARY NETWORK FLOW PROBLEMS. */

	/*  [1] BERTSEKAS, D. P., "A UNIFIED FRAMEWORK FOR PRIMAL-DUAL METHODS ..." */
	/*      MATHEMATICAL PROGRAMMING, VOL. 32, 1985, PP. 125-145. */
	/*  [2] BERTSEKAS, D. P., AND TSENG, P., "RELAXATION METHODS FOR */
	/*      MINIMUM COST ..." OPERATIONS RESEARCH, VOL. 26, 1988, PP. 93-114. */

	/*     THE RELAXATION METHOD IS ALSO DESCRIBED IN THE BOOKS: */

	/*  [3] BERTSEKAS, D. P., "LINEAR NETWORK OPTIMIZATION: ALGORITHMS AND CODES" */
	/*      MIT PRESS, 1991. */
	/*  [4] BERTSEKAS, D. P. AND TSITSIKLIS, J. N., "PARALLEL AND DISTRIBUTED */
	/*      COMPUTATION: NUMERICAL METHODS", PRENTICE-HALL, 1989. */



	/* --------------------------------------------------------------- */

	/*  SOURCE -  THIS CODE WAS WRITTEN BY DIMITRI P. BERTSEKAS */
	/*     AND PAUL TSENG, WITH A CONTRIBUTION BY JONATHAN ECKSTEIN */
	/*     IN THE PHASE II INITIALIZATION.  THE ROUTINE AUCTION WAS WRITTEN */
	/*     BY DIMITRI P. BERTSEKAS AND IS BASED ON THE METHOD DESCRIBED IN */
	/*     THE PAPER: */

	/*  [5] BERTSEKAS, D. P., "AN AUCTION/SEQUENTIAL SHORTEST PATH ALGORITHM */
	/*      FOR THE MINIMUM COST FLOW PROBLEM", LIDS REPORT P-2146, MIT, NOV. 1992. */

	/*     FOR INQUIRIES ABOUT THE CODE, PLEASE CONTACT: */

	/*     DIMITRI P. BERTSEKAS */
	/*     LABORATORY FOR INFORMATION AND DECISION SYSTEMS */
	/*     MASSACHUSETTS INSTITUTE OF TECHNOLOGY */
	/*     CAMBRIDGE, MA 02139 */
	/*     (617) 253-7267, DIMITRIB@MIT.EDU */

	/* --------------------------------------------------------------- */

	/*  USER GUIDELINES - */

	/*     THIS ROUTINE IS IN THE PUBLIC DOMAIN TO BE USED ONLY FOR RESEARCH */
	/*     PURPOSES.  IT CANNOT BE USED AS PART OF A COMMERCIAL PRODUCT, OR */
	/*     TO SATISFY IN ANY PART COMMERCIAL DELIVERY REQUIREMENTS TO */
	/*     GOVERNMENT OR INDUSTRY, WITHOUT PRIOR AGREEMENT WITH THE AUTHORS. */
	/*     USERS ARE REQUESTED TO ACKNOWLEDGE THE AUTHORSHIP OF THE CODE, */
	/*     AND THE RELAXATION METHOD.  THEY SHOULD ALSO REGISTER WITH THE */
	/*     AUTHORS TO RECEIVE UPDATES AND SUBSEQUENT RELEASES. */

	/*     NO MODIFICATION SHOULD BE MADE TO THIS CODE OTHER */
	/*     THAN THE MINIMAL NECESSARY */
	/*     TO MAKE IT COMPATIBLE WITH THE FORTRAN COMPILERS OF SPECIFIC */
	/*     MACHINES.  WHEN REPORTING COMPUTATIONAL RESULTS PLEASE BE SURE */
	/*     TO DESCRIBE THE MEMORY LIMITATIONS OF YOUR MACHINE. GENERALLY */
	/*     RELAX4 REQUIRES MORE MEMORY THAN PRIMAL SIMPLEX CODES AND MAY */
	/*     BE PENALIZED SEVERELY BY LIMITED MACHINE MEMORY. */

	/* --------------------------------------------------------------- */

	/*     MAXNN = DIMENSION OF NODE-LENGTH ARRAYS */
	/*     MAXNA = DIMENSION OF ARC-LENGTH ARRAYS */


	/*  INPUT PARAMETERS (SEE NOTES 1, 2, 4) */

	/*     N         = NUMBER OF NODES */
	/*     NA        = NUMBER OF ARCS */
	/*     LARGE     = A VERY LARGE INTEGER TO REPRESENT INFINITY */
	/*                 (SEE NOTE 3) */
	/*     REPEAT    = .TRUE. IF INITIALIZATION IS TO BE SKIPPED */
	/*                 (.FALSE. OTHERWISE) */
	/*     CRASH     = 0 IF DEFAULT INITIALIZATION IS USED */
	/*                 1 IF AUCTION INITIALIZATION IS USED */
	/*     STARTN(J) = STARTING NODE FOR ARC J,           J = 1,...,NA */
	/*     ENDN(J)   = ENDING NODE FOR ARC J,             J = 1,...,NA */
	/*     FOU(I)    = FIRST ARC OUT OF NODE I,          I = 1,...,N */
	/*     NXTOU(J)  = NEXT ARC OUT OF THE STARTING NODE OF ARC J, */
	/*                                                    J = 1,...,NA */
	/*     FIN(I)    = FIRST ARC INTO NODE I,             I = 1,...,N */
	/*     NXTIN(J)  = NEXT ARC INTO THE ENDING NODE OF ARC J, */
	/*                                                    J = 1,...,NA */

	/* ^^                                     B                     ^^ */
	/* ^^          TEMPIN I1 P,TEMPOU PRICE I2,I3,I4,I5,I6,I7        ^^ */
	/* ^^                      MARK   TFSTOU  TNXTOU  TFSTIN TNXTIN ^^ */
	/* ^^                  I14      I15        I16     I17          ^^ */

	/*  UPDATED PARAMETERS (SEE NOTES 1, 3, 4) */

	/*     RC(J)     = REDUCED COST OF ARC J,             J = 1,...,NA */
	/*     U(J)      = CAPACITY OF ARC J ON INPUT */
	/*                 AND (CAPACITY OF ARC J) - X(J) ON OUTPUT, */
	/*                                                    J = 1,...,NA */
	/*     DFCT(I)   = DEMAND AT NODE I ON INPUT */
	/*                 AND ZERO ON OUTPUT,                I = 1,...,N */


	/*  OUTPUT PARAMETERS (SEE NOTES 1, 3, 4) */

	/*     X(J)      = FLOW ON ARC J,                     J = 1,...,NA */
	/*     NMULTINODE = NUMBER OF MULTINODE RELAXATION ITERATIONS IN RELAX4 */
	/*     ITER       = NUMBER OF RELAXATION ITERATIONS IN RELAX4 */
	/*     NUM_AUGM   = NUMBER OF FLOW AUGMENTATION STEPS IN RELAX4 */
	/*     NUM_ASCNT  = NUMBER OF MULTINODE ASCENT STEPS IN RELAX4 */
	/*     NSP        = NUMBER OF AUCTION/SHORTEST PATH ITERATIONS */


	/*  WORKING PARAMETERS (SEE NOTES 1, 4, 5) */


	/*  TIMING PARAMETERS */


	/*  NOTE 1 - */
	/*     TO RUN IN LIMITED MEMORY SYSTEMS, DECLARE THE ARRAYS */
	/*     STARTN, ENDN, NXTIN, NXTOU, FIN, FOU, LABEL, */
	/*     PRDCSR, SAVE, TFSTOU, TNXTOU, TFSTIN, TNXTIN, */
	/*     DDPOS,DDNEG,NXTQUEUE AS INTEGER*2 INSTEAD. */

	/*  NOTE 2 - */
	/*     THIS ROUTINE MAKES NO EFFORT TO INITIALIZE WITH A FAVORABLE X */
	/*     FROM AMONGST THOSE FLOW VECTORS THAT SATISFY COMPLEMENTARY SLACKNESS */
	/*     WITH THE INITIAL REDUCED COST VECTOR RC. */
	/*     IF A FAVORABLE X IS KNOWN, THEN IT CAN BE PASSED, TOGETHER */
	/*     WITH THE CORRESPONDING ARRAYS U AND DFCT, TO THIS ROUTINE */
	/*     DIRECTLY.  THIS, HOWEVER, REQUIRES THAT THE CAPACITY */
	/*     TIGHTENING PORTION AND THE FLOW INITIALIZATION PORTION */
	/*     OF THIS ROUTINE (UP TO LINE LABELED 90) BE SKIPPED. */

	/*  NOTE 3 - */
	/*     ALL PROBLEM DATA SHOULD BE LESS THAN LARGE IN MAGNITUDE, */
	/*     AND LARGE SHOULD BE LESS THAN, SAY, 1/4 THE LARGEST INTEGER*4 */
	/*     OF THE MACHINE USED.  THIS WILL GUARD PRIMARILY AGAINST */
	/*     OVERFLOW IN UNCAPACITATED PROBLEMS WHERE THE ARC CAPACITIES */
	/*     ARE TAKEN FINITE BUT VERY LARGE.  NOTE, HOWEVER, THAT AS IN */
	/*     ALL CODES OPERATING WITH INTEGERS, OVERFLOW MAY OCCUR IF SOME */
	/*     OF THE PROBLEM DATA TAKES VERY LARGE VALUES. */

	/*  NOTE 4 - */
	/*     EACH COMMON BLOCK CONTAINS JUST ONE ARRAY, SO THE ARRAYS IN RELAX4 */
	/*     CAN BE DIMENSIONED TO 1 AND TAKE THEIR DIMENSION FROM THE */
	/*     MAIN CALLING ROUTINE.  WITH THIS TRICK, RELAX4 NEED NOT BE RECOMPILED */
	/*     IF THE ARRAY DIMENSIONS IN THE CALLING ROUTINE CHANGE. */
	/*     IF YOUR FORTRAN COMPILER DOES NOT SUPPORT THIS FEATURE, THEN */
	/*     CHANGE THE DIMENSION OF ALL THE ARRAYS TO BE THE SAME AS THE ONES */
	/*     DECLARED IN THE MAIN CALLING PROGRAM. */

	/*  NOTE 5 - */
	/*       Note :  EQUIVALENCE(DDPOS,TFSTOU),(DDNEG,TFSTIN)  : */
	/*     DDPOS AND DDNEG ARE ARRAYS THAT GIVE THE DIRECTIONAL DERIVATIVES FOR */
	/*     ALL POSITIVE AND NEGATIVE SINGLE-NODE PRICE CHANGES.  THESE ARE USED */
	/*     ONLY IN PHASE II OF THE INITIALIZATION PROCEDURE, BEFORE THE */
	/*     LINKED LIST OF BALANCED ARCS COMES TO PLAY.  THEREFORE, TO REDUCE */
	/*     STORAGE, THEY ARE EQUIVALENCE TO TFSTOU AND TFSTIN, */
	/*     WHICH ARE OF THE SAME SIZE (NUMBER OF NODES) AND ARE USED */
	/*     ONLY AFTER THE TREE COMES INTO USE. */

	/* --------------------------------------------------------------- */

	/*  INITIALIZATION PHASE I */

	/*     IN THIS PHASE, WE REDUCE THE ARC CAPACITIES BY AS MUCH AS */
	/*     POSSIBLE WITHOUT CHANGING THE PROBLEM; */
	/*     THEN WE SET THE INITIAL FLOW ARRAY X, TOGETHER WITH */
	/*     THE CORRESPONDING ARRAYS U AND DFCT. */

	/*     THIS PHASE AND PHASE II (FROM HERE UP TO LINE LABELED 90) */
	/*     CAN BE SKIPPED (BY SETTING REPEAT TO .TRUE.) IF THE CALLING PROGRAM */
	/*     PLACES IN COMMON USER-CHOSEN VALUES FOR THE ARC FLOWS, THE RESIDUAL ARC */
	/*     CAPACITIES, AND THE NODAL DEFICITS.  WHEN THIS IS DONE, */
	/*     IT IS CRITICAL THAT THE FLOW AND THE REDUCED COST FOR EACH ARC */
	/*     SATISFY COMPLEMENTARY SLACKNESS */
	/*     AND THE DFCT ARRAY PROPERLY CORRESPOND TO THE INITIAL ARC/FLOWS. */

		if (input_1.repeat)
		{
		goto L90;
		}

		relax4_i__1 = input_1.n;
		for (node = 1; node <= relax4_i__1; ++node)
		{
		node_def__ = arrayit_1.dfct[node - 1];
		blks2_1.fpushf[node - 1] = node_def__;
		blks2_1.fpushb[node - 1] = -node_def__;
		maxcap = 0;
		scapou = 0;
		arc = blks_1.fou[node - 1];
	L11:
		if (arc > 0)
		{
			if (scapou <= input_1.large - arrayit_1.u[arc - 1])
			{
			scapou += arrayit_1.u[arc - 1];
			}
			else
			{
			goto L10;
			}
			arc = blks_1.nxtou[arc - 1];
			goto L11;
		}
		if (scapou <= input_1.large - node_def__)
		{
			capout = scapou + node_def__;
		}
		else
		{
			goto L10;
		}
		if (capout < 0)
		{

	/*     PROBLEM IS INFEASIBLE - EXIT */

			   Console.WriteLine("EXIT DURING CAPACITY ADJUSTMENT.");
			   Console.WriteLine("EXOGENOUS FLOW INTO NODE {0}.", node.ToString());
	/*         PRINT*,'EXIT DURING CAPACITY ADJUSTMENT' */
	/*         PRINT*,'EXOGENOUS FLOW INTO NODE',NODE, */
	/*    $    ' EXCEEDS OUT CAPACITY' */
	/*         CALL PRINTFLOWS(NODE) */
			goto L4400;
		}

		scapin = 0;
		arc = blks_1.fin[node - 1];
	L12:
		if (arc > 0)
		{
	/* Computing MIN */
			i__2 = arrayit_1.u[arc - 1];
			//arrayit_1.u[arc - 1] = min(i__2,capout);
			if (maxcap < arrayit_1.u[arc - 1])
			{
			maxcap = arrayit_1.u[arc - 1];
			}
			if (scapin <= input_1.large - arrayit_1.u[arc - 1])
			{
			scapin += arrayit_1.u[arc - 1];
			}
			else
			{
			goto L10;
			}
			arc = blks_1.nxtin[arc - 1];
			goto L12;
		}
		if (scapin <= input_1.large + node_def__)
		{
			capin = scapin - node_def__;
		}
		else
		{
			goto L10;
		}
		if (capin < 0)
		{

	/*     PROBLEM IS INFEASIBLE - EXIT */

			   Console.WriteLine("EXIT DURING CAPACITY ADJUSTMENT.");
			   Console.WriteLine("EXOGENOUS FLOW OUT OF NODE {0}.", node.ToString());
	/*         PRINT*,'EXIT DURING CAPACITY ADJUSTMENT' */
	/*         PRINT*,'EXOGENOUS FLOW OUT OF NODE',NODE, */
	/*    $    ' EXCEEDS IN CAPACITY' */
	/*         CALL PRINTFLOWS(NODE) */
			goto L4400;
		}

		arc = blks_1.fou[node - 1];
	L15:
		if (arc > 0)
		{
	/* Computing MIN */
			i__2 = arrayit_1.u[arc - 1];
			//arrayit_1.u[arc - 1] = min(i__2,capin);
			arc = blks_1.nxtou[arc - 1];
			goto L15;
		}
	L10:
		;
		}

	/* --------------------------------------------------------------- */

	/*  INITIALIZATION PHASE II */

	/*     IN THIS PHASE, WE INITIALIZE THE PRICES AND FLOWS BY EITHER CALLING */
	/*     THE ROUTINE AUCTION OR BY PERFORMING ONLY SINGLE NODE (COORDINATE) */
	/*     RELAXATION ITERATIONS. */

		if (input_1.crash == 1)
		{
		  output_1.nsp = 0;
		  GlobalMembersRelax4.auction();
		  goto L70;
		}

	/*     INITIALIZE THE ARC FLOWS TO SATISFY COMPLEMENTARY SLACKNESS WITH THE */
	/*     PRICES.  U(ARC) IS THE RESIDUAL CAPACITY OF ARC, AND X(ARC) IS THE FLOW. */
	/*     THESE TWO ALWAYS ADD UP TO THE TOTAL CAPACITY FOR ARC. */
	/*     ALSO COMPUTE THE DIRECTIONAL DERIVATIVES FOR EACH COORDINATE */
	/*     AND COMPUTE THE ACTUAL DEFICITS. */

		relax4_i__1 = input_1.na;
		for (arc = 1; arc <= relax4_i__1; ++arc)
		{
		arrayit_1.x[arc - 1] = 0;
		if (arrayit_1.rc[arc - 1] <= 0)
		{
			t = arrayit_1.u[arc - 1];
			t1 = arrayit_1.startn[arc - 1];
			t2 = arrayit_1.endn[arc - 1];
			blks2_1.fpushf[t1 - 1] += t;
			blks2_1.fpushb[t2 - 1] += t;
			if (arrayit_1.rc[arc - 1] < 0)
			{
			arrayit_1.x[arc - 1] = t;
			arrayit_1.u[arc - 1] = 0;
			arrayit_1.dfct[t1 - 1] += t;
			arrayit_1.dfct[t2 - 1] -= t;
			blks2_1.fpushb[t1 - 1] -= t;
			blks2_1.fpushf[t2 - 1] -= t;
			}
		}
	/* L20: */
		}

	/*     MAKE 2 OR 3 PASSES THROUGH ALL NODES, PERFORMING ONLY */
	/*     SINGLE NODE RELAXATION ITERATIONS.  THE NUMBER OF */
	/*     PASSES DEPENDS ON THE DENSITY OF THE NETWORK */

		if (input_1.na > input_1.n * 10)
		{
		numpasses = 2;
		}
		else
		{
		numpasses = 3;
		}

		relax4_i__1 = numpasses;
		for (passes = 1; passes <= relax4_i__1; ++passes)
		{
		i__2 = input_1.n;
		for (node = 1; node <= i__2; ++node)
		{
			if (arrayit_1.dfct[node - 1] == 0)
			{
			goto L40;
			}
			if (blks2_1.fpushf[node - 1] <= 0)
			{

	/*     COMPUTE DELPRC, THE STEPSIZE TO THE NEXT BREAKPOINT */
	/*     IN THE DUAL COST AS THE PRICE OF NODE IS INCREASED. */
	/*     [SINCE THE REDUCED COST OF ALL OUTGOING (RESP., */
	/*     INCOMING) ARCS WILL DECREASE (RESP., INCREASE) AS */
	/*     THE PRICE OF NODE IS INCREASED, THE NEXT BREAKPOINT IS */
	/*     THE MINIMUM OF THE POSITIVE REDUCED COST ON OUTGOING */
	/*     ARCS AND OF THE NEGATIVE REDUCED COST ON INCOMING ARCS.] */

			delprc = input_1.large;
			arc = blks_1.fou[node - 1];
	L51:
			if (arc > 0)
			{
				trc = arrayit_1.rc[arc - 1];
				if (trc > 0 && trc < delprc)
				{
				delprc = trc;
				}
				arc = blks_1.nxtou[arc - 1];
				goto L51;
			}
			arc = blks_1.fin[node - 1];
	L52:
			if (arc > 0)
			{
				trc = arrayit_1.rc[arc - 1];
				if (trc < 0 && trc > -delprc)
				{
				delprc = -trc;
				}
				arc = blks_1.nxtin[arc - 1];
				goto L52;
			}

	/*     IF NO BREAKPOINT IS LEFT AND DUAL ASCENT IS STILL */
	/*     POSSIBLE, THE PROBLEM IS INFEASIBLE. */

			if (delprc >= input_1.large)
			{
				if (blks2_1.fpushf[node - 1] == 0)
				{
				goto L40;
				}
				goto L4400;
			}

	/*     DELPRC IS THE STEPSIZE TO NEXT BREAKPOINT.  INCREASE */
	/*     PRICE OF NODE BY DELPRC AND COMPUTE THE STEPSIZE TO */
	/*     THE NEXT BREAKPOINT IN THE DUAL COST. */

	L53:
			nxtbrk = input_1.large;

	/*     LOOK AT ALL ARCS OUT OF NODE. */

			arc = blks_1.fou[node - 1];
	L54:
			if (arc > 0)
			{
				trc = arrayit_1.rc[arc - 1];
				if (trc == 0)
				{
				t1 = arrayit_1.endn[arc - 1];
				t = arrayit_1.u[arc - 1];
				if (t > 0)
				{
					arrayit_1.dfct[node - 1] += t;
					arrayit_1.dfct[t1 - 1] -= t;
					arrayit_1.x[arc - 1] = t;
					arrayit_1.u[arc - 1] = 0;
				}
				else
				{
					t = arrayit_1.x[arc - 1];
				}
				blks2_1.fpushb[node - 1] -= t;
				blks2_1.fpushf[t1 - 1] -= t;
				}

	/*     DECREASE THE REDUCED COST ON ALL OUTGOING ARCS. */

				trc -= delprc;
				if (trc > 0 && trc < nxtbrk)
				{
				nxtbrk = trc;
				}
				else if (trc == 0)
				{

	/*     ARC GOES FROM INACTIVE TO BALANCED.  UPDATE THE */
	/*     RATE OF DUAL ASCENT AT NODE AND AT ITS NEIGHBOR. */

				blks2_1.fpushf[node - 1] += arrayit_1.u[arc - 1];
				blks2_1.fpushb[arrayit_1.endn[arc - 1] - 1] += arrayit_1.u[arc - 1];
				}
				arrayit_1.rc[arc - 1] = trc;
				arc = blks_1.nxtou[arc - 1];
				goto L54;
			}

	/*     LOOK AT ALL ARCS INTO NODE. */

			arc = blks_1.fin[node - 1];
	L55:
			if (arc > 0)
			{
				trc = arrayit_1.rc[arc - 1];
				if (trc == 0)
				{
				t1 = arrayit_1.startn[arc - 1];
				t = arrayit_1.x[arc - 1];
				if (t > 0)
				{
					arrayit_1.dfct[node - 1] += t;
					arrayit_1.dfct[t1 - 1] -= t;
					arrayit_1.u[arc - 1] = t;
					arrayit_1.x[arc - 1] = 0;
				}
				else
				{
					t = arrayit_1.u[arc - 1];
				}
				blks2_1.fpushf[t1 - 1] -= t;
				blks2_1.fpushb[node - 1] -= t;
				}

	/*     INCREASE THE REDUCED COST ON ALL INCOMING ARCS. */

				trc += delprc;
				if (trc < 0 && trc > -nxtbrk)
				{
				nxtbrk = -trc;
				}
				else if (trc == 0)
				{

	/*     ARC GOES FROM ACTIVE TO BALANCED.  UPDATE THE */
	/*     RATE OF DUAL ASCENT AT NODE AND AT ITS NEIGHBOR. */

				blks2_1.fpushb[arrayit_1.startn[arc - 1] - 1] += arrayit_1.x[arc - 1];
				blks2_1.fpushf[node - 1] += arrayit_1.x[arc - 1];
				}
				arrayit_1.rc[arc - 1] = trc;
				arc = blks_1.nxtin[arc - 1];
				goto L55;
			}

	/*     IF PRICE OF NODE CAN BE INCREASED FURTHER WITHOUT DECREASING */
	/*     THE DUAL COST (EVEN IF THE DUAL COST DOESN'T INCREASE), */
	/*     RETURN TO INCREASE THE PRICE FURTHER. */

			if (blks2_1.fpushf[node - 1] <= 0 && nxtbrk < input_1.large)
			{
				delprc = nxtbrk;
				goto L53;
			}
			}
			else if (blks2_1.fpushb[node - 1] <= 0)
			{

	/*     COMPUTE DELPRC, THE STEPSIZE TO THE NEXT BREAKPOINT */
	/*     IN THE DUAL COST AS THE PRICE OF NODE IS DECREASED. */
	/*     [SINCE THE REDUCED COST OF ALL OUTGOING (RESP., */
	/*     INCOMING) ARCS WILL INCREASE (RESP., DECREASE) AS */
	/*     THE PRICE OF NODE IS DECREASED, THE NEXT BREAKPOINT IS */
	/*     THE MINIMUM OF THE NEGATIVE REDUCED COST ON OUTGOING */
	/*     ARCS AND OF THE POSITIVE REDUCED COST ON INCOMING ARCS.] */

			delprc = input_1.large;
			arc = blks_1.fou[node - 1];
	L61:
			if (arc > 0)
			{
				trc = arrayit_1.rc[arc - 1];
				if (trc < 0 && trc > -delprc)
				{
				delprc = -trc;
				}
				arc = blks_1.nxtou[arc - 1];
				goto L61;
			}
			arc = blks_1.fin[node - 1];
	L62:
			if (arc > 0)
			{
				trc = arrayit_1.rc[arc - 1];
				if (trc > 0 && trc < delprc)
				{
				delprc = trc;
				}
				arc = blks_1.nxtin[arc - 1];
				goto L62;
			}

	/*     IF NO BREAKPOINT IS LEFT AND DUAL ASCENT IS STILL */
	/*     POSSIBLE, THE PROBLEM IS INFEASIBLE. */

			if (delprc == input_1.large)
			{
				if (blks2_1.fpushb[node - 1] == 0)
				{
				goto L40;
				}
				goto L4400;
			}

	/*     DELPRC IS THE STEPSIZE TO NEXT BREAKPOINT.  DECREASE */
	/*     PRICE OF NODE BY DELPRC AND COMPUTE THE STEPSIZE TO */
	/*     THE NEXT BREAKPOINT IN THE DUAL COST. */

	L63:
			nxtbrk = input_1.large;

	/*     LOOK AT ALL ARCS OUT OF NODE. */

			arc = blks_1.fou[node - 1];
	L64:
			if (arc > 0)
			{
				trc = arrayit_1.rc[arc - 1];
				if (trc == 0)
				{
				t1 = arrayit_1.endn[arc - 1];
				t = arrayit_1.x[arc - 1];
				if (t > 0)
				{
					arrayit_1.dfct[node - 1] -= t;
					arrayit_1.dfct[t1 - 1] += t;
					arrayit_1.u[arc - 1] = t;
					arrayit_1.x[arc - 1] = 0;
				}
				else
				{
					t = arrayit_1.u[arc - 1];
				}
				blks2_1.fpushf[node - 1] -= t;
				blks2_1.fpushb[t1 - 1] -= t;
				}

	/*     INCREASE THE REDUCED COST ON ALL OUTGOING ARCS. */

				trc += delprc;
				if (trc < 0 && trc > -nxtbrk)
				{
				nxtbrk = -trc;
				}
				else if (trc == 0)
				{

	/*     ARC GOES FROM ACTIVE TO BALANCED.  UPDATE THE */
	/*     RATE OF DUAL ASCENT AT NODE AND AT ITS NEIGHBOR. */

				blks2_1.fpushb[node - 1] += arrayit_1.x[arc - 1];
				blks2_1.fpushf[arrayit_1.endn[arc - 1] - 1] += arrayit_1.x[arc - 1];
				}
				arrayit_1.rc[arc - 1] = trc;
				arc = blks_1.nxtou[arc - 1];
				goto L64;
			}

	/*     LOOK AT ALL ARCS INTO NODE. */

			arc = blks_1.fin[node - 1];
	L65:
			if (arc > 0)
			{
				trc = arrayit_1.rc[arc - 1];
				if (trc == 0)
				{
				t1 = arrayit_1.startn[arc - 1];
				t = arrayit_1.u[arc - 1];
				if (t > 0)
				{
					arrayit_1.dfct[node - 1] -= t;
					arrayit_1.dfct[t1 - 1] += t;
					arrayit_1.x[arc - 1] = t;
					arrayit_1.u[arc - 1] = 0;
				}
				else
				{
					t = arrayit_1.x[arc - 1];
				}
				blks2_1.fpushb[t1 - 1] -= t;
				blks2_1.fpushf[node - 1] -= t;
				}

	/*     DECREASE THE REDUCED COST ON ALL INCOMING ARCS. */

				trc -= delprc;
				if (trc > 0 && trc < nxtbrk)
				{
				nxtbrk = trc;
				}
				else if (trc == 0)
				{

	/*     ARC GOES FROM INACTIVE TO BALANCED.  UPDATE THE */
	/*     RATE OF DUAL ASCENT AT NODE AND AT ITS NEIGHBOR. */

				blks2_1.fpushf[arrayit_1.startn[arc - 1] - 1] += arrayit_1.u[arc - 1];
				blks2_1.fpushb[node - 1] += arrayit_1.u[arc - 1];
				}
				arrayit_1.rc[arc - 1] = trc;
				arc = blks_1.nxtin[arc - 1];
				goto L65;
			}

	/*     IF PRICE OF NODE CAN BE DECREASED FURTHER WITHOUT DECREASING */
	/*     THE DUAL COST (EVEN IF THE DUAL COST DOESN'T INCREASE), */
	/*     RETURN TO DECREASE THE PRICE FURTHER. */

			if (blks2_1.fpushb[node - 1] <= 0 && nxtbrk < input_1.large)
			{
				delprc = nxtbrk;
				goto L63;
			}
			}
	L40:
			;
		}
	/* L30: */
		}


	L70:

	/*    READ TIME FOR INITIALIZATION */
	/*      TIME1 = LONG(362)/60.0 - TIME0 */
	/*     TIME1 = SECNDS(TIME0) */


	/* --------------------------------------------------------------- */

	/*     INITIALIZE TREE DATA STRUCTURE. */
		relax4_i__1 = input_1.n;
		for (i__ = 1; i__ <= relax4_i__1; ++i__)
		{
		blks2_1.fpushf[i__ - 1] = 0;
		blks2_1.fpushb[i__ - 1] = 0;
	/* L80: */
		}
		relax4_i__1 = input_1.na;
		for (i__ = 1; i__ <= relax4_i__1; ++i__)
		{
		blks2_1.nxtpushb[i__ - 1] = -1;
		blks2_1.nxtpushf[i__ - 1] = -1;
		if (arrayit_1.rc[i__ - 1] == 0)
		{
			blks2_1.nxtpushf[i__ - 1] = blks2_1.fpushf[arrayit_1.startn[i__ - 1] - 1];
			blks2_1.fpushf[arrayit_1.startn[i__ - 1] - 1] = i__;
			blks2_1.nxtpushb[i__ - 1] = blks2_1.fpushb[arrayit_1.endn[i__ - 1] - 1];
			blks2_1.fpushb[arrayit_1.endn[i__ - 1] - 1] = i__;
		}
	/* L81: */
		}

	/*     INITIALIZE OTHER VARIABLES. */

	L90:
		blks_1.feasbl = TRUE_;
		output_1.iter = 0;
		output_1.nmultinode = 0;
		output_1.num_augm__ = 0;
		output_1.num_ascnt__ = 0;
		num_passes__ = 0;
		numnz = input_1.n;
		numnz_new__ = 0;
		switch__ = FALSE_;
		relax4_i__1 = input_1.n;
		for (i__ = 1; i__ <= relax4_i__1; ++i__)
		{
		blks2_1.path_id__[i__ - 1] = FALSE_;
		blks2_1.scan[i__ - 1] = FALSE_;
	/* L91: */
		}
		nlabel = 0;

	/*     RELAX4 USES AN ADAPTIVE STRATEGY TO DECIDE WHETHER TO */
	/*     CONTINUE THE SCANNING PROCESS AFTER A MULTINODE PRICE CHANGE. */
	/*     THE THRESHOLD PARAMETER TP AND TS THAT CONTROL */
	/*     THIS STRATEGY ARE SET IN THE NEXT TWO LINES. */

		tp = 10;
		ts = input_1.n / 15;

	/*     INITIALIZE THE QUEUE OF NODES WITH NONZERO DEFICIT */

		relax4_i__1 = input_1.n - 1;
		for (node = 1; node <= relax4_i__1; ++node)
		{
		blks3_1.nxtqueue[node - 1] = node + 1;
	/* L92: */
		}
		blks3_1.nxtqueue[input_1.n - 1] = 1;
		node = input_1.n;
		lastqueue = input_1.n;

	/*    READ TIME FOR INITIALIZATION */
		output_.time1 = ((clock() - output_1.time0) / (double)CLOCKS_PER_SEC) * (double)(1000);

		/* --------------------------------------------------------------- */

	/*     START THE RELAXATION ALGORITHM. */
	L100:

	/*     CODE FOR ADVANCING THE QUEUE OF NONZERO DEFICIT NODES */

		prevnode = node;
		node = blks3_1.nxtqueue[node - 1];
		defcit = arrayit_1.dfct[node - 1];
		if (node == lastqueue)
		{
		numnz = numnz_new__;
		numnz_new__ = 0;
		lastqueue = prevnode;
		++num_passes__;
		}

	/*     CODE FOR DELETING A NODE FROM THE QUEUE */

		if (defcit == 0)
		{
		nxtnode = blks3_1.nxtqueue[node - 1];
		if (node == nxtnode)
		{
			return 0;
		}
		else
		{
			blks3_1.nxtqueue[prevnode - 1] = nxtnode;
			blks3_1.nxtqueue[node - 1] = 0;
			node = nxtnode;
			goto L100;
		}
		}
		else
		{
		posit = defcit > 0;
		}

		++output_1.iter;
		++numnz_new__;

		if (posit != null)
		{

	/*     ATTEMPT A SINGLE NODE ITERATION FROM NODE WITH POSITIVE DEFICIT */

		pchange = FALSE_;
		indef = defcit;
		delx = 0;
		nb = 0;

	/*     CHECK OUTGOING (PROBABLY) BALANCED ARCS FROM NODE. */

		arc = blks2_1.fpushf[node - 1];
	L4500:
		if (arc > 0)
		{
			if (arrayit_1.rc[arc - 1] == 0 && arrayit_1.x[arc - 1] > 0)
			{
			delx += arrayit_1.x[arc - 1];
			++nb;
			blks_1.save[nb - 1] = arc;
			}
			arc = blks2_1.nxtpushf[arc - 1];
			goto L4500;
		}

	/*     CHECK INCOMING ARCS. */

		arc = blks2_1.fpushb[node - 1];
	L4501:
		if (arc > 0)
		{
			if (arrayit_1.rc[arc - 1] == 0 && arrayit_1.u[arc - 1] > 0)
			{
			delx += arrayit_1.u[arc - 1];
			++nb;
			blks_1.save[nb - 1] = -arc;
			}
			arc = blks2_1.nxtpushb[arc - 1];
			goto L4501;
		}

	/*     END OF INITIAL NODE SCAN. */

	L4018:

	/*     IF NO PRICE CHANGE IS POSSIBLE, EXIT. */

		if (delx > defcit)
		{
			quit = defcit < indef;
			goto L4016;
		}

	/*     RELAX4 SEARCHES ALONG THE ASCENT DIRECTION FOR THE */
	/*     BEST PRICE BY CHECKING THE SLOPE OF THE DUAL COST */
	/*     AT SUCCESSIVE BREAK POINTS.  FIRST, WE */
	/*     COMPUTE THE DISTANCE TO THE NEXT BREAK POINT. */

		delprc = input_1.large;
		arc = blks_1.fou[node - 1];
	L4502:
		if (arc > 0)
		{
			rdcost = arrayit_1.rc[arc - 1];
			if (rdcost < 0 && rdcost > -delprc)
			{
			delprc = -rdcost;
			}
			arc = blks_1.nxtou[arc - 1];
			goto L4502;
		}
		arc = blks_1.fin[node - 1];
	L4503:
		if (arc > 0)
		{
			rdcost = arrayit_1.rc[arc - 1];
			if (rdcost > 0 && rdcost < delprc)
			{
			delprc = rdcost;
			}
			arc = blks_1.nxtin[arc - 1];
			goto L4503;
		}

	/*     CHECK IF PROBLEM IS INFEASIBLE. */

		if (delx < defcit && delprc == input_1.large)
		{

	/*     THE DUAL COST CAN BE DECREASED WITHOUT BOUND. */

			goto L4400;
		}

	/*     SKIP FLOW ADJUSTEMT IF THERE IS NO FLOW TO MODIFY. */

		if (delx == 0)
		{
			goto L4014;
		}

	/*     ADJUST THE FLOW ON THE BALANCED ARCS INCIDENT TO NODE TO */
	/*     MAINTAIN COMPLEMENTARY SLACKNESS AFTER THE PRICE CHANGE. */

		relax4_i__1 = nb;
		for (j = 1; j <= relax4_i__1; ++j)
		{
			arc = blks_1.save[j - 1];
			if (arc > 0)
			{
			node2 = arrayit_1.endn[arc - 1];
			t1 = arrayit_1.x[arc - 1];
			arrayit_1.dfct[node2 - 1] += t1;
			if (blks3_1.nxtqueue[node2 - 1] == 0)
			{
				blks3_1.nxtqueue[prevnode - 1] = node2;
				blks3_1.nxtqueue[node2 - 1] = node;
				prevnode = node2;
			}
			arrayit_1.u[arc - 1] += t1;
			arrayit_1.x[arc - 1] = 0;
			}
			else
			{
			narc = -arc;
			node2 = arrayit_1.startn[narc - 1];
			t1 = arrayit_1.u[narc - 1];
			arrayit_1.dfct[node2 - 1] += t1;
			if (blks3_1.nxtqueue[node2 - 1] == 0)
			{
				blks3_1.nxtqueue[prevnode - 1] = node2;
				blks3_1.nxtqueue[node2 - 1] = node;
				prevnode = node2;
			}
			arrayit_1.x[narc - 1] += t1;
			arrayit_1.u[narc - 1] = 0;
			}
	/* L4013: */
		}
		defcit -= delx;
	L4014:
		if (delprc == input_1.large)
		{
			quit = TRUE_;
			goto L4019;
		}

	/*     NODE CORRESPONDS TO A DUAL ASCENT DIRECTION.  DECREASE */
	/*     THE PRICE OF NODE BY DELPRC AND COMPUTE THE STEPSIZE TO THE */
	/*     NEXT BREAKPOINT IN THE DUAL COST. */

		nb = 0;
		pchange = TRUE_;
		dp = delprc;
		delprc = input_1.large;
		delx = 0;
		arc = blks_1.fou[node - 1];
	L4504:
		if (arc > 0)
		{
			rdcost = arrayit_1.rc[arc - 1] + dp;
			arrayit_1.rc[arc - 1] = rdcost;
			if (rdcost == 0)
			{
			++nb;
			blks_1.save[nb - 1] = arc;
			delx += arrayit_1.x[arc - 1];
			}
			if (rdcost < 0 && rdcost > -delprc)
			{
			delprc = -rdcost;
			}
			arc = blks_1.nxtou[arc - 1];
			goto L4504;
		}
		arc = blks_1.fin[node - 1];
	L4505:
		if (arc > 0)
		{
			rdcost = arrayit_1.rc[arc - 1] - dp;
			arrayit_1.rc[arc - 1] = rdcost;
			if (rdcost == 0)
			{
			++nb;
			blks_1.save[nb - 1] = -arc;
			delx += arrayit_1.u[arc - 1];
			}
			if (rdcost > 0 && rdcost < delprc)
			{
			delprc = rdcost;
			}
			arc = blks_1.nxtin[arc - 1];
			goto L4505;
		}

	/*     RETURN TO CHECK IF ANOTHER PRICE CHANGE IS POSSIBLE. */

		goto L4018;

	/*     PERFORM FLOW AUGMENTATION AT NODE. */

	L4016:
		relax4_i__1 = nb;
		for (j = 1; j <= relax4_i__1; ++j)
		{
			arc = blks_1.save[j - 1];
			if (arc > 0)
			{

	/*     ARC IS AN OUTGOING ARC FROM NODE. */

			node2 = arrayit_1.endn[arc - 1];
			t1 = arrayit_1.dfct[node2 - 1];
			if (t1 < 0)
			{

	/*     DECREASE THE TOTAL DEFICIT BY DECREASING FLOW OF ARC. */

				quit = TRUE_;
				t2 = arrayit_1.x[arc - 1];
	/* Computing MIN */
				i__2 = defcit, i__3 = -t1, i__2 = ((i__2) < (i__3)) ? (i__2) : (i__3);
				dx = ((i__2) < (t2)) ? (i__2) : (t2);
				defcit -= dx;
				arrayit_1.dfct[node2 - 1] = t1 + dx;
				if (blks3_1.nxtqueue[node2 - 1] == 0)
				{
				blks3_1.nxtqueue[prevnode - 1] = node2;
				blks3_1.nxtqueue[node2 - 1] = node;
				prevnode = node2;
				}
				arrayit_1.x[arc - 1] = t2 - dx;
				arrayit_1.u[arc - 1] += dx;
				if (defcit == 0)
				{
				goto L4019;
				}
			}
			}
			else
			{

	/*     -ARC IS AN INCOMING ARC TO NODE. */

			narc = -arc;
			node2 = arrayit_1.startn[narc - 1];
			t1 = arrayit_1.dfct[node2 - 1];
			if (t1 < 0)
			{

	/*     DECREASE THE TOTAL DEFICIT BY INCREASING FLOW OF -ARC. */

				quit = TRUE_;
				t2 = arrayit_1.u[narc - 1];
	/* Computing MIN */
				i__2 = defcit, i__3 = -t1, i__2 = ((i__2) < (i__3)) ? (i__2) : (i__3);
				dx = ((i__2) < (t2)) ? (i__2) : (t2);
				defcit -= dx;
				arrayit_1.dfct[node2 - 1] = t1 + dx;
				if (blks3_1.nxtqueue[node2 - 1] == 0)
				{
				blks3_1.nxtqueue[prevnode - 1] = node2;
				blks3_1.nxtqueue[node2 - 1] = node;
				prevnode = node2;
				}
				arrayit_1.x[narc - 1] += dx;
				arrayit_1.u[narc - 1] = t2 - dx;
				if (defcit == 0)
				{
				goto L4019;
				}
			}
			}
	/* L4011: */
		}
	L4019:
		arrayit_1.dfct[node - 1] = defcit;

	/*     RECONSTRUCT THE LINKED LIST OF BALANCE ARCS INCIDENT TO THIS NODE. */
	/*      FOR EACH ADJACENT NODE, WE ADD ANY NEWLY BLANCED ARCS */
	/*      TO THE LIST, BUT DO NOT BOTHER REMOVING FORMERLY BALANCED ONES */
	/*      (THEY WILL BE REMOVED THE NEXT TIME EACH ADJACENT NODE IS SCANNED). */

		if (pchange != null)
		{
			arc = blks2_1.fpushf[node - 1];
			blks2_1.fpushf[node - 1] = 0;
	L4506:
			if (arc > 0)
			{
			nxtarc = blks2_1.nxtpushf[arc - 1];
			blks2_1.nxtpushf[arc - 1] = -1;
			arc = nxtarc;
			goto L4506;
			}
			arc = blks2_1.fpushb[node - 1];
			blks2_1.fpushb[node - 1] = 0;
	L4507:
			if (arc > 0)
			{
			nxtarc = blks2_1.nxtpushb[arc - 1];
			blks2_1.nxtpushb[arc - 1] = -1;
			arc = nxtarc;
			goto L4507;
			}

	/*     NOW ADD THE CURRENTLY BALANCED ARCS TO THE LIST FOR THIS NODE */
	/*     (WHICH IS NOW EMPTY), AND THE APPROPRIATE ADJACENT ONES. */

			relax4_i__1 = nb;
			for (j = 1; j <= relax4_i__1; ++j)
			{
			arc = blks_1.save[j - 1];
			if (arc <= 0)
			{
				arc = -arc;
			}
			if (blks2_1.nxtpushf[arc - 1] < 0)
			{
				blks2_1.nxtpushf[arc - 1] = blks2_1.fpushf[arrayit_1.startn[arc - 1] - 1];
				blks2_1.fpushf[arrayit_1.startn[arc - 1] - 1] = arc;
			}
			if (blks2_1.nxtpushb[arc - 1] < 0)
			{
				blks2_1.nxtpushb[arc - 1] = blks2_1.fpushb[arrayit_1.endn[arc - 1] - 1];
				blks2_1.fpushb[arrayit_1.endn[arc - 1] - 1] = arc;
			}
	/* L4508: */
			}
		}

	/*     END OF SINGLE NODE ITERATION FOR POSITIVE DEFICIT NODE. */

		}
		else
		{

	/*     ATTEMPT A SINGLE NODE ITERATION FROM NODE WITH NEGATIVE DEFICIT */

		pchange = FALSE_;
		defcit = -defcit;
		indef = defcit;
		delx = 0;
		nb = 0;

		arc = blks2_1.fpushb[node - 1];
	L4509:
		if (arc > 0)
		{
			if (arrayit_1.rc[arc - 1] == 0 && arrayit_1.x[arc - 1] > 0)
			{
			delx += arrayit_1.x[arc - 1];
			++nb;
			blks_1.save[nb - 1] = arc;
			}
			arc = blks2_1.nxtpushb[arc - 1];
			goto L4509;
		}
		arc = blks2_1.fpushf[node - 1];
	L4510:
		if (arc > 0)
		{
			if (arrayit_1.rc[arc - 1] == 0 && arrayit_1.u[arc - 1] > 0)
			{
			delx += arrayit_1.u[arc - 1];
			++nb;
			blks_1.save[nb - 1] = -arc;
			}
			arc = blks2_1.nxtpushf[arc - 1];
			goto L4510;
		}

	L4028:
		if (delx >= defcit)
		{
			quit = defcit < indef;
			goto L4026;
		}

	/*     COMPUTE DISTANCE TO NEXT BREAKPOINT. */

		delprc = input_1.large;
		arc = blks_1.fin[node - 1];
	L4511:
		if (arc > 0)
		{
			int arc_check = arc;

			rdcost = arrayit_1.rc[arc - 1];

			if (rdcost < 0 && rdcost > -delprc)
			{
			delprc = -rdcost;
			}
			arc = blks_1.nxtin[arc - 1];
			goto L4511;
		}
		arc = blks_1.fou[node - 1];
	L4512:
		if (arc > 0)
		{
			rdcost = arrayit_1.rc[arc - 1];
			if (rdcost > 0 && rdcost < delprc)
			{
			delprc = rdcost;
			}
			arc = blks_1.nxtou[arc - 1];
			goto L4512;
		}

	/*     CHECK IF PROBLEM IS INFEASIBLE. */

		if (delx < defcit && delprc == input_1.large)
		{
		  goto L4400;
		}
		if (delx == 0)
		{
			goto L4024;
		}

	/*     FLOW AUGMENTATION IS POSSIBLE. */

		relax4_i__1 = nb;
		for (j = 1; j <= relax4_i__1; ++j)
		{
			arc = blks_1.save[j - 1];
			if (arc > 0)
			{
			node2 = arrayit_1.startn[arc - 1];
			t1 = arrayit_1.x[arc - 1];
			arrayit_1.dfct[node2 - 1] -= t1;
			if (blks3_1.nxtqueue[node2 - 1] == 0)
			{
				blks3_1.nxtqueue[prevnode - 1] = node2;
				blks3_1.nxtqueue[node2 - 1] = node;
				prevnode = node2;
			}
			arrayit_1.u[arc - 1] += t1;
			arrayit_1.x[arc - 1] = 0;
			}
			else
			{
			narc = -arc;
			node2 = arrayit_1.endn[narc - 1];
			t1 = arrayit_1.u[narc - 1];
			arrayit_1.dfct[node2 - 1] -= t1;
			if (blks3_1.nxtqueue[node2 - 1] == 0)
			{
				blks3_1.nxtqueue[prevnode - 1] = node2;
				blks3_1.nxtqueue[node2 - 1] = node;
				prevnode = node2;
			}
			arrayit_1.x[narc - 1] += t1;
			arrayit_1.u[narc - 1] = 0;
			}
	/* L4023: */
		}
		defcit -= delx;
	L4024:
		if (delprc == input_1.large)
		{
			quit = TRUE_;
			goto L4029;
		}

	/*     PRICE INCREASE AT NODE IS POSSIBLE. */

		nb = 0;
		pchange = TRUE_;
		dp = delprc;
		delprc = input_1.large;
		delx = 0;
		arc = blks_1.fin[node - 1];
	L4513:
		if (arc > 0)
		{
			rdcost = arrayit_1.rc[arc - 1] + dp;
			arrayit_1.rc[arc - 1] = rdcost;
			if (rdcost == 0)
			{
			++nb;
			blks_1.save[nb - 1] = arc;
			delx += arrayit_1.x[arc - 1];
			}
			if (rdcost < 0 && rdcost > -delprc)
			{
			delprc = -rdcost;
			}
			arc = blks_1.nxtin[arc - 1];
			goto L4513;
		}
		arc = blks_1.fou[node - 1];
	L4514:
		if (arc > 0)
		{
			rdcost = arrayit_1.rc[arc - 1] - dp;
			arrayit_1.rc[arc - 1] = rdcost;
			if (rdcost == 0)
			{
			++nb;
			blks_1.save[nb - 1] = -arc;
			delx += arrayit_1.u[arc - 1];
			}
			if (rdcost > 0 && rdcost < delprc)
			{
			delprc = rdcost;
			}
			arc = blks_1.nxtou[arc - 1];
			goto L4514;
		}
		goto L4028;

	/*     PERFORM FLOW AUGMENTATION AT NODE. */

	L4026:
		relax4_i__1 = nb;
		for (j = 1; j <= relax4_i__1; ++j)
		{
			arc = blks_1.save[j - 1];
			if (arc > 0)
			{

	/*     ARC IS AN INCOMING ARC TO NODE. */

			node2 = arrayit_1.startn[arc - 1];
			t1 = arrayit_1.dfct[node2 - 1];
			if (t1 > 0)
			{
				quit = TRUE_;
				t2 = arrayit_1.x[arc - 1];
	/* Computing MIN */
				i__2 = ((defcit) < (t1)) ? (defcit) : (t1);
				dx = ((i__2) < (t2)) ? (i__2) : (t2);
				defcit -= dx;
				arrayit_1.dfct[node2 - 1] = t1 - dx;
				if (blks3_1.nxtqueue[node2 - 1] == 0)
				{
				blks3_1.nxtqueue[prevnode - 1] = node2;
				blks3_1.nxtqueue[node2 - 1] = node;
				prevnode = node2;
				}
				arrayit_1.x[arc - 1] = t2 - dx;
				arrayit_1.u[arc - 1] += dx;
				if (defcit == 0)
				{
				goto L4029;
				}
			}
			}
			else
			{

	/*     -ARC IS AN OUTGOING ARC FROM NODE. */

			narc = -arc;
			node2 = arrayit_1.endn[narc - 1];
			t1 = arrayit_1.dfct[node2 - 1];
			if (t1 > 0)
			{
				quit = TRUE_;
				t2 = arrayit_1.u[narc - 1];
	/* Computing MIN */
				i__2 = ((defcit) < (t1)) ? (defcit) : (t1);
				dx = ((i__2) < (t2)) ? (i__2) : (t2);
				defcit -= dx;
				arrayit_1.dfct[node2 - 1] = t1 - dx;
				if (blks3_1.nxtqueue[node2 - 1] == 0)
				{
				blks3_1.nxtqueue[prevnode - 1] = node2;
				blks3_1.nxtqueue[node2 - 1] = node;
				prevnode = node2;
				}
				arrayit_1.x[narc - 1] += dx;
				arrayit_1.u[narc - 1] = t2 - dx;
				if (defcit == 0)
				{
				goto L4029;
				}
			}
			}
	/* L4021: */
		}
	L4029:
		arrayit_1.dfct[node - 1] = -defcit;

	/*     RECONSTRUCT THE LIST OF BALANCED ARCS INCIDENT TO NODE. */

		if (pchange != null)
		{
			arc = blks2_1.fpushf[node - 1];
			blks2_1.fpushf[node - 1] = 0;
	L4515:
			if (arc > 0)
			{
			nxtarc = blks2_1.nxtpushf[arc - 1];
			blks2_1.nxtpushf[arc - 1] = -1;
			arc = nxtarc;
			goto L4515;
			}
			arc = blks2_1.fpushb[node - 1];
			blks2_1.fpushb[node - 1] = 0;
	L4516:
			if (arc > 0)
			{
			nxtarc = blks2_1.nxtpushb[arc - 1];
			blks2_1.nxtpushb[arc - 1] = -1;
			arc = nxtarc;
			goto L4516;
			}

	/*     NOW ADD THE CURRENTLY BALANCED ARCS TO THE LIST FOR THIS NODE */
	/*     (WHICH IS NOW EMPTY), AND THE APPROPRIATE ADJACENT ONES. */

			relax4_i__1 = nb;
			for (j = 1; j <= relax4_i__1; ++j)
			{
			arc = blks_1.save[j - 1];
			if (arc <= 0)
			{
				arc = -arc;
			}
			if (blks2_1.nxtpushf[arc - 1] < 0)
			{
				blks2_1.nxtpushf[arc - 1] = blks2_1.fpushf[arrayit_1.startn[arc - 1] - 1];
				blks2_1.fpushf[arrayit_1.startn[arc - 1] - 1] = arc;
			}
			if (blks2_1.nxtpushb[arc - 1] < 0)
			{
				blks2_1.nxtpushb[arc - 1] = blks2_1.fpushb[arrayit_1.endn[arc - 1] - 1];
				blks2_1.fpushb[arrayit_1.endn[arc - 1] - 1] = arc;
			}
	/* L4517: */
			}
		}

	/*     END OF SINGLE NODE ITERATION FOR A NEGATIVE DEFICIT NODE. */

		}

		if (quit != null || num_passes__ <= 3)
		{
		goto L100;
		}

	/*     DO A MULTINODE ITERATION FROM NODE. */

		++output_1.nmultinode;

	/*     IF NUMBER OF NONZERO DEFICIT NODES IS SMALL, CONTINUE */
	/*     LABELING UNTIL A FLOW AUGMENTATION IS DONE. */

		switch__ = numnz < tp;

	/*     UNMARK NODES LABELED EARLIER. */

		relax4_i__1 = nlabel;
		for (j = 1; j <= relax4_i__1; ++j)
		{
		node2 = blks_1.label[j - 1];
		blks2_1.path_id__[node2 - 1] = FALSE_;
		blks2_1.scan[node2 - 1] = FALSE_;
	/* L4090: */
		}

	/*     INITIALIZE LABELING. */

		nlabel = 1;
		blks_1.label[0] = node;
		blks2_1.path_id__[node - 1] = TRUE_;
		blks_1.prdcsr[node - 1] = 0;

	/*     SCAN STARTING NODE. */

		blks2_1.scan[node - 1] = TRUE_;
		nscan = 1;
		dm = arrayit_1.dfct[node - 1];
		delx = 0;
		relax4_i__1 = nb;
		for (j = 1; j <= relax4_i__1; ++j)
		{
		arc = blks_1.save[j - 1];
		if (arc > 0)
		{
			if (posit != null)
			{
			node2 = arrayit_1.endn[arc - 1];
			}
			else
			{
			node2 = arrayit_1.startn[arc - 1];
			}
			if (!blks2_1.path_id__[node2 - 1])
			{
			++nlabel;
			blks_1.label[nlabel - 1] = node2;
			blks_1.prdcsr[node2 - 1] = arc;
			blks2_1.path_id__[node2 - 1] = TRUE_;
			delx += arrayit_1.x[arc - 1];
			}
		}
		else
		{
			narc = -arc;
			if (posit != null)
			{
			node2 = arrayit_1.startn[narc - 1];
			}
			else
			{
			node2 = arrayit_1.endn[narc - 1];
			}
			if (!blks2_1.path_id__[node2 - 1])
			{
			++nlabel;
			blks_1.label[nlabel - 1] = node2;
			blks_1.prdcsr[node2 - 1] = arc;
			blks2_1.path_id__[node2 - 1] = TRUE_;
			delx += arrayit_1.u[narc - 1];
			}
		}
	/* L4095: */
		}

	/*     START SCANNING A LABELED BUT UNSCANNED NODE. */

	L4120:
		++nscan;

	/*     CHECK TO SEE IF SWITCH NEEDS TO BE SET TO TRUE SO TO */
	/*     CONTINUE SCANNING EVEN AFTER A PRICE CHANGE. */

		switch__ = switch__ || nscan > ts && numnz < ts;

	/*     SCANNING WILL CONTINUE UNTIL EITHER AN OVERESTIMATE OF THE RESIDUAL */
	/*     CAPACITY ACROSS THE CUT CORRESPONDING TO THE SCANNED SET OF NODES (CALLED */
	/*     DELX) EXCEEDS THE ABSOLUTE VALUE OF THE TOTAL DEFICIT OF THE SCANNED */
	/*     NODES (CALLED DM), OR ELSE AN AUGMENTING PATH IS FOUND.  ARCS THAT ARE */
	/*     IN THE TREE BUT ARE NOT BALANCED ARE REMOVED AS PART OF THE SCANNING */
	/*     PROCESS. */

		i__ = blks_1.label[nscan - 1];
		blks2_1.scan[i__ - 1] = TRUE_;
		naugnod = 0;
		if (posit != null)
		{

	/*     SCANNING NODE I IN CASE OF POSITIVE DEFICIT. */

		prvarc = 0;
		arc = blks2_1.fpushf[i__ - 1];
	L4518:
		if (arc > 0)
		{

	/*     ARC IS AN OUTGOING ARC FROM NODE. */

			if (arrayit_1.rc[arc - 1] == 0)
			{
			if (arrayit_1.x[arc - 1] > 0)
			{
				node2 = arrayit_1.endn[arc - 1];
				if (!blks2_1.path_id__[node2 - 1])
				{

	/*     NODE2 IS NOT LABELED, SO ADD NODE2 TO THE LABELED SET. */

				blks_1.prdcsr[node2 - 1] = arc;
				if (arrayit_1.dfct[node2 - 1] < 0)
				{
					++naugnod;
					blks_1.save[naugnod - 1] = node2;
				}
				++nlabel;
				blks_1.label[nlabel - 1] = node2;
				blks2_1.path_id__[node2 - 1] = TRUE_;
				delx += arrayit_1.x[arc - 1];
				}
			}
			prvarc = arc;
			arc = blks2_1.nxtpushf[arc - 1];
			}
			else
			{
			tmparc = arc;
			arc = blks2_1.nxtpushf[arc - 1];
			blks2_1.nxtpushf[tmparc - 1] = -1;
			if (prvarc == 0)
			{
				blks2_1.fpushf[i__ - 1] = arc;
			}
			else
			{
				blks2_1.nxtpushf[prvarc - 1] = arc;
			}
			}
			goto L4518;
		}
		prvarc = 0;
		arc = blks2_1.fpushb[i__ - 1];
	L4519:
		if (arc > 0)
		{

	/*     ARC IS AN INCOMING ARC INTO NODE. */

			if (arrayit_1.rc[arc - 1] == 0)
			{
			if (arrayit_1.u[arc - 1] > 0)
			{
				node2 = arrayit_1.startn[arc - 1];
				if (!blks2_1.path_id__[node2 - 1])
				{

	/*     NODE2 IS NOT LABELED, SO ADD NODE2 TO THE LABELED SET. */

				blks_1.prdcsr[node2 - 1] = -arc;
				if (arrayit_1.dfct[node2 - 1] < 0)
				{
					++naugnod;
					blks_1.save[naugnod - 1] = node2;
				}
				++nlabel;
				blks_1.label[nlabel - 1] = node2;
				blks2_1.path_id__[node2 - 1] = TRUE_;
				delx += arrayit_1.u[arc - 1];
				}
			}
			prvarc = arc;
			arc = blks2_1.nxtpushb[arc - 1];
			}
			else
			{
			tmparc = arc;
			arc = blks2_1.nxtpushb[arc - 1];
			blks2_1.nxtpushb[tmparc - 1] = -1;
			if (prvarc == 0)
			{
				blks2_1.fpushb[i__ - 1] = arc;
			}
			else
			{
				blks2_1.nxtpushb[prvarc - 1] = arc;
			}
			}
			goto L4519;
		}

	/*     CORRECT THE RESIDUAL CAPACITY OF THE SCANNED NODE CUT. */

		arc = blks_1.prdcsr[i__ - 1];
		if (arc > 0)
		{
			try
			{
				delx -= arrayit_1.x[arc - 1];
			}
			catch (Exception error)
			{
				Console.WriteLine("FAILED");
			}
		}
		else
		{
			delx -= arrayit_1.u[-arc - 1];
		}

	/*     END OF SCANNING OF NODE I FOR POSITIVE DEFICIT CASE. */

		}
		else
		{

	/*     SCANNING NODE I FOR NEGATIVE DEFICIT CASE. */

		prvarc = 0;
		arc = blks2_1.fpushb[i__ - 1];
	L4520:
		if (arc > 0)
		{
			if (arrayit_1.rc[arc - 1] == 0)
			{
			if (arrayit_1.x[arc - 1] > 0)
			{
				node2 = arrayit_1.startn[arc - 1];
				if (!blks2_1.path_id__[node2 - 1])
				{
				blks_1.prdcsr[node2 - 1] = arc;
				if (arrayit_1.dfct[node2 - 1] > 0)
				{
					++naugnod;
					blks_1.save[naugnod - 1] = node2;
				}
				++nlabel;
				blks_1.label[nlabel - 1] = node2;
				blks2_1.path_id__[node2 - 1] = TRUE_;
				delx += arrayit_1.x[arc - 1];
				}
			}
			prvarc = arc;
			arc = blks2_1.nxtpushb[arc - 1];
			}
			else
			{
			tmparc = arc;
			arc = blks2_1.nxtpushb[arc - 1];
			blks2_1.nxtpushb[tmparc - 1] = -1;
			if (prvarc == 0)
			{
				blks2_1.fpushb[i__ - 1] = arc;
			}
			else
			{
				blks2_1.nxtpushb[prvarc - 1] = arc;
			}
			}
			goto L4520;
		}

		prvarc = 0;
		arc = blks2_1.fpushf[i__ - 1];
	L4521:
		if (arc > 0)
		{
			if (arrayit_1.rc[arc - 1] == 0)
			{
			if (arrayit_1.u[arc - 1] > 0)
			{
				node2 = arrayit_1.endn[arc - 1];
				if (!blks2_1.path_id__[node2 - 1])
				{
				blks_1.prdcsr[node2 - 1] = -arc;
				if (arrayit_1.dfct[node2 - 1] > 0)
				{
					++naugnod;
					blks_1.save[naugnod - 1] = node2;
				}
				++nlabel;
				blks_1.label[nlabel - 1] = node2;
				blks2_1.path_id__[node2 - 1] = TRUE_;
				delx += arrayit_1.u[arc - 1];
				}
			}
			prvarc = arc;
			arc = blks2_1.nxtpushf[arc - 1];
			}
			else
			{
			tmparc = arc;
			arc = blks2_1.nxtpushf[arc - 1];
			blks2_1.nxtpushf[tmparc - 1] = -1;
			if (prvarc == 0)
			{
				blks2_1.fpushf[i__ - 1] = arc;
			}
			else
			{
				blks2_1.nxtpushf[prvarc - 1] = arc;
			}
			}
			goto L4521;
		}

		arc = blks_1.prdcsr[i__ - 1];
		if (arc > 0)
		{
			delx -= arrayit_1.x[arc - 1];
		}
		else
		{
			delx -= arrayit_1.u[-arc - 1];
		}
		}

	/*     ADD DEFICIT OF NODE SCANNED TO DM. */

		dm += arrayit_1.dfct[i__ - 1];

	/*     CHECK IF THE SET OF SCANNED NODES CORRESPOND */
	/*     TO A DUAL ASCENT DIRECTION; IF YES, PERFORM A */
	/*     PRICE ADJUSTMENT STEP, OTHERWISE CONTINUE LABELING. */

		if (nscan < nlabel)
		{
		if (switch__ != null)
		{
			goto L4210;
		}
		if (delx >= dm && delx >= -dm)
		{
			goto L4210;
		}
		}

	/*     TRY A PRICE CHANGE. */
	/*     [NOTE THAT SINCE DELX-ABS(DM) IS AN OVERESTIMATE OF ASCENT SLOPE, WE */
	/*     MAY OCCASIONALLY TRY A DIRECTION THAT IS NOT AN ASCENT DIRECTION. */
	/*     IN THIS CASE, THE ASCNT ROUTINES RETURN WITH QUIT=.FALSE., */
	/*     SO WE CONTINUE LABELING NODES. */

		if (posit != null)
		{
		GlobalMembersRelax4.ascnt1_(dm, delx, nlabel, blks_1.feasbl, switch__, nscan, node, prevnode);
		++output_1.num_ascnt__;
		}
		else
		{
		GlobalMembersRelax4.ascnt2_(dm, delx, nlabel, blks_1.feasbl, switch__, nscan, node, prevnode);
		++output_1.num_ascnt__;
		}
		if (!blks_1.feasbl)
		{
		goto L4400;
		}
		if (switch__ == null)
		{
		goto L100;
		}

	/*     STORE THOSE NEWLY LABELED NODES TO WHICH FLOW AUGMENTATION IS POSSIBLE. */

		naugnod = 0;
		relax4_i__1 = nlabel;
		for (j = nscan + 1; j <= relax4_i__1; ++j)
		{
		node2 = blks_1.label[j - 1];
		if (posit != null && arrayit_1.dfct[node2 - 1] < 0)
		{
			++naugnod;
			blks_1.save[naugnod - 1] = node2;
		}
		else if (posit == null && arrayit_1.dfct[node2 - 1] > 0)
		{
			++naugnod;
			blks_1.save[naugnod - 1] = node2;
		}
	/* L530: */
		}

	/*     CHECK IF FLOW AUGMENTATION IS POSSIBLE. */
	/*     IF NOT, RETURN TO SCAN ANOTHER NODE. */

	L4210:

		if (naugnod == 0)
		{
		goto L4120;
		}

		relax4_i__1 = naugnod;
		for (j = 1; j <= relax4_i__1; ++j)
		{
		++output_1.num_augm__;
		augnod = blks_1.save[j - 1];
		if (posit != null)
		{

	/*     DO THE AUGMENTATION FROM NODE WITH POSITIVE DEFICIT. */

			dx = -arrayit_1.dfct[augnod - 1];
			ib = augnod;
	L1500:
			if (ib != node)
			{
			arc = blks_1.prdcsr[ib - 1];
			if (arc > 0)
			{
	/* Computing MIN */
				i__2 = dx, i__3 = arrayit_1.x[arc - 1];
				dx = ((i__2) < (i__3)) ? (i__2) : (i__3);
				ib = arrayit_1.startn[arc - 1];
			}
			else
			{
	/* Computing MIN */
				i__2 = dx, i__3 = arrayit_1.u[-arc - 1];
				dx = ((i__2) < (i__3)) ? (i__2) : (i__3);
				ib = arrayit_1.endn[-arc - 1];
			}
			goto L1500;
			}
	/* Computing MIN */
			i__2 = dx, i__3 = arrayit_1.dfct[node - 1];
			dx = ((i__2) < (i__3)) ? (i__2) : (i__3);
			if (dx > 0)
			{

	/*     INCREASE (DECREASE) THE FLOW OF ALL FORWARD (BACKWARD) */
	/*     ARCS IN THE FLOW AUGMENTING PATH.  ADJUST NODE DEFICIT ACCORDINGLY. */

			if (blks3_1.nxtqueue[augnod - 1] == 0)
			{
				blks3_1.nxtqueue[prevnode - 1] = augnod;
				blks3_1.nxtqueue[augnod - 1] = node;
				prevnode = augnod;
			}
			arrayit_1.dfct[augnod - 1] += dx;
			arrayit_1.dfct[node - 1] -= dx;
			ib = augnod;
	L1501:
			if (ib != node)
			{
				arc = blks_1.prdcsr[ib - 1];
				if (arc > 0)
				{
				arrayit_1.x[arc - 1] -= dx;
				arrayit_1.u[arc - 1] += dx;
				ib = arrayit_1.startn[arc - 1];
				}
				else
				{
				narc = -arc;
				arrayit_1.x[narc - 1] += dx;
				arrayit_1.u[narc - 1] -= dx;
				ib = arrayit_1.endn[narc - 1];
				}
				goto L1501;
			}
			}
		}
		else
		{

	/*     DO THE AUGMENTATION FROM NODE WITH NEGATIVE DEFICIT. */

			dx = arrayit_1.dfct[augnod - 1];
			ib = augnod;
	L1502:
			if (ib != node)
			{
			arc = blks_1.prdcsr[ib - 1];
			if (arc > 0)
			{
	/* Computing MIN */
				i__2 = dx, i__3 = arrayit_1.x[arc - 1];
				dx = ((i__2) < (i__3)) ? (i__2) : (i__3);
				ib = arrayit_1.endn[arc - 1];
			}
			else
			{
	/* Computing MIN */
				i__2 = dx, i__3 = arrayit_1.u[-arc - 1];
				dx = ((i__2) < (i__3)) ? (i__2) : (i__3);
				ib = arrayit_1.startn[-arc - 1];
			}
			goto L1502;
			}
	/* Computing MIN */
			i__2 = dx, i__3 = -arrayit_1.dfct[node - 1];
			dx = ((i__2) < (i__3)) ? (i__2) : (i__3);
			if (dx > 0)
			{

	/*     UPDATE THE FLOW AND DEFICITS. */

			if (blks3_1.nxtqueue[augnod - 1] == 0)
			{
				blks3_1.nxtqueue[prevnode - 1] = augnod;
				blks3_1.nxtqueue[augnod - 1] = node;
				prevnode = augnod;
			}
			arrayit_1.dfct[augnod - 1] -= dx;
			arrayit_1.dfct[node - 1] += dx;
			ib = augnod;
	L1503:
			if (ib != node)
			{
				arc = blks_1.prdcsr[ib - 1];
				if (arc > 0)
				{
				arrayit_1.x[arc - 1] -= dx;
				arrayit_1.u[arc - 1] += dx;
				ib = arrayit_1.endn[arc - 1];
				}
				else
				{
				narc = -arc;
				arrayit_1.x[narc - 1] += dx;
				arrayit_1.u[narc - 1] -= dx;
				ib = arrayit_1.startn[narc - 1];
				}
				goto L1503;
			}
			}
		}
		if (arrayit_1.dfct[node - 1] == 0)
		{
			goto L100;
		}
		if (arrayit_1.dfct[augnod - 1] != 0)
		{
			switch__ = FALSE_;
		}
	/* L4096: */
		}

	/*     IF NODE STILL HAS NONZERO DEFICIT AND ALL NEWLY */
	/*     LABELED NODES HAVE SAME SIGN FOR THEIR DEFICIT AS */
	/*     NODE, WE CAN CONTINUE LABELING.  IN THIS CASE, CONTINUE */
	/*     LABELING ONLY WHEN FLOW AUGMENTATION IS DONE */
	/*     RELATIVELY INFREQUENTLY. */

		if (switch__ != null && output_1.iter > output_1.num_augm__ << 3)
		{
		goto L4120;
		}

	/*     RETURN TO DO ANOTHER RELAXATION ITERATION. */

		goto L100;

	/*     PROBLEM IS FOUND TO BE INFEASIBLE */

	L4400:
		Model.FireOnErrorGlobal("PROBLEM IS FOUND TO BE INFEASIBLE.");
		return 1;
	 } // relax4_

	/* Subroutine */	 public static int auction()
	 {
		/* System generated locals */
		long i__1;

		/* Local variables */
		long node;
		long pend;
		long naug;
		long incr;
		long last;
		long pass;
		long term;
		long flow;
		long seclevel;
		long red_cost__;
		long root;
		long bstlevel;
		long prevnode;
		long i__;
		long resid;
		long pterm;
		long start;
		long new_level__;
		long prevlevel;
		long lastqueue;
		long secarc;
		long factor;
		long extarc;
		long rdcost;
		long nolist;
		long pstart;
		long num_passes__;
		long arc;
		long end;
		long nas;
		long prd;
		long eps;
		long prevarc;
		long pr_term__;
		long thresh_dfct__;
		long mincost;
		long maxcost;
		long nxtnode;

	/* --------------------------------------------------------------- */

	/*  PURPOSE - THIS SUBROUTINE USES A VERSION OF THE AUCTION */
	/*     ALGORITHM FOR MIN COST NETWORK FLOW TO COMPUTE A */
	/*     GOOD INITIAL FLOW AND PRICES FOR THE PROBLEM. */

	/* --------------------------------------------------------------- */

	/*     MAXNN = DIMENSION OF NODE-LENGTH ARRAYS */
	/*     MAXNA = DIMENSION OF ARC-LENGTH ARRAYS */


	/*  INPUT PARAMETERS */

	/*     N         = NUMBER OF NODES */
	/*     NA        = NUMBER OF ARCS */
	/*     LARGE     = A VERY LARGE INTEGER TO REPRESENT INFINITY */
	/*                 (SEE NOTE 3) */
	/*     STARTN(I) = STARTING NODE FOR THE I-TH ARC,    I = 1,...,NA */
	/*     ENDN(I)   = ENDING NODE FOR THE I-TH ARC,      I = 1,...,NA */
	/*     FOU(I)    = FIRST ARC LEAVING I-TH NODE,       I = 1,...,N */
	/*     NXTOU(I)  = NEXT ARC LEAVING THE STARTING NODE OF J-TH ARC, */
	/*                                                    I = 1,...,NA */
	/*     FIN(I)    = FIRST ARC ENTERING I-TH NODE,      I = 1,...,N */
	/*     NXTIN(I)  = NEXT ARC ENTERING THE ENDING NODE OF J-TH ARC, */
	/*                                                    I = 1,...,NA */


	/*  UPDATED PARAMETERS */

	/*     RC(J)     = REDUCED COST OF ARC J,             J = 1,...,NA */
	/*     U(J)      = RESIDUAL CAPACITY OF ARC J, */
	/*                                                    J = 1,...,NA */
	/*     X(J)      = FLOW ON ARC J,                     J = 1,...,NA */
	/*     DFCT(I)   = DEFICIT AT NODE I,                 I = 1,...,N */


	/*  OUTPUT PARAMETERS */

	/*  WORKING PARAMETERS */

	/* ^^                                     B                     ^^ */
	/* ^^          TEMPIN I1 P,TEMPOU PRICE I2,I3,I4,I5,I6,I7        ^^ */
	/* ^^                      MARK   TFSTOU  TNXTOU  TFSTIN TNXTIN ^^ */
	/* ^^                  I14      I15        I16     I17          ^^ */

	/*  START INITIALIZATION USING AUCTION */
		naug = 0;
		pass = 0;
		thresh_dfct__ = 0;

	/*  FACTOR DETERMINES BY HOW MUCH EPSILON IS REDUCED AT EACH MINIMIZATION */
		factor = DefineConstants.EPSFACTOR;

	/*  NUM_PASSES DETERMINES HOW MANY AUCTION SCALING PHASES ARE PERFORMED */
		num_passes__ = DefineConstants.MAXAUCTIONPASS; //1;

	/*    SET ARC FLOWS TO SATISFY CS AND CALCULATE MAXCOST AND MINCOST */
		maxcost = -input_1.large / 50;
		mincost = input_1.large / 50;
		i__1 = input_1.na;
		for (arc = 1; arc <= i__1; ++arc)
		{
		start = arrayit_1.startn[arc - 1];
		end = arrayit_1.endn[arc - 1];
		rdcost = arrayit_1.rc[arc - 1];
		if (maxcost < rdcost)
		{
			maxcost = rdcost;
		}
		if (mincost > rdcost)
		{
			mincost = rdcost;
		}
		if (rdcost < 0)
		{
			arrayit_1.dfct[start - 1] += arrayit_1.u[arc - 1];
			arrayit_1.dfct[end - 1] -= arrayit_1.u[arc - 1];
			arrayit_1.x[arc - 1] = arrayit_1.u[arc - 1];
			arrayit_1.u[arc - 1] = 0;
		}
		else
		{
			arrayit_1.x[arc - 1] = 0;
		}
	/* L49: */
		}

	/*     SET INITIAL EPSILON */

		if (maxcost - mincost >= 8)
		{
		eps = (maxcost - mincost) / 8;
		}
		else
		{
		eps = 1;
		}

	/*     SET INITIAL PRICES TO ZERO */

		i__1 = input_1.n;
		for (node = 1; node <= i__1; ++node)
		{
		blks_1.label[node - 1] = 0;
	/* L48: */
		}

	/*     INITIALIZATION USING AUCTION/SHORTEST PATHS. */
	/*     START OF THE FIRST SCALING PHASE. */

	L100:
		++pass;
		if (pass == num_passes__ || eps == 1)
		{
		   input_1.crash = 0;
		}
		nolist = 0;

	/*     CONSTRUCT LIST OF POSITIVE SURPLUS NODES AND QUEUE OF NEGATIVE SURPLUS */
	/*     NODES */

		i__1 = input_1.n;
		for (node = 1; node <= i__1; ++node)
		{
		blks_1.prdcsr[node - 1] = 0;
		blks2_1.path_id__[node - 1] = FALSE_;
		blks3_1.extend_arc__[node - 1] = 0;
		blks3_1.sb_level__[node - 1] = -input_1.large;
		blks3_1.nxtqueue[node - 1] = node + 1;
		if (arrayit_1.dfct[node - 1] > 0)
		{
			++nolist;
			blks_1.save[nolist - 1] = node;
		}
	/* L110: */
		}

		blks3_1.nxtqueue[input_1.n - 1] = 1;
		root = 1;
		prevnode = input_1.n;
		lastqueue = input_1.n;

	/*     INITIALIZATION WITH DOWN ITERATIONS FOR NEGATIVE SURPLUS NODES */

		i__1 = nolist;
		for (i__ = 1; i__ <= i__1; ++i__)
		{
		node = blks_1.save[i__ - 1];
		++output_1.nsp;

	/*     BUILD THE LIST OF ARCS W/ ROOM FOR PUSHING FLOW */
	/*     AND FIND PROPER PRICE FOR DOWN ITERATION */

		bstlevel = -input_1.large;
		blks2_1.fpushf[node - 1] = 0;
		arc = blks_1.fou[node - 1];
	L152:
		if (arc > 0)
		{
			if (arrayit_1.u[arc - 1] > 0)
			{
			if (blks2_1.fpushf[node - 1] == 0)
			{
				blks2_1.fpushf[node - 1] = arc;
				blks2_1.nxtpushf[arc - 1] = 0;
				last = arc;
			}
			else
			{
				blks2_1.nxtpushf[last - 1] = arc;
				blks2_1.nxtpushf[arc - 1] = 0;
				last = arc;
			}
			}
			if (arrayit_1.x[arc - 1] > 0)
			{
			new_level__ = blks_1.label[arrayit_1.endn[arc - 1] - 1] + arrayit_1.rc[arc - 1];
			if (new_level__ > bstlevel)
			{
				bstlevel = new_level__;
				extarc = arc;
			}
			}
			arc = blks_1.nxtou[arc - 1];
			goto L152;
		}

		blks2_1.fpushb[node - 1] = 0;
		arc = blks_1.fin[node - 1];
	L154:
		if (arc > 0)
		{
			if (arrayit_1.x[arc - 1] > 0)
			{
			if (blks2_1.fpushb[node - 1] == 0)
			{
				blks2_1.fpushb[node - 1] = arc;
				blks2_1.nxtpushb[arc - 1] = 0;
				last = arc;
			}
			else
			{
				blks2_1.nxtpushb[last - 1] = arc;
				blks2_1.nxtpushb[arc - 1] = 0;
				last = arc;
			}
			}
			if (arrayit_1.u[arc - 1] > 0)
			{
			new_level__ = blks_1.label[arrayit_1.startn[arc - 1] - 1] - arrayit_1.rc[arc - 1];
			if (new_level__ > bstlevel)
			{
				bstlevel = new_level__;
				extarc = -arc;
			}
			}
			arc = blks_1.nxtin[arc - 1];
			goto L154;
		}
		blks3_1.extend_arc__[node - 1] = extarc;
		blks_1.label[node - 1] = bstlevel - eps;
	/* L150: */
		}

	/*     START THE AUGMENTATION CYCLES OF THE NEW SCALING PHASE. */

	L200:
		if (arrayit_1.dfct[root - 1] >= thresh_dfct__)
		{
		goto L3000;
		}
		term = root;
		blks2_1.path_id__[root - 1] = TRUE_;

	/*     MAIN FORWARD ALGORITHM WITH ROOT AS ORIGIN. */

	L500:
	/*     START OF A NEW FORWARD ITERATION */

		pterm = blks_1.label[term - 1];
		extarc = blks3_1.extend_arc__[term - 1];
		if (extarc == 0)
		{

	/*     BUILD THE LIST OF ARCS W/ ROOM FOR PUSHING FLOW */

		blks2_1.fpushf[term - 1] = 0;
		arc = blks_1.fou[term - 1];
	L502:
		if (arc > 0)
		{
			if (arrayit_1.u[arc - 1] > 0)
			{
			if (blks2_1.fpushf[term - 1] == 0)
			{
				blks2_1.fpushf[term - 1] = arc;
				blks2_1.nxtpushf[arc - 1] = 0;
				last = arc;
			}
			else
			{
				blks2_1.nxtpushf[last - 1] = arc;
				blks2_1.nxtpushf[arc - 1] = 0;
				last = arc;
			}
			}
			arc = blks_1.nxtou[arc - 1];
			goto L502;
		}

		blks2_1.fpushb[term - 1] = 0;
		arc = blks_1.fin[term - 1];
	L504:
		if (arc > 0)
		{
			if (arrayit_1.x[arc - 1] > 0)
			{
			if (blks2_1.fpushb[term - 1] == 0)
			{
				blks2_1.fpushb[term - 1] = arc;
				blks2_1.nxtpushb[arc - 1] = 0;
				last = arc;
			}
			else
			{
				blks2_1.nxtpushb[last - 1] = arc;
				blks2_1.nxtpushb[arc - 1] = 0;
				last = arc;
			}
			}
			arc = blks_1.nxtin[arc - 1];
			goto L504;
		}
		goto L600;
		}

	/*     SPECULATIVE PATH EXTENSION ATTEMPT */
	/*     NOTE: ARC>0 MEANS THAT ARC IS ORIENTED FROM THE ROOT TO THE DESTINATIONS */
	/*     ARC<0 MEANS THAT ARC IS ORIENTED FROM THE DESTINATIONS TO THE ROOT */
	/*     EXTARC=0 OR PRDARC=0, MEANS THE EXTENSION ARC OR THE PREDECESSOR ARC, */
	/*     RESPECTIVELY, HAS NOT BEEN ESTABLISHED */

	/* L510: */
		if (extarc > 0)
		{
		if (arrayit_1.u[extarc - 1] == 0)
		{
			seclevel = blks3_1.sb_level__[term - 1];
			goto L580;
		}
		end = arrayit_1.endn[extarc - 1];
		bstlevel = blks_1.label[end - 1] + arrayit_1.rc[extarc - 1];
		if (pterm >= bstlevel)
		{
			if (blks2_1.path_id__[end - 1])
			{
			goto L1200;
			}
			term = end;
			blks_1.prdcsr[term - 1] = extarc;
			blks2_1.path_id__[term - 1] = TRUE_;

	/*     IF NEGATIVE SURPLUS NODE IS FOUND, DO AN AUGMENTATION */

			if (arrayit_1.dfct[term - 1] > 0)
			{
			goto L2000;
			}

	/*     RETURN FOR ANOTHER ITERATION */

			goto L500;
		}
		}
		else
		{
		extarc = -extarc;
		if (arrayit_1.x[extarc - 1] == 0)
		{
			seclevel = blks3_1.sb_level__[term - 1];
			goto L580;
		}
		start = arrayit_1.startn[extarc - 1];
		bstlevel = blks_1.label[start - 1] - arrayit_1.rc[extarc - 1];
		if (pterm >= bstlevel)
		{
			if (blks2_1.path_id__[start - 1])
			{
			goto L1200;
			}
			term = start;
			blks_1.prdcsr[term - 1] = -extarc;
			blks2_1.path_id__[term - 1] = TRUE_;

	/*     IF NEGATIVE SURPLUS NODE IS FOUND, DO AN AUGMENTATION */

			if (arrayit_1.dfct[term - 1] > 0)
			{
			goto L2000;
			}

	/*     RETURN FOR ANOTHER ITERATION */

			goto L500;
		}
		}

	/*     SECOND BEST LOGIC TEST APPLIED TO SAVE A FULL NODE SCAN */
	/*     IF OLD BEST LEVEL CONTINUES TO BE BEST GO FOR ANOTHER CONTRACTION */

	L550:
		seclevel = blks3_1.sb_level__[term - 1];
		if (bstlevel <= seclevel)
		{
		goto L800;
		}

	/*     IF SECOND BEST CAN BE USED DO EITHER A CONTRACTION */
	/*     OR START OVER WITH A SPECULATIVE EXTENSION */

	L580:
		if (seclevel > -input_1.large)
		{
		extarc = blks3_1.sb_arc__[term - 1];
		if (extarc > 0)
		{
			if (arrayit_1.u[extarc - 1] == 0)
			{
			goto L600;
			}
			bstlevel = blks_1.label[arrayit_1.endn[extarc - 1] - 1] + arrayit_1.rc[extarc - 1];
		}
		else
		{
			if (arrayit_1.x[-extarc - 1] == 0)
			{
			goto L600;
			}
			bstlevel = blks_1.label[arrayit_1.startn[-extarc - 1] - 1] - arrayit_1.rc[-extarc - 1];
		}
		if (bstlevel == seclevel)
		{
			blks3_1.sb_level__[term - 1] = -input_1.large;
			blks3_1.extend_arc__[term - 1] = extarc;
			goto L800;
		}
		}

	/*     EXTENSION/CONTRACTION ATTEMPT WAS UNSUCCESSFUL, SO SCAN TERMINAL NODE */

	L600:
		++output_1.nsp;
		bstlevel = input_1.large;
		seclevel = input_1.large;
		arc = blks2_1.fpushf[term - 1];
	L700:
		if (arc > 0)
		{
		new_level__ = blks_1.label[arrayit_1.endn[arc - 1] - 1] + arrayit_1.rc[arc - 1];
		if (new_level__ < seclevel)
		{
			if (new_level__ < bstlevel)
			{
			seclevel = bstlevel;
			bstlevel = new_level__;
			secarc = extarc;
			extarc = arc;
			}
			else
			{
			seclevel = new_level__;
			secarc = arc;
			}
		}
		arc = blks2_1.nxtpushf[arc - 1];
		goto L700;
		}
		arc = blks2_1.fpushb[term - 1];
	L710:
		if (arc > 0)
		{
		new_level__ = blks_1.label[arrayit_1.startn[arc - 1] - 1] - arrayit_1.rc[arc - 1];
		if (new_level__ < seclevel)
		{
			if (new_level__ < bstlevel)
			{
			seclevel = bstlevel;
			bstlevel = new_level__;
			secarc = extarc;
			extarc = -arc;
			}
			else
			{
			seclevel = new_level__;
			secarc = -arc;
			}
		}
		arc = blks2_1.nxtpushb[arc - 1];
		goto L710;
		}
		blks3_1.sb_level__[term - 1] = seclevel;
		blks3_1.sb_arc__[term - 1] = secarc;
		blks3_1.extend_arc__[term - 1] = extarc;

	/*     END OF NODE SCAN. */
	/*     IF THE TERMINAL NODE IS THE ROOT, ADJUST ITS PRICE AND CHANGE ROOT */

	L800:
		if (term == root)
		{
		blks_1.label[term - 1] = bstlevel + eps;
		if (pterm >= input_1.large)
		{
			   //printf("NO PATH TO THE DESTINATION\n");
			   //printf("PROBLEM IS FOUND TO BE INFEASIBLE.\n");
			   //printf("PROGRAM ENDED; PRESS <CR> TO EXIT'\n");
	/*         PRINT*,'NO PATH TO THE DESTINATION' */
	/*         PRINT*,' PROBLEM IS FOUND TO BE INFEASIBLE.' */
	/*         PRINT*, 'PROGRAM ENDED; PRESS <CR> TO EXIT' */
	/*         PAUSE */
			//exit(0);

			   Console.WriteLine("NO PATH FOUND TO THE DESTINATION BY AUCTION:");
			   Console.WriteLine("   EPSILON = {0} AT SCALING PHRASE = {1}.", eps.ToString(), pass.ToString());
			   //Console::WriteLine(S"***********************************");
			   input_1.crash = 0;
			   goto L4000;

		}
		blks2_1.path_id__[root - 1] = FALSE_;
		prevnode = root;
		root = blks3_1.nxtqueue[root - 1];
		goto L200;
		}

	/*     CHECK WHETHER EXTENSION OR CONTRACTION */

		prd = blks_1.prdcsr[term - 1];
		if (prd > 0)
		{
		pr_term__ = arrayit_1.startn[prd - 1];
		prevlevel = blks_1.label[pr_term__ - 1] - arrayit_1.rc[prd - 1];
		}
		else
		{
		pr_term__ = arrayit_1.endn[-prd - 1];
		prevlevel = blks_1.label[pr_term__ - 1] + arrayit_1.rc[-prd - 1];
		}

		if (prevlevel > bstlevel)
		{

	/*     PATH EXTENSION */

		if (prevlevel >= bstlevel + eps)
		{
			blks_1.label[term - 1] = bstlevel + eps;
		}
		else
		{
			blks_1.label[term - 1] = prevlevel;
		}
		if (extarc > 0)
		{
			end = arrayit_1.endn[extarc - 1];
			if (blks2_1.path_id__[end - 1])
			{
			goto L1200;
			}
			term = end;
		}
		else
		{
			start = arrayit_1.startn[-extarc - 1];
			if (blks2_1.path_id__[start - 1])
			{
			goto L1200;
			}
			term = start;
		}
		blks_1.prdcsr[term - 1] = extarc;
		blks2_1.path_id__[term - 1] = TRUE_;

	/*     IF NEGATIVE SURPLUS NODE IS FOUND, DO AN AUGMENTATION */

		if (arrayit_1.dfct[term - 1] > 0)
		{
			goto L2000;
		}

	/*     RETURN FOR ANOTHER ITERATION */

		goto L500;
		}
		else
		{

	/*     PATH CONTRACTION. */

		blks_1.label[term - 1] = bstlevel + eps;
		blks2_1.path_id__[term - 1] = FALSE_;
		term = pr_term__;
		if (pr_term__ != root)
		{
			if (bstlevel <= pterm + eps)
			{
			goto L2000;
			}
		}
		pterm = blks_1.label[term - 1];
		extarc = prd;
		if (prd > 0)
		{
			bstlevel = bstlevel + eps + arrayit_1.rc[prd - 1];
		}
		else
		{
			bstlevel = bstlevel + eps - arrayit_1.rc[-prd - 1];
		}

	/*     DO A SECOND BEST TEST AND IF THAT FAILS, DO A FULL NODE SCAN */

		goto L550;
		}

	/*     A CYCLE IS ABOUT TO FORM; DO A RETREAT SEQUENCE. */

	L1200:

		node = term;
	L1600:
		if (node != root)
		{
		blks2_1.path_id__[node - 1] = FALSE_;
		prd = blks_1.prdcsr[node - 1];
		if (prd > 0)
		{
			pr_term__ = arrayit_1.startn[prd - 1];
			if (blks_1.label[pr_term__ - 1] == blks_1.label[node - 1] + arrayit_1.rc[prd - 1] + eps)
			{
			node = pr_term__;
			goto L1600;
			}
		}
		else
		{
			pr_term__ = arrayit_1.endn[-prd - 1];
			if (blks_1.label[pr_term__ - 1] == blks_1.label[node - 1] - arrayit_1.rc[-prd - 1] + eps)
			{
			node = pr_term__;
			goto L1600;
			}
		}

	/*     DO A FULL SCAN AND PRICE RISE AT PR_TERM */

		++output_1.nsp;
		bstlevel = input_1.large;
		seclevel = input_1.large;
		arc = blks2_1.fpushf[pr_term__ - 1];
	L1700:
		if (arc > 0)
		{
			new_level__ = blks_1.label[arrayit_1.endn[arc - 1] - 1] + arrayit_1.rc[arc - 1];
			if (new_level__ < seclevel)
			{
			if (new_level__ < bstlevel)
			{
				seclevel = bstlevel;
				bstlevel = new_level__;
				secarc = extarc;
				extarc = arc;
			}
			else
			{
				seclevel = new_level__;
				secarc = arc;
			}
			}
			arc = blks2_1.nxtpushf[arc - 1];
			goto L1700;
		}

		arc = blks2_1.fpushb[pr_term__ - 1];
	L1710:
		if (arc > 0)
		{
			new_level__ = blks_1.label[arrayit_1.startn[arc - 1] - 1] - arrayit_1.rc[arc - 1];
			if (new_level__ < seclevel)
			{
			if (new_level__ < bstlevel)
			{
				seclevel = bstlevel;
				bstlevel = new_level__;
				secarc = extarc;
				extarc = -arc;
			}
			else
			{
				seclevel = new_level__;
				secarc = -arc;
			}
			}
			arc = blks2_1.nxtpushb[arc - 1];
			goto L1710;
		}
		blks3_1.sb_level__[pr_term__ - 1] = seclevel;
		blks3_1.sb_arc__[pr_term__ - 1] = secarc;
		blks3_1.extend_arc__[pr_term__ - 1] = extarc;
		blks_1.label[pr_term__ - 1] = bstlevel + eps;
		if (pr_term__ == root)
		{
			prevnode = root;
			blks2_1.path_id__[root - 1] = FALSE_;
			root = blks3_1.nxtqueue[root - 1];
			goto L200;
		}
		blks2_1.path_id__[pr_term__ - 1] = FALSE_;
		prd = blks_1.prdcsr[pr_term__ - 1];
		if (prd > 0)
		{
			term = arrayit_1.startn[prd - 1];
		}
		else
		{
			term = arrayit_1.endn[-prd - 1];
		}
		if (term == root)
		{
			prevnode = root;
			blks2_1.path_id__[root - 1] = FALSE_;
			root = blks3_1.nxtqueue[root - 1];
			goto L200;
		}
		else
		{
			goto L2000;
		}
		}

	/*     END OF AUCTION/SHORTEST PATH ROUTINE. */
	/*     DO AUGMENTATION FROM ROOT AND CORRECT THE PUSH LISTS */

	L2000:
		incr = -arrayit_1.dfct[root - 1];
		node = root;
	L2050:
		extarc = blks3_1.extend_arc__[node - 1];
		blks2_1.path_id__[node - 1] = FALSE_;
		if (extarc > 0)
		{
		node = arrayit_1.endn[extarc - 1];
		if (incr > arrayit_1.u[extarc - 1])
		{
			incr = arrayit_1.u[extarc - 1];
		}
		}
		else
		{
		node = arrayit_1.startn[-extarc - 1];
		if (incr > arrayit_1.x[-extarc - 1])
		{
			incr = arrayit_1.x[-extarc - 1];
		}
		}
		if (node != term)
		{
		goto L2050;
		}
		blks2_1.path_id__[term - 1] = FALSE_;
		if (arrayit_1.dfct[term - 1] > 0)
		{
		if (incr > arrayit_1.dfct[term - 1])
		{
			incr = arrayit_1.dfct[term - 1];
		}
		}

		node = root;
	L2100:
		extarc = blks3_1.extend_arc__[node - 1];
		if (extarc > 0)
		{
		end = arrayit_1.endn[extarc - 1];

	/*     ADD ARC TO THE REDUCED GRAPH */

		if (arrayit_1.x[extarc - 1] == 0)
		{
			blks2_1.nxtpushb[extarc - 1] = blks2_1.fpushb[end - 1];
			blks2_1.fpushb[end - 1] = extarc;
			new_level__ = blks_1.label[node - 1] - arrayit_1.rc[extarc - 1];
			if (blks3_1.sb_level__[end - 1] > new_level__)
			{
			blks3_1.sb_level__[end - 1] = new_level__;
			blks3_1.sb_arc__[end - 1] = -extarc;
			}
		}
		arrayit_1.x[extarc - 1] += incr;
		arrayit_1.u[extarc - 1] -= incr;

	/*    REMOVE ARC FROM THE REDUCED GRAPH */

		if (arrayit_1.u[extarc - 1] == 0)
		{
			++nas;
			arc = blks2_1.fpushf[node - 1];
			if (arc == extarc)
			{
			blks2_1.fpushf[node - 1] = blks2_1.nxtpushf[arc - 1];
			}
			else
			{
			prevarc = arc;
			arc = blks2_1.nxtpushf[arc - 1];
	L2200:
			if (arc > 0)
			{
				if (arc == extarc)
				{
				blks2_1.nxtpushf[prevarc - 1] = blks2_1.nxtpushf[arc - 1];
				goto L2250;
				}
				prevarc = arc;
				arc = blks2_1.nxtpushf[arc - 1];
				goto L2200;
			}
			}
		}
	L2250:
		node = end;
		}
		else
		{
		extarc = -extarc;
		start = arrayit_1.startn[extarc - 1];

	/*    ADD ARC TO THE REDUCED GRAPH */

		if (arrayit_1.u[extarc - 1] == 0)
		{
			blks2_1.nxtpushf[extarc - 1] = blks2_1.fpushf[start - 1];
			blks2_1.fpushf[start - 1] = extarc;
			new_level__ = blks_1.label[node - 1] + arrayit_1.rc[extarc - 1];
			if (blks3_1.sb_level__[start - 1] > new_level__)
			{
			blks3_1.sb_level__[start - 1] = new_level__;
			blks3_1.sb_arc__[start - 1] = extarc;
			}
		}
		arrayit_1.u[extarc - 1] += incr;
		arrayit_1.x[extarc - 1] -= incr;

	/*    REMOVE ARC FROM THE REDUCED GRAPH */

		if (arrayit_1.x[extarc - 1] == 0)
		{
			++nas;
			arc = blks2_1.fpushb[node - 1];
			if (arc == extarc)
			{
			blks2_1.fpushb[node - 1] = blks2_1.nxtpushb[arc - 1];
			}
			else
			{
			prevarc = arc;
			arc = blks2_1.nxtpushb[arc - 1];
	L2300:
			if (arc > 0)
			{
				if (arc == extarc)
				{
				blks2_1.nxtpushb[prevarc - 1] = blks2_1.nxtpushb[arc - 1];
				goto L2350;
				}
				prevarc = arc;
				arc = blks2_1.nxtpushb[arc - 1];
				goto L2300;
			}
			}
		}
	L2350:
		node = start;
		}
		if (node != term)
		{
		goto L2100;
		}
		arrayit_1.dfct[term - 1] -= incr;
		arrayit_1.dfct[root - 1] += incr;

	/*     INSERT TERM IN THE QUEUE IF IT HAS A LARGE ENOUGH SURPLUS */

		if (arrayit_1.dfct[term - 1] < thresh_dfct__)
		{
		if (blks3_1.nxtqueue[term - 1] == 0)
		{
			nxtnode = blks3_1.nxtqueue[root - 1];
			if (blks_1.label[term - 1] >= blks_1.label[nxtnode - 1] && root != nxtnode)
			{
			blks3_1.nxtqueue[root - 1] = term;
			blks3_1.nxtqueue[term - 1] = nxtnode;
			}
			else
			{
			blks3_1.nxtqueue[prevnode - 1] = term;
			blks3_1.nxtqueue[term - 1] = root;
			prevnode = term;
			}
		}
		}

	/*     IF ROOT HAS A LARGE ENOUGH SURPLUS, KEEP IT */
	/*     IN THE QUEUE AND RETURN FOR ANOTHER ITERATION */

		if (arrayit_1.dfct[root - 1] < thresh_dfct__)
		{
		prevnode = root;
		root = blks3_1.nxtqueue[root - 1];
		goto L200;
		}

	/*     END OF AUGMENTATION CYCLE */

	L3000:

	/*     CHECK FOR TERMINATION OF SCALING PHASE. IF SCALING PHASE IS */
	/*     NOT FINISHED, ADVANCE THE QUEUE AND RETURN TO TAKE ANOTHER NODE. */

		nxtnode = blks3_1.nxtqueue[root - 1];
		if (root != nxtnode)
		{
		blks3_1.nxtqueue[root - 1] = 0;
		blks3_1.nxtqueue[prevnode - 1] = nxtnode;
		root = nxtnode;
		goto L200;
		}

	/*     END OF SUBPROBLEM (SCALING PHASE). */

	/* L3600: */
	/*     IF ANOTHER AUCTION SCALING PHASE REMAINS, RESET THE FLOWS & THE PUSH LISTS */
	/*     ELSE RESET ARC FLOWS TO SATISFY CS AND COMPUTE REDUCED COSTS */   
		if (input_1.crash == 1)
		{
			// Save auction result before adjusting any value, added by ST on 10/09/2009.
			GlobalMembersRelax4.SaveAuctionValues(pass, eps, TRUE_);

			/*     REDUCE EPSILON. */
			eps /= factor;
			if (eps < 1)
			{
				eps = 1;
			}
			thresh_dfct__ /= factor;
			if (eps == 1)
			{
				thresh_dfct__ = 0;
			}

			i__1 = input_1.na;
			for (arc = 1; arc <= i__1; ++arc)
			{
				start = arrayit_1.startn[arc - 1];
				end = arrayit_1.endn[arc - 1];
				pstart = blks_1.label[start - 1];
				pend = blks_1.label[end - 1];
				red_cost__ = arrayit_1.rc[arc - 1] + pend - pstart;
				if (0 > red_cost__ + eps)
				{
					resid = arrayit_1.u[arc - 1];
					if (resid > 0)
					{
						arrayit_1.dfct[start - 1] += resid;
						arrayit_1.dfct[end - 1] -= resid;
						arrayit_1.x[arc - 1] += resid;
						arrayit_1.u[arc - 1] = 0;
					}
				}
				else
				{
					if (0 < red_cost__ - eps)
					{
						flow = arrayit_1.x[arc - 1];
						if (flow > 0)
						{
							arrayit_1.dfct[start - 1] -= flow;
							arrayit_1.dfct[end - 1] += flow;
							arrayit_1.x[arc - 1] = 0;
							arrayit_1.u[arc - 1] += flow;
						}
					}
				}
	/* L3800: */
			}

	/*     RETURN FOR ANOTHER PHASE */
	/* L3850: */
		   goto L100;
		}
		else
		{
			GlobalMembersRelax4.SaveAuctionValues(pass, eps, FALSE_);

			input_1.crash = 1;
			i__1 = input_1.na;
			for (arc = 1; arc <= i__1; ++arc)
			{
				start = arrayit_1.startn[arc - 1];
				end = arrayit_1.endn[arc - 1];
				pstart = blks_1.label[start - 1];
				pend = blks_1.label[end - 1];

				red_cost__ = arrayit_1.rc[arc - 1] + pend - pstart;
				if (red_cost__ < 0)
				{
					resid = arrayit_1.u[arc - 1];
					if (resid > 0)
					{
						arrayit_1.dfct[start - 1] += resid;
						arrayit_1.dfct[end - 1] -= resid;
						arrayit_1.x[arc - 1] += resid;
						arrayit_1.u[arc - 1] = 0;
					}
				}
				else
				{
					if (red_cost__ > 0)
					{
						flow = arrayit_1.x[arc - 1];
						if (flow > 0)
						{
							arrayit_1.dfct[start - 1] -= flow;
							arrayit_1.dfct[end - 1] += flow;
							arrayit_1.x[arc - 1] = 0;
							arrayit_1.u[arc - 1] += flow;
						}
					}
				}
				arrayit_1.rc[arc - 1] = red_cost__;
	/* L3900: */
			}
		}
	L4000:
		/* This section was added by ST on 10/09/2009.
		// It will used to step back one pass before auction failed. */
		if (input_1.crash == 0)
		{
			input_1.crash = 1;
			GlobalMembersRelax4.RecallAuctionValues();
		}
		return 0;
	 } // auction_
//C++ TO C# CONVERTER NOTE: This was formerly a static local variable declaration (not allowed in C#):
private static long ascnt1__i__1;
//C++ TO C# CONVERTER NOTE: This was formerly a static local variable declaration (not allowed in C#):
private static long ascnt1__node;
long node2;
long i__;
long j;
long nsave;
long t1;
long t2;
long t3;
long nb;
long delprc;
long rdcost;
long arc;
long dlx;



	public static int ascnt1_(long dm, long delx, long nlabel, logical1 feasbl, logical1 switch__, long nscan, long curnode, long prevnode)
	{
		/* System generated locals */
	//C++ TO C# CONVERTER NOTE: This static local variable declaration (not allowed in C#) has been moved just prior to the method:
	//	static long i__1;

		/* Local variables */
	//C++ TO C# CONVERTER NOTE: This static local variable declaration (not allowed in C#) has been moved just prior to the method:
	//	static long node, node2, i__, j, nsave, t1, t2, t3, nb, delprc, rdcost, arc, dlx;


	/* --------------------------------------------------------------- */

	/*  PURPOSE - THIS SUBROUTINE PERFORMS THE MULTI-NODE PRICE */
	/*     ADJUSTMENT STEP FOR THE CASE WHERE THE SCANNED NODES */
	/*     HAVE POSITIVE DEFICIT.  IT FIRST CHECKS IF DECREASING */
	/*     THE PRICE OF THE SCANNED NODES INCREASES THE DUAL COST. */
	/*     IF YES, THEN IT DECREASES THE PRICE OF ALL SCANNED NODES. */
	/*     THERE ARE TWO POSSIBILITIES FOR PRICE DECREASE: */
	/*     IF SWITCH=.TRUE., THEN THE SET OF SCANNED NODES */
	/*     CORRESPONDS TO AN ELEMENTARY DIRECTION OF MAXIMAL */
	/*     RATE OF ASCENT, IN WHICH CASE THE PRICE OF ALL SCANNED */
	/*     NODES ARE DECREASED UNTIL THE NEXT BREAKPOINT IN THE */
	/*     DUAL COST IS ENCOUNTERED.  AT THIS POINT, SOME ARC */
	/*     BECOMES BALANCED AND MORE NODE(S) ARE ADDED TO THE */
	/*     LABELED SET AND THE SUBROUTINE IS EXITED. */
	/*     IF SWITCH=.FALSE., THEN THE PRICE OF ALL SCANNED NODES */
	/*     ARE DECREASED UNTIL THE RATE OF ASCENT BECOMES */
	/*     NEGATIVE (THIS CORRESPONDS TO THE PRICE ADJUSTMENT */
	/*     STEP IN WHICH BOTH THE LINE SEARCH AND THE DEGENERATE */
	/*     ASCENT ITERATION ARE IMPLEMENTED). */

	/* --------------------------------------------------------------- */

	/*     MAXNN = DIMENSION OF NODE-LENGTH ARRAYS */
	/*     MAXNA = DIMENSION OF ARC-LENGTH ARRAYS */


	/* ^^                                     B                     ^^ */
	/* ^^          TEMPIN I1 P,TEMPOU PRICE I2,I3,I4,I5,I6,I7        ^^ */
	/* ^^                      MARK   TFSTOU  TNXTOU  TFSTIN TNXTIN ^^ */
	/* ^^                  I14      I15        I16     I17          ^^ */

	/*  INPUT PARAMETERS */

	/*     DM        = TOTAL DEFICIT OF SCANNED NODES */
	/*     SWITCH    = .TRUE. IF LABELING IS TO CONTINUE AFTER PRICE CHANGE */
	/*     NSCAN     = NUMBER OF SCANNED NODES */
	/*     CURNODE   = MOST RECENTLY SCANNED NODE */
	/*     N         = NUMBER OF NODES */
	/*     NA        = NUMBER OF ARCS */
	/*     LARGE     = A VERY LARGE INTEGER TO REPRESENT INFINITY */
	/*                 (SEE NOTE 3) */
	/*     STARTN(I) = STARTING NODE FOR THE I-TH ARC,    I = 1,...,NA */
	/*     ENDN(I)   = ENDING NODE FOR THE I-TH ARC,      I = 1,...,NA */
	/*     FOU(I)    = FIRST ARC LEAVING I-TH NODE,       I = 1,...,N */
	/*     NXTOU(I)  = NEXT ARC LEAVING THE STARTING NODE OF J-TH ARC, */
	/*                                                    I = 1,...,NA */
	/*     FIN(I)    = FIRST ARC ENTERING I-TH NODE,      I = 1,...,N */
	/*     NXTIN(I)  = NEXT ARC ENTERING THE ENDING NODE OF J-TH ARC, */
	/*                                                    I = 1,...,NA */


	/*  UPDATED PARAMETERS */

	/*     DELX      = A LOWER ESTIMATE OF THE TOTAL FLOW ON BALANCED ARCS */
	/*                 IN THE SCANNED-NODES CUT */
	/*     NLABEL    = NUMBER OF LABELED NODES */
	/*     FEASBL    = .FALSE. IF PROBLEM IS FOUND TO BE INFEASIBLE */
	/*     PREVNODE  = THE NODE BEFORE CURNODE IN QUEUE */
	/*     RC(J)     = REDUCED COST OF ARC J,             J = 1,...,NA */
	/*     U(J)      = RESIDUAL CAPACITY OF ARC J, */
	/*                                                    J = 1,...,NA */
	/*     X(J)      = FLOW ON ARC J,                     J = 1,...,NA */
	/*     DFCT(I)   = DEFICIT AT NODE I,                 I = 1,...,N */
	/*     LABEL(K)  = K-TH NODE LABELED,                 K = 1,NLABEL */
	/*     PRDCSR(I) = PREDECESSOR OF NODE I IN TREE OF LABELED NODES */
	/*                 (O IF I IS UNLABELED),             I = 1,...,N */
	/*     FPUSHF(I) = FIRST BALANCED ARC OUT OF NODE I,  I = 1,...,N */
	/*     NXTPUSHF(J) = NEXT BALANCED ARC OUT OF THE STARTING NODE OF ARC J, */
	/*                                                    J = 1,...,NA */
	/*     FPUSHB(I) = FIRST BALANCED ARC INTO NODE I,  I = 1,...,N */
	/*     NXTPUSHB(J) = NEXT BALANCED ARC INTO THE ENDING NODE OF ARC J, */
	/*                                                    J = 1,...,NA */
	/*     NXTQUEUE(I) = NODE FOLLOWING NODE I IN THE FIFO QUEUE */
	/*                   (0 IF NODE IS NOT IN THE QUEUE), I = 1,...,N */
	/*     SCAN(I)   = .TRUE. IF NODE I IS SCANNED,       I = 1,...,N */
	/*     PATH_ID(I)   = .TRUE. IF NODE I IS LABELED,       I = 1,...,N */


	/*  WORKING PARAMETERS */


	/*     STORE THE ARCS BETWEEN THE SET OF SCANNED NODES AND */
	/*     ITS COMPLEMENT IN SAVE AND COMPUTE DELPRC, THE STEPSIZE */
	/*     TO THE NEXT BREAKPOINT IN THE DUAL COST IN THE DIRECTION */
	/*     OF DECREASING PRICES OF THE SCANNED NODES. */
	/*     [THE ARCS ARE STORED INTO SAVE BY LOOKING AT THE ARCS */
	/*     INCIDENT TO EITHER THE SET OF SCANNED NODES OR ITS */
	/*     COMPLEMENT, DEPENDING ON WHETHER NSCAN>N/2 OR NOT. */
	/*     THIS IMPROVES THE EFFICIENCY OF STORING.] */

		delprc = input_1.large;
		dlx = 0;
		nsave = 0;
		if (nscan <= input_1.n / 2)
		{
		ascnt1__i__1 = nscan;
		for (i__ = 1; i__ <= ascnt1__i__1; ++i__)
		{
			ascnt1__node = blks_1.label[i__ - 1];
			arc = blks_1.fou[ascnt1__node - 1];
	L500:
			if (arc > 0)
			{

	/*     ARC POINTS FROM SCANNED NODE TO AN UNSCANNED NODE. */

			node2 = arrayit_1.endn[arc - 1];
			if (!blks2_1.scan[node2 - 1])
			{
				++nsave;
				blks_1.save[nsave - 1] = arc;
				rdcost = arrayit_1.rc[arc - 1];
				if (rdcost == 0 && blks_1.prdcsr[node2 - 1] != arc)
				{
				dlx += arrayit_1.x[arc - 1];
				}
				if (rdcost < 0 && rdcost > -delprc)
				{
				delprc = -rdcost;
				}
			}
			arc = blks_1.nxtou[arc - 1];
			goto L500;
			}
			arc = blks_1.fin[ascnt1__node - 1];
	L501:
			if (arc > 0)
			{

	/*     ARC POINTS FROM UNSCANNED NODE TO SCANNED NODE. */

			node2 = arrayit_1.startn[arc - 1];
			if (!blks2_1.scan[node2 - 1])
			{
				++nsave;
				blks_1.save[nsave - 1] = -arc;
				rdcost = arrayit_1.rc[arc - 1];
				if (rdcost == 0 && blks_1.prdcsr[node2 - 1] != -arc)
				{
				dlx += arrayit_1.u[arc - 1];
				}
				if (rdcost > 0 && rdcost < delprc)
				{
				delprc = rdcost;
				}
			}
			arc = blks_1.nxtin[arc - 1];
			goto L501;
			}
	/* L1: */
		}
		}
		else
		{
		ascnt1__i__1 = input_1.n;
		for (ascnt1__node = 1; ascnt1__node <= ascnt1__i__1; ++ascnt1__node)
		{
			if (blks2_1.scan[ascnt1__node - 1])
			{
			goto L2;
			}
			arc = blks_1.fin[ascnt1__node - 1];
	L502:
			if (arc > 0)
			{
			node2 = arrayit_1.startn[arc - 1];
			if (blks2_1.scan[node2 - 1])
			{
				++nsave;
				blks_1.save[nsave - 1] = arc;
				rdcost = arrayit_1.rc[arc - 1];
				if (rdcost == 0 && blks_1.prdcsr[ascnt1__node - 1] != arc)
				{
				dlx += arrayit_1.x[arc - 1];
				}
				if (rdcost < 0 && rdcost > -delprc)
				{
				delprc = -rdcost;
				}
			}
			arc = blks_1.nxtin[arc - 1];
			goto L502;
			}
			arc = blks_1.fou[ascnt1__node - 1];
	L503:
			if (arc > 0)
			{
			node2 = arrayit_1.endn[arc - 1];
			if (blks2_1.scan[node2 - 1])
			{
				++nsave;
				blks_1.save[nsave - 1] = -arc;
				rdcost = arrayit_1.rc[arc - 1];
				if (rdcost == 0 && blks_1.prdcsr[ascnt1__node - 1] != -arc)
				{
				dlx += arrayit_1.u[arc - 1];
				}
				if (rdcost > 0 && rdcost < delprc)
				{
				delprc = rdcost;
				}
			}
			arc = blks_1.nxtou[arc - 1];
			goto L503;
			}
	L2:
			;
		}
		}

	/*     CHECK IF THE SET OF SCANNED NODES TRULY CORRESPONDS */
	/*     TO A DUAL ASCENT DIRECTION.  [HERE DELX+DLX IS THE EXACT */
	/*     SUM OF THE FLOW ON ARCS FROM THE SCANNED SET TO THE */
	/*     UNSCANNED SET PLUS THE (CAPACITY - FLOW) ON ARCS FROM */
	/*     THE UNSCANNED SET TO THE SCANNED SET.] */
	/*     IF THIS WERE NOT THE CASE, SET SWITCH TO .TRUE. */
	/*     AND EXIT SUBROUTINE. */

		if (!(switch__) && delx + dlx >= dm)
		{
		switch__ = TRUE_;
		return 0;
		}
		delx += dlx;

	/*     CHECK THAT THE PROBLEM IS FEASIBLE. */

	L4:
		if (delprc == input_1.large)
		{

	/*     WE CAN INCREASE THE DUAL COST WITHOUT BOUND, SO */
	/*     THE PRIMAL PROBLEM IS INFEASIBLE. */

		feasbl = FALSE_;
		return 0;
		}

	/*     DECREASE THE PRICES OF THE SCANNED NODES, ADD MORE */
	/*     NODES TO THE LABELED SET AND CHECK IF A NEWLY LABELED NODE */
	/*     HAS NEGATIVE DEFICIT. */

		if (switch__ != null)
		{
		ascnt1__i__1 = nsave;
		for (i__ = 1; i__ <= ascnt1__i__1; ++i__)
		{
			arc = blks_1.save[i__ - 1];
			if (arc > 0)
			{
			arrayit_1.rc[arc - 1] += delprc;
			if (arrayit_1.rc[arc - 1] == 0)
			{
				node2 = arrayit_1.endn[arc - 1];
				if (blks2_1.nxtpushf[arc - 1] < 0)
				{
				blks2_1.nxtpushf[arc - 1] = blks2_1.fpushf[arrayit_1.startn[arc - 1] - 1];
				blks2_1.fpushf[arrayit_1.startn[arc - 1] - 1] = arc;
				}
				if (blks2_1.nxtpushb[arc - 1] < 0)
				{
				blks2_1.nxtpushb[arc - 1] = blks2_1.fpushb[node2 - 1];
				blks2_1.fpushb[node2 - 1] = arc;
				}
				if (!blks2_1.path_id__[node2 - 1])
				{
				blks_1.prdcsr[node2 - 1] = arc;
				++(nlabel);
				blks_1.label[nlabel - 1] = node2;
				blks2_1.path_id__[node2 - 1] = TRUE_;
				}
			}
			}
			else
			{
			arc = -arc;
			arrayit_1.rc[arc - 1] -= delprc;
			if (arrayit_1.rc[arc - 1] == 0)
			{
				node2 = arrayit_1.startn[arc - 1];
				if (blks2_1.nxtpushf[arc - 1] < 0)
				{
				blks2_1.nxtpushf[arc - 1] = blks2_1.fpushf[node2 - 1];
				blks2_1.fpushf[node2 - 1] = arc;
				}
				if (blks2_1.nxtpushb[arc - 1] < 0)
				{
				blks2_1.nxtpushb[arc - 1] = blks2_1.fpushb[arrayit_1.endn[arc - 1] - 1];
				blks2_1.fpushb[arrayit_1.endn[arc - 1] - 1] = arc;
				}
				if (!blks2_1.path_id__[node2 - 1])
				{
				blks_1.prdcsr[node2 - 1] = -arc;
				++(nlabel);
				blks_1.label[nlabel - 1] = node2;
				blks2_1.path_id__[node2 - 1] = TRUE_;
				}
			}
			}
	/* L7: */
		}
		return 0;
		}
		else
		{

	/*     DECREASE THE PRICES OF THE SCANNED NODES BY DELPRC. */
	/*     ADJUST FLOW TO MAINTAIN COMPLEMENTARY SLACKNESS WITH */
	/*     THE PRICES. */

		nb = 0;
		ascnt1__i__1 = nsave;
		for (i__ = 1; i__ <= ascnt1__i__1; ++i__)
		{
			arc = blks_1.save[i__ - 1];
			if (arc > 0)
			{
			t1 = arrayit_1.rc[arc - 1];
			if (t1 == 0)
			{
				t2 = arrayit_1.x[arc - 1];
				t3 = arrayit_1.startn[arc - 1];
				arrayit_1.dfct[t3 - 1] -= t2;
				if (blks3_1.nxtqueue[t3 - 1] == 0)
				{
				blks3_1.nxtqueue[prevnode - 1] = t3;
				blks3_1.nxtqueue[t3 - 1] = curnode;
				prevnode = t3;
				}
				t3 = arrayit_1.endn[arc - 1];
				arrayit_1.dfct[t3 - 1] += t2;
				if (blks3_1.nxtqueue[t3 - 1] == 0)
				{
				blks3_1.nxtqueue[prevnode - 1] = t3;
				blks3_1.nxtqueue[t3 - 1] = curnode;
				prevnode = t3;
				}
				arrayit_1.u[arc - 1] += t2;
				arrayit_1.x[arc - 1] = 0;
			}
			arrayit_1.rc[arc - 1] = t1 + delprc;
			if (arrayit_1.rc[arc - 1] == 0)
			{
				delx += arrayit_1.x[arc - 1];
				++nb;
				blks_1.prdcsr[nb - 1] = arc;
			}
			}
			else
			{
			arc = -arc;
			t1 = arrayit_1.rc[arc - 1];
			if (t1 == 0)
			{
				t2 = arrayit_1.u[arc - 1];
				t3 = arrayit_1.startn[arc - 1];
				arrayit_1.dfct[t3 - 1] += t2;
				if (blks3_1.nxtqueue[t3 - 1] == 0)
				{
				blks3_1.nxtqueue[prevnode - 1] = t3;
				blks3_1.nxtqueue[t3 - 1] = curnode;
				prevnode = t3;
				}
				t3 = arrayit_1.endn[arc - 1];
				arrayit_1.dfct[t3 - 1] -= t2;
				if (blks3_1.nxtqueue[t3 - 1] == 0)
				{
				blks3_1.nxtqueue[prevnode - 1] = t3;
				blks3_1.nxtqueue[t3 - 1] = curnode;
				prevnode = t3;
				}
				arrayit_1.x[arc - 1] += t2;
				arrayit_1.u[arc - 1] = 0;
			}
			arrayit_1.rc[arc - 1] = t1 - delprc;
			if (arrayit_1.rc[arc - 1] == 0)
			{
				delx += arrayit_1.u[arc - 1];
				++nb;
				blks_1.prdcsr[nb - 1] = arc;
			}
			}
	/* L6: */
		}
		}

		if (delx <= dm)
		{

	/*     THE SET OF SCANNED NODES STILL CORRESPONDS TO A */
	/*     DUAL (POSSIBLY DEGENERATE) ASCENT DIRECTON.  COMPUTE */
	/*     THE STEPSIZE DELPRC TO THE NEXT BREAKPOINT IN THE */
	/*     DUAL COST. */

		delprc = input_1.large;
		ascnt1__i__1 = nsave;
		for (i__ = 1; i__ <= ascnt1__i__1; ++i__)
		{
			arc = blks_1.save[i__ - 1];
			if (arc > 0)
			{
			rdcost = arrayit_1.rc[arc - 1];
			if (rdcost < 0 && rdcost > -delprc)
			{
				delprc = -rdcost;
			}
			}
			else
			{
			arc = -arc;
			rdcost = arrayit_1.rc[arc - 1];
			if (rdcost > 0 && rdcost < delprc)
			{
				delprc = rdcost;
			}
			}
	/* L10: */
		}
		if (delprc != input_1.large || delx < dm)
		{
			goto L4;
		}
		}

	/*     ADD NEW BALANCED ARCS TO THE SUPERSET OF BALANCED ARCS. */

		ascnt1__i__1 = nb;
		for (i__ = 1; i__ <= ascnt1__i__1; ++i__)
		{
		arc = blks_1.prdcsr[i__ - 1];
		if (blks2_1.nxtpushb[arc - 1] == -1)
		{
			j = arrayit_1.endn[arc - 1];
			blks2_1.nxtpushb[arc - 1] = blks2_1.fpushb[j - 1];
			blks2_1.fpushb[j - 1] = arc;
		}
		if (blks2_1.nxtpushf[arc - 1] == -1)
		{
			j = arrayit_1.startn[arc - 1];
			blks2_1.nxtpushf[arc - 1] = blks2_1.fpushf[j - 1];
			blks2_1.fpushf[j - 1] = arc;
		}
	/* L9: */
		}
		return 0;
	} // ascnt1_
//C++ TO C# CONVERTER NOTE: This was formerly a static local variable declaration (not allowed in C#):
private static long ascnt2__i__1;
//C++ TO C# CONVERTER NOTE: This was formerly a static local variable declaration (not allowed in C#):
private static long ascnt2__node;
long node2;
long i__;
long j;
long nsave;
long t1;
long t2;
long t3;
long nb;
long delprc;
long rdcost;
long arc;
long dlx;



	public static int ascnt2_(long dm, long delx, long nlabel, logical1 feasbl, logical1 switch__, long nscan, long curnode, long prevnode)
	{
		/* System generated locals */
	//C++ TO C# CONVERTER NOTE: This static local variable declaration (not allowed in C#) has been moved just prior to the method:
	//	static long i__1;

		/* Local variables */
	//C++ TO C# CONVERTER NOTE: This static local variable declaration (not allowed in C#) has been moved just prior to the method:
	//	static long node, node2, i__, j, nsave, t1, t2, t3, nb, delprc, rdcost, arc, dlx;


	/* --------------------------------------------------------------- */

	/*  PURPOSE - THIS ROUTINE IS ANALOGOUS TO ASCNT BUT FOR */
	/*     THE CASE WHERE THE SCANNED NODES HAVE NEGATIVE DEFICIT. */

	/* --------------------------------------------------------------- */

	/*     MAXNN = DIMENSION OF NODE-LENGTH ARRAYS */
	/*     MAXNA = DIMENSION OF ARC-LENGTH ARRAYS */


	/* ^^                                     B                     ^^ */
	/* ^^          TEMPIN I1 P,TEMPOU PRICE I2,I3,I4,I5,I6,I7        ^^ */
	/* ^^                      MARK   TFSTOU  TNXTOU  TFSTIN TNXTIN ^^ */
	/* ^^                  I14      I15        I16     I17          ^^ */


	/*     STORE THE ARCS BETWEEN THE SET OF SCANNED NODES AND */
	/*     ITS COMPLEMENT IN SAVE AND COMPUTE DELPRC, THE STEPSIZE */
	/*     TO THE NEXT BREAKPOINT IN THE DUAL COST IN THE DIRECTION */
	/*     OF INCREASING PRICES OF THE SCANNED NODES. */

		delprc = input_1.large;
		dlx = 0;
		nsave = 0;
		if (nscan <= input_1.n / 2)
		{
		ascnt2__i__1 = nscan;
		for (i__ = 1; i__ <= ascnt2__i__1; ++i__)
		{
			ascnt2__node = blks_1.label[i__ - 1];
			arc = blks_1.fin[ascnt2__node - 1];
	L500:
			if (arc > 0)
			{
			node2 = arrayit_1.startn[arc - 1];
			if (!blks2_1.scan[node2 - 1])
			{
				++nsave;
				blks_1.save[nsave - 1] = arc;
				rdcost = arrayit_1.rc[arc - 1];
				if (rdcost == 0 && blks_1.prdcsr[node2 - 1] != arc)
				{
				dlx += arrayit_1.x[arc - 1];
				}
				if (rdcost < 0 && rdcost > -delprc)
				{
				delprc = -rdcost;
				}
			}
			arc = blks_1.nxtin[arc - 1];
			goto L500;
			}
			arc = blks_1.fou[ascnt2__node - 1];
	L501:
			if (arc > 0)
			{
			node2 = arrayit_1.endn[arc - 1];
			if (!blks2_1.scan[node2 - 1])
			{
				++nsave;
				blks_1.save[nsave - 1] = -arc;
				rdcost = arrayit_1.rc[arc - 1];
				if (rdcost == 0 && blks_1.prdcsr[node2 - 1] != -arc)
				{
				dlx += arrayit_1.u[arc - 1];
				}
				if (rdcost > 0 && rdcost < delprc)
				{
				delprc = rdcost;
				}
			}
			arc = blks_1.nxtou[arc - 1];
			goto L501;
			}
	/* L1: */
		}
		}
		else
		{
		ascnt2__i__1 = input_1.n;
		for (ascnt2__node = 1; ascnt2__node <= ascnt2__i__1; ++ascnt2__node)
		{
			if (blks2_1.scan[ascnt2__node - 1])
			{
			goto L2;
			}
			arc = blks_1.fou[ascnt2__node - 1];
	L502:
			if (arc > 0)
			{
			node2 = arrayit_1.endn[arc - 1];
			if (blks2_1.scan[node2 - 1])
			{
				++nsave;
				blks_1.save[nsave - 1] = arc;
				rdcost = arrayit_1.rc[arc - 1];
				if (rdcost == 0 && blks_1.prdcsr[ascnt2__node - 1] != arc)
				{
				dlx += arrayit_1.x[arc - 1];
				}
				if (rdcost < 0 && rdcost > -delprc)
				{
				delprc = -rdcost;
				}
			}
			arc = blks_1.nxtou[arc - 1];
			goto L502;
			}
			arc = blks_1.fin[ascnt2__node - 1];
	L503:
			if (arc > 0)
			{
			node2 = arrayit_1.startn[arc - 1];
			if (blks2_1.scan[node2 - 1])
			{
				++nsave;
				blks_1.save[nsave - 1] = -arc;
				rdcost = arrayit_1.rc[arc - 1];
				if (rdcost == 0 && blks_1.prdcsr[ascnt2__node - 1] != -arc)
				{
				dlx += arrayit_1.u[arc - 1];
				}
				if (rdcost > 0 && rdcost < delprc)
				{
				delprc = rdcost;
				}
			}
			arc = blks_1.nxtin[arc - 1];
			goto L503;
			}
	L2:
			;
		}
		}

		if (!(switch__) && delx + dlx >= -(dm))
		{
		switch__ = TRUE_;
		return 0;
		}
		delx += dlx;

	/*     CHECK THAT THE PROBLEM IS FEASIBLE. */

	L4:
		if (delprc == input_1.large)
		{
		feasbl = FALSE_;
		return 0;
		}

	/*     INCREASE THE PRICES OF THE SCANNED NODES, ADD MORE */
	/*     NODES TO THE LABELED SET AND CHECK IF A NEWLY LABELED NODE */
	/*     HAS POSITIVE DEFICIT. */

		if (switch__ != null)
		{
		ascnt2__i__1 = nsave;
		for (i__ = 1; i__ <= ascnt2__i__1; ++i__)
		{
			arc = blks_1.save[i__ - 1];
			if (arc > 0)
			{
			arrayit_1.rc[arc - 1] += delprc;
			if (arrayit_1.rc[arc - 1] == 0)
			{
				node2 = arrayit_1.startn[arc - 1];
				if (blks2_1.nxtpushf[arc - 1] < 0)
				{
				blks2_1.nxtpushf[arc - 1] = blks2_1.fpushf[node2 - 1];
				blks2_1.fpushf[node2 - 1] = arc;
				}
				if (blks2_1.nxtpushb[arc - 1] < 0)
				{
				blks2_1.nxtpushb[arc - 1] = blks2_1.fpushb[arrayit_1.endn[arc - 1] - 1];
				blks2_1.fpushb[arrayit_1.endn[arc - 1] - 1] = arc;
				}
				if (!blks2_1.path_id__[node2 - 1])
				{
				blks_1.prdcsr[node2 - 1] = arc;
				++(nlabel);
				blks_1.label[nlabel - 1] = node2;
				blks2_1.path_id__[node2 - 1] = TRUE_;
				}
			}
			}
			else
			{
			arc = -arc;
			arrayit_1.rc[arc - 1] -= delprc;
			if (arrayit_1.rc[arc - 1] == 0)
			{
				node2 = arrayit_1.endn[arc - 1];
				if (blks2_1.nxtpushf[arc - 1] < 0)
				{
				blks2_1.nxtpushf[arc - 1] = blks2_1.fpushf[arrayit_1.startn[arc - 1] - 1];
				blks2_1.fpushf[arrayit_1.startn[arc - 1] - 1] = arc;
				}
				if (blks2_1.nxtpushb[arc - 1] < 0)
				{
				blks2_1.nxtpushb[arc - 1] = blks2_1.fpushb[node2 - 1];
				blks2_1.fpushb[node2 - 1] = arc;
				}
				if (!blks2_1.path_id__[node2 - 1])
				{
				blks_1.prdcsr[node2 - 1] = -arc;
				++(nlabel);
				blks_1.label[nlabel - 1] = node2;
				blks2_1.path_id__[node2 - 1] = TRUE_;
				}
			}
			}
	/* L7: */
		}
		return 0;
		}
		else
		{
		nb = 0;
		ascnt2__i__1 = nsave;
		for (i__ = 1; i__ <= ascnt2__i__1; ++i__)
		{
			arc = blks_1.save[i__ - 1];
			if (arc > 0)
			{
			t1 = arrayit_1.rc[arc - 1];
			if (t1 == 0)
			{
				t2 = arrayit_1.x[arc - 1];
				t3 = arrayit_1.startn[arc - 1];
				arrayit_1.dfct[t3 - 1] -= t2;
				if (blks3_1.nxtqueue[t3 - 1] == 0)
				{
				blks3_1.nxtqueue[prevnode - 1] = t3;
				blks3_1.nxtqueue[t3 - 1] = curnode;
				prevnode = t3;
				}
				t3 = arrayit_1.endn[arc - 1];
				arrayit_1.dfct[t3 - 1] += t2;
				if (blks3_1.nxtqueue[t3 - 1] == 0)
				{
				blks3_1.nxtqueue[prevnode - 1] = t3;
				blks3_1.nxtqueue[t3 - 1] = curnode;
				prevnode = t3;
				}
				arrayit_1.u[arc - 1] += t2;
				arrayit_1.x[arc - 1] = 0;
			}
			arrayit_1.rc[arc - 1] = t1 + delprc;
			if (arrayit_1.rc[arc - 1] == 0)
			{
				delx += arrayit_1.x[arc - 1];
				++nb;
				blks_1.prdcsr[nb - 1] = arc;
			}
			}
			else
			{
			arc = -arc;
			t1 = arrayit_1.rc[arc - 1];
			if (t1 == 0)
			{
				t2 = arrayit_1.u[arc - 1];
				t3 = arrayit_1.startn[arc - 1];
				arrayit_1.dfct[t3 - 1] += t2;
				if (blks3_1.nxtqueue[t3 - 1] == 0)
				{
				blks3_1.nxtqueue[prevnode - 1] = t3;
				blks3_1.nxtqueue[t3 - 1] = curnode;
				prevnode = t3;
				}
				t3 = arrayit_1.endn[arc - 1];
				arrayit_1.dfct[t3 - 1] -= t2;
				if (blks3_1.nxtqueue[t3 - 1] == 0)
				{
				blks3_1.nxtqueue[prevnode - 1] = t3;
				blks3_1.nxtqueue[t3 - 1] = curnode;
				prevnode = t3;
				}
				arrayit_1.x[arc - 1] += t2;
				arrayit_1.u[arc - 1] = 0;
			}
			arrayit_1.rc[arc - 1] = t1 - delprc;
			if (arrayit_1.rc[arc - 1] == 0)
			{
				delx += arrayit_1.u[arc - 1];
				++nb;
				blks_1.prdcsr[nb - 1] = arc;
			}
			}
	/* L6: */
		}
		}

		if (delx <= -(dm))
		{
		delprc = input_1.large;
		ascnt2__i__1 = nsave;
		for (i__ = 1; i__ <= ascnt2__i__1; ++i__)
		{
			arc = blks_1.save[i__ - 1];
			if (arc > 0)
			{
			rdcost = arrayit_1.rc[arc - 1];
			if (rdcost < 0 && rdcost > -delprc)
			{
				delprc = -rdcost;
			}
			}
			else
			{
			arc = -arc;
			rdcost = arrayit_1.rc[arc - 1];
			if (rdcost > 0 && rdcost < delprc)
			{
				delprc = rdcost;
			}
			}
	/* L10: */
		}
		if (delprc != input_1.large || delx < -(dm))
		{
			goto L4;
		}
		}

	/*     ADD NEW BALANCED ARCS TO THE SUPERSET OF BALANCED ARCS. */

		ascnt2__i__1 = nb;
		for (i__ = 1; i__ <= ascnt2__i__1; ++i__)
		{
		arc = blks_1.prdcsr[i__ - 1];
		if (blks2_1.nxtpushb[arc - 1] == -1)
		{
			j = arrayit_1.endn[arc - 1];
			blks2_1.nxtpushb[arc - 1] = blks2_1.fpushb[j - 1];
			blks2_1.fpushb[j - 1] = arc;
		}
		if (blks2_1.nxtpushf[arc - 1] == -1)
		{
			j = arrayit_1.startn[arc - 1];
			blks2_1.nxtpushf[arc - 1] = blks2_1.fpushf[j - 1];
			blks2_1.fpushf[j - 1] = arc;
		}
	/* L9: */
		}
		return 0;
	} // ascnt2_
#endif
    }

    //#include <omp.h>

    //#include "debug.h"
    //#include "errmsg.h"

    public class arrayit_
    {
        public long[] startn = new long[DefineConstants.MAXLINKSRELAX]; // FLOW ON ARC J. -  CAPACITY - FLOW ON ARC J. -  REDUCED COST OF ARC J = COST(J) - (PRICE(STARTN) - PRICE(ENDN)). -  COST OF ARC J. -  DEMAND AT NODE I ON INPUT; MUST BE ZERO ON OUTPUT. -  ENDING NODE FOR ARC J. -  STARTING NODE FOR ARC J.
        public long[] endn = new long[DefineConstants.MAXLINKSRELAX];
        public long[] dfct = new long[DefineConstants.MAXNODESRELAX];
        public long[] c__ = new long[DefineConstants.MAXLINKSRELAX];
        public long[] rc = new long[DefineConstants.MAXLINKSRELAX];
        public long[] u = new long[DefineConstants.MAXLINKSRELAX];
        public long[] x = new long[DefineConstants.MAXLINKSRELAX];
    }
    public class arraysave_
    {
        // Store the information for a recall to relax.
        public long numlinks; // Previous solved flow. -  Previous remaining capacity. -  Reduced cost from prev soln. -  Previous cost as passed. -  For VERIFICATION  not change) -  For VERIFICATION (network must
        public long tcost;
        public long tprice;
        public long[] dfct = new long[DefineConstants.MAXNODESRELAX];
        public long[] dprice = new long[DefineConstants.MAXNODESRELAX];
        public long[] startn = new long[DefineConstants.MAXLINKSRELAX];
        public long[] endn = new long[DefineConstants.MAXLINKSRELAX];
        public long[] cost = new long[DefineConstants.MAXLINKSRELAX];
        public long[] rc = new long[DefineConstants.MAXLINKSRELAX];
        public long[] u = new long[DefineConstants.MAXLINKSRELAX];
        public long[] x = new long[DefineConstants.MAXLINKSRELAX];
        /* dfct2[MAXNODESRELAX],   // Final dfct of the soln (MUST BE ZERO: TOTALIN = TOTALOUT). */
    }
    public class auctionsave_
    {
        public long pass;
        public long eps;
        public long[] startn = new long[DefineConstants.MAXLINKSRELAX];
        public long[] endn = new long[DefineConstants.MAXLINKSRELAX];
        public long[] dfct = new long[DefineConstants.MAXNODESRELAX];
        public long[] x = new long[DefineConstants.MAXLINKSRELAX];
        public long[] u = new long[DefineConstants.MAXLINKSRELAX];
        public long[] rc = new long[DefineConstants.MAXLINKSRELAX];
        public long[] label = new long[DefineConstants.MAXNODESRELAX];
        public long[] prdcsr = new long[DefineConstants.MAXNODESRELAX];
    }
    public class blks_
    {
        public long[] label = new long[DefineConstants.MAXNODESRELAX]; // NEXT ARC INTO THE ENDING NODE OF ARC J. -  FIRST ARC INTO NODE I. -  NEXT ARC OUT OF THE STARTING NODE OF ARC J. -  FIRST ARC OUT OF NODE I.
        public long[] prdcsr = new long[DefineConstants.MAXNODESRELAX];
        public long[] fou = new long[DefineConstants.MAXNODESRELAX];
        public long[] nxtou = new long[DefineConstants.MAXLINKSRELAX];
        public long[] fin = new long[DefineConstants.MAXNODESRELAX];
        public long[] nxtin = new long[DefineConstants.MAXLINKSRELAX];
        public long[] save = new long[DefineConstants.MAXLINKSRELAX];
        public bool feasbl;
    }
    public class blks2_
    {
        public bool[] scan = new bool[DefineConstants.MAXNODESRELAX];
        public bool[] path_id__ = new bool[DefineConstants.MAXNODESRELAX];
        public long[] fpushf = new long[DefineConstants.MAXNODESRELAX]; // NEXT BALANCED ARC INTO THE ENDING NODE OF ARC J. -  FIRST BALANCED ARC INTO NODE I. -  NEXT BALANCED ARC OUT OF THE STARTING NODE OF ARC J. -  FIRST BALANCED ARC OUT OF NODE I.
        public long[] nxtpushf = new long[DefineConstants.MAXLINKSRELAX];
        public long[] fpushb = new long[DefineConstants.MAXNODESRELAX];
        public long[] nxtpushb = new long[DefineConstants.MAXLINKSRELAX];
    }
    public class blks3_
    {
        public long[] nxtqueue = new long[DefineConstants.MAXNODESRELAX];
        public long[] extend_arc__ = new long[DefineConstants.MAXNODESRELAX];
        public long[] sb_level__ = new long[DefineConstants.MAXNODESRELAX];
        public long[] sb_arc__ = new long[DefineConstants.MAXNODESRELAX];
    }
    public class input_
    {
        public long n; // 0 IF DEFAULT INITIALIZATION IS USED; 1 IF AUCTION IS TO BE USED. -  A VERY LARGE INTEGER TO REPRESENT INFINITY. -  NUMBER OF ARCS -  NUMBER OF NODES.
        public long na;
        public long large;
        public long crash;
        public bool repeat; // TRUE. IF SOME INITIALIZATION IS TO BE SKIPPED; FALSE OTHERWISE.
    }
    public class output_
    {
        public DateTime time0; // TOTAL SOLUTION TIME. -  TIME IN INITIALIZATION. -  INITIALIZER FOR SYSTEM TIMER.
        public double time1;
        public double time2;
        public long nmultinode; // NUMBER OF AUCTION/SHORTEST PATH ITERATIONS. -  NUMBER OF MULTINODE ASCENT STEPS IN RELAX4. -  NUMBER OF FLOW AUGMENTATION STEPS IN RELAX4. -  NUMBER OF RELAXATION ITERATIONS IN RELAX4. -  NUMBER OF MULTINODE RELAXATION ITERATIONS IN RELAX4.
        public long iter;
        public long num_augm__;
        public long num_ascnt__;
        public long nsp;
        public bool feasbl; // CHECK RESULT (as shown in CheckOUTPUT routine.)
    }

}
