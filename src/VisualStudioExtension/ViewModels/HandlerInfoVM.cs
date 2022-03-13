using VisualStudioExtension.Misc;

namespace VisualStudioExtension.ViewModels
{
    public class HandlerInfoVM : IContextMenuItemVM
    {
        public string Text { get; set; }
        public CodeLocation CodeLocation { get; set; }
    }
}
