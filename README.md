# Mandelbrot-CS
Not-So-Simple multithreaded Mandelbrot Renderer with smooth coloring in C# with WinFoms GUI.

The API spports zooming and panning, but the application itself doesn't expose this interactively (command-line paramaters are necessary). I'll incorporate that sometime into the future (possibly). In the meantime, this program is open-source so that you can change what you see by editing the configuration in `Program.cs` 🤣.

## Controls:
1. <kbd>ESC</kbd> - Closes the display window.
2. <kbd>S</kbd> - Saves the image displayed in the invocation directory as `Image.png`.

## Command-line Switches:
1. `-s` or `--size` followed by `<width>,<height>` as a regex matching `$(\d+),(\d+)^`.
2. `-i` or `--iterations` followed by `<max iteration count>` as a regex matching `$(\d+)^`.
3. `-p` or `--palette` followed by a path to a text file specifying a palette configuration as a text file - see `paletteConfiguration.txt` in the root directory of the `master` branch for details on the format.

__Defaults__
`-s 3840,2160 -i 256`

## Running:
Easy as pie to run. Download the `.exe` from the [Releases](https://github.com/tamchow/Mandelbrot-CS/releases) page and double click. Enjoy the Mandelbrot!

# Obligatory screenshot:
It looks like this (a zoom with center at (0.16125, 0.637i) with a bounds of (0.001, 0.001i)):

![Mandelbrot Sample](https://github.com/tamchow/Mandelbrot-CS/blob/master/Output.png)

## P.S.

If I hadn't mentioned it already, this is pretty fast. The image above is 1920x1080, and it was generated in about 380 ms. That's 0.38 seconds! It could provide a frame-rate of 2.5 FPS at FHD! (Now if you're doing a smaller render, it'll be proportionately faster - a 640x480 (standard fractal resolution) render of the same region takes about 75ms (that's about 12.5FPS on a video).

Now, it is possible in the API to constrain the region of the output necessary, allowing to implement reasonably efficient zooming and panning when combined with frame-interpolation, allowing realtime renders at about 20-25FPS at 640x480. However, that's not my job - it's more in line with my patron's line of work (Elliot Media SDKs). Mr. Cameron Elliot (@[cameron-elliot](https://github.com/cameron-elliott)) has graciously sponsored the development of this program from its inception. I am very grateful for his support.
