/*
 * Created by SharpDevelop.
 * User: dan
 * Date: 10/5/2008
 * Time: 4:23 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;

using System.Collections.Generic;

using Mwsw.Ops;
using Mwsw.Geom;
using Mwsw.Util;
using Mwsw.Spidx;

namespace Mwsw.BoundGen.ArcIntegration
{
	public delegate void ProgressCallback(double howfar);
	/// <summary>
	/// A scratch implementation of boundary gen, just to see if it works.
	/// </summary>
	public class BGenImp
	{
		double m_dtol, m_atol;
		ISpatialReference m_spref;
		private int m_seensegs;
		public BGenImp(ISpatialReference spref, double dtol, double atol)
		{
			m_spref = spref;
			m_dtol = dtol; m_atol = atol;
			m_seensegs = 0;
		}
		public int TotalSegments { get { return m_seensegs; } }
		
		private class ArcSegInf : IHasLine {
			private int m_src_fid, m_shell_num, m_seg_num;
			private LineSeg m_seg;
			public ArcSegInf(LineSeg seg, int src_fid, int shell_num, int seg_num) {
				m_seg = seg; m_src_fid = src_fid; m_shell_num = shell_num; m_seg_num = seg_num;
			}
			public int SrcFid { get { return m_src_fid; } }
			public int ShellNum { get { return m_shell_num; } }
			public int SegNum { get { return m_seg_num; } }
			public LineSeg LineSeg { get { return m_seg;} }
		}
		
		
		public void Run(IFeatureClass inp, IFeatureClass outp, ProgressCallback progress) {
			IFeatureCursor all_inp = inp.Search(null,true);
			
			LineSegOverlay<ArcSegInf> op = new LineSegOverlay<ArcSegInf>(new PMQTree(4,8),m_atol, m_dtol);
			
			double inp_count = inp.FeatureCount(null);
			int steps = (int) Math.Ceiling((double)inp_count / 20.0);
			int inp_progress = 0;
			IFeature infeat = all_inp.NextFeature();
			while (infeat != null) {
				int fid = infeat.OID;
				IGeometryCollection c = infeat.Shape as IGeometryCollection;
				for (int shell_num = 0; shell_num < c.GeometryCount; shell_num++) {
					IPointCollection ps = c.get_Geometry(shell_num) as IPointCollection;
					IPoint last = null;
					for (int seg_num = 0; seg_num < ps.PointCount; seg_num++) {
						IPoint cur = ps.get_Point(seg_num);
						if (last != null) {
							LineSeg seg = LineSeg.FromEndpoints(new Vector(last.X,last.Y),
							                                    new Vector(cur.X, cur.Y));
							op.Insert(new ArcSegInf(seg, fid, shell_num, seg_num));
							m_seensegs++;
						}
						last = cur;
					}
				}
				inp_progress++;
				if (inp_progress % steps == 0) {
					progress(0.5 * (inp_progress / inp_count));
				}
				infeat = all_inp.NextFeature();
			}

			int lfid = outp.FindField("left_fid");
			int rfid = outp.FindField("right_fid");
			
			// Reassemble line strings
			SegmentAssembler sa = new SegmentAssembler();
			foreach(Pair<LineSeg, IEnumerable<ArcSegInf>> r in op.Segments) {
				int overlap_count = 0;
				foreach(Pair<ArcSegInf, ArcSegInf> v in 
				        Pair<ArcSegInf,ArcSegInf>.Pairs(r.Second)) {
					
					OverlapSegment o = new OverlapSegment(r.First, v.First, v.Second);
					if (v.First.SrcFid < v.Second.SrcFid) 
						sa.AddSegment(o);
					overlap_count++;
				}
				if (overlap_count == 0) {
					// TODO: add exterior boundaries?
					ArcSegInf seen = null;
					foreach (ArcSegInf seg in r.Second) {
						seen = seg;
					}
					sa.AddSegment(new OverlapSegment(r.First, seen, null));
				}
			}

			double outp_count = sa.OverlapCount;
			int outp_progress = 0;
			int outp_steps = (int) Math.Ceiling((double) outp_count / 20.0);
			IFeatureBuffer outfeat = outp.CreateFeatureBuffer();
			IFeatureCursor outcursor = outp.Insert(true);
			
			foreach(Pair<Pair<int,int>,Pair<int,int>> ss in sa.SegsAndShells) {
				int rhs_fid = ss.First.First; 
				int rhs_shellid = ss.First.Second;
				int lhs_fid = ss.Second.First;
				int lhs_shellid = ss.Second.Second;
				
				foreach (Pair<Pair<int,int>,List<LineSeg>> linestring in sa.GetStrings(ss)) {
					IPolyline line = assemblePolyLine(linestring.Second);
					outfeat.set_Value(lfid, lhs_fid);
					outfeat.set_Value(rfid, rhs_fid);
					outfeat.Shape = line;
					outcursor.InsertFeature(outfeat);
					
					// Same boundary but swapped!
					outfeat.set_Value(lfid, rhs_fid);
					outfeat.set_Value(rfid, lhs_fid);
					outfeat.Shape = line;
					outcursor.InsertFeature(outfeat);
					
				}
				// progress
				outp_progress++;
				if (outp_progress % outp_steps == 0) {
					progress(0.5 + (0.5 * outp_progress / outp_count));
				}

			}
			
			outcursor.Flush();
			System.Runtime.InteropServices.Marshal.ReleaseComObject(outcursor);
		}
		
//		private LineSeg appropriatelySwapped(double dst, LineSeg prior, LineSeg cur, LineSeg next) {
//			if (prior == null && next == null)
//				return cur;
//			if (prior != null) {
//				if ((prior.End - cur.End).LengthSq < dst) {
//					return LineSeg.FromEndpoints(cur.End, cur.Start);
//				}
//			} else if (next != null) {
//				if ((cur.Start - next.Start).LengthSq < dst) {
//					return LineSeg.FromEndpoints(cur.End, cur.Start);
//				}
//			}
//			return cur;
//		}
//		
		private object __missing = System.Reflection.Missing.Value;
		private IPolyline assemblePolyLine(List<LineSeg> segs) {
			IPolyline ln = new PolylineClass();
			double dsq = m_dtol * m_dtol;
			
			ISegmentCollection path = new PathClass();
			
//			List<LineSeg> sorted = new List<LineSeg>();;
//			for (int i = 0; i < segs.Count; i++) {
//				LineSeg prior = (i > 0 ? sorted[i-1] : null);
//				LineSeg next = (i < segs.Count-1 ? segs[i+1] : null);
//				sorted.Add(appropriatelySwapped(dsq,prior,segs[i],next));
//			}
//			
			
			for (int i = 0; i < /*sorted*/segs.Count; i++) {
				LineSeg l = /*sorted*/segs[i];
				ILine part = new LineClass();
				IPoint fromp = new PointClass();
				fromp.X = l.Start.X;
				fromp.Y = l.Start.Y;
				IPoint top = new PointClass();
				top.X = l.End.X;
				top.Y = l.End.Y;
				part.FromPoint = fromp;
				part.ToPoint = top;
				object ignored = System.Reflection.Missing.Value;
				path.AddSegment((ISegment) part , ref __missing, ref __missing);
			}
			(ln as IGeometryCollection).AddGeometry((IGeometry)path, ref __missing, ref __missing);
			ln.SpatialReference = m_spref;
			return ln;
		}
		
		private class OverlapSegment {
			public int Fid;
			public int OFid;
			public int Shell;
			public int OShell;
			public int SegNum;
			public int OSegNum;
			public LineSeg Seg;
			public OverlapSegment(LineSeg s, ArcSegInf m, ArcSegInf o) {
				Seg = s;
				Fid = m.SrcFid;
				OFid = o == null ? -1 : o.SrcFid;
				Shell = m.ShellNum;
				OShell = o == null ? -1 : o.ShellNum;
				SegNum = m.SegNum;
				OSegNum = o == null ? -1 : o.SegNum;
			}
		}
		
		private class SegmentAssembler {
			private Dictionary<Pair<Pair<int,int>,Pair<int,int>>, List<OverlapSegment>> m_parts;
			public SegmentAssembler() { m_parts = new Dictionary<Pair<Pair<int,int>,Pair<int,int>>, List<OverlapSegment>>(); }
			public void AddSegment(OverlapSegment o) {
				Pair<Pair<int,int>,Pair<int,int>> key = 
					new Pair<Pair<int,int>,Pair<int,int>>(new Pair<int,int>(o.Fid, o.Shell), 
					                                      new Pair<int,int>(o.OFid, o.OShell));
				if (!m_parts.ContainsKey(key))
					m_parts[key] = new List<OverlapSegment>();
				m_parts[key].Add(o);
			}
			
			public IEnumerable<Pair<Pair<int,int>,Pair<int,int>>> SegsAndShells { get {
					return m_parts.Keys;
				}
			}
			public int OverlapCount { get { return m_parts.Count; } }
			// IEnumerable< starting and ending SegNum, Connected-line-segments > for a given feature & shell
			public IEnumerable<Pair<Pair<int,int>,List<LineSeg>>> GetStrings(Pair<Pair<int,int>,Pair<int,int>> key) {
				List<OverlapSegment> vs = m_parts[key];

				// Sort by segment #.
				vs.Sort( delegate(OverlapSegment a, OverlapSegment b) {
				        	return a.SegNum.CompareTo(b.SegNum);
				        });
				
				LineSeg last_seg = null;
				int start_segnum = -1;
				int cur_segnum = -1;
				List<LineSeg> cur_seg = null;
				foreach (OverlapSegment s in vs) {
					// if there is a 'jump'!
					if (last_seg != null && s.SegNum - cur_segnum != 1) {
						Pair<int,int> range = new Pair<int,int>(start_segnum, cur_segnum);
						yield return new Pair<Pair<int,int>,List<LineSeg>>(range, cur_seg);
						last_seg = null;
					}
					
					if (last_seg == null) {
						last_seg = s.Seg;
						start_segnum = s.SegNum;
						cur_seg = new List<LineSeg>();
					}
					
					cur_segnum = s.SegNum;
					cur_seg.Add(s.Seg);
					
				}
				if (cur_seg != null && cur_seg.Count > 0) {
						Pair<int,int> range = new Pair<int,int>(start_segnum, cur_segnum);
						yield return new Pair<Pair<int,int>,List<LineSeg>>(range, cur_seg);
				}
			}
		}

	}
}
