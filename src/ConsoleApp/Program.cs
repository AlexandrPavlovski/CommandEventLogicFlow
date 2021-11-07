using System;
using System.Configuration;
using Core;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var cfg = new Config();

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

            var msWorkspace = MSBuildWorkspace.Create();
            var solutionPath = ConfigurationManager.AppSettings["SolutionPath"];
            var solution = msWorkspace.OpenSolutionAsync(solutionPath).Result;

            var analyzer = new Analyzer(solution, cfg);
            analyzer.Start();
        }
    }
}
