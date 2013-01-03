using System;
using System.Collections;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Globalization;
using Zak.Setup.IIS6.contrib.Exceptions;

namespace Zak.Setup.IIS6.contrib
{
	public enum IISWebsiteStatus
	{
		Starting = 1,
		Started = 2,
		Stopping = 3,
		Stopped = 4,
		Pausing = 5,
		Paused = 6,
		Continuign = 7
	}

	/// <summary>
	/// IISWebsite is a class to manage websites on a IIS server. You can not create
	/// a IISWebsite instance directly with a IISWebsite constructor.
	/// 
	/// To get a IISWebsite instance, call IISWebsite.OpenWebsite to open an existing website or
	/// IISWebsite.CreateWebsite to create a new website. 
	/// 
	/// Both of these two methods would return a instance of IISWebsite so that you can use
	/// this instance to handle your website.
	/// 
	/// Author: luckzj
	/// Time  : 27/June. 2010
	/// contact: luckzj12@163.com
	/// website: http://www.soft-bin.com
	/// copy right (c) luckzj. All rights reserved
	/// </summary>
	public class IISWebsite
	{
		// website entry
		private readonly DirectoryEntry _websiteEntry;
		internal const string IIS_WEB_SERVER = "IIsWebServer";

		/// <summary>
		/// Protect this constructor so that user can not create a IISWebsite instance directly.
		/// To get IISWebsite instance, please use IISWebsite.OpenWebsite or IISWebsite.CreateWebsite
		/// </summary>
		/// <param name="server"></param>
		protected IISWebsite(DirectoryEntry server)
		{
			_websiteEntry = server;
		}


		#region Properties

		/// <summary>
		/// get or set name of this website
		/// </summary>
		public string Name
		{
			get
			{
				return _websiteEntry.Properties["ServerComment"][0].ToString();
			}
			set
			{
				_websiteEntry.Properties["ServerComment"][0] = value;
				_websiteEntry.CommitChanges();
			}
		}

		/// <summary>
		/// get or set website port
		/// </summary>
		public int Port
		{
			get
			{
				string port = _websiteEntry.Properties["Serverbindings"][0].ToString();
				port = port.Substring(1);
				port = port.Remove(port.Length - 1, 1);
				return Convert.ToInt32(port);
			}
			set
			{
				_websiteEntry.Properties["Serverbindings"][0] = ":" + value + ":";
				_websiteEntry.CommitChanges();
			}
		}

		/// <summary>
		/// Root path
		/// </summary>
		public IISWebVirturalDir Root
		{
			get
			{
				foreach (DirectoryEntry entry in _websiteEntry.Children)
				{
					if (entry.SchemaClassName == IISWebVirturalDir.IIS_VIRTUAL_DIR)
					{
						return new IISWebVirturalDir(entry);
					}
				}

				throw new WebsiteWithoutRootException(Name);
			}
		}

		/// <summary>
		/// Get iis website Status
		/// </summary>
		public IISWebsiteStatus Status
		{
			get
			{
				object status = _websiteEntry.InvokeGet("Status");
				return (IISWebsiteStatus)status;
			}
		}

		#endregion Properties

		#region Operations

		/// <summary>
		/// Start this website
		/// </summary>
		public void Start()
		{
			_websiteEntry.Invoke("Start");
		}

		/// <summary>
		/// Stop this website
		/// </summary>
		public void Stop()
		{
			_websiteEntry.Invoke("Stop");
		}

		/// <summary>
		/// Parse this website
		/// </summary>
		public void Parse()
		{
			_websiteEntry.Invoke("Pause");
		}

		/// <summary>
		/// Continue to run this website.
		/// </summary>
		public void Continue()
		{
			_websiteEntry.Invoke("Continue");
		}

		#endregion Operations

		#region Static Methods

		/// <summary>
		/// create a new website
		/// </summary>
		/// <param name="name">website name</param>
		/// <param name="port">website port</param>
		/// <param name="rootPath">root path</param>
		/// <returns></returns>
		public static IISWebsite CreateWebsite(string name, int port, string rootPath)
		{
			return CreateWebsite(name, port, rootPath, null);
		}

		public static IISWebsite CreateWebsite(string name, int port, string rootPath, string appPool)
		{
			// validate root path
			if (System.IO.Directory.Exists(rootPath) == false)
			{
				throw new DirNotFoundException(rootPath);
			}

			// get directory service
			var services = new DirectoryEntry("IIS://localhost/W3SVC");

			// get server name (index)
			int index = 0;
			foreach (DirectoryEntry child in services.Children)
			{
				if (child.SchemaClassName == "IIsWebServer")
				{
					if (child.Properties["ServerComment"][0].ToString() == name)
					{
						throw new Exception("website:" + name + " already exsit.");
					}

					if (Convert.ToInt32(child.Name) > index)
					{
						index = Convert.ToInt32(child.Name);
					}
				}
			}
			index++; // new index created

			// create website
			DirectoryEntry server = services.Children.Add(index.ToString(CultureInfo.InvariantCulture), IIS_WEB_SERVER);
			server.Properties["ServerComment"].Clear();
			server.Properties["ServerComment"].Add(name);
			server.Properties["Serverbindings"].Clear();
			server.Properties["Serverbindings"].Add(":" + port + ":");

			// create ROOT for website
			DirectoryEntry root = server.Children.Add("ROOT", IISWebVirturalDir.IIS_VIRTUAL_DIR);
			root.Properties["path"].Clear();
			root.Properties["path"].Add(rootPath);

			// create application
			if (string.IsNullOrEmpty(appPool))
			{
				root.Invoke("appCreate", 0);
			}
			else
			{
				// use application pool
				root.Invoke("appCreate3", 0, appPool, true);
			}

			root.Properties["AppFriendlyName"].Clear();
			root.Properties["AppIsolated"].Clear();
			root.Properties["AccessFlags"].Clear();
			root.Properties["FrontPageWeb"].Clear();
			root.Properties["AppFriendlyName"].Add(root.Name);
			root.Properties["AppIsolated"].Add(2);
			root.Properties["AccessFlags"].Add(513);
			root.Properties["FrontPageWeb"].Add(1);

			// commit changes
			root.CommitChanges();
			server.CommitChanges();

			// return the newly created website
			var website = new IISWebsite(server);
			return website;
		}

		/// <summary>
		/// open a website object
		/// </summary>
		/// <param name="name">name of the website to be opened</param>
		/// <returns>IISWebsite object</returns>
		public static IISWebsite OpenWebsite(string name)
		{
			// get directory service
			var services = new DirectoryEntry("IIS://localhost/W3SVC");
			IEnumerator ie = services.Children.GetEnumerator();
			// ReSharper disable TooWideLocalVariableScope
			DirectoryEntry server;
			// ReSharper restore TooWideLocalVariableScope

			// find iis website
			while (ie.MoveNext())
			{
				server = (DirectoryEntry)ie.Current;
				if (server.SchemaClassName == "IIsWebServer")
				{
					// "ServerComment" means name
					if (server.Properties["ServerComment"][0].ToString() == name)
					{
						return new IISWebsite(server);
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Get exsited websites
		/// </summary>
		public static string[] ExistedWebsites
		{
			get
			{
				var ret = new List<string>();

				// get directory service
				var services = new DirectoryEntry("IIS://localhost/W3SVC");
				IEnumerator ie = services.Children.GetEnumerator();
				// ReSharper disable TooWideLocalVariableScope
				DirectoryEntry server;
				// ReSharper restore TooWideLocalVariableScope

				// find iis website
				while (ie.MoveNext())
				{
					server = (DirectoryEntry)ie.Current;
					if (server.SchemaClassName == "IIsWebServer")
					{
						// "ServerComment" means name
						ret.Add(server.Properties["ServerComment"][0].ToString());

					}
				}

				return ret.ToArray();
			}
		}

		/// <summary>
		/// Delete a website service
		/// </summary>
		/// <param name="name">the name of the website</param>
		public static bool DeleteWebsite(string name)
		{
			IISWebsite website = OpenWebsite(name);
			if (website == null)
			{
				return false;
			}

			website._websiteEntry.DeleteTree();
			return true;
		}

		public static bool Exist(string name)
		{
			// get directory service
			var services = new DirectoryEntry("IIS://localhost/W3SVC");
			IEnumerator ie = services.Children.GetEnumerator();
			// ReSharper disable TooWideLocalVariableScope
			DirectoryEntry server;
			// ReSharper restore TooWideLocalVariableScope

			// find iis website
			while (ie.MoveNext())
			{
				server = (DirectoryEntry)ie.Current;
				if (server.SchemaClassName == "IIsWebServer")
				{
					// "ServerComment" means name
					if (server.Properties["ServerComment"][0].ToString() == name)
					{
						return true;
					}
				}
			}

			return false;
		}

		#endregion Static Methods
	}
}
