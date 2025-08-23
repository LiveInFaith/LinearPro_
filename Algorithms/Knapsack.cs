using LinearPro_.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LinearPro_.Algorithms
{
    /// <summary>
    /// 0/1 Knapsack via Branch & Bound that branches on the first fractional item
    /// from the fractional-knapsack greedy plan (your Excel pivot rule).
    /// - No bound pruning (shows all node tables).
    /// - Infeasible nodes are displayed but not expanded.
    /// - Labeling:
    ///     p0
    ///       ├─ p1  (pivot = 0)
    ///       └─ p2  (pivot = 1)
    ///     deeper:
    ///       pA.B.2 = 0-branch,  pA.B.1 = 1-branch
    /// </summary>
    internal sealed class Knapsack : IAlgorithm
    {
        public string Name => "Knapsack";

        private sealed class Node
        {
            // Fix per ORIGINAL variable index: -1 unknown, 0 fixed to 0, 1 fixed to 1
            public int[] Fix;
            public string Label;           // p0, p1, p2, p1.1, p1.2, ...
            public string BranchHeader;    // "x5 = 0" / "x5 = 1" or ""
            public double Bound;           // greedy upper bound (used for display)
            public double ValueFixed;      // sum of profits for fix==1
            public double WeightFixed;     // sum of weights for fix==1
            public int? PivotOrig;         // original index of pivot (first fractional)
            public List<(int orig, double take, double left)> Plan; // display rows in ORIGINAL order
        }

        public List<string> Solve(LPModel model)
        {

            return new List<string> { Name };
        }
    }
}
