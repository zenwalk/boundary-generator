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
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;

namespace Mwsw.BoundGen.ArcIntegration.AOData 
{
	/// <summary>
	/// Description of AoTable.
	/// </summary>
	public class AoTable {
		private List<AoField> m_fields;
		public AoTable() {
			m_fields = new List<AoField>();
		}
		
		public AoTable(IFields flds) : this() {
			for (int i = 0; i < flds.FieldCount; i++)
				m_fields.Add(new AoField(flds.get_Field(i)));
		}
		
		public AoTable(params AoField[] fields) : this() {
			m_fields.AddRange(fields);
		}
		
		public IEnumerable<AoField> AoFields { get { return m_fields; } }
		
		public static AoTable From(IDEFeatureClass c) { return From(c as IDETable); }
		public static AoTable From(IDETable t) { return new AoTable(t.Fields); }
		public static AoTable From(IFeatureClass t) { return new AoTable(t.Fields); }
		public static AoTable From(ITable t) { return From(t as ITable); }
		
		public AoField this[string name] { 
			get {
				foreach (AoField f in m_fields)
					if (f.Name == name)
						return f;
				
				return null;
			} 
			set {
				AoField existing = null;
				int i = 0;
				for (i = 0; i < m_fields.Count; i++) {
					if (m_fields[i].Name == name) {
						existing = m_fields[i];
						break;
					}
				}
				if (existing == null) {
					m_fields.Add(value);
					return;
				}
				
				m_fields[i] = value;
			}
		}
		
		public void MergeOnto(IFields ffs) {
			IFieldsEdit fe = ffs as IFieldsEdit;
			List<AoField> to_append = new List<AoField>();
			foreach (AoField f in m_fields) {
				int idx = fe.FindField(f.Name);
				if (idx == -1) { // already there.
					to_append.Add(f);
				} else { // new
					fe.set_Field(idx, f.Field);
				}
			}
			
			foreach (AoField f in to_append)
				fe.AddField(f.Field);
		}
		
		public IFields Fields { 
			get {
				IFields n = new FieldsClass();
				MergeOnto(n);
				return n;
			}
		}
	}
	
	public class AoField {
		private IField m_if;
		private string m_nm;
		
		public AoField(IField f) { m_if = (f as IClone).Clone() as IField; m_nm = m_if.Name; }
		private AoField(string name,
		                esriFieldType tp) {
			m_nm = name;
			m_if = new FieldClass();
			Ed.Name_2 = name;
			Ed.Type_2 = tp;
		}
		
		public string Name { get { return m_nm; } }
		public IFieldEdit Ed { get { return m_if as IFieldEdit; } }
		public IField Field { get { return m_if; } }
		
		public static AoField Integer(string nm) {
			return new AoField(nm, esriFieldType.esriFieldTypeInteger);
		}

		public static AoField Double(string nm) {
			return new AoField(nm, esriFieldType.esriFieldTypeDouble);
		}
		
		public static AoField Shape(string nm, 
		                            esriGeometryType g, 
		                            ISpatialReference spref) {
			AoField r = new AoField(nm, esriFieldType.esriFieldTypeGeometry);
			
			IGeometryDefEdit geom = new GeometryDefClass() as IGeometryDefEdit;
			geom.GeometryType_2 = g;
			geom.SpatialReference_2 = (spref == null ? null : (spref as IClone).Clone() as ISpatialReference);
			r.Ed.GeometryDef_2 = geom;			
			return r;
		}
	}
}
