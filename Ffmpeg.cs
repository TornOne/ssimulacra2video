using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

class Ffmpeg {
	public readonly Process process;
	public readonly Stream stream;

	public Ffmpeg(string path, int skip) {
		GetVideoSize(path);

		List<string> args = ["-hide_banner", "-loglevel", "error", "-i", path, "-map", "v:0"];
		if (skip > 1) {
			args.Add("-vf");
			args.Add($"fps=source_fps/{skip}");
		}
		args.AddRange(["-pix_fmt", "gbrpf32le", "-f", "rawvideo", "-"]);
		ProcessStartInfo startInfo = new("ffmpeg", args) {
			RedirectStandardOutput = true
		};
		process = Process.Start(startInfo)!;
		stream = process.StandardOutput.BaseStream;
	}

	static void GetVideoSize(string path) {
		if (Ssimu2Bridge.planeSize > 0) {
			return;
		}

		try {
			ProcessStartInfo ffprobeStartInfo = new("ffprobe", ["-hide_banner", "-of", "default=nw=1", "-select_streams", "v:0", "-show_entries", "stream=width,height", path]) {
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};
			Process process = Process.Start(ffprobeStartInfo)!;
			StreamReader ffprobeOut = process.StandardOutput;

			while (!ffprobeOut.EndOfStream) {
				string[] pair = process.StandardOutput.ReadLine()!.Split('=', 2);
				string key = pair[0];
				string value = pair[1];

				if (key == "width") {
					Ssimu2Bridge.width = int.Parse(value);
				} else if (key == "height") {
					Ssimu2Bridge.height = int.Parse(value);
				}
			}
		} catch {
			Console.WriteLine("Failed to get video size via ffprobe.");
			Ssimu2Bridge.width = GetInt("Enter video width in pixels:");
			Ssimu2Bridge.height = GetInt("Enter video height in pixels:");
		}

		Ssimu2Bridge.planeSize = Ssimu2Bridge.width * Ssimu2Bridge.height * 4;
	}

	static int GetInt(string prompt) {
		Console.WriteLine(prompt);
		while (true) {
			if (int.TryParse(Console.ReadLine(), out int val)) {
				return val;
			}
			Console.WriteLine("Failed to parse. Try again.");
		}
	}
}
