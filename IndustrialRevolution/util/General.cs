namespace IndustrialRevolution.util;

using System.Collections.Generic;
using System;

public class General
{
    /// NOTE: "getNeighbors(T) T" must only return valid (bounded) coordinates
    public static void BreadthFirstSearch<Pos, XYZ>(
        Dictionary<XYZ, Pos> occupied, Queue<XYZ> toCheck, XYZ root, int limit,
        Func<XYZ, XYZ[]> getNeighbors, Func<XYZ, Pos> toPos,
        Action<XYZ, Pos, XYZ, Pos>? onVisit
    ) where XYZ : notnull
    {
        if (occupied.Count == 0) occupied.Add(root, toPos(root));
        if (toCheck.Count == 0) toCheck.Enqueue(root);

        while (occupied.Count < limit && toCheck.Count > 0)
        {
            XYZ currXYZ = toCheck.Dequeue();

            foreach (XYZ neighXYZ in getNeighbors(currXYZ))
            {
                // TODO: verify if this creates unrecoverable state
                if (occupied.Count >= limit) break;

                Pos neighPos = toPos(neighXYZ);

                if (occupied.ContainsKey(neighXYZ)) continue;

                if (onVisit == null)
                {
                    occupied.Add(neighXYZ, neighPos);
                    toCheck.Enqueue(neighXYZ);
                }
                else
                {
                    onVisit.Invoke(
                        currXYZ, occupied[currXYZ],
                        neighXYZ, neighPos
                    );
                }
            }
        }
    }

    public static float[] QuaternionToMatrix(System.Numerics.Quaternion q)
    {
        var m = System.Numerics.Matrix4x4.CreateFromQuaternion(q);

        // NOTE: System.Numerics is row major with row vectors, while Matrixf
        // is column major, column vector BUT the memory layout is the same
        // THIS IS NOT A TRANSPOSE, this is just copying a Matrix4x4 to a list
        return new[]
        {
            m.M11, m.M12, m.M13, m.M14,
            m.M21, m.M22, m.M23, m.M24,
            m.M31, m.M32, m.M33, m.M34,
            m.M41, m.M42, m.M43, m.M44,
        };
    }
}
