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
			var vpkDirFileName = args.First();
			var filesToExtract = args.Skip(1);

			using (var vpk = new VpkFile(vpkDirFileName))
			{
				vpk.Open();
				Console.WriteLine("Got VPK version {0}", vpk.Version);

				foreach (var fileToExtract in filesToExtract)
				{
					var fileNode = vpk.GetFile(fileToExtract);
					if (fileNode == null)
					{
						Console.WriteLine("Entry not found: {0}", fileToExtract);
					}
					else
					{
						if (fileNode.ArchiveIndex == VpkNode.DirectoryArchiveIndex)
						{
							Console.WriteLine("Found entry: {0}", fileToExtract);
						}
						else
						{
							Console.WriteLine("Found entry: {0} in VPK {1}", fileToExtract, fileNode.ArchiveIndex);
						}
						ExtractFile(vpkDirFileName, fileNode, fileToExtract);
					}
				}
			}
		}

		static void ExtractFile(string vpkDirFileName, VpkNode node, string path)
		{
			using (var inputStream = GetInputStream(vpkDirFileName, node))
			{
				var fileName = path.Split('/').Last();
				using (var fsout = File.OpenWrite(fileName))
				{
					var buffer = new byte[1024];
					int amtToRead = (int)node.EntryLength;
					int read;

					while ((read = inputStream.Read(buffer, 0, buffer.Length)) > 0 && amtToRead > 0)
					{
						fsout.Write(buffer, 0, Math.Min(amtToRead, read));
						amtToRead -= read;
					}
				}
			}
		}

		static Stream GetInputStream(string vpkDirFileName, VpkNode node)
		{
			if (node.EntryLength == 0 && node.PreloadBytes > 0)
			{
				return new MemoryStream(node.PreloadData);
			}
			else if (node.PreloadBytes == 0)
			{
				var prefix = new string(Enumerable.Repeat('0', 3 - node.ArchiveIndex.ToString().Length).ToArray());
				var dataPakFilename = vpkDirFileName.Replace("_dir.vpk", "_" + prefix + node.ArchiveIndex + ".vpk");

				var fsin = new FileStream(dataPakFilename, FileMode.Open);
				fsin.Seek(node.EntryOffset, SeekOrigin.Begin);
				return fsin;
			} else {
				throw new NotSupportedException("Unable to get entry data: Both EntryLength and PreloadBytes specified.");
			}
		}

		static void PrintUsage()
		{
			Console.WriteLine("Usage: VPKExtract package.vpk file1...filen");
		}
	}
}
