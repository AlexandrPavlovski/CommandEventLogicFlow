using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public class Config
    {
        public string SolutionPath { get; set; }
        public string ProjectThatContainsCommandInterface { get; set; }
        public string ProjectThatContainsEventInterface { get; set; }
        public string CommandInterfaceTypeNameWithNamespace { get; set; }
        public string EventInterfaceTypeNameWithNamespace { get; set; }
        public string[] HandlerMethodNames { get; set; }
        public string HandlerMarkerInterfaceTypeNameWithNamespace { get; set; }

        public bool IsEmpty()
        {
            return SolutionPath == null 
                || ProjectThatContainsCommandInterface == null
                || ProjectThatContainsEventInterface == null
                || CommandInterfaceTypeNameWithNamespace == null
                || EventInterfaceTypeNameWithNamespace == null
                || HandlerMethodNames == null
                || HandlerMarkerInterfaceTypeNameWithNamespace == null;
        }
    }
}
