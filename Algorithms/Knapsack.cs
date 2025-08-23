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
            var steps = new List<string>();

            // --- Expect exactly one <= knapsack constraint ---
            if (model?.Constraints == null || model.Constraints.Count != 1 ||
                model.Constraints[0].Relation != Relation.LE)
            {
                return new List<string> { "ERROR: Knapsack expects exactly one <= constraint." };
            }

            var values = model.ObjectiveCoefficients;
            var weights = model.Constraints[0].Coefficients;
            double capacity = model.Constraints[0].Rhs;
            int n = values.Length;

            if (weights.Length != n)
                return new List<string> { "ERROR: Objective and constraint variable counts differ." };

            // Names (x1..xn if none provided)
            string[] names = (model.VariableColumns != null && model.VariableColumns.Count == n)
                ? model.VariableColumns.ToArray()
                : Enumerable.Range(1, n).Select(i => "x" + i).ToArray();

            // ---- TOP BLOCK: ranking table (rt, rank, c) ----
            steps.Add(RenderRankingBlock(names, values, weights, capacity));

            // Ratio order (descending profit/weight), safe if weight==0
            var order = Enumerable.Range(0, n)
                                  .Select(i => new
                                  {
                                      i,
                                      r = (weights[i] == 0
                                            ? (values[i] > 0 ? double.PositiveInfinity : 0.0)
                                            : values[i] / weights[i])
                                  })
                                  .OrderByDescending(a => a.r)
                                  .Select(a => a.i)
                                  .ToArray();

            // --- Root node (all unknown) ---
            var root = NewNode(
                fix: Enumerable.Repeat(-1, n).ToArray(),
                parentLabel: null,
                isRoot: true,
                branchVarOrig: null,
                branchValue: -1,
                values: values,
                weights: weights,
                capacity: capacity,
                order: order,
                names: names);

            steps.Add($"Initial bound: {root.Bound:0.###}");
            steps.Add(RenderNodeBlock(root, names));

            return new List<string> { Name };
        }
    }
}
