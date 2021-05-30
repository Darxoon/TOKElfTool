using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using ZstdNet;
using Path = System.IO.Path;

namespace TOKElfTool.ProgressReports
{
	public enum ZstdMethod
    {
        Decompress,
        Compress,
    }

    /// <summary>
    /// Interaction logic for ZstdFolderProgressPopupWindow.xaml
    /// </summary>
    public partial class ZstdFolderProgressPopupWindow : Window
    {
        private BackgroundWorker worker;

        public string TargetDir { get; set; }
        public string OutputDir { get; set; }
        public ZstdMethod Method { get; set; }

        public ZstdFolderProgressPopupWindow()
        {
            InitializeComponent();

            worker = new BackgroundWorker()
            {
                WorkerReportsProgress = true,
            };
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            List<Thread> threads = new List<Thread>();

            void DecompressFolder(string path)
            {
                // Check subdirectories recursively
                foreach (string child in Directory.GetDirectories(path))
                {
                    DecompressFolder(child);
                }

                // Decompress files
                foreach (string file in Directory.GetFiles(path))
                {
                    worker.ReportProgress(0);
                    Thread thread = new Thread(() =>
                    {
                        string outputPath = Path.Combine(OutputDir, Util.GetRelativePath(TargetDir, file));
                        if (Method == ZstdMethod.Decompress && outputPath.EndsWith(".zst"))
                            outputPath = outputPath.Substring(0, outputPath.Length - ".zst".Length);
                        if (Method == ZstdMethod.Compress && !outputPath.EndsWith(".zst"))
                            outputPath += ".zst";

                        byte[] input;
                        try
                        {
                            input = File.ReadAllBytes(file);
                        }
                        catch
                        {
                            MyMessageBox.Show(this, $"Couldn't read file '{file}'.\nMaybe it's opened by another program or doesn't exist",
                                "TOK ELF Tool ZSTD Tools", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                            return;
                        }

                        byte[] result;
                        if (Method == ZstdMethod.Decompress)
                        {
                            using Decompressor decompressor = new Decompressor();
                            result = decompressor.Unwrap(input);
                        }
                        else
                        {
                            using Compressor compressor = new Compressor();
                            result = compressor.Wrap(input);
                        }
                        File.WriteAllBytes(outputPath, result);
                    });
                    thread.Start();
                    threads.Add(thread);
                    worker.ReportProgress(1);
                }
            }

            DecompressFolder(TargetDir);

            for (int i = threads.Count - 1; i >= 0; i--)
            {
                threads[i].Join();
                threads.PopBack();
            }
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            switch (e.ProgressPercentage)
            {
                case 0:
                    progressBar.Maximum += 1;
                    break;
                case 1:
                    progressBar.Value += 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            DialogResult = true;
        }


        private void ZstdFolderProgressPopupWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            worker.RunWorkerAsync();
        }
    }
}
