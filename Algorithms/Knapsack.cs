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

            // Track best (optional informational only)
            double bestVal = double.NegativeInfinity;
            int[] bestSol = null;

            // DFS stack; no pruning by bound; infeasible nodes are shown but not expanded
            var stack = new Stack<Node>();
            EnqueueChildren(root, isRoot: true, values, weights, capacity, order, names, stack);

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                steps.Add(RenderNodeBlock(node, names));

                // Infeasible: don't branch further
                if (node.WeightFixed > capacity + 1e-9)
                {
                    steps.Add($"{node.Label} is infeasible (weight {node.WeightFixed:0.###} > capacity {capacity:0.###}).");
                    continue;
                }

                // Integral leaf? record candidate and stop branching
                if (!node.PivotOrig.HasValue)
                {
                    double cand = 0.0;
                    for (int i = 0; i < n; i++)
                        if (node.Plan.Any(p => p.orig == i && p.take >= 1.0 - 1e-9))
                            cand += values[i];

                    if (cand > bestVal)
                    {
                        bestVal = cand;
                        bestSol = node.Fix.ToArray();

                        // Fill unknowns with 0/1 from plan for a concrete vector
                        for (int i = 0; i < n; i++)
                        {
                            if (bestSol[i] < 0)
                                bestSol[i] = node.Plan.Any(p => p.orig == i && p.take >= 1.0 - 1e-9) ? 1 : 0;
                        }

                        steps.Add($"New best at {node.Label}: value = {bestVal:0.###}");
                    }
                    continue;
                }

                // Else: branch on pivot variable
                EnqueueChildren(node, isRoot: false, values, weights, capacity, order, names, stack);
            }

            // Final best (optional)
            if (bestSol != null)
            {
                steps.Add("\n=== BEST SOLUTION (from displayed leaves) ===");
                steps.Add($"Value: {bestVal:0.###}");
                steps.Add("Take: " + string.Join(", ",
                    Enumerable.Range(0, n).Where(i => bestSol[i] == 1).Select(i => names[i])));
            }

            return steps;
        }

        private static string RenderRankingBlock(string[] names, double[] values, double[] weights, double capacity)
        {
            int n = names.Length;

            // ratios and ranks
            var ratio = new double[n];
            for (int i = 0; i < n; i++)
                ratio[i] = (weights[i] == 0)
                    ? (values[i] > 0 ? double.PositiveInfinity : 0.0)
                    : values[i] / weights[i];

            var order = Enumerable.Range(0, n).OrderByDescending(i => ratio[i]).ThenBy(i => i).ToArray();
            var rank = new int[n];
            for (int pos = 0; pos < n; pos++)
                rank[order[pos]] = pos + 1;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Ranking (by profit/weight):");
            sb.AppendLine("item\trt\trank\tc1");
            for (int i = 0; i < n; i++)
            {
                string rtStr = double.IsPositiveInfinity(ratio[i])
                    ? "inf"
                    : ratio[i].ToString("0.###", CultureInfo.InvariantCulture);
                sb.AppendLine($"{names[i]}\t{rtStr}\t{rank[i]}\t{weights[i].ToString("0.###", CultureInfo.InvariantCulture)}");
            }
            sb.AppendLine("RHS Value:\t\t" + capacity.ToString("0.###", CultureInfo.InvariantCulture));
            sb.AppendLine(); // blank line
            return sb.ToString();
        }


    }
}
