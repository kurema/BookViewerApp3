// See https://aka.ms/new-console-template for more information
while (true)
{
    var val = await Console.In.ReadLineAsync();
    if (val is null) return;
    //var match = System.Text.RegularExpressions.Regex.Match(val, @"\(""(\w+)""");
    //if (match.Success)
    //{
    //    Console.WriteLine($@"public const string {match.Groups[1].Value} = ""{match.Groups[1].Value}"";");
    //}
    Console.WriteLine(System.Text.RegularExpressions.Regex.Replace(val, @"\(""(\w+)""", @"(SettingKeys.$1"));
}
