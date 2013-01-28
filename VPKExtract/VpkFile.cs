using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPKExtract
{
	class VpkFile : IDisposable
	{
		const uint Magic = 0x55AA1234;

		public VpkFile(string filename)
		{
			this.filename = filename;
		}

		readonly string filename;
		private Stream fileStream;
		private uint treeLength;

		public uint Version { get; private set; }
		private List<VpkNode> nodes;

		public void Open()
		{
			fileStream = File.OpenRead(filename);
			fileStream.Seek(0, SeekOrigin.Begin);

			using (var reader = new BinaryReader(fileStream, Encoding.UTF8, true))
			{
				Load(reader);
			}
		}

		void Load(BinaryReader reader)
		{
			var signature = reader.ReadUInt32();
			if (signature != Magic)
			{
				throw new InvalidDataException("Incorrect magic");
			}

			Version = reader.ReadUInt32();

			if (Version < 1 || Version > 2)
			{
				throw new InvalidDataException("Unknown version");
			}

			switch (Version)
			{
				case 1:
					LoadVersion1Header(reader);
					break;
				case 2:
					LoadVersion2Header(reader);
					break;
				default:
					throw new InvalidOperationException("I got lost.");
			}

			this.nodes = LoadRootNodes(reader);
		}

		static List<VpkNode> LoadRootNodes(BinaryReader reader)
		{
			var nodes = new List<VpkNode>();

			VpkNode newNode = null;
			while (newNode == null || !string.IsNullOrEmpty(newNode.Name))
			{
				newNode = new VpkNode();
				newNode.Load(reader);
				if (!string.IsNullOrEmpty(newNode.Name))
				{
					nodes.Add(newNode);
					newNode.Children = LoadNodeChildren(reader, newNode);
				}
			}

			return nodes;
		}

		static VpkNode[] LoadNodeChildren(BinaryReader reader, VpkNode parent)
		{
			var nodes = new List<VpkNode>();

			VpkNode newNode = null;
			while (newNode == null || !string.IsNullOrEmpty(newNode.Name))
			{
				newNode = new VpkNode(parent);
				newNode.Load(reader);
				if (!string.IsNullOrEmpty(newNode.Name))
				{
					nodes.Add(newNode);
					newNode.Children = LoadNodeFileChildren(reader, newNode);
				}
			}

			return nodes.ToArray();
		}

		static VpkNode[] LoadNodeFileChildren(BinaryReader reader, VpkNode parent)
		{
			var nodes = new List<VpkNode>();

			VpkNode newNode = null;

			while (newNode == null || !string.IsNullOrEmpty(newNode.Name))
			{
				newNode = new VpkNode(parent);
				newNode.LoadFileInfo(reader);
				if (!string.IsNullOrEmpty(newNode.Name))
				{
					nodes.Add(newNode);
				}
			}

			return nodes.ToArray();
		}

		void LoadVersion1Header(BinaryReader reader)
		{
			treeLength = reader.ReadUInt32();
		}

		void LoadVersion2Header(BinaryReader reader)
		{
			treeLength = reader.ReadUInt32();

			var unk1 = reader.ReadInt32();
			var footerLength = reader.ReadUInt32();
			var unk2 = reader.ReadInt32();
			var unk3 = reader.ReadInt32();
		}

		public uint DataOffset
		{
			get
			{
				switch (Version)
				{
					case 1:
						return sizeof(uint) * 3;
					case 2:
						return (sizeof(uint) * 4) + sizeof(int) * 3;
					default:
						throw new InvalidOperationException("Called DataOffset on a VpkFile with unknown version");
				}
			}
		}

		public void Close()
		{
			if (fileStream != null)
			{
				fileStream.Close();
				fileStream.Dispose();
			}
		}

		public VpkNode GetFile(string name)
		{
			var files = from node in nodes
						from dir in node.Children
						from fileEntry in dir.Children
						where fileEntry.FilePath == name
						select fileEntry;
			return files.SingleOrDefault();
		}

		public VpkNode[] GetAllFilesInDirectoryAndSubdirectories(string name)
		{
			var files = from node in nodes
						from dir in node.Children
						where dir.Name == name || dir.Name.StartsWith(name + "/")
						select dir.Children;
			return files.SelectMany(x => x).ToArray();
		}

		public void Dispose()
		{
			Close();
		}
	}
}
