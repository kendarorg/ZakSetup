using System;
using System.Diagnostics;
using System.DirectoryServices;

namespace Zak.Setup.IIS6.contrib
{
    /// <summary>
    /// There are two types of IISWebDir
    /// </summary>
    public enum IISWebDirType
    {
        IISWebVirtualDir = 1,
        IISWebDirectory = 2,
        IISWebDirUnknow = 3
    }

    /// <summary>
    /// iis web directory
    /// </summary>
    public class IISWebDir
    {
        // directory entry
        private readonly DirectoryEntry _server;

        // directory type mapping table
        private const string IIS_VIRTUAL_DIR = "IIsWebVirtualDir";
        private const string IIS_WEB_DIRECTORY = "IIsWebDirectory";
        private const string DEFAULT_SCRIPT_EXE_FILE = @"c:\windows\microsoft.NET\framework\v2.0.50727\aspnet_isapi.dll";

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="server">directory entry</param>
        internal IISWebDir(DirectoryEntry server)
        {
            _server = server;
        }

        /// <summary>
        /// get or set virtual directory path
        /// </summary>
        public string Path
        {
            get
            {
                
                    return _server.Properties["path"][0].ToString();
                
               
            }
            set
            {
                _server.Properties["path"][0] = value;
            }
        }

        /// <summary>
        /// name of this virtual path
        /// </summary>
        public string Name
        {
            get
            {
                return _server.Name;
            }
  
        }

        /// <summary>
        /// Schema class
        /// </summary>
        public string SchemaClassName
        {
            get
            {
                return _server.SchemaClassName;
            }
        }

        /// <summary>
        /// IISWebType
        /// </summary>
        public IISWebDirType Type
        {
            get
            {
                if (SchemaClassName == IIS_VIRTUAL_DIR)
                {
                    return IISWebDirType.IISWebVirtualDir;
                }
                if (SchemaClassName == IIS_WEB_DIRECTORY)
                {
                    return IISWebDirType.IISWebDirectory;
                }
                return IISWebDirType.IISWebDirUnknow;
            }
        }


        /// <summary>
        /// create application from this dir
        /// </summary>
        /// <param name="appPool">app pool to use, null means default</param>
        public void CreateApplication(IISAppPool appPool)
        {
            if (appPool == null)
            {
                _server.Invoke("AppCreate", true);
            }
            else
            {
                _server.Invoke("AppCreate3", new object[] { 0, appPool.Name, true });
            }

            _server.Properties["AppFriendlyName"].Clear();
            _server.Properties["AppIsolated"].Clear();
            _server.Properties["AccessFlags"].Clear();
            _server.Properties["FrontPageWeb"].Clear();
            _server.Properties["AppFriendlyName"].Add(_server.Name);
            _server.Properties["AppIsolated"].Add(2);
            _server.Properties["AccessFlags"].Add(513);
            _server.Properties["FrontPageWeb"].Add(1);
            //siteVDir.Invoke("AppCreate3",   new   object[]   {2,   "DefaultAppPool",   true});     

            _server.CommitChanges();
        }

        /// <summary>
        /// Create a application
        /// </summary>
        /// <param name="appPool"></param>
        public void CreateApplication(string appPool)
        {
            if (string.IsNullOrEmpty(appPool))
            {
                _server.Invoke("AppCreate", true);
            }
            else
            {
                _server.Invoke("AppCreate3", new object[] { 0, appPool, true });
            }

            _server.Properties["AppFriendlyName"].Clear();
            _server.Properties["AppIsolated"].Clear();
            _server.Properties["AccessFlags"].Clear();
            _server.Properties["FrontPageWeb"].Clear();
            _server.Properties["AppFriendlyName"].Add(_server.Name);
            _server.Properties["AppIsolated"].Add(2);
            _server.Properties["AccessFlags"].Add(513);
            _server.Properties["FrontPageWeb"].Add(1);
            //siteVDir.Invoke("AppCreate3",   new   object[]   {2,   "DefaultAppPool",   true});     

            _server.CommitChanges();
        }

        /// <summary>
        /// delete application, virtual dire can only be deleted using static version
        /// </summary>
        /// <returns></returns>
        public bool DeleteApplication()
        {
            try
            {
                DirectoryEntry parent = _server.Parent;
                var param = new object[2];
                param[0] = "IIsWebVirtualDir"; // indicate that the operation target is a virtual directory
                param[1] = _server.Name; 
                parent.Invoke("Delete", param);
             //   this.server.Invoke("AppDelete", true);
             //   this.server.CommitChanges();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            return true;
        }

        /// <summary>
        /// delete virtual dir
        /// </summary>
        /// <param name="virtualDir">dir want to delete</param>
        /// <returns></returns>
        public static bool DeleteVirtualDir(IISWebDir virtualDir)
        {
            if (virtualDir.Type == IISWebDirType.IISWebVirtualDir)
            {
                virtualDir.DeleteApplication();
                return true;
            }
            return false;
        }

        public bool AddScriptMap(string name)
        {
            return AddScriptMap(name, DEFAULT_SCRIPT_EXE_FILE);
        }


        /// <summary>
        /// add a script map for application
        /// </summary>
        /// <param name="name">".do" or something like this</param>
        /// <param name="exefile">dll file</param>
        public bool AddScriptMap(string name, string exefile)
        {
            return AddScriptMap(name, exefile, 1, "");
        }

        /// <summary>
        /// add script map for application
        /// </summary>
        /// <param name="name">".do" or something like this</param>
        /// <param name="exefile">dll to be loaded</param>
        /// <param name="mask">1 means check "script engine", 4 means check "check file exsit", can be added together</param>
        /// <param name="limitString">limit string</param>
        /// <returns></returns>
        public bool AddScriptMap(string name, string exefile, int mask, string limitString)  
        {
            // validate exefile
            if (System.IO.File.Exists(exefile) == false)
            {
                throw new Exception("file not exist:" + exefile);
            }

            // validate name
            if (name.IndexOf(".",StringComparison.InvariantCultureIgnoreCase) != 0)
            {
                name = "." + name;
            }
            PropertyValueCollection oldMap = _server.Properties["ScriptMaps"];

            // check if exsit
            for (int i = 0; i < oldMap.Count; i++)
            {
                string mapFile = oldMap[i].ToString();
                // already exsit
                if (mapFile.IndexOf(name,StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    return false;
                }
            }

            // add
            string newMap = name + "," + exefile;
            newMap += "," + mask + ",";   // 1 & 4 means the two options
            newMap += limitString;
            _server.Properties["ScriptMaps"].Add(newMap);
            _server.CommitChanges();
            return true;
        }

        /// <summary>
        /// create a new virtual dir
        /// </summary>
        /// <param name="name">virutal dir name</param>
        /// <param name="path">path</param>
        /// <returns>IISWebDir object</returns>
        public IISWebDir CreateVirtualDir(string name, string path)
        {
            // validate path
            if (System.IO.Directory.Exists(path) == false)
            {
                throw new Exception("path:" + path + " not exsit!");
            }

            DirectoryEntry entry = _server.Children.Add(name, IIS_VIRTUAL_DIR);
            entry.Properties["path"].Clear();
            entry.Properties["path"].Add(path);
            entry.CommitChanges();
            return new IISWebDir(entry);
        }

    

        /// <summary>
        /// get dir from this dir
        /// </summary>
        /// <param name="name">the name of the dir</param>
        /// <returns></returns>
        public IISWebDir OpenDir(string name)
        {
            foreach (DirectoryEntry entry in _server.Children)
            {
                if (entry.Name.ToLower() == name.Trim().ToLower())
                {
                    return new IISWebDir(entry);
                }
            }

            throw new Exception("the website directory:" + name + " not found!");

        }

    }
}
