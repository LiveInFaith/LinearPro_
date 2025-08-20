using LinearPro_.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinearPro_.Algorithms
{
    //max +2 +3 +3 +5 +2 +4 
    // +11 +8 +6 +14 +10 +10 <= 40 
    internal class Knapsack : IAlgorithm
    {
        public string Name => "Knapsack";
        public Knapsack() { }

        public List<string> Solve(LPModel lPModel)
        {




            return new List<string>();
        }

        public void Rank(List<string> rank)
        {
            double[] objCoefficients = new double[6] { 2, 3, 3, 5, 2, 4 };
            List<double> conCoefficients = new List<double> { 11, 8, 6, 14, 10, 10, 40 };
            Dictionary<int, double> RankAndValue = new Dictionary<int, double>();
            List<double> tempValues = new List<double>();


            double RHS = conCoefficients.Last();

            for (int i = 0; i < objCoefficients.Length; i++)
            {
                tempValues.Add(objCoefficients[i] / conCoefficients[i]);
            }

            tempValues.Sort();
            tempValues.Reverse();

            for (int i = 0; i < tempValues.Count; i++)
            {
                RankAndValue.Add(i + 1, tempValues[i]);
            }

        }

    }
}
