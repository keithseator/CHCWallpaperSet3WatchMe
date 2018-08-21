using System;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;

public enum DeviceCap
{
    HORZRES = 8,
    VERTRES = 10,
    LOGPIXELSX = 88,
    LOGPIXELSY = 90,
    DESKTOPVERTRES = 117,
    DESKTOPHORZRES = 118

    // http://pinvoke.net/default.aspx/gdi32/GetDeviceCaps.html
}

namespace CHCWallpaperSet3
{
    class Program
    {
        // Dll to import for fonts - so that the Futura font can be used without being installed on the client
        [DllImport("gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont, IntPtr pdv, [In] ref uint pcFonts);
        static FontFamily ff;

        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        // CHC colours
        //const string chcBlueColourHex = "#004685";
        const string chcGreyColourHex = "#383A35";
        //const string chcOrangeColourHex = "#F04F23";

        static Color _color = ColorTranslator.FromHtml(chcGreyColourHex);

        // Location to save the wallpaper and Logon screen
        static string saveWallpaperLocation = @"C:\ProgramData\Microsoft\CHC\CHC Wallpaper\CHCWallpaper.bmp";
        static string saveLognScreenLocation = @"C:\Windows\Sysnative\oobe\info\backgrounds\backgroundDefault.jpg";
        static string watchMeMessage = @"Watch Me: Who's got your back today?";

        static int totalWidth = 0;
        static int maxHeight = 0;
        static int lowestXCoOrdinate = 0;
        static int lowestYCoOrdinate = 0;
        static int maxXCoOrdinate = 0;
        static int maxYCoOrdinate = 0;
        static float ScreenScalingFactor = 0;
        static int margin = 25;
        static double fontSizeScaled = double.MinValue;
        static double chcBirdLogoScale = double.MinValue;
        static double serviceDeskInfoScale = double.MinValue;

        static void Main(string[] args)
        {
            // Create the user / computer info text
            string infoString = CreateInfoString();

            // Load the Lucida Grande font for displaying the info string
            LoadFont();

            // Set alignment to the right
            StringFormat infoStringFormat = new StringFormat();
            infoStringFormat.Alignment = StringAlignment.Far;

            StringFormat infoStringFormatWatchMe = new StringFormat();
            infoStringFormatWatchMe.Alignment = StringAlignment.Center;

            // Set the background depending on Windows 7 or Windows 10
            if (DetermineOSHigherThanWindow7())
            {
                CreateWindows10Wallpaper(infoString, infoStringFormat);
            }
            else
            {
                CreateWindows7Wallpaper(infoString, infoStringFormat);
            }

            // Set the desktop backround to the newly created image
            try
            {
                Uri uri = new Uri(saveWallpaperLocation, UriKind.Relative);
                Wallpaper.Set(uri, Wallpaper.Style.Tiled);
            }
            catch
            {

            }

            // Create and set the Logon Screen
            CreateLogonScreen();
        }

        private static void CreateWindows10Wallpaper(string infoString, StringFormat infoStringFormat)
        {
            Screen[] screens = Screen.AllScreens;

            FindCanvasDimensions(screens);

            if (screens.Length == 1)
            {
                using (Bitmap b = new Bitmap(totalWidth, maxHeight))
                {
                    using (Graphics g = Graphics.FromImage(b))
                    {
                        g.Clear(_color);

                        Color myColor = Color.FromArgb(255, 76, 0);
                        SolidBrush myBrush = new SolidBrush(myColor);

                        int screenWidth = totalWidth;
                        int screenHeight = maxHeight;

                        CalculateScales(screenWidth, screenHeight);

                        // Load the font to be used for the info string, with the correct size of text
                        Font futuraFont = new Font(ff, Convert.ToInt32(fontSizeScaled), FontStyle.Regular);
                        Font futuraFont2 = new Font(ff, Convert.ToInt32(fontSizeScaled + 32), FontStyle.Bold);

                        // Resize the images
                        Bitmap chcBirdLogo = new Bitmap(Properties.Resources.CHC_Logo);
                        Bitmap serviceDeskInfo = new Bitmap(Properties.Resources.ServiceDeskGreyCHCHeli);

                        Bitmap chcBirdLogoResized = Resize(chcBirdLogo, chcBirdLogoScale);
                        Bitmap serviceDeskInfoResized = Resize(serviceDeskInfo, serviceDeskInfoScale);

                        // Calculate the height of the info string based on the size of the font used
                        int stringHeight = InfoHeight(infoString, futuraFont);
                        int stringWidth = InfoWidth(watchMeMessage, futuraFont2);

                        // Add the resized images onto the main canvas
                        g.DrawImage(chcBirdLogoResized, (screenWidth - chcBirdLogoResized.Width - margin), margin);
                        g.DrawString(infoString, futuraFont, Brushes.White, new Point(screenWidth - margin, ((screenHeight / 2) - (stringHeight / 2) + 4)), infoStringFormat);
                        g.DrawImage(serviceDeskInfoResized, (screenWidth - serviceDeskInfoResized.Width - margin), (screenHeight - serviceDeskInfoResized.Height - (50 * ScreenScalingFactor)));
                        g.DrawString(watchMeMessage, futuraFont2, myBrush, (screenWidth / 2) - (stringWidth / 2), (screenHeight / 2));

                    }
                    try
                    {
                        b.Save(saveWallpaperLocation, ImageFormat.Bmp);
                    }
                    catch
                    {

                    }
                }
            }

            else
            {
                // Create the background and add resized images and text to it
                using (Bitmap b = new Bitmap(totalWidth, maxHeight))
                {
                    using (Graphics g = Graphics.FromImage(b))
                    {
                        g.Clear(_color);

                        Color myColor = Color.FromArgb(255, 76, 0);
                        SolidBrush myBrush = new SolidBrush(myColor);

                        foreach (Screen screen in screens)
                        {
                            int screenWidth = screen.Bounds.Width;
                            int screenHeight = screen.Bounds.Height;

                            CalculateScales(screenWidth, screenHeight);

                            // Load the font to be used for the info string, with the correct size of text
                            Font futuraFont = new Font(ff, Convert.ToInt32(fontSizeScaled), FontStyle.Regular);
                            Font futuraFont2 = new Font(ff, Convert.ToInt32(fontSizeScaled + 32), FontStyle.Bold);

                            // Resize the images
                            Bitmap chcBirdLogo = new Bitmap(Properties.Resources.CHC_Logo);
                            Bitmap serviceDeskInfo = new Bitmap(Properties.Resources.ServiceDeskGreyCHCHeli);

                            Bitmap chcBirdLogoResized = Resize(chcBirdLogo, chcBirdLogoScale);
                            Bitmap serviceDeskInfoResized = Resize(serviceDeskInfo, serviceDeskInfoScale);

                            // Calculate the height of the info string based on the size of the font used
                            int stringHeight = InfoHeight(infoString, futuraFont);
                            int stringWidth = InfoWidth(watchMeMessage, futuraFont2);

                            // Add the resized images onto the main canvas
                            g.DrawImage(chcBirdLogoResized, (screen.Bounds.Location.X + screenWidth - lowestXCoOrdinate - chcBirdLogoResized.Width - margin), (screen.Bounds.Location.Y - lowestYCoOrdinate + margin));
                            g.DrawString(infoString, futuraFont, Brushes.White, new Point(screen.Bounds.Location.X + screenWidth - lowestXCoOrdinate - margin, (screen.Bounds.Location.Y - lowestYCoOrdinate + (screenHeight / 2) - (stringHeight / 2) + 4)), infoStringFormat);
                            g.DrawImage(serviceDeskInfoResized, (screen.Bounds.Location.X + screenWidth - lowestXCoOrdinate - serviceDeskInfoResized.Width - margin), (screen.Bounds.Location.Y - lowestYCoOrdinate + screenHeight - serviceDeskInfoResized.Height - 50));
                            g.DrawString(watchMeMessage, futuraFont2, myBrush, screen.Bounds.Location.X + (screenWidth / 2) - lowestXCoOrdinate - (stringWidth / 2), screen.Bounds.Location.Y + (screenHeight / 2) - lowestYCoOrdinate );
                        }
                        
                        
                    }
                    // Save the newly created image
                    try
                    {
                        b.Save(saveWallpaperLocation, ImageFormat.Bmp);
                    }
                    catch
                    {

                    }
                }
            }
        }

        private static void CreateWindows7Wallpaper(string infoString, StringFormat infoStringFormat)
        {
            Screen[] screens = Screen.AllScreens;

            FindCanvasDimensions(screens);

            if (screens.Length == 1)
            {
                int screenWidth = Screen.PrimaryScreen.Bounds.Width;
                int screenHeight = Screen.PrimaryScreen.Bounds.Height;

                CalculateScales(screenWidth, screenHeight);

                using (Bitmap b = new Bitmap(screenWidth, screenHeight))
                {
                    using (Graphics g = Graphics.FromImage(b))
                    {
                        g.Clear(_color);

                        // Load the font to be used for the info string
                        Font futuraFont = new Font(ff, Convert.ToInt32(fontSizeScaled), FontStyle.Regular);

                        // Resize the images
                        Bitmap chcBirdLogo = new Bitmap(Properties.Resources.CHC_Logo);
                        Bitmap serviceDeskInfo = new Bitmap(Properties.Resources.ServiceDeskGreyCHCHeli);

                        Bitmap chcBirdLogoResized = Resize(chcBirdLogo, chcBirdLogoScale);
                        Bitmap serviceDeskInfoResized = Resize(serviceDeskInfo, serviceDeskInfoScale);

                        // Calculate the height of the info string based on the size of the font used
                        int stringHeight = InfoHeight(infoString, futuraFont);

                        // Add the resized images onto the main canvas
                        g.DrawImage(chcBirdLogoResized, (screenWidth - chcBirdLogoResized.Width - margin), margin);
                        g.DrawString(infoString, futuraFont, Brushes.White, new Point(screenWidth - margin, ((screenHeight / 2) - (stringHeight / 2) + 4)), infoStringFormat);
                        g.DrawImage(serviceDeskInfoResized, (screenWidth - serviceDeskInfoResized.Width - margin), (screenHeight - serviceDeskInfoResized.Height - 50));
                    }
                    try
                    {
                        b.Save(saveWallpaperLocation, ImageFormat.Bmp);
                    }
                    catch
                    {

                    }
                }
            }

            if (screens.Length == 2)
            {
                bool reverseMonitors = false;
                bool heightsAkward = false;

                if (lowestXCoOrdinate < 0)
                {
                    //MessageBox.Show("Has monitor left of the main monitor");
                    reverseMonitors = true;
                }

                if (lowestYCoOrdinate < 0)
                {
                    //MessageBox.Show("Akward heights");
                    heightsAkward = true;
                }

                using (Bitmap b = new Bitmap(totalWidth, maxHeight))
                {
                    using (Graphics g = Graphics.FromImage(b))
                    {
                        g.Clear(_color);

                        foreach (Screen screen in screens)
                        {
                            int screenWidth = screen.Bounds.Width;
                            int screenHeight = screen.Bounds.Height;

                            CalculateScales(screenWidth, screenHeight);

                            // Load the font to be used for the info string
                            Font futuraFont = new Font(ff, Convert.ToInt32(fontSizeScaled), FontStyle.Regular);

                            // Resize the images
                            Bitmap chcBirdLogo = new Bitmap(Properties.Resources.CHC_Logo);
                            Bitmap serviceDeskInfo = new Bitmap(Properties.Resources.ServiceDeskGreyCHCHeli);

                            Bitmap chcBirdLogoResized = Resize(chcBirdLogo, chcBirdLogoScale);
                            Bitmap serviceDeskInfoResized = Resize(serviceDeskInfo, serviceDeskInfoScale);

                            // Calculate the height of the info string based on the size of the font used
                            int stringHeight = InfoHeight(infoString, futuraFont);

                            if (!(reverseMonitors))
                            {
                                if (heightsAkward)
                                {
                                    if (screen.Bounds.Location.Y < 0)
                                    {
                                        // Add the resized images onto the other canvases
                                        g.DrawImage(chcBirdLogoResized, (screen.Bounds.Location.X + screenWidth - lowestXCoOrdinate - chcBirdLogoResized.Width - margin), (screen.Bounds.Location.Y + margin));
                                        g.DrawImage(chcBirdLogoResized, (screen.Bounds.Location.X + screenWidth - lowestXCoOrdinate - chcBirdLogoResized.Width - margin), (maxHeight + lowestYCoOrdinate + margin));
                                        g.DrawString(infoString, futuraFont, Brushes.White, new Point(screen.Bounds.Location.X + screenWidth - lowestXCoOrdinate - margin, (screen.Bounds.Location.Y + (screenHeight / 2) - (stringHeight / 2) + 10)), infoStringFormat);
                                        g.DrawImage(serviceDeskInfoResized, (screen.Bounds.Location.X + screenWidth - lowestXCoOrdinate - serviceDeskInfoResized.Width - margin), (screen.Bounds.Location.Y + screenHeight - serviceDeskInfoResized.Height - margin));
                                    }
                                    else
                                    {
                                        // Add the resized images onto the main canvas
                                        g.DrawImage(chcBirdLogoResized, (screenWidth - chcBirdLogoResized.Width - margin), (margin));
                                        g.DrawString(infoString, futuraFont, Brushes.White, new Point(screenWidth - margin, ((screenHeight / 2) - (stringHeight / 2) + 4)), infoStringFormat);
                                        g.DrawImage(serviceDeskInfoResized, (screenWidth - serviceDeskInfoResized.Width - margin), (screenHeight - serviceDeskInfoResized.Height - 50));
                                    }
                                }
                                else
                                {
                                    if (screen.Primary == true)
                                    {
                                        // Add the resized images onto the main canvas
                                        g.DrawImage(chcBirdLogoResized, (screen.Bounds.Location.X + screenWidth - lowestXCoOrdinate - chcBirdLogoResized.Width - margin), (screen.Bounds.Location.Y - lowestYCoOrdinate + margin));
                                        g.DrawString(infoString, futuraFont, Brushes.White, new Point(screen.Bounds.Location.X + screenWidth - lowestXCoOrdinate - margin, (screen.Bounds.Location.Y - lowestYCoOrdinate + (screenHeight / 2) - (stringHeight / 2) + 4)), infoStringFormat);
                                        g.DrawImage(serviceDeskInfoResized, (screen.Bounds.Location.X + screenWidth - lowestXCoOrdinate - serviceDeskInfoResized.Width - margin), (screen.Bounds.Location.Y - lowestYCoOrdinate + screenHeight - serviceDeskInfoResized.Height - 50));
                                    }
                                    else
                                    {
                                        // Add the resized images onto the other canvases
                                        g.DrawImage(chcBirdLogoResized, (screen.Bounds.Location.X + screenWidth - lowestXCoOrdinate - chcBirdLogoResized.Width - margin), (screen.Bounds.Location.Y - lowestYCoOrdinate + margin));
                                        g.DrawString(infoString, futuraFont, Brushes.White, new Point(screen.Bounds.Location.X + screenWidth - lowestXCoOrdinate - margin, (screen.Bounds.Location.Y - lowestYCoOrdinate + (screenHeight / 2) - (stringHeight / 2) + 10)), infoStringFormat);
                                        g.DrawImage(serviceDeskInfoResized, (screen.Bounds.Location.X + screenWidth - lowestXCoOrdinate - serviceDeskInfoResized.Width - margin), (screen.Bounds.Location.Y - lowestYCoOrdinate + screenHeight - serviceDeskInfoResized.Height - margin));
                                    }
                                }
                            }
                            else
                            {
                                if (heightsAkward)
                                {
                                    if (screen.Bounds.Location.X < 0)
                                    {
                                        // Add the resized images onto the left canvas
                                        g.DrawImage(chcBirdLogoResized, (totalWidth - chcBirdLogoResized.Width - margin), (screen.Bounds.Location.Y + margin));
                                        g.DrawImage(chcBirdLogoResized, (totalWidth - chcBirdLogoResized.Width - margin), (maxHeight + lowestYCoOrdinate + margin));
                                        g.DrawString(infoString, futuraFont, Brushes.White, new Point(totalWidth - margin, (screen.Bounds.Location.Y + (screenHeight / 2) - (stringHeight / 2) + 10)), infoStringFormat);
                                        g.DrawImage(serviceDeskInfoResized, (totalWidth - serviceDeskInfoResized.Width - margin), (screen.Bounds.Location.Y + screenHeight - serviceDeskInfoResized.Height - margin));
                                    }
                                    else
                                    {
                                        // Add the resized images onto the main canvas
                                        g.DrawImage(chcBirdLogoResized, (screenWidth - chcBirdLogoResized.Width - margin), (margin));
                                        g.DrawString(infoString, futuraFont, Brushes.White, new Point(screenWidth - margin, ((screenHeight / 2) - (stringHeight / 2) + 4)), infoStringFormat);
                                        g.DrawImage(serviceDeskInfoResized, (screenWidth - serviceDeskInfoResized.Width - margin), (screenHeight - serviceDeskInfoResized.Height - 50));
                                    }
                                }
                                else
                                {
                                    if (screen.Bounds.Location.X < 0)
                                    {
                                        // Add the resized images onto the left canvas
                                        g.DrawImage(chcBirdLogoResized, (totalWidth - chcBirdLogoResized.Width - margin), (screen.Bounds.Location.Y - lowestYCoOrdinate + margin));
                                        g.DrawString(infoString, futuraFont, Brushes.White, new Point(totalWidth - margin, (screen.Bounds.Location.Y - lowestYCoOrdinate + (screenHeight / 2) - (stringHeight / 2) + 10)), infoStringFormat);
                                        g.DrawImage(serviceDeskInfoResized, (totalWidth - serviceDeskInfoResized.Width - margin), (screen.Bounds.Location.Y - lowestYCoOrdinate + screenHeight - serviceDeskInfoResized.Height - margin));
                                    }
                                    else
                                    {
                                        // Add the resized images onto the main canvas
                                        g.DrawImage(chcBirdLogoResized, (screenWidth - chcBirdLogoResized.Width - margin), (margin));
                                        g.DrawString(infoString, futuraFont, Brushes.White, new Point(screenWidth - margin, ((screenHeight / 2) - (stringHeight / 2) + 4)), infoStringFormat);
                                        g.DrawImage(serviceDeskInfoResized, (screenWidth - serviceDeskInfoResized.Width - margin), (screenHeight - serviceDeskInfoResized.Height - 50));
                                    }
                                }
                            }
                        }
                    }
                    try
                    {
                        b.Save(saveWallpaperLocation, ImageFormat.Bmp);
                    }
                    catch
                    {

                    }
                }
            }

            if (screens.Length == 3)
            {
                bool reverseMonitors = false;
                bool heightsAkward = false;
                bool twoReversedMonitors = false;
                int numberOfReversedMonitors = 0;

                // Find the number of monitors to the left of the main monitor
                foreach (Screen screen in screens)
                {
                    if (screen.Bounds.Location.X < 0) { numberOfReversedMonitors++; }
                }

                if (lowestXCoOrdinate < 0)
                {
                    //MessageBox.Show("Has monitor left of the main monitor");
                    reverseMonitors = true;
                    if (numberOfReversedMonitors > 1)
                    {
                        twoReversedMonitors = true;
                        //MessageBox.Show("Has two monitors to the left of the main monitor"); 
                    }
                }

                if (lowestYCoOrdinate < 0)
                {
                    //MessageBox.Show("Akward heights");
                    heightsAkward = true;
                }

                // Create the background and add resized images and text to it
                using (Bitmap b = new Bitmap(totalWidth, maxHeight))
                {
                    using (Graphics g = Graphics.FromImage(b))
                    {
                        g.Clear(_color);

                        foreach (Screen screen in screens)
                        {
                            //MessageBox.Show(screen.Bounds.Location.X.ToString());
                            int screenWidth = screen.Bounds.Width;
                            int screenHeight = screen.Bounds.Height;

                            CalculateScales(screenWidth, screenHeight);

                            // Load the font to be used for the info string
                            Font futuraFont = new Font(ff, Convert.ToInt32(fontSizeScaled), FontStyle.Regular);

                            // Resize the images
                            Bitmap chcBirdLogo = new Bitmap(Properties.Resources.CHC_Logo);
                            Bitmap serviceDeskInfo = new Bitmap(Properties.Resources.ServiceDeskGreyCHCHeli);

                            Bitmap chcBirdLogoResized = Resize(chcBirdLogo, chcBirdLogoScale);
                            Bitmap serviceDeskInfoResized = Resize(serviceDeskInfo, serviceDeskInfoScale);

                            // Calculate the height of the info string based on the size of the font used
                            int stringHeight = InfoHeight(infoString, futuraFont);

                            if (!(reverseMonitors))
                            {
                                if (heightsAkward)
                                {
                                    if (screen.Bounds.Location.Y < 0)
                                    {
                                        // Add the resized images onto the main canvas
                                        g.DrawImage(chcBirdLogoResized, (screen.Bounds.Location.X + screenWidth - lowestXCoOrdinate - chcBirdLogoResized.Width - margin), (screen.Bounds.Location.Y + margin));
                                        g.DrawImage(chcBirdLogoResized, (screen.Bounds.Location.X + screenWidth - lowestXCoOrdinate - chcBirdLogoResized.Width - margin), (maxHeight + screen.Bounds.Location.Y + margin));
                                        g.DrawString(infoString, futuraFont, Brushes.White, new Point(screen.Bounds.Location.X + screenWidth - lowestXCoOrdinate - margin, (screen.Bounds.Location.Y + (screenHeight / 2) - (stringHeight / 2) + 10)), infoStringFormat);
                                        g.DrawImage(serviceDeskInfoResized, (screen.Bounds.Location.X + screenWidth - lowestXCoOrdinate - serviceDeskInfoResized.Width - margin), (screen.Bounds.Location.Y + screenHeight - serviceDeskInfoResized.Height - margin));
                                    }
                                    else
                                    {
                                        if (screen.Primary == true)
                                        {
                                            // Add the resized images onto the main canvas
                                            g.DrawImage(chcBirdLogoResized, (screen.Bounds.Location.X + screenWidth - chcBirdLogoResized.Width - margin), (screen.Bounds.Location.Y + margin));
                                            g.DrawString(infoString, futuraFont, Brushes.White, new Point(screen.Bounds.Location.X + screenWidth - margin, (screen.Bounds.Location.Y + (screenHeight / 2) - (stringHeight / 2) + 4)), infoStringFormat);
                                            g.DrawImage(serviceDeskInfoResized, (screen.Bounds.Location.X + screenWidth - serviceDeskInfoResized.Width - margin), (screen.Bounds.Location.Y + screenHeight - serviceDeskInfoResized.Height - 50));
                                        }

                                        else
                                        {
                                            // Add the resized images onto the other canvases
                                            g.DrawImage(chcBirdLogoResized, (screen.Bounds.Location.X + screenWidth - lowestXCoOrdinate - chcBirdLogoResized.Width - margin), (screen.Bounds.Location.Y + margin));
                                            g.DrawString(infoString, futuraFont, Brushes.White, new Point(screen.Bounds.Location.X + screenWidth - lowestXCoOrdinate - margin, (screen.Bounds.Location.Y + (screenHeight / 2) - (stringHeight / 2) + 10)), infoStringFormat);
                                            g.DrawImage(serviceDeskInfoResized, (screen.Bounds.Location.X + screenWidth - lowestXCoOrdinate - serviceDeskInfoResized.Width - margin), (screen.Bounds.Location.Y + screenHeight - serviceDeskInfoResized.Height - margin));
                                        }
                                    }
                                }
                                else
                                {
                                    if (screen.Primary == true)
                                    {
                                        // Add the resized images onto the main canvas
                                        g.DrawImage(chcBirdLogoResized, (screen.Bounds.Location.X + screenWidth - lowestXCoOrdinate - chcBirdLogoResized.Width - margin), (screen.Bounds.Location.Y + margin));
                                        g.DrawString(infoString, futuraFont, Brushes.White, new Point(screen.Bounds.Location.X + screenWidth - lowestXCoOrdinate - margin, (screen.Bounds.Location.Y + (screenHeight / 2) - (stringHeight / 2) + 4)), infoStringFormat);
                                        g.DrawImage(serviceDeskInfoResized, (screen.Bounds.Location.X + screenWidth - lowestXCoOrdinate - serviceDeskInfoResized.Width - margin), (screen.Bounds.Location.Y + screenHeight - serviceDeskInfoResized.Height - 50));
                                    }
                                    else
                                    {
                                        // Add the resized images to the other canvases
                                        g.DrawImage(chcBirdLogoResized, (screen.Bounds.Location.X + screenWidth - lowestXCoOrdinate - chcBirdLogoResized.Width - margin), (screen.Bounds.Location.Y - lowestYCoOrdinate + margin));
                                        g.DrawString(infoString, futuraFont, Brushes.White, new Point(screen.Bounds.Location.X + screenWidth - lowestXCoOrdinate - margin, (screen.Bounds.Location.Y - lowestYCoOrdinate + (screenHeight / 2) - (stringHeight / 2) + 10)), infoStringFormat);
                                        g.DrawImage(serviceDeskInfoResized, (screen.Bounds.Location.X + screenWidth - lowestXCoOrdinate - serviceDeskInfoResized.Width - margin), (screen.Bounds.Location.Y - lowestYCoOrdinate + screenHeight - serviceDeskInfoResized.Height - margin));
                                    }
                                }
                            }
                            else
                            {
                                if (twoReversedMonitors)
                                {
                                    if (screen.Bounds.Location.X == 0)
                                    {
                                        // Add the resized images onto the main canvas
                                        g.DrawImage(chcBirdLogoResized, (screen.Bounds.Location.X + screenWidth - chcBirdLogoResized.Width - margin), (screen.Bounds.Location.Y + margin));
                                        g.DrawString(infoString, futuraFont, Brushes.White, new Point(screen.Bounds.Location.X + screenWidth - margin, (screen.Bounds.Location.Y + (screenHeight / 2) - (stringHeight / 2) + 4)), infoStringFormat);
                                        g.DrawImage(serviceDeskInfoResized, (screen.Bounds.Location.X + screenWidth - serviceDeskInfoResized.Width - margin), (screen.Bounds.Location.Y + screenHeight - serviceDeskInfoResized.Height - 50));
                                    }
                                }
                                else
                                {
                                    if (heightsAkward)
                                    {
                                        if (screen.Bounds.Location.X < 0)
                                        {
                                            // Add the resized images onto the left canvas
                                            g.DrawImage(chcBirdLogoResized, (totalWidth - chcBirdLogoResized.Width - margin), (screen.Bounds.Location.Y + margin));
                                            g.DrawImage(chcBirdLogoResized, (totalWidth - chcBirdLogoResized.Width - margin), (maxHeight + screen.Bounds.Location.Y + margin));
                                            g.DrawString(infoString, futuraFont, Brushes.White, new Point(totalWidth - margin, (screen.Bounds.Location.Y + (screenHeight / 2) - (stringHeight / 2) + 10)), infoStringFormat);
                                            g.DrawImage(serviceDeskInfoResized, (totalWidth - serviceDeskInfoResized.Width - margin), (screen.Bounds.Location.Y + screenHeight - serviceDeskInfoResized.Height - margin));
                                        }
                                        else
                                        {
                                            if (screen.Bounds.Location.Y < 0)
                                            {
                                                // Add the resized images onto the main canvas
                                                g.DrawImage(chcBirdLogoResized, (screen.Bounds.Location.X + screenWidth - chcBirdLogoResized.Width - margin), (screen.Bounds.Location.Y + margin));
                                                g.DrawImage(chcBirdLogoResized, (screen.Bounds.Location.X + screenWidth - chcBirdLogoResized.Width - margin), (maxHeight + screen.Bounds.Location.Y + margin));
                                                g.DrawString(infoString, futuraFont, Brushes.White, new Point(screen.Bounds.Location.X + screenWidth - margin, (screen.Bounds.Location.Y + (screenHeight / 2) - (stringHeight / 2) + 10)), infoStringFormat);
                                                g.DrawImage(serviceDeskInfoResized, (screen.Bounds.Location.X + screenWidth - serviceDeskInfoResized.Width - margin), (screen.Bounds.Location.Y + screenHeight - serviceDeskInfoResized.Height - margin));
                                            }
                                            else
                                            {
                                                if (screen.Primary == true)
                                                {
                                                    // Add the resized images onto the main canvas
                                                    g.DrawImage(chcBirdLogoResized, (screen.Bounds.Location.X + screenWidth - chcBirdLogoResized.Width - margin), (screen.Bounds.Location.Y + margin));
                                                    g.DrawString(infoString, futuraFont, Brushes.White, new Point(screen.Bounds.Location.X + screenWidth - margin, (screen.Bounds.Location.Y + (screenHeight / 2) - (stringHeight / 2) + 4)), infoStringFormat);
                                                    g.DrawImage(serviceDeskInfoResized, (screen.Bounds.Location.X + screenWidth - serviceDeskInfoResized.Width - margin), (screen.Bounds.Location.Y + screenHeight - serviceDeskInfoResized.Height - 50));
                                                }
                                                else
                                                {
                                                    // Add the resized images onto the main canvas
                                                    g.DrawImage(chcBirdLogoResized, (screen.Bounds.Location.X + screenWidth - chcBirdLogoResized.Width - margin), (screen.Bounds.Location.Y + margin));
                                                    g.DrawString(infoString, futuraFont, Brushes.White, new Point(screen.Bounds.Location.X + screenWidth - margin, (screen.Bounds.Location.Y + (screenHeight / 2) - (stringHeight / 2) + 10)), infoStringFormat);
                                                    g.DrawImage(serviceDeskInfoResized, (screen.Bounds.Location.X + screenWidth - serviceDeskInfoResized.Width - margin), (screen.Bounds.Location.Y + screenHeight - serviceDeskInfoResized.Height - margin));
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (screen.Bounds.Location.X < 0)
                                        {
                                            // Add the resized images onto the left canvas
                                            g.DrawImage(chcBirdLogoResized, (totalWidth - chcBirdLogoResized.Width - margin), (screen.Bounds.Location.Y + margin));
                                            g.DrawString(infoString, futuraFont, Brushes.White, new Point(totalWidth - margin, (screen.Bounds.Location.Y + (screenHeight / 2) - (stringHeight / 2) + 10)), infoStringFormat);
                                            g.DrawImage(serviceDeskInfoResized, (totalWidth - serviceDeskInfoResized.Width - margin), (screen.Bounds.Location.Y + screenHeight - serviceDeskInfoResized.Height - margin));
                                        }
                                        else
                                        {
                                            if (screen.Primary == true)
                                            {
                                                // Add the resized images onto the main canvas
                                                g.DrawImage(chcBirdLogoResized, (screen.Bounds.Location.X + screenWidth - chcBirdLogoResized.Width - margin), (screen.Bounds.Location.Y + margin));
                                                g.DrawString(infoString, futuraFont, Brushes.White, new Point(screen.Bounds.Location.X + screenWidth - margin, (screen.Bounds.Location.Y + (screenHeight / 2) - (stringHeight / 2) + 4)), infoStringFormat);
                                                g.DrawImage(serviceDeskInfoResized, (screen.Bounds.Location.X + screenWidth - serviceDeskInfoResized.Width - margin), (screen.Bounds.Location.Y + screenHeight - serviceDeskInfoResized.Height - 50));
                                            }
                                            else
                                            {
                                                // Add the resized images onto the main canvas
                                                g.DrawImage(chcBirdLogoResized, (screen.Bounds.Location.X + screenWidth - chcBirdLogoResized.Width - margin), (screen.Bounds.Location.Y + margin));
                                                g.DrawString(infoString, futuraFont, Brushes.White, new Point(screen.Bounds.Location.X + screenWidth - margin, (screen.Bounds.Location.Y + (screenHeight / 2) - (stringHeight / 2) + 10)), infoStringFormat);
                                                g.DrawImage(serviceDeskInfoResized, (screen.Bounds.Location.X + screenWidth - serviceDeskInfoResized.Width - margin), (screen.Bounds.Location.Y + screenHeight - serviceDeskInfoResized.Height - margin));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    try
                    {
                        b.Save(saveWallpaperLocation, ImageFormat.Bmp);
                    }
                    catch
                    {

                    }
                }
            }

            if (screens.Length > 3)
            {
                //MessageBox.Show("More than three monitors connected");

                int screenWidth = Screen.PrimaryScreen.Bounds.Width;
                int screenHeight = Screen.PrimaryScreen.Bounds.Height;

                CalculateScales(screenWidth, screenHeight);

                // Create the background and add resized images and text to it
                using (Bitmap b = new Bitmap(totalWidth, maxHeight))
                {
                    using (Graphics g = Graphics.FromImage(b))
                    {
                        g.Clear(_color);

                        // Load the font to be used for the info string
                        Font futuraFont = new Font(ff, Convert.ToInt32(fontSizeScaled), FontStyle.Regular);

                        // Resize the images
                        Bitmap chcBirdLogo = new Bitmap(Properties.Resources.CHC_Logo);
                        Bitmap serviceDeskInfo = new Bitmap(Properties.Resources.ServiceDeskGreyCHCHeli);

                        Bitmap chcBirdLogoResized = Resize(chcBirdLogo, chcBirdLogoScale);
                        Bitmap serviceDeskInfoResized = Resize(serviceDeskInfo, serviceDeskInfoScale);

                        // Calculate the height of the info string based on the size of the font used
                        int stringHeight = InfoHeight(infoString, futuraFont);

                        // Add the resized images onto the main canvas
                        g.DrawImage(chcBirdLogoResized, (screenWidth - chcBirdLogoResized.Width - margin), margin);
                        g.DrawString(infoString, futuraFont, Brushes.White, new Point(screenWidth - margin, ((screenHeight / 2) - (stringHeight / 2) + 4)), infoStringFormat);
                        g.DrawImage(serviceDeskInfoResized, (screenWidth - serviceDeskInfoResized.Width - margin), (screenHeight - serviceDeskInfoResized.Height - 50));
                    }
                    // Save the newly created image
                    try
                    {
                        b.Save(saveWallpaperLocation, ImageFormat.Bmp);
                    }
                    catch
                    {

                    }
                }
            }
        }

        // Determine if Windows version is higher than Window 7 or not
        static bool DetermineOSHigherThanWindow7()
        {
            int majorVersion = Environment.OSVersion.Version.Major;
            int minorVersion = Environment.OSVersion.Version.Minor;

            if (majorVersion < 6)
            {
                return false;
            }

            if (majorVersion == 6)
            {
                if (minorVersion < 2)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            if (majorVersion > 6)
            {
                return true;
            }

            return false;
        }

        // Create the information test for the middle right side of the screen
        static string CreateInfoString()
        {
            string userNameClc = Environment.UserName;
            string computerNameClc = Environment.MachineName;
            string hostIPAddress = string.Empty;
            string gatewayAddress = string.Empty;
            string dnsAddresses = string.Empty;

            try
            {
                NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

                foreach (NetworkInterface networkInterface in networkInterfaces)
                {
                    if (networkInterface.OperationalStatus == OperationalStatus.Up)
                    {
                        if ((networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet) || (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
                        {
                            GatewayIPAddressInformationCollection gatewayAddresses = networkInterface.GetIPProperties().GatewayAddresses;

                            // Get the IP, DG and DNS addresses (IPv4) of any adapter that is up and has a DG set.
                            if (gatewayAddresses.Count > 0)
                            {
                                UnicastIPAddressInformationCollection localIPAddress = networkInterface.GetIPProperties().UnicastAddresses;
                                foreach (UnicastIPAddressInformation IPAddress in localIPAddress)
                                {
                                    if (IPAddress.Address.AddressFamily == AddressFamily.InterNetwork)
                                    {
                                        hostIPAddress += IPAddress.Address.ToString() + "\r\n";
                                    }
                                }

                                foreach (GatewayIPAddressInformation gatewayIPAddress in gatewayAddresses)
                                {
                                    gatewayAddress += gatewayIPAddress.Address.ToString() + "\r\n";
                                }

                                IPAddressCollection DNSAddresses = networkInterface.GetIPProperties().DnsAddresses;

                                foreach (IPAddress DNSServer in DNSAddresses)
                                {
                                    if (DNSServer.AddressFamily == AddressFamily.InterNetwork)
                                    {
                                        if (!(dnsAddresses.Contains(DNSServer.ToString())))
                                        {
                                            dnsAddresses += DNSServer.ToString() + "\r\n";
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                // Remove any tailing carriage returns
                hostIPAddress = hostIPAddress.TrimEnd('\r', '\n');
                gatewayAddress = gatewayAddress.TrimEnd('\r', '\n');
                dnsAddresses = dnsAddresses.TrimEnd('\r', '\n');
            }
            catch (Exception)
            {
                // Just continue
            }

            string domainClc = Environment.UserDomainName;
            string logonClc = Environment.GetEnvironmentVariable("logonserver").Replace("\\", "");

            // Join all the info together into one string

            StringBuilder infoStringBuild = new StringBuilder("User Name");
            infoStringBuild.AppendLine();
            infoStringBuild.Append(userNameClc);
            infoStringBuild.AppendLine();
            infoStringBuild.AppendLine();
            infoStringBuild.Append("Computer Name");
            infoStringBuild.AppendLine();
            infoStringBuild.Append(computerNameClc);
            infoStringBuild.AppendLine();
            infoStringBuild.AppendLine();
            infoStringBuild.Append("IP Address");
            infoStringBuild.AppendLine();
            infoStringBuild.Append(hostIPAddress);
            infoStringBuild.AppendLine();
            infoStringBuild.AppendLine();
            infoStringBuild.Append("Default Gateway");
            infoStringBuild.AppendLine();
            infoStringBuild.Append(gatewayAddress);
            infoStringBuild.AppendLine();
            infoStringBuild.AppendLine();
            infoStringBuild.Append("DNS Servers");
            infoStringBuild.AppendLine();
            infoStringBuild.Append(dnsAddresses);
            infoStringBuild.AppendLine();
            infoStringBuild.AppendLine();
            infoStringBuild.Append("Domain");
            infoStringBuild.AppendLine();
            infoStringBuild.Append(domainClc);
            infoStringBuild.AppendLine();
            infoStringBuild.AppendLine();
            infoStringBuild.Append("Logon Server");
            infoStringBuild.AppendLine();
            infoStringBuild.Append(logonClc);
            infoStringBuild.AppendLine();
            infoStringBuild.AppendLine();

            String infoString = infoStringBuild.ToString();

            return infoString;
        }

        // Load the Lucida Grande font for displaying text
        private static void LoadFont()
        {
            // Create the byte array and get its length
            byte[] fontArray = Properties.Resources.LucidaGrande;
            int dataLength = Properties.Resources.LucidaGrande.Length;

            // Assign memory and copy BYTE[] on that memory address
            IntPtr ptrData = Marshal.AllocCoTaskMem(dataLength);
            Marshal.Copy(fontArray, 0, ptrData, dataLength);

            uint cFonts = 0;

            AddFontMemResourceEx(ptrData, (uint)fontArray.Length, IntPtr.Zero, ref cFonts);
            PrivateFontCollection pfc = new PrivateFontCollection();

            pfc.AddMemoryFont(ptrData, dataLength);

            Marshal.FreeCoTaskMem(ptrData);

            ff = pfc.Families[0];
            // Use something like this to use the font
            // font = new Font(ff, 15F, FontStyle.Bold);
        }

        // Find the total area of the canvas and the lowest and highest co-ordinates
        private static void FindCanvasDimensions(Screen[] screens)
        {
            
            //MessageBox.Show(screens.Length.ToString());
            if (screens.Length == 1)
            {
                Graphics gra = Graphics.FromHwnd(IntPtr.Zero);
                IntPtr desktop = gra.GetHdc();

                totalWidth = GetDeviceCaps(desktop, (int)DeviceCap.DESKTOPHORZRES); // Gets the physical desktop size of the primary monitor
                //int LogicalScreenWidth = GetDeviceCaps(desktop, (int)DeviceCap.HORZRES);
                //int logpixelsx = GetDeviceCaps(desktop, (int)DeviceCap.LOGPIXELSX);

                maxHeight = GetDeviceCaps(desktop, (int)DeviceCap.DESKTOPVERTRES); // Gets the physical desktop size of the primary monitor
                int LogicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.VERTRES);
                //int logpixelsy = GetDeviceCaps(desktop, (int)DeviceCap.LOGPIXELSY);

                //MessageBox.Show(totalWidth + "    " + LogicalScreenWidth + "    " + logpixelsx);
                //MessageBox.Show(maxHeight + "    " + LogicalScreenHeight + "    " + logpixelsy);

                ScreenScalingFactor = (float)maxHeight / (float)LogicalScreenHeight;
            }

            else
            {
            
                foreach (Screen screen in screens)
                {
                    if (screen.Bounds.Location.X < lowestXCoOrdinate) lowestXCoOrdinate = screen.Bounds.Location.X; // left most point to start from
                    if (screen.Bounds.Location.Y < lowestYCoOrdinate) lowestYCoOrdinate = screen.Bounds.Location.Y; // highest point to start from

                    if ((screen.Bounds.Location.X + screen.Bounds.Width) > maxXCoOrdinate) maxXCoOrdinate = (screen.Bounds.Location.X + screen.Bounds.Width); // right most point
                    if ((screen.Bounds.Location.Y + screen.Bounds.Height) > maxYCoOrdinate) maxYCoOrdinate = (screen.Bounds.Location.Y + screen.Bounds.Height); // lowest point
                }

                totalWidth = maxXCoOrdinate - lowestXCoOrdinate;
                maxHeight = maxYCoOrdinate - lowestYCoOrdinate;
            }
            //MessageBox.Show(totalWidth.ToString());
        }

        // Calculate the scale of images and text to be used depending on the screen size
        private static void CalculateScales(int screenWidth, int screenHeight )
        {
            if (screenWidth >= screenHeight)
            {
                double screenHeightDouble = Convert.ToDouble(screenHeight);
                fontSizeScaled = Math.Round((screenHeightDouble / 100), 0, MidpointRounding.AwayFromZero);

                chcBirdLogoScale = ((screenHeight * 0.105) / 579);

                serviceDeskInfoScale = ((screenHeight * 0.075) / 234);

                if (screenHeight < 768)
                {
                    margin = 21;
                }
            }
            else
            {
                double screenWidthDouble = Convert.ToDouble(screenWidth);
                fontSizeScaled = Math.Round((screenWidthDouble / 100), 0, MidpointRounding.AwayFromZero);

                chcBirdLogoScale = ((screenWidth * 0.105) / 579);

                serviceDeskInfoScale = ((screenWidth * 0.075) / 234);

                if (screenWidth < 768)
                {
                    margin = 21;
                }
            }
        }

        // Use to resize images to scale on different screen resolutions
        static Bitmap Resize(Image imageFile, double scaleFactor)
        {
            using (var srcImage = (imageFile))
            {
                var newWidth = (int)(srcImage.Width * scaleFactor);
                var newHeight = (int)(srcImage.Height * scaleFactor);
                var newImage = new Bitmap(newWidth, newHeight);
                using (var graphics = Graphics.FromImage(newImage))
                {
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.DrawImage(srcImage, new Rectangle(0, 0, newWidth, newHeight));
                    return newImage;
                }
            }
        }

        // Get the height of the text depending on the font and size used
        static int InfoHeight(string infoStringUsed, Font fontUsed)
        {
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                SizeF size = g.MeasureString(infoStringUsed, fontUsed);
                int stringHeight = (int)Math.Ceiling(size.Height);
                return stringHeight;
            }
        }

        // Get the height of the text depending on the font and size used
        static int InfoWidth(string infoStringUsed, Font fontUsed)
        {
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                SizeF size = g.MeasureString(infoStringUsed, fontUsed);
                int stringWidth = (int)Math.Ceiling(size.Width);
                return stringWidth;
            }
        }

        // Create and save the Logon Screen
        static void CreateLogonScreen()
        {
            try
            {
                FileInfo file = new FileInfo(saveLognScreenLocation);
                file.Directory.Create();
            }
            catch
            {

            }
            Screen primaryScreen = Screen.PrimaryScreen;

            int primaryScreenWidth = primaryScreen.Bounds.Width;
            int primaryScreenHeight = primaryScreen.Bounds.Height;

            using (Bitmap logonScreen = new Bitmap(primaryScreenWidth, primaryScreenHeight))
            {
                using (Graphics graphicLogon = Graphics.FromImage(logonScreen))
                {
                    graphicLogon.Clear(_color);

                    Bitmap chcBirdLogo = new Bitmap(Properties.Resources.CHC_Logo);

                    double chcBirdLogonScale = ((primaryScreenHeight * 0.145) / 579);

                    Bitmap chcBirdLogoResized = Resize(chcBirdLogo, chcBirdLogonScale);

                    double loginScreenMargin = Math.Round((primaryScreenHeight * 0.055), 0, MidpointRounding.AwayFromZero);

                    graphicLogon.DrawImage(chcBirdLogoResized, (primaryScreenWidth - chcBirdLogoResized.Width - Convert.ToInt32(loginScreenMargin)), (Convert.ToInt32(loginScreenMargin)));
                }
                try
                {
                    logonScreen.Save(saveLognScreenLocation, ImageFormat.Jpeg);
                }
                catch
                {

                }
            }
        }
    }
}
