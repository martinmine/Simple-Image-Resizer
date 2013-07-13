using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace SimpleImageResizer
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// All the supported formats which this program can handle
        /// </summary>
        private static readonly string[] ACCEPTABLE_FORMATS = { "png", "jpg", "bmp" };
        /// <summary>
        /// The JPEG codec used to encode the output files
        /// </summary>
        private ImageCodecInfo jpegCodec;

        public Form1()
        {
            InitializeComponent();
            SetCodec();
            
            this.checkBox1.Checked = true;  // Make this window top-most by default
        }
        
        /// <summary>
        /// Checks if the file string is a supported format
        /// </summary>
        /// <param name="file">Full path to an image</param>
        /// <returns>True if support, false otherwise</returns>
        private static bool isValidFormat(string file)
        {
            for (int i = 0; i < ACCEPTABLE_FORMATS.Length; i++)
                if (file.EndsWith(ACCEPTABLE_FORMATS[i]))
                    return true;

            return false;
        }

        /// <summary>
        /// Event called when the topmost checkbox is checked in the UI
        /// </summary>
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            this.TopMost = checkBox1.Checked;
        }

        /// <summary>
        /// Enables the small icon which indicates you can drop an item on the blue box
        /// </summary>
        private void panel1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        /// <summary>
        /// Event called once an item has been dropped in the drop area
        /// </summary>
        private void panel1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            foreach (string file in files)
            {
                if (isValidFormat(file.ToLower()))
                    handleImage(file);
                else
                    MessageBox.Show("The file format of this file is unknown or not supported");
            }
        }

        /// <summary>
        /// Compresses and resize an image
        /// </summary>
        /// <param name="path">Path to the image</param>
        private void handleImage(string path)
        {
            Image img = Image.FromFile(path);   // Load image
            int quality = 100;  // Quality of the image
            int size = 0;       // The file size of the image
            int maxHeight;      // Maximum allowed height on the image
            int maxSize;        // Maximum allowed file size on the image

            string newPath = null;  // Path to the compressed/resized image

            if (!int.TryParse(outputKbSize.Text, out maxSize))
            {
                MessageBox.Show("Invalid text input size, must be numbers only!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!int.TryParse(maxHeightInput.Text, out maxHeight))
            {
                MessageBox.Show("Invalid max height, must be numbers only!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (img.Size.Height > maxHeight)    // Resize image if needed
            {
                int newHeight = maxHeight;
                int newWidth = (int)((float)img.Size.Width * ((float)newHeight / (float)img.Size.Height));
                resizeImage(ref img, new Size(newWidth, newHeight));
            }

            do
            {
                if (!string.IsNullOrEmpty(newPath))
                    File.Delete(newPath);

                newPath = path.Insert(path.Length - 4, " compressed");
                CompressImage(img, quality, newPath);

                FileInfo fileInfo = new FileInfo(newPath);  // Get the size of the output file
                size = (int)(fileInfo.Length / 1024);

                quality -= 10;  // Decrease compression rate with 10 at each try
            }
            while (size > maxSize && quality > 0);  // While compression rate is > 0 and size is above max size

            if (size > maxSize)
            {
                MessageBox.Show(string.Format("Unable to create an image as small as {0} kb for image {1}. Please reduce the output kb limit.", maxSize, path), 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            img.Dispose();
        }

        /// <summary>
        /// Sets the JPEG codec, called at program startup
        /// </summary>
        private void SetCodec()
        {
            //List all avaible codecs (system wide)
            ImageCodecInfo[] alleCodecs = ImageCodecInfo.GetImageEncoders();

            //Find and choose JPEG codec
            for (int i = 0; i < alleCodecs.Length && jpegCodec == null; i++)
                if (alleCodecs[i].MimeType == "image/jpeg")
                    jpegCodec = alleCodecs[i];
        }

        /// <summary>
        /// Compresses the image and saves the image
        /// </summary>
        /// <param name="sourceImage">The image to resize</param>
        /// <param name="imageQuality">Compression rate</param>
        /// <param name="savePath">Path to where the image shall be saved</param>
        private void CompressImage(Image sourceImage, int imageQuality, string savePath)
        {
            //Set quality factor for compression
            EncoderParameter imageQualitysParameter = new EncoderParameter(
                        System.Drawing.Imaging.Encoder.Quality, imageQuality);

            EncoderParameters codecParameter = new EncoderParameters(1);
            codecParameter.Param[0] = imageQualitysParameter;

            //Save compressed image
            sourceImage.Save(savePath, jpegCodec, codecParameter);
        }

        /// <summary>
        /// Resizes the image in height and width
        /// </summary>
        /// <param name="imgToResize">Referance to the image to resize</param>
        /// <param name="size">The size of the image</param>
        private static void resizeImage(ref Image imgToResize, Size size)
        {
            int sourceWidth = imgToResize.Width;
            int sourceHeight = imgToResize.Height;

            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;

            nPercentW = ((float)size.Width / (float)sourceWidth);
            nPercentH = ((float)size.Height / (float)sourceHeight);

            if (nPercentH < nPercentW)
                nPercent = nPercentH;
            else
                nPercent = nPercentW;

            int destWidth = (int)(sourceWidth * nPercent);
            int destHeight = (int)(sourceHeight * nPercent);

            Bitmap b = new Bitmap(destWidth, destHeight);
            using (Graphics g = Graphics.FromImage((Image)b))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
            }
            
            imgToResize.Dispose();
            imgToResize = (Image)b;
        }
    }
}
