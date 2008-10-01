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

  
  /// Extend LineSegOverlayTest, but use the PMQTree instead
  [TestFixture()]
  public class PMQTreeTest : LineSegOverlayTest {
    protected override ISegIdx GetIndex(double dtol, double atol) {
      return new PMQTree(4, 6);
      //return new NullSegIdx(); 
    }

    protected override int TestCount { get { return 4096; } } 
    
    // Choose a density/length that won't crush the index with too many crossing
    //   segments: 
    protected override double LineSegLength { get { return (Double() * 100.0)/TestCount; } } 
    //protected override double LineSegLength { get { return 0.002; } } 
    
    protected override void DumpIndex(ISegIdx i) {
      if (i is PMQTree)
      	return;

      PMQTree tr = i as PMQTree;
      foreach(Pair<int,Aabb> p in tr.Dump()) {
	Console.Error.WriteLine("DUMP: node(" + p.First + "," + p.Second + ")");
      }
      foreach(IIndexedSeg l in tr.AllSegments) {
	Console.Error.WriteLine("DUMP: line(" + l.Val.LineSeg.ToString() + ")");
      }
    }
  }
}