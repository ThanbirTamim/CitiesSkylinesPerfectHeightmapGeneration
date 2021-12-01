using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace PerfectHeightMapDownload
{
    public class HeightMapCreator
    {
        private double topleftX = 0.0, topleftY = 0.0, bottomrightX = 0.0, bottomrightY = 0.0;
        private readonly string outputFolder = "";
        private string keepTempFolder { get; set; } = @"C:\keepTempFolder";
        private int image_dimension = 0;
        private int zoom = 14;
        public HeightMapCreator(int zoom, double minx, double miny, double maxx, double maxy, string oFolder, int img_dimension)
        {
            topleftX = minx;
            topleftY = maxy;
            bottomrightX = maxx;
            bottomrightY = miny;
            outputFolder = oFolder;
            this.image_dimension = img_dimension;
            this.zoom = zoom;
            keepTempFolder = oFolder + @"\KeepTempFolder";
            if (!Directory.Exists(keepTempFolder))
            {
                Directory.CreateDirectory(keepTempFolder);
                File.WriteAllText(keepTempFolder + @"\DownloadFailed.txt", "");
            }
        }
        private double MaxHeight { get; set; } = 13.6;
        private double MinHeight { get; set; } = 1.6;
        public bool getHeightMap()
        {
            try
            {
                int zoom = this.zoom;

                // get a tile that covers the top left and bottom right (for the tile count calculation)
                int x = long2tile(topleftX, zoom);
                int y = lat2tile(topleftY, zoom);
                int x2 = long2tile(bottomrightX, zoom);
                int y2 = lat2tile(bottomrightY, zoom);

                // get the required tile count in Zoom 17
                int tileCnt = Math.Max(x2 - x + 1, y2 - y + 1);
                int iCnt = tileCnt;

                //create 2D array with null value to store height images' location into tiles[,]
                string[,] tiles = Create2DArray(tileCnt, null);

                int totalTile = (tileCnt * tileCnt) - 1;

                #region Internet Connection Checking

                //here we are going to checking the internet connectoion
                try
                {
                    Ping myPing = new Ping();
                    String host = "google.com";
                    byte[] buffer = new byte[32];
                    int timeout = 1000;
                    PingOptions pingOptions = new PingOptions();
                    PingReply reply = myPing.Send(host, timeout, buffer, pingOptions);
                    if ((reply.Status == IPStatus.Success))
                        Console.WriteLine("Internet Connection OK!");
                    else
                        Console.WriteLine("Internet Connection Problem!");
                }
                catch (Exception e)
                {
                    StringBuilder readText = new StringBuilder(File.ReadAllText(keepTempFolder + @"\DownloadFailed.txt"));
                    readText.Append(Environment.NewLine + "Failed to connect internet" + Environment.NewLine);
                    File.WriteAllText(keepTempFolder + @"\DownloadFailed.txt", readText.ToString());

                    throw new Exception();
                }


                #endregion 



                for (int i = 0; i < tileCnt; i++)
                {
                    for (int j = 0; j < tileCnt; j++)
                    {
                        //here we are downloading height data png from mapbox api
                        string FileNameWithoutExtension = "" + zoom + "_" + (x + i) + "_" + (y + j);
                        if (!File.Exists(keepTempFolder + @"\" + FileNameWithoutExtension + ".png"))
                        {
                            string file = downloadPNG(zoom, x + i, y + j);
                            tiles[i, j] = file;
                        }
                        else
                        {
                            tiles[i, j] = keepTempFolder + @"\" + FileNameWithoutExtension + ".png";
                        }
                    }
                }

                double tileLng = tile2long(x, zoom);
                double tileLat = tile2lat(y, zoom);

                double tileLng2 = tile2long(x + tileCnt, zoom);
                double tileLat2 = tile2lat(y + tileCnt, zoom);


                //here we are merging all tile height data
                string combinedFile = MergeImage(tiles);
                if (combinedFile == null)
                    return false;
                double[,] heightData = modifyHeightmapFind(combinedFile);//colored data to height info
                MaxHeight = Enumerable.Range(0, heightData.GetLength(1)).Max(i => heightData[1, i]);
                MinHeight = Enumerable.Range(0, heightData.GetLength(1)).Min(i => heightData[1, i]);
                double differrenceRange = MaxHeight - MinHeight;
                double shaddingIncrement = (double)(differrenceRange / 255.0);
                Image Image = imageCreate(heightData, MaxHeight, MinHeight, shaddingIncrement);
                string initial_save_loc = keepTempFolder + @"\HeightMap.png";
                Image.Save(initial_save_loc, ImageFormat.Png);


                #region PNG to TIF
                //gdal_translate.exe -of GTiff -a_srs EPSG:4326 -a_ullr 90.340576171875 23.6998647091815 90.4518127441406 23.8016801980914 D:\HeightMap.png D:\HeightMapOUTPUT.tif
                string initial_save_tif_loc = keepTempFolder + @"\HeightMap.tif";
                string mapbox_grid_extent = $"{tileLng} {tileLat} {tileLng2} {tileLat2}";
                var command = @"gdal_translate.exe -of GTiff -a_srs EPSG:4326 -a_ullr " +
                    mapbox_grid_extent +
                    " " + initial_save_loc +
                    " " + initial_save_tif_loc; //here we are converting vector to tif file
                ExecuteCommandSync(command);
                #endregion

                #region TIF clip by required extent
                //(clip raster) gdal_translate -projwin 90.340748 23.80105 90.440748 23.70105 -of GTiff "D:/Bangladesh.tif" C:/OUTPUT.tif
                string req_extent = $"{topleftX} {topleftY} {bottomrightX} {bottomrightY}";
                string cropped_tif_loc = keepTempFolder + @"\HeightMapCrop.tif";
                command = @"gdal_translate.exe -projwin " + req_extent + " -of GTiff " + initial_save_tif_loc + " " + cropped_tif_loc;
                ExecuteCommandSync(command);
                #endregion

                #region Tif to PNG
                //gdal_translate.exe -of PNG -scale -co worldfile=yes testVectorToImage\LandImageTif.tif testVectorToImage\LandImageTif.png
                //gdal_translate.exe -of PNG -ot UInt32 -scale 90.340748 23.80105 90.440748 23.70105 D:\HeightMapCrop.tif D:\HeightMapCliped.png
                //gdal_translate.exe - of PNG D:\HeightMapCrop.tif D:\HeightMapCliped.png
                string clipped_png = keepTempFolder + @"\HeightMapCliped.png";
                command = @"gdal_translate.exe -of PNG " + cropped_tif_loc + " " + clipped_png; ////here we are converting tif to png file
                ExecuteCommandSync(command);
                #endregion

                Bitmap cliped_png = new Bitmap(clipped_png);
                Bitmap final_image = new Bitmap(cliped_png, new Size(image_dimension, image_dimension));

                final_image.Save(outputFolder + @"\HeightMap.png", ImageFormat.Png);
                final_image.Dispose();
                File.WriteAllText(outputFolder + @"\HeightInfo.txt", $"{MaxHeight},{MinHeight}");
                Console.WriteLine("Download Complete!!");
                return true;

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return false;
            }
        }
        private void ExecuteCommandSync(object command)
        {
            try
            {
                System.Diagnostics.ProcessStartInfo procStartInfo = new System.Diagnostics.ProcessStartInfo("cmd", "/c " + command);
                // The following commands are needed to redirect the standard output.
                // This means that it will be redirected to the Process.StandardOutput StreamReader.
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                // Do not create the black window.
                procStartInfo.CreateNoWindow = true;
                // Now we create a process, assign its ProcessStartInfo and start it
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo = procStartInfo;
                proc.Start();
                // Get the output into a string
                string result = proc.StandardOutput.ReadToEnd();
                // Display the command output.
                Console.WriteLine(result);
            }
            catch (Exception objException)
            {
                // Log the exception
                Console.WriteLine(objException.Message);
            }
        }
        private double[,] modifyHeightmapFind(string file)
        {
            Bitmap image = new Bitmap(file);
            int dimension = image.Height;
            double[,] heightData = new double[dimension, dimension];
            for (int y = 0; y < dimension; y++)
            {
                for (int x = 0; x < dimension; x++)
                {
                    Color color = image.GetPixel(x, y);
                    double R = color.R;
                    double G = color.G;
                    double B = color.B;
                    double height = (-100000 + (R * 256 * 256 + G * 256 + B)) * 0.1;
                    heightData[x, y] = height;
                }
            }

            return heightData;
        }
        private Image imageCreate(double[,] heightmap, double MaxHeight, double MinHeight, double shaddingIncrement)
        {
            int dimension = (int)(Math.Sqrt(heightmap.Length));
            Bitmap image = new Bitmap(dimension, dimension);
            for (int y = 0; y < dimension; y++)
            {
                for (int x = 0; x < dimension; x++)
                {
                    //double bb = heightmap[x, y];
                    //double valueSum = 0;
                    //int avgCount = 0;
                    //if (x - 1 >= 0 && x - 1 < dimension && y - 1 >= 0 && y - 1 < dimension)
                    //{
                    //    valueSum += heightmap[x - 1, y - 1];
                    //    heightmap[x - 1, y - 1] = (heightmap[x - 1, y - 1] + bb) / 2;
                    //    avgCount++;
                    //}
                    //if (x >= 0 && x < dimension && y - 1 >= 0 && y - 1 < dimension)
                    //{
                    //    valueSum += heightmap[x, y - 1];
                    //    heightmap[x, y - 1] = (heightmap[x, y - 1] + bb) / 2;
                    //    avgCount++;
                    //}
                    //if (x + 1 >= 0 && x + 1 < dimension && y - 1 >= 0 && y - 1 < dimension)
                    //{
                    //    valueSum += heightmap[x + 1, y - 1];
                    //    heightmap[x + 1, y - 1] = (heightmap[x + 1, y - 1] + bb) / 2;
                    //    avgCount++;
                    //}
                    //if (x + 1 >= 0 && x + 1 < dimension && y >= 0 && y < dimension)
                    //{
                    //    valueSum += heightmap[x + 1, y];
                    //    heightmap[x + 1, y] = (heightmap[x + 1, y] + bb) / 2;
                    //    avgCount++;
                    //}
                    //if (x + 1 >= 0 && x + 1 < dimension && y + 1 >= 0 && y + 1 < dimension)
                    //{
                    //    valueSum += heightmap[x + 1, y + 1];
                    //    heightmap[x + 1, y + 1] = (heightmap[x + 1, y + 1] + bb) / 2;
                    //    avgCount++;
                    //}
                    //if (x >= 0 && x < dimension && y + 1 >= 0 && y + 1 < dimension)
                    //{
                    //    valueSum += heightmap[x, y + 1];//change korte hobe
                    //    heightmap[x, y + 1] = (heightmap[x, y + 1] + bb) / 2;
                    //    avgCount++;
                    //}
                    //if (x - 1 >= 0 && x - 1 < dimension && y + 1 >= 0 && y + 1 < dimension)
                    //{
                    //    valueSum += heightmap[x - 1, y + 1];
                    //    heightmap[x - 1, y + 1] = (heightmap[x - 1, y + 1] + bb) / 2;
                    //    avgCount++;
                    //}
                    //if (x - 1 >= 0 && x - 1 < dimension && y >= 0 && y < dimension)
                    //{
                    //    valueSum += heightmap[x - 1, y];
                    //    heightmap[x - 1, y] = (heightmap[x - 1, y] + bb) / 2;
                    //    avgCount++;
                    //}

                    //valueSum = valueSum / avgCount;

                    double value = heightmap[x, y];
                    int avgRGB = (int)Math.Round(255.0 - ((MaxHeight - value) / (double)shaddingIncrement));
                    //int avgRGB = (int)Math.Round(255.0 - ((MaxHeight - valueSum) / (double)shaddingIncrement));
                    //int avgRGB = (int)Math.Round((value + 8000) * shaddingIncrement);

                    if (avgRGB <= 0)
                        avgRGB = 0;
                    if (avgRGB >= 255)
                        avgRGB = 255;

                    Color color = Color.FromArgb(255, avgRGB, avgRGB, avgRGB);
                    image.SetPixel(x, y, color);
                }
            }
            return image;
        }
        private string url = "";
        private string downloadPNG(int zoom, int x, int y)
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    string urlP1 = "https://api.mapbox.com/v4/mapbox.terrain-rgb/";
                    string urlP2 = zoom + "/" + x + "/" + y;
                    string urlP3 = "@2x.pngraw?access_token=pk.eyJ1IjoiYXdnYyIsImEiOiJja3Rta294NWwwYnVjMnZvNzh6Y3BqMG01In0.vcLCP0sti2UtcKdf96GNsA";

                    url = urlP1 + urlP2 + urlP3;

                    if (!Directory.Exists(keepTempFolder))
                    {
                        Directory.CreateDirectory(keepTempFolder);
                    }

                    string FileNameWithoutExtension = "" + zoom + "_" + x + "_" + y;
                    string file = keepTempFolder + @"\" + FileNameWithoutExtension + ".png";
                    //byte[] a = wc.DownloadData(url);
                    //string ax = wc.DownloadString(url);
                    //uri = new Uri(url);
                    wc.DownloadFile(url, file);

                    if (!File.Exists(file))
                        throw new Exception();
                    //after download the png height map. We have to convert it colored to gray
                    return file;
                }
            }
            catch (Exception ex)
            {
                StringBuilder readText = new StringBuilder(File.ReadAllText(keepTempFolder + @"\DownloadFailed.txt"));

                readText.Append(Environment.NewLine + url + ";" + Environment.NewLine);

                File.WriteAllText(keepTempFolder + @"\DownloadFailed.txt", readText.ToString());
                Console.WriteLine("Error: " + ex.Message);
                return null;
            }

        }
        private int long2tile(double lon, int z)
        {
            int a = Convert.ToInt32(Math.Floor((lon + 180.0) / 360.0 * (1 << z)));
            return a;
        }
        private int lat2tile(double lat, int z)
        {
            int a = Convert.ToInt32(Math.Floor((1 - Math.Log(Math.Tan(ToRadians(lat)) + 1 / Math.Cos(ToRadians(lat))) / Math.PI) / 2 * (1 << z)));
            return a;
        }
        private double ToRadians(double angle)
        {
            return (angle * Math.PI) / 180;
        }
        private double tile2long(int x, int z)
        {
            return x / (double)(1 << z) * 360.0 - 180;
        }
        private double tile2lat(int y, int z)
        {
            double n = Math.PI - 2.0 * Math.PI * y / (double)(1 << z);
            return 180.0 / Math.PI * Math.Atan(0.5 * (Math.Exp(n) - Math.Exp(-n)));
        }
        private string[,] Create2DArray(int rows, string value)
        {
            string[,] arr = new string[rows, rows];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    arr[i, j] = value;
                }
            }
            return arr;
        }
        private string MergeImage(string[,] tiles)
        {
            DirectoryInfo directory = new DirectoryInfo(keepTempFolder);
            if (directory != null)
            {
                FileInfo[] files = directory.GetFiles("*.png");
                string outputFileHeightData = CombineImages(files, tiles);
                if (outputFileHeightData != null)
                {
                    return outputFileHeightData;
                }
                else
                {
                    return null;
                }

            }
            else
            {
                return null;
            }

        }
        private string CombineImages(FileInfo[] files, string[,] tiles)
        {
            try
            {
                //change the location to store the final image.
                string finalImageFile = keepTempFolder + @"\CombinedImage.png";
                int height_width = (int)Math.Sqrt(files.Length);
                Bitmap finalImage = new Bitmap(height_width * 512, height_width * 512);
                int crtX = 0;
                int crtY = 0;
                int trackCrtY = 0;
                for (int tiley = 0; tiley < height_width; tiley++)
                {
                    int trackCrtX = 0;
                    crtX = trackCrtX;
                    for (int tilex = 0; tilex < height_width; tilex++)
                    {
                        string file = tiles[tilex, tiley];
                        file = Path.GetDirectoryName(file) + @"\" + Path.GetFileNameWithoutExtension(file) + ".png";
                        Bitmap inputImage = new Bitmap(file);



                        for (int y = 0; y < 512; y++)
                        {
                            for (int x = 0; x < 512; x++)
                            {
                                Color color = inputImage.GetPixel(x, y);
                                finalImage.SetPixel(crtX, crtY, color);

                                //Console.WriteLine(crtX + "&" + crtY + "____" + x + "&" + y + "____" + color.ToArgb());
                                crtX++;
                            }
                            crtX = trackCrtX;
                            crtY++;//= y;
                                   //Console.WriteLine(crtY + "__" + y);
                        }
                        inputImage.Dispose();
                        trackCrtX = ((tilex + 1) * 512);
                        crtX = trackCrtX;
                        crtY = trackCrtY;
                        //crtX = crtX + ((tilex + 1) * 512);
                    }
                    //crtY = crtY + ((tiley + 1) * 512);
                    trackCrtY = (tiley + 1) * 512;
                    crtY = trackCrtY;
                }

                finalImage.Save(finalImageFile);
                //Console.WriteLine("OK Merged!!!");
                return finalImageFile;
            }
            catch (Exception ex)
            {
                return null;
            }

        }
    }
}
