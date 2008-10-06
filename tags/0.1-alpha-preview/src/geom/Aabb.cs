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
  public class Aabb {
    private double m_xmin,m_xmax,m_ymin,m_ymax;
    
    public Aabb(double x0, double x1, double y0, double y1) {
      m_xmin = x0; m_xmax = x1; m_ymin = y0; m_ymax = y1;
      if (m_xmin > m_xmax) throw new Exception("bad Aabb; xmin > xmax");
      if (m_ymin > m_ymax) throw new Exception("bad Aabb; ymin > ymax");
      
    }

    private static bool intervalsOverlap(double a,double b,double min,double max) {
      double amin = Math.Min(a,b);
      double amax = Math.Max(a,b);
      return ( amax >= min &&
	       amin <= max );
    }

    public Vector Origin { get { return new Vector(m_xmin,m_ymin); } }
    public double W { get { return m_xmax-m_xmin; } } 
    public double H { get { return m_ymax-m_ymin; } } 

    public static Aabb ConstructForPoints(System.Collections.Generic.IEnumerable<Vector> pts) {
      double xmin = 0.0;
      double xmax = 0.0;
      double ymin = 0.0;
      double ymax = 0.0;
      bool first = true;

      foreach (Vector v in pts) {
	if (first) {
	  xmin = v.X; xmax = v.X; ymin = v.Y; ymax = v.Y; first = false;
	} else {
	  xmin = Math.Min(xmin,v.X);
	  xmax = Math.Max(xmax,v.X);
	  ymin = Math.Min(ymin,v.Y);
	  ymax = Math.Max(ymax,v.Y);
	}
      }
      return new Aabb(xmin,xmax,ymin,ymax);
    }

    /// Intersection test for other aabb's
    public bool Intersects(Aabb o) {
      return (intervalsOverlap(o.m_xmin, o.m_xmax, m_xmin, m_xmax) &&
	      intervalsOverlap(o.m_ymin, o.m_ymax, m_ymin, m_ymax));
    }

    public static Aabb Intersect(Aabb a, Aabb b) {
      if (a.Intersects(b))
	return new Aabb(Math.Max(a.m_xmin, b.m_xmin),
			Math.Min(a.m_xmax, b.m_xmax),
			Math.Max(a.m_ymin, b.m_ymin),
			Math.Min(a.m_ymax, b.m_ymax));
      else
	return null;
    }

    /// Intersection test for line segments..
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
      int side = dir.Side(ll);
      
      if (side == 0) // the point lies right on the line 
	return true;

      Vector lr = new Vector(m_xmax,m_ymin) - sp;
      
      if (side != dir.Side(lr))
	return true;
	
      Vector ul = new Vector(m_xmin,m_ymax) - sp;
      if (side != dir.Side(ul))
	return true;

      Vector ur = new Vector(m_xmax,m_ymax) - sp;
      if (side != dir.Side(ur))
	return true;

      // If we got here, all 4 corners lie on the same side of the line
      return false; 
    }
    
    public bool Contains(LineSeg ll) {
      Vector s = ll.Start; Vector e = ll.End;
      return (m_xmin <= s.X && s.X <= m_xmax &&
	      m_xmin <= e.X && e.X <= m_xmax &&
	      m_ymin <= s.Y && s.Y <= m_ymax &&
	      m_ymin <= e.Y && e.Y <= m_ymax);
    }

    public bool Contains(Aabb o) {
      return (m_xmin <= o.m_xmin && o.m_xmin <= m_xmax &&
	      m_xmin <= o.m_xmax && o.m_xmax <= m_xmax &&
	      m_ymin <= o.m_ymin && o.m_ymin <= m_ymax &&
	      m_ymin <= o.m_ymax && o.m_ymax <= m_ymax);
    }


    public Aabb Grown(double dist) {
      if (dist < 0.0)
	throw new Exception("negative growth?") ;

      return new Aabb(m_xmin - dist,
		      m_xmax + dist,
		      m_ymin - dist,
		      m_ymax + dist);
    }

    public void QuadBreak(out Aabb ll,
			  out Aabb lr,
			  out Aabb ul,
			  out Aabb ur) {
      double midx = ( m_xmin + m_xmax ) * 0.5;
      double midy = ( m_ymin + m_ymax ) * 0.5;

      ll = new Aabb(m_xmin,midx,m_ymin,midy);

      lr = new Aabb(midx,m_xmax,m_ymin,midy);

      ul = new Aabb(m_xmin,midx,midy,m_ymax);

      ur = new Aabb(midx,m_xmax,midy,m_ymax);
    }

    public Aabb Square {
      get {
	if (W > H)
	  return new Aabb(m_xmin, m_xmin + W,
			  m_ymin, m_ymin + W);
	else 
	  return new Aabb(m_xmin, m_xmin + H,
			  m_ymin, m_ymin + H);
      }
    }

    public int ConstructNeighborsTowards(int direction,
					 out Aabb ll,
					 out Aabb lr,
					 out Aabb ul,
					 out Aabb ur,
					 out Aabb tot) {
      double w = m_xmax - m_xmin;
      double h = m_ymax - m_ymin;
      /*      double dx_min = target.m_xmin - m_xmin;
      double dx_max = target.m_xmax - m_xmax;
      double dy_min = target.m_ymin - m_ymin;
      double dy_max = target.m_ymax - m_ymax;
      */
      switch(direction) {
      case 0: 	  
	tot = new Aabb(m_xmin, m_xmax + w,
		       m_ymin, m_ymax + h);
	tot.QuadBreak(out ll, out lr, out ul, out ur);
	ll = this;
	return 0;
      case 1:
	tot = new Aabb(m_xmin - w, m_xmax,
		       m_ymin, m_ymax + h);
	tot.QuadBreak(out ll, out lr, out ul, out ur);
	lr = this;
	return 1;
      case 2:
	tot = new Aabb(m_xmin, m_xmax + w,
		       m_ymin - h, m_ymax );
	tot.QuadBreak(out ll, out lr, out ul, out ur);
	ul = this;
	return 2;
      case 3:
	tot = new Aabb(m_xmin - w, m_xmax,
		       m_ymin - h, m_ymax);
	tot.QuadBreak(out ll, out lr, out ul, out ur);
	ur = this;
	return 3;
      }
      throw new Exception("Bad!");
      /*
      if (dx_min > dx_max) { // grow towards left
	if (dy_min > dy_max) { // grow down
	  tot = new Aabb(m_xmin - w, m_xmax,
			 m_ymin - h, m_ymax);
	  tot.QuadBreak(out ll, out lr, out ul, out ur);
	  ur = this;
	  return 3;
	} else { // grow up
	  tot = new Aabb(m_xmin - w, m_xmax,
			 m_ymin, m_ymax + h);
	  tot.QuadBreak(out ll, out lr, out ul, out ur);
	  lr = this;
	  return 1;
	}
      } else { // grow right
	if (dy_min > dy_max) { // grow down
	  tot = new Aabb(m_xmin, m_xmax + w,
			 m_ymin - h, m_ymax );
	  tot.QuadBreak(out ll, out lr, out ul, out ur);
	  ul = this;
	  return 2;
	} else { // grow up
	  tot = new Aabb(m_xmin, m_xmax + w,
			 m_ymin, m_ymax + h);
	  tot.QuadBreak(out ll, out lr, out ul, out ur);
	  ll = this;
	  return 0;
	}
	} 
      */
    }
    public override string ToString() {
      return "[" + m_xmin + "," + m_ymin + "," + m_xmax + "," + m_ymax + "]";
    }				  
  }
 
}