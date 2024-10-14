using System;

namespace Csu.Modsim.ModsimModel
{
    /*  glover:
    *      calculate depletion factors for each
    *      node and link based on transmissivity,
    *      specific yield, and distance from
    *      source, as input by user;
    *      for complete details of methodology
    *      the user is asked to refer to:
    *
    *      glover,r.e. transient ground water
    *      hydraulics, water resources publications,
    *      fort collins, colorado, 1977.
    */

    /* This routine used to be called for each node and link in the org
    * file as loading occured.  Now this routine is called once during
    * model startup to find and calculate all lags and routing 
    * coefficients.
    */

    public static class GlobalMembersGlover
    {
        public static void glover(Model mi)
        {
            int i;
            Node n;
            Link l;
            LagInfo li;
            double tconv, dconv;
            double timeDays = 0.0;
            double alpha;
            double y1;
            double y2;
            double a;
            double z1;
            double z2;
            double z;
            double sum;
            double f;
            double f1;
            double g1;
            double g2;
            double h1;
            double h2;
            double wsq;
            double x;
            double x1;
            double w1;
            double e1;
            double x2;
            double w2;
            double e2;
            double cc;
            double c1;
            double c2;
            double kmin;
            double kmax;
            double xrout;
            double trout;
            double krout;
            double pisq = Math.Pow(Math.PI, 2.0);
            int nperGlover;
            int montmp;
            int k;
            double dist;
            double trans;
            // nperGlover is at minimum 4, and is limited by either the user selected
            // number of lags or total number of time periods.
            nperGlover = System.Math.Max(4, System.Math.Min(mi.nlags, mi.TimeStepManager.noModelTimeSteps));

            // Convert transmissivity from default units to ft^2/day & distance to ft
            tconv = ModsimUnits.GetDefaultUnits(ModsimUnitsType.AreaRate, mi.UseMetricUnits).ConvertTo(1.0, ModsimUnits.GetDefaultUnits(ModsimUnitsType.AreaRate, false));
            dconv = ModsimUnits.GetDefaultUnits(ModsimUnitsType.Length, mi.UseMetricUnits).ConvertTo(1.0, ModsimUnits.GetDefaultUnits(ModsimUnitsType.Length, false));

            /* conversion for time - month */
            timeDays = mi.timeStep.AverageTimeSpan.TotalDays;

            /*
            *         calculate depletion factors for non-reservoir nodes
            *
            *     interaction of a water table aquifer receiving recharge from
            *     irrigation and precipitation, and an interconnected stream.
            *     (maasland , 1959) using method developed for a parallel
            *     drain system.
            *
            *     f = 8/pi sum ((1/n**2) exp[(-n**2 * pi**2 (a*t/l**2)]
            *
            *      n =1,2,3,...inf
            *      a=t/s.y.
            *      l = distance
            */
            for (i = 0; i < mi.mInfo.demList.Length; i++)
            {
                n = mi.mInfo.demList[i];
                for (li = n.m.infLagi; li != null; li = li.next)
                {
                    li.lagInfoData = new double[nperGlover];
                }
                for (li = n.m.pumpLagi; li != null; li = li.next)
                {
                    li.lagInfoData = new double[nperGlover];
                }
                if (n.m.spyld > 0)
                {
                    dist = n.m.Distance * dconv;
                    trans = n.m.trans * tconv;
                    alpha = trans / n.m.spyld;
                    wsq = (double)4.0 * dist * dist;
                    a = alpha * timeDays * (double)(pisq / wsq);
                    for (montmp = 0; montmp < nperGlover; montmp++)
                    {
                        sum = 0.0;
                        x = (double)(montmp);
                        y1 = -a * x;
                        y2 = -a * (double)(x + 1);
                        for (k = 1; k <= 21; k += 2)
                        {
                            y1 = y1 * (double)(k * k);
                            y2 = y2 * (double)(k * k);
                            z1 = 0.0;
                            if (y1 >= -200.0)
                            {
                                z1 = (double)Math.Exp(y1);
                            }
                            z2 = 0.0;
                            if (y2 >= -200.0)
                            {
                                z2 = (double)Math.Exp(y2);
                            }
                            z = (z1 - z2) / ((double)(k * k));
                            sum = sum + z;
                        }
                        // Before we added multiple return/depletion locations
                        // Not the best way, assumes all distributions of lags are the same
                        // Need to make these user editable at some point from the interface.
                        for (li = n.m.infLagi; li != null; li = li.next)
                        {
                            li.lagInfoData[montmp] = (double)(8.0 / pisq * sum);
                        }
                        for (li = n.m.pumpLagi; li != null; li = li.next)
                        {
                            li.lagInfoData[montmp] = (double)(8.0 / pisq * sum);
                        }
                    }
                }
            }

            /*
            *                 calculate depletion factors for
            *                            reservoirs
            *
            *    reservoir seepage is defined as a point source application.
            *    the effect on the stream corresponds to the effect of a
            *    recharge well, which has the same absolute flow magnitude
            *    as the effect of a pumping well with the flow direction
            *    reversed.  this solution turns out to be the same as a
            *    line source. (glover,1974).
            *
            *        q = i/2 erfc[ l/(4at)**.5]
            *     i= applied line source flow rate
            *     a= t/sy
            *     l= distance
            *     t= timeDays
            */
            //for(i = 0; i < mi->mInfo->resListLen; i++)
            for (i = 0; i < mi.mInfo.resList.Length; i++)
            {
                n = mi.mInfo.resList[i];
                for (li = n.m.infLagi; li != null; li = li.next)
                {
                    li.numLags = nperGlover;
                    li.lagInfoData = new double[nperGlover];
                }
                for (li = n.m.pumpLagi; li != null; li = li.next)
                {
                    li.numLags = nperGlover;
                    li.lagInfoData = new double[nperGlover];
                }

                if (n.m.spyld > 0)
                {
                    dist = n.m.Distance * dconv;
                    trans = n.m.trans * tconv;
                    alpha = trans / n.m.spyld;
                    for (montmp = 0; montmp < nperGlover; montmp++)
                    {
                        x = (double)montmp + 1;
                        y1 = x * timeDays;
                        f = (double)Math.Sqrt(4.0 * alpha * y1);
                        g1 = dist / f;
                        h1 = GlobalMembersGlover.errfc(g1);
                        y2 = (x - 1) * timeDays;
                        if (y2 > 0)
                        {
                            f1 = (double)Math.Sqrt(4.0 * alpha * y2);
                            g2 = dist / f1;
                            h2 = GlobalMembersGlover.errfc(g2);
                        }
                        else
                        {
                            h2 = 0.0;
                        }
                        // Not the best way, assumes all distributions of lags are the same
                        // Need to make these user editable at some point from the interface.
                        for (li = n.m.infLagi; li != null; li = li.next)
                        {
                            li.lagInfoData[montmp] = h1 - h2;
                        }
                        for (li = n.m.pumpLagi; li != null; li = li.next)
                        {
                            li.lagInfoData[montmp] = h1 - h2;
                        }
                    }
                }
            }
            for (i = 0; i < mi.mInfo.realLinkList.Length; i++)
            {
                l = mi.mInfo.realLinkList[i];
                if (l.m.spyldc > 0)
                {
                    if (l.m.loss_coef < 1.0)
                    {
                        /*
                        * calculate depletion factors for canals -
                        *     seepage from a canal or stream is assumed to correspond
                        *     to a line source of recharge water. (mcwhorter,1972)
                        *     q = i/2 erfc[ l/(4at)**.5]
                        *      i= applied line source flow rate
                        *      a= t/sy
                        *      l= distance
                        *      t= timeDays
                        */
                        dist = l.m.distc * dconv;
                        trans = l.m.transc * tconv;
                        alpha = (trans / l.m.spyldc);
                        for (montmp = 0; montmp < nperGlover; montmp++)
                        {
                            x = (double)montmp;
                            x1 = x * timeDays;
                            y1 = (double)Math.Sqrt(4.0 * alpha * x1);
                            w1 = dist / y1;
                            e1 = GlobalMembersGlover.errfc(w1);
                            x2 = (x - 1) * timeDays;
                            if (x2 != 0)
                            {
                                y2 = (double)Math.Sqrt(4.0 * alpha * x2);
                                w2 = dist / y2;
                                e2 = GlobalMembersGlover.errfc(w2);
                            }
                            else
                            {
                                e2 = 0.0;
                            }
                            l.m.lagfactors[montmp] = e1 - e2;
                        }
                    }
                    else
                    {
                    /*
                    c     case(4)
                    c
                    c             calculate routing factor using muskingum method
                    c             reference usace : em 110-2-1408 routing of floods
                    c                               through river channels
                    c                                1 march 1960
                    c                        usace: hec-5 simulation of flood control
                    c                               and conservation systems user manual
                    c                                exhibit 1: stream routing methods
                    c                                april 1982
                    c
                    */
                    addAFactor:
                        xrout = l.m.spyldc;
                        krout = l.m.transc;
                        trout = l.m.distc;
                        /* check routing parameters */
                        if (xrout > 0.5)
                            xrout = 0.5;
                        kmin = (trout) / (2 * (1 - xrout));
                        kmax = trout / (2 * xrout);
                        if (krout > kmax)
                        {
                            krout = kmax;
                        }
                        if (krout < kmin)
                        {
                            krout = kmin;
                        }
                        /* calculate routing parameters */
                        c1 = (trout - 2 * xrout * krout) / (2 * krout * (1 - xrout) + trout);
                        cc = ((2 * krout * (1 - xrout) + trout) - 2 * trout) / (2 * krout * (1 - xrout) + trout);
                        c2 = c1 * cc + (trout + 2 * xrout * krout) / (2 * krout * (1 - xrout) + trout);
                        double sumCoeffs = 0.0;
                        l.m.lagfactors[0] = c1;
                        l.m.lagfactors[1] = c2;
                        sumCoeffs = c1 + c2;
                        for (montmp = 2; montmp < mi.maxLags; montmp++)
                        {
                            if (sumCoeffs < 1)
                            {
                                l.m.lagfactors[montmp] = l.m.lagfactors[montmp - 1] * cc;
                                sumCoeffs += l.m.lagfactors[montmp];
                            }
                        }
                        if (sumCoeffs > 0.9995 || montmp + 2 >= l.m.lagfactors.Length)
                        {
                            string msg = string.Concat(System.Convert.ToString(nperGlover), " Muskingum coefficients in use. Sum = ", System.Convert.ToString(sumCoeffs));
                            mi.FireOnMessage(msg);
                        }
                        else
                        {
                            //mi->FireOnMessage("Its necessary to increase the number of lags.");
                            nperGlover += 1;
                            mi.maxLags = nperGlover;
                            if (mi.maxLags < l.m.lagfactors.Length)
                            {
                                goto addAFactor;
                            }
                            else
                            {
                                string msg = string.Concat(System.Convert.ToString(nperGlover), " Muskingum coefficients in use. Sum = ", System.Convert.ToString(sumCoeffs));
                                mi.FireOnMessage(msg);
                            }
                        }
                    }
                }
            }
        }
        /* calculate the error function of value x */
        public static double errf(double x)
        {
            if (x > 3.8)
            {
                return (GlobalMembersGlover.erfa(x));
            }
            else
            {
                return (GlobalMembersGlover.erfm(x));
            }
        }
        /* calculate the error function value of x by approximation */
        public static double erfa(double x)
        {
            double[] c = { .3193815, -.3565638, 1.781487, -1.821256, 1.330274, .2316419, 2.506628275, 1.414213562 };
            double u;
            double w;
            double ph;
            double phi;
            u = c[7] * x;
            w = 1 / (1 + c[5] * u);
            ph = Math.Exp(-u * u / 2) / c[6];
            phi = 1 - ph * w * (c[0] + w * (c[1] + w * (c[2] + c[3] * w + c[4] * w * w)));
            return (double)(phi + phi - 1);
        }
        /* calculate the complementary error function value of x */
        public static double errfc(double x)
        {
            return (double)(1 - GlobalMembersGlover.errf(x));
        }
        /* calculate the error function value of x by maclaurian series */
        public static double erfm(double x)
        {
            double erfmrtn;
            double erf1;
            double sign;
            double tk;
            double tm;
            double erfn;

            erfmrtn = x;
            erf1 = x;
            sign = -1.0;
            tk = 1.0;
        L_20:
            tm = tk + tk + 1;
            erf1 = erf1 * x * x / tk;
            erfn = erf1 * sign / tm;
            erfmrtn = erfmrtn + erfn;
            if (System.Math.Abs(erfn) < 1.0E-09)
            {
                goto L_40;
            }
            sign = -sign;
            tk = tk + 1;
            goto L_20;
        L_40:
            erfmrtn = (erfmrtn + erfmrtn) / (double)1.772453851;
            return erfmrtn;
        }
    }

}
