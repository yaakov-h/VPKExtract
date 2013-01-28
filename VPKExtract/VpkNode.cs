using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPKExtract
{
	[DebuggerDisplay("Name = {Name}")]
	class VpkNode
	{
		public VpkNode()
		{
		}

		const uint Terminator = 0xFFFF;

		internal void Load(BinaryReader reader)
		{
			var builder = new StringBuilder();
			char nextChar;
			do
			{
				nextChar = reader.ReadChar();
				builder.Append(nextChar);
			} while (nextChar != '\0');

			this.Name = builder.ToString().TrimEnd('\0');
		}

		internal void LoadFileInfo(BinaryReader reader)
		{
			Load(reader);
			if (string.IsNullOrEmpty(Name))
			{
				return;
			}

			this.Crc = reader.ReadUInt32();
			this.PreloadBytes = reader.ReadInt16();
			this.ArchiveIndex = reader.ReadInt16();
			this.EntryOffset = reader.ReadUInt32();
			this.EntryLength = reader.ReadUInt32();

			var terminator = reader.ReadUInt16();

			if (terminator != Terminator)
			{
				throw new InvalidDataException("Error: VPK entry did not end with correct terminator");
			}

			if (PreloadBytes >= 0)
			{
				this.PreloadData = reader.ReadBytes(PreloadBytes);
			}
		}

		public string Name
		{
			get;
			private set;
		}

		public uint Crc
		{
			get;
			private set;
		}

		public short PreloadBytes
		{
			get;
			private set;
		}

		public short ArchiveIndex
		{
			get;
			private set;
		}

		public uint EntryOffset
		{
			get;
			private set;
		}

		public uint EntryLength
		{
			get;
			private set;
		}

		public byte[] PreloadData
		{
			get;
			private set;
		}

		public VpkNode[] Children { get; internal set; }
	}
}
