using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    class InstantiationInfo
    {
        public ReferenceLocation ReferenceLocation;
        //public IMethodSymbol ContainingMethodSymbol;

        public InstantiationInfo(ReferenceLocation referenceLocation)
        {
            ReferenceLocation = referenceLocation;
            //ContainingMethodSymbol = containingMethodSymbol;
        }
    }
}
