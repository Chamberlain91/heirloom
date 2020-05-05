using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Heirloom.Collections
{
    public sealed class Graph<T> : IGraph<T>
    {
        private readonly Dictionary<T, List<Edge<T>>> _outgoing;
        private readonly EdgeCollection _edges;

        public Graph()
        {
            _outgoing = new Dictionary<T, List<Edge<T>>>();
            _edges = new EdgeCollection();
        }

        public IEnumerable<T> Vertices => _outgoing.Keys;

        public int VertexCount => _outgoing.Count;

        public IEnumerable<(T A, T B)> Edges => _edges.Select(e => (e.A, e.B));

        public int EdgeCount => _edges.Count;

        public void Clear()
        {
            _edges.Clear();
            _outgoing.Clear();
        }

        #region Vertex Manipulation

        public void AddVertex(T v)
        {
            if (ContainsVertex(v))
            {
                throw new ArgumentException($"Graph already contained the vertex '{v}'.", nameof(v));
            }

            // Construct adjacency list for vertex
            _outgoing[v] = new List<Edge<T>>();
        }

        public bool RemoveVertex(T v)
        {
            // Do we know about this vertex?
            if (_outgoing.TryGetValue(v, out var outgoing))
            {
                // Disconnect outgoing edges connected to this vertex
                outgoing.Apply(e => RemoveEdge(e.A, e.B));
                _outgoing.Remove(v);

                // Remove vertex and associated eges
                return true;
            }

            // Vertex was not contained, so return false.
            return false;
        }

        public bool ContainsVertex(T v)
        {
            return _outgoing.ContainsKey(v);
        }

        #endregion

        #region Edge Manipulation

        public void AddEdge(T a, T b, float weight)
        {
            // Validate vertices
            if (!ContainsVertex(a)) { throw new ArgumentException($"Unable to add edge, vertex does not exist.", nameof(a)); }
            if (!ContainsVertex(b)) { throw new ArgumentException($"Unable to add edge, vertex does not exist.", nameof(b)); }

            // Insert edge into edge collection
            var edge = new Edge<T>(a, b, weight);
            _edges.AddEdge(edge);

            // Add edge to both
            _outgoing[a].Add(edge);
            _outgoing[b].Add(edge);
        }

        public bool RemoveEdge(T a, T b)
        {
            // Validate vertices
            if (!ContainsVertex(a)) { throw new ArgumentException($"Unable to remove edge, vertex does not exist.", nameof(a)); }
            if (!ContainsVertex(b)) { throw new ArgumentException($"Unable to remove edge, vertex does not exist.", nameof(b)); }

            // Does edge exist?
            if (_edges.TryGetEdge(a, b, out var edge))
            {
                // Remove edge from edge collection
                _edges.RemoveEdge(edge);

                // Remove from adjacency lists
                _outgoing[a].Remove(edge);
                _outgoing[b].Remove(edge);

                // Edge was contained and removed
                return true;
            }

            // Edge was not contained, unable to remove
            return false;
        }

        public bool ContainsEdge(T a, T b)
        {
            return _edges.Contains(a, b);
        }

        public float GetEdgeWeight(T a, T b)
        {
            if (_edges.TryGetEdge(a, b, out var edge))
            {
                return edge.Weight;
            }
            else
            {
                throw new ArgumentException($"Unable to retreive edge weight, graph does not contain the specified edge.");
            }
        }

        public void SetEdgeWeight(T a, T b, float weight)
        {
            if (_edges.TryGetEdge(a, b, out var edge))
            {
                edge.Weight = weight;
            }
            else
            {
                throw new ArgumentException($"Unable to set edge weight, graph does not contain the specified edge.");
            }
        }

        #endregion

        public IEnumerable<T> GetSuccessors(T v)
        {
            // Return outgoing edge targets (ie, successors)
            return _outgoing[v].Select(e => e.GetOther(v));
        }

        #region Algorithms

        public IReadOnlyList<T> FindPath(T start, T goal, HeuristicCost<T> heuristic)
        {
            return Search.HeuristicSearch(start, goal, GetSuccessors, GetEdgeWeight, heuristic);
        }

        public IReadOnlyList<T> FindPath(T start, Func<T, bool> goalCondition, HeuristicCost<T> heuristic)
        {
            return Search.HeuristicSearch(start, goalCondition, GetSuccessors, GetEdgeWeight, heuristic);
        }

        public IEnumerable<T> Traverse(T start, TraversalMethod method)
        {
            return method switch
            {
                TraversalMethod.BreadthFirst => Search.BreadthFirst(start, GetSuccessors),
                TraversalMethod.DepthFirst => Search.DepthFirst(start, GetSuccessors),

                _ => throw new ArgumentException($"Invalid traversal method.", nameof(method)),
            };
        }

        public Graph<T> FindMinimumSpanningTree()
        {
            // todo: recycle/optimize use of C E and Q?
            var C = new Dictionary<T, float>();
            var E = new Dictionary<T, T>();
            var Q = new Heap<T>((a, b) => C[a].CompareTo(C[b]));

            // Construct output graph (ie, the forest)
            var F = new Graph<T>();

            // Initialize
            foreach (var v in Vertices)
            {
                C[v] = float.PositiveInfinity;
                Q.Add(v);
            }

            while (Q.Count > 0)
            {
                var v = Q.Remove();

                // Insert vertex into tree
                F.AddVertex(v);

                // Add best edge to tree, if a minimum weight is known.
                if (E.TryGetValue(v, out var e))
                {
                    F.AddEdge(e, v, C[v]);
                }

                // 
                foreach (var edge in _outgoing[v])
                {
                    var w = edge.GetOther(v);

                    if (Q.Contains(w))
                    {
                        if (edge.Weight < C[w])
                        {
                            C[w] = edge.Weight;
                            E[w] = v;

                            // Notify the queue the priority of the item has changed
                            Q.Update(w);
                        }
                    }
                }
            }

            return F;
        }

        IGraph<T> IGraph<T>.FindMinimumSpanningTree()
        {
            return FindMinimumSpanningTree();
        }

        #endregion

        #region Enumerator

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var v in Vertices)
            {
                yield return v;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        private class EdgeCollection : EdgeCollection<EdgeCollection.Pair, T>
        {
            protected override Pair CreatePair(T a, T b)
            {
                return new Pair(a, b);
            }

            internal readonly struct Pair : IEquatable<Pair>
            {
                public readonly T A;

                public readonly T B;

                public Pair(T a, T b)
                {
                    A = a;
                    B = b;
                }

                public override bool Equals(object obj)
                {
                    return obj is Pair pair
                        && Equals(pair);
                }

                public bool Equals(Pair other)
                {
                    return (A.Equals(other.A) && B.Equals(other.B))
                        || (A.Equals(other.B) && B.Equals(other.A));
                }

                public override int GetHashCode()
                {
                    return HashCode.Combine(A, B)
                         + HashCode.Combine(B, A);
                }
            }
        }
    }
}
