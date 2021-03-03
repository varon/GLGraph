using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using ObjectTK;
using ObjectTK.GLObjects;

namespace GLGraphs.ObjectTKExtensions {
    public static class TextureExtensions {
        
        [NotNull]
        private static Bitmap LoadFromRes(string name) {
            
            // retrieves THIS assembly, not the one that started the process.
            // in this way, it's always certain to load from here.
            var assembly = Assembly.GetExecutingAssembly();
            var resources = assembly.GetManifestResourceNames();
            var resourceName = resources.Single(s => s.EndsWith("." + name, StringComparison.OrdinalIgnoreCase));

            using var stream = assembly.GetManifestResourceStream(resourceName);
            var bmp = new Bitmap(stream!);
            return bmp;
        }
        
        
        /// Creates a 2D texture from an embedded resource image..
        [NotNull]
        [MustUseReturnValue]
        public static Texture2D FromEmbeddedImage([NotNull] this GLTextureFactory fact, TextureConfig cfg, string name) {
            var bmp = LoadFromRes(name);
            // update the name to not have the extension.
            name = Path.GetFileNameWithoutExtension(name);
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var bitmapData = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            return fact.Create2D(name, cfg, bmp.Width, bmp.Height, bitmapData.Scan0);
        }
        
        
        /// Creates a 2D texture from a bitmap.
        [NotNull]
        [MustUseReturnValue]
        public static Texture2D FromBitmap([NotNull] this GLTextureFactory fact, string name, TextureConfig cfg, Bitmap bmp) {
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var bitmapData = bmp.LockBits(rect, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            return fact.Create2D(name, cfg, bmp.Width, bmp.Height, bitmapData.Scan0);
        }

    }
}
