using Core.Graph;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using Core;

namespace VisualStudioExtension
{
    /// <summary>
    /// Interaction logic for CommandEventTreeExplorerControl.
    /// </summary>
    public partial class CommandEventTreeExplorerControl : UserControl
    {
        public Analyzer Analyzer { get; set; }

        public VM vm { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandEventTreeExplorerControl"/> class.
        /// </summary>
        public CommandEventTreeExplorerControl()
        {
            vm = new VM();

            InitializeComponent();
            DataContext = vm;
        }

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private async void button1_Click(object sender, RoutedEventArgs e)
        {
            await Analyzer.StartAsync();
            var graph = Analyzer.GetCommandsEventsGraph();

            if (graph.Commands != null)
            {
                foreach (var item in graph.Commands)
                {
                    vm.Commands.Add(item);
                }
            }
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