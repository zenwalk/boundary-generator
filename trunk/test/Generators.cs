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

namespace Mwsw.Test {
  public delegate T Generator<T>();
  public delegate T MapF<V,T>(V val);

  public class Generators {
    private Random m_r;
    private int m_seed;

    public Generators(int randseed) {
      m_r = new Random(randseed);
      m_seed = randseed;
    }

    public Generators() {
      m_r = new Random();
      m_seed = m_r.Next();
      m_r = new Random(m_seed);
    }

    public static IEnumerable<T> Take<T>(int n, IEnumerable<T> xs) {
      int i = 0;
      foreach (T x in xs) {
	if (i >= n)
	  break;

	yield return x;
	i++;
      }
    }

    public static IEnumerable<T> Gen<T>(Generator<T> gen) {
      while(true)
	yield return gen();
    }

    public static IEnumerable<T> Map<V,T>(IEnumerable<V> vals, MapF<V,T> mapf) {
      foreach (V x in vals) {
	yield return mapf(x);
      }
    }

    public static IEnumerable<Pair<A,B>> Zip<A,B>(IEnumerable<A> a,
						  IEnumerable<B> b) {
      using (IEnumerator<A> avs = a.GetEnumerator()) {
	using (IEnumerator<B> bs = b.GetEnumerator()) {
	  bool hasa = avs.MoveNext();
	  bool hasb = bs.MoveNext();
	  while (hasa || hasb) {
	    Pair<A,B> res = new Pair<A,B>(hasa ? avs.Current : default(A),
					  hasb ? bs.Current : default(B));
	    yield return res;
	    hasa = hasa && avs.MoveNext();
	    hasb = hasb && bs.MoveNext();
	  }
	}
      }
    }

    public double Double() { return m_r.NextDouble(); }
    public IEnumerable<double> Doubles { get { return Gen<double>(Double); } }

    public bool Boolean() { return m_r.Next(1) == 1; }

    public Vector PointInside(Aabb bounds) {
      double x = Double() * bounds.W;
      double y = Double() * bounds.H;
      Vector v = new Vector(x,y);
      return v - bounds.Origin;
    }

    public Vector PointOutside(Aabb bounds) {
      // generate a point inside the bounds, then shift it outside.
      double x = Double() * bounds.W;
      double y = Double() * bounds.H;
      double vw = Boolean() ? bounds.W : -bounds.W;
      double vh = Boolean() ? bounds.H : -bounds.H;
      switch (m_r.Next(3)) {
      case 0 :
	x += vw;
	break;
      case 1 : 
	y += vh;
	break;
      case 2 :
	x += vw; y += vh;
	break;
      }
      Vector v = new Vector(x,y);
      return v - bounds.Origin;
    }
    
    public IEnumerable<Vector> PointsInside(Aabb bounds) {
      return Gen<Vector>(delegate() { return PointInside(bounds); } );
    }

    public Vector GenVector() {
      return new Vector(Double(),Double());
    }
    public IEnumerable<Vector> Vectors { get { return Gen<Vector>(GenVector); } }
  }
}