using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    class HandlerInfo
    {
        public IMethodSymbol MethodSymbol;
        public SyntaxNode MethodNode;
    }
}
