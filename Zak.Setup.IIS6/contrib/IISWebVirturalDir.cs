using System.Collections.Generic;
using System.DirectoryServices;
using Zak.Setup.IIS6.contrib.Exceptions;

namespace Zak.Setup.IIS6.contrib
{
    /// <summary>
    /// This class represent a IIS virtual directory.
    /// 
    /// Author: luckzj
    /// Time:   27/June 2010
    /// Email:  luckzj12@163.com
    /// Website: http://soft-bin.com
    /// </summary>
    public class IISWebVirturalDir
    {
        // Directory entry for this dir
        private readonly DirectoryEntry _entry;
        internal const string IIS_VIRTUAL_DIR = "IIsWebVirtualDir";


        /// <summary>
        /// Internal this constructor so that user of this dll can not create a instance of IISWebVirtualDir directly.
        /// To get a instance of IISWebVirtualDir, please use IISWebVirtualDir.OpenSubVirtualDir or IISWebVirtualDir.CreateSubVirtualDir.
        /// </summary>
        /// <param name="entry"></param>
        internal IISWebVirturalDir(DirectoryEntry entry)
        {
            _entry = entry;
        }


        #region Properties

        /// <summary>
        /// Get or set the path of this virtual directory
        /// </summary>
        public string Path
        {
            get
            {
                return _entry.Properties["path"][0].ToString();
            }
            set
            {
                _entry.Properties["path"][0] = value;
                _entry.CommitChanges();
            }
        }

        /// <summary>
        /// Get name of this virtual path.
        /// </summary>
        public string Name
        {
            get
            {
                return _entry.Name;
            }

        }

        #endregion Properties

        #region Operations

        /// <summary>
        /// Check whether a virtual directory exists.
        /// </summary>
        /// <param name="name">Name of dir checked</param>
        /// <returns>true if exist. Otherwise false.</returns>
        public bool ExistVirtualDir(string name)
        {
	        return (FindSubEntry(name) != null);
        }

        /// <summary>
        /// Create a sub virtual directory
        /// </summary>
        /// <param name="name">Name of the sub virtual directory to be created.</param>
        /// <param name="path">Path of the sub virtual directory.</param>
        /// <param name="appPool">
        /// Application pool. Application pool with this name would be created if not exist. 
        /// Use string.Empty or null to this parameter if you don't want to use a application pool.
        /// </param>
        /// <returns>A IISWebVirtualDir if created. Otherwise  null.</returns>
        public IISWebVirturalDir CreateSubVirtualDir(string name, string path, string appPool)
        {
            // already exist
            if (ExistVirtualDir(name))
            {
                throw new VirtualDirAlreadyExistException(_entry, path);
            }

            // validate path
            if (System.IO.Directory.Exists(path) == false)
            {
                throw new DirNotFoundException(path);
            }

            
            DirectoryEntry entry = _entry.Children.Add(name, IIS_VIRTUAL_DIR);
            entry.Properties["path"].Clear();
            entry.Properties["path"].Add(path);

            // create application
            if (string.IsNullOrEmpty(appPool))
            {
                entry.Invoke("appCreate", 0);
            }
            else
            {
                // use application pool
                entry.Invoke("appCreate3", 0, appPool, true);
            }

            entry.Properties["AppFriendlyName"].Clear();
            entry.Properties["AppIsolated"].Clear();
            entry.Properties["AccessFlags"].Clear();
            entry.Properties["FrontPageWeb"].Clear();
            entry.Properties["AppFriendlyName"].Add(_entry.Name);
            entry.Properties["AppIsolated"].Add(2);
            entry.Properties["AccessFlags"].Add(513);
            entry.Properties["FrontPageWeb"].Add(1);

            entry.CommitChanges();
            return new IISWebVirturalDir(entry);
        }

        /// <summary>
        /// Create a virtual directory
        /// </summary>
        /// <param name="name">Name of the virtual directory</param>
        /// <param name="path">Path of the virtual directory</param>
        /// <returns>A IISWebVirtualDir instance if succeed. Otherwise false.</returns>
        public IISWebVirturalDir CreateSubVirtualDir(string name, string path)
        {
            return CreateSubVirtualDir(name, path, null);
        }

        /// <summary>
        /// Open a sub virtual directory
        /// </summary>
        /// <param name="name">Name of directory to be opened. Case insensitive.</param>
        /// <returns>A IISWebVirtualDir instance if open successfully done.Otherwise null.</returns>
        public IISWebVirturalDir OpenSubVirtualDir(string name)
        {
            DirectoryEntry entry = FindSubEntry(name);

            if (entry == null)
            {
                return null;
            }

            return new IISWebVirturalDir(entry);
        }

        /// <summary>
        /// Enumerate sub virtual directorys
        /// </summary>
        /// <returns></returns>
        public string[] EnumSubVirtualDirs()
        {
            var ret = new List<string>();
            foreach (DirectoryEntry entry in _entry.Children)
            {
                if (entry.SchemaClassName == IIS_VIRTUAL_DIR)
                {
                    ret.Add(entry.Name);
                }
            }

            return ret.ToArray();
        }

        /// <summary>
        /// Delete a sub virtual directory
        /// </summary>
        /// <param name="name">Name of the sub virtual directory to be deleted</param>
        /// <returns>true if successfully deleted. Otherwise false.</returns>
        public bool DeleteSubVirtualDir(string name)
        {
            DirectoryEntry entry = FindSubEntry(name);

            if (entry == null)
            {
                return false;
            }

            entry.DeleteTree();

            return true;

        }

        #region Script Map

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
                throw new FileNotFoundException(exefile);
            }

            // validate name
            if (name.IndexOf(".", System.StringComparison.Ordinal) != 0)
            {
                name = "." + name;
            }
            PropertyValueCollection oldMap = _entry.Properties["ScriptMaps"];

            // check if exsit
            for (int i = 0; i < oldMap.Count; i++)
            {
                string mapFile = oldMap[i].ToString();
                // already exsit
                if (mapFile.IndexOf(name, System.StringComparison.Ordinal) == 0)
                {
                    return false;
                }
            }

            // add
            string newMap = name + "," + exefile;
            newMap += "," + mask + ",";   // 1 & 4 means the two options
            newMap += limitString;
            _entry.Properties["ScriptMaps"].Add(newMap);
            _entry.CommitChanges();
            return true;
        }

        #endregion Script Map

        #endregion Operations

        #region internal utils

        protected DirectoryEntry FindSubEntry(string name)
        {
            DirectoryEntry ret = null;
            foreach (DirectoryEntry entry in _entry.Children)
            {
                if (entry.SchemaClassName == IIS_VIRTUAL_DIR && entry.Name.ToLower() == name.ToLower())
                {
                    ret = entry;
                }
            }

            return ret;
        }

        #endregion internal utils

    }
}
