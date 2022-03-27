using Core;
using Microsoft.Build.Locator;
using System.Configuration;
using System.Threading.Tasks;

namespace ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var cfg = new Config();

            cfg.SolutionPath = ConfigurationManager.AppSettings["SolutionPath"];
            cfg.ProjectThatContainsCommandInterface = ConfigurationManager.AppSettings["ProjectThatContainsCommandInterface"];
            cfg.ProjectThatContainsEventInterface = ConfigurationManager.AppSettings["ProjectThatContainsEventInterface"];
            cfg.CommandInterfaceTypeNameWithNamespace = ConfigurationManager.AppSettings["CommandInterfaceTypeNameWithNamespace"];
            cfg.EventInterfaceTypeNameWithNamespace = ConfigurationManager.AppSettings["EventInterfaceTypeNameWithNamespace"];
            cfg.HandlerMethodNames = ConfigurationManager.AppSettings["HandlerMethodNames"].Split(',');
            cfg.HandlerMarkerInterfaceTypeNameWithNamespace = ConfigurationManager.AppSettings["HandlerMarkerInterfaceTypeNameWithNamespace"];

            MSBuildLocator.RegisterDefaults();

            //MSBuildLocator.RegisterMSBuildPath(@"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin");

            //var t = MSBuildLocator.QueryVisualStudioInstances().First();
            //MSBuildLocator.RegisterInstance(t);

            var analyzer = new Analyzer();

            var st = System.Diagnostics.Stopwatch.StartNew();
            await analyzer.StartAsync(cfg);
            st.Stop();

            var g = analyzer.GetCommandsEventsGraph();
        }
    }
}
