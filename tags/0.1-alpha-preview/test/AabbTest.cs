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
using NUnit.Framework;
using Mwsw.Geom;
using System.Collections.Generic;

namespace Mwsw.Test {

  [TestFixture()]
  public class AabbTest : Generators {

    [Test()]
    public void TestLineSegIntersect() {
      foreach (Aabb box in Take(100,Aabbs)) {
	checkBox(box);
	Aabb a,b,c,d;
	box.QuadBreak(out a, out b, out c, out d);
	checkBox(a); checkBox(b); checkBox(c); checkBox(d);

	
	Aabb[] subs = new Aabb[]{a,b,c,d};
	
	foreach (Aabb t in subs) {
	  Assert.IsTrue(box.Contains(t));
	  Assert.IsTrue(box.Intersects(t)); 
	  Assert.IsTrue(t.Intersects(box));
	  Assert.IsFalse(t.Contains(box));

	  Assert.IsTrue(Math.Abs((box.H * 0.5) - t.H) < 0.00001, "Not half height?");
	  Assert.IsTrue(Math.Abs((box.W * 0.5) - t.W) < 0.00001, "Not half width?");

	  foreach (Aabb tt in subs) {
	    if (tt == t) 
	      continue;
	    Assert.IsFalse(t.Contains(tt));
	    Assert.IsFalse(tt.Contains(t));
	    if (t.Intersects(tt)) {
	      Aabb x = Aabb.Intersect(t,tt);
	      Assert.IsTrue(x.W >= 0.0, "Neg width?");
	      Assert.IsTrue(x.H >= 0.0, "Neg height?");
	      Assert.IsTrue((x.W*x.H) < 0.000001, "non-tiny intersection?");
	    }
	  }
	}

	foreach (Aabb o in new Aabb[]{b,c,d}) 
	  Assert.IsFalse(a.Contains(o));

      }
    }

    private void checkBox(Aabb box) {
      IEnumerable<LineSeg> segs = Gen<LineSeg>(delegate() { return LineSeg.FromEndpoints( PointInside(box), PointOutside(box)); });
      
      foreach (LineSeg ls in Take(100,segs)) {
	Assert.IsTrue(box.Intersects(ls));
	
	LineSeg outline = new LineSeg(ls.End, ls.Dir);
	Assert.IsFalse(box.Intersects(outline));
	
	LineSeg swapped = LineSeg.FromEndpoints(ls.End,ls.Start);
	Assert.IsTrue(box.Intersects(swapped));
	
	// neither endpoint is inside but the line def. crosses
	LineSeg crossing = new LineSeg(ls.Start - ls.Dir, ls.Dir * 2.0);
	Assert.IsTrue(box.Intersects(crossing));	  
	
	// Shift it outside of the box by moving it sideways a min amt...
	double shift_length = Math.Sqrt(box.W * box.W + box.H * box.H);
	Vector shift = shift_length * crossing.Dir.Normalize().Perp;
	
	LineSeg out1 = new LineSeg(crossing.Start + shift,
				   crossing.Dir);
	Assert.IsFalse(box.Intersects(out1));
      }
    }
  }
}