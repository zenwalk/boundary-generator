/*
 * Created by SharpDevelop.
 * User: dan
 * Date: 10/1/2008
 * Time: 2:26 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Reflection;
using System.Collections.Generic;

namespace Mwsw.BoundGen.ArcIntegration.AOIntrospect
{
	/// <summary>
	/// Description of AOIntrospector.
	/// </summary>
	public class AOIntrospector
	{
		private object m_obj;

		private static List<Assembly> _asms;
		public AOIntrospector(object v)
		{
			m_obj = v;
		}
		
		public IEnumerable<System.Type> GetImplementedInterfaces() {
			if (_asms == null) {
				_asms = new List<Assembly>();
				foreach (AssemblyName n in Assembly.GetExecutingAssembly().GetReferencedAssemblies()) {
					if (n.FullName.Contains("ESRI.ArcGIS")) {
				   		Assembly arcasm = Assembly.Load(n);
				   		_asms.Add(arcasm);
					}
				}
			}
			
			foreach (Assembly arcasm in _asms) {
				foreach(Type t in arcasm.GetExportedTypes()) {
					if (t.IsInterface) {
						if (t.IsInstanceOfType(m_obj))
							yield return t;
					}
				}
			}
		}
	}
}
