using Novolis.Testing.Playwright;

namespace Novolis.Testing.Unit;

public sealed class PlaywrightWalkthroughTitleTests
{
    [Test]
    public async Task FromTestName_replaces_underscores_with_spaces()
    {
        await Assert.That(PlaywrightWalkthroughTitle.FromTestName(
                "Game_shop_from_platform_customer_through_month_hr_and_auditor"))
            .IsEqualTo("Game shop from platform customer through month hr and auditor");
    }

    [Test]
    public async Task FromTypeName_spaces_a_Pascal_class()
    {
        await Assert.That(PlaywrightWalkthroughTitle.FromTypeName("HoursGameMonthScenarioTests"))
            .IsEqualTo("Hours Game Month Scenario Tests");
    }

    [Test]
    public async Task Display_humanizes_an_identifier_and_leaves_a_sentence()
    {
        await Assert.That(PlaywrightWalkthroughTitle.Display("Helen_is_hr_and_admin_without_a_week"))
            .IsEqualTo("Helen is hr and admin without a week");
        await Assert.That(PlaywrightWalkthroughTitle.Display("3. Robin registers the month"))
            .IsEqualTo("3. Robin registers the month");
    }
}
