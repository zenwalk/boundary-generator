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

namespace Mwsw.Util {
  
  public class Pair<A,B> {
    private A m_a; private B m_b;
    public Pair(A a, B b) { m_a = a; m_b = b; }
    public A First { get { return m_a; } }
    public B Second { get { return m_b; } }

    public override int GetHashCode() { 
    	return (m_a != null ? m_a.GetHashCode() : 0) ^ (m_b != null ? m_b.GetHashCode() : 0);
    }
	public override bool Equals(object obj)	{
    	Pair<A,B> o = obj as Pair<A, B>;
    	if (o == null) return false;
    	return (m_a != null ? m_a.Equals(o.m_a) : o.m_a == null) &&
    		(m_b != null ? m_b.Equals(o.m_b) : o.m_b == null);
 	}
    
    public static IEnumerable<Pair<A,A>> Pairs(IEnumerable<A> vs) {
    	int aidx = 0;
    	int bidx = 0;
    	foreach (A fst in vs) {
    		foreach (A snd in vs) {
    			if (aidx != bidx) {
    				yield return new Pair<A,A>(fst,snd);
    			}
    			bidx++;
    		}
    		aidx++; bidx = 0;
    	}
    }
  }
}
