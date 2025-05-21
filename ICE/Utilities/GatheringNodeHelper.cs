using System.Collections.Generic;
using static ICE.Utilities.GatheringUtil;

namespace ICE.Utilities;

/// <summary>
/// Provides methods for finding optimal gathering paths using TSP heuristics.
/// </summary>
public class GatheringPathfinder
{
    private readonly Random _rng = new Random(); // Used for random selections in cyclical TSP

    /// <summary>
    /// Calculates the Euclidean distance between two GathNodeInfo objects based on their positions.
    /// </summary>
    /// <param name="node1">The first gathering node.</param>
    /// <param name="node2">The second gathering node.</param>
    /// <returns>The distance between the two nodes.</returns>
    private double GetDistance(GathNodeInfo node1, GathNodeInfo node2)
    {
        return node1.Position.Distance(node2.Position);
    }

    /// <summary>
    /// Calculates the Euclidean distance from a given Vector3 position to a GathNodeInfo object's position.
    /// </summary>
    /// <param name="pos">The starting Vector3 position.</param>
    /// <param name="node">The gathering node.</param>
    /// <returns>The distance from the position to the node.</returns>
    private double GetDistance(Vector3 pos, GathNodeInfo node)
    {
        return pos.Distance(node.Position);
    }

    /// <summary>
    /// Calculates the total path length for a given list of nodes.
    /// If <paramref name="isCyclical"/> is true, the distance from the last node back to the first is included.
    /// </summary>
    /// <param name="path">The ordered list of nodes in the path.</param>
    /// <param name="isCyclical">True if the path should be treated as a cycle (i.e., add distance from last to first node).</param>
    /// <returns>The total length of the path.</returns>
    private double CalculatePathLength(List<GathNodeInfo> path, bool isCyclical)
    {
        if (path == null || path.Count < 2)
        {
            return 0; // A path needs at least two nodes to have a length
        }

        double totalDistance = 0;
        for (int i = 0; i < path.Count - 1; i++)
        {
            totalDistance += GetDistance(path[i], path[i + 1]);
        }

        // For cyclical paths, add the distance from the last node back to the first
        if (isCyclical)
        {
            totalDistance += GetDistance(path[path.Count - 1], path[0]);
        }

        return totalDistance;
    }

    /// <summary>
    /// Applies the 2-Opt local search algorithm to improve a given path.
    /// It repeatedly swaps two non-adjacent edges if doing so reduces the total path length.
    /// </summary>
    /// <param name="path">The initial path to optimize.</param>
    /// <param name="isCyclical">True if the path should be treated as a cycle during optimization.</param>
    /// <returns>The optimized path.</returns>
    private List<GathNodeInfo> TwoOpt(List<GathNodeInfo> path, bool isCyclical)
    {
        // 2-Opt requires at least 3 nodes to perform a meaningful swap.
        if (path.Count < 3) return new List<GathNodeInfo>(path);

        List<GathNodeInfo> bestPath = new List<GathNodeInfo>(path);
        double bestDistance = CalculatePathLength(bestPath, isCyclical);
        bool improved = true;

        // Continue iterating as long as improvements are being made
        while (improved)
        {
            improved = false;
            // Iterate through all possible pairs of edges (i, i+1) and (k, k+1)
            // For open TSP, we usually fix the first and last node, so i starts from 1.
            // For cyclical, the indices wrap around, but a simpler implementation for 2-Opt
            // is to treat it as an open path and let CalculatePathLength handle the cycle.
            for (int i = 1; i < bestPath.Count - 1; i++)
            {
                for (int k = i + 1; k < bestPath.Count; k++)
                {
                    // Create a new path by reversing the segment between i and k
                    List<GathNodeInfo> newPath = new List<GathNodeInfo>(bestPath);
                    newPath.Reverse(i, k - i + 1); // Reverses the segment from index i to k (inclusive)

                    double newDistance = CalculatePathLength(newPath, isCyclical);

                    // If the new path is shorter, update the best path and continue searching for improvements
                    if (newDistance < bestDistance)
                    {
                        bestPath = newPath;
                        bestDistance = newDistance;
                        improved = true;
                    }
                }
            }
        }
        return bestPath;
    }

    /// <summary>
    /// Solves the Open-Ended Traveling Salesperson Problem (TSP) for gathering nodes.
    /// It finds an efficient path that visits all provided nodes exactly once, starting from a given player position.
    /// </summary>
    /// <param name="playerPosition">The current position of the player (starting point).</param>
    /// <param name="nodes">An enumerable collection of all available gathering nodes.</param>
    /// <returns>An ordered enumerable of <see cref="GathNodeInfo"/> representing the optimal path.</returns>
    public IEnumerable<GathNodeInfo> SolveOpenEndedTSP(Vector3 playerPosition, IEnumerable<GathNodeInfo> nodes)
    {
        if (nodes == null || !nodes.Any())
        {
            return Enumerable.Empty<GathNodeInfo>();
        }

        List<GathNodeInfo> allNodes = nodes.ToList();
        if (allNodes.Count == 1)
        {
            return new List<GathNodeInfo> { allNodes.First() };
        }

        // 1. Find the closest node to the player's starting position. This will be the first node in our path.
        GathNodeInfo initialStartNode = null;
        double minDistToPlayer = double.MaxValue;
        foreach (var node in allNodes)
        {
            double dist = GetDistance(playerPosition, node);
            if (dist < minDistToPlayer)
            {
                minDistToPlayer = dist;
                initialStartNode = node;
            }
        }

        // This should not happen if allNodes is not empty.
        if (initialStartNode == null)
        {
            return Enumerable.Empty<GathNodeInfo>();
        }

        // 2. Build an initial path using the Nearest Neighbor heuristic.
        List<GathNodeInfo> nnPath = new List<GathNodeInfo>();
        HashSet<uint> visitedNodeIds = new HashSet<uint>();

        nnPath.Add(initialStartNode);
        visitedNodeIds.Add(initialStartNode.NodeId);

        GathNodeInfo currentNode = initialStartNode;

        // Continue adding the nearest unvisited node until all nodes are visited
        while (nnPath.Count < allNodes.Count)
        {
            GathNodeInfo nextNode = null;
            double minDistance = double.MaxValue;

            foreach (var node in allNodes)
            {
                if (!visitedNodeIds.Contains(node.NodeId))
                {
                    double dist = GetDistance(currentNode, node);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nextNode = node;
                    }
                }
            }

            // If a next node is found, add it to the path and mark as visited
            if (nextNode != null)
            {
                nnPath.Add(nextNode);
                visitedNodeIds.Add(nextNode.NodeId);
                currentNode = nextNode;
            }
            else
            {
                // This break should ideally not be reached if allNodes are reachable and distinct.
                break;
            }
        }

        // 3. Improve the path using the 2-Opt local search algorithm.
        // For open-ended TSP, the first and last nodes are typically fixed (or the algorithm ensures they are not swapped).
        // Our 2-Opt implementation is designed to preserve the first and last elements of the sequence.
        List<GathNodeInfo> optimizedPath = TwoOpt(nnPath, false);

        return optimizedPath;
    }

    /// <summary>
    /// Solves the Cyclical Traveling Salesperson Problem (TSP) for a subset of gathering nodes.
    /// Given a set of X nodes, it finds a subset of N nodes (where N is specified) that form the shortest possible cycle.
    /// This method uses a heuristic approach involving multiple random trials to find a good subset.
    /// </summary>
    /// <param name="nodes">An enumerable collection of all available gathering nodes (X nodes).</param>
    /// <param name="n">The desired number of nodes (N) to include in the shortest cycle.</param>
    /// <returns>An ordered enumerable of <see cref="GathNodeInfo"/> representing the nodes in the optimal cycle.</returns>
    public IEnumerable<GathNodeInfo> SolveCyclicalTSP(IEnumerable<GathNodeInfo> nodes, int n)
    {
        if (nodes == null || !nodes.Any() || n <= 0)
        {
            return Enumerable.Empty<GathNodeInfo>();
        }

        List<GathNodeInfo> allNodes = nodes.ToList();
        if (n > allNodes.Count)
        {
            // Cannot select more nodes than available, so cap N at the total number of nodes.
            n = allNodes.Count;
        }

        // If only one node is requested, just return any single node.
        if (n == 1)
        {
            return new List<GathNodeInfo> { allNodes.First() };
        }

        List<GathNodeInfo> bestSubsetPath = null;
        double minTotalDistance = double.MaxValue;

        // Heuristic: Perform multiple trials to find a good subset and path.
        // The number of trials can be adjusted for performance vs. accuracy.
        // Capped at 1000 trials or a number related to the square of nodes for smaller sets.
        int numTrials = Math.Min(1000, allNodes.Count * (allNodes.Count - 1) / 2 + 1);
        if (numTrials == 0 && allNodes.Count > 0) numTrials = 1; // Ensure at least one trial if nodes exist.

        for (int trial = 0; trial < numTrials; trial++)
        {
            // 1. Select a random starting node for this trial's subset.
            GathNodeInfo startNode = allNodes[_rng.Next(allNodes.Count)];

            List<GathNodeInfo> currentSubset = new List<GathNodeInfo>();
            currentSubset.Add(startNode);
            HashSet<uint> visitedInSubset = new HashSet<uint>();
            visitedInSubset.Add(startNode.NodeId);

            GathNodeInfo currentNode = startNode;

            // 2. Greedily build a subset of N nodes using a Nearest Neighbor-like approach.
            // From the current node, always pick the closest unvisited node from the *entire* set of nodes.
            while (currentSubset.Count < n)
            {
                GathNodeInfo nextNode = null;
                double minDistance = double.MaxValue;

                foreach (var candidateNode in allNodes)
                {
                    if (!visitedInSubset.Contains(candidateNode.NodeId))
                    {
                        double dist = GetDistance(currentNode, candidateNode);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            nextNode = candidateNode;
                        }
                    }
                }

                // If a next node is found, add it to the current subset
                if (nextNode != null)
                {
                    currentSubset.Add(nextNode);
                    visitedInSubset.Add(nextNode.NodeId);
                    currentNode = nextNode;
                }
                else
                {
                    // This can happen if 'n' is greater than the number of available nodes,
                    // or if all remaining nodes are already in the subset (which shouldn't happen if logic is correct).
                    break;
                }
            }

            // 3. If a subset of N nodes was successfully built, optimize its cycle.
            if (currentSubset.Count == n)
            {
                // Optimize this N-node cycle using 2-Opt.
                // 'true' indicates that CalculatePathLength should include the wrap-around distance for a cycle.
                List<GathNodeInfo> optimizedCurrentPath = TwoOpt(currentSubset, true);
                double currentPathLength = CalculatePathLength(optimizedCurrentPath, true);

                // Update the best found path if the current one is shorter
                if (currentPathLength < minTotalDistance)
                {
                    minTotalDistance = currentPathLength;
                    bestSubsetPath = optimizedCurrentPath;
                }
            }
        }

        // Return the best subset path found across all trials, or an empty enumerable if no path was found.
        return bestSubsetPath ?? Enumerable.Empty<GathNodeInfo>();
    }
}