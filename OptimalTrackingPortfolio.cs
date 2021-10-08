using System;
using ILOG.Concert;
using ILOG.CPLEX;

namespace FE
{
    public class OptimalTrackingPortfolio
    {
        public class TrackingPortolioArgs
        {
            public int[] InitialContracts;
            public double[] InstrumentForecasts, ContractsPerUnit, CostPerTrade;
            public double[,] CorrelationMatrix; 
            public double ShadowCost, UnitSize;
            public int MaxContracts;
        }

        public static (double[], double, double) TrackingPortolio(TrackingPortolioArgs args)
        {
            Cplex model = new Cplex();

            int N = args.InstrumentForecasts.Length;

            ILPMatrix lp = model.AddLPMatrix();

            IIntVar[] optContracts = model.IntVarArray(model.ColumnArray(lp, N), -args.MaxContracts, args.MaxContracts);
            IIntVar[] trades = model.IntVarArray(model.ColumnArray(lp, N), 0, 2 * args.MaxContracts);

            //absolute value modelling

            double[] lhs = new double[N];
            double[] rhs = new double[N];
            int[][] ind = new int[N][];
            double[][] val = new double[N][];
            double[][] val2 = new double[N][];
            for (int i = 0; i < N; ++i)
            {
                lhs[i] = args.InitialContracts[i];
                rhs[i] = int.MaxValue;
                ind[i] = new int[] { i, i + N };
                val[i] = new double[] { -1.0, 1.0 };
                val2[i] = new double[] { 1.0, 1.0 };
            }
            // -x + z  >= x0  (z>= x-x0)
            lp.AddRows(lhs, rhs, ind, val);
            // x + z >= x0 (z >= -(x-x0) )
            lp.AddRows(lhs, rhs, ind, val2);

            INumExpr trackingCovar = model.Constant(0.0);
            for (int i = 0; i < N; ++i)
            {
                for (int j = 0; j < N; ++j)
                {
                    INumExpr wtrack_i = model.Diff(args.InstrumentForecasts[i], model.Prod(1.0 / args.ContractsPerUnit[i], optContracts[i] ));
                    INumExpr wtrack_j = model.Diff(args.InstrumentForecasts[j], model.Prod(1.0 / args.ContractsPerUnit[j], optContracts[j] ));
                    trackingCovar = model.Sum(trackingCovar, model.Prod(args.CorrelationMatrix[i, j], model.Prod(wtrack_i, wtrack_j)));
                }
            }
            trackingCovar = model.Prod(args.UnitSize * args.UnitSize, trackingCovar);

            //minimize costs
            double[] costConstants = new double[N];
            for (int i = 0; i < N; ++i) costConstants[i] = args.ShadowCost * args.CostPerTrade[i];
            INumExpr costPenalty = model.ScalProd(trades, costConstants);
            model.Add(model.Minimize(model.Sum(costPenalty, trackingCovar)));


            model.SetParam(Cplex.Param.RootAlgorithm, Cplex.Algorithm.Dual);

            bool solutionFound = model.Solve();

            if (!solutionFound)
            {
                throw new System.Exception("Could not solve tracking portfolio problem!");
            }
            double[] _optContracts = model.GetValues(optContracts);
            double _costPenalty = model.GetValue(costPenalty), _trackingCovar = model.GetValue(trackingCovar);
            
            model.End();

            return (_optContracts, _trackingCovar, _costPenalty);
        }


    }
}
