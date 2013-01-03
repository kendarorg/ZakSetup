using System;
using System.IO;
using System.Text;
using System.Xml;
using Zak.Setup.Steps;

namespace Zak.Setup.Core.Steps
{
	[Serializable]
	public class WriteTemplateStep : SingleWorkflowStep
	{
		public override string GetNodeType()
		{
			return "writeTemplate";
		}
		public WriteTemplateStep()
		{
			Template = null;
			To = null;
		}
		public string Template { get; set; }
		public string To { get; set; }

		public override bool Execute(ref string template)
		{
			var from =  DoAllReplaces(Template);
			var item = _setupFile.BaseTemplates[from];
			item.Content = DoAllReplaces(item.Content);
			var to = DoAllReplaces(To);
			var destinationPath = _setupFile.GetKey("DestinationPath");
			var destinationFile = Path.Combine(destinationPath, to);
			if (item.IsXml)
			{
				File.WriteAllText(destinationFile, PrintXml(item.Content));
			}
			else
			{
				File.WriteAllText(destinationFile, item.Content);
			}

			Console.WriteLine("Written file {0}", destinationFile);

			return true;
		}

		public override SingleWorkflowStep Undo()
		{
			var to = DoAllReplaces(To);
			var destinationPath = _setupFile.GetKey("DestinationPath");
			var destinationFile = Path.Combine(destinationPath, to);
			if (_setupFile.Unattended)
			{
				return new DeleteTemplateStep
					{
						From = destinationFile,
						Help = string.Format("Delete {0}", destinationFile)
					};
			}
			var varName = Guid.NewGuid().ToString();
			var toret = new AskWorkflowStep
				{
					Help = string.Format("Sould be {0} deleted?", destinationFile),
					For = "YesNo",
					ApplyOn = "${" + varName + "}"
				};
			var ifStep = new IfWorkflowStep
				{
					Value = "${" + varName + "}",
					Is = "true"
				};
			toret.WorkflowSteps.Add(ifStep);
			ifStep.WorkflowSteps.Add(new DeleteTemplateStep
				{
					From = destinationFile
				});
			return toret;
		}

		private static String PrintXml(String xml)
		{
			xml = xml.Trim();
			string result;

			using (var mStream = new MemoryStream())
			{

				using (var writer = new XmlTextWriter(mStream, Encoding.Unicode))
				{
					var document = new XmlDocument();

					// Load the XmlDocument with the XML.
					document.LoadXml(xml);

					writer.Formatting = Formatting.Indented;

					// Write the XML into a formatting XmlTextWriter
					document.WriteContentTo(writer);
					writer.Flush();
					mStream.Flush();

					// Have to rewind the MemoryStream in order to read
					// its contents.
					mStream.Position = 0;

					// Read MemoryStream contents into a StreamReader.
					var sReader = new StreamReader(mStream);

					// Extract the text from the StreamReader.
					result = sReader.ReadToEnd();
				}
			}

			return result;
		}
	}
}