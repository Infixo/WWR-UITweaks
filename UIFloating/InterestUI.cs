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

    public string Description => ID > 0 ? MainCity.Name : (ID == -2 ? "Close cities" : "Singles");

    public CityUser[] Cities { get; private set; }

    public CityCluster(List<CityUser> cities, int id, ushort player = 0)
    {
        if (cities.Count == 0)
            throw new ArgumentException("City cluster cannot be empty.");
        Cities = [.. cities];
        ID = id;
        // Set MainCity, try hubs first
        MainCity = Cities.Where(x => x.GetHub(player) != null).MaxBy(x => x.Level)!;
        if (MainCity == null)
            MainCity = Cities.MaxBy(x => x.Level)!;
    }

    public bool Contains(CityUser city) => Cities.Contains(city);

    public override string ToString()
    {
        string _result = $"({ID}).";
        for (int i = 0; i < Cities.Length; i++)
            _result += Cities[i].ToString() + ".";
        return _result;
    }

    public double GetEpsilon()
    {
        if (Cities.Length < 2)
        {
            return 0.0;
        }
        double _distance = 0.0;
        for (int i = 1; i < Cities.Length; i++)
        {
            double _dist = GameScene.GetDistance(Cities[i - 1], Cities[i]);
            if (_dist > _distance)
            {
                _distance = _dist;
            }
        }
        return _distance;
    }

    internal string GetNames(GameScene scene)
    {
        return String.Join(" ", Cities.Select(x => x.City.Country_id == MainCity.City.Country_id ? x.Name : x.GetNameWithIcon(scene)));
    }
}


internal class InterestUI : IFloatUI
{
    // Origin cities
    private readonly List<CityUser> Cities;
    internal CityUser City => Cities[0]; // TEMPORARY
    private ControlCollection citiesCollection;
    //private ScrollSettings citiesScrollSettings;
    private Dictionary<CityUser, IControl> citiesItems;
    private ScrollPreset? scrollPreset = null;

    // Destination cities

    // City clusters
    private readonly List<CityCluster> Clusters;
    private ControlCollection clustersCollection;
    private ScrollSettings clustersScroll;
    private GrowArray<PathArrow> arrows;
    private CityCluster? selected;


    internal InterestUI(CityUser city, GameScene scene) : base(null, scene)
    {
        // Origin cities
        Cities = [city];
        citiesCollection = new ControlCollection(ContentRectangle.Zero);
        //citiesScrollSettings = new ScrollSettings();
        citiesItems = [];

        // City clusters
        Clusters = [];
        clustersCollection = new ControlCollection(ContentRectangle.Zero);
        clustersScroll = new ScrollSettings();
        arrows = new GrowArray<PathArrow>();
        selected = null;

        // Build
        Construct();
    }


    private void Construct()
    {
        AddHeader("Route Planner");
        GetOriginCities();
        EndBack();
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


    // ORIGIN CITIES UI
    // [add country][add close by cities]

    private const float CitiesScrollHeight = 200f;

    private void GetOriginCities()
    {
        Grid _grid = new Grid(new ContentRectangle(0f, 0f, 400f, CitiesScrollHeight, 1f), 1, 2, SizeType.Weight);
        _grid.horizontal_alignment = HorizontalAlignment.Stretch;
        _grid.Margin_local = new FloatSpace(0f, MainData.Margin_content, 0f, 0f);
        AddControl(_grid, "cities");
        _grid.SetRow(0, SizeType.Pixels, MainData.Size_button);
        // Header?
        Label _header = LabelPresets.GetBold(Localization.GetGeneral("travelers"), Scene.Engine);
        _header.horizontal_alignment = HorizontalAlignment.Stretch;
        _header.Opacity = 0.75f;
        _grid.Transfer(_header, 0, 0);
        // Button?
        ButtonItem _toggle = ButtonPresets.TextGreenPulse(ContentRectangle.Stretched, "", Scene.Engine);
        _toggle.Control.Margin_local = new FloatSpace(MainData.Size_button * 2, 0f);
        _grid.Transfer(_toggle.Control, 0, 0);
        _toggle.Label.Text = "Cities"; //(show_travelers ? Localization.GetGeneral("travelers") : Localization.GetInfrastructure("traffic"));
                                       //_toggle.Control.OnButtonPress += (Action)delegate
                                       //{
                                       //ToggleBottom(_toggle.Label);
                                       //};
        _toggle.Control.OnMouseStillTime += (Action)delegate
        {
            GetToggleTooltip(_toggle.Control);
        };

        // List of cities
        citiesCollection = new ControlCollection(ContentRectangle.Zero);
        citiesCollection.horizontal_alignment = HorizontalAlignment.Stretch;
        ScrollSettings _settings = ContentPreset.GetScrollSettingsNoMargin();
        _settings.history = "c_" + City.City.City_id + "ts";
        //IControl _scroll = ScrollPreset.GetVertical(ContentRectangle.Stretched, citiesControlCollection, _settings); // oringal call
        //new ScrollPreset(rectangle, child, settings, ScrollType.Vertical, on_scroll).main_control; // GetVertical
        // ScrollPreset has private constructor and we need to store it to later access the vertical slider
        scrollPreset = typeof(ScrollPreset).CallPrivateConstructor<ScrollPreset>(
            [typeof(ContentRectangle), typeof(IControl), typeof(ScrollSettings), typeof(ScrollPreset.ScrollType), typeof(Action)], // arg types
            [ContentRectangle.Stretched, citiesCollection, _settings, ScrollPreset.ScrollType.Vertical, null!]); // Action on_scroll is null
        IControl _scroll = scrollPreset.GetPrivateField<ControlContainer>("main_control");
        _grid.Transfer(_scroll, 0, 1);
        UpdateOriginCities();
    }

    private void GetToggleTooltip(IControl parent)
    {
        TooltipPreset tooltipPreset = TooltipPreset.Get(Localization.GetInfrastructure("traffic_toggle"), Scene.Engine, can_lock: true);
        tooltipPreset.AddDescription(Localization.GetInfo("traffic"));
        tooltipPreset.AddToControlAbove(parent);
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
        if (citiesItems.TryGetValue(city, out IControl? item))
        {
            //y += item.Size_local.Y;
            return item;
        }

        // City button
        ControlCollection _content;
        Button _button = ButtonPresets.GetGeneral(new ContentRectangle(0f, 0f, 400f, MainData.Size_button, 1f), Scene.Engine, out _content);
        _button.horizontal_alignment = HorizontalAlignment.Left;
        _button.Margin_local = new FloatSpace(MainData.Margin_content_items, MainData.Margin_content_items / 2);
        //_button.use_multi_texture = true;
        //y += _button.Size_local_total.Y;
        citiesCollection.Transfer(_button);

        // Remove city
        _button.OnButtonPress += (Action)delegate
        {
            citiesItems[city].CloseWithAnimation(close_if_no_animation: true);
            citiesItems.Remove(city);
            Cities.Remove(city);
            UpdateOriginCities();
            GenerateClusters();
            UpdateCityClusters();
        };
        _button.SetCloseAnimation(AnimationPresets.Opacity(0f, 0.2f));
        _button.OnMouseStillTime += () => TooltipPreset.Get("Click to remove", Scene.Engine).AddToControlLeft(_button);

        // Name
        ControlCollection _collection = new ControlCollection(ContentRectangle.Stretched);
        _button.TransferContent(_collection);
        city.City.GetCountry(Scene).Name.GetTranslation(Localization.Language);
        Label _label = LabelPresets.GetDefault(city.GetNameWithIcon(Scene), Scene.Engine);
        _label.Margin_local = new FloatSpace(MainData.Margin_content);
        _collection.Transfer(_label);
        _label = LabelPresets.GetBold("right", Scene.Engine, LabelPresets.Color_stats);
        _label.horizontal_alignment = HorizontalAlignment.Right;
        _label.Margin_local = new FloatSpace(MainData.Margin_content);
        _collection.Transfer(_label);
        // Info (e.g. travelers)
        //TravelersDest _result = new TravelersDest(city, city, _panel);
        citiesItems.Add(city, _button);
        //_label.OnUpdate += (Action)delegate
        //{
        //_label.Text = "<!cicon_passenger> " + StrConversions.CleanNumber(_result.Total());
        //};
        //_panel.OnMouseStillTime += (Action)delegate
        //{
        //GetTravelerTooltip(_result);
        //};
        //y += _panel.Size_local.Y;
        return _button;
    }


    // CITY CLUSTERS UI
    // [up][dn] epsilon [up][dn] min cities [show-all]
    // [scroll with items]

    private void GetCityClusters()
    {
        ControlCollection _content = new ControlCollection(new ContentRectangle(0f, 0f, 0f, MainData.Size_button, 1f));
        _content.horizontal_alignment = HorizontalAlignment.Stretch;
        _content.Margin_local = new FloatSpace(MainData.Margin_content, MainData.Margin_content, MainData.Margin_content, 0f);
        AddControl(_content, "destinations_header");
        // Title
        Label _label = LabelPresets.GetBold(Localization.GetVehicle("travel_destinations"), Scene.Engine);
        _label.horizontal_alignment = HorizontalAlignment.Center;
        _label.Opacity = 0.75f;
        _content.Transfer(_label);
        // Scroll area
        clustersCollection = new ControlCollection(ContentRectangle.Zero);
        clustersCollection.horizontal_alignment = HorizontalAlignment.Stretch;
        clustersScroll = ContentPreset.GetScrollSettingsNoMargin();
        clustersScroll.history = "c_" + City.City.City_id + "ds";
        IControl _scroll = ScrollPreset.GetVertical(new ContentRectangle(0f, 0f, 0f, 300f, 1f), clustersCollection, clustersScroll);
        _scroll.horizontal_alignment = HorizontalAlignment.Stretch;
        _scroll.Margin_local = new FloatSpace(MainData.Margin_content, 0f, MainData.Margin_content, MainData.Margin_content);
        AddControl(_scroll, "destinations");
        // Sort button
        Button _sort = ButtonPresets.IconGreen(new ContentRectangle(0f, 0f, MainData.Size_button, MainData.Size_button, 1f), MainData.Icon_cogwheel, Scene.Engine).Control;
        _sort.horizontal_alignment = HorizontalAlignment.Right;
        _sort.vertical_alignment = VerticalAlignment.Center;
        _sort.Margin_local = new FloatSpace(MainData.Margin_content, 0f);
        _content.Transfer(_sort);
        //_sort.OnButtonPress += (Action)delegate
        //{
        //ButtonPresets.OpenDropdown(_sort, GetSortOptions, Scene.Engine);
        //};
        GenerateClusters(); // initial city
        UpdateCityClusters();
    }


    private void UpdateCityClusters()
    {
        clustersCollection.Clear();
        float _y = 0f;
        foreach(CityCluster cluster in Clusters)
        {
            AddCityClusterItem(cluster, ref _y); //, City.Last_destination <= m);
        }
        clustersCollection.Size_local = new Vector2(0f, _y);
        clustersCollection.Parent.Parent.dirty_size = true;

        selected = Clusters.Count > 0 ? Clusters.Where(x => x.ID > 0).MaxBy(x => x.Cities.Length) : null;
        if (selected != null)
        {
            GenerateArrows(selected);
        }
        else DestroyArrows();
    }


    private void GenerateArrows(CityCluster cluster)
    {
        DestroyArrows();
        if (cluster.ID > 0)
        {
            // NEW LOGIC - show main city and intra-cluster
            // MainCity
            foreach (CityUser city in Cities)
                GenerateArrow(city, cluster.MainCity);

            // Cluster connections
            foreach (CityUser cc in cluster.Cities.Where(x => x != cluster.MainCity))
                GenerateArrow(cc, cluster.MainCity);
        }
        else
        {
            if (Cities.Count == 0) return;
            foreach (CityUser cc in cluster.Cities)
                GenerateArrow(cc, Cities[0]);
        }
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
    private void GenerateArrow(CityUser origin, CityUser destination)
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

        // Decide color and build 1 or 2 arrows
        Color arrowColor = (originToDest || destToOrigin)
            ? (directRoute ? ConnectionColor : InterestColor)
            : (directRoute ? DirectRouteColor : NoConnectionColor);
        if (destToOrigin)
            AddArrow(destination, origin, arrowColor);
        else // if (originToDest)
            AddArrow(origin, destination, arrowColor); // we need an arrow to show no-connection status
    }


    private void AddCityClusterItem(CityCluster cluster, ref float y)
    {
        // Main button with the city name TODO: this will be main "cluster" button
        // Design:       [name,country     count] [on/off ] (arrows toggle)
        //               [city names (scrolling)] [analyze]
        Button _button = ButtonPresets.GetBlack(new ContentRectangle(0f, y, 0f, MainData.Size_button * 2, 1f), Scene.Engine, out ControlCollection _content);
        y += _button.Size_local_total.Y;
        _button.horizontal_alignment = HorizontalAlignment.Stretch;
        _button.Margin_local = new FloatSpace(MainData.Margin_content_items, MainData.Margin_content / 2);
        _button.use_multi_texture = true;
        clustersCollection.Transfer(_button);

        // Show arrows
        _button.OnButtonPress += (Action)delegate
        {
            if (selected != null && selected.ID == cluster.ID)
            {
                selected = null;
                DestroyArrows();
            }
            else
            {
                selected = cluster;
                GenerateArrows(cluster);
            }
        };

        // Show briefly if not selected
        _button.OnMouseStillTime += (Action)delegate
        {
            if (selected == null)
                GenerateArrows(cluster);
        };

        _button.OnMouseLeave += (Action)delegate
        {
            if (selected == null)
                DestroyArrows();
        };

        // Grid 2x2 in the main button
        Grid _grid = new Grid(ContentRectangle.Stretched, 2, 2, SizeType.Weight);
        _grid.horizontal_alignment = HorizontalAlignment.Stretch;
        _grid.Margin_local = new FloatSpace(0f, MainData.Margin_content_items/2);
        _grid.SetColumn(1, SizeType.Pixels, MainData.Size_button);
        _content.Transfer(_grid);

        // City name
        string _text = cluster.MainCity.GetNameWithIcon(Scene) 
            + $" <!cicon_star>{cluster.MainCity.Level}" 
            + $" <!#{(LabelPresets.Color_main * 0.75f).GetHex()}>({cluster.MainCity.City.GetCountry(Scene).Name.GetTranslation(Localization.Language)})";
        Label _name = LabelPresets.GetBold(cluster.ID > 0 ? _text : cluster.Description, Scene.Engine);
        _name.horizontal_alignment = HorizontalAlignment.Left;
        _name.Margin_local = new FloatSpace(MainData.Margin_content);
        _grid.Transfer(_name, 0, 0);

        // Num cites in the cluster
        Label _count = LabelPresets.GetBold(cluster.Cities.Length.ToString(), Scene.Engine, LabelPresets.Color_stats);
        _count.horizontal_alignment = HorizontalAlignment.Right;
        _count.Margin_local = new FloatSpace(MainData.Margin_content);
        _grid.Transfer(_count, 0, 0);

        // Bottom row - list of cities in the cluster
        Label _cluster = LabelPresets.GetDefault(cluster.GetNames(Scene), Scene.Engine);
        _cluster.Margin_local = new FloatSpace(MainData.Margin_content/2);
        IControl _radio = LabelPresets.GetRadio(_cluster, 500);
        _radio.Mouse_visible = false;
        _grid.Transfer(_radio, 0, 1);
    }


    // TODO: will list all info about cluster, inc. all cities, connections, etc.
    private void GetDestinationTooltip(IControl parent, CityDestination destination)
    {
        TooltipPreset _tooltip = TooltipPreset.Get(Localization.GetVehicle("destination"), Scene.Engine, can_lock: true);
        string _text = City.GetNameWithIcon(Scene) + " <!cicon_right> " + destination.Destination.User.GetNameWithIcon(Scene);
        _tooltip.AddBoldLabel(_text, null, center: true);
        _tooltip.AddSeparator();
        if (City.DestinationIndirrectProblem(destination, out var _amount))
        {
            _tooltip.AddDescription(Localization.GetTasks("indirect_info")).Color = LabelPresets.Color_negative;
            _tooltip.AddStatsLine(Localization.GetCity("connecting"), "<!cicon_passenger> " + StrConversions.CleanNumber(_amount));
            _tooltip.AddSeparator();
        }
        _tooltip.AddDescription(Localization.GetInfo("destination"));
        _tooltip.AddSpace();
        _tooltip.AddDescription(Localization.GetInfo("destination_last").Replace("{percent}", StrConversions.Percent((float)MainData.Defaults.City_destination_change)));
        _tooltip.AddSeparator();
        _tooltip.AddStatsLine(Localization.GetCity("level"), "<!cicon_star> " + StrConversions.OutOf(destination.Level, MainData.Defaults.Max_level_destination));
        _tooltip.AddStatsLine("<!cl:demand:" + Localization.GetCity("demand_price") + ">", "<!cicon_demand> " + StrConversions.CleanNumber((decimal)destination.Demand_price / 100m, 2));
        _tooltip.AddStatsLine(Localization.GetCity("passengers_per_month"), "<!cicon_passenger> " + StrConversions.CleanNumber(MainData.Defaults.GetMinPassengersPerMonth() * destination.Level));
        _tooltip.AddStatsLine(Localization.GetGeneral("max"), "<!cicon_passenger> " + StrConversions.CleanNumber(MainData.Defaults.GetMaxPassengersPerMonth()), alt: true, 1);
        if (destination.Tourism != 0)
        {
            _tooltip.AddSeparator();
            _tooltip.AddStatsLine("<!cl:tourism:" + Localization.GetCity("tourism") + ">", StrConversions.PercentChange((float)destination.Tourism / 100f));
            //AppendTourismEvents(_tooltip, City.City, Scene);
        }
        _tooltip.AddSeparator();
        _tooltip.AddStatsLine(Localization.GetCity("fulfillment"), () => StrConversions.Percent((float)destination.Percent));
        _tooltip.AddStatsLine(Localization.GetVehicle("last_month"), () => StrConversions.Percent((float)destination.Last), alt: true, 1);
        _tooltip.AddSeparator();
        _tooltip.AddStatsLine(Localization.GetGeneral("distance"), () => StrConversions.GetDistance(GameScene.GetDistance(City, destination.Destination.User)));
        //AttachLines(_tooltip, City, destination.Destination.User, Scene);
        //SetUpArrows(destination.Destination.User, _tooltip.Main_control);
        _tooltip.AddToControlAuto(parent);
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
        //UpdateBottom(); // TODO: update cities if needed
        //UpdateCityClusters();
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

        // move all close cities into a separate cluster
        bool IsClose(CityUser x)
        {
            foreach (CityUser city in Cities)
                if (GameScene.GetDistance(city, x) < 300f)
                    return true;
            return false;
        }
        List<CityUser> closeCities = [.. destinations.Where(x => IsClose(x))];
        destinations.ExceptWith(closeCities);

        // Call DBSCAN and create clusters
        var dbscan = new DBSCAN(300f, 2); // 300km, min 2 cities in a cluster
        var pointClusters = dbscan.Fit(destinations);
        foreach (List<DBSCAN.Point> points in pointClusters)
            Clusters.Add(new CityCluster([.. points.Select(x => x.City)], points[0].ClusterID));

        // Exclude singles temporarily
        int singlesIndex = Clusters.FindIndex(x => x.ID == -1);
        CityCluster? singles = singlesIndex < 0 ? null : Clusters[singlesIndex];
        if (singlesIndex >= 0)
            Clusters.RemoveAt(singlesIndex);

        // Sorting by number of cities, then singles and closebys at the end
        Clusters.Sort((a, b) => b.Cities.Length.CompareTo(a.Cities.Length));
        if (singles != null) Clusters.Add(singles);
        if (closeCities.Count > 0) Clusters.Add(new CityCluster(closeCities, -2));
    }
}


public class DBSCAN
{
    public class Point
    {
        public CityUser City;
        public int ClusterID; // 0 = unvisited, -1 = noise, >0 = cluster ID
        public Point(CityUser city)
        {
            City = city;
            ClusterID = 0;
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

        int clusterId = 0;

        foreach (Point p in points)
        {
            if (p.ClusterID != 0)
                continue; // already visited

            List<Point> neighbors = GetNeighbors(p, points);

            if (neighbors.Count < minPts)
            {
                p.ClusterID = -1; // mark as noise
                continue;
            }

            clusterId++;
            ExpandCluster(p, neighbors, clusterId, points);
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

            if (q.ClusterID == -1)
                q.ClusterID = clusterId;

            if (q.ClusterID != 0)
                continue;

            q.ClusterID = clusterId;

            var qNeighbors = GetNeighbors(q, points);
            if (qNeighbors.Count >= minPts)
            {
                foreach (var n in qNeighbors)
                    queue.Enqueue(n);
            }
        }
    }

    private List<Point> GetNeighbors(Point p, List<Point> points)
    {
        return [.. points.Where(other => Distance(p, other) <= eps)];
    }

    private static double Distance(Point a, Point b)
    {
        double _dist = GameScene.GetDistance(a.City, b.City);
        if (a.City.City.Country_id != b.City.City.Country_id)
            _dist *= 1.5f;
        return _dist;
    }
}
