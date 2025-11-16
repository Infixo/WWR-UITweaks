using STM.Data;
using STM.GameWorld;
using STM.GameWorld.Users;
using STM.UI;
using STM.UI.Explorer;
using STMG.UI.Control;
using STVisual.Utility;

namespace UITweaks.UIExplorer;


public class ExplorerDestination : IExplorerItem
{
    private Button main_button;

    private Grid main_grid;

    private Image alt;

    private string country;

    private PathArrow path;

    public GameScene Scene { get; private set; }

    public CityUser City { get; private set; }

    public bool Valid { get; set; }

    public bool Selected { get; set; }

    public Label[] Labels { get; private set; }

    public Button Main_control => main_button;

    public bool Valid_default => true;

    public ExplorerDestination(CityUser city, GameScene scene)
    {
        City = city;
        Scene = scene;
        GetMainControl(scene);
        SetUpArrow(scene);
    }

    public void Update(GameScene scene, Company company)
    {
        path?.Update(scene.UI.Frame_time, scene);
        if (scene.Session.New_month)
        {
            Labels[2].Text = "<!cicon_star> " + StrConversions.CleanNumber(City.Level);
        }
    }

    public void SetSelected(bool selected)
    {
        Selected = selected;
    }

    public void SetExplorer(object explorer)
    {
    }

    public void SetAlt(bool alt)
    {
        this.alt.Opacity = (alt ? 0.05f : 0f);
    }

    public bool Smaller(IExplorerItem item, int sort_id)
    {
        ExplorerDestination _item = (ExplorerDestination)item;
        if (Valid != _item.Valid)
        {
            return Valid.CompareTo(_item.Valid) > 0;
        }
        if (sort_id == 0)
        {
            int _result3 = Labels[0].Text.CompareTo(_item.Labels[0].Text);
            if (_result3 == 0)
            {
                return country.CompareTo(_item.country) < 0;
            }
            return _result3 < 0;
        }
        if (sort_id - Labels.Length == 0)
        {
            int _result5 = Labels[0].Text.CompareTo(_item.Labels[0].Text);
            if (_result5 == 0)
            {
                return country.CompareTo(_item.country) < 0;
            }
            return _result5 > 0;
        }
        if (sort_id == 1)
        {
            int _result6 = country.CompareTo(_item.country);
            if (_result6 == 0)
            {
                _result6 = City.Level.CompareTo(_item.City.Level);
                if (_result6 == 0)
                {
                    return Labels[0].Text.CompareTo(_item.Labels[0].Text) < 0;
                }
                return _result6 > 0;
            }
            return _result6 < 0;
        }
        if (sort_id - Labels.Length == 1)
        {
            int _result4 = country.CompareTo(_item.country);
            if (_result4 == 0)
            {
                _result4 = City.Level.CompareTo(_item.City.Level);
                if (_result4 == 0)
                {
                    return Labels[0].Text.CompareTo(_item.Labels[0].Text) < 0;
                }
                return _result4 > 0;
            }
            return _result4 > 0;
        }
        if (sort_id == 2)
        {
            int _result2 = City.Level.CompareTo(_item.City.Level);
            if (_result2 == 0)
            {
                return Labels[0].Text.CompareTo(_item.Labels[0].Text) < 0;
            }
            return _result2 > 0;
        }
        if (sort_id - Labels.Length == 2)
        {
            int _result = City.Level.CompareTo(_item.City.Level);
            if (_result == 0)
            {
                return Labels[0].Text.CompareTo(_item.Labels[0].Text) > 0;
            }
            return _result < 0;
        }
        return false;
    }

    public void GetSizes(int[] sizes)
    {
        for (int i = 0; i < sizes.Length; i++)
        {
            sizes[i] = Math.Max((int)(Labels[i].GetSizeText.X + (float)(MainData.Margin_content * 2)), sizes[i]);
        }
    }

    public void SetSizes(int[] sizes)
    {
        for (int i = 0; i < sizes.Length - 1; i++)
        {
            main_grid.SetColumn(i, SizeType.Pixels, sizes[i]);
        }
    }

    public void FillCategories(FilterCategory[] categories)
    {
    }

    public bool Matches(FilterCategory[] categories, GameScene scene, Company company, CityUser city)
    {
        return true;
    }

    private void GetMainControl(GameScene scene)
    {
        Labels = new Label[3];
        int _height = 32;
        main_button = ButtonPresets.Get(new ContentRectangle(0f, 0f, 0f, _height, 1f), scene.Engine, out var _collection, null, MainData.Panel_button_hover, mouse_pass: false, MainData.Sound_button_03_press, MainData.Sound_button_03_hover);
        main_button.Opacity = 0f;
        main_button.horizontal_alignment = HorizontalAlignment.Stretch;
        main_button.OnMouseStillTime += (Action)delegate
        {
            GetTooltip(scene);
        };
        alt = new Image(ContentRectangle.Stretched, MainData.Panel_empty);
        alt.Opacity = 0f;
        _collection.Transfer(alt);
        main_grid = new Grid(ContentRectangle.Stretched, Labels.Length, 1, SizeType.Weight);
        main_grid.OnFirstUpdate += (Action)delegate
        {
            main_grid.update_children = false;
        };
        _collection.Transfer(main_grid);
        Label _name = LabelPresets.GetDefault(City.Name, scene.Engine);
        _name.Margin_local = new FloatSpace(MainData.Margin_content);
        main_grid.Transfer(_name, 0, 0);
        Labels[0] = _name;
        Country _c = City.City.GetCountry(scene);
        country = _c.Name.GetTranslation(Localization.Language);
        Label _country = LabelPresets.GetDefault("<!cicon_" + _c.ISO3166_1 + ":28> " + country, scene.Engine);
        _country.Margin_local = new FloatSpace(MainData.Margin_content);
        main_grid.Transfer(_country, 1, 0);
        Labels[1] = _country;
        Label _level = LabelPresets.GetDefault("<!cicon_star> " + StrConversions.CleanNumber(City.Level), scene.Engine);
        _level.Margin_local = new FloatSpace(MainData.Margin_content);
        _level.horizontal_alignment = HorizontalAlignment.Center;
        main_grid.Transfer(_level, 2, 0);
        Labels[2] = _level;
    }

    private void GetTooltip(GameScene scene)
    {
        GeneralTooltips.GetCity(scene.Engine, City).AddToControlRight(main_button);
    }

    private void SetUpArrow(GameScene scene)
    {
        main_button.OnMouseEnter += (Action)delegate
        {
            if (path == null)
            {
                path = new PathArrow(City, scene);
                path.strength = 2f;
            }
        };
        main_button.OnMouseLeave += (Action)delegate
        {
            path?.Destroy(scene);
            path = null;
        };
        main_button.OnClose += (Action)delegate
        {
            path?.Destroy(scene);
            path = null;
        };
    }


    // Methods that usually are in InfoUI

    private static Session? Session;

    internal static void OpenExplorer(IControl parent, Session session)
    {
        Session = session;
        ExplorerUI<ExplorerDestination> explorerUI = new ExplorerUI<ExplorerDestination>(GetCategories(), OnItemSelect, null, parent.Ui, session.Scene, 1, "ve_dest", GetItemTooltip);
        explorerUI.AddItems(GetExplorerItems);
        explorerUI.AddToControlBellow(parent);
    }

    private static string[] GetCategories()
    {
        return new string[3]
        {
        Localization.GetGeneral("name"),
        Localization.GetCity("country"),
        Localization.GetCity("level")
        };
    }

    private static void OnItemSelect(ExplorerDestination item)
    {
        item.City.Select(item.Scene, track: true);
        MainData.Sound_track.Play();
        item.Scene.tracking = item.City;
    }

    private static void GetItemTooltip(IControl parent, int id)
    {
        TooltipPreset _tooltip = null;
        switch (id)
        {
            case 0:
                _tooltip = GeneralTooltips.GetCity(Session?.Scene.Engine);
                break;
            case 2:
                _tooltip = GeneralTooltips.GetCity(Session?.Scene.Engine);
                break;
        }
        _tooltip?.AddToControlBellow(parent);
    }

    private static GrowArray<ExplorerDestination> GetExplorerItems()
    {
        GrowArray<ExplorerDestination> _result = new GrowArray<ExplorerDestination>();
        if (Session == null) return _result;
        GrowArray<City> _cities = Session.Scene.Cities;
        for (int i = 0; i < _cities.Count; i++)
        {
            if (_cities[i].User.Level > 0)
            {
                _result.Add(new ExplorerDestination(_cities[i].User, Session.Scene));
            }
        }
        return _result;
    }
}
