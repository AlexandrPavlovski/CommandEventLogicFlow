using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Graph
{
    public class GraphNode
    {
        public GraphNodeType Type { get; set; }
        public string Name { get; set; }
        public List<GraphNode> Children { get; set; }

        public void AddChild(GraphNode child)
        {
            if (Children == null)
            {
                Children = new List<GraphNode>();
            }

            Children.Add(child);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
