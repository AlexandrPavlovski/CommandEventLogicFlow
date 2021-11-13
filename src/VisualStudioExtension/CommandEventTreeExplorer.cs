using Core.Graph;
using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

namespace VisualStudioExtension
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("e42382b3-8d48-43e7-bbfc-f0fa5cc8aca8")]
    public class CommandEventTreeExplorer : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandEventTreeExplorer"/> class.
        /// </summary>
        public CommandEventTreeExplorer() : base(null)
        {
            this.Caption = "Command Event Tree Explorer";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new CommandEventTreeExplorerControl();
        }

        public void SetGraph(CommandsEventsGraph graph)
        {
            ((CommandEventTreeExplorerControl)Content).SetGraph(graph);
        }
    }
}
