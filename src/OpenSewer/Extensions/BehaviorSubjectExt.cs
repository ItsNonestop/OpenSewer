using System.Reactive.Subjects;

namespace OpenSewer.Extensions
{
    internal static class BehaviorSubjectExt
    {
        public static void Toggle(this BehaviorSubject<bool> subject) => subject.OnNext(!subject.Value);
    }
}

