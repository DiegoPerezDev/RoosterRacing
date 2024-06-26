﻿using System.Collections.Generic;
using PathCreation.Utility;
using UnityEngine;
using System;


namespace PathCreation {
    /// A vertex path is a collection of points (vertices) that lie along a bezier path.
    /// This allows one to do things like move at a constant speed along the path,
    /// which is not possible with a bezier path directly due to how they're constructed mathematically.

    /// This class also provides methods for getting the position along the path at a certain distance or time
    /// (where time = 0 is the start of the path, and time = 1 is the end of the path).
    /// Other info about the path (tangents, normals, rotation) can also be retrieved in this manner.

    public class VertexPath {
        #region Fields

        public readonly PathSpace space;
        public readonly bool isClosedLoop;
        public readonly Vector3[] localPoints;
        public readonly Vector3[] localTangents;
        public readonly Vector3[] localNormals;

        /// Percentage along the path at each vertex (0 being start of path, and 1 being the end)
        public readonly float[] times;
        /// Total distance between the vertices of the polyline
        public readonly float length;
        /// Total distance from the first vertex up to each vertex in the polyline
        public readonly float[] cumulativeLengthAtEachVertex;
        /// Bounding box of the path
        public readonly Bounds bounds;
        /// Equal to (0,0,-1) for 2D paths, and (0,1,0) for XZ paths
        public readonly Vector3 up;

        // Default values and constants:    
        const int accuracy = 10; // A scalar for how many times bezier path is divided when determining vertex positions
        const float minVertexSpacing = .01f;

        Transform transform;

        #endregion

        #region Constructors

        /// <summary> Splits bezier path into array of vertices along the path.</summary>
        ///<param name="maxAngleError">How much can the angle of the path change before a vertex is added. This allows fewer vertices to be generated in straighter sections.</param>
        ///<param name="minVertexDst">Vertices won't be added closer together than this distance, regardless of angle error.</param>
        public VertexPath (BezierPath bezierPath, Transform transform, float maxAngleError = 0.3f, float minVertexDst = 0):
            this (bezierPath, VertexPathUtility.SplitBezierPathByAngleError (bezierPath, maxAngleError, minVertexDst, VertexPath.accuracy), transform) { }

        /// <summary> Splits bezier path into array of vertices along the path.</summary>
        ///<param name="maxAngleError">How much can the angle of the path change before a vertex is added. This allows fewer vertices to be generated in straighter sections.</param>
        ///<param name="minVertexDst">Vertices won't be added closer together than this distance, regardless of angle error.</param>
        ///<param name="accuracy">Higher value means the change in angle is checked more frequently.</param>
        public VertexPath (BezierPath bezierPath, Transform transform, float vertexSpacing):
            this (bezierPath, VertexPathUtility.SplitBezierPathEvenly (bezierPath, Mathf.Max (vertexSpacing, minVertexSpacing), VertexPath.accuracy), transform) { }

        /// Internal contructor
        VertexPath (BezierPath bezierPath, VertexPathUtility.PathSplitData pathSplitData, Transform transform) {
            this.transform = transform;
            space = bezierPath.Space;
            isClosedLoop = bezierPath.IsClosed;
            int numVerts = pathSplitData.vertices.Count;
            length = pathSplitData.cumulativeLength[numVerts - 1];

            localPoints = new Vector3[numVerts];
            localNormals = new Vector3[numVerts];
            localTangents = new Vector3[numVerts];
            cumulativeLengthAtEachVertex = new float[numVerts];
            times = new float[numVerts];
            bounds = new Bounds ((pathSplitData.minMax.Min + pathSplitData.minMax.Max) / 2, pathSplitData.minMax.Max - pathSplitData.minMax.Min);

            // Figure out up direction for path
            up = (bounds.size.z > bounds.size.y) ? Vector3.up : -Vector3.forward;
            Vector3 lastRotationAxis = up;

            // Loop through the data and assign to arrays.
            for (int i = 0; i < localPoints.Length; i++) {
                localPoints[i] = pathSplitData.vertices[i];
                localTangents[i] = pathSplitData.tangents[i];
                cumulativeLengthAtEachVertex[i] = pathSplitData.cumulativeLength[i];
                times[i] = cumulativeLengthAtEachVertex[i] / length;

                // Calculate normals
                if (space == PathSpace.xyz) {
                    if (i == 0) {
                        localNormals[0] = Vector3.Cross (lastRotationAxis, pathSplitData.tangents[0]).normalized;
                    } else {
                        // First reflection
                        Vector3 offset = (localPoints[i] - localPoints[i - 1]);
                        float sqrDst = offset.sqrMagnitude;
                        Vector3 r = lastRotationAxis - offset * 2 / sqrDst * Vector3.Dot (offset, lastRotationAxis);
                        Vector3 t = localTangents[i - 1] - offset * 2 / sqrDst * Vector3.Dot (offset, localTangents[i - 1]);

                        // Second reflection
                        Vector3 v2 = localTangents[i] - t;
                        float c2 = Vector3.Dot (v2, v2);

                        Vector3 finalRot = r - v2 * 2 / c2 * Vector3.Dot (v2, r);
                        Vector3 n = Vector3.Cross (finalRot, localTangents[i]).normalized;
                        localNormals[i] = n;
                        lastRotationAxis = finalRot;
                    }
                } else {
                    localNormals[i] = Vector3.Cross (localTangents[i], up) * ((bezierPath.FlipNormals) ? 1 : -1);
                }
            }

            // Apply correction for 3d normals along a closed path
            if (space == PathSpace.xyz && isClosedLoop) {
                // Get angle between first and last normal (if zero, they're already lined up, otherwise we need to correct)
                float normalsAngleErrorAcrossJoin = Vector3.SignedAngle (localNormals[localNormals.Length - 1], localNormals[0], localTangents[0]);
                // Gradually rotate the normals along the path to ensure start and end normals line up correctly
                if (Mathf.Abs (normalsAngleErrorAcrossJoin) > 0.1f) // don't bother correcting if very nearly correct
                {
                    for (int i = 1; i < localNormals.Length; i++) {
                        float t = (i / (localNormals.Length - 1f));
                        float angle = normalsAngleErrorAcrossJoin * t;
                        Quaternion rot = Quaternion.AngleAxis (angle, localTangents[i]);
                        localNormals[i] = rot * localNormals[i] * ((bezierPath.FlipNormals) ? -1 : 1);
                    }
                }
            }

            // Rotate normals to match up with user-defined anchor angles
            if (space == PathSpace.xyz) {
                for (int anchorIndex = 0; anchorIndex < pathSplitData.anchorVertexMap.Count - 1; anchorIndex++) {
                    int nextAnchorIndex = (isClosedLoop) ? (anchorIndex + 1) % bezierPath.NumSegments : anchorIndex + 1;

                    float startAngle = bezierPath.GetAnchorNormalAngle (anchorIndex) + bezierPath.GlobalNormalsAngle;
                    float endAngle = bezierPath.GetAnchorNormalAngle (nextAnchorIndex) + bezierPath.GlobalNormalsAngle;
                    float deltaAngle = Mathf.DeltaAngle (startAngle, endAngle);

                    int startVertIndex = pathSplitData.anchorVertexMap[anchorIndex];
                    int endVertIndex = pathSplitData.anchorVertexMap[anchorIndex + 1];

                    int num = endVertIndex - startVertIndex;
                    if (anchorIndex == pathSplitData.anchorVertexMap.Count - 2) {
                        num += 1;
                    }
                    for (int i = 0; i < num; i++) {
                        int vertIndex = startVertIndex + i;
                        float t = i / (num - 1f);
                        float angle = startAngle + deltaAngle * t;
                        Quaternion rot = Quaternion.AngleAxis (angle, localTangents[vertIndex]);
                        localNormals[vertIndex] = (rot * localNormals[vertIndex]) * ((bezierPath.FlipNormals) ? -1 : 1);
                    }
                }
            }

            // * Mod part of the constructor
            if (localPoints.Length >= pointsPerSegment * minimumSegmentsForSegmentation)
            {
                pathSegmentated = true;
                segmentsAmount = Math.DivRem(localPoints.Length, pointsPerSegment, out int remainder);
                if (showLogMessages)
                    Debug.Log("Path segmentated");
            }
            else
            {
                pathSegmentated = false;
                if(showLogMessages)
                    Debug.Log("Path not segmentated");
            }
            // * End mod part of the constuctor
        }

        #endregion

        #region Public methods and accessors

        public void UpdateTransform (Transform transform) {
            this.transform = transform;
        }
        public int NumPoints {
            get {
                return localPoints.Length;
            }
        }

        public Vector3 GetTangent (int index) {
            return MathUtility.TransformDirection (localTangents[index], transform, space);
        }

        public Vector3 GetNormal (int index) {
            return MathUtility.TransformDirection (localNormals[index], transform, space);
        }

        public Vector3 GetPoint (int index) {
            return MathUtility.TransformPoint (localPoints[index], transform, space);
        }

        /// Gets point on path based on distance travelled.
        public Vector3 GetPointAtDistance (float dst, EndOfPathInstruction endOfPathInstruction = EndOfPathInstruction.Loop) {
            float t = dst / length;
            return GetPointAtTime (t, endOfPathInstruction);
        }

        /// Gets forward direction on path based on distance travelled.
        public Vector3 GetDirectionAtDistance (float dst, EndOfPathInstruction endOfPathInstruction = EndOfPathInstruction.Loop) {
            float t = dst / length;
            return GetDirection (t, endOfPathInstruction);
        }

        /// Gets normal vector on path based on distance travelled.
        public Vector3 GetNormalAtDistance (float dst, EndOfPathInstruction endOfPathInstruction = EndOfPathInstruction.Loop) {
            float t = dst / length;
            return GetNormal (t, endOfPathInstruction);
        }

        /// Gets a rotation that will orient an object in the direction of the path at this point, with local up point along the path's normal
        public Quaternion GetRotationAtDistance (float dst, EndOfPathInstruction endOfPathInstruction = EndOfPathInstruction.Loop) {
            float t = dst / length;
            return GetRotation (t, endOfPathInstruction);
        }

        /// Gets point on path based on 'time' (where 0 is start, and 1 is end of path).
        public Vector3 GetPointAtTime (float t, EndOfPathInstruction endOfPathInstruction = EndOfPathInstruction.Loop) {
            var data = CalculatePercentOnPathData (t, endOfPathInstruction);
            return Vector3.Lerp (GetPoint (data.previousIndex), GetPoint (data.nextIndex), data.percentBetweenIndices);
        }

        /// Gets forward direction on path based on 'time' (where 0 is start, and 1 is end of path).
        public Vector3 GetDirection (float t, EndOfPathInstruction endOfPathInstruction = EndOfPathInstruction.Loop) {
            var data = CalculatePercentOnPathData (t, endOfPathInstruction);
            Vector3 dir = Vector3.Lerp (localTangents[data.previousIndex], localTangents[data.nextIndex], data.percentBetweenIndices);
            return MathUtility.TransformDirection (dir, transform, space);
        }

        /// Gets normal vector on path based on 'time' (where 0 is start, and 1 is end of path).
        public Vector3 GetNormal (float t, EndOfPathInstruction endOfPathInstruction = EndOfPathInstruction.Loop) {
            var data = CalculatePercentOnPathData (t, endOfPathInstruction);
            Vector3 normal = Vector3.Lerp (localNormals[data.previousIndex], localNormals[data.nextIndex], data.percentBetweenIndices);
            return MathUtility.TransformDirection (normal, transform, space);
        }

        /// Gets a rotation that will orient an object in the direction of the path at this point, with local up point along the path's normal
        public Quaternion GetRotation (float t, EndOfPathInstruction endOfPathInstruction = EndOfPathInstruction.Loop) {
            var data = CalculatePercentOnPathData (t, endOfPathInstruction);
            Vector3 direction = Vector3.Lerp (localTangents[data.previousIndex], localTangents[data.nextIndex], data.percentBetweenIndices);
            Vector3 normal = Vector3.Lerp (localNormals[data.previousIndex], localNormals[data.nextIndex], data.percentBetweenIndices);
            return Quaternion.LookRotation (MathUtility.TransformDirection (direction, transform, space), MathUtility.TransformDirection (normal, transform, space));
        }

        /// Finds the closest point on the path from any point in the world
        public Vector3 GetClosestPointOnPath (Vector3 worldPoint) {
            TimeOnPathData data = CalculateClosestPointOnPathData (worldPoint);
            return Vector3.Lerp (GetPoint (data.previousIndex), GetPoint (data.nextIndex), data.percentBetweenIndices);
        }

        /// Finds the 'time' (0=start of path, 1=end of path) along the path that is closest to the given point
        public float GetClosestTimeOnPath (Vector3 worldPoint) {
            TimeOnPathData data = CalculateClosestPointOnPathData (worldPoint);
            return Mathf.Lerp (times[data.previousIndex], times[data.nextIndex], data.percentBetweenIndices);
        }

        /// Finds the distance along the path that is closest to the given point
        public float GetClosestDistanceAlongPath (Vector3 worldPoint) {
            TimeOnPathData data = CalculateClosestPointOnPathData (worldPoint);
            return Mathf.Lerp (cumulativeLengthAtEachVertex[data.previousIndex], cumulativeLengthAtEachVertex[data.nextIndex], data.percentBetweenIndices);
        }

        #endregion

        #region Internal methods

        /// For a given value 't' between 0 and 1, calculate the indices of the two vertices before and after t. 
        /// Also calculate how far t is between those two vertices as a percentage between 0 and 1.
        TimeOnPathData CalculatePercentOnPathData (float t, EndOfPathInstruction endOfPathInstruction) {
            // Constrain t based on the end of path instruction
            switch (endOfPathInstruction) {
                case EndOfPathInstruction.Loop:
                    // If t is negative, make it the equivalent value between 0 and 1
                    if (t < 0) {
                        t += Mathf.CeilToInt (Mathf.Abs (t));
                    }
                    t %= 1;
                    break;
                case EndOfPathInstruction.Reverse:
                    t = Mathf.PingPong (t, 1);
                    break;
                case EndOfPathInstruction.Stop:
                    t = Mathf.Clamp01 (t);
                    break;
            }

            int prevIndex = 0;
            int nextIndex = NumPoints - 1;
            int i = Mathf.RoundToInt (t * (NumPoints - 1)); // starting guess

            // Starts by looking at middle vertex and determines if t lies to the left or to the right of that vertex.
            // Continues dividing in half until closest surrounding vertices have been found.
            while (true) {
                // t lies to left
                if (t <= times[i]) {
                    nextIndex = i;
                }
                // t lies to right
                else {
                    prevIndex = i;
                }
                i = (nextIndex + prevIndex) / 2;

                if (nextIndex - prevIndex <= 1) {
                    break;
                }
            }

            float abPercent = Mathf.InverseLerp (times[prevIndex], times[nextIndex], t);
            return new TimeOnPathData (prevIndex, nextIndex, abPercent);
        }

        public struct TimeOnPathData {
            public readonly int previousIndex;
            public readonly int nextIndex;
            public readonly float percentBetweenIndices;

            public TimeOnPathData (int prev, int next, float percentBetweenIndices) {
                this.previousIndex = prev;
                this.nextIndex = next;
                this.percentBetweenIndices = percentBetweenIndices;
            }
        }

        /// Calculate time data for closest point on the path from given world point
        TimeOnPathData CalculateClosestPointOnPathData(Vector3 worldPoint)
        {
            float minSqrDst = float.MaxValue;
            Vector3 closestPoint = Vector3.zero;
            int closestSegmentIndexA = 0;
            int closestSegmentIndexB = 0;

            for (int i = 0; i < localPoints.Length; i++)
            {
                int nextI = i + 1;
                if (nextI >= localPoints.Length)
                {
                    if (isClosedLoop)
                        nextI %= localPoints.Length;
                    else
                        break;
                }

                Vector3 closestPointOnSegment = MathUtility.ClosestPointOnLineSegment(worldPoint, GetPoint(i), GetPoint(nextI));
                float sqrDst = (worldPoint - closestPointOnSegment).sqrMagnitude;
                if (sqrDst < minSqrDst)
                {
                    minSqrDst = sqrDst;
                    closestPoint = closestPointOnSegment;
                    closestSegmentIndexA = i;
                    closestSegmentIndexB = nextI;
                }
            }
            float closestSegmentLength = (GetPoint(closestSegmentIndexA) - GetPoint(closestSegmentIndexB)).magnitude;
            float t = (closestPoint - GetPoint(closestSegmentIndexA)).magnitude / closestSegmentLength;
            return new TimeOnPathData(closestSegmentIndexA, closestSegmentIndexB, t);
        }

        #endregion

        #region Mods Rooster racing

        // Note: The constructor is also modified at the end of its body.

        public bool showLogMessages;
        private readonly bool pathSegmentated;
        private readonly int minimumSegmentsForSegmentation = 4;
        private readonly int pointsPerSegment = 40;
        private readonly int segmentsAmount;

        public struct CharacterInPathFeatures
        {
            public int AtSegmentPartNum { get; set; }
            public int PointIndexB { get; set; }
            public int PointIndexA { get; set; }
            public bool WaitingForPathRestart { get; set; }
        }

        /// Finds the 'time' (0=start of path, 1=end of path) along the path that is closest to the given point
        public float GetClosestTimeOnPath(Vector3 worldPoint, ref CharacterInPathFeatures characterAtSegmentPartNum)
        {
            if(!pathSegmentated)
                return GetClosestTimeOnPath(worldPoint);
            else
            {
                TimeOnPathData data = CalculateClosestPointOnPathData(worldPoint, ref characterAtSegmentPartNum);
                return Mathf.Lerp(times[data.previousIndex], times[data.nextIndex], data.percentBetweenIndices);
            }
        }

        /// <summary>
        /// This overload gives the closest point on the path but only searching the point in a segment part and not through all of the path. 
        /// </summary>
        TimeOnPathData CalculateClosestPointOnPathData(Vector3 worldPoint, ref CharacterInPathFeatures characterAtSegmentPart)
        {
            float minSqrDst = float.MaxValue;
            Vector3 closestPoint = Vector3.zero;
            int closestSegmentIndexA = 0;
            int closestSegmentIndexB = 0;

            for (int i = characterAtSegmentPart.PointIndexA; i < characterAtSegmentPart.PointIndexB + pointsPerSegment; i++)
            {
                int indexA = i;
                int indexB = i + 1;

                // Prevents overflow when index is greater than localPoints lenght
                if (indexB >= localPoints.Length)
                {
                    if (indexA >= localPoints.Length)
                    {
                        indexA -= localPoints.Length;
                        indexB = indexA + 1;
                    }
                    else
                    {
                        indexA = localPoints.Length - 2;
                        indexB = localPoints.Length - 1;
                    }
                }

                Vector3 closestPointOnSegment = MathUtility.ClosestPointOnLineSegment(worldPoint, GetPoint(indexA), GetPoint(indexB));
                float sqrDst = (worldPoint - closestPointOnSegment).sqrMagnitude;
                if (sqrDst < minSqrDst)
                {
                    minSqrDst = sqrDst;
                    closestPoint = closestPointOnSegment;
                    closestSegmentIndexA = indexA;
                    closestSegmentIndexB = indexB;
                }
                if (CheckSegmentSurpass(ref characterAtSegmentPart, closestSegmentIndexB))
                    break;
            }
            float closestSegmentLength = (GetPoint(closestSegmentIndexA) - GetPoint(closestSegmentIndexB)).magnitude;
            float t = (closestPoint - GetPoint(closestSegmentIndexA)).magnitude / closestSegmentLength;
            return new TimeOnPathData(closestSegmentIndexA, closestSegmentIndexB, t);
        }

        /// <summary>
        /// Check if the character already reached the next segment part of the path and set it if so.
        /// </summary>
        /// <param name="characterAtSegmentPart"></param>
        /// <param name="closestSegmentIndexB"></param>
        /// <returns></returns>
        private bool CheckSegmentSurpass(ref CharacterInPathFeatures characterPathFeatures, int closestSegmentIndexB)
        {
            // Check if the player surpass the last point when in last segment
            if (characterPathFeatures.AtSegmentPartNum >= segmentsAmount)
            {
                // if ((closestSegmentIndexB >= localPoints.Length - 1))
                if ((closestSegmentIndexB <= pointsPerSegment))
                {
                    characterPathFeatures.AtSegmentPartNum = 1;
                    characterPathFeatures.PointIndexA = 0;
                    characterPathFeatures.PointIndexB = pointsPerSegment;
                    if (showLogMessages)
                    {
                        Debug.Log("path restarted");
                        Debug.Log($" AtSegmentPartNum: {characterPathFeatures.AtSegmentPartNum}/{segmentsAmount}");
                        Debug.Log($" PointIndexA: {characterPathFeatures.PointIndexA}/{localPoints.Length - 1}, PointIndexB: {characterPathFeatures.PointIndexB}/{localPoints.Length - 1}");
                    }
                    return true;
                }
            }

            // Check if player already in the next segment
            else if (closestSegmentIndexB > characterPathFeatures.PointIndexB)
            {
                characterPathFeatures.AtSegmentPartNum++;
                characterPathFeatures.PointIndexA = (characterPathFeatures.AtSegmentPartNum * pointsPerSegment) - pointsPerSegment;
                if (characterPathFeatures.AtSegmentPartNum >= segmentsAmount)
                    characterPathFeatures.PointIndexB = localPoints.Length - 1;
                else
                    characterPathFeatures.PointIndexB = characterPathFeatures.PointIndexA + pointsPerSegment;
                if (showLogMessages)
                {
                    Debug.Log($"AtSegmentPartNum: {characterPathFeatures.AtSegmentPartNum}/{segmentsAmount}");
                    Debug.Log($"PointIndexA: {characterPathFeatures.PointIndexA}/{localPoints.Length - 1}, PointIndexB: {characterPathFeatures.PointIndexB}/{localPoints.Length - 1}");
                }
                return true;
            }

            // Character have not surpass the current segment
            return false;
        }

        /// <summary>
        /// <para> Get the selected character position on the path to know in which segment of it it's placed. </para>
        /// <para> Needed when starting the movement on path or when the character teleported to other segment part of the path. </para>
        /// </summary>
        /// <param name="characterPathFeatures"></param>
        /// <param name="characterCurrentPos"></param>
        public void SetCharacterInPath(ref CharacterInPathFeatures characterPathFeatures, Vector3 characterCurrentPos)
        {
            if (!pathSegmentated)
                return;

            characterPathFeatures = new CharacterInPathFeatures();
            var playerAtPoint = GetPlayerCurrentPointOnPath(characterCurrentPos);
            int playerAtSegment = 1;
            for (int i = 1; i < segmentsAmount + 1; i++)
            {
                if (playerAtPoint > (i * pointsPerSegment))
                    playerAtSegment++;
                else
                    break;
            }
            
            characterPathFeatures.AtSegmentPartNum = playerAtSegment;
            characterPathFeatures.PointIndexA = (playerAtSegment - 1) * pointsPerSegment;
            characterPathFeatures.PointIndexB = characterPathFeatures.PointIndexA + pointsPerSegment;
            if (showLogMessages)
            {
                Debug.Log($"AtSegmentPartNum: {characterPathFeatures.AtSegmentPartNum}/{segmentsAmount}");
                Debug.Log($"PointIndexA: {characterPathFeatures.PointIndexA}/{localPoints.Length - 1}, PointIndexB: {characterPathFeatures.PointIndexB}/{localPoints.Length - 1}");
                Debug.Log($"player started at point: {playerAtPoint}");
            }
        }

        public int GetPlayerCurrentPointOnPath(Vector3 worldPoint)
        {
            float minSqrDst = float.MaxValue;
            int closestSegmentIndexA = 0;

            for (int i = 0; i < localPoints.Length; i++)
            {
                int nextI = i + 1;
                if (nextI >= localPoints.Length)
                {
                    if (isClosedLoop)
                        nextI %= localPoints.Length;
                    else
                        break;
                }

                Vector3 closestPointOnSegment = MathUtility.ClosestPointOnLineSegment(worldPoint, GetPoint(i), GetPoint(nextI));
                float sqrDst = (worldPoint - closestPointOnSegment).sqrMagnitude;
                if (sqrDst < minSqrDst)
                {
                    minSqrDst = sqrDst;
                    closestSegmentIndexA = i;
                }
            }
            return closestSegmentIndexA;
        }

        #endregion

    }

}