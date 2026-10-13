using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace StateKoneUtility.Kirjasto.Rajapinnat
{
    public interface IDurableGraph<TNode, TEdge> :
     IEnumerable<(TNode Node, IReadOnlyCollection<(TNode Destination, TEdge Edge)> OutgoingEdges)>,
     IReadOnlyCollection<(TNode Node, IReadOnlyCollection<(TNode Destination, TEdge Edge)> OutgoingEdges)>
         where TNode : notnull
    {
        /// <summary>
        /// Gets a collection of all nodes in the graph.
        /// </summary>
        IReadOnlyCollection<TNode> Nodes { get; }

        /// <summary>
        /// Adds a node to the graph.
        /// </summary>
        /// <param name="node">The node to add.</param>
        /// <returns><c>true</c> if the node was added; <c>false</c> if the node already exists.</returns>
        bool AddNode(TNode node);

        /// <summary>
        /// Removes a node and all of its incoming and outgoing edges from the graph.
        /// </summary>
        /// <param name="node">The node to remove.</param>
        /// <returns><c>true</c> if the node was found and removed; otherwise, <c>false</c>.</returns>
        bool RemoveNode(TNode node);

        /// <summary>
        /// Determines whether the graph contains the specified node.
        /// </summary>
        /// <param name="node">The node to locate.</param>
        /// <returns><c>true</c> if the node is found; otherwise, <c>false</c>.</returns>
        bool ContainsNode(TNode node);

        /// <summary>
        /// Adds a directed edge between two nodes. Both nodes must already exist in the graph.
        /// </summary>
        /// <param name="source">The source node of the edge.</param>
        /// <param name="destination">The destination node of the edge.</param>
        /// <param name="edge">The value or data associated with the edge.</param>
        /// <returns><c>true</c> if the edge was added; <c>false</c> if an edge between the specified nodes already exists.</returns>
        /// <exception cref="ArgumentException">Thrown if either the source or destination node does not exist.</exception>
        bool AddEdge(TNode source, TNode destination, TEdge edge);

        /// <summary>
        /// Replaces an existing edge or adds a new one if it does not exist. Both nodes must already exist in the graph.
        /// </summary>
        /// <param name="source">The source node of the edge.</param>
        /// <param name="destination">The destination node of the edge.</param>
        /// <param name="edge">The new value or data to associate with the edge.</param>
        /// <exception cref="ArgumentException">Thrown if either the source or destination node does not exist.</exception>
        void UpsertEdge(TNode source, TNode destination, TEdge edge);

        /// <summary>
        /// Removes the directed edge between two nodes.
        /// </summary>
        /// <param name="source">The source node of the edge.</param>
        /// <param name="destination">The destination node of the edge.</param>
        /// <returns><c>true</c> if the edge was found and removed; otherwise, <c>false</c>.</returns>
        bool RemoveEdge(TNode source, TNode destination);

        /// <summary>
        /// Attempts to get the edge connecting a source node to a destination node.
        /// </summary>
        /// <param name="source">The source node.</param>
        /// <param name="destination">The destination node.</param>
        /// <param name="edge">When this method returns, contains the edge information, if found; otherwise, the default value of TEdge.</param>
        /// <returns><c>true</c> if an edge exists from source to destination; otherwise, <c>false</c>.</returns>
        bool TryGetEdge(TNode source, TNode destination, [MaybeNullWhen(false)] out TEdge edge);

        /// <summary>
        /// Gets a collection of all nodes connected to the specified node by an incoming or outgoing edge.
        /// </summary>
        /// <param name="node">The node whose neighbors to find.</param>
        /// <returns>A read-only collection of all neighbor nodes.</returns>
        IReadOnlyCollection<TNode> GetNeighbors(TNode node);

        /// <summary>
        /// Gets a collection of all outgoing edges from the specified node.
        /// </summary>
        /// <param name="node">The source node.</param>
        /// <returns>A read-only collection of tuples, each containing a destination node and the corresponding edge data.</returns>
        IReadOnlyCollection<(TNode Node, TEdge Edge)> GetOutgoing(TNode node);

        /// <summary>
        /// Gets a collection of all incoming edges to the specified node.
        /// </summary>
        /// <param name="node">The destination node.</param>
        /// <returns>A read-only collection of tuples, each containing a source node and the corresponding edge data.</returns>
        IReadOnlyCollection<(TNode Node, TEdge Edge)> GetIncoming(TNode node);

        /// <summary>
        /// Removes all nodes and edges from the graph.
        /// </summary>
        void Clear();
    }
}
