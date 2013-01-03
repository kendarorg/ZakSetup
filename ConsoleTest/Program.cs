using System;
using Zak.Setup.Commons;

namespace ConsoleTest
{
	class Program
	{
		static void Main(string[] args)
		{
			TestLoadExternalAssembly();
		}

		private static void TestLoadExternalAssembly()
		{
			AssembliesManager.LoadAssemblyFrom("Microsoft.Web.Administration.dll", null, @"inetsrv");
			var type = AssembliesManager.LoadType(" Microsoft.Web.Administration.ServerManager");
			var obj = Activator.CreateInstance(type);
			Console.WriteLine(obj.GetType().FullName);
		}
	}
}
