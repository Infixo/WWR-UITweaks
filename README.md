# Worldwide Rush Infixo's UI Tweaks
[Worldwide Rush](https://store.steampowered.com/app/3325500/Worldwide_Rush/) mod that adds some UI enhancements to the game.

## Features

### Various
- World view.
  - City names are colored based on how overcrowded their indirect capacity is. Red cities no longer accept indirect passengers.
  - (0.5) Resorts are marked with an umbrella icon.
  - (0.6) RClick opens routes, w/ Shift opens a Hub.
  - (0.6) Info about number of routes in a city available on the map.
- InfoUI
  - (0.4) Improved vehicles tooltips.
  - (0.6) Button to close all floating windows at once.
- (0.8) Vehicle tooltip has info about its hub and location on the route.

### Explorers (new columns)
- Countries. Average city level, how many cities are not connected to any lines.
- Cities.
  - Indirect capacity, biggest crowd, fulfillment, trust, buildings.
  - (0.2) Filters and counters for city features and buildings.
  - (0.3) Number of unfulfilled destinations.
- Lines.
  - Num of cities, total length, (0.6) line age, (0.4) evaluation flag.
  - (0.2) Search by Country.
  - (0.3) Mark lines that have >1 hub.
  - (0.5) New filters: empty, national, international, evaluated.
- Vehicle types. Minimum passengers, estimated profit, monthly throughput on a 1000km line, range for planes.
- (0.2) Vehicles. Quarter efficieny and throughput.
- Hubs. 
  - (0.3) Shows budget and brands info.
  - (0.4) Info about generated plans.

### Floating windows
- RouteUI.
  - (0.5) Ability to change the assigned vehicle type. Only when the line is empty.
  - Extra info about the line. Quarterly throughput, number of vehicles, total distance.
  - Waiting passengers in each city and on the entire line.
  - City names are colores based on indirect capacity usage.
  - Quarterly efficieny is calculated using weighted average on their capacity.
  - (0.2) One-click upgrade to the next in chain.
  - (0.4) Shows evaluations done by a hub manager in a tooltip.
  - (0.8) Separate edit/change line buttons (less clicks), space for more vehicles.
- UpgradeUI. More info in the vehicle selection drop-down. Quick next/prev from the same company buttons.
- CityUI. Travellers toolip shows lines at the top (no more scrolling!) and indirect connections are sorted by number of people.
- CountryUI.
  - Info about: not connected cities, average city level, best vehicle providers.
  - In cities list, there is a mark if the city is connected to any lines.
- HubUI.
  - (0.3) Hire manager specific for a country of currently used vehicles. 
  - (0.4) Info about generated plans and list of brands.
- VehicleUI.
  - (0.4) VehicleUI shows line number.
  - (0.8) Ability to move a single vehicle to a different hub.
- (0.7) NEW Route Planner.
  - Groups into clusters cities that have interest in a group of origin cities.
  - Displays connections within a cluster and between the cluster and origin cities.
  - Use Ctrl-RClick on a city to start it and later add more origin cities.
  - (0.7.1) Displays connections between origin cities.
  - (0.7.1) Ability to mark any origin city as main city.

### Troubleshooting
- Output messages are logged into UITweaksLog.txt in the %TEMP% dir.

## Technical

### Requirements and Compatibility
- [WWR ModLoader](https://github.com/Infixo/WWR-ModLoader).
- [Harmony v2.4.1 for .net 8.0](https://github.com/pardeike/Harmony/releases/tag/v2.4.1.0). The correct dll is provided in the release files.

### Known Issues
- None atm.

### Changelog
- v0.8.0 (2025-10-30)
  - Ability to move a single vehicle to a different hub.
  - Vehicle tooltip has info about its hub and location on the route.
  - RouteUI: separate edit/change line buttons (less clicks), space for more vehicles.
- v0.7.1 (2025-10-27)
  - Planner: Displays connections between origin cities.
  - Planner: Ability to mark any origin city as main city.
  - Removed issue with range when editing empty plane routes.
- v0.7.0 (2025-10-26)
  - New feature: Route Planner
- v0.6.0 (2025-10-20)
  - RClick opens routes, w/ Shift opens a Hub.
  - Info about number of routes in a city available on the map.
  - Button to close all floating windows at once.
- v0.5.1 (2025-10-19)
  - Fixed hub manager crashing when there is no brands selected.
- v0.5.0 (2025-10-18)
  - Ability to change line type.
  - New filters in the lines explorer.
  - Resorts marked with an umbrella icon :)
- v0.4.0 (2025-10-16)
  - AITweaks link to get evaluations info.
  - Info about generated plans.
  - Minor improvements.
  - Removed: waiting passengers from lines explorer.
- v0.3.0 (2025-10-10)
  - Budget and brands in hubs explorer.
  - More enhancements in cities and lines explorer.
  - HubUI hire specific manager.
- v0.2.0 (2025-10-06)
  - One-click upgrade.
  - Filters and counters in the city explorer.
  - Search by country in the lines view.
- v0.1.0 (2025-10-02)
  - Initial release.

### Support
- Please report bugs and issues on [GitHub](https://github.com/Infixo/WWR-UITweaks).
- You may also leave comments on [Discord](https://discord.com/channels/1342565384066170964/1421898965556920342).
