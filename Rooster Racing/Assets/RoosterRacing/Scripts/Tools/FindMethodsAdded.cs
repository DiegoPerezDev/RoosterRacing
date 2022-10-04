using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*  INSTRUCTIONS:
 *  Last full check: V0.3
 *  Simple tools made by us for general unity management, like gameObject management, search tools, math and so.
 */
namespace MyTools
{
    /*  INSTRUCTIONS:
    *  Last full check: V0.3
    *  Other methods to find gameObjects that are not simplified by Unity.
    */
    public class FindMethodsAdded : ScriptableObject
    {
        /// <summary>
        /// Return all the GameObjects in Hierarchy of the same layer.
        /// </summary>
        public static GameObject[] FindGameObjectsInLayer(int layer)
        {
            var goArray = FindObjectsOfType(typeof(GameObject)) as GameObject[];
            var goList = new List<GameObject>();
            for (int i = 0; i < goArray.Length; i++)
            {
                if (goArray[i].layer == layer)
                    goList.Add(goArray[i]);
            }
            if (goList.Count == 0)
                return null;
            return goList.ToArray();
        }

        /// <summary>
        /// Get the first GameObject of an array of GameObjects that has no parent.
        /// This could be usefull e.g. to get the parent GameObject among all of the gameobject with the same tag or in the same layer.
        /// </summary>
        public static GameObject GetNonChildGameObject(GameObject[] objects)
        {
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i].transform.parent == null)
                    return objects[i];
            }
            return null;
        }
    }


    /*  INSTRUCTIONS:
    *  Last full check: V0.3
    *  Other methods for math operations that we didn't find on other math library.
    */
    public class MyMath
    {
        /// <summary>
        /// Map variable value between its own min and max values and a target min and max values.
        /// </summary>
        /// <param name="x">Variable to map.</param>
        /// <param name="in_min">Own variable minimum value</param>
        /// <param name="in_max">Own variable maximum value</param>
        /// <param name="out_min">targer minimum value</param>
        /// <param name="out_max">Target maximum value</param>
        /// <returns></returns>
        public static float Map(float x, float in_min, float in_max, float out_min, float out_max)
        {
            return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        }

        /// <summary>
        /// Get the closest point of a target among a segment of points given.
        /// </summary>
        /// <param name="targetPoint">Target position to compare between the segment of point. The reference point.</param>
        /// <param name="pointsToCompare">Segment of point positions.</param>
        /// <returns>Return the point of the segment of points that is closest to the target.</returns>
        public static Vector3 CalculateClosestPoint(Vector3 targetPoint, Vector3[] pointsToCompare)
        {
            float minSqrDst = float.MaxValue;
            int pointA = 0;
            int pointB = 0;

            for (int i = 0; i < pointsToCompare.Length - 1; i++)
            {
                Vector3 closestPointOnSegment = ClosestPointOnLineSegment(targetPoint, pointsToCompare[i], pointsToCompare[i + 1]);
                float sqrDst = (targetPoint - closestPointOnSegment).sqrMagnitude;
                if (sqrDst < minSqrDst)
                {
                    minSqrDst = sqrDst;
                    pointA = i;
                    pointB = i + 1;
                }
            }

            var dist1 = Vector3.Distance(pointsToCompare[pointA], targetPoint);
            var dist2 = Vector3.Distance(pointsToCompare[pointB], targetPoint);
            if (dist1 <= dist2)
            {
                //print($"Checkpoint #{pointA} at pos: {pointsToCompare[pointA]}");
                return pointsToCompare[pointA];
            }
            else
            {
                //print($"Checkpoint #{pointB} at pos: {pointsToCompare[pointB]}");
                return pointsToCompare[pointB];
            }
        }

        /// <summary>
        /// Get the closest point of a target among a segment of points given.
        /// </summary>
        /// <param name="targetPoint">Target position to compare between the segment of point. The reference point.</param>
        /// <param name="pointsToCompare">Segment of point positions.</param>
        /// <returns>Return the point of the segment that is closest to the target.</returns>
        public static Vector3 CalculateClosestPoint(Vector3 targetPoint, List<Vector3> pointsToCompare)
        {
            Vector3[] pointsToCompareOnArray = new Vector3[pointsToCompare.Count];
            for (int i = 0; i < pointsToCompareOnArray.Length; i++)
                pointsToCompareOnArray[i] = pointsToCompare[i];
            return CalculateClosestPoint(targetPoint, pointsToCompareOnArray);
        }

        /// <summary>
        /// Get the closes point of a target in a line between two points.
        /// </summary>
        /// <param name="target">target to compate, the referense point</param>
        /// <returns>Return any point of the segment that is closest to the target point.</returns>
        public static Vector3 ClosestPointOnLineSegment(Vector3 target, Vector3 a, Vector3 b)
        {
            Vector3 aB = b - a;
            Vector3 aP = target - a;
            float sqrLenAB = aB.sqrMagnitude;

            if (sqrLenAB == 0)
                return a;

            float t = Mathf.Clamp01(Vector3.Dot(aP, aB) / sqrLenAB);
            return a + aB * t;
        }
    }

}