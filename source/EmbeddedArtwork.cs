using System;
using System.IO;
using UnityEngine;

namespace Dressmaker.DeskMail
{
    internal sealed class EmbeddedArtwork : IDisposable
    {
        public const string ResourcePrefix = "Dressmaker.DeskMail.Assets.";
        private Texture2D texture;
        public Sprite Sprite { get; private set; }
        public float Aspect => (float)texture.width / texture.height;

        public static EmbeddedArtwork Load(string filename, bool readable = false)
        {
            var artwork = new EmbeddedArtwork();
            try
            {
                using (var input = typeof(EmbeddedArtwork).Assembly.GetManifestResourceStream(ResourcePrefix + filename))
                {
                    if (input == null) throw new InvalidOperationException("Missing embedded artwork: " + filename);
                    using (var bytes = new MemoryStream())
                    {
                        input.CopyTo(bytes);
                        artwork.texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        // Only the holder needs CPU pixels for alpha-based hit testing.
                        if (!ImageConversion.LoadImage(artwork.texture, bytes.ToArray(), !readable))
                            throw new InvalidOperationException("Could not decode artwork: " + filename);
                    }
                }
                artwork.texture.name = "DeskMail." + filename;
                artwork.texture.filterMode = FilterMode.Bilinear;
                artwork.texture.wrapMode = TextureWrapMode.Clamp;
                artwork.Sprite = Sprite.Create(artwork.texture,
                    new Rect(0, 0, artwork.texture.width, artwork.texture.height),
                    new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect);
                artwork.Sprite.name = "DeskMail." + filename + ".Sprite";
                return artwork;
            }
            catch { artwork.Dispose(); throw; }
        }

        public void Dispose()
        {
            if (Sprite) UnityEngine.Object.Destroy(Sprite);
            if (texture) UnityEngine.Object.Destroy(texture);
            Sprite = null;
            texture = null;
        }
    }
}
