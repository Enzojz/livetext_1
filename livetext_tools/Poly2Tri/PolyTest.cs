using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Poly2Tri
{
    public static class PolyTest
    {
        public static void ProcessLevel(Polygon poly, ref PolygonHierachy localRoot)
        {
            if (localRoot == null)
            {
                localRoot = new PolygonHierachy(poly);
                return;
            }

            // Check if source is the new root
            if (CheckIfInside(localRoot.Current.Points, poly.Points))
            {
                var nroot = new PolygonHierachy(poly);
                var tmp = localRoot;
                while (tmp != null)
                {
                    var cur = tmp;
                    tmp = tmp.Next;
                    cur.Next = null;
                    nroot.Childs.Add(cur);
                }

                localRoot = nroot;
                return;
            }

            // Check if source is not in the local root
            if (!CheckIfInside(poly.Points, localRoot.Current.Points))
            {
                ProcessLevel(poly, ref localRoot.Next);
                return;
            }

            // Now process the childs
            for (var i = 0; i < localRoot.Childs.Count; ++i)
            {
                if (!CheckIfInside(poly.Points, localRoot.Childs[i].Current.Points)) continue;

                // Process to the child level
                var childRoot = localRoot.Childs[i];
                ProcessLevel(poly, ref childRoot);
                localRoot.Childs[i] = childRoot;
                return;
            }

            // Else -> new child
            var newChildList = new List<PolygonHierachy>();
            var newPoly = new PolygonHierachy(poly);
            newChildList.Add(newPoly);
            for (var i = 0; i < localRoot.Childs.Count; ++i)
            {
                if (CheckIfInside(localRoot.Childs[i].Current.Points, poly.Points))
                {
                    newPoly.Childs.Add(localRoot.Childs[i]);
                }
                else
                {
                    newChildList.Add(localRoot.Childs[i]);
                }
            }

            localRoot.Childs = newChildList; //.Childs.Add(new PolygonHierachy(poly));
        }

        public class PolygonHierachy
        {
            public Polygon Current;
            public List<PolygonHierachy> Childs;
            public PolygonHierachy Next;

            public PolygonHierachy(Polygon current)
            {
                Current = current;
                Childs = new List<PolygonHierachy>();
                Next = null;
            }
        }

        /// <summary>
        /// Check if a point is in a polygon
        /// </summary>
        /// <param name="p">Point to check</param>
        /// <param name="poly">Container polygon candidate</param>
        /// <returns>true in, false out</returns>
        public static bool PointInPolygon(TriangulationPoint p, IList<TriangulationPoint> poly)
        {
            PolygonPoint p1, p2;
            var inside = false;
            var oldPoint = new PolygonPoint(poly[poly.Count - 1].X, poly[poly.Count - 1].Y);

            for (var i = 0; i < poly.Count; i++)
            {
                var newPoint = new PolygonPoint(poly[i].X, poly[i].Y);
                if (newPoint.X > oldPoint.X) { p1 = oldPoint; p2 = newPoint; }
                else { p1 = newPoint; p2 = oldPoint; }
                if ((newPoint.X < p.X) == (p.X <= oldPoint.X) && ((long)p.Y - (long)p1.Y) * (long)(p2.X - p1.X)
                     < ((long)p2.Y - (long)p1.Y) * (long)(p.X - p1.X))
                {
                    inside = !inside;
                }
                oldPoint = newPoint;
            }
            return inside;
        }

        public static void ProcessSetLevel(PolygonSet set, PolygonHierachy current)
        {
            while (current != null)
            {
                var poly = current.Current;
                foreach (var child in current.Childs)
                {
                    poly.AddHole(child.Current);
                    foreach (var grandchild in child.Childs) ProcessSetLevel(set, grandchild);
                }
                set.Add(poly);
                current = current.Next;
            }
        }

        /// <summary>
        /// Check if a polygon is inside another. 
        /// </summary>
        /// <param name="polygonToTest"></param>
        /// <param name="containingPolygon"></param>
        /// <returns>true if at least 60% of the points are inside</returns>
        public static bool CheckIfInside(
            IList<TriangulationPoint> polygonToTest,
            IList<TriangulationPoint> containingPolygon)
        {
            var t = 0;
            for (var i = 0; i < polygonToTest.Count; ++i)
            {
                if (PointInPolygon(polygonToTest[i], containingPolygon)) t++;
            }

            return ((float)t) >= (polygonToTest.Count * .6f) ? true : false;
        }

        public static PolygonSet CreateSetFromList(IEnumerable<Polygon> _source)
        {
            var source = _source.ToList();
            // First we need to reorganize the polygons
            var root = new PolygonHierachy(source[0]);

            for (var i = 1; i < source.Count; ++i)
            {
                ProcessLevel(source[i], ref root);
            }

            // Generate the set from the hierachy
            var set = new PolygonSet();
            ProcessSetLevel(set, root);

            return set;
        }
    }
}
