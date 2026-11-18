using System.Collections.Generic;
using UnityEngine;

namespace PuzzleKingdoms.Views.Common
{
    /// <summary>
    /// Painted UI art (Meshy images in Resources/Art/UI): building pictures and chapter map paintings.
    /// Loaded as textures and wrapped into sprites once; a missing file gives null and callers fall back to
    /// the procedural look.
    /// </summary>
    public static class ArtSprites
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string name)
        {
            if (Cache.TryGetValue(name, out Sprite sprite) && sprite != null)
            {
                return sprite;
            }
            var texture = Resources.Load<Texture2D>("Art/UI/" + name);
            sprite = texture == null ? null
                : Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            Cache[name] = sprite;
            return sprite;
        }

        /// <summary>Picture of a Kingdom building by its rule id, or null.</summary>
        public static Sprite Building(string buildingId)
        {
            switch (buildingId)
            {
                case "barracks": return Get("Building_Barracks");
                case "archer_tower": return Get("Building_ArcherTower");
                case "mage_tower": return Get("Building_MageTower");
                case "treasury": return Get("Building_Treasury");
                default: return null;
            }
        }

        /// <summary>Painting behind the stage map of a chapter, or null.</summary>
        public static Sprite ChapterMap(int chapter) => Get("Map_Chapter" + chapter);
    }
}
