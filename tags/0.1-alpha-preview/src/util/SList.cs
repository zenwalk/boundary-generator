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

using System.Collections.Generic;

namespace Mwsw.Util {

  /// A singly linked list.
  public class SList<T> : IEnumerable<T> { 
    private T m_v;
    private SList<T> m_nxt;    
    public SList(T val, SList<T> next) { m_v = val; m_nxt = next; }

    public static SList<T> Nil = new SList<T>(default(T), null);

    public IEnumerator<T> GetEnumerator() {
      SList<T> cur = this;
      while (cur != Nil) {
	yield return cur.m_v;
	cur = cur.m_nxt;
      }
    }

    public SList<T> Cons(T val) {
      return new SList<T>(val, this);
    }

    public T HeadVal { get { return m_v; } }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
      SList<T> cur = this;
      while (cur != Nil) {
	yield return cur.m_v;
	cur = cur.m_nxt;
      }
    }
  }

}