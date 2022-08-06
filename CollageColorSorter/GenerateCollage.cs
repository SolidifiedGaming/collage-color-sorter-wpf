using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xceed.Wpf.Toolkit;
using d = System.Drawing;

namespace CollageColorSorter
{
    class GenerateCollage : INotifyPropertyChanged
    {
        private int collageColumns = 1;
        public int CollageColumns
        {
            get { return collageColumns; }
            set
            {
                collageColumns = value > 1 ? value : 1;
                NotifyChange();
            }
        }

        private int collageRows = 1;
        public int CollageRows
        {
            get { return collageRows; }
            set
            {
                collageRows = value > 1 ? value : 1;
                NotifyChange();
            }
        }

        private int borderWidth = 0;
        public int BorderWidth
        {
            get { return borderWidth; }
            set
            {
                borderWidth = value > 0 ? value : 0;
                NotifyChange();
            }
        }

        private Color backgroundColor = Colors.Transparent;
        public Color BackgroundColor
        {
            get { return backgroundColor; }
            set { backgroundColor = value; NotifyChange(); }
        }

        private SortType sortType = SortType.Alphabetical;
        public SortType SortType
        {
            get { return sortType; }
            set { sortType = value; NotifyChange(); }
        }

        private List<CollageImageProperties> imageProps = new();
        public List<CollageImageProperties> ImageProps
        {
            get { return imageProps; }
            set { imageProps = value; NotifyChange(); }
        }

        private List<string> imagePaths = new();
        public List<string> ImagePaths
        {
            get { return imagePaths; }
            set { imagePaths = value; NotifyChange(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyChange([CallerMemberName] string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        internal void DisplaySelectedImageWithProps(Image image, StackPanel stackPanel, int selectedIndex)
        {
            if (selectedIndex > -1 && selectedIndex < imageProps.Count)
            {
                stackPanel.Children.Clear();
                foreach (PropertyInfo propInfo in typeof(CollageImageProperties).GetProperties())
                {
                    var stp = new StackPanel() { Orientation = Orientation.Horizontal };
                    Control? stpChild = null;

                    if (propInfo.PropertyType.Equals(typeof(int)))
                    {
                        stp.Children.Add(new Label()
                        {
                            Content = propInfo.Name,
                            VerticalContentAlignment = VerticalAlignment.Center
                        });
                        stpChild = new IntegerUpDown()
                        {
                            Name = $"iupImage{propInfo.Name}",
                            Value = (int?)propInfo.GetValue(imageProps[selectedIndex]),
                            Margin = new Thickness(8, 4, 8, 4)
                        };
                        if (stackPanel.FindName(stpChild.Name) != null)
                        {
                            stackPanel.UnregisterName(stpChild.Name);
                        }
                        stackPanel.RegisterName(stpChild.Name, stpChild as IntegerUpDown);
                    }
                    else if (propInfo.PropertyType.Equals(typeof(bool)))
                    {
                        stpChild = new CheckBox() 
                        { 
                            Name = $"chkImage{propInfo.Name}",
                            Content = propInfo.Name,
                            IsChecked = (bool?)propInfo.GetValue(imageProps[selectedIndex]),
                            Margin = new Thickness(8, 4, 8, 4)
                        };
                        if (stackPanel.FindName(stpChild.Name) != null)
                        {
                            stackPanel.UnregisterName(stpChild.Name);
                        }
                        stackPanel.RegisterName(stpChild.Name, stpChild as CheckBox);
                    }

                    if (stpChild != null)
                    {
                        stp.Children.Add(stpChild);
                        stackPanel.Children.Add(stp);
                    }
                }
                image.Source = CollageUtils.ImageSourceFromFile(ImageProps[selectedIndex].FilePath);
            }
        }

        internal void RemoveSelectedImage(Image imgSelected, int selectedIndex)
        {
            ImageProps.RemoveAt(selectedIndex);
            imgSelected.Source = null;
        }

        internal void SortImagePropsList(SortType sortType, bool descending)
        {
            imageProps = CollageUtils.SortImageProps(imageProps, sortType, descending);
        }

        internal void SwapImageListItems(int firstIndex, int secondIndex)
        {
            if (firstIndex > -1 && firstIndex < imageProps.Count &&
                secondIndex > -1 && secondIndex < imageProps.Count)
            {
                (imageProps[secondIndex], imageProps[firstIndex]) = (imageProps[firstIndex], imageProps[secondIndex]);
            }
        }

        internal void CreateCollagePreview(Image imgPreview)
        {
            var imagePaths = imageProps.Select(i => i.FilePath).ToList();
            var preview = MergeImages(CollageUtils.GetImagesFromPaths(imagePaths), borderWidth);
            if (preview != null)
            {
                imgPreview.Source = CollageUtils.ImageSourceFromBitmap(preview);
            }
        }

        internal void ExportCollage()
        {
            var imagePaths = imageProps.Select(i => i.FilePath).ToList();
            d.Bitmap? collage = MergeImages(CollageUtils.GetImagesFromPaths(imagePaths), 20);
            if (collage != null)
            {
                SaveFileDialog dlg = new()
                {
                    DefaultExt = ".bmp",
                    Filter = "BMP files (*.bmp)|*.bmp|JPG files (*.jpg, *.jpeg)|*.jpg;*.jpeg|PNG files (*.png)|*.png",
                    AddExtension = true,
                };
                if (dlg.ShowDialog() == true && Path.GetExtension(dlg.FileName) is string ext && ext.Length > 1)
                {
                    if (ext == ".jpg")
                    {
                        ext = ".jpeg";
                    }
                    string[] validExts = { ".bmp", ".jpeg", ".png" };
                    if (!validExts.Contains(ext))
                    {
                        ext = dlg.DefaultExt;
                    }

                    string mimeType = $"image/{ext[1..]}";
                    if (CollageUtils.GetEncoderInfo(mimeType) is d.Imaging.ImageCodecInfo encoder)
                    {
                        collage.Save(dlg.FileName, encoder, null);
                        Debug.WriteLine("File saved successfully.");
                    }
                }
            }
        }

        private d.Bitmap? MergeImages(List<d.Image> images, int space)
        {
            if (images.Count < 1) return null;

            int outputWidth = imageProps.Select(i => i.Width)
                .OrderByDescending(i => i)
                .Take(collageColumns)
                .Sum() + space * (collageColumns - 1);
            int outputHeight = imageProps.Select(i => i.Height)
                .OrderByDescending(i => i)
                .Take(collageRows)
                .Sum() + space * (collageRows - 1);
            d.Bitmap output = new(outputWidth, outputHeight);

            using d.Graphics g = d.Graphics.FromImage(output);
            g.Clear(d.Color.FromArgb(backgroundColor.A, backgroundColor.R, backgroundColor.G, backgroundColor.B));
            int currentX = 0;
            int currentY = 0;
            int maxRowHeight = 0;
            for (int i = 0; i < images.Count; i++)
            {
                int width = imageProps[i].Width > 0 ? imageProps[i].Width : images[i].Width;
                int height = imageProps[i].Height > 0 ? imageProps[i].Height : images[i].Height;
                if (imageProps[i].FlipHorizontal) images[i].RotateFlip(d.RotateFlipType.RotateNoneFlipX);
                if (imageProps[i].FlipVertical) images[i].RotateFlip(d.RotateFlipType.RotateNoneFlipY);

                g.DrawImage(images[i], currentX, currentY, width, height);
                currentX += width + space;
                if (height > maxRowHeight)
                {
                    maxRowHeight = height;
                }
                if ((i + 1) % collageColumns == 0)
                {
                    currentX = 0;
                    currentY += maxRowHeight + space;
                    maxRowHeight = 0;
                }
            }
            return output;
        }

        internal void SetBestSize()
        {
            int imageCount = imageProps.Count;
            if (Math.Sqrt(imageCount) % 1 == 0)
            {
                collageColumns = collageRows = (int)Math.Sqrt(imageCount);
                return;
            }

            var factors = CollageUtils.GetFactors(imageCount);
            if (factors.Count > 2)
            {
                int cols = collageColumns = factors[factors.Count / 2];
                int rows = collageRows = factors[(factors.Count / 2) + 1];
                if (cols > rows * 2 || rows > cols * 2)
                {
                    collageColumns = (int)Math.Sqrt(imageCount);
                    collageRows = (int)Math.Ceiling((double)imageCount / collageColumns);
                }
            }
            else
            {
                collageColumns = (int)Math.Sqrt(imageCount);
                collageRows = (int)Math.Ceiling((double)imageCount / collageColumns);
            }
        }
    }
}
