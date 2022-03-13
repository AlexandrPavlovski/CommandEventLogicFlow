using System.Collections.Generic;
using VisualStudioExtension.Misc;

namespace VisualStudioExtension.ViewModels
{
    public class InstantiationInfoVM : IContextMenuItemVM
    {
        public string Text { get; set; }
        public string ProjectName { get; set; }
        public CodeLocation CodeLocation { get; set; }
        public List<InstantiationInfoVM> Methods { get; set; }
    }
}
