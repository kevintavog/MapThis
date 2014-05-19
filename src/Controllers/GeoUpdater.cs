﻿using System;
using System.IO;
using NLog;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Collections.Generic;

namespace MapThis.Controllers
{
	public class GeoUpdater
	{
		static private readonly Logger logger = LogManager.GetCurrentClassLogger();
		static private bool autoCheckExifTool = true;

		static public void UpdateFiles(
			IList<string> filePaths, 
			double latitude, double longitude, 
			Action<string,int> fileCompletion,
			Action operationCompleted)
		{
			// Make sure each path is a file that exists - no support for directories in this method
			foreach (var file in filePaths)
			{
				if (false == File.Exists(file))
				{
					throw new ArgumentException("Path must be an existing file: " + file);
				}
			}

			if (autoCheckExifTool)
			{
				if (!autoCheckExifTool)
				{
					throw new InvalidOperationException("Unable to run ExifTool");
				}
			}

			var latRef = latitude < 0 ? "S" : "N";
			var longRef = longitude < 0 ? "W" : "E";
			var normLat = Math.Abs(latitude);
			var normlLong = Math.Abs(longitude);

			foreach (var file in filePaths)
			{
				if (false == File.Exists(file))
				{
					throw new ArgumentException("Path must be an existing file: " + file);
				}

				UpdateExif(file, normLat, latRef, normlLong, longRef);
				if (fileCompletion != null)
				{
					fileCompletion(file, filePaths.IndexOf(file));
				}
			}

			if (operationCompleted != null)
			{
				operationCompleted();
			}
		}

		static public bool CheckExifTool()
		{
			if (!File.Exists(GetExifPath()))
			{
				return false;
			}

			try
			{
				RunExifTool("-ver");
				autoCheckExifTool = false;
				return true;
			}
			catch (Exception e)
			{
				logger.Warn("Unable to invoke exif tool: {0}", e);
				return false;
			}
		}

		static private void UpdateExif(string filename, double exifLatitude, string latitudeRef, double exifLongitude, string longitudeRef)
		{
//			"exiftool -P -fast -q -overwrite_original -exif:gpslatitude=47.61498370877631 -exif:gpslatituderef=N -exif:gpslongitude=122.3395174741745 -exif:gpslongituderef=W sax.JPG"
			RunExifTool(
				"-P -fast -q -overwrite_original -exif:gpslatitude={0} -exif:gpslatituderef={1} -exif:gpslongitude={2} -exif:gpslongituderef={3} \"{4}\"",
				exifLatitude, 
				latitudeRef, 
				exifLongitude, 
				longitudeRef, 
				filename);
		}

		static private string GetExifPath()
		{
			// Mac specific path to exiftool
			return "/usr/bin/exiftool";
		}

		static private string RunExifTool(string commandLine, params object[] args)
		{
			// Arbitrary timeout for now...
			const int timeout = 10 * 1000;

			var psi = new ProcessStartInfo
			{
				FileName = GetExifPath(),
				Arguments = String.Format(commandLine, args),
				CreateNoWindow = true,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				UseShellExecute = false,
			};

			logger.Info("Running ExifTool: {0} {1}", psi.FileName, psi.Arguments);

			using (var outputWaitHandle = new AutoResetEvent(false))
			using (var errorWaitHandle = new AutoResetEvent(false))
			{
				var output = new StringBuilder(1024);
				var error = new StringBuilder(1024);
				var exitCode = -1;

				using (var process = new Process())
				{
					process.StartInfo = psi;
					process.OutputDataReceived += (sender, e) =>
					{
						if (e.Data == null)
						{
							outputWaitHandle.Set();
						}
						else
						{
							output.AppendLine(e.Data);
						}
					};

					process.ErrorDataReceived += (sender, e) =>
					{
						if (e.Data == null)
						{
							errorWaitHandle.Set();
						}
						else
						{
							error.AppendLine(e.Data);
						}
					};

					process.Start();

					process.BeginOutputReadLine();
					process.BeginErrorReadLine();

					if (process.WaitForExit(timeout) && 
						outputWaitHandle.WaitOne(timeout) && 
						errorWaitHandle.WaitOne(timeout))
					{
						exitCode = process.ExitCode;
					}
				}

				logger.Info("ExifTool stdout: '{0}'", output);
				logger.Info("ExifTool stderr: '{0}'", error);

				return output.ToString();
			}
		}
	}
}
