using System;
using NLog;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Xml;
using System.Xml.XPath;

namespace MapThis
{
    public class XmpReader
    {
        static private readonly Logger logger = LogManager.GetCurrentClassLogger();
        private string fullPath;

        public string[] Keywords { get; private set; }

        public XmpReader(string fullPath)
        {
            this.fullPath = fullPath;

            LoadXml();
        }


        private void LoadXml()
        {
            var list = new List<string>();
            try
            {
                using (var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var bufferedStream = new BufferedStream(fileStream))
                using (var reader = new BinaryReader(bufferedStream))
                {
                    // Is it a JPEG?
                    if (ReadUshort(reader) == 0xFFD8)
                    {
                        // There can be multiple EXIF start markers; walk each one until we run out or we find XMP
                        while (FindNextExifStart(bufferedStream, reader))
                        {
                            var startPosition = bufferedStream.Position;
                            var dataLength = ReadUshort(reader);
                            var signature = new String(reader.ReadChars(29));
                            if (signature.Equals("http://ns.adobe.com/xap/1.0/\0"))
                            {
                                // Parse the XML
                                var content = new String(reader.ReadChars(dataLength - signature.Length - 2));
                                var firstLen = content.Length;

                                int pos = content.LastIndexOf("?>");
                                if (pos > 0)
                                {
                                    content = content.Substring(0, pos + 2);
                                }

                                var xmpDoc = XDocument.Parse(content);

                                var namespaceManager = new XmlNamespaceManager(new NameTable());
                                namespaceManager.AddNamespace("rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
                                namespaceManager.AddNamespace("exif", "http://ns.adobe.com/exif/1.0/");
                                namespaceManager.AddNamespace("x", "adobe:ns:meta/");
                                namespaceManager.AddNamespace("xap", "http://ns.adobe.com/xap/1.0/");
                                namespaceManager.AddNamespace("tiff", "http://ns.adobe.com/tiff/1.0/");
                                namespaceManager.AddNamespace("dc", "http://purl.org/dc/elements/1.1/");

                                var subjectBag = xmpDoc.Root.XPathSelectElement(".//dc:subject/rdf:Bag", namespaceManager);
                                if (subjectBag != null)
                                {
                                    foreach (var e in subjectBag.Elements())
                                    {
                                        list.Add(e.Value);
                                    }
                                }
                            }

                            bufferedStream.Seek(startPosition + dataLength, SeekOrigin.Begin);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error("Error reading XMP from '{0}': {1}", fullPath, e);
            }
            Keywords = list.ToArray();
        }

        private bool FindNextExifStart(Stream stream, BinaryReader reader)
        {
            byte markerStart;
            byte markerNumber = 0;
            while (((markerStart = reader.ReadByte()) == 0xFF) && (markerNumber = reader.ReadByte()) != 0xE1)
            {
                var dataLength = ReadUshort(reader);
                stream.Seek(dataLength - 2, SeekOrigin.Current);
            }

            return markerStart == 0xFF && markerNumber == 0xE1;
        }

        private ushort ReadUshort(BinaryReader reader)
        {
            var data = reader.ReadBytes(2);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(data);
            return BitConverter.ToUInt16(data, 0);
        }
    }
}
