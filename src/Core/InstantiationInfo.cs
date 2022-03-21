using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public class InstantiationInfo
    {
        public ReferenceLocation ReferenceLocation;
        public IMethodSymbol ContainingMethodSymbol;

        public InstantiationInfo(ReferenceLocation referenceLocation)
        {
            ReferenceLocation = referenceLocation;
        }

        public string ToShortMethodName()
        {
            var containingMethod = ContainingMethodSymbol.Name;

            var parameters = "";
            if (ContainingMethodSymbol.Parameters.Length > 0)
            {
                parameters = string.Join(", ", ContainingMethodSymbol.Parameters.Select(x => x.Type.Name));
            }

            var name = $"{containingMethod}({parameters})";
            return name;
        }

        public string ToFullMethodName()
        {
            var containingType = ContainingMethodSymbol.ContainingType.ToDisplayString();

            var name = ToShortMethodName();
            name = $"{containingType}->{name}";

            return name;
        }
    }
}
