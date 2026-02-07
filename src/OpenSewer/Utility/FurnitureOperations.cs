using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OpenSewer.Utility
{
    internal static class FurnitureOperations
    {
        public static bool TryGetFurnitureList(out IList<Furniture> furnitures)
        {
            furnitures = null;
            try
            {
                var source = DeveloperConsole.Instance?.furniture?.furnitures;
                if (source == null)
                    return false;

                furnitures = source as IList<Furniture> ?? source.ToList();
                return furnitures != null;
            }
            catch
            {
                furnitures = null;
                return false;
            }
        }

        public static bool IsReady()
        {
            try
            {
                return DeveloperConsole.Instance != null
                    && DeveloperConsole.Instance.furniture?.furnitures != null
                    && BuildingSystem.instance != null
                    && BuildingSystem.instance.availableFurnitures != null;
            }
            catch
            {
                return false;
            }
        }

        public static IEnumerable<(Furniture furniture, Furniture.Skin skin, string title, Sprite image, int ownedCount)> ExpandToCells(IList<Furniture> furnitures)
        {
            if (furnitures == null)
                yield break;

            foreach (var furniture in furnitures)
            {
                if (furniture == null)
                    continue;

                yield return (
                    furniture: furniture,
                    skin: null,
                    title: SafeTitle(furniture, null),
                    image: SafeImage(furniture, null),
                    ownedCount: GetOwnedCount(furniture, null)
                );

                var skins = furniture.skins;
                if (skins == null)
                    continue;

                foreach (var skin in skins)
                {
                    yield return (
                        furniture: furniture,
                        skin: skin,
                        title: SafeTitle(furniture, skin),
                        image: SafeImage(furniture, skin),
                        ownedCount: GetOwnedCount(furniture, skin)
                    );
                }
            }
        }

        public static BuildingSystem.FurnitureInfo.Meta MakeMeta(Furniture furniture, Furniture.Skin skin)
        {
            var meta = new BuildingSystem.FurnitureInfo.Meta();
            try
            {
                meta.currentSkin = SkinIndex(furniture, skin);
            }
            catch
            {
                meta.currentSkin = 0;
            }

            return meta;
        }

        public static void Spawn(Furniture furniture, Furniture.Skin skin)
        {
            try
            {
                if (furniture == null)
                    return;

                var buildingSystem = BuildingSystem.instance;
                if (buildingSystem == null)
                    return;

                buildingSystem.AddFurniture(furniture, 1, MakeMeta(furniture, skin));
            }
            catch
            {
                // no-op by design: GUI actions should not throw
            }
        }

        public static int GetOwnedCount(Furniture furniture, Furniture.Skin skin)
        {
            try
            {
                if (furniture == null)
                    return 0;

                var available = BuildingSystem.instance?.availableFurnitures;
                if (available == null)
                    return 0;

                int skinIndex = SkinIndex(furniture, skin);
                return available
                    .Where(af => af != null && af.furniture != null && af.meta != null)
                    .Where(af => af.furniture.Id == furniture.Id && af.meta.currentSkin == skinIndex)
                    .Sum(af => af.amount);
            }
            catch
            {
                return 0;
            }
        }

        private static int SkinIndex(Furniture furniture, Furniture.Skin skin)
        {
            if (skin == null || furniture?.skins == null)
                return 0;

            var list = furniture.skins as IList<Furniture.Skin> ?? furniture.skins.ToList();
            int index = list.IndexOf(skin);
            return index < 0 ? 0 : index + 1;
        }

        private static string SafeTitle(Furniture furniture, Furniture.Skin skin)
        {
            string baseTitle = string.IsNullOrWhiteSpace(furniture?.title)
                ? furniture?.name ?? "<unknown>"
                : furniture.title;

            if (skin == null)
                return baseTitle;

            string skinId = skin.id ?? string.Empty;
            return string.IsNullOrWhiteSpace(skinId) ? baseTitle : $"{baseTitle} {skinId}";
        }

        private static Sprite SafeImage(Furniture furniture, Furniture.Skin skin)
        {
            if (skin != null && skin.image != null)
                return skin.image;

            return furniture?.image;
        }
    }
}
