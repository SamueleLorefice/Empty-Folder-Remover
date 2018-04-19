/*MIT License

Copyright(c) 2018 Samuele Lorefice

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.*/

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.IO;

namespace Empty_folder_remover
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void SelectPathBtn_Click(object sender, RoutedEventArgs e)
        {
            var Dialog = new System.Windows.Forms.FolderBrowserDialog();
            Dialog.ShowDialog();
            PathBox.Text = Dialog.SelectedPath;
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(PathBox.Text))
            {
                Logbox.Text += "\nERROR: root Directory is invalid. Please select a valid directory and retry";
            }
            else
            {
                Logbox.Text += "\nINFO: starting directory scan...";
                var dirs = new List<string>(Directory.GetDirectories(PathBox.Text, "*", SearchOption.AllDirectories));
                var EmptyDirs = new List<string>();
                Logbox.Text += "\nINFO: Total directories found = " + dirs.Count;
                Logbox.Text += "\nINFO: Starting empty dirs check";
                Parallel.ForEach(dirs, (currentDir) =>
                {
                    if (CheckDirectoryEmpty(currentDir))
                        EmptyDirs.Add(currentDir);
                });
                Logbox.Text += "\nINFO: Total empty directories found = " + EmptyDirs.Count;
                Logbox.Text += "\nINFO: Starting parallel deletion";
                Parallel.ForEach(EmptyDirs, (emptyDir) => { Directory.Delete(emptyDir); });
                Logbox.Text += "\nINFO: Finished.";
            }
        }

        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct WIN32_FIND_DATA
        {
            public uint dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll")]
        private static extern bool FindClose(IntPtr hFindFile);

        public static bool CheckDirectoryEmpty(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(path);
            }

            if (Directory.Exists(path))
            {
                if (path.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    path += "*";
                else
                    path += Path.DirectorySeparatorChar + "*";

                WIN32_FIND_DATA findData;
                var findHandle = FindFirstFile(path, out findData);

                if (findHandle != INVALID_HANDLE_VALUE)
                {
                    try
                    {
                        bool empty = true;
                        do
                        {
                            if (findData.cFileName != "." && findData.cFileName != "..")
                                empty = false;
                        } while (empty && FindNextFile(findHandle, out findData));

                        return empty;
                    }
                    finally
                    {
                        FindClose(findHandle);
                    }
                }

                throw new Exception("Failed to get directory first file",
                    Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
            }
            throw new DirectoryNotFoundException();
        }
    }
}
