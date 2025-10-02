# Worldwide Rush Infixo's UI Tweaks
![Worldwide Rush](https://store.steampowered.com/app/3325500/Worldwide_Rush/) mod that adds some UI enhancements to the game.

## Features

### Generic
- World view. City names are colored based on how overcrowded their indirect capacity is. Red cities no longer accept indirect passengers.

### Explorers (new columns)
- Countries. Average city level, how many cities are not connected to any lines.
- Cities. Indirect capacity, biggest crowd, fulfillment, trust, buildings.
- Lines. Num of cities, total length, theoretical throughput, waiting passengers.
- Vehicle types. Minimum passengers, estimated profit, monthly throughput on a 1000km line, range for planes.

### Floating windows
- RouteUI.
  - Extra info about the line. Quarterly throughput, number of vehicles, total distance.
  - Waiting passengers in each city and on the entire line.
  - City names are colores based on indirect capacity usage.
  - Quarterly efficieny is calculated using weighted average on their capacity.
- UpgradeUI. More info in the vehicle selection drop-down. Quick next/prev from the same company buttons.
- CityUI. Travellers toolip shows lines at the top (no more scrolling!) and indirect connections are sorted by number of people.
- CountryUI.
  - Info about: not connected cities, average city level, best vehicle providers.
  - In cities list, there is a mark if the city is connected to any lines.

### Troubleshooting
- Output messages are logged into UITweaksLog.txt in the %TEMP% dir.

## Technical

### Requirements and Compatibility
- [WWR ModLoader](https://github.com/Infixo/WWR-ModLoader).
- [Harmony v2.4.1 for .net 8.0](https://github.com/pardeike/Harmony/releases/tag/v2.4.1.0). The correct dll is provided in the release files.

### Known Issues
- Explorer Lines. Calculating passengers for hundreds of lines may take a bit :(.

### Changelog
- v0.1.0 (2025-10-02)
  - Initial release.

### Support
- Please report bugs and issues on [GitHub](https://github.com/Infixo/WWR-UITweaks).
- You may also leave comments on [Discord](https://discord.com/channels/1342565384066170964/1421898965556920342).
