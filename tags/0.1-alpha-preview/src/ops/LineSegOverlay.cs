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

    public IEnumerable<Pair<LineSeg, IEnumerable<T> > > Segments {
      get {
	foreach (IIndexedSeg s in m_index.AllSegments) {
	  Fragment f = s.Val as Fragment;
	  yield return new Pair<LineSeg, IEnumerable<T> >(f.LineSeg, f.Sources);
	}
      }
    }

    private class Fragment : IHasLine {
      private LineSeg m_l;
      public LineSeg LineSeg { get { return m_l; } set { m_l = value; } }
      public SList<T> Sources; // All the Ts overlapping at this fragment.
      public Fragment Prev; // prev connected fragment
      public Fragment Next; // next connected fragment
      public void Split(out Fragment a, LineSeg aseg,
			out Fragment b, LineSeg bseg) {
	a = new Fragment(); a.m_l = aseg; a.Sources = this.Sources;
	b = new Fragment(); b.m_l = bseg; b.Sources = this.Sources;
	a.Prev = Prev; a.Next = b;
	b.Prev = a; a.Next = Next;
      }
      public override string ToString() {
	String r = "Frag(" + m_l ;
	foreach (T s in Sources)
	  r = r + "," + s;
	r += ")";
	return r;
      }
    }
    
    private bool insertFrag(T src, 
			    LineSeg l,
			    IIndexedSeg cand,
			    Stack<IIndexedSeg> cand_remainders,
			    Stack<LineSeg> l_remainders) {
      Fragment frag = cand.Val as Fragment;

      LineSeg a,b,o; bool l_before, l_after;
      LineSeg.Overlay(l, frag.LineSeg, m_atol, m_dtolsq,
		      out b, out l_before, // leftover before
		      out o,
		      out a, out l_after); // leftover after

      if (o != null) { // There was an overlay...
	// For partial overlays, manage any leftovers that are
	// already in the index.
	if (b != null && !l_before) {
            Fragment nfraga, nfragb;
            frag.Split(out nfraga, b, out nfragb, o);
            Pair<IIndexedSeg,IIndexedSeg> sp = 
              m_index.Split(cand, nfraga, nfragb);

            // relable 'c' and 'frag' to point at the overlaping part.
	    cand_remainders.Push(sp.First);
            cand = sp.Second;
            frag = nfragb; 
	}
          
	if (a != null && !l_after) {
	  Fragment nfraga, nfragb;
	  frag.Split(out nfraga, o, out nfragb, a);
	  Pair<IIndexedSeg,IIndexedSeg> sp = 
	    m_index.Split(cand, nfraga, nfragb);
	  
	  // relable 'c' and 'frag' to point at the overlaping part.
	  cand_remainders.Push(sp.Second);
	  cand = sp.First;
	  frag = nfraga;
	}

	// Attribute the overlapping portion w/ the source line.
	frag.Sources = frag.Sources.Cons(src);
	
	// And manage any leftovers that are -not- part of the index.
	if (b != null && l_before)
	  l_remainders.Push(b);
	if (a != null && l_after)
	  l_remainders.Push(a);
	
	return true;
      } else {
	// No overlap so leftovers are complete.
	cand_remainders.Push(cand);
	return false;
      }
    }
			      


    /// Insert the given line.
    /// 
    ///  TODO: FUTURE: Make the results of this independent of
    ///                insertion order... (tricky due to tolerance
    ///                issues; what if lines a and b are within the
    ///                tol-distance; ditto b and c, but a and c are
    ///                not -- order will matter!)
    public void Insert(T line) {
      //      Console.Error.WriteLine("Insert...." + line);

      Stack<LineSeg> working_set = new Stack<LineSeg>();
      working_set.Push(line.LineSeg);

	// Find all candidates...
      Stack<IIndexedSeg> candidates = new Stack<IIndexedSeg>();
      foreach (IIndexedSeg cand in 
	       m_index.SearchByLineSeg(line.LineSeg, m_dtol))
	candidates.Push(cand);

      // Match the candidates vs. the given line segment.
      Stack<LineSeg> unmatched = new Stack<LineSeg>();

      while (working_set.Count > 0) {
	LineSeg l = working_set.Pop();
	
	//Console.Error.WriteLine("Part :" + l);

	Stack<IIndexedSeg> ncands = new Stack<IIndexedSeg>();
	//	Console.Error.WriteLine("cands::" + candidates.Count);
	//	foreach (IIndexedSeg c in candidates)
	//  Console.Error.WriteLine("    " + c.Val);
	  
	bool intersected = false;

	while (!intersected && candidates.Count > 0) {
	  IIndexedSeg cand = candidates.Pop();
	  intersected = insertFrag(line,l,cand,ncands,working_set);
	  if (!intersected) {
	    ncands.Push(cand);
	  }
	}

	//	Console.Error.WriteLine("intr? " + intersected + ", working_set:" + working_set.Count);

	if (!intersected)
	  unmatched.Push(l);
	
	while (candidates.Count > 0)
	  ncands.Push(candidates.Pop());

	candidates = ncands;
      }

      // Any unmatched pieces of the line:
      foreach (LineSeg l in unmatched) {
	Fragment fresh = new Fragment();
	fresh.Prev = null; fresh.Next = null;
	fresh.Sources = SList<T>.Nil.Cons(line);
	fresh.LineSeg = l;
	m_index.Insert(fresh);
      }
    }
    
  }
}
