using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenSewer.Utility
{
    internal static class ItemHandler
    {
        private static IEnumerable<string> _categories;
        private static IEnumerable<Item> _items;

        public static IEnumerable<string> Categories
        {
            get { return _categories ??= GetCategories(); }
        }

        public static IEnumerable<Item> Items
        {
            get { return _items ??= GetItems(); }
        }

        static IEnumerable<string> GetCategories()
        {
            IEnumerable<string> categories = ItemDatabase.database
                .SelectMany(x => x.Categories)
                .Distinct()
                .OrderBy(x => x);

            return categories.ToList();
        }

        static IEnumerable<Item> GetItems()
        {
            var items = ItemDatabase.database
                .Where(x => x.ID != -1)
                .OrderBy(x => x.Title);

            return items.ToList();
        }
    }
}

