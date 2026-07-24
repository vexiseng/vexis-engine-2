using Xunit;
using Vexis.World;

namespace Vexis.World.Tests;

public sealed class ProjectValidationTests
{
    [Fact]
    public void ValidationFlagsOutOfBoundsObjectsAndDuplicateContentIds()
    {
        var service = new ProjectValidationService();
        var issues = service.Validate(
            64,
            64,
            [new ValidationSceneObject("Out of Bounds", 80, 10)],
            [new ValidationContentItem("npc", "guard"), new ValidationContentItem("npc", "guard")]);

        Assert.Contains(issues, issue => issue.Code == "object-out-of-bounds");
        Assert.Contains(issues, issue => issue.Code == "duplicate-content-id");
    }
}
