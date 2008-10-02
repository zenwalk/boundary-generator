/*
 * Created by SharpDevelop.
 * User: dan
 * Date: 10/1/2008
 * Time: 2:26 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;

namespace Mwsw.BoundGen.ArcIntegration.AOIntrospect
{
	/// <summary>
	/// Description of AOIntrospector.
	/// </summary>
	public class AOIntrospector
	{
		private object m_obj;
		private Type m_t;
		public AOIntrospector(object v, Type knowntype)
		{
			m_obj = v;
			m_t = knowntype;
		}
		
		public IEnumerable<System.Type> GetImplementedInterfaces() {
			foreach(Type t in m_t.Assembly.GetExportedTypes()) {
				if (t.IsInterface) {
					if (t.IsInstanceOfType(m_obj))
						yield return t;
				}
			}
		}
	}
}
