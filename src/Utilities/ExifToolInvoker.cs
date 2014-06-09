using System;
using System.Text;
using System.Diagnostics;
using NLog;
using System.IO;
using System.Threading;

namespace MapThis.Utilities
{
	public class ExifToolInvoker
	{
		public string OutputString { get { return outputBuffer.ToString(); } }
		public string ErrorString { get { return errorBuffer.ToString(); } }

		static public ExifToolInvoker Run(string commandLine, params object[] args)
		{
			if (autoCheckExifTool)
			{
				if (!CheckExifTool())
				{
					throw new InvalidOperationException("Unable to run ExifTool");
				}
			}

			var invoker = new ExifToolInvoker();
			invoker.Invoke(commandLine, args);
			return invoker;
		}

		static private readonly Logger logger = LogManager.GetCurrentClassLogger();
		static private bool autoCheckExifTool = true;

		private StringBuilder outputBuffer = new StringBuilder(1024);
		private StringBuilder errorBuffer = new StringBuilder(1024);

		private ExifToolInvoker()
		{
		}

		private void Invoke(string commandLine, params object[] args)
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
							outputBuffer.AppendLine(e.Data);
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
							errorBuffer.AppendLine(e.Data);
						}
					};

					process.Start();

					process.BeginOutputReadLine();
					process.BeginErrorReadLine();

					if (process.WaitForExit(timeout) &&
						outputWaitHandle.WaitOne(timeout) &&
						errorWaitHandle.WaitOne(timeout))
					{
						var exitCode = process.ExitCode;
						if (exitCode != 0)
						{
							if (exitCode != 1)
							{
								logger.Warn("ExifTool exit code: {0}", exitCode);
							}
							throw new InvalidOperationException("ExifTool returned an error: " + exitCode + "; " + ErrorString);
						}
					}
				}
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
				new ExifToolInvoker().Invoke("-ver");
				autoCheckExifTool = false;
				return true;
			}
			catch (Exception e)
			{
				logger.Warn("Unable to invoke exif tool: {0}", e);
				return false;
			}
		}

		static private string GetExifPath()
		{
			// Mac specific path to exiftool
			return "/usr/bin/exiftool";
		}
	}
}

