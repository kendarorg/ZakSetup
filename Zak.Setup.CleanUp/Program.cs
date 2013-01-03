using System;
using System.IO;
using System.Threading;

namespace Zak.Setup.CleanUp
{
	class Program
	{
		static void Main(string[] args)
		{
			Thread.Sleep(1000);
			foreach (var dir in args)
			{
				CleanUpDir(new DirectoryInfo(dir));
				Console.WriteLine("Cleaning {0}.",dir);
			}
		}

		private static void CleanUpDir(DirectoryInfo rootDirInfo)
		{
			DirectoryInfo[] dirs = rootDirInfo.GetDirectories("*", SearchOption.TopDirectoryOnly);
			foreach (FileInfo file in rootDirInfo.GetFiles("*.*",SearchOption.TopDirectoryOnly))
			{
				file.Delete();
			}
			for (int index = dirs.Length - 1; index >= 0; index--)
			{
				CleanUpDir(dirs[index]);
			}
			Directory.Delete(rootDirInfo.FullName);
		}
	}
}
