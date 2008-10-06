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

  /// A PM(R) quadtree.
  ///  Each leaf contains a list of line segments that intersect it; 
  ///   leaves are subdivided at a threshold (but only once).
  /// Has some degeneracies: lots of lines in close proximity will lead
  ///  to a leaf node with far more than the threshold # of entries.
  /// See: http://www.cs.umd.edu/~hjs/pubs/NelsSIGG86.pdf
  /// (no deleting/merging logic here, yet)
  public class PMQTree : ISegIdx {
    private int m_split_thresh;
    private int m_join_thresh;

    private int m_seg_keyseq = 0;
    private int segKey() { return (m_seg_keyseq++); }

    private RootNode m_root;
    private Dictionary<int, LSeg> m_allsegs;

    #region Private classes for storing nodes & segments

    private class LSeg : IIndexedSeg {
      private int m_keyval;
      private PMQTree m_o;
      private LinkedList< LNode > m_nodes; // leaf nodes that contain this seg
      private IHasLine m_v;

      public LSeg(PMQTree o, IHasLine v) { 
	m_nodes = new LinkedList<LNode>();
	m_v = v;
	m_o = o;
	m_keyval = m_o.segKey();
      }

      public int Keyval { get { return m_keyval; } }
      public IHasLine Val { get { return m_v; } }

      public Pair<LSeg,LSeg> DoSplit(IHasLine a, IHasLine b) {

	LSeg ar = new LSeg(m_o,a);
	LSeg br = new LSeg(m_o,b);
	List<LNode> thenodes = new List<LNode>(m_nodes);

	foreach (LNode n in thenodes) {
	  n.RemoveLSeg(this);
	  if (n.Intersects(a.LineSeg)) { n.AddSeg(ar); }
	  if (n.Intersects(b.LineSeg)) { n.AddSeg(br); }
	  //n.CheckSplit();
	}

	return new Pair<LSeg,LSeg>(ar,br);
      }

      public void ClearNode(LinkedListNode<LNode> r) { 
	m_nodes.Remove(r); 
      }
      public LinkedListNode<LNode> AddLeaf(LNode l) { return m_nodes.AddFirst(l); }
    }

    private abstract class Node { // node.
      protected PMQTree m_o;    // owner qtree
      protected Aabb m_bound;
      public Node(PMQTree o, Aabb b) { m_o = o; m_bound = b; }
      
      public bool Intersects(LineSeg l, double dist) {
	return m_bound.Grown(dist).Intersects(l); 
      }
      public bool Intersects(LineSeg l) { return m_bound.Intersects(l); }
      public bool Contains(LineSeg l) { return m_bound.Contains(l); }
      public Aabb Bounds { get { return m_bound; } }
      public abstract void Search(LineSeg s, double dist, Dictionary<int,LSeg> res);      
      public abstract Node Insert(LSeg l);
      public virtual IEnumerable<Node> Children { get { yield break; } }
    }

    private class RootNode : Node {
      private Node m_s;
      public RootNode(PMQTree o) : base(o, new Aabb(0,0,0,0)) {
	m_s = null;
      }
      public override void Search(LineSeg s, double dist, Dictionary<int, LSeg> res) {
	if (m_s != null)
	  m_s.Search(s,dist,res);
      }

      public override IEnumerable<Node> Children { get {yield return m_s; } }

      public override Node Insert(LSeg l) {
	if (m_s == null) {
	  m_s = new LNode(m_o, l.Val.LineSeg.BBox.Square.Grown(l.Val.LineSeg.BBox.W * 0.01) ); // grow 1%
	} else {
	  int dir = 0; // Sloppy: grow m_s (in rotating directions)
		       // until it contains the segment.

	  while (!m_s.Contains(l.Val.LineSeg)) {
	    Aabb bigger, vll, vlr, vul, vur;
	    Aabb sbounds = m_s.Bounds;
	    int c = sbounds.ConstructNeighborsTowards(dir % 4, 
					      out vll,out vlr,
					      out vul, out vur,
					      out bigger); 
	    Node nll = (c == 0) ? m_s : new LNode(m_o,vll);
	    Node nlr = (c == 1) ? m_s : new LNode(m_o,vlr);
	    Node nul = (c == 2) ? m_s : new LNode(m_o,vul);
	    Node nur = (c == 3) ? m_s : new LNode(m_o,vur);

	    dir++;
	    
	    if (nll == null || nlr == null || nul == null || nur == null)
	      throw new Exception("Hmmm here 1");

	    if (dir > 100)
	      throw new Exception("Issues fitting " + l.Val.LineSeg + " inside " + m_s.Bounds);

	    m_s = new QNode(m_o, bigger, nll, nlr, nul, nur);
	  }
	  
	}
	m_s = m_s.Insert(l);
	return this;
      }
    }

    private class QNode : Node { // branch node
      private Node ll,lr,ul,ur;
      public QNode(PMQTree o, Aabb b, Node vll, Node vlr, Node vul, Node vur) : base(o,b) {
	ll = vll; lr = vlr; ul = vul; ur = vur;
      }

      public override IEnumerable<Node> Children { 
	get {
	  yield return ll; yield return lr; yield return ul; yield return ur;
	}
      }
      public override void Search(LineSeg s, double dist, Dictionary<int,LSeg> res) {
	foreach (Node n in Children) {
	  if (n.Intersects(s,dist)) {
	    n.Search(s,dist,res);
	  }
	}
      }

      public override Node Insert(LSeg l) {
	if (ll.Intersects(l.Val.LineSeg))
	  ll = ll.Insert(l);
	if (lr.Intersects(l.Val.LineSeg))
	  lr = lr.Insert(l);
	if (ul.Intersects(l.Val.LineSeg))
	  ul = ul.Insert(l);
	if (ur.Intersects(l.Val.LineSeg))
	  ur = ur.Insert(l);

	return this; 
      }
    }

    private class LNode : Node { // leaf node
      private Dictionary<int,Pair<LSeg, LinkedListNode<LNode>>> m_segs;

      public LNode(PMQTree o, Aabb bound) : base(o,bound) { 
	m_segs = new Dictionary<int,Pair<LSeg, LinkedListNode<LNode>>>(); 
      }

      public void RemoveLSeg(LSeg l) { m_segs.Remove(l.Keyval); }

      public override Node Insert(LSeg l) {
	AddSeg(l);
	return CheckSplit();
      }

      public override void Search(LineSeg s, double dist, Dictionary<int,LSeg> r) {
	foreach (Pair<LSeg,LinkedListNode<LNode>> l in m_segs.Values)
	  r[l.First.Keyval] = l.First;
      }

      public void AddSeg(LSeg l) {
	m_segs[l.Keyval] = 
	  new Pair<LSeg,LinkedListNode<LNode>>(l,l.AddLeaf(this));
      }

      // Perform splits.
      public Node CheckSplit() {
	if (m_segs.Count > m_o.m_split_thresh) {
	  Aabb ll,lr,ul,ur;
	  m_bound.QuadBreak(out ll, out lr, out ul, out ur);
	  LNode nll = new LNode(m_o, ll);
	  LNode nlr = new LNode(m_o, lr);
	  LNode nul = new LNode(m_o, ul);
	  LNode nur = new LNode(m_o, ur);
	  foreach (Pair<LSeg,LinkedListNode<LNode>> segvs in m_segs.Values) {
	    LSeg l = segvs.First;

	    // remove backreferences from the segment to this.
	    l.ClearNode(segvs.Second);
	    
	    int ic = 0;
	    foreach (LNode n in new LNode[]{nll,nlr,nul,nur}) {
	      if (n.m_bound.Intersects(l.Val.LineSeg)) {
		n.AddSeg(l);
		ic++;
	      }
	    }
	    
	  }
	  m_segs.Clear();
	  m_segs = null; // poison this node... exceptions on access now!
	  
	  // Create a new 'parent' node
	  QNode qn = new QNode(m_o, m_bound, nll,nlr,nul,nur);
	  return qn;
	} else {
	  return this;
	}
      }
    }

    #endregion

    public PMQTree(int spthresh,
		   int mgthresh) {
      m_split_thresh = spthresh;
      m_join_thresh = mgthresh; 
      m_join_thresh = m_join_thresh + 0; // warning supression
      m_root = new RootNode(this);
      m_allsegs = new Dictionary<int,LSeg>();
    }

    #region ISegIdx implementation

    /// Insert a new value and return a handle to it
    public IIndexedSeg Insert(IHasLine val) {
      if (val == null || val.LineSeg == null) {
	throw new Exception("WTFERY,it's null!!");
      }
      LSeg r = new LSeg(this,val);
      m_allsegs[r.Keyval] = r;
      m_root.Insert(r);
      return r;
    }

    /// In-place split an existing line segment into two parts.
    public Pair<IIndexedSeg,IIndexedSeg> Split(IIndexedSeg tosplit, 
					IHasLine a, 
					IHasLine b) {
      LSeg l = tosplit as LSeg;
      Pair<LSeg,LSeg> r = l.DoSplit(a,b);
      m_allsegs[r.First.Keyval] = r.First;
      m_allsegs[r.Second.Keyval] = r.Second;
      m_allsegs.Remove(l.Keyval);
      
      return new Pair<IIndexedSeg,IIndexedSeg>(r.First,r.Second);
    }

    /// Return all segements that might lie w/in a given distance.
    public IEnumerable<IIndexedSeg> SearchByLineSeg(LineSeg o, double dist) {
      Dictionary<int,LSeg> r = new Dictionary<int,LSeg>();
      m_root.Search(o,dist,r);

      foreach (LSeg l in r.Values)
	yield return l;
    }

    /// Return all segements 
    public IEnumerable<IIndexedSeg> AllSegments { 
      get {
	List<LSeg> r = new List<LSeg>(m_allsegs.Values);
	foreach (LSeg l in r)
	  yield return l;
      }
    }

    #endregion

    public IEnumerable<Pair<int, Aabb> > Dump() {
      List<Pair<int,Aabb> > rs = new List<Pair<int,Aabb>>();
      dumpGather(rs,m_root,0);
      return rs;
    }
    private void dumpGather(List<Pair<int,Aabb> > o, Node n, int depth) {
      o.Add(new Pair<int,Aabb>(depth,n.Bounds));
      foreach (Node c in n.Children) {
	dumpGather(o,c,depth+1);
      }
    }
  }

}