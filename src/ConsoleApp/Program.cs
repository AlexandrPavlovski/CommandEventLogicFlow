using Core;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
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

            var analyzer = new Analyzer(cfg);
            await analyzer.StartAsync();
            var g = analyzer.GetCommandsEventsGraph();
        }
    }
}
