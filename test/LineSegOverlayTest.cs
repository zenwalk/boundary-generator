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


using NUnit.Framework;
using System;
using System.Collections.Generic;
using Mwsw.Geom;
using Mwsw.Ops;
using Mwsw.Spidx;
using Mwsw.Util;

namespace Mwsw.Test {

  /// This test inherits all the LineSeg tests,
  ///  but runs them under a spatial index.
  [TestFixture()]
  public class LineSegOverlayTest : Generators {// LineSegTest {

    protected ISegIdx GetIndex(double disttol,
				 double ang_tol) {
      return new NullSegIdx();
    }

    private class OverlayObj : IHasLine {
      private LineSeg m_seg;
      public bool FromA;
      public OverlayObj(LineSeg l, bool froma) { m_seg = l; FromA = froma; }
      public LineSeg LineSeg { get { return m_seg; } }
    }
    protected /*override*/ void OverlayFn(LineSeg a,
				      LineSeg b,
				      double ang_tol,
				      double disttol,
				      out LineSeg before,
				      out bool a_before,
				      out LineSeg overlap,
				      out LineSeg after,
				      out bool a_after) {
      before = null; 
      a_before = false; overlap = null; after = null; a_after = false;

      OverlayObj aobj = new OverlayObj(a,true);
      OverlayObj bobj = new OverlayObj(b,false);

      double dtol = Math.Sqrt(disttol);

      LineSegOverlay<OverlayObj> overlay =
	new LineSegOverlay<OverlayObj>(GetIndex(dtol,ang_tol),
				    ang_tol,dtol);
      overlay.Insert(aobj);
      overlay.Insert(bobj);
      
      List<Pair<LineSeg,OverlayObj>> nonoverlaps = new List<Pair<LineSeg,OverlayObj>>();

      bool have_overlay = false;

      foreach (Pair<LineSeg,IEnumerable<OverlayObj>> r in overlay.Segments) {
	int count = 0; bool fsta = false; 
	OverlayObj o = null;

	foreach (OverlayObj os in r.Second) {
	  o = os;
	  if (count > 0 && os.FromA != fsta) { // An overlay.
	    overlap = r.First;
	    have_overlay = true;
	  } else if (count == 0) {
	    fsta = os.FromA;
	  }
	  count++;
	}

	if (count == 1)
	  nonoverlaps.Add(new Pair<LineSeg,OverlayObj>(r.First,o));

      }
      
      if (have_overlay) {

	foreach (Pair<LineSeg,OverlayObj> ls in nonoverlaps) {
	  double start_tval, end_tval;
	  ls.First.PointDistanceSq(overlap.Start, out start_tval);
	  ls.First.PointDistanceSq(overlap.End, out end_tval);


	  bool dir = (start_tval - disttol <= 0.0); 

	  if (!dir) {
	    after = ls.First;
	    a_after = ls.Second.FromA;
	  } else {
	    before = ls.First;
	    a_before = ls.Second.FromA;
	  }
	}
      } else {
	overlap = null; before = null; after = null;
      }
    }

    private class NumLineSeg : IHasLine {
      private LineSeg m_seg;
      private int m_idx;
      public NumLineSeg(LineSeg seg,int idx) { m_seg = seg; m_idx = idx; }
      public LineSeg LineSeg { get { return m_seg; } }
      public int Idx { get { return m_idx; } }
      public override string ToString() { return "("  + m_idx + ")"; }
    }

    //    [Test()]
    public void TestUnrelated() {
      // Super strict...
      double ang_tol = System.Double.Epsilon;
      double dist_tol = System.Double.Epsilon;
      Dictionary<int,bool> seen = new Dictionary<int,bool>();

      LineSegOverlay<NumLineSeg> overlay =
	new LineSegOverlay<NumLineSeg>(GetIndex(dist_tol,ang_tol),
				       ang_tol,dist_tol);

      int idx = 0;
      foreach (LineSeg l in Take(1000,LineSegs)) {
	overlay.Insert(new NumLineSeg(l,idx++));
      }

      foreach (Pair<LineSeg,IEnumerable<NumLineSeg>> res in overlay.Segments) {
	bool here = false;
	foreach (NumLineSeg r in res.Second) {
	  Assert.IsFalse(seen.ContainsKey(r.Idx));
	  Assert.IsFalse(here);
	  seen[r.Idx] = true;
	  here = true;
	}
      }

      for (int i = 0; i < 1000; i++) 
	Assert.IsTrue(seen[i]);

    }

    [Test()]
    public void TestBeforeAndAfter() {
      int count = 500; // run 100x
      double ang_tol = 0.0001;
      double dist_tol = 0.000001;
      Dictionary<int, int> seen = new Dictionary<int,int>();

      LineSegOverlay<NumLineSeg> overlay =
	new LineSegOverlay<NumLineSeg>(GetIndex(dist_tol,ang_tol),
				       ang_tol,dist_tol);

      int idx = 0;
      LineSeg[] lines = new LineSeg[count];
      foreach (LineSeg ll in Take(count,LineSegs)) {
	LineSeg l = ll;
	if (idx == 0) {
	  l = LineSeg.FromEndpoints(new Vector(0,0),
				    new Vector(0,1));
	}
	lines[idx] = l;
	//	overlay.Insert(new NumLineSeg(l,idx));
	idx++;

      }

      for (int i = 0; i < lines.Length; i++) {
	double ta = -0.75; 
	double tb = 0.25;
	ta = 0.75;
	tb = 1.75;
	// The 'before' half:
	LineSeg bl = LineSeg.FromEndpoints(lines[i].Start + (ta * lines[i].Dir),
					   lines[i].Start + (tb * lines[i].Dir));

	// the 'after' half:
	ta = -0.75;
	tb = 0.25;
	LineSeg al = LineSeg.FromEndpoints(lines[i].Start + (ta * lines[i].Dir),
				   lines[i].Start + (tb * lines[i].Dir));

	NumLineSeg a = new NumLineSeg(al, count + i);
	NumLineSeg b = new NumLineSeg(bl, 2*count + i);
	NumLineSeg c = new NumLineSeg(lines[i], i);
	switch(Int(6)) {
	case 0:
	  overlay.Insert(a); overlay.Insert(b); overlay.Insert(c);
	  break;	  
	case 5:
	  overlay.Insert(a); overlay.Insert(c); overlay.Insert(b);
	  break;
	case 1:
	  overlay.Insert(b); overlay.Insert(a); overlay.Insert(c);
	  break;
	case 4:
	  overlay.Insert(b); overlay.Insert(c); overlay.Insert(a);
	  break;
	case 2:
	  overlay.Insert(c); overlay.Insert(b); overlay.Insert(a);
	  break;
	case 3:
	  overlay.Insert(c); overlay.Insert(a); overlay.Insert(b);
	  break;
	}

      }
      

      int rcount = 0;
      foreach (Pair<LineSeg,IEnumerable<NumLineSeg>> res in overlay.Segments) {
	//	Console.Error.WriteLine("Res-----");
	foreach (NumLineSeg r in res.Second) {
	  // Console.Error.WriteLine("  Portion: " + r.Idx);
	  if (!seen.ContainsKey(r.Idx))
	    seen[r.Idx] = 0;
	  seen[r.Idx]++;
	}
	rcount++;
      }

      Assert.AreEqual(5*count,rcount);

      for (int i = 0; i < 3*count; i++) {
	if (i < count) {
	  Assert.AreEqual(3, seen[i]);
	}
	if (i > count)
	  Assert.AreEqual(2, seen[i]);
      }
    }
  }
}