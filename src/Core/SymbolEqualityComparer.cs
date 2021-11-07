using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    class SymbolEqualityComparer : IEqualityComparer<ISymbol>
    {
        public bool Equals(ISymbol x, ISymbol y)
        {
            return x.ToString() == y.ToString();
        }

        public int GetHashCode(ISymbol obj)
        {
            return obj.ToString().GetHashCode();
        }
    }
}
