using System;
using System.Collections.Generic;
using System.Xml;

namespace Zak.Setup
{
	public static class XmlNodeExtension
	{
		public static List<XmlNode> GetChildrenByTag(this XmlNode node, params string[] tagNames)
		{
			var returnList = new List<XmlNode>();
			for (int i = 0; i < node.ChildNodes.Count; i++)
			{
				var child = node.ChildNodes[i];
				CheckTagName(tagNames, child, returnList);
			}
			return returnList;
		}

		private static void CheckTagName(string[] tagNames, XmlNode child, List<XmlNode> returnList)
		{
			foreach (var tagNameCasualCase in tagNames)
			{
				var tagName = tagNameCasualCase.ToLower();
				if (child.Name.ToLower() == tagName)
				{
					returnList.Add(child);
					break;
				}
			}
		}


		public static bool GetAttributeBool(this XmlNode node, string name, bool defaultValue = false)
		{
			var trueFalse = GetAttribute(node, name, defaultValue.ToString().ToLower());
			return string.Compare("true", trueFalse, StringComparison.InvariantCultureIgnoreCase) == 0;
		}

		public static string GetAttribute(this XmlNode node, string name, string defaultValue = null)
		{
			name = name.ToLower();
			if (node.Attributes == null) return defaultValue;
			for (int i = 0; i < node.Attributes.Count; i++)
			{
				var attribute = node.Attributes[i];
				if (attribute.Name.ToLower() == name)
				{
					return attribute.Value;
				}
			}
			return defaultValue;
		}

		public static bool IsAttributeSet(this XmlNode node, string name)
		{
			name = name.ToLower();
			if (node.Attributes == null) return false;
			for (int i = 0; i < node.Attributes.Count; i++)
			{
				var attribute = node.Attributes[i];
				if (attribute.Name.ToLower() == name)
				{
					return true;
				}
			}
			return false;
		}
	}
}