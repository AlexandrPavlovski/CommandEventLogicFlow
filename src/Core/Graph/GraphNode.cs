using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Graph
{
    public class GraphNode
    {
        public GraphNodeType Type;
        public string Name;
        public GraphNode[] Children;
    }
}
