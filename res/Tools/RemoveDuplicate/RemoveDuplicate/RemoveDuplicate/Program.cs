using var sr1 = new StreamReader(args[0]);
var list1 = sr1.ReadToEnd().Split('\r', '\n');
using var sr2 = new StreamReader(args[1]);
var list2 = sr2.ReadToEnd().Split('\r', '\n');
foreach (var item in list2)
{
	if (!list1.Contains(item)) Console.WriteLine(item);
}
