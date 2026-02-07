using UnityEngine;

namespace OpenSewer.Utility
{
    internal class Components
    {
        internal static void DrawSpriteInRect(Sprite sprite, Rect rect)
        {
            if (sprite == null) return;

            var tex = sprite.texture;
            var uv = new Rect(
                sprite.textureRect.x / tex.width,
                sprite.textureRect.y / tex.height,
                sprite.textureRect.width / tex.width,
                sprite.textureRect.height / tex.height
            );

            float spriteAspect = sprite.textureRect.width / sprite.textureRect.height;
            float targetAspect = rect.width / rect.height;

            Rect drawRect;
            if (spriteAspect > targetAspect)
            {
                float height = rect.width / spriteAspect;
                float y = rect.y + (rect.height - height) * 0.5f;
                drawRect = new Rect(rect.x, y, rect.width, height);
            }
            else
            {
                float width = rect.height * spriteAspect;
                float x = rect.x + (rect.width - width) * 0.5f;
                drawRect = new Rect(x, rect.y, width, rect.height);
            }

            GUI.DrawTextureWithTexCoords(drawRect, tex, uv);
        }

        internal static bool SpriteButton(Sprite sprite, float size)
        {
            if (sprite == null) return false;

            var tex = sprite.texture;

            // Reserve the button rect
            var rect = GUILayoutUtility.GetRect(size, size, GUI.skin.button);

            // Draw the button background + handle click
            if (GUI.Button(rect, GUIContent.none))
                return true;

            // Sprite UVs inside the atlas
            var uv = new Rect(
                sprite.textureRect.x / tex.width,
                sprite.textureRect.y / tex.height,
                sprite.textureRect.width / tex.width,
                sprite.textureRect.height / tex.height
            );

            // Compute aspect-correct inner rect
            float spriteAspect = sprite.textureRect.width / sprite.textureRect.height;
            float targetAspect = 1f; // 64x64 square

            Rect drawRect;

            if (spriteAspect > targetAspect)
            {
                // Sprite is wider → letterbox vertically
                float height = rect.width / spriteAspect;
                float y = rect.y + (rect.height - height) * 0.5f;
                drawRect = new Rect(rect.x, y, rect.width, height);
            }
            else
            {
                // Sprite is taller → letterbox horizontally
                float width = rect.height * spriteAspect;
                float x = rect.x + (rect.width - width) * 0.5f;
                drawRect = new Rect(x, rect.y, width, rect.height);
            }

            // Draw the sprite inside the padded rect
            GUI.DrawTextureWithTexCoords(drawRect, tex, uv);

            return false;
        }
    }
}

