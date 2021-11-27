using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public struct AnalysisProgress
    {
        public int Percent { get; }
        public string Description { get; }

        public AnalysisProgress(int percent, string description)
        {
            Percent = percent;
            Description = description;
        }
    }
}
