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

  /// Axis-aligned bounding box.
  public struct Aabb {
    private double m_xmin,m_xmax,m_ymin,m_ymax;
    
    public Aabb(double x0, double x1, double y0, double y1) {
      m_xmin = x0; m_xmax = x1; m_ymin = y0; m_ymax = y1;
    }

    private static bool intervalsOverlap(double a,double b,double min,double max) {
      double amin = Math.Min(a,b);
      double amax = Math.Max(a,b);
      return ( amax >= min &&
	       amin <= max );
    }

    /// Intersection test
    public bool Intersects(LineSeg l) {
      // Seperating axis method.
      Vector sp = l.Start;
      Vector ep = l.End;
      Vector dir = l.Dir;
      
      // Projection onto the x & y axis & check for overlap
      if (!intervalsOverlap(sp.X,ep.X,m_xmin,m_xmax))
	return false;
      if (!intervalsOverlap(sp.Y,ep.Y,m_ymin,m_ymax))
	return false;

      // Test line-sidedness 
      Vector ll = new Vector(m_xmin,m_ymin) - sp;
      int sign = Math.Sign(Vector.Dot(dir,ll));
      
      if (sign == 0) // irritating edge case: ll is directly perp to the line direction. Correct via perturbation?
	throw new Exception("TODO: Edge case box-corner-perp-to-line.");

      Vector lr = new Vector(m_xmax,m_ymin) - sp;
      if (sign != Math.Sign(Vector.Dot(dir,lr))) // side change
	return true;
	
      Vector ul = new Vector(m_xmin,m_ymax) - sp;
      if (sign != Math.Sign(Vector.Dot(dir,ul))) 
	return true;

      Vector ur = new Vector(m_xmax,m_ymax) - sp;
      if (sign != Math.Sign(Vector.Dot(dir,ur))) 
	return true;

      // If we got here, all 4 corners lie on the same side of the line
      return false; 
    }

    public void QuadBreak(out Aabb ll,
			  out Aabb lr,
			  out Aabb ul,
			  out Aabb ur) {
      double midx = ( m_xmin + m_xmax ) / 2.0;
      double midy = ( m_ymin + m_ymax ) / 2.0;
      ll = new Aabb(m_xmin,midx,m_ymin,midy);
      lr = new Aabb(midx,m_xmax,m_ymin,midy);
      ul = new Aabb(m_xmin,midx,midy,m_ymax);
      ur = new Aabb(midx,m_xmax,midy,m_ymax);
    }
  }
 
}