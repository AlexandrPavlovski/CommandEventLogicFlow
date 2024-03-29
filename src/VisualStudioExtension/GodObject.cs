﻿using Core;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualStudioExtension
{
    static class GodObject
    {
        public static Analyzer Analyzer { get; set; }
        public static Package Package { get; set; }


        private static CommandEventTreeExplorerControl _treeControl;
        public static CommandEventTreeExplorerControl TreeControl
        {
            get => _treeControl;
            set
            {
                _treeControl = value;

#if !TESTING
                if (_treeControl != null)
                {
                    if (IsSolutionOpen)
                        _treeControl.EnableAnalyzeButton();
                    else
                        _treeControl.DisableAnalyzeButton();
                }
#endif
            }
        }

        private static bool IsSolutionOpen;

        public static void HandleAfterOpenSolution(object sender = null, OpenSolutionEventArgs e = null)
        {
            IsSolutionOpen = true;
            if (TreeControl != null)
            {
                TreeControl.EnableAnalyzeButton();
            }
        }

        public static void HandleBeforeCloseSolution(object sender, EventArgs e)
        {
            IsSolutionOpen = false;
            if (TreeControl != null)
            {
                TreeControl.DisableAnalyzeButton();
                TreeControl.DisableToolBar();
                TreeControl.ClearTree();
            }
        }
    }
}
