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

namespace Mwsw.Test {

  [TestFixture()]
  public class LineSegTest : Generators {
    
    
    // Test direct overlaps w/in a distance by shifting them..
    [Test()]
    public void TestOverlapsShifted() {
      double partol = Vector.GetParTolerance(0.001 / 180.0 * Math.PI);

      foreach(LineSeg l in Take(1000,LineSegs)) {
	double dist = Double();
	double dist_sq = (dist * dist) * 1.00001;
	Vector shift = l.Dir.Perp.Normalize() * dist;
	LineSeg ll = new LineSeg(l.Start + shift,l.Dir);


	LineSeg o = null; LineSeg before = null; LineSeg after = null;
	bool ab, af;
	// First, with itself:
	LineSeg.Overlay(l,l, partol, dist_sq,
			out before, out ab, out o, out after, out af);

	Assert.IsNull(before);
	Assert.IsNull(after);
	Assert.IsNotNull(o);
	
	// same length modulo epsilon
	Assert.Less(o.Dir.LengthSq, l.Dir.LengthSq * 1.00001);


	// Now with the shifted version:
	LineSeg.Overlay(l,ll, partol, dist_sq,
			out before, out ab, out o, out after, out af);


	Assert.IsNull(before);
	Assert.IsNull(after);
	Assert.IsNotNull(o);
	Assert.Less(o.Dir.LengthSq, l.Dir.LengthSq * 1.00001);
	Assert.Greater(o.Dir.LengthSq, l.Dir.LengthSq * 0.9999);
      }
    }

    [Test()]
    public void TestRotated() {
      double max_rotang = 10.0 / 180.0 * Math.PI;
      double tol = Vector.GetParTolerance(max_rotang);
      foreach (LineSeg l in Take(1000,LineSegs)) {
	double ang = Double() * max_rotang;
	if (Boolean())
	  ang = -ang;

	LineSeg ll = new LineSeg(l.Start, l.Dir.Rotate(ang));
	
	// Come up w/ an appropriate distance threshold
	// (that ll will pass.)
	double threshdist = Math.Sin(ang) * l.Dir.Length;
	threshdist *= threshdist;

	LineSeg o = null; LineSeg before = null; LineSeg after = null;
	bool ab,af;
	LineSeg.Overlay(l,ll, threshdist, tol,
			out before, out ab, out o, out after, out af);

	Assert.IsNull(before);

	Assert.IsNotNull(o);

	if (after != null) {
	  Assert.Greater(after.Dir.LengthSq, threshdist);
	  // Measure the expected length...
	  double nlength = Math.Cos(ang) * o.Dir.Length;
	  Assert.Less(o.Dir.Length, nlength * 1.001);
	  Assert.Greater(o.Dir.Length, nlength * 0.9999);
	} else {
	  Assert.Less(o.Dir.LengthSq, l.Dir.LengthSq * 1.0001);
	}

	
	//Assert.Greater(o.Dir.LengthSq, nlength * 0.99);
      }
    }

    [Test()]
    public void TestOverlaps() {
      foreach (LineSeg l in Take(1000,LineSegs)) {
	// Test overlaps after:
	double t = Double() * 0.5 + 0.5;
	LineSeg ll = new LineSeg(l.Start + (l.Dir * t),
				 l.Dir);

	LineSeg o = null; LineSeg before = null; LineSeg after = null;
	bool ab,af;
	
	LineSeg.Overlay(l,ll, 0.0001, l.Dir.LengthSq * 0.000001, 
			out before, out ab, out o, out after, out af);
	
	Assert.IsNotNull(before);
	Assert.IsNotNull(o);
	Assert.IsNotNull(after);
	
	Assert.IsTrue(ab); // some of 'a' before
	Assert.IsFalse(af); // some of 'b' after.

	Assert.Less(before.Dir.Length + o.Dir.Length, l.Dir.Length + 0.0001);
	Assert.Less(o.Dir.Length + after.Dir.Length,  ll.Dir.Length + 0.0001);
	
	Assert.Less(o.Dir.Length, (1.0 - t) * l.Dir.Length + 0.0001);

	
      }
      
      foreach (LineSeg l in Take(1000,LineSegs)) {
	// Test overlaps before:
	double t = Double() * 0.5 + 0.5;
	LineSeg ll = new LineSeg(l.Start - (l.Dir * t),
				 l.Dir);
	
	LineSeg o = null; LineSeg before = null; LineSeg after = null;
	bool ab,af;
	
	LineSeg.Overlay(l,ll, 0.0001, l.Dir.LengthSq * 0.000001, 
			out before, out ab, out o, out after, out af);
	
	Assert.IsNotNull(before);
	Assert.IsNotNull(o);
	Assert.IsNotNull(after);
	
	Assert.IsTrue(af); // some of 'b' before
	Assert.IsFalse(ab); // some of 'a' after.

	Assert.Less(before.Dir.Length + o.Dir.Length, ll.Dir.Length + 0.0001);
	Assert.Less(o.Dir.Length + after.Dir.Length,  l.Dir.Length + 0.0001);
	Assert.Less(o.Dir.Length, (1.0 - t) * l.Dir.Length + 0.0001);

      }
    }

    [Test()]
    public void TestNonOverlaps() {
      foreach (LineSeg l in Take(1000,LineSegs)) {
	
	double t = Double(); // 0..1
	Vector dir = l.Dir;
	if (Boolean()) {
	  t *= -1;
	  dir = dir * -1.0;
	} else {
	  t += 1.001;
	}
	LineSeg ll = new LineSeg(l.Start + (l.Dir * t),
				 dir);
	LineSeg o = null; LineSeg before = null; LineSeg after = null;
	bool ab,af;
	
	LineSeg.Overlay(l,ll, 0.0001, l.Dir.LengthSq * 0.000001, 
			out before, out ab, out o, out after, out af);
	Assert.IsNull(o);
	Assert.IsNull(before);
	Assert.IsNull(after);
	
      }
    }
  }
}
