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
    
    /// The length.
    public double Length { get { return Math.Sqrt(LengthSq); } }

    /// A perpendicular: (easy in 2d)
    public Vector Perp { get { return new Vector(-m_y,m_x); } }

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

    /// A parallel check w/ a tolerance. Tolerance for a specific angle theta
    ///  is expressed as cos(theta)^2. Tolerances >= 90 degrees are 
    public static bool AreParallel(Vector a, Vector b, double costol) {
      double dp = Dot(a,b); // (a.Length * b.Length * cos(theta))
      double dpsq = dp * dp; // (a.LengthSq * b.LengthSq * cos(theta)^2)

      // Consider the first quadrant only.
      //  Cos(theta) strictly decreases for 0 <= theta <= pi/2.
      //  Thus if theta_a <= theta_b, cos(theta_a) >= cos(theta_b)
      //                            , cos(theta_a)^2 >= cos(theta_b)^2
      //                            , x * cos(theta_a)^2 >= x * cos(theta_b)^2
      //                               (for x > 0)
      return (dpsq >= costol * a.LengthSq * b.LengthSq);
    }

    /// Helper for AreParallel: generate a tolerance value for a given angle
    ///  in radians:
    public static double GetParTolerance(double theta) {
      double v = Math.Cos(theta);
      return v*v;
    }

    public static Vector operator -(Vector a, Vector b) {
      return new Vector(a.X - b.X,a.Y-b.Y);
    }

    public static Vector operator +(Vector a, Vector b) {
      return new Vector(a.X + b.X,a.Y+b.Y);
    }

    public static Vector operator * (Vector a, double b) {
      return new Vector(a.X * b, a.Y * b);
    }
    public static Vector operator * (double a, Vector b) { return b*a; }
    
  }

}