## About
An ffmpeg-based version of [ssimulacra2](https://github.com/cloudinary/ssimulacra2/tree/main) for video.  
Needs an existing build of ffmpeg on your system, and preferrably in your path. (Might work with the ffmpeg executable named "ffmpeg" without the .exe adjacent to the ssimulacra2video executable.) ffprobe also recommended.

Releases only work on Windows. The non-standalone release requires .NET 9.  
If you want to build this yourself, follow the build instructions in the ssimulacra2 repository linked above, replace `ssimulacra2_main.cc`, and build as a library.

## Usage
`ssimulacara2video.exe original_video distorted_video [-t threads] [-s] [-n skip]`  
`-t threads` - The amount of threads to use. By default half your cores, rounded up. Threads after the first several usually start to see heavily diminishing returns.  
`-s` - Enable silent mode. Will not output progress, only the average score at the very end.  
`-n skip` - 1 out of how many frames to compare. Compares every frame by default.
