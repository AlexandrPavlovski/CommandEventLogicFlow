using Core.Graph;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TestWpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


        public ObservableCollection<GraphNode> Commands { get; set; }


        public MainWindow()
        {
            Commands = new ObservableCollection<GraphNode>();

            InitializeComponent();
            DataContext = this;

            Commands.Add(GetTestData("asdasda"));
            Commands.Add(GetTestData("rerferg"));
            Commands.Add(GetTestData("dsvxvxc"));
            Commands.Add(GetTestData("ssssss"));
            Commands.Add(GetTestData("ytjtyjghn"));
            Commands.Add(GetTestData("pokpokpokp"));
        }

        private GraphNode GetTestData(string t)
        {
            var node = new GraphNode() { Text = t, Handlers2 = new List<string> { "asdasdad", "wrfw23rf234few" } };

            for (int i = 0; i < 10; i++)
            {
                var node2 = new GraphNode() { Text = i.ToString(), Handlers2 = new List<string> { "123123123123", ":L:LK:LK:LostKeyboardFocus:LK " } };
                node.AddChild(node2);

                for (int k = 0; k < 10; k++)
                {
                    node2.AddChild(new GraphNode() { Text = i.ToString() + k.ToString() + "ASDASDASDASDASD asdasdAsdasdasd asd asda sd" });
                }
            }

            return node;
        }

        private void StackPanel_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject;
            while (source != null && !(source is TreeViewItem))
            {
                source = VisualTreeHelper.GetParent(source);
            }
            var treeViewItem = source as TreeViewItem;

            if (treeViewItem != null)
            {
                treeViewItem.Focus();
                e.Handled = true;
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var t = ((MenuItem)sender).Tag as string;
        }
    }
}
