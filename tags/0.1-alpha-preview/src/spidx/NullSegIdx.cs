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

  /// A do-nothing index.
  public class NullSegIdx : ISegIdx {
    private LinkedList<IHasLine> m_segments;
    
    private class Handle : IIndexedSeg {
      public LinkedListNode<IHasLine> Node;
      public Handle(LinkedListNode<IHasLine> n) { Node = n; }
      public IHasLine Val { get { return Node.Value; } }
    }

    public NullSegIdx() { m_segments = new LinkedList<IHasLine>(); }
    public IIndexedSeg Insert(IHasLine v) { return new Handle(m_segments.AddFirst(v)); }

    public Pair<IIndexedSeg, IIndexedSeg> Split(IIndexedSeg tosplit,
						IHasLine a,
						IHasLine b) {

      Handle na = new Handle(m_segments.AddFirst(a));
      Handle nb = new Handle(m_segments.AddFirst(b));

      Handle h = tosplit as Handle;
      m_segments.Remove(h.Node);

      return new Pair<IIndexedSeg, IIndexedSeg>(na,nb);
    }


    public IEnumerable<IIndexedSeg> SearchByLineSeg(LineSeg o, double dist) { 
      LinkedListNode<IHasLine> cur = m_segments.First;
      while (cur != null) {
	yield return new Handle(cur);
	cur = cur.Next;
      }
    }

    public IEnumerable<IIndexedSeg> AllSegments { 
      get { 
	LinkedListNode<IHasLine> nd = m_segments.First;
	while (nd != null) {
	  yield return new Handle(nd);
	  nd = nd.Next;
	}
      } 
    }

  }
}
