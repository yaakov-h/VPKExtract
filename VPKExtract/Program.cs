using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPKExtract
{
	class Program
	{
		static void Main(string[] args)
		{
			var vpkFilename = args.First();
			var filesToExtract = args.Skip(1);

			using (var vpk = new VpkFile(vpkFilename))
			{
				vpk.Open();

				foreach (var fileToExtract in filesToExtract)
				{
					var fileNode = vpk.GetFile(fileToExtract);
					if (fileNode == null)
					{
						Console.WriteLine("Entry not found: {0}", fileToExtract);
					}
					else
					{
						Console.WriteLine("Found entry: {0} in VPK {1}", fileToExtract, fileNode.ArchiveIndex);
						ExtractFile(vpkFilename, fileNode, fileToExtract);
					}
				}
			}
		}

		static void ExtractFile(string vpkFileName, VpkNode node, string path)
		{

			var prefix = new string(Enumerable.Repeat('0', 3 - node.ArchiveIndex.ToString().Length).ToArray());
			var dataPakFilename = vpkFileName.Replace("_dir.vpk", "_" + prefix + node.ArchiveIndex + ".vpk");
			using (var fsin = new FileStream(dataPakFilename, FileMode.Open))
			{
				fsin.Seek(node.EntryOffset, SeekOrigin.Begin);

				var fileName = path.Split('/').Last();
				using (var fsout = File.OpenWrite(fileName))
				{
					var buffer = new byte[1024];
					int amtToRead = (int)node.EntryLength;
					int read;

					while ((read = fsin.Read(buffer, 0, buffer.Length)) > 0 && amtToRead > 0)
					{
						fsout.Write(buffer, 0, Math.Min(amtToRead, read));
						amtToRead -= read;
					}
				}
			}
		}

		static void PrintUsage()
		{
			Console.WriteLine("Usage: VPKExtract package.vpk file1...filen");
		}
	}
}
