using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace RandomModButton;

public class RandomModButton : Mod
{
    internal static Hook buildModPack = null;
    internal static MethodInfo modLoaderReload = null;

    public override void Load()
    {
        var modAsm = typeof(Mod).Assembly;
        var localMod = modAsm.GetType("Terraria.ModLoader.Core.LocalMod");
        var modEnum = typeof(IEnumerable<>).MakeGenericType(localMod);
        ConstructorInfo constructorInfo = modAsm.GetType("Terraria.ModLoader.UI.UIModPackItem").GetConstructors()[0];

        buildModPack = new Hook(constructorInfo, CtorDetour);
    }

    public static void CtorDetour(Action<UIModPackItem, string, string[], bool, object> orig, UIModPackItem self, string name, string[] mods, bool legacy, object localMods)
    {
        orig(self, name, mods, legacy, localMods);

        var randomMod = new UIAutoScaleTextTextPanel<string>(Language.GetTextValue("Mods.RandomModButton.Random"))
        {
            Width = StyleDimension.FromPixels(140),
            Height = StyleDimension.FromPixels(36),
            Left = StyleDimension.FromPixels(374),
            Top = StyleDimension.FromPixels(160),
        }.WithFadedMouseOver();
        randomMod.PaddingTop -= 2f;
        randomMod.PaddingBottom -= 2f;
        randomMod.OnLeftClick += (_, _) => EnableRandomMod(self);
        self.Append(randomMod);

        var quickMod = new UIAutoScaleTextTextPanel<string>(Language.GetTextValue("Mods.RandomModButton.Quick"))
        {
            Width = StyleDimension.FromPixels(140),
            Height = StyleDimension.FromPixels(36),
            Left = StyleDimension.FromPixels(230),
            Top = StyleDimension.FromPixels(160),
        }.WithFadedMouseOver();
        quickMod.PaddingTop -= 2f;
        quickMod.PaddingBottom -= 2f;
        quickMod.OnLeftClick += (_, _) => EnableRandomMod(self, true);
        self.Append(quickMod);
    }

    private static void EnableRandomMod(UIModPackItem modListItem, bool quick = false)
    {
        LocalMod[] array = ModOrganizer.FindMods();
        LocalMod[] validModsToChoose = [.. array.Where(x => !x.Enabled && modListItem._mods.Contains(x.Name))];

        if (validModsToChoose.Length == 0)
        {
            Interface.infoMessage.Show(Language.GetTextValue("Mods.RandomModButton.NoModToChoose"), 10016);
            return;
        }

        LocalMod mod = Main.rand.Next(validModsToChoose);
        mod.Enabled = true;

        if (!quick)
        {
            ModLoader.OnSuccessfulLoad = (Action)Delegate.Combine(ModLoader.OnSuccessfulLoad, (Action)delegate
            {
                Main.menuMode = 10016;
            });
            ModLoader.Reload();
        }
    }
}
