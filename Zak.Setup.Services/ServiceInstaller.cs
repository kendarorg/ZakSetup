using System;
using System.Runtime.InteropServices;
using System.Threading;
#if USE_TRANSACTED_INSTALLER
using System.Collections;
using System.Collections.Generic;
using System.Configuration.Install;
using System.IO;
using System.Reflection;
#else
#endif
// ReSharper disable FieldCanBeMadeReadOnly.Local
namespace Zak.Setup.Services
{
#if USE_TRANSACTED_INSTALLER
	public class ServiceInstaller
	{
		private readonly string _sourcePath;
		private readonly string _serviceExecutable;
		private string[] _parameters;

		public ServiceInstaller(string sourcePath, string serviceExecutable)
		{
			_sourcePath = sourcePath;
			_serviceExecutable = serviceExecutable;
			var parametersList = new List<string>();

		}

		private void SetupParameters(Dictionary<string, string> parameters)
		{
			var parametersList = new List<string>();
			if (parameters != null)
			{
				foreach (var item in parameters)
				{
					parametersList.Add(string.Format("-{0}", item.Key));
					if (!string.IsNullOrEmpty(item.Value))
					{
						parametersList.Add(item.Value);
					}
				}
			}
			_parameters = parametersList.ToArray();
		}

		private TransactedInstaller InitializeInstaller(Dictionary<string, string> parameters)
		{
			SetupParameters(parameters);
			var ti = new TransactedInstaller();
			var ai = new AssemblyInstaller(Path.Combine(_sourcePath, _serviceExecutable), _parameters);
			ti.Installers.Add(ai);
			var ic = new InstallContext("Install.log", _parameters);
			ti.Context = ic;
			return ti;
		}

		public void Install(Dictionary<string, string> parameters = null)
		{
			var transactedInstaller = InitializeInstaller(parameters);
			transactedInstaller.Install(new Hashtable());
		}

		public void Uninstall(Dictionary<string, string> parameters = null)
		{
			var transactedInstaller = InitializeInstaller(parameters);
			transactedInstaller.Uninstall(new Hashtable());
		}
	}
#else
	public class ServiceInstaller
	{
		//private const int STANDARD_RIGHTS_REQUIRED = 0xF0000;
		private const int SERVICE_WIN32_OWN_PROCESS = 0x00000010;

#pragma warning disable 169
		// ReSharper disable ConvertToConstant.Local
		[StructLayout(LayoutKind.Sequential)]
		private class SERVICE_STATUS
		{
			public int dwServiceType = 0;
			public ServiceState dwCurrentState = 0;
			public int dwControlsAccepted = 0;
			public int dwWin32ExitCode = 0;
			public int dwServiceSpecificExitCode = 0;
			public int dwCheckPoint = 0;
			public int dwWaitHint = 0;
		}
		// ReSharper restore ConvertToConstant.Local
#pragma warning restore 169

		#region OpenSCManager
		[DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
		static extern IntPtr OpenSCManager(string machineName, string databaseName, ScmAccessRights dwDesiredAccess);
		#endregion

		#region OpenService
		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		static extern IntPtr OpenService(IntPtr hScManager, string lpServiceName, ServiceAccessRights dwDesiredAccess);
		#endregion

		#region CreateService
		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		private static extern IntPtr CreateService(IntPtr hScManager, string lpServiceName, string lpDisplayName, 
			ServiceAccessRights dwDesiredAccess, int dwServiceType, ServiceBootFlag dwStartType, ServiceError dwErrorControl, 
			string lpBinaryPathName, string lpLoadOrderGroup, IntPtr lpdwTagId, string lpDependencies, string lp, string lpPassword);
		#endregion

		#region CloseServiceHandle
		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool CloseServiceHandle(IntPtr hScObject);
		#endregion

		#region QueryServiceStatus
		[DllImport("advapi32.dll")]
		private static extern int QueryServiceStatus(IntPtr hService, SERVICE_STATUS lpServiceStatus);
		#endregion

		#region DeleteService
		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool DeleteService(IntPtr hService);
		#endregion

		#region ControlService
		[DllImport("advapi32.dll")]
		private static extern int ControlService(IntPtr hService, ServiceControl dwControl, SERVICE_STATUS lpServiceStatus);
		#endregion

		#region StartService
		[DllImport("advapi32.dll", SetLastError = true)]
		private static extern int StartService(IntPtr hService, int dwNumServiceArgs, int lpServiceArgVectors);
		#endregion

		public void Uninstall(string serviceName)
		{
			IntPtr scm = OpenScManager(ScmAccessRights.AllAccess);

			try
			{
				IntPtr service = OpenService(scm, serviceName, ServiceAccessRights.AllAccess);
				if (service == IntPtr.Zero)
					throw new ApplicationException("Service not installed.");

				try
				{
					StopService(service);
					if (!DeleteService(service))
						throw new ApplicationException("Could not delete service " + Marshal.GetLastWin32Error());
				}
				finally
				{
					CloseServiceHandle(service);
				}
			}
			finally
			{
				CloseServiceHandle(scm);
			}
		}

		public bool ServiceIsInstalled(string serviceName)
		{
			IntPtr scm = OpenScManager(ScmAccessRights.Connect);

			try
			{
				IntPtr service = OpenService(scm, serviceName, ServiceAccessRights.QueryStatus);

				if (service == IntPtr.Zero)
					return false;

				CloseServiceHandle(service);
				return true;
			}
			finally
			{
				CloseServiceHandle(scm);
			}
		}

		public void InstallAndStart(string serviceName, string displayName, string fileName, string user, string password, ServiceBootFlag startType = ServiceBootFlag.AutoStart)
		{
			Install(serviceName,displayName,fileName,user,password,startType);
			IntPtr scm = OpenScManager(ScmAccessRights.AllAccess);
			try
			{
				IntPtr service = OpenService(scm, serviceName, ServiceAccessRights.AllAccess);

				try
				{
					StartService(service);
				}
				finally
				{
					CloseServiceHandle(service);
				}
			}
			finally
			{
				CloseServiceHandle(scm);
			}
		}


		public void Install(string serviceName, string displayName, string fileName, string user, string password, ServiceBootFlag startType = ServiceBootFlag.AutoStart)
		{
			IntPtr scm = OpenScManager(ScmAccessRights.AllAccess);

			try
			{
				IntPtr service = OpenService(scm, serviceName, ServiceAccessRights.AllAccess);

				if (service == IntPtr.Zero)
					service = CreateService(scm, serviceName, displayName, ServiceAccessRights.AllAccess,
						SERVICE_WIN32_OWN_PROCESS, startType, ServiceError.Normal, fileName, null, IntPtr.Zero, null, user, password);

				if (service == IntPtr.Zero)
					throw new ApplicationException("Failed to install service.");

			}
			finally
			{
				CloseServiceHandle(scm);
			}
		}

		public void StartService(string serviceName)
		{
			IntPtr scm = OpenScManager(ScmAccessRights.Connect);

			try
			{
				IntPtr service = OpenService(scm, serviceName, ServiceAccessRights.QueryStatus | ServiceAccessRights.Start);
				if (service == IntPtr.Zero)
					throw new ApplicationException("Could not open service.");

				try
				{
					StartService(service);
				}
				finally
				{
					CloseServiceHandle(service);
				}
			}
			finally
			{
				CloseServiceHandle(scm);
			}
		}

		public void StopService(string serviceName)
		{
			IntPtr scm = OpenScManager(ScmAccessRights.Connect);

			try
			{
				IntPtr service = OpenService(scm, serviceName, ServiceAccessRights.QueryStatus | ServiceAccessRights.Stop);
				if (service == IntPtr.Zero)
					throw new ApplicationException("Could not open service.");

				try
				{
					StopService(service);
				}
				finally
				{
					CloseServiceHandle(service);
				}
			}
			finally
			{
				CloseServiceHandle(scm);
			}
		}

		private void StartService(IntPtr service)
		{
			StartService(service, 0, 0);
			var changedStatus = WaitForServiceStatus(service, ServiceState.StartPending, ServiceState.Running);
			if (!changedStatus)
				throw new ApplicationException("Unable to start service");
		}

		private void StopService(IntPtr service)
		{
			var status = new SERVICE_STATUS();
			ControlService(service, ServiceControl.Stop, status);
			var changedStatus = WaitForServiceStatus(service, ServiceState.StopPending, ServiceState.Stopped);
			if (!changedStatus)
				throw new ApplicationException("Unable to stop service");
		}

		public ServiceState GetServiceStatus(string serviceName)
		{
			IntPtr scm = OpenScManager(ScmAccessRights.Connect);

			try
			{
				IntPtr service = OpenService(scm, serviceName, ServiceAccessRights.QueryStatus);
				if (service == IntPtr.Zero)
					return ServiceState.NotFound;

				try
				{
					return GetServiceStatus(service);
				}
				finally
				{
					CloseServiceHandle(service);
				}
			}
			finally
			{
				CloseServiceHandle(scm);
			}
		}

		private ServiceState GetServiceStatus(IntPtr service)
		{
			var status = new SERVICE_STATUS();

			if (QueryServiceStatus(service, status) == 0)
				throw new ApplicationException("Failed to query service status.");

			return status.dwCurrentState;
		}

		private bool WaitForServiceStatus(IntPtr service, ServiceState waitStatus, ServiceState desiredStatus)
		{
			var status = new SERVICE_STATUS();

			QueryServiceStatus(service, status);
			if (status.dwCurrentState == desiredStatus) return true;

			int dwStartTickCount = Environment.TickCount;
			int dwOldCheckPoint = status.dwCheckPoint;

			while (status.dwCurrentState == waitStatus)
			{
				// Do not wait longer than the wait hint. A good interval is
				// one tenth the wait hint, but no less than 1 second and no
				// more than 10 seconds.

				int dwWaitTime = status.dwWaitHint / 10;

				if (dwWaitTime < 1000) dwWaitTime = 1000;
				else if (dwWaitTime > 10000) dwWaitTime = 10000;

				Thread.Sleep(dwWaitTime);

				// Check the status again.

				if (QueryServiceStatus(service, status) == 0) break;

				if (status.dwCheckPoint > dwOldCheckPoint)
				{
					// The service is making progress.
					dwStartTickCount = Environment.TickCount;
					dwOldCheckPoint = status.dwCheckPoint;
				}
				else
				{
					if (Environment.TickCount - dwStartTickCount > status.dwWaitHint)
					{
						// No progress made within the wait hint
						break;
					}
				}
			}
			return (status.dwCurrentState == desiredStatus);
		}

		private IntPtr OpenScManager(ScmAccessRights rights)
		{
			IntPtr scm = OpenSCManager(null, null, rights);
			if (scm == IntPtr.Zero)
				throw new ApplicationException("Could not connect to service control manager.");

			return scm;
		}
	}


	public enum ServiceState
	{
		Unknown = -1, // The state cannot be (has not been) retrieved.
		NotFound = 0, // The service is not known on the host server.
		Stopped = 1,
		StartPending = 2,
		StopPending = 3,
		Running = 4,
		ContinuePending = 5,
		PausePending = 6,
		Paused = 7
	}

	[Flags]
	public enum ScmAccessRights
	{
		Connect = 0x0001,
		CreateService = 0x0002,
		EnumerateService = 0x0004,
		Lock = 0x0008,
		QueryLockStatus = 0x0010,
		ModifyBootConfig = 0x0020,
		StandardRightsRequired = 0xF0000,
		AllAccess = (StandardRightsRequired | Connect | CreateService |
								 EnumerateService | Lock | QueryLockStatus | ModifyBootConfig)
	}

	[Flags]
	public enum ServiceAccessRights
	{
		QueryConfig = 0x1,
		ChangeConfig = 0x2,
		QueryStatus = 0x4,
		EnumerateDependants = 0x8,
		Start = 0x10,
		Stop = 0x20,
		PauseContinue = 0x40,
		Interrogate = 0x80,
		UserDefinedControl = 0x100,
		Delete = 0x00010000,
		StandardRightsRequired = 0xF0000,
		AllAccess = (StandardRightsRequired | QueryConfig | ChangeConfig |
								 QueryStatus | EnumerateDependants | Start | Stop | PauseContinue |
								 Interrogate | UserDefinedControl)
	}

	public enum ServiceBootFlag
	{
		Start = 0x00000000,	//Boot start
		SystemStart = 0x00000001,	//At system startup
		AutoStart = 0x00000002, //During startup
		Manual = 0x00000003, //Manual
		Disabled = 0x00000004 //Cannot be started
	}

	public enum ServiceControl
	{
		Stop = 0x00000001,
		Pause = 0x00000002,
		Continue = 0x00000003,
		Interrogate = 0x00000004,
		Shutdown = 0x00000005,
		ParamChange = 0x00000006,
		NetBindAdd = 0x00000007,
		NetBindRemove = 0x00000008,
		NetBindEnable = 0x00000009,
		NetBindDisable = 0x0000000A
	}

	public enum ServiceError
	{
		Ignore = 0x00000000,
		Normal = 0x00000001,
		Severe = 0x00000002,
		Critical = 0x00000003
	}
#endif
}
// ReSharper restore FieldCanBeMadeReadOnly.Local