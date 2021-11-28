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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace VisualStudioExtension
{
    public partial class CommandEventTreeExplorerControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;


        public ObservableCollection<GraphNode> Commands { get; set; }

        private string _searchString { get; set; }
        public string SearchString 
        { 
            get => _searchString;
            set
            {
                _searchString = value;
                OnSearchStringChanged();
                OnPropertyChanged();
            }
        }

        private IProgress<AnalysisProgress> _progressUpdater;

        public CommandEventTreeExplorerControl()
        {
            _progressUpdater = new Progress<AnalysisProgress>(x => { progressText.Text = x.Description; progressBar.Value = x.Percent; });

            Commands = new ObservableCollection<GraphNode>();

            InitializeComponent();
            DataContext = this;

            //analyzeBtn.IsEnabled = false;

            Commands.Add(GetTestData("asdasda"));
            Commands.Add(GetTestData("rerferg"));
            Commands.Add(GetTestData("dsvxvxc"));
            Commands.Add(GetTestData("ssssss"));
            Commands.Add(GetTestData("ytjtyjghn"));
            Commands.Add(GetTestData("pokpokpokp"));
        }

        public void EnableAnalyzeButton()
        {
            analyzeBtn.IsEnabled = true;
        }

        public void DisableAnalyzeButton()
        {
            analyzeBtn.IsEnabled = false;
        }

        private async void analyzeBtn_Click(object sender, RoutedEventArgs e)
        {
            DisableAnalyzeButton();
            progressBar.Value = 0;

            treeView.Visibility = Visibility.Collapsed;
            progressContainer.Visibility = Visibility.Visible;

            //await System.Threading.Tasks.Task.Delay(1000);

            await Orchestrator.Analyzer.StartAsync(_progressUpdater);
            var graph = Orchestrator.Analyzer.GetCommandsEventsGraph();

            if (graph.Commands != null)
            {
                foreach (var item in graph.Commands.OrderBy(x => x.Text))
                {
                    Commands.Add(item);
                }
            }

            progressContainer.Visibility = Visibility.Collapsed;
            treeView.Visibility = Visibility.Visible;

            EnableAnalyzeButton();
        }

        private void clearSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            SearchString = null;
        }

        private void OnSearchStringChanged()
        {
            if (string.IsNullOrEmpty(SearchString))
            {
                foreach (GraphNode node in treeView.Items)
                {
                    ((TreeViewItem)treeView.ItemContainerGenerator.ContainerFromItem(node)).Visibility = Visibility.Visible;
                }
            }
            else
            {
                bool foundAny = false;
                foreach (GraphNode node in treeView.Items)
                {
                    var tvi = (TreeViewItem)treeView.ItemContainerGenerator.ContainerFromItem(node);

                    if (node.Text.Contains(SearchString))
                    {
                        tvi.Visibility = Visibility.Visible;
                        foundAny = true;
                    }
                    else
                    {
                        if (SearchInChildren(node))
                        {
                            tvi.Visibility = Visibility.Visible;
                            foundAny = true;
                        }
                        else
                        {
                            tvi.Visibility = Visibility.Collapsed;
                        }
                    }
                }
            }
        }

        private bool SearchInChildren(GraphNode node, int maxDepth = 1, int currentDepth = 1)
        {
            foreach (var childNode in node.Children)
            {
                if (childNode.Text.Contains(SearchString)
                    || (currentDepth != maxDepth && SearchInChildren(childNode, maxDepth, currentDepth + 1)))
                {
                    return true;
                }
            }
            return false;
        }


        private GraphNode GetTestData(string t)
        {
            var node = new GraphNode() { Text = t };

            for (int i = 0; i < 10; i++)
            {
                var node2 = new GraphNode() { Text = i.ToString() };
                node.AddChild(node2);

                for (int k = 0; k < 10; k++)
                {
                    node2.AddChild(new GraphNode() { Text = i.ToString() + k.ToString() });
                }
            }

            return node;
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
