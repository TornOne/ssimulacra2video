using System;
using System.Diagnostics;
using System.Threading.Tasks;

static class Program {
	static void Main(string[] args) {
		if (args.Length < 2) {
			Console.WriteLine("Usage:\nssimulacara2video.exe original_video distorted_video [-t threads] [-s]");
			return;
		}
		int threadsArg = Array.IndexOf(args, "-t") + 1;
		int threads = threadsArg > 0 && int.TryParse(args[threadsArg], out threads) ? threads : Environment.ProcessorCount / 2;
		bool verbose = Array.IndexOf(args, "-s") < 0;

		//Initialize ffmpeg streams
		Ffmpeg orig = new(args[0]);
		Ffmpeg dist = new(args[1]);

		//Initialize ssimulacra2 bridges
		Ssimu2Bridge[] bridges = new Ssimu2Bridge[threads];
		Task<double>[] tasks = new Task<double>[threads];
		Array.Fill(tasks, Task.FromResult(double.NaN));

		//Compare the frames
		Stopwatch timer = Stopwatch.StartNew();
		int frameCounter = 0;
		double totalScore = 0;
		while (true) {
			int i = Task.WaitAny(tasks);
			if (bridges[i] is null) {
				bridges[i] = new Ssimu2Bridge(orig.stream, dist.stream);
			} else {
				frameCounter++;
				totalScore += tasks[i].Result;

				if (verbose && frameCounter % threads == 0) {
					Console.Write($"Frame = {frameCounter}, Speed = {frameCounter / timer.Elapsed.TotalSeconds:0.000}fps, Average similarity = {totalScore / frameCounter:0.000}");
					Console.CursorLeft = 0;
				}
			}

			if (!bridges[i].ReadFrame()) {
				frameCounter--;
				tasks[i] = Task.FromResult(0d);
				break;
			}
			tasks[i] = Task.Run(bridges[i].ProcessFrame);
		}

		Task.WaitAll(tasks);
		for (int i = 0; i < threads; i++) {
			if (bridges[i] is null) {
				continue;
			}

			frameCounter++;
			totalScore += tasks[i].Result;
		}

		if (verbose) {
			Console.WriteLine($"Frame = {frameCounter}, Speed = {frameCounter / timer.Elapsed.TotalSeconds:0.000}fps, Average similarity = {totalScore / frameCounter:0.000}");
		} else {
			Console.WriteLine(totalScore / frameCounter);
		}
	}
}
