// See https://aka.ms/new-console-template for more information

using System.Xml;

//var client = new HttpClient();
//var text = await client.GetStringAsync("http://aozora.textlive.net/catalog.opds");
//Console.WriteLine(text);

using var xr = XmlReader.Create("http://aozora.textlive.net/catalog.opds");
var sf = System.ServiceModel.Syndication.SyndicationFeed.Load(xr);
Console.WriteLine(sf.Title?.Text);
Console.WriteLine(sf.Description?.Text);

foreach(var item in sf.Items)
{
    Console.WriteLine($"Title: {item.Title?.Text}");
    Console.WriteLine($"Link: {item.Links?.First()?.Uri?.AbsolutePath}");
}

Console.ReadLine();