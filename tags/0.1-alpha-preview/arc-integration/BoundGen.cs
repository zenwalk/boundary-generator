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
using System.Runtime.InteropServices;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;

using Mwsw.BoundGen.ArcIntegration.AOIntrospect;
using Mwsw.BoundGen.ArcIntegration.AOData;
using System.Reflection;

using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.ADF.CATIDs;
using ESRI.ArcGIS.Catalog;
	
namespace Mwsw.BoundGen.ArcIntegration
{
	/// <summary>
	/// Boundary generator geoprocessing tool.
	/// 
	/// (Follows the sample code at
	///   http://edndoc.esri.com/arcobjects/9.2/NET/ViewCodePages/914fc3ff-817b-4825-8f15-4df2d725c370GPCalculateAreaCalculateAreaFunction.cs.htm
	/// closely ... for now. It's rather ugly.
	/// </summary>
	/// 
	[ComVisible(true)]
	[Guid("C608F9E1-745B-43CC-97B2-D9D5BEC09CBF")]
	public class BoundGen : IGPFunction
	{
		private IEnumerable<AoGPParameter> m_parms;
		private IArray m_parms_ao;
		private AoTable m_outp_schema;
		IGPUtilities m_util;
		
		public BoundGen() { 
			//throw new Exception("ERE?");
		}
		
		#region IGPFunction implementation

		public IGPMessages Validate(IArray paramValues, 
		                            bool updateValues, 
		                            IGPEnvironmentManager envMgr) {
			
			if (m_util == null) { // delay constructor activities.... they segfault arcmap 
				m_util = new GPUtilitiesClass();
				m_outp_schema = new AoTable(
						AoField.Integer("left_fid"),
						AoField.Integer("right_fid")
			);

			}
			IGPMessages ms = m_util.InternalValidate(ParameterInfo,
			                        paramValues,
			                        updateValues,
			                        true,
			                        envMgr);
			bool passed = AoGPParameter.ValidateAll(m_parms, paramValues, ms);
			
			IGPMessage m = (IGPMessage) ms;
			if (!passed || m.IsError()) {
				ms.AddError(2,"An error here.");
				return ms;
			}
			
			// TODO: Set up the output schema.
			IGPParameter outparam = paramValues.get_Element(3) as IGPParameter;
			IGPFeatureLayer fl = outparam.Value as IGPFeatureLayer;
			if (fl == null)
				fl = new GPFeatureLayerClass();
			
			fl.DEFeatureClass = new DEFeatureClassClass();
			(fl.DEFeatureClass as IDETable).Fields = m_outp_schema.Fields;
			
			// Set up its catalog path / name..
			//			IDataElement de = (fc as IDataElement;
			//			if ( (old_val == null || old_val.DEFeatureClass == null) && src_path != null && src_path.DEFeatureClass != null) {
			//				//src_path = m_util.UnpackGPValue(src_path) as IGPFeatureLayer;
			//				IDataElement srcde = src_path.DEFeatureClass as IDataElement; // lazy cut-n-paste shortcut
			//				de.CatalogPath = srcde.GetPath() + "\\" + srcde.GetBaseName() + "_bndgen." + srcde.GetExtension();// old_val.CatalogPath;
			//				de.Children = srcde.Children;
			//				de.ChildrenExpanded = srcde.ChildrenExpanded;
			//				de.MetadataRetrieved = srcde.MetadataRetrieved;
			//				//de.Name = old_val.Name + "bndgen";
			//				de.Type = srcde.Type;	
			//			}
			m_util.PackGPValue(fl as IGPValue,
			                   outparam);
			return ms;
		}
		public void Execute(IArray paramvalues, 
		                    ITrackCancel trackcancel, 
		                    IGPEnvironmentManager envMgr, 
		                    IGPMessages messages) {
			// Re-validate.
			IGPMessages ms = m_util.InternalValidate(ParameterInfo,
			                                         paramvalues,
							                         false,false,
			                    				     envMgr);

			
			messages.AddMessage("MWSW Boundary Generator, v. 0.0");
			
			if (((IGPMessage)(ms)).IsError()) {
				messages.AddMessages(ms);
				return;
			}
			if (!AoGPParameter.ValidateAll(m_parms, paramvalues,messages)) {
				return;
			}
			
			try {		
				IGPValue inlyr = m_util.UnpackGPValue((paramvalues.get_Element(0) as IGPParameter).Value);
				IGPDouble ang_tol = m_util.UnpackGPValue((paramvalues.get_Element(1) as IGPParameter).Value) as IGPDouble;
				IGPDouble dist_tol = m_util.UnpackGPValue((paramvalues.get_Element(2) as IGPParameter).Value) as IGPDouble;
				IGPValue outlyr = m_util.UnpackGPValue((paramvalues.get_Element(3) as IGPParameter).Value);
	
		//		DumpIt("In layer",inlyr,messages);
		//		DumpIt("Out layer",outlyr,typeof(IGPValue),messages);
	
				IFeatureClass inputfc;
				IQueryFilter inputqf;
				m_util.DecodeFeatureLayer(inlyr, out inputfc, out inputqf);
				
	//			DumpIt("In Featureclass",inputfc,typeof(IFeatureClass),messages);
	//			DumpIt("In QF",inputqf,typeof(IQueryFilter),messages);
				
				messages.AddMessage("In angle tolerance: " + ang_tol.Value);
				messages.AddMessage("In distance tolerance: " + dist_tol.Value);
				messages.AddMessage("Input featureclass: " + inputfc.AliasName);
				messages.AddMessage("Output path: " + outlyr.GetAsText());
				                    
				trackcancel.Progressor.Show();
				trackcancel.Progressor.Message = "Processing...";
			
				string outp_txt = outlyr.GetAsText();
				AoFeatureClassName fcname = new AoFeatureClassName(outp_txt);
				ISpatialReference spref = AoTable.From(inputfc)[inputfc.ShapeFieldName].Field.GeometryDef.SpatialReference;
				
				string shapename = fcname.ShapeFieldName != "" ? fcname.ShapeFieldName : "Shape";
				m_outp_schema[shapename] = AoField.Shape(shapename,
					              esriGeometryType.esriGeometryPolyline,
					              spref);
									

				// TODO: shapefiles seem to get an FID automatically,
				//   while geodb needs an 'OBJECTID',
				//   who knows about other workspace types?
				// Is there a way to figure this out w/o looking at the type?
	
				IFeatureClass outputfc = 
					fcname.Workspace.CreateFeatureClass(fcname.Basename,
					                                    m_outp_schema.Fields,
				                                        null, 
				                                        null, 
				                                        esriFeatureType.esriFTSimple, 
				                                        shapename, 
				                                        "");

				IStepProgressor progressor = trackcancel.Progressor as IStepProgressor;
				progressor.MaxRange = 200;
				
				BGenImp implementation = new BGenImp(spref,ang_tol.Value, dist_tol.Value);
				implementation.Run( inputfc, outputfc, delegate(double howfar) {
				                   	progressor.Position = (int) (200.0 * howfar);
				                   });
								
				messages.AddMessage("Finished, worked through " + implementation.TotalSegments + " line segments total.");
				return;
				
			} catch (Exception e) {
				while (e != null) {
					messages.AddError(1,"Exception: " + e.Message);
					e = e.InnerException;
				}
			}
		}

		private void DumpIt(string l, object o, IGPMessages m) {
			DumpIt(l,"   ", o,m,true);
		}
		private void DumpIt(string label, string idt, object o, IGPMessages ms, bool recurse) {
			if (o == null) {
				ms.AddMessage(label + ": null.");
				return;
			}
			
			AOIntrospector ir = new AOIntrospector(o);
			ms.AddMessage(label + ":");
			foreach(System.Type t in ir.GetImplementedInterfaces()) {
				ms.AddMessage(idt + "(" + t.Name + "):");
				foreach (PropertyInfo pi in t.GetProperties()) {
					if (pi.CanRead) {
						try {
							if (pi.GetIndexParameters().Length == 0) {
								object propv = pi.GetValue(o,null);
								if (propv != null) {
									if (pi.PropertyType.IsValueType || typeof(string).IsAssignableFrom(pi.PropertyType))
										ms.AddMessage(idt + idt + "." + pi.Name + " = " + propv.ToString());
									else if (recurse)
										DumpIt(idt + idt + "." + pi.Name , idt + idt + idt, propv, ms,false);
									else
										ms.AddMessage(idt + idt + "." + pi.Name + " = {{" + propv.ToString() + "}}");
								}
								else
									ms.AddMessage(idt + idt + "." + pi.Name + " = (null)");
							} else {
								ms.AddMessage(idt + idt + "." + pi.Name + " = []");
							}
						} catch (Exception e) {
							ms.AddMessage(idt + idt + "." + pi.Name + " = E{" + e.GetType().Name + ":" + e.Message + "}");
						}
					}
				}
			}
		}
		
		public string Name { get { return "MwswBoundGen"; } }
		
		public string DisplayName { get {return "MWSW Boundary Generator"; } }
		
		public ESRI.ArcGIS.esriSystem.IArray ParameterInfo {
			get {
				if (m_parms == null) {
					m_parms = new AoGPParameter[] {
						AoGPParameter.In("inp_layer", "Input polygons",
						                 new AoFeatureClassType(true, esriGeometryType.esriGeometryPolygon)),
						AoGPParameter.In("inp_angle_tolerance", "Angle tolerance (radians)",
						                 new AoRangedDoubleType(0, Double.MaxValue)),
						AoGPParameter.In("inp_distance_tolerance", "Distance tolerance",
						                 new AoRangedDoubleType(0, Double.MaxValue)),
						AoGPParameter.Out("outp_layer", "Output polylines",
						                  new AoFeatureClassType(false,  m_outp_schema,  esriGeometryType.esriGeometryPolyline,
						                                        esriGeometryType.esriGeometryNull))
					}; 
					m_parms_ao = AoGPParameter.Parameters(m_parms);
				}
				return m_parms_ao;
			}
		}
		public ESRI.ArcGIS.esriSystem.UID DialogCLSID { get { return null; } }
		
		public ESRI.ArcGIS.esriSystem.IName FullName { get { 
				BoundGenFactory fact = new BoundGenFactory();
				return (IName) fact.GetFunctionName("MwswBoundGen");
			}
		}
		
		public string HelpFile { get { return null; } }
		
		public int HelpContext { get { return 0; } }
		
		public string MetadataFile { get {
				return null; // TODO: ship with sample MetaData.
		} }
		
		public bool IsLicensed() { return true; /* good ol' gpl */ }
		
		public object GetRenderer(IGPParameter pParam) { return null; }
		
		#endregion
        
	}
	[ComVisible(true),
	Guid("48F071EC-E75A-45FA-8CF3-CEB6677FC130")]
	public class BoundGenFactory : IGPFunctionFactory {
		
		public BoundGenFactory() {
			
		}
		private IGPFunctionName makeName(string name,
		                                string description,
		                               string dispname) {
			IGPFunctionName nm = new GPFunctionNameClass();
			IGPName n = (IGPName) nm;
			n.Category = this.Name;
			n.Name = name;
			n.Description = description;
			n.DisplayName = dispname;
			n.Factory = this;
			return nm;
		}
		
		[ComRegisterFunction()]
		static void Reg(string key) {
			GPFunctionFactories.Register(key);
		}
		[ComUnregisterFunction()]
		static void UnReg(string key) {
			GPFunctionFactories.Unregister(key);
		}

		public UID CLSID {
			get {
				UID val = new UIDClass();
				val.Value = this.GetType().GUID.ToString("B");
				return val;
			}
		}
		
		public string Name { get { return "MWSW Geoprocessing Tools"; } }
		
		public string Alias { get { return "boundcalc"; } }
		
		public IGPFunction GetFunction(string name) {
			if (name == "MwswBoundGen")
				return new BoundGen();
			else
				return null;
		}
		
		public IGPName GetFunctionName(string name) {
			if (name == "MwswBoundGen") 
				return (IGPName) makeName("MwswBoundGen",
				                "Monkey Wrench Software, Inc. Boundary Generator",
				                "MWSW Boundary Generator");
			else
				return null;
		}
		
		public IEnumGPName GetFunctionNames() {
			IArray res = new EnumGPNameClass();
			res.Add(GetFunctionName("MwswBoundGen"));
			return (IEnumGPName) res;
		}
		
		public IEnumGPEnvironment GetFunctionEnvironments() {
			return null;
		}
		
	}
}
