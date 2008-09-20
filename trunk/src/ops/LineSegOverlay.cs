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
using Mwsw.Spidx;
using Mwsw.Util;

namespace Mwsw.Ops {
  
  /// Finds overlapping portions from a collection of line segments.
  /// (Left-handed)...
  public class LineSegOverlay<T> where T : IHasLine {
    private double m_atol, m_dtol, m_dtolsq;
    private ISegIdx m_index;

    public LineSegOverlay(ISegIdx index, double ang_tol, double dist_tol) {
      m_index = index;
      m_atol = Vector.GetParTolerance(ang_tol);
      m_dtol = dist_tol;
      m_dtolsq = dist_tol * dist_tol;
    }


    private class Fragment : IHasLine {
      private LineSeg m_l;
      public LineSeg LineSeg { get { return m_l; } set { m_l = value; } }
      public SList<T> Sources; // All the Ts overlapping at this fragment.
      public Fragment Prev; // prev connected fragment
      public Fragment Next; // next connected fragment
      public void Split(out Fragment a, LineSeg aseg,
			out Fragment b, LineSeg bseg) {
	a = new Fragment(); a.LineSeg = aseg; a.Sources = Sources;
	b = new Fragment(); b.LineSeg = bseg; b.Sources = Sources;
	a.Prev = Prev; a.Next = b;
	b.Prev = a; a.Next = Next;
      }
    }
    
    /// Insert the given line.
    /// 
    ///  TODO: FUTURE: Make the results of this completely independent
    ///                of insertion order... (tricky due to tolerance
    ///                issues; what if lines a and b are within the
    ///                tol-distance; ditto b and c, but a and c are
    ///                not -- order will matter!)
    public void Insert(T line) {
      
      Stack<LineSeg> working_set = new Stack<LineSeg>();
      working_set.Push(line.LineSeg);

	// Find all candidates...
      Stack<IIndexedSeg> candidates = new Stack<IIndexedSeg>();
      foreach (IIndexedSeg cand in m_index.SearchByLineSeg(line.LineSeg, m_dtol))
	candidates.Push(cand);

      //NOT GOOD ENOUGH I AM HERE.
      while (working_set.Count > 0 && candidates.Count > 0) {
	
	// Pop
	LineSeg l = working_set.Pop();
	IIndexedSeg cand = candidates.Pop();
	Fragment frag = cand.Val as Fragment;
	
	LineSeg a,b,o; bool l_before, l_after;
	LineSeg.Overlay(frag.LineSeg, l, m_atol, m_dtolsq,
			out b, out l_before,
			out o,
			out a, out l_after);
	  
	if (o != null) { // Overlap
	  // There are some 'leftovers' which don't overlap and
	  //  are already present in the index.
	  // -- 
	  if (b != null && !l_before) {
	    Fragment nfraga, nfragb;
	    frag.Split(out nfraga, b, out nfragb, o);
	    Pair<IIndexedSeg,IIndexedSeg> sp = 
	      m_index.Split(cand, nfraga, nfragb);

	    // relable 'c' and 'frag' to point at the overlaping part.
	    cand = sp.Second;
	    frag = nfragb; 
	  }
	  
	  if (a != null && !l_after) {
	    Fragment nfraga, nfragb;
	    frag.Split(out nfraga, o, out nfragb, a);
	    Pair<IIndexedSeg,IIndexedSeg> sp = 
	      m_index.Split(cand, nfraga, nfragb);

	    // relable 'c' and 'frag' to point at the overlaping part.
	    cand = sp.First;
	    frag = nfraga;
	  }
	  // --
	  
	  // Add 'T' to the overlap's list of contributors
	  frag.Sources = frag.Sources.Cons(line);
	  
	  // Is there any of the line left to insert?
	  if (b != null & l_before)
	    working_set.Push(b);
	  if (a != null && l_after)
	    working_set.Push(a);
	}
      }

      // Any unmatched pieces of the line:
      foreach (LineSeg unmatched in working_set) {
	Fragment fresh = new Fragment();
	fresh.Prev = null; fresh.Next = null;
	fresh.Sources = SList<T>.Nil.Cons(line);
	fresh.LineSeg = unmatched;
	m_index.Insert(fresh);
      }
    }
    
  }
}
