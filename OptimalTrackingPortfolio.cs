using System;
using System.Collections.Generic;
using ILOG.Concert;
using ILOG.CPLEX;

namespace FE
{
    public class OptimalTrackingPortfolio
    {
        public class TrackingPortolioArgs
        {
            public int[] InitialContracts, MinTradeQty;
            public double[] InstrumentForecasts, ContractsPerUnit, CostPerTrade;
            public double[,] CorrelationMatrix; 
            public double ShadowCost, UnitSize;
            public int[] MaxContracts;
        }

        public static (double[], double, double, double) TrackingPortolio(TrackingPortolioArgs args)
        {
            Cplex model = new Cplex();

            model.SetOut(null);

            int N = args.InstrumentForecasts.Length;


            ILPMatrix lp = model.AddLPMatrix();
            int[] zeros = new int[N];
            int[] negMaxContracts = new int[N];
            int[] twoTimesMaxContracts = new int[N];
            for (int i = 0; i < N; ++i)
            {
                negMaxContracts[i] = -args.MaxContracts[i];
                twoTimesMaxContracts[i] = 2*args.MaxContracts[i];
            }

            IIntVar[] optContracts = model.IntVarArray(model.ColumnArray(lp, N), negMaxContracts, args.MaxContracts);
            IIntVar[] trades = model.IntVarArray(model.ColumnArray(lp, N), zeros, twoTimesMaxContracts);

            //absolute value modelling
            List<IRange> constraints = new();
            for (int i = 0; i < N; ++i)
            {
                IRange absConstraintI = model.AddGe(model.Diff(trades[i], model.Prod(args.MinTradeQty[i],optContracts[i])), -args.InitialContracts[i], $"-x_{i} + z_{i}  >= {args.InitialContracts[i]}"); // -x + z  >= -x0 
                IRange absConstraintII = model.AddGe(model.Sum(trades[i], model.Prod(args.MinTradeQty[i],optContracts[i])), args.InitialContracts[i], $"x_{i} + z_{i}  >= {args.InitialContracts[i]}"); // x + z >= x0
                constraints.Add(absConstraintI);
                constraints.Add(absConstraintII);
            }
            lp.AddRows(constraints.ToArray());

            INumExpr trackingCovar = model.Constant(0.0);
            for (int i = 0; i < N; ++i)
            {
                for (int j = 0; j < N; ++j)
                {
                    INumExpr wtrack_i = model.Diff(args.InstrumentForecasts[i], model.Prod(args.MinTradeQty[i] / args.ContractsPerUnit[i], optContracts[i] ));
                    INumExpr wtrack_j = model.Diff(args.InstrumentForecasts[j], model.Prod(args.MinTradeQty[j] / args.ContractsPerUnit[j], optContracts[j] ));
                    trackingCovar = model.Sum(trackingCovar, model.Prod(args.CorrelationMatrix[i, j], model.Prod(wtrack_i, wtrack_j)));
                }
            }
            trackingCovar = model.Prod(args.UnitSize * args.UnitSize, trackingCovar);

            //minimize costs
            double[] costConstantsFO = new double[N];
            double[] costConstants = new double[N];
            for (int i = 0; i < N; ++i) costConstantsFO[i] = args.ShadowCost * args.ShadowCost * args.CostPerTrade[i];
            for (int i = 0; i < N; ++i) costConstants[i] = args.CostPerTrade[i];
            INumExpr costPenalty = model.ScalProd(trades, costConstantsFO);
            INumExpr execCosts = model.ScalProd(trades, costConstants);
            model.Add(model.Minimize(model.Sum(costPenalty, trackingCovar)));


            model.SetParam(Cplex.Param.RootAlgorithm, Cplex.Algorithm.Dual);

            bool solutionFound = model.Solve();

            if (!solutionFound)
            {
                throw new System.Exception("Could not solve tracking portfolio problem!");
            }
            double[] _optContracts = new double[N];
            double[] optUnits = model.GetValues(optContracts);
            for (int i = 0; i <  N; ++i)
            {
                _optContracts[i] = args.MinTradeQty[i] * optUnits[i];
            }
            double _costPenalty = model.GetValue(costPenalty), _trackingCovar = model.GetValue(trackingCovar);
            double _execCosts = model.GetValue(execCosts);
            double[] _trades = model.GetValues(trades);

            model.End();

            return (_optContracts, _trackingCovar, _costPenalty, _execCosts);
        }


    }
}
