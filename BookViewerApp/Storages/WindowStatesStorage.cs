using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookViewerApp.Storages;
public static class WindowStatesStorage
{
	public static StorageContent<WindowStates.WindowStates> Content = new(SavePlaces.Local, "Sessions.xml", () => new() { Entries = new WindowStates.WindowStatesEntry[0] });
}
