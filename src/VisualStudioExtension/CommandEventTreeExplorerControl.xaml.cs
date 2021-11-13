using Core.Graph;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Linq;

namespace VisualStudioExtension
{
    /// <summary>
    /// Interaction logic for CommandEventTreeExplorerControl.
    /// </summary>
    public partial class CommandEventTreeExplorerControl : UserControl
    {
        public VM vm { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandEventTreeExplorerControl"/> class.
        /// </summary>
        public CommandEventTreeExplorerControl()
        {
            vm = new VM();

            InitializeComponent();
            DataContext = vm;

            //MenuItem root = new MenuItem() { Text = "Menu" };
            //MenuItem childItem1 = new MenuItem() { Text = "Child item #1" };
            //childItem1.Items.Add(new MenuItem() { Text = "Child item #1.1" });
            //childItem1.Items.Add(new MenuItem() { Text = "Child item #1.2" });
            //root.Items.Add(childItem1);
            //root.Items.Add(new MenuItem() { Text = "Child item #2" });
            //treeView.Items.Add(root);

            //vm.Commands.Add(new GraphNode() { Name = "ASDASD" });
        }


        public void SetGraph(CommandsEventsGraph graph)
        {
            if (graph.Commands != null)
            {
                foreach (var item in graph.Commands)
                {
                    vm.Commands.Add(item);
                }
            }
        }

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Invoked '{0}'", this.ToString()),
                "Command Event Tree Explorer");
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

    public class MenuItem
    {
        public MenuItem()
        {
            this.Items = new ObservableCollection<MenuItem>();
        }

        public string Text { get; set; }

        public ObservableCollection<MenuItem> Items { get; set; }
    }
}