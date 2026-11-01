using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace LunarAccountManager
{
    /// <summary>
    /// Renders avatar images straight from the 64x64 skin texture that Mojang returns for a
    /// UUID. Third-party render sites (mc-heads, crafatar, minotar) often hand back the
    /// default Steve skin, which is why accounts appeared to wear the wrong skin.
    /// </summary>
    public static class SkinRender
    {
        public static Bitmap FetchTexture(string uuid, int timeoutMs)
        {
            string url = Web.GetSkinTextureUrl(uuid, timeoutMs);
            if (string.IsNullOrEmpty(url))
                return null;

            Image img = Web.TryDownloadImage(new[] { url }, timeoutMs);
            var bmp = img as Bitmap;
            if (bmp == null)
            {
                if (img != null) img.Dispose();
                return null;
            }
            if (bmp.Width < 64 || bmp.Height < 64)
            {
                bmp.Dispose();
                return null;
            }
            return bmp;
        }

        public static Bitmap Head(Bitmap skin, int size)
        {
            var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.Clear(Color.Transparent);
                g.DrawImage(skin, new Rectangle(0, 0, size, size), new Rectangle(8, 8, 8, 8), GraphicsUnit.Pixel);
                g.DrawImage(skin, new Rectangle(0, 0, size, size), new Rectangle(40, 8, 8, 8), GraphicsUnit.Pixel);
            }
            return bmp;
        }

        public static Bitmap Body(Bitmap skin, int width, int height)
        {
            var canvas = new Bitmap(16, 32);
            using (var g = Graphics.FromImage(canvas))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.Clear(Color.Transparent);

                // head (front + hat layer)
                g.DrawImage(skin, new Rectangle(4, 0, 8, 8), new Rectangle(8, 8, 8, 8), GraphicsUnit.Pixel);
                g.DrawImage(skin, new Rectangle(4, 0, 8, 8), new Rectangle(40, 8, 8, 8), GraphicsUnit.Pixel);

                // right arm, torso, left arm (front faces)
                g.DrawImage(skin, new Rectangle(0, 8, 4, 12), new Rectangle(44, 20, 4, 12), GraphicsUnit.Pixel);
                g.DrawImage(skin, new Rectangle(4, 8, 8, 12), new Rectangle(20, 20, 8, 12), GraphicsUnit.Pixel);
                g.DrawImage(skin, new Rectangle(12, 8, 4, 12), new Rectangle(36, 52, 4, 12), GraphicsUnit.Pixel);

                // legs (front faces)
                g.DrawImage(skin, new Rectangle(4, 20, 4, 12), new Rectangle(4, 20, 4, 12), GraphicsUnit.Pixel);
                g.DrawImage(skin, new Rectangle(8, 20, 4, 12), new Rectangle(20, 52, 4, 12), GraphicsUnit.Pixel);
            }

            var scaled = new Bitmap(width, height);
            using (var g = Graphics.FromImage(scaled))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.Clear(Color.Transparent);

                int drawW = width;
                int drawH = height;
                if (width * 2 > height) drawW = height / 2;   // keep the 1:2 body aspect
                else drawH = width * 2;
                g.DrawImage(canvas, new Rectangle((width - drawW) / 2, 0, drawW, drawH));
            }
            canvas.Dispose();
            return scaled;
        }
    }
}
