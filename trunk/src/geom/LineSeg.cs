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

    public LineSeg(Vector st, Vector dir) { m_v = dir; m_start = st; }

    public static LineSeg FromEndpoints(Vector st, Vector nd) {
      return new LineSeg(st, nd-st);
    }

    public Vector Start { get { return m_start; } }
    public Vector Dir { get { return m_v; } }
    public Vector End { get { return m_start + m_v; } }


    /// Return the distance^2 from the line to a point,
    /// and the t value such that m_start + t * m_v = closes point on the line.
    public double PointDistanceSq(Vector coord,
				out double tval) {
      // See the derivation here:
      //   http://geometryalgorithms.com/Archive/algorithm_0102/algorithm_0102.htm#Distance%20to%20Parametric%20Line

      Vector dp = coord - m_start;
      tval = Vector.Dot(dp,m_v) / Vector.Dot(m_v,m_v);
      
      Vector pt = m_start + (tval * m_v);
      return (coord - pt).LengthSq;
    }

    /// Convenience wrapper if you don't care about the t value.
    public double PointDistanceSq(Vector c) {
      double ignored;
      return PointDistanceSq(c, out ignored);
    }

    /// Overlay two lines with given tolerances.
    /// (see Vector.GetParTolerance for the parallel tolerance; the
    ///  distance tolerance should be a distance^2.)
    /// Outputs are: the overlap itself,
    ///  any leftover bits prior to the overlap,
    ///  any leftover bits afterwards,
    ///  and two booleans indicating which lineseg 'owns' the leftovers.
    public static void Overlay(LineSeg a,
			       LineSeg b,
			       double par_tolerance, 
			       double sep_dist_squared,
			       out LineSeg prior,
			       out bool a_is_prior,
			       out LineSeg overlap,
			       out LineSeg after,
			       out bool a_is_after) {
      prior = null; overlap = null; after = null; 
      a_is_prior = false; a_is_after = false;

      // If they aren't parallel, we're done.
      if (!Vector.AreParallel(a.Dir,b.Dir, par_tolerance))
	return;

      double a_tval; // the t value for line a where the overlap with b may start
      double distsq = a.PointDistanceSq(b.Start, out a_tval);
      if (distsq > sep_dist_squared) // too far away
	return;

      double a_nd_tval; // t value where the end of b overlaps line a
      distsq = a.PointDistanceSq(b.End, out a_nd_tval);
      if (distsq > sep_dist_squared) // too far away
	return;
      
      // The lines themselves are overlapping but the line
      //  segments might not be, in which case neither of b's endpoints
      //   will generate a tval in the [0,1] interval.

      double st_t = Math.Min(a_tval, a_nd_tval); // Order things so a
      double nd_t = Math.Max(a_tval, a_nd_tval); // line from st_t to
						 // nd_t will be
						 // parallel to A.

      // If the overlap is completely outside of a, done:
      if (st_t > 1.0) 
	return;
      if (nd_t < 0.0)
	return;

      // There's an overlap.
      // Threshold a_tval and a_nd_tval into [0,1] for the overlapping portion.
      double clamp_st_t = Math.Max(st_t, 0.0);
      double clamp_nd_t = Math.Min(nd_t, 1.0);

      overlap = new LineSeg(a.Start + (clamp_st_t * a.Dir),
			    ((clamp_nd_t - clamp_st_t) * a.Dir));

      // Don't return overlaps shorter than the error distance...
      if (overlap.Dir.LengthSq < sep_dist_squared) {
	overlap = null;
	return;
      }

      // Find any leftover, non-overlapping portions of a and b:

      if (st_t < 0.0) { // some of line b is before line a's start
	prior = FromEndpoints(a.Start + (st_t * a.Dir), a.Start);
	a_is_prior = false;
      } else if (st_t > 0.0) { // some of line a is left over
	prior = FromEndpoints(a.Start, a.Start + (st_t * a.Dir));
	a_is_prior = true;
      }
      
      if (nd_t > 1.0) { // Some of 'b' is after a's end:
	after = FromEndpoints(a.End, a.Start + (nd_t * a.Dir));
	a_is_after = false;
      } else if (nd_t < 1.0) { // some of 'a' leftover.
	after = FromEndpoints(a.Start + (nd_t * a.Dir), a.End);
	a_is_after = true;
      }

      // Null out the leftovers if they aren't larger than the tolerance:
      if (prior != null && prior.Dir.LengthSq < sep_dist_squared)
	prior = null;
      if (after != null && after.Dir.LengthSq < sep_dist_squared)
	after = null;
    }
    
    public override string ToString() {
      return "[" + Start + "->" + End + "]";
    }

  }
}
