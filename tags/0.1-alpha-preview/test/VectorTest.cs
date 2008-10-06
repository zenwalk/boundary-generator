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
  public class VectorTest : Generators {

    [Test()]
    public void TestPerpDot() {
      double ninety = 90.0 / 180.0 * Math.PI;
      double perptol = Vector.GetParTolerance(0.001 / 180.0 * Math.PI);

      foreach (Vector v in Take(1000,Vectors)) {
	// These should be zero or very close to it.
	Assert.Less(Math.Abs(Vector.Dot(v,v.Perp)), 0.00001);
	Assert.Less(Math.Abs(Vector.Dot(v,v.Rotate(ninety))), 0.00001);

	// A vector dot itself ...
	Assert.Less(Vector.Dot(v,v), v.LengthSq + 0.000001);

	// A check that a vector and its perp are parallel too.
	Assert.IsTrue(Vector.AreParallel(v.Perp,v.Rotate(ninety), perptol));

      }
    }


    [Test()]
    public void TestParallel() {

      double maxang = 89.0 / 180.0 * Math.PI;

      foreach (Vector v in Take(1000, Vectors)) {
	double rotate_by = Double() * maxang;
	double scale_by = (Double() * 2.0) - 1.0;
	double tolerance = Double() * maxang;

	Vector testv = v.Rotate(rotate_by) * scale_by;

	double ta = Vector.GetParTolerance(rotate_by * 1.001);
	Assert.IsTrue(Vector.AreParallel(v,testv,ta));
	
	double tb = Vector.GetParTolerance(rotate_by * 0.999);
	Assert.IsFalse(Vector.AreParallel(v,testv,tb));

	double tc = Vector.GetParTolerance(tolerance);
	Assert.AreEqual(Vector.AreParallel(v,testv,tc), rotate_by < tolerance);

      }
    }

  }
  
}