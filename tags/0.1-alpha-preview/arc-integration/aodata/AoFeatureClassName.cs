/*
 * Created by SharpDevelop.
 * User: dan
 * Date: 10/2/2008
 * Time: 4:31 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geoprocessing;

namespace Mwsw.BoundGen.ArcIntegration.AOData
{
	/// <summary>
	/// A catalog path wrapper for feature classes. (TODO: for anything?)
	///  TODO: error checking...
	/// </summary>
	public class AoFeatureClassName
	{
		private IFeatureClassName m_fcn;
		public AoFeatureClassName(IFeatureClassName n) { m_fcn = n; }
		public AoFeatureClassName(string p) {
			IGPUtilities util = new GPUtilitiesClass();
			m_fcn = util.CreateFeatureClassName(p) as IFeatureClassName;
		}
		public string Basename { get { return (m_fcn as IDatasetName).Name; } }
		public IFeatureWorkspace Workspace { 
			get {
				return ((m_fcn as IDatasetName).WorkspaceName as IName).Open() as IFeatureWorkspace;
			}
		}
		public string ShapeFieldName { get { return m_fcn.ShapeFieldName; } }
		
	}
}
