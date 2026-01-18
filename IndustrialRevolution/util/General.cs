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
}
