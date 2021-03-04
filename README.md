## Fast, simple charting for .NET (WPF or standalone)

GLGraphs is a real-time graphing/charting/data visualization library for .NET that runs standalone or integrated into WPF.


![Image](screenshot.gif)

The API is easily discoverable and simple to work with.

```c#
// Create a new graph with strings as the point values.
var graph = new CartesianGraph<string>();
// Add a series:
var series = graph.state.AddSeries(SeriesType.Point, "Example Series");
// Add some points:
series.Add("Origin", 0, 0);
series.Add("Destination", 2.5f, 5.0f);
```

To Render the graph, you need an OpenGL context. Either use `GLWpfControl` or simply the native windowing.

```c#
// update the state and render the graph
graph.State.Update(deltaTime)
graph.Render()
```

For more info, see the [Examples](src/Examples).

## Features

- Line Plots
- Scatter XY Plots
- Network Graph view
- Dynamic Axes
- Animations
- Interactivity (selection/drag selection)
- Tooltips
- Camera control (zoom/pan)
- WPF integration
  

## Planned Improvements

- Smooth graph scaling on point addition
- Better text handling 
- Additional integrations


## Something is not working

Oh no, it looks like you should have bought [SciChart](https://www.scichart.com/) instead!

GLGraph is a community-run project. If something is broken you'll need to fix it yourself.

Thankfully the code is simple, clean and easy to work with, and there's hopefully soon to be a budding community of people using this library.

Drop by the OpenTK Discord and ping [@varon](https://github.com/varon) in the #general channel for some info.


[![Discord](https://discordapp.com/api/guilds/337627185248468993/widget.png)](https://discord.gg/6HqD48s)




## FAQ


### Why not use LiveCharts?

- It's slow. Even with the paid-for geared package.
- The codebase is a mess.
- The maintainer is AWOL.

### Is there animation?

 Yes, everything is animated.

### How fast is it?

Way, way faster than LiveCharts.

Easily 10 million points with full camera animation at 60fps.


### How did you get this so fast?

Simplicity and performance are the primary goals of the library.

No MVVM is used, and the code is clean, fast and simple.

All rendering is hardware accelerated by OpenGL via [OpenTK](https://github.com/opentk/opentk/).


### Can you support 'X' UI framework (Avalonia, etc)?

This should be fairly easy to do. More or less create a control on that framework that can display OpenGL, make the right calls and you're good to go.

