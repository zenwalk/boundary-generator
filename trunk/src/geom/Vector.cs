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
   * A vector -- represents a magnitude (length) and a direction.
   */
  public struct Vector {
    private double m_x, m_y;
    public double X { get { return m_x; } }
    public double Y { get { return m_y; } }

    public Vector(double x, double y) { m_x = x; m_y = y; }

    /// The dot product of two vectors.
    public static double Dot(Vector a, Vector b) {
      return (a.X * b.X) + (a.Y * b.Y);
    }

    /// The length, squared.
    public double LengthSq { get { return Dot(this,this); } }
    
    // The length
    public double Length { get { return Math.Sqrt(LengthSq); } }

    /// Normalize to length one:
    public Vector Normalized() { 
      if (m_x == 0.0 && m_y == 0.0) // Avoid a divide-by-zero.
	return this;

      double len = Length;
      return new Vector(m_x/len,m_y/len);
    }

    /// Get the angle of the vector (in radians)
    public double Angle { get { return Math.Atan2(m_y,m_x); } } 

    /// Get the angle between two vectors (in radians)
    public static double AngleBetween(Vector a, Vector b) {
      double num = Dot(a,b);
      double den = a.Length * b.Length;
      return Math.Acos(num/den);
    }

    /// Test whether two vectors are parallel within a specified
    ///  tolerance ( radians).
    public static bool AreParallel(Vector a, Vector b, double tolerance) {
      return AngleBetween(a,b) <= tolerance;
    }
  }

}