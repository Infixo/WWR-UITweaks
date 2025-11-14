using HarmonyLib;
using Microsoft.Xna.Framework;
using STM.Data;
using STM.GameWorld;
using STM.GameWorld.Users;
using STM.UI;
using STMG.UI.Control;
using STMG.Utility;
using Utilities;

namespace UITweaks.UI;


[HarmonyPatch(typeof(NotificationPresets))]
public static class NotificationPresets_Patches
{
    private static Color GetColor(CityUser city, ushort company)
    {
        if (city.Routes.Count == 0) return LabelPresets.Color_main;
        for (int i = 0; i < city.Routes.Count; i++)
            if (city.Routes[i].Vehicle.Company == company) return LabelPresets.Color_positive;
        return LabelPresets.Color_negative;
    }


    [HarmonyPatch("TryAddLevelUp"), HarmonyPrefix]
    public static bool NotificationPresets_TryAddLevelUp_Prefix(GameScene scene)
    {
        if (NotificationPresets.Level_up.Count == 0)
            return false;
        CityUser[] _items = NotificationPresets.Level_up.ToArray();
        NotificationPresets.Level_up.Clear();
        Button _button = typeof(NotificationPresets).CallPrivateStaticMethod<Button>("GetBase", [Localization.GetTasks("cities_grow"), LabelPresets.Color_positive, scene.Engine]);
        // Prevent easy closing
        _button.OnButtonPress.Clear();
        _button.OnButtonPress += (Action)delegate
        {
            if(scene.Engine.Keys.Ctrl || scene.Engine.Keys.Shift)
                _button.CloseWithAnimation(close_if_no_animation: true);
        };
        _button.OnMouseStillTime += (Action)delegate
        {
            TooltipPreset tooltipPreset = TooltipPreset.Get(Localization.GetTasks("cities_grow"), scene.Engine, can_lock: true);
            tooltipPreset.AddDescription(Localization.GetInfo("city"));
            tooltipPreset.AddSeparator();
            foreach(CityUser city in _items)
            {
                // Adds info about new destination
                string _name = $"<!cicon_star>{city.Level} {city.GetNameWithIcon(scene)}";
                _name += $" <!#{(LabelPresets.Color_main * 0.75f).GetHex()}>({city.City.GetCountry(scene).Name.GetTranslation(Localization.Language)})";
                CityUser _dest = city.Destinations.Items.Last.Destination.User;
                tooltipPreset.AddStatsLine(_name, $"<!#{GetColor(_dest, scene.Session.Player).GetHex()}>{_dest.GetNameWithIcon(scene)} <!cicon_star>{_dest.Level}");
            }
            // Legend
            tooltipPreset.AddSeparator();
            tooltipPreset.AddDescription($"New destination color: <!#{LabelPresets.Color_positive.GetHex()}>green when your route(s) exist, <!#{LabelPresets.Color_negative.GetHex()}>red for other players', <!#{LabelPresets.Color_main.GetHex()}>white when there is no route.");
            tooltipPreset.AddToControlAuto(_button);
        };
        scene.Tasks_ui.AddNotification(_button);
        return false;
    }


    [HarmonyPatch("TryAddDestinationChange"), HarmonyPrefix]
    public static bool NotificationPresets_TryAddDestinationChange_Prefix(GameScene scene)
    {
        if (NotificationPresets.Destination_change.Count == 0)
            return false;
        CityUser[] _items = NotificationPresets.Destination_change.ToArray();
        NotificationPresets.Destination_change.Clear();
        Button _button = typeof(NotificationPresets).CallPrivateStaticMethod<Button>("GetBase", [Localization.GetTasks("destination_change"), LabelPresets.Color_main, scene.Engine]);
        // Prevent easy closing
        _button.OnButtonPress.Clear();
        _button.OnButtonPress += (Action)delegate
        {
            if (scene.Engine.Keys.Ctrl || scene.Engine.Keys.Shift)
                _button.CloseWithAnimation(close_if_no_animation: true);
        };
        _button.OnMouseStillTime += (Action)delegate
        {
            TooltipPreset tooltipPreset = TooltipPreset.Get(Localization.GetTasks("destination_change"), scene.Engine, can_lock: true);
            tooltipPreset.AddDescription(Localization.GetInfo("destination_last").Replace("{percent}", StrConversions.Percent((float)MainData.Defaults.City_destination_change)));
            tooltipPreset.AddSeparator();
            for (int i = 0; i < _items.Length; i += 3)
                tooltipPreset.AddStatsLine(
                    $"<!cicon_star>{_items[i].Level} {_items[i].GetNameWithIcon(scene)}",
                    $"{_items[i + 1].GetNameWithIcon(scene)} <!cicon_right> <!#{GetColor(_items[i + 2], scene.Session.Player).GetHex()}>{_items[i + 2].GetNameWithIcon(scene)} <!cicon_star>{_items[i + 2].Level}");
            tooltipPreset.AddToControlAuto(_button);
        };
        scene.Tasks_ui.AddNotification(_button);
        return false;
    }
}
