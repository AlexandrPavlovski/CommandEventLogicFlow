using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public static class Extensions
    {
        public static void SafeReport(this IProgress<AnalysisProgress> progress, int percent, string description)
        {
            if (progress != null)
            {
                progress.Report(new AnalysisProgress(percent, description));
            }
        }
    }
}
