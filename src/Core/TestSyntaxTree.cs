using System;
using System.Collections.Generic;
using System.Linq;

namespace Core
{
    class A { }
    class B : A { }

    class TestSyntaxTree
    {
        void Method(int a)
        {
            var d = new int[] { 1 };
            var ter = false ? 4 : 2;

            if (true) { }

            IEnumerable<int> v = new int[] { 1 };
            foreach (var item in v)
            {
                m2(item);
            }
            Action<int> t = m2;
        }

        void m2(int p)
        {

        }
    }
}
