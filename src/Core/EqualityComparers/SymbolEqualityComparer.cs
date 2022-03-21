using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Core.EqualityComparers
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
