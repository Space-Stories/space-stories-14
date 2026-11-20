// ReSharper disable CheckNamespace

namespace Content.Client.UserInterface.Systems.Bwoink;

public sealed partial class AHelpUIController
{
    private void StoriesResetAdminControl()
    {
        if (UIHelper is not AdminAHelpUIHandler { Control: { } control } helper)
            return;

        if (helper.IsOpen)
            helper.Close();

        control.BwoinkArea.RemoveAllChildren();
        helper.Window = null;
        helper.Control = null;
        helper.EverOpened = false;
    }
}
