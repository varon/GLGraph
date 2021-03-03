using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using JetBrains.Annotations;
using ObjectTK;
using ObjectTK.GLObjects;
using OpenTK.Graphics.OpenGL;

namespace GLGraphs.ObjectTKExtensions.Text {

    public static class TextRendering {
        
        private struct TextItem {
            public string Text;
            public string Font;
            public int FontSize;
            public StringAlignment HorizontalAlignment;
        }


        private static readonly Dictionary<TextItem, Texture2D> _cache = new Dictionary<TextItem, Texture2D>();
        private static readonly Dictionary<(string, int), Font> _fontCache = new Dictionary<(string, int), Font>();

        [MustUseReturnValue]
        [NotNull]
        private static Font GetFont(string fontName, int fontSize) {
            if (!_fontCache.TryGetValue((fontName, fontSize), out var font)) {
                font = new Font(fontName, fontSize);
                _fontCache[(fontName, fontSize)] = font;
            }
            return font;
        }


        [MustUseReturnValue]
        [NotNull]
        private static Texture2D CreateTexture(TextItem ti) {
            var width =  (1+ti.Text.Length) * ti.FontSize;
            var height = ti.FontSize * 2f;
            var font = GetFont(ti.Font, ti.FontSize);

            var g2 = Graphics.FromHwnd(IntPtr.Zero);
            
            var format = new StringFormat {
                LineAlignment = StringAlignment.Center,
                Alignment = ti.HorizontalAlignment,
            };
            
            var size = g2.MeasureString(ti.Text, font,PointF.Empty, format);
            
            using var bmp = new Bitmap((int) MathF.Ceiling(size.Width), (int) MathF.Ceiling(size.Height));
            using var g = Graphics.FromImage(bmp);
            
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            g.SmoothingMode = SmoothingMode.HighQuality;
            var gu = GraphicsUnit.Pixel;
            var rect = bmp.GetBounds(ref gu);
            g.Clear(Color.White);
            
            g.DrawString(ti.Text, font, Brushes.Black, rect, format);
            g.Flush(FlushIntention.Sync);
            var name = $"Text[{ti.Font}-{ti.HorizontalAlignment}-{ti.FontSize}]: {ti.Text}";
            var tex = GLFactory.Texture.FromBitmap(name, TextureConfig.Default, bmp);
            GL.BindTexture(TextureTarget.Texture2D, tex.Handle);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) TextureWrapMode.ClampToEdge);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            return tex;
        }

        [MustUseReturnValue]
        [NotNull]
        public static Texture2D GetTextTexture(string text, StringAlignment horizontalAlignment = StringAlignment.Center, string fontName = "Roboto Condensed Light", int fontSize = 96) {
            var ti = new TextItem {
                Font = fontName,
                FontSize = fontSize,
                Text = text,
                HorizontalAlignment = horizontalAlignment
            };
            if (!_cache.TryGetValue(ti, out var tex)) {
                tex = CreateTexture(ti);
                _cache.Add(ti, tex);
            }
            return tex;
        }
    }
}
