using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Zak.Setup.Commons
{
	public class AssembliesManager
	{
		public static Type LoadType(string fullQualifiedName)
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			for (int index = 0; index < assemblies.Length; index++)
			{
				var asm = assemblies[index];
				var toret = LoadType(asm, fullQualifiedName);
				if (toret != null) return toret;

			}
			return null;
		}

		public static Type LoadType(Assembly sourceAssembly, string fullQualifiedName)
		{
			return sourceAssembly.GetType(fullQualifiedName, false);
		}

		public static IEnumerable<Type> LoadTypesWithAttribute(params Type[] types)
		{
			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (!asm.IsDynamic && !string.IsNullOrEmpty(asm.CodeBase))
				{
					foreach (var type in LoadTypesWithAttribute(asm, types))
					{
						yield return type;
					}
				}
			}
		}

		public static IEnumerable<Type> LoadTypesWithAttribute(Assembly sourceAssembly, params Type[] types)
		{
			var toret = new List<Type>();
			
				try
				{
					
					var classTypes = sourceAssembly.GetTypes();
					for (int classTypeIndex = 0; classTypeIndex < classTypes.Length; classTypeIndex++)
					{
						var classType = classTypes[classTypeIndex];
						var customAttributes = classType.GetCustomAttributes(true);
						bool founded = false;
						for (int attributeIndex = 0; attributeIndex < customAttributes.Length && founded==false; attributeIndex++)
						{
							object attribute = customAttributes[attributeIndex];
							for (int attributesTypeIndex = 0; attributesTypeIndex < types.Length; attributesTypeIndex++)
							{
								var type = types[attributesTypeIndex];
								if (attribute.GetType().FullName == type.FullName)
								{
									toret.Add(classType);
									founded = true;
									break;
								}
							}
						}
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex);
				}
			
			return toret;
		}

		public static IEnumerable<Type> LoadTypesInheritingFrom(params Type[] types)
		{
			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (!asm.IsDynamic && !string.IsNullOrEmpty(asm.CodeBase))
				{
					foreach (var type in LoadTypesInheritingFrom(asm, types))
					{
						yield return type;
					}
				}
			}
		}

		private static bool InterfaceFilter(Type typeObj, Object criteriaObj)
		{
			if (typeObj.ToString() == criteriaObj.ToString())
				return true;
			return false;
		}

		public static IEnumerable<Type> LoadTypesInheritingFrom(Assembly sourceAssembly, params Type[] types)
		{
			var toret = new List<Type>();
			try
			{	
				var classTypes = sourceAssembly.GetTypes();
				for (int classTypeIndex = 0; classTypeIndex < classTypes.Length; classTypeIndex++)
				{
					var classType = classTypes[classTypeIndex];
					for (int attributesTypeIndex = 0; attributesTypeIndex < types.Length; attributesTypeIndex++)
					{
						var type = types[attributesTypeIndex];
						if (classType != type)
						{
							if (classType.IsSubclassOf(type) || classType.FindInterfaces(InterfaceFilter,type).Length>0)
							{
								toret.Add(classType);
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}
			return toret;
		}

		private static void DumpAssembly(string path, Assembly assm, Dictionary<string, Assembly> alreadyLoaded)
		{
			AssemblyName fullQualifiedName = assm.GetName();
			if (alreadyLoaded.ContainsKey(fullQualifiedName.FullName))
			{
				return;
			}
			alreadyLoaded[fullQualifiedName.FullName] = assm;

			foreach (AssemblyName name in assm.GetReferencedAssemblies())
			{
				if (!alreadyLoaded.ContainsKey(name.FullName))
				{
					var dllFile = GetAssemblyName(name.FullName);
					var matchingFiles = GetFiles(path, dllFile, SearchOption.AllDirectories);
					if (matchingFiles.Count != 1)
					{
						try
						{
							Assembly.Load(name);
						}
						catch
						{
							throw new FileNotFoundException(string.Format("Dll not found in {0} or subdirectories", path), dllFile);
						}
					}
					else
					{
						Assembly referenced = Assembly.LoadFrom(matchingFiles[0]);
						DumpAssembly(path, referenced, alreadyLoaded);
					}
				}
			}
		}

		private static string GetAssemblyName(string fullName)
		{
			var comma = fullName.IndexOf(",", StringComparison.InvariantCultureIgnoreCase);
			return fullName.Substring(0, comma) + ".dll";
		}

		public static bool LoadAssemblyFrom(string dllFile, List<string> missingDll, params string[] pathsFurnsihed)
		{
			var assemblyLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			var paths = new List<string>();
			foreach (var path in pathsFurnsihed)
			{
				if (Path.IsPathRooted(path))
				{
					paths.Add(path);
				}
				else
				{
					if (!string.IsNullOrEmpty(assemblyLocation)) paths.Add(Path.Combine(assemblyLocation, path));
					paths.Add(Path.Combine(Environment.CurrentDirectory, path));
					paths.Add(Path.Combine(Environment.SystemDirectory, path));
				}
			}

			foreach (var path in paths)
			{
				try
				{
					if (LoadAssemblyFrom(path, dllFile,false))
					{
						return true;
					}
				}
				catch (FileNotFoundException ex)
				{
					if (missingDll != null) missingDll.Add(ex.FileName);
				}
			}
			return false;
		}

		public static bool LoadAssemblyFrom(string path, string dllFile, bool throwOnError = true)
		{
			if (!Directory.Exists(path)) return false;
			if (dllFile == null) return true;
			var alreadyPresentAssemblies = new Dictionary<string, Assembly>();

			foreach (var alreadyPresent in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (!alreadyPresent.IsDynamic && !string.IsNullOrEmpty(alreadyPresent.CodeBase))
				{
					alreadyPresentAssemblies.Add(alreadyPresent.FullName, alreadyPresent);

				}
			}
			var matchingFiles = GetFiles(path, dllFile, SearchOption.AllDirectories);
			if (matchingFiles.Count != 1)
			{
				if (!throwOnError) return false;
				throw new FileNotFoundException(string.Format("Dll not found in {0} or subdirectories", path),
																				Path.Combine(path, dllFile));
			}
			try
			{
				var sm = Assembly.LoadFrom(matchingFiles[0]);
				DumpAssembly(path, sm, alreadyPresentAssemblies);
				return true;
			}
			catch (Exception)
			{
				if (!throwOnError) return false;
				throw new FileNotFoundException("Dll not found ", matchingFiles[0]);
			}
		}


		public static IEnumerable<Assembly> LoadAssembliesFrom(string path, bool deep = true)
		{
			var result = new List<Assembly>();
			var asmFileList = GetFiles(path, "*.dll", deep ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
			var alreadyPresentAssemblies = new Dictionary<string, Assembly>();

			foreach (var alreadyPresent in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (!alreadyPresent.IsDynamic && !string.IsNullOrEmpty(alreadyPresent.CodeBase))
				{
					alreadyPresentAssemblies.Add(alreadyPresent.FullName, alreadyPresent);
				}
			}

			int assemblyLoaded;
			do
			{
				assemblyLoaded = 0;

				for (int i = asmFileList.Count - 1; i > -1; i--)
				{
					try
					{
						var asmPath = asmFileList[i];
						var reflectionOnlyAssembly = Assembly.ReflectionOnlyLoadFrom(asmPath);
						if (!alreadyPresentAssemblies.ContainsKey(reflectionOnlyAssembly.FullName))
						{
#if DEBUG
							Console.WriteLine("Loading '{0}'",Path.GetFileName(asmPath));
#endif
							result.Add(Assembly.LoadFrom(asmPath));
						}

						asmFileList.RemoveAt(i);
						assemblyLoaded++;
					}
					catch (Exception ex)
					{
						Debug.WriteLine(ex);
					}
				}
			} while (assemblyLoaded > 0 && asmFileList.Count > 0);

			if (asmFileList.Count > 0)
				throw new Exception(string.Concat("Error loading assemblies: ", Environment.NewLine, string.Join(Environment.NewLine, asmFileList)));

			return result;
		}

		private static List<string> GetFiles(string path, string pattern, SearchOption options)
		{
			var toret = new List<string>();
			if (!Directory.Exists(path)) return toret;
			var files = Directory.GetFiles(path, pattern, SearchOption.TopDirectoryOnly);
			if (files.Length > 0)
			{
				toret.AddRange(files);
			}
			
			if (options==SearchOption.TopDirectoryOnly) return toret;

			foreach (var dir in Directory.GetDirectories(path))
			{
				try
				{
					var founded = GetFiles(dir, pattern, options);
					toret.AddRange(founded);
				}
				catch (Exception)
				{
					Debug.WriteLine("Forbidden {0}",dir);
				}
				
			}
			return toret;
		}
	}
}
