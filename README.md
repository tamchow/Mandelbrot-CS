# Mandelbrot-CS
Not-So-Simple multithreaded Mandelbrot Renderer with smooth coloring in C# with WinFoms GUI.

The API spports zooming and panning, but the application itself doesn't expose this interactively (command-line paramaters are necessary). I'll incorporate that sometime into the future (possibly).

Controls:
1. <kbd>ESC</kbd> - Closes the display window.
2. <kbd>S</kbd> - Saves the image displayed in the invocation directory as `Image.png`.

Easy as pie to run. Download the `.exe` from the [Releases](https://github.com/tamchow/Mandelbrot-CS/releases) page and double click. Enjoy the Mandelbrot!

It looks like this (a zoom with center at (0.16125, 0.637i) with a bounds of (0.001, 0.001i)):

![Mandelbrot Sample](https://github.com/tamchow/Mandelbrot-CS/blob/master/Output.png)
