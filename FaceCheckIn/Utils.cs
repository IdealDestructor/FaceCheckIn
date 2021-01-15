using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
namespace FaceCheckIn
{
    public class Utils
    {
        public static string ImageToBase64(Image image, ImageFormat format)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, format);
                byte[] imageBytes = ms.ToArray();

                string base64String = Convert.ToBase64String(imageBytes);
                return base64String;
            }
        }
    }
}
