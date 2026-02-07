//using System;
//using System.Collections.Generic;
//using System.Reactive.Linq;

//namespace OpenSewer.Extensions
//{
//    public static class IOBservableExt
//    {
//        public class HoverState<T>(bool IsHovering, T? Item);

//        public static IObservable<HoverState<T>> DetectHover<T>(this IObservable<T?> source)
//        {
//            return source
//                .Scan(
//                    (hovering: false, lastItem: (T?)default),
//                    (state, next) =>
//                    {
//                        if (next is not null)
//                        {
//                            // Hovering begins or continues
//                            return (hovering: true, lastItem: next);
//                        }

//                        // next is null
//                        if (state.hovering)
//                        {
//                            // Hover just ended
//                            return (hovering: false, lastItem: default);
//                        }

//                        // Still not hovering
//                        return state;
//                    })
//                .DistinctUntilChanged()
//                .Select(s => new HoverState<T>(s.hovering, s.lastItem));
//        }

//        public static IObservable<T?> StabilizeHover<T>(this IObservable<T?> source)
//        {
//            return source
//                .Scan(
//                    (prev: (T?)default, stable: (T?)default, hasPrev: false),
//                    (state, next) =>
//                    {
//                        // First value ever → becomes stable immediately
//                        if (!state.hasPrev)
//                            return (next, next, true);

//                        // If next == prev → stable value confirmed
//                        if (EqualityComparer<T?>.Default.Equals(state.prev, next))
//                            return (next, next, true);

//                        // Otherwise still fluctuating → update prev only
//                        return (next, state.stable, true);
//                    })
//                .Select(s => s.stable)
//                .DistinctUntilChanged();
//        }
//    }
//}

