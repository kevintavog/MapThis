using MonoMac.Foundation;
using System.Collections.Generic;
using MonoMac.ImageIO;

namespace MapThis.View
{
	public partial class MainWindowController : MonoMac.AppKit.NSWindowController
	{
        private string[] videoTypes = { "public.mpeg-4" };
        
		private ImageFilter[] imageFilters =
        {
            new ImageFilter("JPEG & PNG", new HashSet<string>() { "public.jpeg", "public.png", "public.mpeg-4" }),
			new ImageFilter("All images", new HashSet<string>(CGImageSource.TypeIdentifiers))
		};

		private void InitializeImageFilters()
		{
			imageFilterSelector.RemoveAllItems();
			foreach (var item in imageFilters)
			{
				imageFilterSelector.AddItem(item.Name);
			}
		}

		partial void fileType(NSObject sender)
		{
			ChangeFileTypes(imageFilters[imageFilterSelector.IndexOfSelectedItem].Types);

			Preferences.Instance.ImageFilterIndex = imageFilterSelector.IndexOfSelectedItem;
			Preferences.Instance.Save();
		}

		partial void imageSize(NSObject sender)
		{
			UpdateImageViewZoom();
		}
	}

	class ImageFilter
	{
		public ImageFilter(string name, HashSet<string> types)
		{
			Name = name;
			Types = types;
		}

		public string Name { get; private set; }
		public HashSet<string> Types { get; private set; }
	}
}
