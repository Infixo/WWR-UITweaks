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


internal class InterestUI : IFloatUI
{
    // Origin cities
    private readonly List<CityUser> Cities;
    internal CityUser City => Cities[0]; // TEMPORARY
    private ControlCollection citiesControlCollection;
    private ScrollSettings citiesScrollSettings;
    private Dictionary<CityUser, IControl> citiesItems;
    private IControl scroll;

    // Destinations
    private ControlCollection destinations;
    private ScrollSettings destinations_scroll;
    private GrowArray<CityUser> destinations_saved;
    private GrowArray<PathArrow> arrows;


    internal InterestUI(CityUser city, GameScene scene) : base(null, scene)
    {
        // Origin cities
        Cities = [city];
        citiesControlCollection = new ControlCollection(ContentRectangle.Zero);
        citiesScrollSettings = new ScrollSettings();
        citiesItems = [];

        // Destinations
        destinations = new ControlCollection(ContentRectangle.Zero);
        destinations_scroll = new ScrollSettings();
        destinations_saved = new GrowArray<CityUser>();
        arrows = new GrowArray<PathArrow>();

        // Build
        Construct();
    }


    private void Construct()
    {
        AddHeader("Route Planner");
        GetOriginCities();
        EndBack();
        GetDestinations();
        Finalize();
        SetPinned(true);
        // events
        base.Main_control.OnClose += () => DestroyArrows();
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
                    ui.Cities.Add(city);
                    ui.UpdateOriginCities(scrollToLast: true);
                }
                return;
            }

        // Not open - create a new one
        scene.Selection.AddUI(new InterestUI(city, scene));
    }


    private void GetOriginCities()
    {
        Grid _grid = new Grid(new ContentRectangle(0f, 0f, 0f, 304f, 1f), 1, 2, SizeType.Weight);
        _grid.horizontal_alignment = HorizontalAlignment.Stretch;
        _grid.Margin_local = new FloatSpace(0f, MainData.Margin_content, 0f, 0f);
        AddControl(_grid, "travelers");
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
        citiesControlCollection = new ControlCollection(ContentRectangle.Zero);
        citiesControlCollection.horizontal_alignment = HorizontalAlignment.Stretch;
        citiesScrollSettings = ContentPreset.GetScrollSettings();
        citiesScrollSettings.history = "c_" + City.City.City_id + "ts";
        scroll = ScrollPreset.GetVertical(ContentRectangle.Stretched, citiesControlCollection, citiesScrollSettings);
        _grid.Transfer(scroll, 0, 1);
        //items = new GrowArray<TravelersDest>();
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
        //for (int i = 0; i < citiesItems.Count; i++)
        {
            IControl cityItem = GetOriginCityItem(city);
            Animation _animation = AnimationPresets.Opacity(1f, 0.2f);
            _animation.Add(AnimationPresets.Location(new Vector2(0f, _y), 0.2f));
            cityItem.SetAnimation(_animation);
            _y += cityItem.Size_local_total.Y;
        }
        citiesControlCollection.Size_local = new Vector2(0f, _y);
        citiesControlCollection.Parent.Parent.dirty_size = true;
        citiesScrollSettings.focus(citiesItems[Cities[^1]]);
    }
    /*
    private void UpdateTravelers()
    {
        for (int l = 0; l < City.Destinations.Items.Count; l++)
        {
            if (City.Destinations.Items[l].People != 0)
            {
                GetOriginCityItem(City.Destinations.Items[l].Destination.User).direct += City.Destinations.Items[l].People;
            }
        }
        for (int k = 0; k < City.Indirect.Count; k++)
        {
            if (City.Indirect[k].People != 0)
            {
                GetOriginCityItem(City.Indirect[k].Destination.User).indirect += City.Indirect[k].People;
            }
        }
        for (int j = 0; j < City.Returns.Count; j++)
        {
            if (City.Returns[j].Ready != 0)
            {
                GetOriginCityItem(City.Returns[j].Home.User).going_home += City.Returns[j].Ready;
            }
        }
        for (int i = citiesItems.Count - 1; i >= 0; i--)
        {
            if (!citiesItems[i].Valid())
            {
                citiesItems[i].Control.CloseWithAnimation(close_if_no_animation: true);
                citiesItems.RemoveAt(i);
            }
        }
    }*/
    /*
    private void UpdateAllItems()
    {
        for (int k = 0; k < City.Destinations.Items.Count; k++)
        {
            GetFromAllItems(City.Destinations.Items[k].Destination.User).direct += Math.Max(City.Destinations.Items[k].People, 1);
        }
        for (int j = 0; j < City.Indirect.Count; j++)
        {
            GetFromAllItems(City.Indirect[j].Destination.User).indirect += Math.Max(City.Indirect[j].People, 1);
        }
        for (int i = 0; i < City.Returns.Count; i++)
        {
            GetFromAllItems(City.Returns[i].Home.User).going_home += Math.Max(City.Returns[i].Ready, 1);
        }
    }

    private TravelersDest GetFromAllItems(CityUser destination)
    {
        for (int i = 0; i < all_items.Count; i++)
        {
            if (all_items[i].Destination == destination)
            {
                return all_items[i];
            }
        }
        TravelersDest _new = new TravelersDest(destination, null!);
        all_items.Add(_new);
        return _new;
    }
    */

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
        citiesControlCollection.Transfer(_button);
        _button.OnButtonPress += (Action)delegate
        {
            citiesItems[city].CloseWithAnimation(close_if_no_animation: true);
            citiesItems.Remove(city);
            Cities.Remove(city);
            UpdateOriginCities();
        };
        _button.SetCloseAnimation(AnimationPresets.Opacity(0f, 0.2f));
        _button.OnMouseStillTime += (Action)delegate
        {
            TooltipPreset _tooltip = TooltipPreset.Get("Press to remove", Scene.Engine);
            _tooltip.AddToControlRight(_button);
        };

        // Main button with the city name TODO: this will be main "cluster" button
        
        //destinations.Transfer(_button);
        //_button.Opacity = 0f;

        // Panel
        /*
        Panel _panel = new Panel(new ContentRectangle(0f, y, 0f, MainData.Size_button, 1f), MainData.Panel_general);
        _panel.horizontal_alignment = HorizontalAlignment.Stretch;
        _panel.Margin_local = new FloatSpace(MainData.Margin_content_items, MainData.Margin_content_items / 2);
        _panel.use_multi_texture = true;
        //_panel.Opacity = 0f;
        citiesControlCollection.Transfer(_panel);
        ControlCollection _collection = new ControlCollection(ContentRectangle.Stretched);
        _panel.TransferContent(_collection);

        // RClick - Remove city
        _panel.OnMousePress += (Action)delegate
        {
            Cities.Remove(city);
            UpdateOriginCities();
        };
        */
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


    private void GetDestinations()
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
        destinations = new ControlCollection(ContentRectangle.Zero);
        destinations.horizontal_alignment = HorizontalAlignment.Stretch;
        destinations_scroll = ContentPreset.GetScrollSettingsNoMargin();
        destinations_scroll.history = "c_" + City.City.City_id + "ds";
        IControl _scroll = ScrollPreset.GetVertical(new ContentRectangle(0f, 0f, 0f, 300f, 1f), destinations, destinations_scroll);
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
        UpdateDestinations();
    }


    // TODO: Not sure if update is so needed, the feature is rather for slow analysis
    private void UpdateDestinations()
    {
        if (DestinationsSame())
        {
            // Sorting logic
            //for (int i = 0; i < sort_buffer.Count; i++)
            //{
            //sort_buffer.Items[i].fullfilment = (float)City.Destinations.Items[sort_buffer.Items[i].destination].Percent;
            //}
            //SortDestinations();
            return;
        }
        GenerateArrows();
        float _y = destinations.Size_local.Y;
        if (destinations_saved.Count < City.Destinations.Items.Count)
        {
            for (int m = destinations.Count; m < City.Destinations.Items.Count; m++)
            {
                destinations_saved.Add(City.Destinations.Items[m].Destination.User);
                AddDestinationItem(City.Destinations.Items[m], ref _y); //, City.Last_destination <= m);
            }
        }
        else if (destinations_saved.Count > City.Destinations.Items.Count)
        {
            IControl _item = destinations[destinations.Count - 1];
            _y -= _item.Size_local_total.Y;
            _item.CloseWithAnimation(close_if_no_animation: true);
            destinations.Remove(_item);
            destinations_saved.RemoveAt(destinations_saved.Count - 1);
        }
        else
        {
            _y = 0f;
            for (int l = 0; l < City.Destinations.Items.Count; l++)
            {
                if (destinations_saved[l] != City.Destinations.Items[l].Destination.User)
                {
                    destinations[l].CloseWithAnimation(close_if_no_animation: true);
                    destinations_saved[l] = City.Destinations.Items[l].Destination.User;
                    AddDestinationItem(City.Destinations.Items[l], ref _y); //, new_animation: true);
                }
                _y += destinations[l].Size_local_total.Y;
            }
        }
        destinations.Size_local = new Vector2(0f, _y);
        destinations.Parent.Parent.dirty_size = true;
        // Sorting logc - TODO perhaps could reuse?
        //sort_buffer = new GrowArray<DestinationLink>(City.Destinations.Items.Count);
        //for (int k = 0; k < City.Destinations.Items.Count; k++)
        //{
        //sort_buffer.Add(new DestinationLink(k, (float)City.Destinations.Items[k].Percent, destinations[k]));
        //}
        //SortDestinations();
        // TODO: Not sure what Highlight does
        if (!City.Highlight)
        {
            return;
        }
        if (destinations.Ui == null)
        {
            destinations.OnSizeChange += (Action)delegate
            {
                destinations.OnSizeChange.Clear();
                destinations_scroll.focus(destinations.Items[destinations.Count - 1]);
            };
        }
        else
        {
            destinations.Parent.Parent.UpdateSize();
            destinations_scroll.focus(destinations.Items[destinations.Count - 1]);
        }
        City.DisableHighlight();
        //}
    }


    private bool DestinationsSame()
    {
        if (destinations_saved.Count != City.Destinations.Items.Count)
        {
            return false;
        }
        for (int i = 0; i < City.Destinations.Items.Count; i++)
        {
            if (destinations_saved[i] != City.Destinations.Items[i].Destination.User)
            {
                return false;
            }
        }
        return true;
    }


    // This must called to toggle on/off arrows on a cluster
    private void GenerateArrows()
    {
        DestroyArrows();
        for (int j = 0; j < City.Destinations.Items.Count; j++)
        {
            arrows.Add(new PathArrow(City, City.Destinations.Items[j].Destination.User, new NewRouteSettings(-1), Scene));
            arrows.Last.color = Color.Blue; // TODO!!!
            arrows.Last.strength = City.Destinations.Items[j].Level;
            int _id = City.Cities_interest.Find(City.Destinations.Items[j].Destination.User);
            if (_id >= 0)
            {
                arrows.Last.strength += City.Cities_interest[_id].Destinations.GetLevel(City.City);
            }
        }
        for (int i = 0; i < City.Cities_interest.Count; i++)
        {
            if (City.Destinations.GetLevel(City.Cities_interest[i].City) <= 0)
            {
                arrows.Add(new PathArrow(City.Cities_interest[i], City, new NewRouteSettings(-1), Scene));
                arrows.Last.color = Color.Pink; // TODO!!!
                arrows.Last.strength = City.Cities_interest[i].Destinations.GetLevel(City.City);
            }
        }
        //UpdateArrowColors(); // This actually animates arrows
    }


    private void DestroyArrows()
    {
        for (int i = 0; i < arrows.Count; i++)
        {
            arrows[i].Destroy(Scene);
        }
        arrows.Clear();
    }


    private void AddDestinationItem(CityDestination destination, ref float y)
    {
        // Main button with the city name TODO: this will be main "cluster" button
        ControlCollection _content;
        Button _button = ButtonPresets.GetBlack(new ContentRectangle(0f, y, 0f, MainData.Size_button * 2, 1f), Scene.Engine, out _content);
        _button.horizontal_alignment = HorizontalAlignment.Stretch;
        _button.Margin_local = new FloatSpace(MainData.Margin_content_items, MainData.Margin_content / 2);
        _button.use_multi_texture = true;
        destinations.Transfer(_button);
        y += _button.Size_local_total.Y;
        _button.OnButtonPress += (Action)delegate
        {
            if (Scene.tracking == destination.Destination.User)
            {
                destination.Destination.User.Select(Scene, track: true);
            }
            else
            {
                Scene.tracking = destination.Destination.User;
            }
        };
        _button.OnMouseStillTime += (Action)delegate
        {
            GetDestinationTooltip(_button, destination);
        };
        //_button.Opacity = 0f;

        // Grid 2x2 in the main button
        Grid _grid = new Grid(ContentRectangle.Stretched, 2, 2, SizeType.Weight);
        _grid.horizontal_alignment = HorizontalAlignment.Stretch;
        _grid.Margin_local = new FloatSpace(0f, MainData.Margin_content_items);
        _grid.SetColumn(1, SizeType.Pixels, MainData.Size_button * 2);
        _content.Transfer(_grid);

        // City name
        string _text = destination.Destination.User.GetNameWithIcon(Scene);
        Label _dest = LabelPresets.GetBold(_text, Scene.Engine);
        _dest.Margin_local = new FloatSpace(MainData.Margin_content);
        _grid.Transfer(_dest, 0, 0);
        // Image of the head - not needed
        //Image _passenger = new Image(MainData.Icon_passenger.Upscale);
        //_passenger.horizontal_alignment = HorizontalAlignment.Center;
        //_passenger.vertical_alignment = VerticalAlignment.Center;
        //_passenger.Opacity = 0.25f;
        //_grid.Transfer(_passenger, 1, 0, 1, 2);
        // Number of people TODO: number of people wanting to come here (destinations) - probably NOT indirect - analysis must be based on RAW demand
        Label _count = LabelPresets.GetBold("", Scene.Engine, LabelPresets.Color_stats);
        _count.horizontal_alignment = HorizontalAlignment.Center;
        _grid.Transfer(_count, 1, 0, 1, 2);
        // Price ?? TODO could use for cluster info like Distance, num, etc.
        Label _price = LabelPresets.GetBold("", Scene.Engine, LabelPresets.Color_positive);
        _price.horizontal_alignment = HorizontalAlignment.Right;
        _price.Margin_local = new FloatSpace(MainData.Margin_content);
        _grid.Transfer(_price, 0, 0);
        _count.OnUpdate += (Action)delegate
        {
                Label label2 = _dest;
                Color color = (_count.Color = LabelPresets.Color_main);
                label2.Color = color;
            _count.Text = StrConversions.CleanNumber(destination.People);
            _price.Text = "<!cicon_demand> " + StrConversions.CleanNumber((decimal)destination.Demand_price / 100.0m, 2);
            if (destination.Demand_price < 100)
            {
                float num2 = (float)(destination.Demand_price - MainData.Defaults.Demand_price_min) / (float)(100 - MainData.Defaults.Demand_price_min);
                _price.Color = LabelPresets.Color_negative.Lerp(LabelPresets.Color_positive, num2 * num2 * num2);
            }
            else
            {
                float percent = (float)(destination.Demand_price - 100) / (float)(MainData.Defaults.Demand_price_max - 100);
                _price.Color = LabelPresets.Color_positive.Lerp(LabelPresets.Color_link, percent);
            }
        };

        // Bottom row - level graphics TODO: list of cities in the cluster with some basic info?
        ControlCollection _level = new ControlCollection(new ContentRectangle(0f, 0f, 0f, MainData.Size_button, 1f));
        _level.horizontal_alignment = HorizontalAlignment.Stretch;
        _level.Mouse_visible = false;
        _grid.Transfer(_level, 0, 1);
        Panel _fill = new Panel(ContentRectangle.Stretched, MainData.Panel_fill_box_back);
        _fill.use_multi_texture = true;
        _fill.Mouse_visible = false;
        _level.Transfer(_fill);
        Label _label = LabelPresets.GetDefault(Localization.GetCity("fulfillment"), Scene.Engine);
        _label.Margin_local = new FloatSpace(MainData.Margin_content);
        _label.Color *= 0.5f;
        _level.Transfer(_label);
        Image _last = new Image(new ContentRectangle(0f, 0f, 4f, 0f, 1f), MainData.Panel_empty);
        _last.vertical_alignment = VerticalAlignment.Stretch;
        _last.Color = Color.Black * 0.5f;
        _level.Transfer(_last);
        Label _count_a = LabelPresets.GetBold("", Scene.Engine);
        _count_a.horizontal_alignment = HorizontalAlignment.Right;
        _count_a.Margin_local = new FloatSpace(MainData.Margin_content);
        _level.Transfer(_count_a);
        _fill = new Panel(ContentRectangle.Zero, MainData.Panel_fill_box);
        _fill.vertical_alignment = VerticalAlignment.Stretch;
        _fill.use_multi_texture = true;
        _fill.Opacity = 0f;
        _fill.Mouse_visible = false;
        _level.Transfer(_fill);
        ControlContainer _fill_container = new ControlContainer(ContentRectangle.Stretched);
        _fill_container.Use_scissors = true;
        _fill_container.mouse_pass = false;
        _fill.TransferContent(_fill_container);
        ControlCollection _fill_collection = new ControlCollection(ContentRectangle.Stretched);
        _fill_container.TransferContent(_fill_collection);
        _label = LabelPresets.GetDefault(Localization.GetCity("fulfillment"), Scene.Engine, LabelPresets.Color_secondary);
        _label.Margin_local = new FloatSpace(MainData.Margin_content);
        _label.Color *= 0.75f;
        _fill_collection.Transfer(_label);
        Label _count_b = LabelPresets.GetBold("", Scene.Engine, LabelPresets.Color_secondary);
        _count.Margin_local = new FloatSpace(MainData.Margin_content);
        _fill_collection.Transfer(_count_b);
        float _p = (float)destination.Percent;
        _level.OnUpdate += (Action)delegate
        {
            _last.Location_local = new Vector2(0f, 0f); // new Vector2(Math.Clamp(num, 0f, _level.Size_local.X - _last.Size_local.X), 0f);
            _last.Opacity = Math.Clamp(0f / 10f, 0.5f, 1f);
            _last.Color = LabelPresets.Color_negative * 0.5f;

            _p = _p * 0.75f + (float)destination.Percent * 0.25f;
            _fill.Size_local = new Vector2(_level.Size_local.X * _p, 0f);
            _count_a.Text = StrConversions.Percent((float)destination.Percent);
            _count_b.Text = _count_a.Text;
            _count_b.Location_local = new Vector2(_level.Size_local.X - (float)MainData.Margin_content - _count_b.Size_local_total.X, 0f);
            if (destination.Tourism != 0)
            {
                _dest.Text = _text + " <!#" + LabelPresets.Color_positive.GetHex() + ">" + StrConversions.PercentChange((float)destination.Tourism / 100f);
            }
            else
            {
                _dest.Text = _text;
            }
            if (_p < 0.01f)
            {
                _fill.Opacity = 0f;
            }
            else if (_p < 0.04f)
            {
                _fill.Opacity = (_p - 0.01f) / 0.03f;
            }
            else
            {
                _fill.Opacity = 1f;
            }
        };
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
        UpdateDestinations();
        //UpdateArrowColors(); // TODO: arrow animations
    }
}
