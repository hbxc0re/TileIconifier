﻿using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using IWshRuntimeLibrary;

namespace TileIconifier.Utilities
{
    // Source: https://astoundingprogramming.wordpress.com/2012/12/17/how-to-get-the-target-of-a-windows-shortcut-c/
    public static class ShortcutUtils
    {
        public static string GetTargetPath(string filePath)
        {
            var targetPath = ResolveMsiShortcut(filePath) ?? ResolveShortcut(filePath);

            return targetPath;
        }

        public static string GetInternetShortcut(string filePath)
        {
            var url = "";

            using (TextReader reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("URL="))
                    {
                        var splitLine = line.Split('=');
                        if (splitLine.Length > 0)
                        {
                            url = splitLine[1];
                            break;
                        }
                    }
                }
            }

            return url;
        }

        public static string ResolveShortcut(string filePath)
        {
            // IWshRuntimeLibrary is in the COM library "Windows Script Host Object Model"
            var shell = new WshShell();

            try
            {
                var shortcut = (IWshShortcut) shell.CreateShortcut(filePath);
                return shortcut.TargetPath;
            }
            catch (COMException)
            {
                // A COMException is thrown if the file is not a valid shortcut (.lnk) file 
                return null;
            }
        }

        public static void CreateLnkFile(string shortcutPath, string targetPath, string description,
            string workingDirectory = null, string iconPath = null)
        {
            var directoryInfo = new FileInfo(shortcutPath).Directory;
            if (directoryInfo == null) return;
            Directory.CreateDirectory(directoryInfo.FullName);

            var wsh = new WshShell();
            var shortcut = wsh.CreateShortcut(
                shortcutPath) as IWshShortcut;
            if (shortcut != null)
            {
                shortcut.Arguments = "";
                shortcut.TargetPath = targetPath;
                shortcut.WindowStyle = 1;
                shortcut.Description = description;
                shortcut.WorkingDirectory = workingDirectory ?? new FileInfo(targetPath).Directory?.FullName;
                shortcut.IconLocation = iconPath ?? targetPath;
                shortcut.Save();
            }
        }

        private static string ResolveMsiShortcut(string file)
        {
            var product = new StringBuilder(NativeMethods.MaxGuidLength + 1);
            var feature = new StringBuilder(NativeMethods.MaxFeatureLength + 1);
            var component = new StringBuilder(NativeMethods.MaxGuidLength + 1);

            NativeMethods.MsiGetShortcutTarget(file, product, feature, component);

            var pathLength = NativeMethods.MaxPathLength;
            var path = new StringBuilder(pathLength);

            var installState = NativeMethods.MsiGetComponentPath(product.ToString(), component.ToString(), path,
                ref pathLength);
            return installState == NativeMethods.InstallState.Local ? path.ToString() : null;
        }

        private static class NativeMethods
        {
            public enum InstallState
            {
                /*NotUsed = -7,
                BadConfig = -6,
                Incomplete = -5,
                SourceAbsent = -4,
                MoreData = -3,
                InvalidArg = -2,
                Unknown = -1,
                Broken = 0,
                Advertised = 1,
                Removed = 1,
                Absent = 2,*/
                Local = 3,
                /*Source = 4,
                Default = 5*/
            }

            public const int MaxFeatureLength = 38;
            public const int MaxGuidLength = 38;
            public const int MaxPathLength = 1024;

            [DllImport("msi.dll", CharSet = CharSet.Unicode)]
            public static extern uint MsiGetShortcutTarget(string targetFile, StringBuilder productCode,
                StringBuilder featureId, StringBuilder componentCode);

            [DllImport("msi.dll", CharSet = CharSet.Unicode)]
            public static extern InstallState MsiGetComponentPath(string productCode, string componentCode,
                StringBuilder componentPath, ref int componentPathBufferSize);
        }
    }
}