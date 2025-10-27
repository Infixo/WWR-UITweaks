using Microsoft.Xna.Framework;
using STM.Data;
using STM.GameWorld;
using STM.GameWorld.Users;
using STM.UI;
using STM.UI.Floating;
using STMG.UI;
using STMG.UI.Control;
using STMG.UI.Utility;
using STMG.Utility;
using STVisual.Utility;
using Utilities;

namespace UITweaks.UIFloating;


public class CityCluster
{
    public int ID { get; private set; }

    public CityUser MainCity { get; private set; }

    public CityUser[] Cities { get; private set; }

    public CityCluster(List<CityUser> cities, int id, List<CityUser> mains, ushort player = 0)
    {
        if (cities.Count == 0)
            throw new ArgumentException("City cluster cannot be empty.");
        Cities = [.. cities];
        ID = id;
        // Set MainCity, try to find in cache, then hubs
        CityUser? _main = cities.Intersect(mains).FirstOrDefault();
        if (_main == null)
        {
            //MainCity = Cities.Where(x => x.GetHub(player) != null).MaxBy(x => x.Level)!;
            //MainCity ??= Cities.MaxBy(x => x.Level)!;
            MainCity = Cities[0];
        }
        else
            MainCity = _main;
    }

    public bool Contains(CityUser city) => Cities.Contains(city);

    public override string ToString()
    {
        string _result = $"({ID}).";
        for (int i = 0; i < Cities.Length; i++)
            _result += Cities[i].ToString() + ".";
        return _result;
    }

    internal string GetNames(GameScene scene)
    {
        return String.Join(" ", Cities.Select(x => (x == MainCity ? "<!cicon_ship_b>" : "") + x.Name));
    }

    internal void ChangeMainCity()
    {
        int i = 0;
        for (; i < Cities.Length; i++)
            if (Cities[i] == MainCity)
                break;
        MainCity = Cities[ (i+1) % Cities.Length];
    }
}


internal class InterestUI : IFloatUI
{
    // Origin cities
    private readonly List<CityUser> Cities;
    private CityUser? MainCity;
    private ControlCollection citiesCollection;
    private readonly Dictionary<CityUser, Grid> citiesItems;
    private ScrollPreset? scrollPreset = null;

    // City clusters
    private readonly List<CityCluster> Clusters;
    private ControlCollection clustersCollection;
    private ScrollSettings clustersScroll;
    private readonly Dictionary<CityCluster, Grid> clustersItems;
    private CityCluster? selected;
    private readonly List<CityUser> MainCities;

    // Arrows
    private readonly GrowArray<PathArrow> arrows;

    // Parameters
    // TODO: History to remember changes
    private double epsilon = 200.0; // max distance to search for neighbors
    private int minCities = 3; // min number of cities (inc. the city) in the neighborhood to be assigned to a cluster


    internal InterestUI(CityUser city, GameScene scene) : base(null, scene)
    {
        // Origin cities
        Cities = [city];
        MainCity = city;
        citiesCollection = new ControlCollection(ContentRectangle.Zero);
        //citiesScrollSettings = new ScrollSettings();
        citiesItems = [];

        // City clusters
        Clusters = [];
        clustersCollection = new ControlCollection(ContentRectangle.Zero);
        clustersScroll = new ScrollSettings();
        clustersItems = [];
        selected = null;
        MainCities = [];

        // Arrows
        arrows = new GrowArray<PathArrow>();

        // Build
        Construct();
    }

    private void Construct()
    {
        AddHeader("Route Planner");
        GetOriginCities();
        EndBack();
        StartBack();
        GetCityClusters();
        Finalize();
        SetPinned(true);
        // events
        base.Main_control.OnUpdate += new Action(Update);
        base.Main_control.OnClose += new Action(DestroyArrows);
    }

    private new void Finalize(int width = 600)
    {
        EndBack();
        base.Main_control.Size_local = new Vector2(width, this.GetPrivateField<int>("height"));
    }

    public static void OpenRegisterCity(CityUser city, GameScene scene)
    {
        // Try to find already open window and add the city to it
        for (int i = 0; i < scene.Selection.Floaters.Count; i++)
            if (scene.Selection.Floaters[i] is InterestUI ui)
            {
                if (ui.Cities.Contains(city))
                    MainData.Sound_error.Play();
                else
                {
                    // Register new city
                    ui.Cities.Add(city);
                    ui.UpdateOriginCities(scrollToLast: true);
                    ui.GenerateClusters();
                    ui.UpdateCityClusters();
                }
                return;
            }

        // Not open - create a new one
        scene.Selection.AddUI(new InterestUI(city, scene));
    }


    // ORIGIN CITIES SECTION

    private const float CitiesScrollHeight = 200f;

    private void GetOriginCities()
    {
        // List of cities
        citiesCollection = new ControlCollection(ContentRectangle.Zero);
        citiesCollection.horizontal_alignment = HorizontalAlignment.Stretch;
        ScrollSettings _settings = ContentPreset.GetScrollSettingsNoMargin();
        //_settings.history = "c_" + City.City.City_id + "ts";
        //IControl _scroll = ScrollPreset.GetVertical(ContentRectangle.Stretched, citiesControlCollection, _settings); // oringal call
        //new ScrollPreset(rectangle, child, settings, ScrollType.Vertical, on_scroll).main_control; // GetVertical
        // ScrollPreset has private constructor and we need to store it to later access the vertical slider
        scrollPreset = typeof(ScrollPreset).CallPrivateConstructor<ScrollPreset>(
            [typeof(ContentRectangle), typeof(IControl), typeof(ScrollSettings), typeof(ScrollPreset.ScrollType), typeof(Action)], // arg types
            [new ContentRectangle(0f, 0f, 0f, CitiesScrollHeight, 1f), citiesCollection, _settings, ScrollPreset.ScrollType.Vertical, null!]); // Action on_scroll is null
        IControl _scroll = scrollPreset.GetPrivateField<ControlContainer>("main_control");
        //_grid.Transfer(_scroll, 0, 1);
        _scroll.horizontal_alignment = HorizontalAlignment.Stretch;
        _scroll.Margin_local = new FloatSpace(MainData.Margin_content_items, 0f);
        AddControl(_scroll, "origin_cities");
        UpdateOriginCities();
    }

    private void UpdateOriginCities(bool scrollToLast = false)
    {
        //UpdateAllItems();
        //items.Sort(show_travelers ? new Func<TravelersDest, TravelersDest, bool>(Compare) : new Func<TravelersDest, TravelersDest, bool>(CompareTraffic));
        float _y = 0;
        foreach (CityUser city in Cities)
        {
            IControl cityItem = GetOriginCityItem(city);
            Animation _animation = AnimationPresets.Opacity(1f, 0.2f);
            _animation.Add(AnimationPresets.Location(new Vector2(0f, _y), 0.2f));
            cityItem.SetAnimation(_animation);
            _y += cityItem.Size_local_total.Y;
        }
        citiesCollection.Size_local = new Vector2(0f, _y);
        //citiesControlCollection.Parent.Parent.dirty_size = true; // When this is true, then the slider always goes to the 1st city
        if (scrollToLast)
        {
            float _pos = CitiesScrollHeight - MainData.Size_button - _y;
            if (_pos < 0)
            {
                ScrollSlider _slider = scrollPreset!.GetPrivateField<ScrollSlider>("vertical");
                _slider.CallPrivateMethodVoid("SetScrollPosY", [_pos]); // Position is the top of the child area relative to the view window, so when it is scrolled down, top is hidden and PosY < 0
            }
        }
    }

    private IControl GetOriginCityItem(CityUser city) //, ref float y)
    {
        // Cache
        if (citiesItems.TryGetValue(city, out Grid? item))
        {
            //y += item.Size_local.Y;
            return item;
        }

        // Grid
        Grid _grid = new Grid(new ContentRectangle(0f, 0f, 0f, MainData.Size_button, 1f), 5, 1, SizeType.Weight);
        _grid.horizontal_alignment = HorizontalAlignment.Stretch;
        _grid.Margin_local = new FloatSpace(MainData.Margin_content_items);
        _grid.SetCloseAnimation(AnimationPresets.Opacity(0f, 0.2f));
        citiesCollection.Transfer(_grid);
        citiesItems.Add(city, _grid);

        // Grid layout
        // 0 button
        _grid.SetColumn(1, SizeType.Pixels, MainData.Margin_content_items);
        _grid.SetColumn(2, SizeType.Pixels, MainData.Size_button); // add more cities
        _grid.SetColumn(3, SizeType.Pixels, MainData.Margin_content_items);
        _grid.SetColumn(4, SizeType.Pixels, MainData.Size_button); // delete

        // Name and country on a button-style panel
        Button _button = ButtonPresets.GetGeneral(ContentRectangle.Stretched, Scene.Engine, out ControlCollection _content);
        _grid.Transfer(_button, 0, 0);
        Label _labelN = LabelPresets.GetDefault(city.GetNameWithIcon(Scene) + $" <!cicon_star>{city.Level}", Scene.Engine);
        _labelN.horizontal_alignment = HorizontalAlignment.Left;
        _labelN.Margin_local = new FloatSpace(MainData.Margin_content_items);
        _grid.Transfer(_labelN, 0, 0);
        Label _labelC = LabelPresets.GetDefault(city.GetCountryName(Scene), Scene.Engine, LabelPresets.Color_stats);
        _labelC.horizontal_alignment = HorizontalAlignment.Right;
        _labelC.Margin_local = new FloatSpace(MainData.Margin_content_items);
        _grid.Transfer(_labelC, 0, 0 );
        if (city == MainCity)
            _labelN.Color = _labelC.Color = LabelPresets.Color_positive;

        _button.OnMouseStillTime += () => TooltipPreset.Get("Click to set main origin city.", Scene.Engine).AddToControlBellow(_button);
        _button.OnButtonPress += (Action)delegate
        {
            if (city != MainCity)
            {
                ColorMainCity(MainCity, LabelPresets.Color_main); // clear previous main city
                ColorMainCity(MainCity = city, LabelPresets.Color_positive); // mark new main city
                if (selected != null)
                    GenerateArrows(selected);
            }
        };

        // Add more cities
        Button _addMore = ButtonPresets.IconGeneral(ContentRectangle.Stretched, MainData.Icon_duplicate, Scene.Engine).Control;
        _grid.Transfer(_addMore, 2, 0);
        _addMore.OnMouseStillTime += () => TooltipPreset.Get("Add neighborhood cities from the country.", Scene.Engine).AddToControlBellow(_addMore);
        _addMore.OnButtonPress += (Action)delegate
        {
            // Find neighbors
            byte _countryId = city.City.Country_id;
            foreach (City other in Scene.Countries[_countryId].Cities)
                if (GameScene.GetDistance(city.Location, other.Location) < epsilon &&
                    !Cities.Contains(other.User))
                    Cities.Add(other.User);
            UpdateOriginCities(scrollToLast: true);
            GenerateClusters();
            UpdateCityClusters();
        };

        // Delete
        Button _delete = ButtonPresets.IconClose(ContentRectangle.Stretched, MainData.Icon_trash, Scene.Engine).Control;
        _grid.Transfer(_delete, 4, 0);
        _delete.OnButtonPress += (Action)delegate
        {
            citiesItems[city].CloseWithAnimation(close_if_no_animation: true);
            citiesItems.Remove(city);
            Cities.Remove(city);
            if (MainCity == city)
            {
                MainCity = (Cities.Count > 0 ? Cities[0] : null);
                ColorMainCity(MainCity, LabelPresets.Color_positive);
            }
            UpdateOriginCities();
            GenerateClusters();
            UpdateCityClusters();
        };

        return _grid; ;

        // Helper
        void ColorMainCity(CityUser? city, Color color)
        {
            if (city != null)
            {
                ((Label)citiesItems[city][1]).Color = color;
                ((Label)citiesItems[city][2]).Color = color;
            }
        }
    }


    // CLUSTERS SECTION

    /// <summary>
    /// Returns a ContentRectangle sized via MainData.Size_button
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public static ContentRectangle GetButtonRectangle(int width = 1, int height = 1)
    {
        return new ContentRectangle(0f, 0f, width * MainData.Size_button, height * MainData.Size_button, 1f);
    }

    private const float ClustersScrollHeight = 500f;

    // CITY CLUSTERS UI
    // title [up][dn] epsilon [up][dn] min cities [show-all]
    // [scroll with items]
    private void GetCityClusters()
    {
        // Grid
        Grid _grid = new Grid(new ContentRectangle(0f, 0f, 0f, MainData.Size_button, 1f), 8, 1, SizeType.Weight);
        _grid.horizontal_alignment = HorizontalAlignment.Stretch;
        _grid.Margin_local = new FloatSpace(MainData.Margin_content); //, MainData.Margin_content, MainData.Margin_content_items);
        AddControl(_grid, "clusters_controls");
        // layout
        // 0 title
        _grid.SetColumn(1, SizeType.Pixels, MainData.Size_button);
        _grid.SetColumn(2, SizeType.Pixels, MainData.Size_button);
        _grid.SetColumn(3, SizeType.Pixels, MainData.Size_button * 3); // epsilon label
        _grid.SetColumn(4, SizeType.Pixels, MainData.Size_button);
        _grid.SetColumn(5, SizeType.Pixels, MainData.Size_button);
        _grid.SetColumn(6, SizeType.Pixels, MainData.Size_button * 2); // min cities label
        _grid.SetColumn(7, SizeType.Pixels, MainData.Size_button);

        // Title
        Label _title = LabelPresets.GetBold("City clusters", Scene.Engine);
        //_title.horizontal_alignment = HorizontalAlignment.Center;
        _title.Margin_local = new FloatSpace(MainData.Margin_content_items, 0f);
        _grid.Transfer(_title, 0, 0);

        // Epsilon controls

        Label _epsilonLabel = LabelPresets.GetBold(StrConversions.GetDistance(epsilon), Scene.Engine, LabelPresets.Color_stats);
        _epsilonLabel.horizontal_alignment = HorizontalAlignment.Center;
        _epsilonLabel.Margin_local = new FloatSpace(MainData.Margin_content_items, 0f);
        _grid.Transfer(_epsilonLabel, 3, 0);

        Button _buttonEpsUp = ButtonPresets.TextGeneral(GetButtonRectangle(), "<!cicon_up>", Scene.Engine).Control;
        _grid.Transfer(_buttonEpsUp, 1, 0);
        _buttonEpsUp.OnButtonPress += (Action)delegate
        {
            if (epsilon < 500d)
            {
                epsilon += 50d;
                _epsilonLabel.Text = StrConversions.GetDistance(epsilon);
                RefreshClusters();
            }
            else
                MainData.Sound_error.Play();
        };

        Button _buttonEpsDn = ButtonPresets.TextGeneral(GetButtonRectangle(), "<!cicon_down>", Scene.Engine).Control;
        _grid.Transfer(_buttonEpsDn, 2, 0);
        _buttonEpsDn.OnButtonPress += (Action)delegate
        {
            if (epsilon > 50d)
            {
                epsilon -= 50d;
                _epsilonLabel.Text = StrConversions.GetDistance(epsilon);
                RefreshClusters();
            }
            else
                MainData.Sound_error.Play();
        };

        // Min cities controls

        Label _minCitiesLabel = LabelPresets.GetBold($"<!cicon_city> {minCities}", Scene.Engine, LabelPresets.Color_stats);
        _minCitiesLabel.horizontal_alignment = HorizontalAlignment.Center;
        _minCitiesLabel.Margin_local = new FloatSpace(MainData.Margin_content_items, 0f);
        _grid.Transfer(_minCitiesLabel, 6, 0);

        Button _buttonMinUp = ButtonPresets.TextGeneral(GetButtonRectangle(), "<!cicon_up>", Scene.Engine).Control;
        _grid.Transfer(_buttonMinUp, 4, 0);
        _buttonMinUp.OnButtonPress += (Action)delegate
        {
            if (minCities < 9)
            {
                minCities += 1;
                _minCitiesLabel.Text = $"<!cicon_city> {minCities}";
                RefreshClusters();
            }
            else
                MainData.Sound_error.Play();
        };

        Button _buttonMinDn = ButtonPresets.TextGeneral(GetButtonRectangle(), "<!cicon_down>", Scene.Engine).Control;
        _grid.Transfer(_buttonMinDn, 5, 0);
        _buttonMinDn.OnButtonPress += (Action)delegate
        {
            if (minCities > 2)
            {
                minCities -= 1;
                _minCitiesLabel.Text = $"<!cicon_city> {minCities}";
                RefreshClusters();
            }
            else
                MainData.Sound_error.Play();
        };


        // Sort button
        Button _sort = ButtonPresets.IconGreen(new ContentRectangle(0f, 0f, MainData.Size_button, MainData.Size_button, 1f), MainData.Icon_cogwheel, Scene.Engine).Control;
        _sort.horizontal_alignment = HorizontalAlignment.Center;
        _sort.vertical_alignment = VerticalAlignment.Center;
        _sort.Margin_local = new FloatSpace(0f); //, 0f);
        _grid.Transfer(_sort, 7, 0);

        // Scroll area
        clustersCollection = new ControlCollection(ContentRectangle.Zero);
        clustersCollection.horizontal_alignment = HorizontalAlignment.Stretch;
        clustersScroll = ContentPreset.GetScrollSettingsNoMargin();
        //clustersScroll.history = "c_" + City.City.City_id + "ds";
        IControl _scroll = ScrollPreset.GetVertical(new ContentRectangle(0f, 0f, 0f, ClustersScrollHeight, 1f), clustersCollection, clustersScroll);
        _scroll.horizontal_alignment = HorizontalAlignment.Stretch;
        _scroll.Margin_local = new FloatSpace(0f, 0f, MainData.Margin_content_items, 0f);
        AddControl(_scroll, "destinations");

        RefreshClusters(); // initial city
    }

    private void RefreshClusters()
    {
        GenerateClusters();
        UpdateCityClusters();
    }

    private void UpdateCityClusters()
    {
        clustersItems.Clear();
        clustersCollection.Clear();
        float _y = 0f;
        foreach(CityCluster cluster in Clusters)
        {
            AddCityClusterItem(cluster, ref _y); //, City.Last_destination <= m);
        }
        clustersCollection.Size_local = new Vector2(0f, _y);
        clustersCollection.Parent.Parent.dirty_size = true;

        selected = null;
        if (Clusters.Count > 0)
            selected = Clusters.Where(x => x.ID > 0).MaxBy(x => x.Cities.Length) ?? Clusters[0];
        if (selected != null)
        {
            ((Label)clustersItems[selected][1]).Color = LabelPresets.Color_positive;
            GenerateArrows(selected);
        }
        else DestroyArrows();
    }

    private void AddCityClusterItem(CityCluster cluster, ref float y)
    {
        // Main button with the city name TODO: this will be main "cluster" button
        // Design:       [name,country     count] [on/off ] (arrows toggle)
        //               [city names (scrolling)] [analyze]

        // Grid 2x2
        Grid _grid = new Grid(new ContentRectangle(0f, y, 0f, MainData.Size_button * 2, 1f)/*ContentRectangle.Stretched*/, 2, 2, SizeType.Weight);
        _grid.horizontal_alignment = HorizontalAlignment.Stretch;
        _grid.Margin_local = new FloatSpace(0f, MainData.Margin_content_items);
        //_grid.Opacity = 1f;
        _grid.SetColumn(1, SizeType.Pixels, MainData.Size_button);
        //_content.Transfer(_grid);
        clustersCollection.Transfer(_grid);
        y += _grid.Size_local_total.Y;
        clustersItems.Add(cluster, _grid);

        // Hover button
        Button _button = ButtonPresets.GetBlack(ContentRectangle.Stretched, Scene.Engine, out ControlCollection _content);
        //_button.Margin_local = new FloatSpace(0f); // MainData.Margin_content_items / 2, MainData.Margin_content_items / 2);
        _grid.Transfer(_button, 0, 0, 1, 2);

        // Show arrows
        _button.OnButtonPress += (Action)delegate
        {
            if (selected != null && selected.ID == cluster.ID)
            {
                selected = null;
                DestroyArrows();
                ((Label)_grid[1]).Color = LabelPresets.Color_main;
            }
            else
            {
                if (selected != null)
                    ((Label)clustersItems[selected][1]).Color = LabelPresets.Color_main;
                selected = cluster;
                ((Label)_grid[1]).Color = LabelPresets.Color_positive;
                GenerateArrows(cluster);
            }
        };

        // Show briefly if not selected
        _button.OnMouseStillTime += (Action)delegate
        {
            GetClusterTooltip(_grid, cluster);
            if (selected == null)
                GenerateArrows(cluster);
        };

        _button.OnMouseLeave += (Action)delegate
        {
            if (selected == null)
                DestroyArrows();
        };

        // City name
        Label _name = LabelPresets.GetBold("name", Scene.Engine);
        _name.horizontal_alignment = HorizontalAlignment.Left;
        _name.Margin_local = new FloatSpace(MainData.Margin_content_items);
        _grid.Transfer(_name, 0, 0);
        if (selected == cluster)
            _name.Color = LabelPresets.Color_positive;

        // Num cites in the cluster
        Label _count = LabelPresets.GetBold(cluster.Cities.Length.ToString(), Scene.Engine, LabelPresets.Color_stats);
        _count.horizontal_alignment = HorizontalAlignment.Right;
        _count.Margin_local = new FloatSpace(MainData.Margin_content_items);
        _grid.Transfer(_count, 0, 0);

        // Select
        ButtonItem _select = ButtonPresets.IconBlack(ContentRectangle.Stretched, MainCities.Contains(cluster.MainCity) ? MainData.Icon_toggle_on : MainData.Icon_toggle_off, Scene.Engine);
        _grid.Transfer(_select.Control, 1, 0);
        _select.Control.OnButtonPress += (Action)delegate
        {
            if (MainCities.Contains(cluster.MainCity))
            {
                MainCities.Remove(cluster.MainCity);
                _select.Icon.Graphics = MainData.Icon_toggle_off;
            }
            else
            {
                MainCities.Add(cluster.MainCity);
                _select.Icon.Graphics = MainData.Icon_toggle_on;
            }
        };
        _select.Control.OnMouseStillTime += () => TooltipPreset.Get("Mark as cluster's main city", Scene.Engine).AddToControlRight(_select.Control);

        // Bottom row - list of cities in the cluster
        Label _cluster = LabelPresets.GetDefault(cluster.GetNames(Scene), Scene.Engine);
        _cluster.Margin_local = new FloatSpace(MainData.Margin_content_items);
        IControl _radio = LabelPresets.GetRadio(_cluster, 500, margin: new FloatSpace(MainData.Margin_content_items)); // TODO: dynamic width?
        _radio.Mouse_visible = false;
        _grid.Transfer(_radio, 0, 1);

        // Change main city
        Button _change = ButtonPresets.TextBlack(ContentRectangle.Stretched, "<!cicon_fast>", Scene.Engine).Control;
        _grid.Transfer(_change, 1, 1);
        _change.OnButtonPress += (Action)delegate
        {
            cluster.ChangeMainCity();
            UpdateClusterInfo();
            if (selected != null && selected.ID == cluster.ID)
                GenerateArrows(cluster);
        };
        _change.OnMouseStillTime += () => TooltipPreset.Get("Change cluster's main city", Scene.Engine).AddToControlRight(_change);

        // Helper
        void UpdateClusterInfo()
        {
            string _text = cluster.MainCity.GetNameWithIcon(Scene)
                + $" <!cicon_star>{cluster.MainCity.Level}"
                + $" <!#{(LabelPresets.Color_main * 0.75f).GetHex()}>({cluster.MainCity.City.GetCountry(Scene).Name.GetTranslation(Localization.Language)})";
            _name.Text = cluster.ID > 0 ? _text : "Singles";
            _cluster.Text = cluster.GetNames(Scene);
            _select.Icon.Graphics = MainCities.Contains(cluster.MainCity) ? MainData.Icon_toggle_on : MainData.Icon_toggle_off;
        }
        UpdateClusterInfo(); // get current data
    }

    private void GetClusterTooltip(IControl control, CityCluster cluster)
    {
        TooltipPreset _tooltip = TooltipPreset.Get("City cluster", Scene.Engine, can_lock: true);
        int _alt = 0;
        foreach (CityUser city in cluster.Cities
            .OrderBy(c => c.City.GetCountry(Scene).Name.GetTranslation(Localization.Language))
            .ThenBy(c => c.Name))
            _tooltip.AddStatsLine(
                city.GetNameWithIcon(Scene) + $" <!cicon_star>{city.Level}",
                (city.GetHub(Scene.Session.Player) != null ? "<!cicon_storage> " : "") + city.GetCountryName(Scene),
                (_alt++ & 1) == 1);
        _tooltip.AddToControlRight(control);
    }


    // ARROWS SECTION

    private void GenerateArrows(CityCluster cluster)
    {
        DestroyArrows();
        if (MainCity == null || Cities.Count == 0) return;
        // Origin cities connections
        foreach (CityUser cc in Cities.Where(x => x != MainCity))
            GenerateArrow(cc, MainCity, true);
        // Singles
        if (cluster.ID == -1)
        {
            foreach (CityUser cc in cluster.Cities)
                GenerateArrow(cc, MainCity, true);
            return;
        }
        // MainCity to origin cities
        foreach (CityUser city in Cities)
            GenerateArrow(city, cluster.MainCity, false);
        // Cluster connections
        foreach (CityUser cc in cluster.Cities.Where(x => x != cluster.MainCity))
            GenerateArrow(cc, cluster.MainCity, true);
    }

    private void AddArrow(CityUser origin, CityUser destination, Color color)
    {
        arrows.Add(new PathArrow(origin, destination, new NewRouteSettings(-1), Scene));
        arrows.Last.color = color;
    }

    private readonly Color ConnectionColor = new(95, 207, 63); // Green route and interest - best combo
    private readonly Color InterestColor = Color.Pink; // there is interest between cities, but no direct route
    private readonly Color DirectRouteColor = new(95, 159, 255); // Blue there is route, but no interest
    private readonly Color NoConnectionColor = new(255, 127, 95); // pinky-red cities in a cluster not connected to the MainCity directly
    // TODO: later could add indirect too

    // This must called to toggle on/off arrows on a cluster
    private void GenerateArrows(CityUser city, CityCluster cluster)
    {
        // Arrows to the cluster and two-ways
        for (int j = 0; j < city.Destinations.Items.Count; j++)
        {
            CityUser _dest = city.Destinations.Items[j].Destination.User;
            if (cluster.Contains(_dest))
            {
                arrows.Add(new PathArrow(city, _dest, new NewRouteSettings(-1), Scene));
                arrows.Last.color = ConnectionColor;
                //arrows.Last.strength = city.Destinations.Items[j].Level; // default is 1
                int _id = city.Cities_interest.Find(_dest);
                if (_id >= 0)
                {
                    arrows.Last.color = DirectRouteColor;
                    arrows.Add(new PathArrow(_dest, city, new NewRouteSettings(-1), Scene));
                    arrows.Last.color = DirectRouteColor;
                }
            }
        }
        // Arrows from the cluster
        for (int i = 0; i < city.Cities_interest.Count; i++)
        {
            CityUser _intr = city.Cities_interest[i];
            if (cluster.Contains(_intr) && !city.Destinations.CallPrivateMethod<bool>("Contains", [_intr.City]))
            {
                arrows.Add(new PathArrow(_intr, city, new NewRouteSettings(-1), Scene));
                arrows.Last.color = InterestColor;
            }
        }
    }


    private void DestroyArrows()
    {
        for (int i = 0; i < arrows.Count; i++)
        {
            arrows[i].Destroy(Scene);
        }
        arrows.Clear();
    }


    /// <summary>
    /// Generates a one- or two-way arrow, with a color saying if cities are connected via a direct route.
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="destination"></param>
    private void GenerateArrow(CityUser origin, CityUser destination, bool showNoConnection = false)
    {
        // Helper - see if there is a specific city in destinations
        static bool Contains(CityUser city, CityUser dest)
        {
            for (int i = 0; i < city.Destinations.Items.Count; i++)
                if (city.Destinations.Items[i].Destination == dest.City)
                    return true;
            return false;
        }
        bool originToDest = Contains(origin, destination);
        bool destToOrigin = Contains(destination, origin);

        // Check if there is a route between them
        bool directRoute = false;
        for (int r = 0; !directRoute && r < origin.Routes.Count; r++)
            for (int i = 0; !directRoute && i < origin.Routes[r].Instructions.Cities.Length; i++)
                if (origin.Routes[r].Instructions.Cities[i] == destination)
                    directRoute = true;

        // Decide if shown
        if (!(showNoConnection || directRoute || originToDest || destToOrigin))
            return;

        // Decide color and build 1 or 2 arrows
        Color arrowColor = (originToDest || destToOrigin)
            ? (directRoute ? ConnectionColor : InterestColor)
            : (directRoute ? DirectRouteColor : NoConnectionColor);
        if (destToOrigin)
            AddArrow(destination, origin, arrowColor);
        else // if (originToDest)
            AddArrow(origin, destination, arrowColor); // we need an arrow to show no-connection status
    }

    private void Update()
    {
        if (base.Main_control.Closing || base.Main_control.Ui == null)
        {
            for (int i = 0; i < arrows.Count; i++)
            {
                arrows[i].strength *= base.Main_control.Opacity;
            }
            return;
        }
        // Animate arrows
        for (int i = 0; i < arrows.Count; i++)
            arrows[i].Update(Scene.Session.Delta, Scene);
    }


    // GENERATE CLUSTERS - CORE DBSCAN ALGORITHM
    private void GenerateClusters()
    {
        Clusters.RemoveAll(x => true);

        // get all unique cities
        HashSet<CityUser> destinations = [];
        foreach (CityUser city in Cities)
        {
            for (int j = 0; j < city.Destinations.Items.Count; j++)
                destinations.Add(city.Destinations.Items[j].Destination.User);
            for (int i = 0; i < city.Cities_interest.Count; i++)
                destinations.Add(city.Cities_interest[i]);
        }
        destinations.ExceptWith(Cities);

        // Call DBSCAN and create clusters
        var dbscan = new DBSCAN(epsilon, minCities);
        var pointClusters = dbscan.Fit(destinations);
        foreach (List<DBSCAN.Point> pointCluster in pointClusters)
            Clusters.Add(new CityCluster([.. pointCluster.OrderByDescending(x => x.Neighbors.Count).Select(x => x.City)], pointCluster[0].ClusterID, MainCities));

        // Exclude singles temporarily
        int singlesIndex = Clusters.FindIndex(x => x.ID == -1);
        CityCluster? singles = singlesIndex < 0 ? null : Clusters[singlesIndex];
        if (singlesIndex >= 0)
            Clusters.RemoveAt(singlesIndex);

        // Sorting by number of cities, then singles and closebys at the end
        Clusters.Sort((a, b) => b.Cities.Length.CompareTo(a.Cities.Length));
        if (singles != null) Clusters.Add(singles);
        //if (closeCities.Count > 0) Clusters.Add(new CityCluster(closeCities, -2));
    }
}


public class DBSCAN
{
    public class Point
    {
        public CityUser City;
        public int ClusterID; // 0 = unvisited, -1 = noise, >0 = cluster ID
        public List<Point> Neighbors;

        public Point(CityUser city)
        {
            City = city;
            ClusterID = 0;
            Neighbors = [];
        }
        public override string ToString() => $"{City} ({ClusterID})";
    }

    private double eps;
    private int minPts;

    public DBSCAN(double eps, int minPts)
    {
        this.eps = eps;
        this.minPts = minPts;
    }

    public List<List<Point>> Fit(HashSet<CityUser> cities)
    {
        // prepare initial list
        List<Point> points = [.. cities.Select(city => new Point(city))];
        foreach(Point p in points)
            p.Neighbors = GetNeighbors(p, points);

        int clusterId = 0;

        foreach (Point p in points.OrderByDescending(p => p.Neighbors.Count)) // analyze from most "dense" neighborhoods
        {
            if (p.ClusterID != 0)
                continue; // already visited

            //List<Point> neighbors = GetNeighbors(p, points);

            if (p.Neighbors.Count < minPts)
            {
                p.ClusterID = -1; // mark as noise
                continue;
            }

            clusterId++;
            ExpandCluster(p, p.Neighbors, clusterId, points);
        }

        // Group by cluster ID and return list of clusters
        return points
            //.Where(p => p.ClusterID > 0)
            .GroupBy(p => p.ClusterID)
            .Select(g => g.ToList())
            .ToList();
    }

    private void ExpandCluster(Point p, List<Point> neighbors, int clusterId, List<Point> points)
    {
        p.ClusterID = clusterId;

        var queue = new Queue<Point>(neighbors);
        while (queue.Count > 0)
        {
            var q = queue.Dequeue();

            if (q.ClusterID > 0) // already assigned
                continue;

            // Check centroid distance before assigning cluster
            Point[] _cluster = [..points.Where(p => p.ClusterID == clusterId)];
            decimal _cx = _cluster.Average(p => p.City.Location.X);
            decimal _cy = _cluster.Average(p => p.City.Location.Y);
            if (GameScene.GetDistance(q.City.Location, new DVector(_cx, _cy)) > eps * 2.0) // TODO: Param 2.0
                continue;

            // Now the point is a neighbor and within centroid proper distance
            q.ClusterID = clusterId;
            var qNeighbors = GetNeighbors(q, points);
            if (qNeighbors.Count >= minPts)
            {
                foreach (var n in qNeighbors)
                    queue.Enqueue(n);
            }
        }
    }

    /// <summary>
    /// Gets neighbors (points with distance < eps) and sorts them by distance.
    /// </summary>
    /// <param name="p"></param>
    /// <param name="points"></param>
    /// <returns></returns>
    private List<Point> GetNeighbors(Point p, List<Point> points)
    {
        return [.. points.Where(other => Distance(p, other) < eps).OrderBy(other => Distance(p, other))];
    }

    private static double Distance(Point a, Point b)
    {
        double _dist = GameScene.GetDistance(a.City, b.City);
        if (a.City.City.Country_id != b.City.City.Country_id)
            _dist *= 1.5f;
        return _dist;
    }
}
