// See https://aka.ms/new-console-template for more information

using Photorganizer;

var sourceFolder = string.Empty;
var destinationFolder = string.Empty;

var image1 = string.Empty;
var image2 = string.Empty;

//Console.WriteLine("Image 1");
//image1 = Console.ReadLine();

//Console.WriteLine("Image 2");
//image2 = Console.ReadLine();

//Console.WriteLine();
//Console.WriteLine(string.Format("Score: {0}", new Organizer().CheckSimilarity(image1, image2)));

while (string.IsNullOrWhiteSpace(sourceFolder) == true)
{
    Console.WriteLine("Select folder containing photos to organize");
    sourceFolder = Console.ReadLine();
}

var start = DateTime.Now;
var result = new Organizer().ProcessSimilarity(sourceFolder);
var end = DateTime.Now;
var diff = end - start;

Console.WriteLine("Found " + result.ToString() + " similar images that would need review");
Console.WriteLine("Operation took: " + diff.TotalHours.ToString() + " to complete.");

//Console.WriteLine(new Organizer().CountMedia(sourceFolder));

//while (string.IsNullOrWhiteSpace(destinationFolder) == true || Directory.Exists(destinationFolder) == false)
//{
//    Console.WriteLine("Select folder where organized photos should go");
//    destinationFolder = Console.ReadLine();
//}




