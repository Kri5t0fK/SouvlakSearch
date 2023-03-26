﻿namespace SouvlakMVP;

using System.Linq;
using indexT = System.Int32;
using edgeWeightT = System.Single;
using System.Dynamic;
using System.Collections.Generic;

/// <summary>
/// Interface for easy use of dijkstra algorithm
/// </summary>
public class VerticesConnections
{
    /// <summary>
    /// Class representing a single connection, aka. weight and road
    /// </summary>
    public class Connection
    {
        public readonly edgeWeightT weight;
        public readonly List<indexT> path;

        public Connection(edgeWeightT weight, List<indexT> path)
        {
            this.weight = weight;
            this.path = path;
        }

        public override string ToString()
        {
            return this.weight.ToString("n2");
        }

        public string ToStringFull()
        {
            if (this.path.Count == 0)   // There shouldn't be a case where weight exists but path is empty, but hey
            {
                return this.weight.ToString() + " : []";
            }
            else
            {
                string str = this.weight.ToString() + " : [";
                foreach (indexT idx in this.path)
                {
                    str += idx.ToString() + ", ";
                }
                str = str.Remove(str.Length - 2) + "]";
                return str;
            }
        }
    }


    private Graph graph;
    private Dictionary<indexT, indexT> indexTranslate;
    private Connection[,] connectionMatrix;


    public void Rebuild(Graph graph)
    {
        this.graph = graph;

        List<indexT> unevenVertices = this.graph.GetUnevenVerticesIdxs();
        this.connectionMatrix = new Connection[unevenVertices.Count, unevenVertices.Count];

        this.indexTranslate = new Dictionary<indexT, indexT>();
        for (indexT thisIdx = 0; thisIdx < unevenVertices.Count; thisIdx++)
        {
            this.indexTranslate.Add(unevenVertices[thisIdx], thisIdx);
        }
    }

    public VerticesConnections(Graph graph)     // Yes I know of DRY, but VS2022 kept giving me warnings if I didn't do it like this
    {
        this.graph = graph;

        List<indexT> unevenVertices = this.graph.GetUnevenVerticesIdxs();
        this.connectionMatrix = new Connection[unevenVertices.Count, unevenVertices.Count];

        this.indexTranslate = new Dictionary<indexT, indexT>();
        for (indexT thisIdx = 0; thisIdx < unevenVertices.Count; thisIdx++)
        {
            this.indexTranslate.Add(unevenVertices[thisIdx], thisIdx);
        }
    }

    public override string ToString()
    {
        var keys = this.indexTranslate.Keys.ToList();
        indexT len = this.connectionMatrix.GetLength(0);
        len += 5;
        
        string str = String.Concat(Enumerable.Repeat(" ", len)) + " |";
        foreach (var key in keys)
        {
            str += key.ToString().PadLeft(len) + " |";
        }

        for (indexT i=0; i<this.connectionMatrix.GetLength(0); i++)
        {
            str += "\n";
            str += String.Concat(Enumerable.Repeat("-", (len + 2) * (keys.Count + 1)));
            str += "\n";
            str += keys[i].ToString().PadLeft(len) + " |";

            for (indexT j = 0; j < this.connectionMatrix.GetLength(1); j++)
            {
                str += (this.connectionMatrix[i, j] == null ? "N/A" : this.connectionMatrix[i, j].ToString()).PadLeft(len) + " |";
            }
        }

        return str;
    }

    private (indexT[], edgeWeightT[]) CalcClassicDijkstra(indexT startVertex)
    {
        int verticesN = this.graph.GetVertexCount();

        // Array containing the minimum costs to reach each vertex from the starting vertex
        edgeWeightT[] minCostToVertex = new edgeWeightT[verticesN];
        // Array containing the preceding vertices on the path from the starting vertex
        indexT[] precedingVertices = new indexT[verticesN];

        // Working lists of vertices
        List<indexT> unvisitedVertices = new List<indexT>();
        List<indexT> visitedVertices = new List<indexT>();

        // Filing out verticesToProcess and minCostToVertex
        for (int i = 0; i < verticesN; i++)
        {
            minCostToVertex[i] = (i == startVertex) ? 0f : edgeWeightT.MaxValue;
            unvisitedVertices.Add(i);
        }

        while (unvisitedVertices.Count > 0)
        {
            indexT processedVertex = indexT.MaxValue;
            edgeWeightT tempMinCost = edgeWeightT.MaxValue;

            // Finding the vertex to process (with the minimum cost to reach from the starting vertex)
            foreach (indexT vertex in unvisitedVertices)
            {
                if (minCostToVertex[vertex] < tempMinCost)
                {
                    tempMinCost = minCostToVertex[vertex];
                    processedVertex = vertex;
                }
            }

            // Moving the current vertex to processed vertices
            if (unvisitedVertices.Remove(processedVertex))
            {
                visitedVertices.Add(processedVertex);
            }

            // Reviewing all neighbors of the relocated vertex
            for (indexT i = 0; i < graph[processedVertex].edgeList.Count(); i++)
            {
                Graph.Edge edge = graph[processedVertex].edgeList[i];
                indexT nextVertex = edge.targetIdx;
                edgeWeightT edgeCost = edge.weight;

                // Check if neighbour has not yet been processed
                if (unvisitedVertices.Contains(nextVertex))
                {
                    edgeWeightT CostToVertex = minCostToVertex[processedVertex] + edgeCost;

                    // Check the new cost and update if it is smaller than the old one
                    if (minCostToVertex[nextVertex] > CostToVertex)
                    {
                        minCostToVertex[nextVertex] = CostToVertex;
                        precedingVertices[nextVertex] = processedVertex;
                    }
                }
            }
        }

        return (precedingVertices, minCostToVertex);
    }

    private (List<indexT>, edgeWeightT) GetPathAndCost(indexT[] precedingVertices, edgeWeightT[] minCostToVertex, indexT endVertex)
    {
        indexT? tempVertex = endVertex;
        List<indexT> shortestPath = new List<indexT>();

        // Create the shortest path
        while (tempVertex != null)
        {
            shortestPath.Add(tempVertex.Value);
            tempVertex = precedingVertices[tempVertex.Value];
        }

        // Reverse the shortest path
        shortestPath.Reverse();

        // Get the path total cost
        edgeWeightT totalCost = minCostToVertex[endVertex];

        return (shortestPath, totalCost);
    }

    public Connection GetConnection(indexT start, indexT stop)
    {
        if (start == stop)
        {
            throw new InvalidOperationException("Can not get connection between the same vertex!");
        }
        else if (!this.indexTranslate.ContainsKey(start) || !this.indexTranslate.ContainsKey(stop))
        {
            throw new IndexOutOfRangeException("One of the given values does not exist in graph!");
        }
        else
        {
            start = this.indexTranslate[start];
            stop = this.indexTranslate[stop];

            if (this.connectionMatrix[start, stop] == null)
            {
                (indexT[] precedingVertices, edgeWeightT[] minCostToVertex) = CalcClassicDijkstra(start);

                List<indexT> endVertices = new List<indexT>();
                for (indexT j = 0; j < this.connectionMatrix.GetLength(1); j++)
                {
                    if (j != stop && this.connectionMatrix[start, j] != null)
                    {
                        endVertices.Add(j);
                    }
                }

                foreach (indexT endVertex in endVertices)
                {
                    (List<indexT> path, edgeWeightT weight) pAc = GetPathAndCost(precedingVertices, minCostToVertex, endVertex);
                    List<indexT> pathReverse = new List<indexT>(pAc.path);
                    pathReverse.Reverse();

                    this.connectionMatrix[start, endVertex] = new Connection(pAc.weight, pAc.path);
                    this.connectionMatrix[endVertex, start] = new Connection(pAc.weight, pathReverse);
                }
            }
            
            return this.connectionMatrix[start, stop];
        }
    }

    public Connection this[indexT start, indexT stop]
    {
        get { return this.GetConnection(start, stop); }
    }
}

/*
 else if (!this.indexTranslate.ContainsKey(start) || !this.indexTranslate.ContainsKey(stop))
        {
            throw new IndexOutOfRangeException("One of the given values does not exist in graph!");
        }
 */