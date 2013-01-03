using Zak.Setup.Commons;
using Zak.Setup.Setup;

namespace Zak.Setup.IIS7
{
	public class OnLoad:IOnLoad
	{
		public void Initialize()
		{
			//Initialize the web administration dll that -should- be present and is not compiled into the package
			AssembliesManager.LoadAssemblyFrom("Microsoft.Web.Administration.dll", null, @"inetsrv");
		}
	}
}
