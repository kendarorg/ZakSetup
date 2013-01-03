using System.DirectoryServices;

namespace Zak.Setup.IIS6.contrib
{
    /// <summary>
    /// IISAppPool is used to manage AppPool of IIS. We can create an AppPool with this class.
    /// I use Directory Service to manage AppPool.
    /// 
    /// Author: luckzj
    /// Time:   27/June 2010
    /// Email:  luckzj12@163.com
    /// Website: http://soft-bin.com
    /// </summary>
    public class IISAppPool
    {
        private readonly DirectoryEntry _entry;

        /// <summary>
        /// Private constructor. Anyone wants to Create an instance of IISAppPool should call
        /// OpenAppPool
        /// </summary>
				/// <param name="entry"></param>
        protected IISAppPool(DirectoryEntry entry)
        {
            _entry = entry;
        }

        /// <summary>
        /// Get name of the App Pool
        /// </summary>
        public string Name
        {
            get
            {
                string name = _entry.Name;
                return name;
            }
        }

        /// <summary>
        /// Start application pool.
        /// </summary>
        public void Start()
        {
            _entry.Invoke("Start");
        }

        /// <summary>
        /// Stop application pool.
        /// </summary>
        public void Stop()
        {
            _entry.Invoke("Stop");
        }

        /// <summary>
        /// Open a application pool and return an IISAppPool instance
        /// </summary>
        /// <param name="name">application pool name</param>
        /// <returns>IISAppPool object</returns>
        public static IISAppPool OpenAppPool(string name)
        {
            string connectStr = "IIS://localhost/W3SVC/AppPools/";
            connectStr += name;

            if (Exsit(name) == false)
            {
                return null;
            }


            var entry = new DirectoryEntry(connectStr); 
            return new IISAppPool(entry);
        }

        /// <summary>
        /// create app pool
        /// </summary>
        /// <param name="name">the app pool to be created</param>
        /// <returns>IISAppPool created if success, else null</returns>
        public static IISAppPool CreateAppPool(string name)
        {
            var service = new DirectoryEntry("IIS://localhost/W3SVC/AppPools");
            foreach (DirectoryEntry entry in service.Children)
            {
                if (entry.Name.Trim().ToLower() == name.Trim().ToLower())
                {
                    return OpenAppPool(name.Trim());
                }
            }

            // create new app pool
            DirectoryEntry appPool = service.Children.Add(name, "IIsApplicationPool");
            appPool.CommitChanges();
            service.CommitChanges();
            
            return new IISAppPool(appPool);
        }

        /// <summary>
        /// if the app pool specified exsit
        /// </summary>
        /// <param name="name">name of app pool</param>
        /// <returns>true if exsit, otherwise false</returns>
        public static bool Exsit(string name)
        {
            var service = new DirectoryEntry("IIS://localhost/W3SVC/AppPools");
            foreach (DirectoryEntry entry in service.Children)
            {
             
                if (entry.Name.Trim().ToLower() == name.Trim().ToLower())
                {
                    return true;
                }
               
            }
            return false;
           
        }

        /// <summary>
        /// Delete an app pool
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool DeleteAppPool(string name)
        {
            if (Exsit(name) == false)
            {
                return false;
            }

            IISAppPool appPool = OpenAppPool(name);
            appPool._entry.DeleteTree();
            return true;
        }     
    }
}
