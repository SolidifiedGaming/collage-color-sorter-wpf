using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DrawColor = System.Drawing.Color;

namespace CollageColorSorter
{
    enum SortType
    {
        Alphabetical,
        Color,
        DateCreated,
        DateModified
    }

    class CollageUtils
    {
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject([In] IntPtr hObject);

        internal static ImageSource ImageSourceFromBitmap(Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(handle);
            }
        }

        internal static ImageSource ImageSourceFromFile(string strFullyQualifiedFilename, int width = 0, int height = 0)
        {
            BitmapImage bmp = new();
            bmp.BeginInit();
            bmp.UriSource = new Uri("file:///" + strFullyQualifiedFilename.Replace("\\", "/"));
            bmp.EndInit();

            double scaleWidth = width > 0 ? width / bmp.Width : 1;
            double scaleHeight = height > 0 ? height / bmp.Height : 1;
            return new TransformedBitmap(bmp, new ScaleTransform(scaleWidth, scaleHeight));
        }

        internal static List<Image> GetImagesFromPaths(List<string> imagePaths)
        {
            List<Image> images = new();
            foreach (string imagePath in imagePaths)
            {
                Image image = Image.FromFile(imagePath);
                images.Add(image);
            }
            return images;
        }

        internal static ImageCodecInfo? GetEncoderInfo(string mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }

        internal static List<CollageImageProperties> SortImageProps(List<CollageImageProperties> imageProps, SortType sortType, bool descending)
        {
            var imagesWithProps = new List<(Bitmap, CollageImageProperties)>();
            var images = GetImagesFromPaths(imageProps.Select(i => i.FilePath).ToList());
            for (int i = 0; i < images.Count; i++)
            {
                imagesWithProps.Add((new Bitmap(images[i]), imageProps[i]));
            }

            switch (sortType)
            {
                case SortType.Color:
                    imagesWithProps.Sort((a, b) => GetAvgHue(a.Item1).CompareTo(GetAvgHue(b.Item1)));
                    break;
                case SortType.Alphabetical:
                    imagesWithProps.Sort((a, b) => a.Item2.FilePath.CompareTo(b.Item2.FilePath));
                    break;
                case SortType.DateCreated:
                    imagesWithProps.Sort((a, b) => GetFileInfo(a.Item2.FilePath).CreationTime.CompareTo(GetFileInfo(b.Item2.FilePath).CreationTime));
                    break;
                case SortType.DateModified:
                    imagesWithProps.Sort((a, b) => GetFileInfo(a.Item2.FilePath).LastWriteTime.CompareTo(GetFileInfo(b.Item2.FilePath).LastWriteTime));
                    break;
                default:
                    break;
            }

            if (descending)
            {
                imagesWithProps.Reverse();
            }
            return imagesWithProps.Select(i => i.Item2).ToList();
        }

        private static FileInfo GetFileInfo(string path)
        {
            return new FileInfo(path);
        }

        private static float GetAvgHue(Bitmap bmp)
        {
            var pixels = GraphicsUnit.Pixel;
            var bmpBounds = bmp.GetBounds(ref pixels);

            var pixelColors = new List<DrawColor>();
            for (int i = 0; i < bmpBounds.Width; i += 2)
            {
                pixelColors.Add(bmp.GetPixel(i, 0));
                pixelColors.Add(bmp.GetPixel(i, (int)bmpBounds.Height - 1));
            }
            for (int j = 0; j < bmpBounds.Height; j += 2)
            {
                pixelColors.Add(bmp.GetPixel(0, j));
                pixelColors.Add(bmp.GetPixel((int)bmpBounds.Width - 1, j));
            }

            return GetAvgColor(pixelColors).GetHue();
        }

        private static DrawColor GetAvgColor(List<DrawColor> colors)
        {
            var totals = new List<int>() { 0, 0, 0, 0 };
            foreach (var color in colors)
            {
                totals[0] += color.A ^ 2;
                totals[1] += color.R ^ 2;
                totals[2] += color.G ^ 2;
                totals[3] += color.B ^ 2;
            }
            for (int i = 0; i < totals.Count; i++)
            {
                totals[i] = (int)Math.Sqrt(totals[i] / colors.Count);
            }
            //totals.ForEach(t => t = (int)Math.Sqrt(t / colors.Count));

            return DrawColor.FromArgb(totals[0], totals[1], totals[2], totals[3]);
        }

        internal static List<int> GetFactors(int num)
        {
            var factors = new List<int>();
            int i = 1;
            while (i * i <= num)
            {
                if (num % i == 0)
                {
                    factors.Add(i);
                    if (num / i != i)
                    {
                        factors.Add(num / i);
                    }
                }
                i++;
            }
            return factors;
        }
    }
}
