using Core.Graph;
using System.Collections.Generic;

namespace Core.EqualityComparers
{
    class GraphNodeEqualityComparer : IEqualityComparer<GraphNode>
    {
        public bool Equals(GraphNode x, GraphNode y)
        {
            return x.Text == y.Text;
        }

        public int GetHashCode(GraphNode obj)
        {
            return obj.Text.GetHashCode();
        }
    }
}
