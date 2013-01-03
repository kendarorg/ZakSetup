using System;
using System.IO;
using Zak.Setup.Steps;

namespace Zak.Setup.Core.Steps
{
	[Serializable]
	public class CopyWorkflowStep : SingleWorkflowStep
	{
		public override string GetNodeType()
		{
			return "copy";
		}
		public CopyWorkflowStep()
		{
			From = null;
			To = null;
			What = "*.*";
		}
		public string From { get; set; }
		public string To { get; set; }
		public string What { get; set; }

		public override bool Execute(ref string template)
		{
			var from = DoAllReplaces(From);
			var to = DoAllReplaces(To);
			var patterns = What.Split(';');
			CopyAll(from, to, patterns);
			return true;
		}

		public override SingleWorkflowStep Undo()
		{
			return new DeleteWorkflowStep
				{
					From = To,
					What = What
				};
		}

		private static void CopyAll(string source, string dest, string[] patterns)
		{
			if (!Directory.Exists(dest))
			{
				Directory.CreateDirectory(dest);
				Console.WriteLine("Created directory {0}", Path.GetFileName(dest));
			}
			if (string.IsNullOrEmpty(source))
			{
				source = _setupFile.GetKey("${setupDirectory}");
			}
			var sourceDirInfo = new DirectoryInfo(source);
			DirectoryInfo[] dirs = sourceDirInfo.GetDirectories("*", SearchOption.TopDirectoryOnly);
			foreach (var pattern in patterns)
			{
				foreach (FileInfo file in sourceDirInfo.GetFiles(pattern))
				{
					if (File.Exists(Path.Combine(dest, file.Name)))
					{
						File.Delete(Path.Combine(dest, file.Name));
					}
					File.Copy(file.FullName, Path.Combine(dest, file.Name));
					Console.WriteLine("Copied {0}", file.Name);
				}
			}
			foreach (var dir in dirs)
			{
				CopyAll(dir.FullName, Path.Combine(dest, dir.Name), patterns);
			}
		}
	}
}