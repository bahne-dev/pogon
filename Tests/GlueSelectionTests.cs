namespace IndustrialRevolution.Tests;

public class GlueSelectionTests
{
    private static bool Solid((int X, int Y, int Z) _) => true;

    private static void Box(
        GlueSelection selection,
        GlueEdit edit,
        (int, int, int) a,
        (int, int, int) b
    )
    {
        selection.Click(a, edit, Solid);
        selection.Click(b, edit, Solid);
    }

    [Fact]
    public void FirstClick_OnlySetsCorner()
    {
        var selection = new GlueSelection();

        var result = selection.Click((1, 2, 3), GlueEdit.Add, Solid);

        Assert.Equal(GlueClickResult.CornerSet, result);
        Assert.Equal((1, 2, 3), selection.PendingCorner);
        Assert.Empty(selection.Blocks);
    }

    [Fact]
    public void SameBlockTwice_AddsOneBlock()
    {
        var selection = new GlueSelection();

        Box(selection, GlueEdit.Add, (1, 2, 3), (1, 2, 3));

        Assert.Equal((1, 2, 3), Assert.Single(selection.Blocks));
        Assert.Null(selection.PendingCorner);
    }

    [Fact]
    public void Add_SkipsAirBlocks()
    {
        var selection = new GlueSelection();

        selection.Click((0, 0, 0), GlueEdit.Add, p => p.X == 0);
        selection.Click((2, 0, 0), GlueEdit.Add, p => p.X == 0);

        Assert.Equal((0, 0, 0), Assert.Single(selection.Blocks));
    }

    [Fact]
    public void Remove_CutsBoxOutOfSelection()
    {
        var selection = new GlueSelection();

        Box(selection, GlueEdit.Add, (0, 0, 0), (2, 0, 2));
        Box(selection, GlueEdit.Remove, (1, 0, 1), (1, 0, 1));

        Assert.Equal(8, selection.Blocks.Count);
        Assert.DoesNotContain((1, 0, 1), selection.Blocks);
    }

    [Fact]
    public void SeparateBoxes_Combine()
    {
        var selection = new GlueSelection();

        Box(selection, GlueEdit.Add, (0, 0, 0), (1, 0, 0));
        Box(selection, GlueEdit.Add, (0, 1, 0), (0, 3, 0));

        Assert.Equal(5, selection.Blocks.Count);
    }

    [Fact]
    public void SwitchingEdit_RestartsCorner()
    {
        var selection = new GlueSelection();

        selection.Click((0, 0, 0), GlueEdit.Add, Solid);
        var result = selection.Click((5, 0, 0), GlueEdit.Remove, Solid);

        Assert.Equal(GlueClickResult.CornerSet, result);
        Assert.Equal((5, 0, 0), selection.PendingCorner);
        Assert.Empty(selection.Blocks);
    }

    [Fact]
    public void Add_OverSpanLimit_IsRejectedAndKeepsSelection()
    {
        var selection = new GlueSelection();
        Box(selection, GlueEdit.Add, (0, 0, 0), (0, 0, 0));

        selection.Click((15, 0, 0), GlueEdit.Add, Solid);
        var fits = selection.Click((15, 0, 0), GlueEdit.Add, Solid);

        selection.Click((16, 0, 0), GlueEdit.Add, Solid);
        var tooFar = selection.Click((16, 0, 0), GlueEdit.Add, Solid);

        Assert.Equal(GlueClickResult.Applied, fits);
        Assert.Equal(GlueClickResult.TooLarge, tooFar);
        Assert.Equal(2, selection.Blocks.Count);
    }

    [Fact]
    public void Add_OverSpanLimit_NeverWalksTheBox()
    {
        var selection = new GlueSelection();
        int checks = 0;

        selection.Click((0, 0, 0), GlueEdit.Add, _ => { checks++; return true; });
        var result = selection.Click(
            (100000, 0, 0), GlueEdit.Add, _ => { checks++; return true; }
        );

        Assert.Equal(GlueClickResult.TooLarge, result);
        Assert.Equal(0, checks);
    }
}
