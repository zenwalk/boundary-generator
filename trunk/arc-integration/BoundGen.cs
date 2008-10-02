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
		IGPUtilities m_util = null;
		IArray m_parameters = null;
		
		public BoundGen() { 
			m_util = new GPUtilitiesClass();
		}
		
		#region IGPFunction implementation

		public IGPMessages Validate(IArray paramValues, 
		                            bool updateValues, 
		                            IGPEnvironmentManager envMgr) {
			
			
			IGPMessages ms = m_util.InternalValidate(ParameterInfo,
			                        paramValues,
			                        updateValues,
			                        true,
			                        envMgr);
			IGPMessage m = (IGPMessage) ms;
			if (m.IsError())
				return ms;
			
			// Set up the output schema.
			IGPParameter outparam = (IGPParameter) paramValues.get_Element(3) as IGPParameter; // outp layer
			IGPFeatureLayer old_val = outparam.Value as IGPFeatureLayer;
			
			IGPParameter srcparam = (IGPParameter) paramValues.get_Element(0) as IGPParameter; // inp layer
			IGPFeatureLayer src_path = srcparam.Value as IGPFeatureLayer;
			
			IDEFeatureClass fc = new DEFeatureClassClass();
			fc.FeatureType = esriFeatureType.esriFTSimple;
			fc.ShapeType = esriGeometryType.esriGeometryPolyline;
			IDETable tab = fc as IDETable;
			
			IFieldsEdit edt = tab.Fields as IFieldsEdit;
			edt.AddField(getField("lhs", esriFieldType.esriFieldTypeInteger));
			edt.AddField(getField("rhs", esriFieldType.esriFieldTypeInteger));
			tab.Fields = edt as IFields;
			
			// Set up its catalog path / name..
			IDataElement de = fc as IDataElement;
			if ( (old_val == null || old_val.DEFeatureClass == null) && src_path != null && src_path.DEFeatureClass != null) {
				//src_path = m_util.UnpackGPValue(src_path) as IGPFeatureLayer;
				IDataElement srcde = src_path.DEFeatureClass as IDataElement; // lazy cut-n-paste shortcut
				de.CatalogPath = srcde.GetPath() + "\\" + srcde.GetBaseName() + "_bndgen." + srcde.GetExtension();// old_val.CatalogPath;
				de.Children = srcde.Children;
				de.ChildrenExpanded = srcde.ChildrenExpanded;
				de.MetadataRetrieved = srcde.MetadataRetrieved;
				//de.Name = old_val.Name + "bndgen";
				de.Type = srcde.Type;	
			}
			IGPFeatureLayer nval = (old_val != null ? old_val as IGPFeatureLayer : new GPFeatureLayerClass() );
			nval.DEFeatureClass = fc;
			m_util.PackGPValue(nval as IGPValue,
			                   outparam);
			
			// Check inputs
			bool a = checkLayerPolygon(paramValues.get_Element(0),ms);
			
			if (!a)
				return ms;
			
			
			// Check that thresholds are > 0 and small,
			double angval = (m_util.UnpackGPValue((paramValues.get_Element(1) as IGPParameter).Value) as IGPDouble).Value;
			if (angval < 0)
				ms.AddError(2,"Angle tolerance < 0 is meaningless.");
			double dstval = (m_util.UnpackGPValue((paramValues.get_Element(2) as IGPParameter).Value) as IGPDouble).Value;
			if (dstval < 0)
				ms.AddError(2,"Distance tolerance < 0 is meaningless.");
			
			IGPValue inlyr = m_util.UnpackGPValue((paramValues.get_Element(1) as IGPParameter).Value);
			IGPValue outp = m_util.UnpackGPValue((paramValues.get_Element(3) as IGPParameter).Value);
//			if (outp.IsEmpty() && !inlyr.IsEmpty()) {
//				IGPValue defval = m_util.GenerateDefaultOutputValue(envMgr, "Boundaries",
//				                                                     (IGPParameter) paramValues.get_Element(4),inlyr, ".shp", 1);
//				m_util.PackGPValue(defval,
//				        	       paramValues.get_Element(4));
//			}
			
			if (angval < 0 || dstval < 0)
				return ms;
			
			// "Create" the output feature class.
			// The fact that this is done in a method named Validate is a gigantic 
			//  pile of bovine poo. But it needs to be done so that people can 
			//  wire up GP tools to the output (which won't really exist until things are run.)
			
//			// TODO

//			IGPFeatureLayer fl = (IGPFeatureLayer) m_util.MakeGPLayer(outp.GetAsText(), new GPFeatureLayerTypeClass());
//			
//			IDEFeatureClass fc = fl.DEFeatureClass;
//			IDETable tab = fc as IDETable;
//			fc.ShapeType = esriGeometryType.esriGeometryPolyline;
//		
//			IFieldsEdit flds = (IFieldsEdit) tab.Fields;
//			addField(flds, "left_fid", esriFieldType.esriFieldTypeOID);
//			addField(flds, "right_fid", esriFieldType.esriFieldTypeOID);
//			tab.Fields = (IFields) flds;
//			
//			// FUTURE: for reconstructing.... addField(flds, "keep", esriFieldType.esriFieldTypeInteger);
//			
			return ms;
		}
		private IFieldEdit getField(string name,
		                      esriFieldType tp) {
			IFieldEdit f = new FieldClass();
			f.Name_2 = name;
			f.Type_2 = tp;
			return f;
		}
		
		private bool checkLayerPolygon(object p0, IGPMessages ms) {
			IGPValue v = ((IGPParameter )p0).Value;
			IGPFeatureLayer fl = v as IGPFeatureLayer;
			if (fl == null) {
				ms.AddError(2,"Input layer is missing.");
				return false;
			} else {
				if (fl.DEFeatureClass.ShapeType != esriGeometryType.esriGeometryPolygon) {
					ms.AddError(2,"Can only generate boundaries for polygon features.");
					return false;
				}
				if (fl.DEFeatureClass.HasZ) {
					ms.AddError(2,"Sorry, 3D polygons not supported yet.");
					return false;					
				}
			}
			return true;
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
			if (((IGPMessage)(ms)).IsError()) {
				messages.AddMessages(ms);
				return;
			}
			
			// TODO: actually do something.
			messages.AddMessage("MWSW Boundary Generator, v. 0.0");
			
			try {
				
				IGPValue inlyr = m_util.UnpackGPValue((paramvalues.get_Element(0) as IGPParameter).Value);
				IGPDouble ang_tol = m_util.UnpackGPValue((paramvalues.get_Element(1) as IGPParameter).Value) as IGPDouble;
				IGPDouble dist_tol = m_util.UnpackGPValue((paramvalues.get_Element(2) as IGPParameter).Value) as IGPDouble;
				IGPValue outlyr = m_util.UnpackGPValue((paramvalues.get_Element(3) as IGPParameter).Value);
	
		//		DumpIt("In layer",inlyr,typeof(IGPValue),messages);
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
				
			
	
				IDEUtilities deutil = new DEUtilitiesClass();
				string outp_txt = outlyr.GetAsText();
				
				IDatasetName outp_name = m_util.CreateFeatureClassName(outp_txt) as IDatasetName;
				IWorkspaceName ws_name = outp_name.WorkspaceName;
				IFeatureWorkspace workspace = (ws_name as IName).Open() as IFeatureWorkspace;
				IFeatureClassName fcn = outp_name as IFeatureClassName;
				
				string shapename = fcn.ShapeFieldName != "" ? fcn.ShapeFieldName : "Shape";
				
				// TODO: shapefiles seem to get an FID automatically,
				//   while geodb needs an 'OBJECTID', who knows about other workspace types?
				
				IFields fields = new FieldsClass();
				
				IFieldsEdit flds = (IFieldsEdit) fields;
				flds.AddField(getField("left_fid", esriFieldType.esriFieldTypeInteger));
				flds.AddField(getField("right_fid", esriFieldType.esriFieldTypeInteger));
				//outlayer.Fields = (IFields) flds;
				
				IFieldEdit shpf = getField(shapename, esriFieldType.esriFieldTypeGeometry);
				IGeometryDef geomdef = new GeometryDefClass();
				
				IGeometryDefEdit geomedit = geomdef as IGeometryDefEdit;
				geomedit.GeometryType_2 = esriGeometryType.esriGeometryPolyline;
				IField srcgeom = inputfc.Fields.get_Field(inputfc.FindField(inputfc.ShapeFieldName));
				geomedit.SpatialReference_2 = (srcgeom.GeometryDef.SpatialReference as IClone).Clone() as ISpatialReference;
				shpf.GeometryDef_2 = geomedit as IGeometryDef;
				flds.AddField(shpf);
				
				string outp_name_string = (outp_name as IName).NameString;
				string alt_out_name_string = outp_name.Name;
				IFeatureClass outputfc = workspace.CreateFeatureClass(alt_out_name_string,
				                                                      fields, null, null, esriFeatureType.esriFTSimple,shapename, "");
				
				// done for the moment.
				if (trackcancel.Progressor is IStepProgressor) {
					(trackcancel.Progressor as IStepProgressor).MaxRange = 50;
				}
				for (int i = 0; i < 50; i++) {
					trackcancel.Progressor.Step();
					System.Threading.Thread.Sleep(10);
				}
				messages.AddMessage("Whee, IUknown is: " + Type.GetTypeFromCLSID(new Guid("{00000000-0000-0000-C000-000000000046}")).Name);
				return;
			} catch (Exception e) {
				while (e != null) {
					messages.AddError(1,"Exception: " + e.Message);
					e = e.InnerException;
				}
			}
		}

		private void DumpIt(string l, object o, System.Type srct, IGPMessages m) {
			DumpIt(l,"   ", o, srct, m,true);
		}
		private void DumpIt(string label, string idt, object o, System.Type srct, IGPMessages ms, bool recurse) {
			if (o == null) {
				ms.AddMessage(label + ": null.");
				return;
			}
			
			AOIntrospector ir = new AOIntrospector(o,srct);
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
										DumpIt(idt + idt + "." + pi.Name , idt + idt + idt, propv, pi.PropertyType, ms,false);
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
				if (m_parameters != null)
					return m_parameters;
				
				// Thanks for the nice clean API, ESRI!
				m_parameters = new ArrayClass();
				
				m_parameters.Add(createLayerParameter("inp_layera",
				                     "Input polygon layer",
				                     true,
				                     true));
				
//				m_parameters.Add(createLayerParameter("inp_layerb",
//				                     "Input polygon layer B",
//				                     true,
//				                     true));
//				
				IGPDouble dv = new GPDoubleClass();
				dv.Value = 32.0 * Double.Epsilon; // A tiny value.
				
				m_parameters.Add(createParameter("inp_angle_tolerance",
				                                 "Angle tolerance in degrees",
				                                 new GPDoubleTypeClass(),
				                                 (IGPValue) dv,
				                                 true,
				                                 esriGPParameterType.esriGPParameterTypeRequired));
				                                 
				m_parameters.Add(createParameter("inp_distance_tolerance",
				                                 "Distance tolerance",
				                                 new GPDoubleTypeClass(),
				                                 (IGPValue) dv,
				                                 true,
				                                 esriGPParameterType.esriGPParameterTypeRequired));
				
				m_parameters.Add(createLayerParameter("outp_lines",
                     "Output poly-line features",
                     false,
                     false));

				return m_parameters;
			}
		}
		
		// helper to create a layer parameter and add it to a parameter array.
		private IGPParameterEdit createLayerParameter(string name,
		                                  string dispname,
		                                  bool input, // true for in; false for out
		                                  bool polygons // false for polylines
		                                 ) {
			IGPParameterEdit edt = createParameter(name,dispname,
			                new GPFeatureLayerTypeClass(),
			                new GPFeatureLayerClass(),
			                input, 
			                input ? esriGPParameterType.esriGPParameterTypeRequired :
			               	esriGPParameterType.esriGPParameterTypeOptional);
			
			return edt;
		}
		
		private IGPParameterEdit createParameter(string name, string dispname,
		                             IGPDataType tp,
		                             IGPValue val,
		                             bool input,
		                             esriGPParameterType required) {
			IGPParameterEdit edt = new GPParameterClass();
			edt.DataType = tp;
			edt.Value = val;
			edt.Direction = input ? 
				esriGPParameterDirection.esriGPParameterDirectionInput
				: esriGPParameterDirection.esriGPParameterDirectionOutput;
			edt.Name = name;
			edt.DisplayName = dispname;
			edt.ParameterType = required;
			return edt;			
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
