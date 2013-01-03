
//As from http://blogs.msdn.com/b/rakkimk/archive/2007/06/09/iis7-sample-code-for-adding-deleting-a-website-programmatically-c-example.aspx
/*
As I told you earlier, IIS7 is a best friend to the developers. Administering websites through code was made very simple by Microsoft.Web.Administration.dll present in the %windir%\system32\inetsrv folder. In this post, let us see a simple command prompt application which add/delete a website. You can do more than what I am going to explain here. This is just a simple sample :)

First of all, you need to add this dll as a reference to your project. To add this from Visual Studio.NET, in the Solution Explorer, right click on the Reference under the current project and select "Add reference". In the browse tab, browse the inetsrv folder and select Microsoft.Web.Administration.dll. Your references will look like the one in the right.

Now, let us see how we can add a new website through our C# application to the IIS7 web server. We need to expose the Microsoft.Web.Administration namespace in our code. There is a ServerManager class available in this namespace which is used to read and write into the IIS7 configuration system. It is the top-level configuration object which allows you to access ApplicationPools, Sites, etc on the IIS7 configuration.

You can add a website using Add method that is present in the Sites collection of ServerManager. Below is the code snippet which creates an application Pool, and then creates a site, also enables the FREB and commit the changes to the IIS7 configuration file:

    ServerManager serverMgr = new ServerManager();
    Site mySite = serverMgr.Sites.Add("MySiteName", "C:\\inetpub\\wwwroot", 8080);
    serverMgr.ApplicationPools.Add("MyAppPool");
    mySite.ApplicationDefaults.ApplicationPoolName = "MyAppPool";
    mySite.TraceFailedRequestsLogging.Enabled = true;
    mySite.TraceFailedRequestsLogging.Directory = "C:\\inetpub\\customfolder\\site";
    serverMgr.CommitChanges();

Now, let's try to delete the same website created. As you know earlier, in IIS7, you need to give an unique name for each website (which was not the case in IIS 6.0).

    Site s1 = serverMgr.Sites["MySiteName"]; // you can pass the site name or the site ID
    serverMgr.Sites.Remove(s1);
    serverMgr.CommitChanges();

That was very simple correct? what are you waiting for? Explore Microsoft.Web.Administration.dll more. You will love it! Again, IIS7 is the best friend to the developers!
 */

using Microsoft.Web.Administration;

namespace Zak.Setup.IIS7
{
	public class InstallIIS7App
	{
		public static void DoSomething()
		{
			var serverMgr = new ServerManager();
			Site mySite = serverMgr.Sites.Add("MySiteName", "C:\\inetpub\\wwwroot", 8080);
			serverMgr.ApplicationPools.Add("MyAppPool");
			mySite.ApplicationDefaults.ApplicationPoolName = "MyAppPool";
			mySite.TraceFailedRequestsLogging.Enabled = true;
			mySite.TraceFailedRequestsLogging.Directory = "C:\\inetpub\\customfolder\\site";
			serverMgr.CommitChanges();
		}
	}
}
