/****
 * Copyright 2008 Monkey Wrench Software, Inc.
 *
 *   This program is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *****/

using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.esriSystem;

namespace Mwsw.BoundGen.ArcIntegration.AOData
{
	/// <summary>
	/// Wrapper for GP Parameters.
	/// </summary>
	public class AoGPParameter
	{
		private string m_name;
		private string m_dname;
		private bool m_indir;
		private AoGPType m_type;
		private AoGPParameter(string n,
		                      string dn,
		                     AoGPType t,
		                     bool is_in) {
			m_name = n; m_type = t; m_indir = is_in; m_dname = dn;
		}
	
		public static AoGPParameter In(string n, string dn, AoGPType t) {
			return new AoGPParameter(n,dn,t,true);
		}
		
		public static AoGPParameter Out(string n, string dn, AoGPType t) {
			return new AoGPParameter(n,dn, t,false);
		}
		
		public IGPParameter GPParameter { 
			get {
				IGPParameterEdit e = new GPParameterClass();
				e.DataType = m_type.GPType;
				e.Value = m_type.DefaultValue;
				e.Direction = m_indir ? esriGPParameterDirection.esriGPParameterDirectionInput : esriGPParameterDirection.esriGPParameterDirectionOutput;
				e.Name = m_name;
				e.DisplayName = m_dname;
				
				e.ParameterType = m_type.Required ? esriGPParameterType.esriGPParameterTypeRequired :
					esriGPParameterType.esriGPParameterTypeOptional;

				return e as IGPParameter;
			}
		}
				
		public bool Validate(IGPParameter vv, IGPMessages msgs, int idx) {
			return m_type.Check(vv.Value, msgs, idx);
		}
		
		public static bool ValidateAll(IEnumerable<AoGPParameter> ps, IArray aops, IGPMessages msgs) {
			int idx = 0;
			bool ok = true;
			foreach (AoGPParameter p in ps) {
				
				IGPParameter tocheck = aops.get_Element(idx) as IGPParameter;
				ok = ok && p.Validate(tocheck,msgs,idx);
				idx++;
			}
			return ok;
		}
		
		public static IArray Parameters(IEnumerable<AoGPParameter> ps) {
			IArray r = new ArrayClass();
			foreach (AoGPParameter p in ps)
				r.Add(p.GPParameter);
			return r;
		}

	}
}
