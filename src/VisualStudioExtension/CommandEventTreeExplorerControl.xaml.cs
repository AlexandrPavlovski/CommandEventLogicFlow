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
using System.Windows.Media;
using EnvDTE;
using Microsoft.VisualStudio;
using VisualStudioExtension.ViewModels;
using VisualStudioExtension.Misc;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace VisualStudioExtension
{
    public partial class CommandEventTreeExplorerControl : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


        private GraphNodeVM[] _commandsFirstTree;
        private GraphNodeVM[] _eventsFirstTree;

        private GraphNodeVM[] _treeItems;
        public GraphNodeVM[] TreeItems
        {
            get => _treeItems;
            set
            {
                _treeItems = value;
                OnPropertyChanged();
            }
        }

        private bool _isEventMode;
        public bool IsEventMode
        {
            get => _isEventMode;
            set
            {
                _isEventMode = value;
                OnModeChanged(value);
                OnPropertyChanged();
            }
        }

        private string _searchString;
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

            InitializeComponent();
            DataContext = this;

#if TESTING
            Commands.Add(GetTestData("asdasda"));
            Commands.Add(GetTestData("rerferg"));
            Commands.Add(GetTestData("dsvxvxc"));
            Commands.Add(GetTestData("ssssss"));
            Commands.Add(GetTestData("ytjtyjghn"));
            Commands.Add(GetTestData("pokpokpokp"));
#else
            analyzeBtn.IsEnabled = false;
#endif
        }

        public void EnableAnalyzeButton()
        {
            analyzeBtn.IsEnabled = true;
        }

        public void DisableAnalyzeButton()
        {
            analyzeBtn.IsEnabled = false;
        }

        public void EnableToolBar()
        {
            searchBoxContainer.Visibility = Visibility.Visible;
            modeToggleBtn.IsEnabled = true;
        }

        public void DisableToolBar()
        {
            searchBoxContainer.Visibility = Visibility.Collapsed;
            modeToggleBtn.IsEnabled = false;
        }

        public void ClearTree()
        {
            TreeItems = null;
            _commandsFirstTree = null;
            _eventsFirstTree = null;
        }

        private async void AnalyzeBtn_Click(object sender, RoutedEventArgs e)
        {
#if TESTING
            DisableAnalyzeButton();
            await System.Threading.Tasks.Task.Delay(1000);
            EnableAnalyzeButton();
            searchTextBox.IsEnabled = !searchTextBox.IsEnabled;
            clearSearchBtn.IsEnabled = searchTextBox.IsEnabled;
            return;
#else
            DisableAnalyzeButton();
            progressBar.Value = 0;

            ClearTree();

            treeView.Visibility = Visibility.Collapsed;
            progressContainer.Visibility = Visibility.Visible;

            try
            {
                var settings = (OptionPageGrid)GodObject.Package.GetDialogPage(typeof(OptionPageGrid));
                var cfg = settings.GetConfig();

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var solution = (IVsSolution)Package.GetGlobalService(typeof(IVsSolution));
                solution.GetSolutionInfo(out _, out string solutionFilePath, out _);

                cfg.SolutionPath = solutionFilePath;

                if (cfg.IsEmpty())
                {
                    _progressUpdater.Report(new AnalysisProgress(0, "First configure the extension.\r\nTools -> Options -> Command event logic flow"));
                    progressBar.Visibility = Visibility.Collapsed;
                }
                else
                {
                    await Task.Factory.StartNew(() =>
                    {
                        var st = System.Diagnostics.Stopwatch.StartNew();
                        GodObject.Analyzer.StartAsync(cfg, _progressUpdater).Wait();
                        st.Stop();
                    },
                        CancellationToken.None,
                        System.Threading.Tasks.TaskCreationOptions.None,
                        System.Threading.Tasks.TaskScheduler.Default
                    );

                    var graph = GodObject.Analyzer.GetCommandsEventsGraph();

                    if (graph.Commands != null)
                    {
                        _commandsFirstTree = graph.Commands.Select(x => new GraphNodeVM(x)).OrderBy(x => x.Text).ToArray();
                        _eventsFirstTree = graph.Events.Select(x => new GraphNodeVM(x)).OrderBy(x => x.Text).ToArray();

                        TreeItems = _isEventMode
                            ? _eventsFirstTree
                            : _commandsFirstTree;

                        EnableToolBar();
                    }

                    progressContainer.Visibility = Visibility.Collapsed;
                    treeView.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }

            EnableAnalyzeButton();
#endif
        }

        private void ClearSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            SearchString = null;
        }

        private void StackPanel_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
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

        private async void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var codeLocation = ((MenuItem)sender).Tag as CodeLocation;
                if (codeLocation == null)
                {
                    return;
                }

                await OpenFileAndMoveToLineAndCharacter(codeLocation.FilePath, codeLocation.Line + 1, codeLocation.Character + 1);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private void OnSearchStringChanged()
        {
            if (string.IsNullOrEmpty(SearchString))
            {
                foreach (GraphNodeVM node in treeView.Items)
                {
                    ((TreeViewItem)treeView.ItemContainerGenerator.ContainerFromItem(node)).Visibility = Visibility.Visible;
                }
            }
            else
            {
                bool foundAny = false;
                foreach (GraphNodeVM node in treeView.Items)
                {
                    var tvi = (TreeViewItem)treeView.ItemContainerGenerator.ContainerFromItem(node);

                    if (node.Text.IndexOf(SearchString, StringComparison.OrdinalIgnoreCase) >= 0)
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

        private bool SearchInChildren(GraphNodeVM node, int maxDepth = 1, int currentDepth = 1)
        {
            if (node.Children != null)
            {
                foreach (var childNode in node.Children)
                {
                    if (childNode.Text.Contains(SearchString)
                        || (currentDepth != maxDepth && SearchInChildren(childNode, maxDepth, currentDepth + 1)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void OnModeChanged(bool isEventMode)
        {
            TreeItems = isEventMode
                ? _eventsFirstTree
                : _commandsFirstTree;

            if (!string.IsNullOrEmpty(_searchString))
            {
                OnSearchStringChanged();
            }
        }

        private async void OnKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                if (e.Key != System.Windows.Input.Key.F12
                        || treeView.SelectedItem == null)
                    return;

                var definitionLocation = ((GraphNodeVM)treeView.SelectedItem).DefinitionLocation;

                await OpenFileAndMoveToLineAndCharacter(definitionLocation.FilePath, definitionLocation.Line + 1, definitionLocation.Character + 1);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private async Task OpenFileAndMoveToLineAndCharacter(string filePath, int line, int character)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
            using (new NewDocumentStateScope(__VSNEWDOCUMENTSTATE.NDS_Provisional, VSConstants.NewDocumentStateReason.SolutionExplorer))
            {
                dte.ItemOperations.OpenFile(filePath);
            }
            (dte.ActiveDocument.Selection as TextSelection).MoveToLineAndOffset(line, character);
        }

        private void HandleException(Exception ex)
        {
            _progressUpdater.Report(new AnalysisProgress(0, "Error =(\r\nSee Output Window -> Extensions for details"));
            progressBar.Visibility = Visibility.Collapsed;
            ex.Log();
        }
    }
}
