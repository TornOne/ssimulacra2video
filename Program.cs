using System;
using System.Diagnostics;
using System.Threading.Tasks;

static class Program {
	static void Main(string[] args) {
		if (args.Length < 2) {
			Console.WriteLine("Usage:\nssimulacara2video.exe original_video distorted_video [-t threads] [-s] [-n skip]");
			return;
		}
		int threads = ParseArg(args, "-t", (Environment.ProcessorCount + 1) / 2);
		bool verbose = Array.IndexOf(args, "-s") < 0;
		int skip = ParseArg(args, "-n", 1);

		Ffmpeg orig = new(args[0]);
		Ffmpeg dist = new(args[1]);

		Ssimu2Bridge[] bridges = new Ssimu2Bridge[threads];
		Task<double>[] tasks = new Task<double>[threads];
		Array.Fill(tasks, Task.FromResult(double.NaN));

		Stopwatch timer = Stopwatch.StartNew();
		int frameCounter = 0;
		double totalScore = 0;
		int leftoverFrames = -1;
		while (true) {
			int i = Task.WaitAny(tasks);
			if (bridges[i] is null) {
				bridges[i] = new Ssimu2Bridge(orig.stream, dist.stream);
			} else {
				frameCounter++;
				totalScore += tasks[i].Result;

				if (verbose && frameCounter % threads == 0) {
					Console.Write($"Frame = {frameCounter * skip}, Speed = {frameCounter * skip / timer.Elapsed.TotalSeconds:0.000}fps, Average similarity = {totalScore / frameCounter:0.000}");
					Console.CursorLeft = 0;
				}
			}

			for (int n = 0; n < skip; n++) {
				if (!bridges[i].ReadFrame()) {
					frameCounter--;
					tasks[i] = Task.FromResult(0d);
					leftoverFrames = n;
					break;
				}
			}
			if (leftoverFrames >= 0) {
				break;
			}
			tasks[i] = Task.Run(bridges[i].ProcessFrame);
		}

		int unusedThreads = 0;
		Task.WaitAll(tasks);
		for (int i = 0; i < threads; i++) {
			if (bridges[i] is null) {
				unusedThreads++;
				continue;
			}

			frameCounter++;
			totalScore += tasks[i].Result;
		}

		if (verbose) {
			Console.WriteLine($"Frame = {frameCounter * skip + leftoverFrames}, Speed = {(frameCounter * skip + leftoverFrames) / timer.Elapsed.TotalSeconds:0.000}fps, Average similarity = {totalScore / frameCounter:0.000}");
			if (unusedThreads > 0) {
				Console.WriteLine($"{unusedThreads} of {threads} threads not used. Limited by video decode speed or pipe throughput. Consider lowering the number of threads or the amount of frames skipped.");
			}
		} else {
			Console.WriteLine(totalScore / frameCounter);
		}
	}

	static int ParseArg(string[] args, string arg, int defaultValue) {
		int i = Array.IndexOf(args, arg) + 1;
		return i > 0 && int.TryParse(args[i], out int value) ? value : defaultValue;
	}
}
