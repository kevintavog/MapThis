using System;
using System.Collections.Generic;
using System.IO;

namespace MapThis
{
    public class MarkerSet
    {
        public string Id { get { return this.GetHashCode().ToString(); } }
        public IList<string> Files { get; set; }

        public string Title
        {
            get
            {
                var title = String.Format("{0} files", Files.Count);
                if (Files.Count == 1)
                {
                    title = Path.GetFileName(Files[0]);
                }

                return title;
            }
        }
    }
}
