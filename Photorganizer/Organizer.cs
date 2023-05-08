using Emgu.CV.Structure;
using Emgu.CV;
using System.Drawing.Imaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

namespace Photorganizer
{
    public class Organizer
    {
        public void CleanupEmptyDirectories(string sourceFolder)
        {
            foreach (var directoryName in Directory.GetDirectories(sourceFolder, "*", SearchOption.AllDirectories))
            {
                if (Directory.GetFiles(directoryName).Length == 0 && Directory.GetDirectories(directoryName).Length == 0)
                {
                    Directory.Delete(directoryName, false);
                }
            }
        }

        public int ProcessSimilarity(string sourceFolder)
        {
            int processedFiles = 0;
            var filesToProcess = Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories);

            var moveToDirectory = string.Format("{0}{1}{2}", sourceFolder, Path.DirectorySeparatorChar, "Similar");
            if (Directory.Exists(moveToDirectory) == false)
            {
                Directory.CreateDirectory(moveToDirectory);
            }

            var filesToMove = new ConcurrentDictionary<string, string>();

            Parallel.For(0, filesToProcess.Length - 1, i => {
                Console.WriteLine("Processing " + (++processedFiles).ToString() + " of " + filesToProcess.Length.ToString());

                var file = filesToProcess[i];
                if (File.Exists(file) == false) return;
                var similarMatchFound = false;
                if (Path.GetExtension(file).ToLower() != ".jpg") return;

                Parallel.For(i + 1, filesToProcess.Length, j =>
                {
                    var fileToCompare = filesToProcess[j];
                    if (File.Exists(fileToCompare) == false) return;
                    if (Path.GetExtension(fileToCompare).ToLower() != ".jpg") return;
                    if (file == fileToCompare) return;
                    if (new FileInfo(file).Length != new FileInfo(fileToCompare).Length) return;
                    if (CheckSimilarity(file, fileToCompare) >= 95)
                    {
                        similarMatchFound = true;
                        int count = 0;
                        var moveToFile = moveToDirectory + Path.DirectorySeparatorChar + Path.GetFileName(fileToCompare);
                        while (File.Exists(moveToFile) == true)
                        {
                            moveToFile = string.Format("{0}{1}{2}{3}{4}", moveToDirectory, Path.DirectorySeparatorChar, count++, "_", Path.GetFileName(fileToCompare));
                        }

                        if (!filesToMove.ContainsKey(fileToCompare)) 
                        { 
                            filesToMove.TryAdd(fileToCompare, moveToFile); 
                            Console.WriteLine("Number of similar files to consider: " + filesToMove.Count);
                        }
                    }
                });

                if (similarMatchFound == true)
                {
                    var moveToFile = moveToDirectory + Path.DirectorySeparatorChar + Path.GetFileName(file);
                    int count = 0;
                    while (File.Exists(moveToFile) == true)
                    {
                        moveToFile = string.Format("{0}{1}{2}{3}{4}", moveToDirectory, Path.DirectorySeparatorChar, count++, "_", Path.GetFileName(file));
                    }

                    if (!filesToMove.ContainsKey(file)) 
                    { 
                        filesToMove.TryAdd(file, moveToFile);
                        Console.WriteLine("Number of similar files to consider: " + filesToMove.Count);
                    }
                }
            });

            foreach (var file in filesToMove)
            {
                var moveToFile = file.Value;
                int count = 0;
                while (File.Exists(moveToFile) == true)
                {
                    moveToFile = string.Format("{0}{1}{2}{3}{4}", moveToDirectory, Path.DirectorySeparatorChar, count++, "_", Path.GetFileName(file.Key));
                }

                File.Move(file.Key, moveToFile);
            }
            return filesToMove.Count;
        }

        public double CheckSimilarity(string image1filename, string image2filename)
        {
            // Load the images
            var img1 = new Image<Bgr, byte>(image1filename);
            var img2 = new Image<Bgr, byte>(image2filename);

            // Convert the images to grayscale
            Image<Gray, byte> grayImg1 = img1.Convert<Gray, byte>();
            Image<Gray, byte> grayImg2 = img2.Convert<Gray, byte>();

            try
            {
                // Compute the absolute difference between the two images
                Image<Gray, byte> diff = grayImg1.AbsDiff(grayImg2);

                // Compute the sum of all pixel values in the difference image
                double sum = diff.GetSum().Intensity;

                // Compute the similarity score as a percentage
                double similarity = (1 - (sum / (grayImg1.Width * grayImg1.Height * 255))) * 100;
                return similarity;
            }
            catch
            {
                return 0.0;
            }
        }

        public DateTime GetDateTakenFromImage(string path)
        {
            var retVal = File.GetLastWriteTime(path);

            if (Path.GetExtension(path).ToLower() != ".jpg")
            {
                return retVal;
            }

            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    try
                    {
                        using (System.Drawing.Image myImage = System.Drawing.Image.FromStream(fs, false, false))
                        {
                            try
                            {
                                PropertyItem? propItem = myImage.GetPropertyItem(36867);

                                if (propItem != null)
                                {
                                    string dateTaken = new Regex(":").Replace(Encoding.UTF8.GetString(propItem.Value), "-", 2);
                                    if (DateTime.TryParse(dateTaken, out retVal) == false)
                                    {
                                        retVal = File.GetLastWriteTime(path);
                                    }
                                }
                            }
                            catch
                            {
                                return retVal;
                            }
                        }
                    }
                    catch
                    {
                        return DateTime.MinValue;
                    }
                }
            }
            catch
            {
                return retVal;
            }

            return retVal;
        }

        public int CountMedia(string sourceFolder)
        {
            return Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories).Length;
        }

        public void Organize(string sourceFolder, string destinationFolder)
        {
            Console.WriteLine("Starting photo organization");
            var filesToProcess = Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories);

            Parallel.ForEach(filesToProcess, filename => {
                var creationTime = GetDateTakenFromImage(filename);

                var yearFolder = creationTime.Year.ToString();
                var monthFolder = creationTime.ToString("MMMM");

                var moveToDirectory = string.Format("{0}{1}{2}{3}{4}", destinationFolder, Path.DirectorySeparatorChar, yearFolder, Path.DirectorySeparatorChar, monthFolder);
                if (creationTime == DateTime.MinValue)
                {
                    moveToDirectory = string.Format("{0}{1}{2}", sourceFolder, Path.DirectorySeparatorChar, "PossiblyCorrupt");
                }

                if (Directory.Exists(moveToDirectory) == false)
                {
                    Directory.CreateDirectory(moveToDirectory);
                }

                var moveToFile = string.Format("{0}{1}{2}", moveToDirectory, Path.DirectorySeparatorChar, Path.GetFileName(filename));
                Console.WriteLine(moveToFile);
                int count = 1;

                if (filename == moveToFile)
                {
                    return;
                }

                while (File.Exists(moveToFile) == true)
                {
                    moveToFile = string.Format("{0}{1}{2}{3}{4}", moveToDirectory, Path.DirectorySeparatorChar, count++, "_", Path.GetFileName(filename));
                }

                if (File.Exists(filename) == false)
                {
                    return;
                }

                File.Move(filename, moveToFile, false);
            });
        }

        public void CleanupInvalidFilenames(string folder)
        {
            foreach (var fqFilename in Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories))
            {
                var filename = Path.GetFileName(fqFilename);
                if (filename.StartsWith("."))
                {
                    var newFilename = fqFilename.Replace(".", string.Empty);
                    var extension = Path.GetExtension(newFilename);
                    int count = 1;
                    while (File.Exists(newFilename) == true)
                    {
                        newFilename = newFilename.Replace(extension, string.Format("{0}{1}", count++, extension));
                    }
                    File.Move(fqFilename, newFilename, false);
                }
            }
        }
    }
}
