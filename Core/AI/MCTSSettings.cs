using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace Chess
{
    [CreateAssetMenu(menuName = "AI/MCTSSettings")]
    public class MCTSSettings : AISettings
    {
        public bool limitNumOfPlayouts;
        public int maxNumOfPlayouts;
        public int playoutDepthLimit;
        public double explorationConstant = 0.7;
        // I added explorationConstant in this class is used to control the exploration-exploitation trade-off in the Monte Carlo Tree Search algorithm. When the value is high, it will encourage the algorithm to explore more, leading to a larger and more diverse set of simulations in the search tree, but potentially at the cost of longer search times.When the value is low, the algorithm will prioritize exploiting the information already gathered in the search tree, potentially leading to faster search times but at the cost of a less diverse set of simulations and potentially missing out on better moves.In the context of the given MCTSSearch program, the explorationConstant is used in the BestChild function.It is used to bias the selection of child nodes in the search tree towards nodes that have been visited less often, allowing the algorithm to explore more of the search space before converging on a final solution.
    }
}
