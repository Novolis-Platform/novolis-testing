using Novolis.Testing.Appium;

namespace Novolis.Testing.Unit;

public sealed class AndroidStartActivityScriptTests
{
    [Test]
    public async Task Arguments_compose_intent_from_package_and_activity()
    {
        var args = AndroidStartActivityScript.Arguments("com.android.settings", ".Settings");
        await Assert.That(args["intent"]).IsEqualTo("com.android.settings/.Settings");
        await Assert.That(args["appPackage"]).IsEqualTo("com.android.settings");
        await Assert.That(args["appActivity"]).IsEqualTo(".Settings");
    }
}
