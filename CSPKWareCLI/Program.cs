using System;
using System.Collections.Generic;
using System.IO;

namespace CSPKWareCLI
{
	class Program
	{
		private static string GetProjectDirectory()
		{
			return Directory.GetParent(Environment.CurrentDirectory).Parent.FullName;
		}
		public static void Main(string[] args)
		{
			string filePath = Path.Combine(GetProjectDirectory(), @"test-files\small.unpacked");

			const int ChunkSize = 0x1000;
			using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				int bytesRead;
				var buffer = new byte[ChunkSize];
				while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
				{
					Console.WriteLine($"read in {bytesRead} bytes");
				}
			}

			Console.ReadLine();

			/*
			bool explode = false;
			bool implode = false;
			List<string> inputFiles = new List<string>();

			foreach (var arg in args)
			{
				if (arg.StartsWith("-"))
				{
					string cmd = arg.Substring(1).ToLower();
					if (cmd.Equals("explode"))
					{
						explode = true;
					}
					else if (cmd.Equals("implode"))
					{
						implode = true;
					}
					//TODO: additional commands can be parsed here
				}
				else
				{
					inputFiles.Add(arg);
				}
			}

			if (!explode && !implode)
			{
				Console.WriteLine("You have to either specify -explode or -implode");
				return;
			}
			else if (explode && implode)
			{
				Console.WriteLine("You cant explode and implode at the same time");
				return;
			}
			else
			{
				foreach (var file in inputFiles)
				{
					if (!File.Exists(file))
					{
						Console.WriteLine("File '" + file + "' does not exist, skipping");
						continue;
					}
					Func<byte[], byte[]> processor;
					string outFile;
					if (implode)
					{
						processor = csharp_pkware.PKWare.Implode;
						outFile = file + ".packed";
					}
					else //explode
					{
						processor = csharp_pkware.PKWare.Explode;
						outFile = file + ".unpacked";
					}

					byte[] inBytes;
					using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
					{
						inBytes = new byte[fs.Length];
						fs.Read(inBytes, 0, inBytes.Length);
					}

					byte[] outBytes = processor(inBytes);
					using (FileStream fs = new FileStream(outFile, FileMode.Create, FileAccess.Write, FileShare.Read))
					{
						fs.Write(outBytes, 0, outBytes.Length);
					}
				}
			}
			*/
		}
	}
}
