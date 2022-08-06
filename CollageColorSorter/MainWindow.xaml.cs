using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.PropertyGrid;

namespace CollageColorSorter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly GenerateCollage genColl;

        public MainWindow()
        {
            InitializeComponent();
            genColl = new GenerateCollage();
            DataContext = genColl;
        }


        private void BtnAddImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new()
            {
                DefaultExt = "*.bmp;*.jpg;*.jpeg;*.png",
                Filter = "Image Files|*.bmp;*.jpg;*.jpeg;*.png;",
                Multiselect = true
            };

            if (dlg.ShowDialog() == true)
            {
                foreach(string fileName in dlg.FileNames)
                {
                    genColl.ImageProps.Add(new CollageImageProperties(fileName));
                }
            }

            RefreshImageList();
        }

        private void BtnRemoveImage_Click(object sender, RoutedEventArgs e)
        {
            if (FindName("imgSelected") is Image imgSelected && FindName("lstImages") is ListBox lstImages)
            {
                genColl.RemoveSelectedImage(imgSelected, lstImages.SelectedIndex);
                lstImages.SelectedIndex = lstImages.SelectedIndex - 1 > -1 ? lstImages.SelectedIndex - 1 : 0;
                RefreshImageList();
                RefreshCollagePreview();
            }
        }

        private void BtnPreviewCollage_Click(object sender, RoutedEventArgs e)
        {
            if (FindName("imgPreview") is Image imgPreview)
            {
                if (imgPreview.Source != null)
                {
                    imgPreview.Source = null;
                }
                else
                {
                    genColl.CreateCollagePreview(imgPreview);
                }
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            genColl.ExportCollage();
        }

        private void LstImages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FindName("imgSelected") is Image imgSelected &&
                FindName("stpImageProps") is StackPanel stpImageProps &&
                FindName("lstImages") is ListBox lstImages &&
                FindName("stpPropButtons") is StackPanel stpPropButtons)
            {
                genColl.DisplaySelectedImageWithProps(imgSelected, stpImageProps, lstImages.SelectedIndex);
                stpPropButtons.Visibility = Visibility.Visible;

                if (FindName("btnRemoveImage") is Button btnRemoveImage)
                {
                    if (lstImages.SelectedIndex > -1 && lstImages.SelectedIndex < lstImages.Items.Count)
                    {
                        btnRemoveImage.IsEnabled = true;
                    }
                    else
                    {
                        btnRemoveImage.IsEnabled = false;
                    }
                }
            }
        }

        private void RefreshImageList()
        {
            if (FindName("lstImages") is ListBox lstImages)
            {
                int selectedIndex = lstImages.SelectedIndex;
                lstImages.Items.Clear();
                foreach (CollageImageProperties props in genColl.ImageProps)
                {
                    ListBoxItem lstImagesItem = new()
                    {
                        Content = Path.GetFileName(props.FilePath)
                    };
                    lstImages.Items.Add(lstImagesItem);
                }
                if (selectedIndex > -1 & selectedIndex < lstImages.Items.Count)
                {
                    lstImages.SelectedIndex = selectedIndex;
                }
            }
        }

        private void CmbSortType_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox cmbSortType)
            {
                cmbSortType.ItemsSource = Enum.GetValues(typeof(SortType)).Cast<SortType>();
            }

        }

        private void OptionChanged(object sender, RoutedEventArgs e)
        {
            if (sender is Control control &&
                (control.Name == "cmbSortType" || control.Name == "chkReverseOrder") &&
                FindName("cmbSortType") is ComboBox cmbSortType &&
                FindName("chkReverseOrder") is CheckBox chkReverseOrder &&
                cmbSortType.SelectedItem is SortType sortType)
            {
                genColl.SortImagePropsList(sortType, chkReverseOrder.IsChecked == true);
                RefreshImageList();
            }
            RefreshCollagePreview();
        }

        private void RefreshCollagePreview()
        {
            if (FindName("imgPreview") is Image imgPreview && imgPreview.Source != null)
            {
                genColl.CreateCollagePreview(imgPreview);
            }
        }

        private void BtnMoveItemUp_Click(object sender, RoutedEventArgs e)
        {
            if (FindName("lstImages") is ListBox lstImages)
            {
                genColl.SwapImageListItems(lstImages.SelectedIndex, lstImages.SelectedIndex - 1);
                if (lstImages.SelectedIndex - 1 > -1)
                {
                    lstImages.SelectedIndex -= 1;
                    RefreshImageList();
                    RefreshCollagePreview();
                }
                else
                {
                    lstImages.SelectedIndex = 0;
                }
            }
        }

        private void BtnMoveItemDown_Click(object sender, RoutedEventArgs e)
        {
            if (FindName("lstImages") is ListBox lstImages)
            {
                genColl.SwapImageListItems(lstImages.SelectedIndex, lstImages.SelectedIndex + 1);
                if (lstImages.SelectedIndex + 1 < lstImages.Items.Count)
                {
                    lstImages.SelectedIndex += 1;
                    RefreshImageList();
                    RefreshCollagePreview();
                }
                else
                {
                    lstImages.SelectedIndex = lstImages.Items.Count - 1;
                }
            }
        }

        private void BtnSetSizeAuto_Click(object sender, RoutedEventArgs e)
        {
            genColl.SetBestSize();
            if (FindName("iupCollageWidth") is IntegerUpDown cols && FindName("iupCollageHeight") is IntegerUpDown rows)
            {
                cols.Value = genColl.CollageColumns;
                rows.Value = genColl.CollageRows;
            }
            RefreshCollagePreview();
        }

        private void BackgroundColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            RefreshCollagePreview();
        }

        private void BtnCancelChanges_Click(object sender, RoutedEventArgs e)
        {
            if (FindName("imgSelected") is Image imgSelected &&
                FindName("stpImageProps") is StackPanel stpImageProps &&
                FindName("lstImages") is ListBox lstImages)
            {
                genColl.DisplaySelectedImageWithProps(imgSelected, stpImageProps, lstImages.SelectedIndex);
            }
        }

        private void BtnSaveChanges_Click(object sender, RoutedEventArgs e)
        {
            if (FindName("stpImageProps") is StackPanel stpImageProps && FindName("lstImages") is ListBox lstImages)
            {
                CollageImageProperties imageProps = genColl.ImageProps[lstImages.SelectedIndex];
                foreach (PropertyInfo propInfo in typeof(CollageImageProperties).GetProperties())
                {
                    string prefix = "";
                    if (propInfo.PropertyType.Equals(typeof(int)))
                    {
                        prefix = "iup";
                    }
                    else if (propInfo.PropertyType.Equals(typeof(bool)))
                    {
                        prefix = "chk";
                    }

                    if (FindName($"{prefix}Image{propInfo.Name}") is not Control control) continue;

                    // Need to make sure the string is in the right format ({prefix}Image{propName})
                    if (!string.IsNullOrWhiteSpace(control.Name) && control.Name.Length > 8)
                    {
                        // Obtain the part of the string after {prefix}Image
                        string propName = control.Name[8..];
                        if (imageProps.GetType().GetProperty(propName) is PropertyInfo imagePropInfo)
                        {
                            if (control as Label != null)
                            {
                                continue;
                            }
                            else if (control as IntegerUpDown != null)
                            {
                                var intUpDown = (IntegerUpDown)control;
                                imagePropInfo.SetValue(imageProps, intUpDown.Value);
                            }
                            else if (control as CheckBox != null)
                            {
                                var checkBox = (CheckBox)control;
                                imagePropInfo.SetValue(imageProps, checkBox.IsChecked);
                            }
                        }
                    }
                }
                RefreshCollagePreview();
            }
        }
    }
}
