using MonoMac.AppKit;
using System.Threading;

namespace MapThis.View
{
	class MainClass
	{
		static void Main(string[] args)
		{
			Thread.CurrentThread.Name = "Main";
			NSApplication.Init();
			NSApplication.Main(args);
		}
	}
}
