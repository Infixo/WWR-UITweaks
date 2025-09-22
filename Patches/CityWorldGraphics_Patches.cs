using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using STM.Data;
using STM.GameWorld;
using STM.GameWorld.Users;
using STM.UI;
using STMG.Drawing;
using STMG.Drawing.FreeType;
using STMG.Engine;
using STVisual.Utility;
using System.Reflection;

namespace UITweaks.Patches;

[HarmonyPatch(typeof(CityWorldGraphics))]
public static class CityWorldGraphics_Patches
{
    [HarmonyPatch("DrawName"), HarmonyPrefix]
    public static bool CityWorldGraphics_DrawName_Prefix(bool hover, CityUser city, float height, Vector2 offset, Color color, ThreadDrawLayer layers, GameEngine engine)
    {
        if (layers.Zoom > 25f)
        {
            return false; // skip the original
        }
        DrawSpriteCollection<VertexPositionColorTexture, DrawTexture> _layer = layers.Text();
        FreeFontSystem _font = MainData.Font_world_text.Get(engine);
        bool _collision = true;
        if (layers.Scene.Selection.IsSelected(city))
        {
            color = Color.Lerp(color, LabelPresets.Color_positive, 0.5f);
        }
        float _s = (city.City.Capital ? 0.002f : 0.0015f);
        if (!city.Important)
        {
            _s *= 0.8f;
        }
        if (layers.Zoom < 5f)
        {
            _s = _s * layers.Zoom / 5f;
        }
        string _name = city.Name;
        if (layers.Scene.Action is CopyRouteAction || layers.Scene.Action is CreateNewRoadAction)
        {
            color *= 0.5f;
            _collision = false;
        }
        else if (layers.Scene.Map.view == 1)
        {
            if (city.Trust.Dominated != ushort.MaxValue)
            {
                color = layers.Scene.Session.Companies[city.Trust.Dominated].Color_main;
                if (city.Trust.Dominated != layers.Scene.Session.Player)
                {
                    color *= layers.Scene.Map.competitors;
                }
            }
            else
            {
                color *= 0.5f;
            }
            _name = "<!cicon_" + city.City.GetCountry(layers.Scene).ISO3166_1 + ":48>" + _name;
        }
        else if (layers.Scene.Map.view > 0)
        {
            color *= 0.5f;
        }
        if (!hover && color == LabelPresets.Color_main)
        {
            color *= 0.85f;
        }
        offset.Y -= 133.33333f * _s;
        if (city.Important)
        {
            if (layers.Zoom > 15f)
            {
                color *= 1f - (layers.Zoom - 15f) / 10f;
            }
            if (color.A <= 10)
            {
                return false; // skip the original
            }
        }
        else
        {
            if (layers.Zoom > 8f)
            {
                color *= 1f - (layers.Zoom - 8f) / 6f;
            }
            color *= 0.8f;
            if (color.A <= 10)
            {
                return false; // skip the original
            }
        }
        if ((layers.Scene.Map.view == 1 || layers.Scene.Info_mode) && city.Trust.Dominated != ushort.MaxValue)
        {
            _name += $"<!cc_{city.Trust.Dominated}:128>";
            if (city.Hubs.Count > 0 && city.Hubs[0].Company != city.Trust.Dominated)
            {
                _name += $"<!cicon_error_x2:128><!cc_{city.Hubs[0].Company}:128>";
            }
        }
        else if (city.City.Capital)
        {
            _name += "<!cicon_country_x2:48>";
        }

        // Infixo: get the color based on overcrowding
        color = OvercrowdedColor(city, color);

        if (hover)
        {
            if (city.Sea != null)
            {
                VectorRectangle _b4 = new VectorRectangle(_s * 60f);
                _b4.Offset(offset.X - 0.1f * _s * 400f, offset.Y + 0.06f * _s * 400f);
                MainData.Icon_port.Upscale.DrawHeight(layers.Cities(), _b4, Color.Black * 0.5f, height);
                MainData.Icon_port.Upscale.DrawHeight(layers.Cities(), _b4, color, height + 0.04f);
            }
            if (city.Accepts_indirect > 20)
            {
                VectorRectangle _b3 = new VectorRectangle(_s * 60f);
                _b3.Offset(offset.X - 0.025f * _s * 400f, offset.Y + 0.06f * _s * 400f);
                float _opacity2 = 0.75f + (float)Math.Sin(engine.Metrics.Time_played.TotalSeconds * 4.0) * 0.25f;
                MainData.Icon_error.Upscale.DrawHeight(layers.Cities(), _b3, Color.Black * 0.5f * _opacity2, height);
                MainData.Icon_error.Upscale.DrawHeight(layers.Cities(), _b3, Color.White * _opacity2, height + 0.04f);
            }
            _font.DrawString(_layer, _name, offset, Color.Black * 0.3f, _s, not_trimmed: true, markup: true, height);
            _font.DrawString(_layer, _name, offset, color, _s, not_trimmed: true, markup: true, height + 0.05f);
        }
        else
        {
            if (city.Sea != null)
            {
                VectorRectangle _b2 = new VectorRectangle(_s * 60f);
                _b2.Offset(offset.X - 0.1f * _s * 400f, offset.Y + 0.06f * _s * 400f);
                MainData.Icon_port.Upscale.DrawHeight(layers.Cities(), _b2, color, height);
            }
            if (city.Accepts_indirect > 20)
            {
                VectorRectangle _b = new VectorRectangle(_s * 60f);
                _b.Offset(offset.X - 0.025f * _s * 400f, offset.Y + 0.06f * _s * 400f);
                float _opacity = 0.75f + (float)Math.Sin(engine.Metrics.Time_played.TotalSeconds * 4.0) * 0.25f;
                MainData.Icon_error.Upscale.DrawHeight(layers.Cities(), _b, Color.White * _opacity, height);
            }
            _font.DrawString(_layer, _name, offset, color, _s, not_trimmed: true, markup: true, height);
        }
        Vector2 _size = _font.MeasureString(city.Name, _s);
        VectorRectangle _bounds = new VectorRectangle(_size.X, _size.Y);
        Vector2 _screen = layers.Camera.ToScreen2D(new Vector3(offset.X, height, offset.Y));
        _screen = layers.Camera.ToWorld2D(_screen);
        _bounds.Offset(_screen.X + _size.X / 2f, _screen.Y + _size.Y / 2f);
        _bounds.y2 += 0.1f;
        if (hover)
        {
            _bounds.Expand(0.05f);
        }
        if (_collision)
        {
            layers.Collisions.Add(new CollisionUser(city, _bounds.Expand(0.1f), -1));
        }
        ((GameSprite)MainData.Panel_blur_shadow).DrawHeight(layers.CitiesAO(), new VectorRectangle(offset.X - 0.5f, offset.Y - 0.2f, offset.X + _size.X + 0.5f, offset.Y + 0.4f), Color.Black * ((float)(int)color.A / 255f) * 0.8f, height);

        // END
        return false; // skip the original
    }


    public static Color OvercrowdedColor(CityUser city, Color defColor)
    {
        int ratio = city.GetTotalIndirect() * 100 / city.GetMaxIndirect();
        if (ratio > 150) return Color.DarkRed;
        else if (ratio > 100) return Color.Red;
        else if (ratio > 75) return Color.DarkOrange;
        else if (ratio > 50) return Color.Yellow;
        return defColor;
    }


    public static void DebugColors()
    {
        void Show(Color c)
        {
            var props = typeof(Color).GetProperties(BindingFlags.Public | BindingFlags.Static);
            foreach (var prop in props)
            {
                if (Equals(prop.GetValue(null), c))
                {
                    Log.Write($"{prop.Name} {c.ToString()}");
                    return;
                }
            }
        }
        Show(Color.LightYellow);
        Show(Color.Yellow);
        Show(Color.DarkOrange);
        Show(Color.OrangeRed);
        Show(Color.Red);
        Show(Color.DarkRed);
    }
}
