using System.ServiceProcess;

namespace WindowsService
{
	public partial class SimpleService : ServiceBase
	{
		public SimpleService()
		{
			InitializeComponent();
		}

		protected override void OnStart(string[] args)
		{
		}

		protected override void OnStop()
		{
		}
	}
}
