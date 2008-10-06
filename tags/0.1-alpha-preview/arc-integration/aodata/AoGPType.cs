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
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;

namespace Mwsw.BoundGen.ArcIntegration.AOData
{
	/// <summary>
	/// Description of AoGPType.
	/// </summary>
	public abstract class AoGPType
	{
		private bool m_req;
		
		public AoGPType(bool req)
		{
			m_req = req;
		}
		
		public bool Required { get { return m_req; } }
		
		// not reentrant/recursive b/c these are instance vars instead of passed
		//  through all calls.
		private IGPMessages m_msgs; private int m_msgidx;
		private bool m_errored;
		public bool Check(IGPValue val, IGPMessages messages, int idx) {
			try {
				m_errored = false;
				m_msgs = messages;
				m_msgidx = idx;
				IGPUtilities util = new GPUtilitiesClass();
				IGPValue c = util.UnpackGPValue(val); // make sure its unpacked
				if (null == c) {
					if (!m_req) {
						Warning("Will use default value.");
						return true;
					} else {
						Error("A value is required.");
						return false;
					}
				}
				ImpCheck(val);
			} finally {
				//m_msgs = null;// don't hold a ref.
			}
			return !m_errored;
		}
		
		protected abstract void ImpCheck(IGPValue v);
		public abstract IGPDataType GPType { get; }
		public abstract IGPValue DefaultValue { get ; }
		
		protected void Error(string err) { m_errored = true; m_msgs.ReplaceError(m_msgidx,2,err);	}
		protected void Warning(string err) { m_msgs.ReplaceWarning(m_msgidx,err);	}
		protected void Info(string err) { m_msgs.ReplaceMessage(m_msgidx,err);	}
		
		
		protected bool CheckT<T>(object val) where T : class {
			T ignored;
			return CheckT<T>(val,out ignored);
		}
		protected bool CheckT<T>(object val, out T outas) where T : class {
			if (typeof(T).IsInstanceOfType(val)) {
				outas = val as T;
				return true;
			} else {
				outas = null;
				return false;
			}
		}
		
		public static AoGPType AoDouble  {	
			get { return new AoSimpleGPType<GPDoubleTypeClass, IGPDouble, GPDoubleClass>("real number",true); }
		}
	}
	
	public class AoSimpleGPType<GPT, VT,DefVC> : AoGPType where VT : class where GPT: class, new() where DefVC : class, new() {
		private string m_friendly;
		public AoSimpleGPType(string friendly, bool req) : base(req) { m_friendly = friendly; }
		protected override void ImpCheck(IGPValue v)
		{
			VT val;
			if (!CheckT(v, out val)) {
				Error("Required a " + m_friendly + " but didn't get it. Programmer error?");
			}
			ValueCheck(val);
		}
		protected virtual void ValueCheck(VT val) { return; }
		public override IGPDataType GPType { 
			get {
				GPT gpt = new GPT();
				return (IGPDataType) gpt;
			}
		}
		public override IGPValue DefaultValue {
			get { return new DefVC() as IGPValue; }
		}
	}
	
	public class AoRangedDoubleType : AoSimpleGPType<GPDoubleTypeClass,IGPDouble, GPDoubleClass> {
		double m_min, m_max;
		public AoRangedDoubleType(double min, double max) 
			: base("real number", true) {m_min = min; m_max = max; }

		protected override void ValueCheck(IGPDouble val)
		{
			double v = val.Value;
			if (v < m_min)
				Error("Value must be greater than " + m_min);
			if (v > m_max)
				Error("Value must be less than " + m_max);
		}
		public override IGPDataType GPType {
			get { return new GPDoubleTypeClass(); }
		}
		public override IGPValue DefaultValue {
			get { 
				IGPDouble defv = new GPDoubleClass();
				defv.Value = m_min;
				return defv as IGPValue;
			}
		}
	}
		
	public class AoFeatureClassType : AoSimpleGPType<GPFeatureLayerTypeClass, IGPFeatureLayer, GPFeatureLayerClass> {
		private esriGeometryType[] m_allowed;
		private AoTable m_attribs;
		public AoFeatureClassType(bool r, params esriGeometryType[] a) : this(r,null,a) {}
		public AoFeatureClassType(bool required, 
		                          AoTable attribs,
		                          params esriGeometryType[] allowed) : base("Feature layer",required) {
			m_allowed = allowed;
			m_attribs = attribs;
		}
		protected override void ValueCheck(IGPFeatureLayer feat) {
			bool found = (m_allowed.Length == 0); // assume true if no geometries
			if (feat != null && feat.DEFeatureClass != null) {
				for (int i = 0; i < m_allowed.Length; i++)
					if (m_allowed[i] == feat.DEFeatureClass.ShapeType) found = true;
				if (!found) {
					Error("Features are of the wrong type.");
					return;
				}
			} else {
				if (this.Required) {
					Error("A layer is required.");
					return;
				} else {
					Warning("Will use default layer name.");
				}
			}
			
			if (m_attribs != null) {
				AoTable vtb = AoTable.From(feat.DEFeatureClass);
				
				bool ok = true;
				foreach (AoField f in m_attribs.AoFields) {
					if (vtb[f.Name] == null) {
						Error("Missing required field " + f.Name);
						ok = false;
					} // TODO: check the field type too?
				}
				if (!ok)
					return;
			}
		}
	}
}
