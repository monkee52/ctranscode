using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ChromecastTranscoder {
    class ChromecastTranscoder
    {
        private static void ShowHelp()
        {
            Console.WriteLine("Chromecast Transcoder");
            Console.WriteLine("----------------------------");
            Console.WriteLine("-i    --input=FILE     Input filename");
            Console.WriteLine("-o    --output=FILE    Output filename");
            Console.WriteLine("-h    --help           Print this message");
            Console.WriteLine();
        }

        private static double UnixTime()
        {
            TimeSpan unixTime = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            return unixTime.TotalSeconds;
        }

        delegate void DataReceiverDelegate(object o, DataReceivedEventArgs e);

        private static DataReceivedEventHandler DataReceiverFactory(StringBuilder output)
        {
            return delegate (object o, DataReceivedEventArgs e)
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    output.Append(e.Data + Environment.NewLine);
                }
            };
        }

        private static string[] ffmpeg(string args, bool redirStdError = false)
        {
            Process ffmpegProcess = new Process();

            ffmpegProcess.StartInfo.FileName = "ffmpeg.exe";
            ffmpegProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            ffmpegProcess.StartInfo.Arguments = args;
            ffmpegProcess.StartInfo.UseShellExecute = false;
            ffmpegProcess.StartInfo.RedirectStandardOutput = true;
            ffmpegProcess.StartInfo.RedirectStandardInput = true;

            if (redirStdError)
            {
                ffmpegProcess.StartInfo.RedirectStandardError = true;
            }

            ffmpegProcess.Start();

            StringBuilder output = new StringBuilder();

            ffmpegProcess.OutputDataReceived += new DataReceivedEventHandler(DataReceiverFactory(output));
            ffmpegProcess.BeginOutputReadLine();
            ffmpegProcess.ErrorDataReceived += new DataReceivedEventHandler(DataReceiverFactory(output));
            ffmpegProcess.BeginErrorReadLine();

            double time = UnixTime();
            int ctr = 0;

            while (!ffmpegProcess.HasExited)
            {
                if (UnixTime() - time > (1.0 / 15.0))
                {
                    switch (ctr++ % 4)
                    {
                        case 0:
                            Console.Write("\r-\r");
                            break;
                        case 1:
                            Console.Write("\r\\\r");
                            break;
                        case 2:
                            Console.Write("\r|\r");
                            break;
                        case 3:
                            Console.Write("\r/\r");
                            break;
                    }

                    time = UnixTime();
                }
            }

            Console.Write("\r \r");

            return output.ToString().Split(new[] { "\r\n", "\n\r", "\r", "\n" }, StringSplitOptions.None);
        }

        private static string[] SGrep(string[] S, string Filter)
        {
            List<string> lines = new List<string>();

            foreach (string line in S)
            {
                if (line.Contains(Filter))
                {
                    lines.Add(line);
                }
            }

            return lines.ToArray();
        }

        private static void WriteLines(string[] lines)
        {
            foreach (string line in lines)
            {
                Console.WriteLine(line);
            }
        }

        public static void Main(string[] args)
        {
            string InputFile = null;
            string OutputFile = null;

            int index = 0;

            while (index < args.Length)
            {
                if (args[index] == "-i")
                {
                    ++index;
                    InputFile = args[index++];
                }
                else if (args[index].StartsWith("--input"))
                {
                    if (args[index].StartsWith("--input="))
                    {
                        InputFile = args[index++].Substring(8);
                    }
                    else
                    {
                        ++index;
                        InputFile = args[index++];
                    }
                }
                else if (args[index] == "-o")
                {
                    ++index;
                    OutputFile = args[index++];
                }
                else if (args[index].StartsWith("--output"))
                {
                    if (args[index].StartsWith("--output="))
                    {
                        OutputFile = args[index++].Substring(9);
                    }
                    else
                    {
                        ++index;
                        OutputFile = args[index++];
                    }
                }
                else if (args[index] == "-h" || args[index] == "--help")
                {
                    ShowHelp();
                    return;
                } else
                {
                    Console.WriteLine("Unknown argument: " + args[index]);
                    ShowHelp();
                    return;
                }
            }

            if (InputFile == null)
            {
                ShowHelp();
                return;
            }

            if (OutputFile == null)
            {
                OutputFile = Path.GetFileNameWithoutExtension(InputFile) + ".mp4";
            }

            Console.WriteLine("Input file: " + InputFile);
            Console.WriteLine("Output file: " + OutputFile);

            Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";C:\\tmp\\PApps\\ffmpeg\\bin");

            string vcodec;
            string acodec;

            string[] info = ffmpeg("-i \"" + InputFile + "\"", true);

            if (SGrep(SGrep(info, "Video:"), "h264").Length > 0)
            {
                if (SGrep(SGrep(info, "Video:"), "High 10").Length > 0)
                {
                    vcodec = "libx264";
                }
                else
                {
                    vcodec = "copy";
                }
            }
            else
            {
                vcodec = "libx264";
            }

            if (SGrep(SGrep(info, "Audio:"), "aac").Length > 0 || SGrep(SGrep(info, "Audio:"), "mp3").Length > 0)
            {
                acodec = "copy";
            }
            else
            {
                acodec = "libvo_aacenc";
            }

            string arguments = "-i \"" + InputFile + "\" -f mp4 -acodec " + acodec + " -ab 192k -ac 2 -vcodec " + vcodec + " -qmax 22 -qmin 20 \"" + OutputFile + "\"";

            Console.WriteLine("ffmpeg " + arguments);

            Process p = new Process();

            p.StartInfo.FileName = "ffmpeg";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.Arguments = arguments;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

            p.Start();
            p.WaitForExit();
        }
    }
}
