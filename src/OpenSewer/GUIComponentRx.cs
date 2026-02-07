using OpenSewer.Utility;
using System.Reactive.Subjects;

namespace OpenSewer
{
    internal partial class GUIComponent
    {
        readonly ReactiveHashSet<string> SelectedCategories = [];
        readonly BehaviorSubject<Item> HoverItem = new(null);
        readonly BehaviorSubject<string> TextFilter = new(string.Empty);
    }
}

