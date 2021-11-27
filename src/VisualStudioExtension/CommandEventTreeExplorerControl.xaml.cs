using Core.Graph;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using Core;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace VisualStudioExtension
{
    /// <summary>
    /// Interaction logic for CommandEventTreeExplorerControl.
    /// </summary>
    public partial class CommandEventTreeExplorerControl : UserControl
    {
        public VM vm { get; set; }

        private IProgress<AnalysisProgress> _progressUpdater;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandEventTreeExplorerControl"/> class.
        /// </summary>
        public CommandEventTreeExplorerControl()
        {
            vm = new VM();

            InitializeComponent();
            DataContext = vm;

            _progressUpdater = new Progress<AnalysisProgress>(x => { progressText.Text = x.Description; progressBar.Value = x.Percent; });

            //analyzeBtn.IsEnabled = false;
        }

        public void EnableAnalyzeButton()
        {
            analyzeBtn.IsEnabled = true;
        }

        public void DisableAnalyzeButton()
        {
            analyzeBtn.IsEnabled = false;
        }

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private async void analyzeBtn_Click(object sender, RoutedEventArgs e)
        {
            DisableAnalyzeButton();
            progressBar.Value = 0;

            treeView.Visibility = Visibility.Collapsed;
            progressContainer.Visibility = Visibility.Visible;

            //await System.Threading.Tasks.Task.Delay(1000);
            //vm.Commands.Add(new GraphNode { Name = "keelekekekekkekekekekekekkekekekkekekekkekekekkekekekekkekekk" });

            await Orchestrator.Analyzer.StartAsync(_progressUpdater);
            var graph = Orchestrator.Analyzer.GetCommandsEventsGraph();

            if (graph.Commands != null)
            {
                foreach (var item in graph.Commands.OrderBy(x => x.Name))
                {
                    vm.Commands.Add(item);
                }
            }

            progressContainer.Visibility = Visibility.Collapsed;
            treeView.Visibility = Visibility.Visible;

            EnableAnalyzeButton();
        }
    }

    public class VM
    {
        public ObservableCollection<GraphNode> Commands { get; set; }
        public string Text { get; set; }

        public VM()
        {
            Commands = new ObservableCollection<GraphNode>();
            Text = "asdads";
        }
    }
}