using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows;

namespace CollageColorSorter
{
    class CollageImageProperties : INotifyPropertyChanged
    {
        private string filePath;
        public string FilePath
        {
            get { return filePath; }
            set { filePath = value; NotifyChange(); }
        }

        private int width;
        public int Width
        {
            get { return width; }
            set { width = value; NotifyChange(); }
        }

        private int height;
        public int Height
        {
            get { return height; }
            set { height = value; NotifyChange(); }
        }

        private bool flipHorizontal = false;
        public bool FlipHorizontal
        {
            get { return flipHorizontal; }
            set { flipHorizontal = value; NotifyChange(); }
        }

        private bool flipVertical = false;
        public bool FlipVertical
        {
            get { return flipVertical; }
            set { flipVertical = value; NotifyChange(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyChange([CallerMemberName] string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public CollageImageProperties(string filePath)
        {
            this.filePath = filePath;
            Bitmap bmp = (Bitmap)Image.FromFile(filePath);
            width = bmp.Width;
            height = bmp.Height;
            bmp.Dispose();
        }

        public static void SetPropBinding(ref System.Windows.Controls.Control control, DependencyProperty controlProp, string imageProp)
        {
            control.SetBinding(controlProp, imageProp);
        }
    }
}
