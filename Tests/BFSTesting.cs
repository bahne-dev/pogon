using IndustrialRevolution.util;

namespace IndustrialRevolution.Tests.util;

public class GeneralTests
{
    private record Coord(int X, int Y);

    [Fact]
    public void BFS_BasicTraversal_VisitsAllReachableNodes()
    {
        var occupied = new Dictionary<Coord, Coord>();
        var toCheck = new Queue<Coord>();
        var root = new Coord(0, 0);

        General.BreadthFirstSearch(
            occupied, toCheck, root, limit: 10,
            getNeighbors: GetNeighbors4Way,
            toPos: c => c,
            onVisit: null
        );

        Assert.True(occupied.Count > 1, "Should visit multiple nodes");
        Assert.Contains(root, occupied);
    }

    [Fact]
    public void BFS_RespectsLimit_StopsAtMaxOccupied()
    {
        var occupied = new Dictionary<Coord, Coord>();
        var toCheck = new Queue<Coord>();
        var root = new Coord(0, 0);
        int limit = 5;

        General.BreadthFirstSearch(
            occupied, toCheck, root, limit,
            getNeighbors: GetNeighbors4Way,
            toPos: c => c,
            onVisit: null
        );

        Assert.Equal(limit, occupied.Count);
    }

    [Fact]
    public void BFS_WithPrePopulatedOccupied_ContinuesFromExisting()
    {
        var occupied = new Dictionary<Coord, Coord>
        {
            [new(0, 0)] = new(1, 0),
            [new(2, 0)] = new(3, 0)
        };

        var toCheck = new Queue<Coord>();
        var root = new Coord(0, 0);

        General.BreadthFirstSearch(
            occupied, toCheck, root, limit: 5,
                getNeighbors: GetNeighbors4Way,
                toPos: c => c,
                onVisit: null
            );

        Assert.Equal(5, occupied.Count);
        Assert.Contains(new Coord(0, 0), occupied);
        Assert.Contains(new Coord(1, 0), occupied);
    }

    [Fact]
    public void BFS_WithPrePopulatedQueue_ProcessesExistingQueue()
    {
        var occupied = new Dictionary<Coord, Coord>
        {
            [new(0, 0)] = new(1, 0),
            [new(2, 0)] = new(3, 0)
        };
        var toCheck = new Queue<Coord>();
        toCheck.Enqueue(new Coord(0, 0));
        toCheck.Enqueue(new Coord(1, 0));
        var root = new Coord(0, 0);

        General.BreadthFirstSearch(
            occupied, toCheck, root, limit: 10,
            getNeighbors: GetNeighbors4Way,
            toPos: c => c,
            onVisit: null
        );

        Assert.True(occupied.Count > 2);
    }

    [Fact]
    public void BFS_DoesNotRevisitOccupiedPositions()
    {
        var occupied = new Dictionary<Coord, Coord>();
        var toCheck = new Queue<Coord>();
        var root = new Coord(0, 0);
        var visitedPairs = new List<(Coord current, Coord neighbor)>();

        General.BreadthFirstSearch(
            occupied, toCheck, root, limit: 100,
            getNeighbors: GetNeighbors4Way,
            toPos: c => c,
            onVisit: (currXYZ, currPos, neighXYZ, neighPos) =>
            {
                visitedPairs.Add((currPos, neighPos));
                occupied.Add(neighXYZ, neighPos);
                toCheck.Enqueue(neighXYZ);
            }
        );

        // no position should be visited as neighbor more than once
        var neighborVisits = visitedPairs.GroupBy(p => p.neighbor);
        Assert.All(neighborVisits, g => Assert.Single(g));
    }

    [Fact]
    public void BFS_WithCustomOnVisit_UsesCallback()
    {
        var occupied = new Dictionary<Coord, Coord>();
        var toCheck = new Queue<Coord>();
        var root = new Coord(0, 0);
        var visitCount = 0;

        General.BreadthFirstSearch(
            occupied, toCheck, root, limit: 5,
            getNeighbors: GetNeighbors4Way,
            toPos: c => c,
            onVisit: (currXYZ, currPos, neighXYZ, neighPos) =>
            {
                visitCount++;
                occupied.Add(neighXYZ, neighPos);
                toCheck.Enqueue(neighXYZ);
            }
        );

        Assert.True(visitCount > 0, "OnVisit should be called");
        Assert.Equal(5, occupied.Count);
    }

    [Fact]
    public void BFS_WithNullOnVisit_AutomaticallyAddsToOccupiedAndQueue()
    {
        var occupied = new Dictionary<Coord, Coord>();
        var toCheck = new Queue<Coord>();
        var root = new Coord(0, 0);

        General.BreadthFirstSearch(
            occupied, toCheck, root, limit: 3,
            getNeighbors: GetNeighbors4Way,
            toPos: c => c,
            onVisit: null
        );

        Assert.Equal(3, occupied.Count);
    }

    [Fact]
    public void BFS_BreadthFirstOrder_VisitsClosestNodesFirst()
    {
        var occupied = new Dictionary<Coord, Coord>();
        var toCheck = new Queue<Coord>();
        var root = new Coord(0, 0);
        var visitOrder = new List<Coord>();

        General.BreadthFirstSearch(
            occupied, toCheck, root, limit: 100,
            getNeighbors: GetNeighbors4Way,
            toPos: c => c,
            onVisit: (currXYZ, currPos, neighXYZ, neighPos) =>
            {
                if (!occupied.ContainsKey(neighXYZ))
                {
                    visitOrder.Add(neighPos);
                    occupied.Add(neighXYZ, neighPos);
                    toCheck.Enqueue(neighXYZ);
                }
            }
        );

        // nodes at distance 1 should come before nodes at distance 2
        var distance1Nodes = visitOrder.Where(c => Math.Abs(c.X) + Math.Abs(c.Y) == 1).ToList();
        var distance2Nodes = visitOrder.Where(c => Math.Abs(c.X) + Math.Abs(c.Y) == 2).ToList();

        if (distance1Nodes.Any() && distance2Nodes.Any())
        {
            var lastDistance1Index = visitOrder.IndexOf(distance1Nodes.Last());
            var firstDistance2Index = visitOrder.IndexOf(distance2Nodes.First());
            Assert.True(lastDistance1Index < firstDistance2Index,
                "All distance-1 nodes should be visited before distance-2 nodes");
        }
    }

    [Fact]
    public void BFS_WithDifferentPosType_WorksWithConversion()
    {
        var occupied = new Dictionary<Coord, string>();
        var toCheck = new Queue<Coord>();
        var root = new Coord(0, 0);

        General.BreadthFirstSearch(
            occupied, toCheck, root, limit: 5,
            getNeighbors: GetNeighbors4Way,
            toPos: c => $"{c.X},{c.Y}",
            onVisit: null
        );

        Assert.Equal(5, occupied.Count);
        Assert.Contains(new Coord(0,0), occupied.Keys);
    }

    [Fact]
    public void BFS_EmptyGraph_OnlyContainsRoot()
    {
        var occupied = new Dictionary<Coord, Coord>();
        var toCheck = new Queue<Coord>();
        var root = new Coord(0, 0);

        // getNeighbors returns empty array
        General.BreadthFirstSearch(
            occupied, toCheck, root, limit: 10,
            getNeighbors: _ => Array.Empty<Coord>(),
            toPos: c => c,
            onVisit: null
        );

        Assert.Single(occupied);
        Assert.Contains(root, occupied);
    }

    [Fact]
    public void BFS_LimitOfOne_OnlyContainsRoot()
    {
        var occupied = new Dictionary<Coord, Coord>();
        var toCheck = new Queue<Coord>();
        var root = new Coord(0, 0);

        General.BreadthFirstSearch(
            occupied, toCheck, root, limit: 1,
            getNeighbors: GetNeighbors4Way,
            toPos: c => c,
            onVisit: null
        );

        Assert.Single(occupied);
        Assert.Contains(root, occupied);
    }

    private static Coord[] GetNeighbors4Way(Coord coord)
    {
        return new[]
        {
            coord with { Y = coord.Y + 1 },
            coord with { Y = coord.Y - 1 },
            coord with { X = coord.X + 1 },
            coord with { X = coord.X - 1 }
        };
    }
}
