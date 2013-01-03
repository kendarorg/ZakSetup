using System.ServiceProcess;

namespace WindowsService
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main()
		{
			var servicesToRun = new ServiceBase[] 
			{ 
				new SimpleService() 
			};
			ServiceBase.Run(servicesToRun);
		}
	}
}
