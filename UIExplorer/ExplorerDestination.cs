using STM.Data;
using STM.GameWorld;
using STM.GameWorld.Users;
using STM.UI;
using STM.UI.Explorer;
using STMG.UI.Control;
using STMG.Utility;
using STVisual.Utility;
using Utilities;

namespace UITweaks.UIExplorer;


public class ExplorerDestination : IExplorerItem
{
    private Button main_button;

    private Grid main_grid;

    private Image alt;

    private string name;

    private PathArrow? path;

    public GameScene Scene { get; private set; }

    public CityUser City { get; private set; }

    public List<CityUser> Origins { get; private set; }

    public bool Valid { get; set; }

    public bool Selected { get; set; }

    public Label[] Labels { get; private set; }

    public Button Main_control => main_button;

    public bool Valid_default => true;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public ExplorerDestination(CityUser city, List<CityUser> origins, GameScene scene)
    {
        Scene = scene;
        City = city;
        Origins = origins;
        GetMainControl(scene);
        SetUpArrow(scene);
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public void Update(GameScene scene, Company company)
    {
        path?.Update(scene.UI.Frame_time, scene);
        if (scene.Session.New_month)
        {
            Labels[1].Text = $"<!cicon_star> {City.Level}";
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
            return Valid.CompareTo(_item.Valid) > 0;
        int result = 0;
        switch (sort_id % Labels.Length)
        {
            case 0: // name
                result = _item.name.CompareTo(name);
                break;
            case 1: // level
                result = City.Level.CompareTo(_item.City.Level);
                break;
            case 2: // count
                result = Origins.Count.CompareTo(_item.Origins.Count);
                break;
        }

        // Fail-safe
        if (result == 0)
            result = _item.name.CompareTo(name);

        return sort_id < Labels.Length ? result > 0 : result < 0;
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
        // 0 Name & Country
        string _country = City.GetCountryName(scene);
        name = City.Name + " " + _country;
        Label _name = LabelPresets.GetDefault($"{City.GetNameWithIcon(scene)} <!#{(LabelPresets.Color_main * 0.75f).GetHex()}>({_country})", scene.Engine);
        _name.Margin_local = new FloatSpace(MainData.Margin_content);
        main_grid.Transfer(_name, 0, 0);
        Labels[0] = _name;
        // 1 Level
        Label _level = LabelPresets.GetDefault($"<!cicon_star> {City.Level}", scene.Engine);
        _level.Margin_local = new FloatSpace(MainData.Margin_content);
        _level.horizontal_alignment = HorizontalAlignment.Center;
        main_grid.Transfer(_level, 1, 0);
        Labels[1] = _level;
        // 2 Count
        Label _count = LabelPresets.GetDefault(Origins.Count.ToString(), scene.Engine);
        _count.Margin_local = new FloatSpace(MainData.Margin_content);
        _count.horizontal_alignment = HorizontalAlignment.Center;
        main_grid.Transfer(_count, 2, 0);
        Labels[2] = _count;
    }

    private void GetTooltip(GameScene scene)
    {
        TooltipPreset tt = TooltipPreset.Get(Localization.GetCity("cities"), scene.Engine);
        foreach(CityUser city in Origins.OrderBy(x => x.City.Country_id).ThenBy(x => x.Name))
            tt.AddStatsLine(city.GetNameWithIcon(scene), city.GetCountryName(scene));
        tt.AddToControlRight(main_button);
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
        /* performance test
        WorldwideRushExtensions.CounterIsConn = 0;
        WorldwideRushExtensions.CounterGetLine0 = 0;
        WorldwideRushExtensions.CounterGetLine1 = 0;
        WorldwideRushExtensions.CounterGetLine2 = 0;
        WorldwideRushExtensions.CounterGetLine3 = 0;
        WorldwideRushExtensions.CounterGetPath = 0;
        Stopwatch sw = new();
        sw.Reset(); sw.Start();
        */
        Session = session;
        ExplorerUI<ExplorerDestination> explorerUI = new ExplorerUI<ExplorerDestination>(GetCategories(), OnItemSelect, null, parent.Ui, session.Scene, 2, "ve_dest", GetItemTooltip);
        explorerUI.AddItems(GetExplorerItems);
        explorerUI.AddToControlBellow(parent);
        /* performance test
        sw.Stop();
        Log.Write($"Elapsed time: {sw.ElapsedMilliseconds} ms, IC={WorldwideRushExtensions.CounterIsConn} GP={WorldwideRushExtensions.CounterGetPath}");
        Log.Write($"Counters: 0= {WorldwideRushExtensions.CounterGetLine0} 1={WorldwideRushExtensions.CounterGetLine1} 2={WorldwideRushExtensions.CounterGetLine2} 3={WorldwideRushExtensions.CounterGetLine3} ");
        */
    }

    private static string[] GetCategories()
    {
        return
        [
        Localization.GetGeneral("name"), // 0
        Localization.GetCity("level"), // 1
        "<!cicon_city>", // 2
        ];
    }

    private static void OnItemSelect(ExplorerDestination item)
    {
        item.City.Select(item.Scene, track: true);
        MainData.Sound_track.Play();
        item.Scene.tracking = item.City;
    }

    private static void GetItemTooltip(IControl parent, int id)
    {
        TooltipPreset? _tooltip = null;
        switch (id)
        {
            case 0:
                _tooltip = GeneralTooltips.GetCity(Session?.Scene.Engine);
                break;
            case 2:
                _tooltip = TooltipPreset.Get("Number of origin cities.", Session?.Scene.Engine);
                break;
        }
        _tooltip?.AddToControlBellow(parent);
    }


    private static GrowArray<ExplorerDestination> GetExplorerItems()
    {
        GrowArray<ExplorerDestination> _result = new GrowArray<ExplorerDestination>();
        if (Session == null) return _result;
        // Process all origin cities
        ushort _player = Session.Player;
        GrowArray<City> _cities = Session.Scene.Cities;
        Dictionary<CityUser, List<CityUser>> _dests = [];
        GrowArray<City[]> _allPaths = new();
        bool _allPathsDirty = true;
        for (int i = 0; i < _cities.Count; i++)
        {
            CityUser _city = _cities[i].User;
            _allPaths.Clear();
            _allPathsDirty = true;
            if (_city.Level > 0 && _city.Trust.GetTrust(_player) > 0)
                // Process all destinations
                for (int j = 0; j < _city.Destinations.Items.Count; j++)
                {
                    CityDestination _dest = _city.Destinations.Items[j];
                    if (_dest.Percent == 0)
                    {
                        if (_allPathsDirty)
                        {
                            _allPaths = WorldwideRushExtensions.GetAllStoredPaths(_city.Routes, _player);
                            _allPathsDirty = false;
                        }
                        CityUser _destCity = _dest.Destination.User;
                        if (!_city.IsConnected(_destCity, _player, _allPaths, Session.Scene))
                            if (!_dests.TryAdd(_destCity, new List<CityUser> { _city }))
                                _dests[_destCity].Add(_city);
                    }
                }
        }
        // Compile the end result array
        foreach(var pair in _dests)
            _result.Add(new ExplorerDestination(pair.Key, pair.Value, Session.Scene));
        return _result;
    }
}
