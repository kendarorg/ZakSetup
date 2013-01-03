using System;
using System.IO;
using Zak.Setup.Steps;

namespace Zak.Setup.Core.Steps
{
	[Serializable]
	public class DeleteWorkflowStep : SingleWorkflowStep
	{
		
		public override string GetNodeType()
		{
			return "delete";
		}
		public string From { get; set; }
		public string What { get; set; }

		public DeleteWorkflowStep()
		{
			From = null;
			What = "*.*";
		}

		public override bool Execute(ref string template)
		{
			var from = DoAllReplaces(From);
			var patterns = What.Split(';');
			DeleteAll(from, patterns);
			return true;
		}

		private static bool DeleteAll(string dest, string[] patterns)
		{
			if (!Directory.Exists(dest))
			{
				return true;
			}
			var canRemove = true;
			var sourceDirInfo = new DirectoryInfo(dest);
			DirectoryInfo[] dirs = sourceDirInfo.GetDirectories("*", SearchOption.TopDirectoryOnly);
			foreach (var pattern in patterns)
			{
				foreach (FileInfo file in sourceDirInfo.GetFiles(pattern))
				{
					try
					{
						if (File.Exists(Path.Combine(dest, file.Name)))
						{
							File.Delete(Path.Combine(dest, file.Name));
						}
						Console.WriteLine("Deleted {0}", file.Name);
					}
					catch (Exception)
					{
						
					}
					
				}
				var fileInfoCount = sourceDirInfo.GetFiles().Length;
				if (fileInfoCount > 0)
				{
					canRemove = false;
				}
			}
			foreach (var dir in dirs)
			{
				if ((dir.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
				{
					if (!DeleteAll(Path.Combine(dest, dir.Name), patterns))
					{
						canRemove = false;
					}
				}
				else
				{
					canRemove = false;
				}
			}
			if (canRemove)
			{
				Directory.Delete(dest);	
			}
			return canRemove;
		}
	}
}