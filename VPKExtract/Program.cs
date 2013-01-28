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
			if (args.Count() < 2)
			{
				PrintUsage();
				return;
			}

			var vpkDirFileName = args.First();
			var filesToExtract = args.Skip(1);

			if (filesToExtract.FirstOrDefault() == "-l")
			{
				if (args.Count() != 3)
				{
					PrintUsage();
					return;
				}

				var files = new List<string>();
				using (var listReader = File.OpenText(args[2]))
				{
					string line;
					while ((line = listReader.ReadLine()) != null)
					{
						if (!string.IsNullOrWhiteSpace(line))
						{
							files.Add(line);
						}
					}
				}

				filesToExtract = files.AsEnumerable();
			}

			using (var vpk = new VpkFile(vpkDirFileName))
			{
				vpk.Open();
				Console.WriteLine("Got VPK version {0}", vpk.Version);

				foreach (var fileToExtract in filesToExtract)
				{
					var fileNode = vpk.GetFile(fileToExtract);
					if (fileNode == null)
					{
						var dirContents = vpk.GetAllFilesInDirectoryAndSubdirectories(fileToExtract);
						if (dirContents.Count() == 0)
						{
							Console.WriteLine("Entry not found: {0}", fileToExtract);
						}
						else
						{
							foreach (var node in dirContents)
							{
								DoExtractFile(vpkDirFileName, node);
							}
						}
					}
					else
					{
						DoExtractFile(vpkDirFileName, fileNode);
					}
				}
			}
		}

		static void DoExtractFile(string vpkDirFileName, VpkNode node)
		{
			if (node.ArchiveIndex == VpkNode.DirectoryArchiveIndex)
			{
				Console.WriteLine("Found entry: {0}", node.FilePath);
			}
			else
			{
				Console.WriteLine("Found entry: {0} in VPK {1}", node.FilePath, node.ArchiveIndex);
			}
			ExtractFile(vpkDirFileName, node);
		}

		static void ExtractFile(string vpkDirFileName, VpkNode node)
		{
			using (var inputStream = GetInputStream(vpkDirFileName, node))
			{
				var pathPieces = node.FilePath.Split('/');
				var directory = pathPieces.Take(pathPieces.Count() - 1);
				var fileName = pathPieces.Last();

				EnsureDirectoryExists(Path.Combine(directory.ToArray()));

				using (var fsout = File.OpenWrite(Path.Combine(pathPieces)))
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

		static void EnsureDirectoryExists(string directory)
		{
			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
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
