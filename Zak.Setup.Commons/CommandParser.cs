using System;
using System.Collections;
using System.Collections.Generic;

namespace Zak.Setup.Commons
{

	public class CommandParser
	{
		private readonly string _helpMessage;
		private readonly Dictionary<string, string> _commandLineValues;
		private static Dictionary<string, string> _kvps;

		public string Help {get { return _helpMessage; }}

		private static readonly object _lockObject = new object();

		private static void LoadEnvironmentVariables(EnvironmentVariableTarget target, bool none = false)
		{
			IDictionary environmentVariables;
			if (none)
			{
				environmentVariables = Environment.GetEnvironmentVariables();
			}
			else
			{
				environmentVariables = Environment.GetEnvironmentVariables(target);
			}

			foreach (DictionaryEntry de in environmentVariables)
			{
				var lowerKey = ((string)de.Key).ToLowerInvariant();
				if (!_kvps.ContainsKey(lowerKey))
				{
					_kvps.Add(lowerKey, (string)de.Value);
				}
			}
		}

		public static string GetEnv(string envVar)
		{
			envVar = envVar.ToLowerInvariant();
			InitializeEnvironmentVariables();
			if (_kvps.ContainsKey(envVar))
			{
				return _kvps[envVar];
			}
			return null;
		}

		public static void SetEnv(string envVar,string val)
		{
			envVar = envVar.ToLowerInvariant();
			InitializeEnvironmentVariables();
			if (_kvps.ContainsKey(envVar))
			{
				_kvps[envVar] = val;
			}
			else
			{
				_kvps.Add(envVar, val);
			}
		}

		private static void InitializeEnvironmentVariables()
		{
			if (_kvps == null)
			{
				lock (_lockObject)
				{
					_kvps = new Dictionary<string, string>();
					LoadEnvironmentVariables(EnvironmentVariableTarget.Process, true);
					LoadEnvironmentVariables(EnvironmentVariableTarget.Process);
					LoadEnvironmentVariables(EnvironmentVariableTarget.User);
					LoadEnvironmentVariables(EnvironmentVariableTarget.Machine);
				}
			}
		}

		public CommandParser(string[] args, string helpMessage)
		{
			InitializeEnvironmentVariables();

			_helpMessage = helpMessage;
			_commandLineValues = new Dictionary<string, string>();
			for (int index = 0; index < args.Length; index++)
			{
				var item = args[index];
				if (item.StartsWith("-"))
				{
					_commandLineValues.Add(item.Substring(1).ToLowerInvariant(), string.Empty);
				}
				if (index < (args.Length - 1))
				{
					var nextItem = args[index + 1];
					if (!nextItem.StartsWith("-"))
					{
						_commandLineValues[item.Substring(1).ToLowerInvariant()] = nextItem;
					}
				}
			}
			if (IsSet("help") || IsSet("h"))
			{
				ShowHelp();
			}
		}

		public string this[string index]
		{
			get
			{
				index = index.ToLowerInvariant();
				if (IsSet(index))
					return _commandLineValues[index];
				return null;
			}
			set
			{
				index = index.ToLowerInvariant();
				if (IsSet(index))
					_commandLineValues[index] = value;
				else
					_commandLineValues.Add(index, value);
			}
		}

		public bool IsSet(string index)
		{
			index = index.ToLowerInvariant();
			return _commandLineValues.ContainsKey(index);
		}

		public bool Has(params string[] vals)
		{
			foreach (var item in vals)
			{
				var index = item.ToLowerInvariant();
				if (!IsSet(index)) return false;
			}
			return true;
		}

		public bool HasAllOrNone(params string[] vals)
		{
			int setted = 0;
			foreach (var item in vals)
			{
				var index = item.ToLowerInvariant();
				if (IsSet(index)) setted++;
			}
			if (setted == 0 || setted == vals.Length) return true;
			return false;
		}

		public bool HasOneAndOnlyOne(params string[] vals)
		{
			bool setted = false;
			foreach (var item in vals)
			{
				var index = item.ToLowerInvariant();
				if (IsSet(index))
				{
					if (setted)
					{
						return false;
					}
					setted = true;
				}
			}
			return setted;
		}

		public void ShowHelp()
		{
			Console.WriteLine(_helpMessage);
			Console.ReadKey();
			Environment.Exit(0);
		}
	}
}