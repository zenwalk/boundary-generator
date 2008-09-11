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

namespace Mwsw.Geom {
  

  /**
   * A line segment.
   */
  public class LineSeg {
    private Vector m_v;
    private Vector m_start;

    public LineSeg(double x0, double y0, double x1, double y1) {
      m_v = new Vector(x1-x0, y1-y0);
      m_start = new Vector(x0,y0);
    }

    /* 
       public static IEnumerable<LineSeg> Overlay<T>(LineSeg a, T oa,
						  LineSeg b, T ob,
						  double angle_tolerance, 
						  double sep_dist_squared) {
      return null; // TODO: I am here.
    }
    */

  }
}
