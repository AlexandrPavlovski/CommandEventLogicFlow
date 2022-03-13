using Core;
//using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace VisualStudioExtension
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(VisualStudioExtensionPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(CommandEventTreeExplorer))]
    [ProvideOptionPage(typeof(OptionPageGrid), "Command event logic flow", "Configuration", 0, 0, supportsAutomation: true)]
    public sealed class VisualStudioExtensionPackage : AsyncPackage
    {
        /// <summary>
        /// VisualStudioExtensionPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "58f16c87-7702-44ea-b553-a0a044f0df88";

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            //var componentModel = (IComponentModel)GetGlobalService(typeof(SComponentModel));
            //var workspace = componentModel.GetService<VisualStudioWorkspace>();
            //var solution = workspace.CurrentSolution;
            //var dte = Package.GetGlobalService(typeof(SDTE)) as DTE;


            OptionPageGrid page = (OptionPageGrid)GetDialogPage(typeof(OptionPageGrid));
            var cfg = new Config();

            cfg.ProjectThatContainsCommandInterface = page.ProjectThatContainsCommandInterface;
            cfg.ProjectThatContainsEventInterface = page.ProjectThatContainsEventInterface;
            cfg.CommandInterfaceTypeNameWithNamespace = page.CommandInterfaceTypeNameWithNamespace;
            cfg.EventInterfaceTypeNameWithNamespace = page.EventInterfaceTypeNameWithNamespace;
            cfg.HandlerMethodNames = page.HandlerMethodNames;
            cfg.HandlerMarkerInterfaceTypeNameWithNamespace = page.HandlerMarkerInterfaceTypeNameWithNamespace;


            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var solution = (IVsSolution)GetGlobalService(typeof(IVsSolution));
            solution.GetSolutionInfo(out _, out string solutionFilePath, out _);
            cfg.SolutionPath = solutionFilePath;

            var analyzer = new Analyzer(cfg);
            GodObject.Analyzer = analyzer;

            await CommandEventTreeExplorerCommand.InitializeAsync(this);

#if !TESTING
            SolutionEvents.OnAfterOpenSolution += GodObject.HandleAfterOpenSolution;
            SolutionEvents.OnBeforeCloseSolution += GodObject.HandleBeforeCloseSolution;

            bool isSolutionLoaded = await IsSolutionLoadedAsync();
            if (isSolutionLoaded)
            {
                GodObject.HandleAfterOpenSolution();
            }
#endif
        }

        private async Task<bool> IsSolutionLoadedAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var solService = (IVsSolution)(await GetServiceAsync(typeof(SVsSolution)));

            ErrorHandler.ThrowOnFailure(solService.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out object value));

            return value is bool isSolOpen && isSolOpen;
        }

        #endregion
    }
}
