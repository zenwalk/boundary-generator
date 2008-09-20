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
using System.Collections.Generic;
using Mwsw.Geom;
using Mwsw.Util;

namespace Mwsw.Spidx {
  public interface IHasLine {
    LineSeg LineSeg { get; }
  }

  public interface IIndexedSeg {
    IHasLine Val { get; }
  }

  /// A spatial index for line segment objects.
  public interface ISegIdx {
    
    /// Insert a new value and return a handle to it
    IIndexedSeg Insert(IHasLine val);

    /// In-place split an existing line segment into two parts.
    Pair<IIndexedSeg,IIndexedSeg> Split(IIndexedSeg tosplit, IHasLine a, IHasLine b);

    /// Return all segements that might lie w/in a given distance.
    IEnumerable<IIndexedSeg> SearchByLineSeg(LineSeg o, double dist);
  }
}