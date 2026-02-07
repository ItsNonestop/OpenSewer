using UnityEngine;

namespace OpenSewer.Utility
{
    internal abstract class WindowBase : MonoBehaviour
    {
        internal readonly int windowId = nameof(OpenSewer).GetHashCode();
        internal Rect windowRect;
    }
}

