using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace JA_Image_Project
{
    internal struct BitmapInfo
    {
        public Bitmap bmpNew { get; set; }
        // Lock image bits for read/write.
        public BitmapData bmpData { get; set; }
        public IntPtr ptr { get; set; }
        // Declare an array to hold the bytes of the bitmap.
        public byte[] byteBuffer { get; set; }
    }

    public static class Filters
    {
        private static BitmapInfo GetBitmapInfo(Image sourceImage)
        {
            BitmapInfo bitmapInfo = new BitmapInfo();
            //initializing components
            bitmapInfo.bmpNew = GetArgbCopy(sourceImage);
            bitmapInfo.bmpData = bitmapInfo.bmpNew.LockBits(
                new Rectangle(0, 0, sourceImage.Width, sourceImage.Height), 
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            bitmapInfo.ptr = bitmapInfo.bmpData.Scan0;
            bitmapInfo.byteBuffer = new byte[bitmapInfo.bmpData.Stride * bitmapInfo.bmpNew.Height];
            return bitmapInfo;
        }
        
        private static Bitmap GetArgbCopy(Image sourceImage)
        {
            Bitmap bmpNew = new Bitmap(sourceImage.Width, sourceImage.Height, PixelFormat.Format32bppArgb);

            using (Graphics graphics = Graphics.FromImage(bmpNew))
            {
                graphics.DrawImage(sourceImage, new Rectangle(0, 0, bmpNew.Width, bmpNew.Height), new Rectangle(0, 0, bmpNew.Width, bmpNew.Height), GraphicsUnit.Pixel);
                graphics.Flush();
            }
            return bmpNew;
        }

        public static Bitmap CopyAsSepiaTone(this Image sourceImage)
        {
            BitmapInfo bitmapInfo = GetBitmapInfo(sourceImage);

            Marshal.Copy(bitmapInfo.ptr, bitmapInfo.byteBuffer, 0, bitmapInfo.byteBuffer.Length);

            byte maxValue = 255;
            float r = 0;
            float g = 0;
            float b = 0;

            for (int k = 0; k < bitmapInfo.byteBuffer.Length; k += 4)
            {
                r = bitmapInfo.byteBuffer[k] * 0.189f + bitmapInfo.byteBuffer[k + 1] * 0.769f + bitmapInfo.byteBuffer[k + 2] * 0.393f;
                g = bitmapInfo.byteBuffer[k] * 0.168f + bitmapInfo.byteBuffer[k + 1] * 0.686f + bitmapInfo.byteBuffer[k + 2] * 0.349f;
                b = bitmapInfo.byteBuffer[k] * 0.131f + bitmapInfo.byteBuffer[k + 1] * 0.534f + bitmapInfo.byteBuffer[k + 2] * 0.272f;

                bitmapInfo.byteBuffer[k + 2] = (r > maxValue ? maxValue : (byte)r);
                bitmapInfo.byteBuffer[k + 1] = (g > maxValue ? maxValue : (byte)g);
                bitmapInfo.byteBuffer[k] = (b > maxValue ? maxValue : (byte)b);
            }

            Marshal.Copy(bitmapInfo.byteBuffer, 0, bitmapInfo.ptr, bitmapInfo.byteBuffer.Length);

            bitmapInfo.bmpNew.UnlockBits(bitmapInfo.bmpData);

            bitmapInfo.bmpData = null;
            bitmapInfo.byteBuffer = null;

            return bitmapInfo.bmpNew;
        }

        public static Bitmap Sharpening(this Image sourceImage)
        {
            BitmapInfo bitmapInfo = GetBitmapInfo(sourceImage);

            const int filterWidth = 3;
            const int filterHeight = 3;
            int width = bitmapInfo.bmpNew.Width;
            int height = bitmapInfo.bmpNew.Height;

            // Create sharpening filter.
            var filter = new double[filterWidth, filterHeight];
            filter[0, 1] = filter[1, 0] = filter[1, 2] = filter[2, 1] = -1;
            filter[0, 0] = filter[2, 0] = filter[0, 2] = filter[2, 2] = 0;
            filter[1, 1] = 5;

            const double factor = 1.0;
            const double bias = 0.0;

            var result = new Color[bitmapInfo.bmpNew.Width, bitmapInfo.bmpNew.Height];

            // Copy the RGB values into the array.
            Marshal.Copy(bitmapInfo.ptr, bitmapInfo.byteBuffer, 0, bitmapInfo.byteBuffer.Length);

            int rgb;
            // Fill the color array with the new sharpened color values.
            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    double red = 0.0, green = 0.0, blue = 0.0, alpha = 0.0;

                    for (int filterX = 0; filterX < filterWidth; filterX++)
                    {
                        for (int filterY = 0; filterY < filterHeight; filterY++)
                        {
                            int imageX = (x - filterWidth / 2 + filterX + width) % width;
                            int imageY = (y - filterHeight / 2 + filterY + height) % height;

                            rgb = imageY * bitmapInfo.bmpData.Stride + 4 * imageX;

                            red += bitmapInfo.byteBuffer[rgb + 2] * filter[filterX, filterY];
                            green += bitmapInfo.byteBuffer[rgb + 1] * filter[filterX, filterY];
                            blue += bitmapInfo.byteBuffer[rgb + 0] * filter[filterX, filterY];
                            alpha += bitmapInfo.byteBuffer[rgb + 3];
                        }
                        int r = Math.Min(Math.Max((int)(factor * red + bias), 0), 255);
                        int g = Math.Min(Math.Max((int)(factor * green + bias), 0), 255);
                        int b = Math.Min(Math.Max((int)(factor * blue + bias), 0), 255);
                        int a = Math.Min(Math.Max((int)(factor * alpha + bias), 0), 255);
                        result[x, y] = Color.FromArgb(a, r, g, b);
                    }
                }
            }

            // Update the image with the sharpened pixels.
            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    rgb = y * bitmapInfo.bmpData.Stride + 4 * x;

                    bitmapInfo.byteBuffer[rgb + 3] = result[x, y].A;
                    bitmapInfo.byteBuffer[rgb + 2] = result[x, y].R;
                    bitmapInfo.byteBuffer[rgb + 1] = result[x, y].G;
                    bitmapInfo.byteBuffer[rgb + 0] = result[x, y].B;
                }
            }

            // Copy the RGB values back to the bitmap.
            Marshal.Copy(bitmapInfo.byteBuffer, 0, bitmapInfo.ptr, bitmapInfo.byteBuffer.Length);
            // Release image bits.
            bitmapInfo.bmpNew.UnlockBits(bitmapInfo.bmpData);

            return bitmapInfo.bmpNew;
        }

        public static Bitmap BlurOnMatrix(this Image sourceImage)
        {
            BitmapInfo bitmapInfo = GetBitmapInfo(sourceImage);

            const int filterWidth = 3;
            const int filterHeight = 3;
            int width = bitmapInfo.bmpNew.Width;
            int height = bitmapInfo.bmpNew.Height;
            const int divisor = 16;

            // Create sharpening filter.
            var filter = new double[filterWidth, filterHeight];
            filter[0, 1] = filter[1, 0] = filter[1, 2] = filter[2, 1] = 2;
            filter[0, 0] = filter[2, 0] = filter[0, 2] = filter[2, 2] = 1;
            filter[1, 1] = 4;

            const double factor = 1.0;
            const double bias = 0.0;

            var result = new Color[bitmapInfo.bmpNew.Width, bitmapInfo.bmpNew.Height];

            // Copy the RGB values into the array.
            Marshal.Copy(bitmapInfo.ptr, bitmapInfo.byteBuffer, 0, bitmapInfo.byteBuffer.Length);

            int rgb;
            // Fill the color array with the new sharpened color values.
            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    double red = 0.0, green = 0.0, blue = 0.0, alpha = 0.0;

                    for (int filterX = 0; filterX < filterWidth; filterX++)
                    {
                        for (int filterY = 0; filterY < filterHeight; filterY++)
                        {
                            int imageX = (x - filterWidth / 2 + filterX + width) % width;
                            int imageY = (y - filterHeight / 2 + filterY + height) % height;

                            rgb = imageY * bitmapInfo.bmpData.Stride + 4 * imageX;

                            red += (bitmapInfo.byteBuffer[rgb + 2] * filter[filterX, filterY]) / divisor;
                            green += (bitmapInfo.byteBuffer[rgb + 1] * filter[filterX, filterY] / divisor);
                            blue += (bitmapInfo.byteBuffer[rgb + 0] * filter[filterX, filterY] / divisor);
                            alpha += bitmapInfo.byteBuffer[rgb + 3];
                        }
                        int r = Math.Min(Math.Max((int)(factor * red + bias), 0), 255);
                        int g = Math.Min(Math.Max((int)(factor * green + bias), 0), 255);
                        int b = Math.Min(Math.Max((int)(factor * blue + bias), 0), 255);
                        int a = Math.Min(Math.Max((int)(factor * alpha + bias), 0), 255);
                        result[x, y] = Color.FromArgb(a, r, g, b);
                    }
                }
            }

            // Update the image with the sharpened pixels.
            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    rgb = y * bitmapInfo.bmpData.Stride + 4 * x;

                    bitmapInfo.byteBuffer[rgb + 3] = result[x, y].A;
                    bitmapInfo.byteBuffer[rgb + 2] = result[x, y].R;
                    bitmapInfo.byteBuffer[rgb + 1] = result[x, y].G;
                    bitmapInfo.byteBuffer[rgb + 0] = result[x, y].B;
                }
            }

            // Copy the RGB values back to the bitmap.
            Marshal.Copy(bitmapInfo.byteBuffer, 0, bitmapInfo.ptr, bitmapInfo.byteBuffer.Length);
            // Release image bits.
            bitmapInfo.bmpNew.UnlockBits(bitmapInfo.bmpData);

            return bitmapInfo.bmpNew;
        }

        public static Bitmap SaltAndPepperFilter(this Image sourceImage)
        {
            BitmapInfo bitmapInfo = GetBitmapInfo(sourceImage);

            const int filterWidth = 3;
            const int filterHeight = 3;
            int width = bitmapInfo.bmpNew.Width;
            int height = bitmapInfo.bmpNew.Height;
            var macierzPixeli = new Int32[9];

            var result = new Color[bitmapInfo.bmpNew.Width, bitmapInfo.bmpNew.Height];

            // Copy the RGB values into the array.
            Marshal.Copy(bitmapInfo.ptr, bitmapInfo.byteBuffer, 0, bitmapInfo.byteBuffer.Length);

            int rgb;
                // Fill the color array with the new sharpened color values.
            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    int indeks = 0;
                    for (int filterX = 0; filterX < filterWidth; filterX++)
                    {
                        for (int filterY = 0; filterY < filterHeight; filterY++)
                        {
                            int imageX = (x - filterWidth / 2 + filterX + width) % width;
                            int imageY = (y - filterHeight / 2 + filterY + height) % height;
                            rgb = imageY * bitmapInfo.bmpData.Stride + 4 * imageX;

                            Color pomocColor = Color.FromArgb(bitmapInfo.byteBuffer[rgb + 3], bitmapInfo.byteBuffer[rgb + 2],
                                bitmapInfo.byteBuffer[rgb + 1], bitmapInfo.byteBuffer[rgb + 0]);
                            macierzPixeli[indeks] = pomocColor.ToArgb();
                            indeks++;
                        }
                    }
                    Array.Sort(macierzPixeli);
                    result[x, y] = Color.FromArgb(macierzPixeli[5]);
                }
            }

            // Update the image with the sharpened pixels.
            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    rgb = y * bitmapInfo.bmpData.Stride + 4 * x;

                    bitmapInfo.byteBuffer[rgb + 3] = result[x, y].A;
                    bitmapInfo.byteBuffer[rgb + 2] = result[x, y].R;
                    bitmapInfo.byteBuffer[rgb + 1] = result[x, y].G;
                    bitmapInfo.byteBuffer[rgb + 0] = result[x, y].B;
                }
            }

            // Copy the RGB values back to the bitmap.
            Marshal.Copy(bitmapInfo.byteBuffer, 0, bitmapInfo.ptr, bitmapInfo.byteBuffer.Length);
            // Release image bits.
            bitmapInfo.bmpNew.UnlockBits(bitmapInfo.bmpData);

            return bitmapInfo.bmpNew;
        }

//Nice blur, but tooo sloooowwwwwww on pixelset
#region 

        public static Bitmap Blur(this Image sourceImage)
        {
            Int32 blurSize = 3;
            Bitmap blurImage = GetArgbCopy(sourceImage);
            return Blur(blurImage, new Rectangle(0, 0, blurImage.Width, blurImage.Height), blurSize);
        }

        private static Bitmap Blur(Bitmap image, Rectangle rectangle, Int32 blurSize)
        {
            // look at every pixel in the blur rectangle
            for (Int32 xx = rectangle.X; xx < rectangle.X + rectangle.Width; xx++)
            {
                for (Int32 yy = rectangle.Y; yy < rectangle.Y + rectangle.Height; yy++)
                {
                    Int32 avgR = 0, avgG = 0, avgB = 0;
                    Int32 blurPixelCount = 0;

                    // average the color of the red, green and blue for each pixel in the
                    // blur size while making sure you don't go outside the image bounds
                    for (Int32 x = xx; (x < xx + blurSize && x < image.Width); x++)
                    {
                        for (Int32 y = yy; (y < yy + blurSize && y < image.Height); y++)
                        {
                            Color pixel = image.GetPixel(x, y);

                            avgR += pixel.R;
                            avgG += pixel.G;
                            avgB += pixel.B;

                            blurPixelCount++;
                        }
                    }

                    avgR = avgR / blurPixelCount;
                    avgG = avgG / blurPixelCount;
                    avgB = avgB / blurPixelCount;

                    // now that we know the average for the blur size, set each pixel to that color
                    for (Int32 x = xx; x < xx + blurSize && x < image.Width && x < rectangle.Width; x++)
                        for (Int32 y = yy; y < yy + blurSize && y < image.Height && y < rectangle.Height; y++)
                            image.SetPixel(x, y, Color.FromArgb(avgR, avgG, avgB));
                }
            }
            return image;
        }
#endregion

    }
}
