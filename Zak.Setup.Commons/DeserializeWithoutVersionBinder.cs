using System;
using System.Reflection;

namespace Zak.Setup.Commons
{
	
	public sealed class DeserializeWithoutVersionBinder : System.Runtime.Serialization.SerializationBinder
	{
		public override Type BindToType(string assemblyName, string typeName)
		{
			Type typeToDeserialize = null;

			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				// Get the type using the typeName and assemblyName
				typeToDeserialize = asm.GetType(typeName ,false,true);
				if (typeToDeserialize != null) return typeToDeserialize;
			}
			

			return null;
		}
	}
}
