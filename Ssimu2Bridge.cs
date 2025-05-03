using System;
using System.IO;
using System.Runtime.InteropServices;

unsafe partial class Ssimu2Bridge {
	[LibraryImport("ssimulacra2.dll", EntryPoint = "initialize")]
	private static partial void** GetBuffers(int width, int height);

	[LibraryImport("ssimulacra2.dll", EntryPoint = "compute_ssimu2")]
	private static partial double ComputeScore(void* orig, void* dist);

	public static int width, height, planeSize;

	readonly Stream origVideo, distVideo;
	readonly byte*[] origFrame = new byte*[3];
	readonly byte*[] distFrame = new byte*[3];
	readonly void* orig, dist;

	public Ssimu2Bridge(Stream origVideo, Stream distVideo) {
		this.origVideo = origVideo;
		this.distVideo = distVideo;

		void** bufferData = GetBuffers(width, height);
		for (int i = 0; i < 3; i++) {
			origFrame[i] = (byte*)bufferData[i * 2];
			distFrame[i] = (byte*)bufferData[i * 2 + 1];
		}
		orig = bufferData[6];
		dist = bufferData[7];
	}

	public bool ReadFrame() => ReadFrame(origVideo, origFrame) && ReadFrame(distVideo, distFrame);

	public double ProcessFrame() => ComputeScore(orig, dist);

	static bool ReadFrame(Stream stream, byte*[] frame) {
		for (int i = 0; i < 3; i++) {
			Span<byte> planeBuffer = new(frame[i], planeSize);
			int bytesRead = 0;
			while (bytesRead < planeSize) {
				int read = stream.Read(planeBuffer[bytesRead..]);
				if (read == 0) {
					return false;
				}
				bytesRead += read;
			}
		}
		return true;
	}
}
