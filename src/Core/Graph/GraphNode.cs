using Microsoft.CodeAnalysis;
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
        public string Text { get; set; }
        public List<HandlerInfo> Handlers { get; set; }
        public List<InstantiationInfo> Instantiations { get; set; }
        public List<string> Handlers2 { get; set; }
        public List<GraphNode> Children { get; set; }
        public bool IsRepeatedInTree { get; set; }
        public ITypeSymbol TypeSymbol { get; set; }


        public void AddChild(GraphNode child)
        {
            if (Children == null)
            {
                Children = new List<GraphNode>();
            }

            Children.Add(child);
        }

        public GraphNode MakeChildfreeCopy()
        {
            return new GraphNode
            {
                Type = Type,
                Text = Text,
                Handlers = Handlers,
                Instantiations = Instantiations,
                TypeSymbol = TypeSymbol
            };
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
