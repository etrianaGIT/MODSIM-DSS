using System;
using System.Collections.Specialized;
using System.IO;

namespace Csu.Modsim.ModsimIO
{
    public static class FileUtility
    {
        /// <summary>
        /// Creates a relative path from one file or folder to another
        /// </summary>
        /// <param name="fromPath">The path that defines the start of the relative path</param>
        /// <param name="toPath">The path that defines the end of the relative path</param>
        /// <returns>The relative path from the start path to the end path</returns>
        /// <remarks></remarks>
        public static string RelativePathTo(string fromPath, string toPath)
        {
            //taken from here:
            //https://github.com/usbr/Pisces/blob/master/Core/FileUtility.cs
            bool isRooted = Path.IsPathRooted(fromPath) && Path.IsPathRooted(toPath);

            if (isRooted)
            {
                bool isDifferentRoot = string.Compare(Path.GetPathRoot(fromPath), Path.GetPathRoot(toPath), true) != 0;
                if (isDifferentRoot)
                {
                    return toPath;
                }

            }

            StringCollection relativePath = new StringCollection();
            string[] fromPaths = fromPath.Split(Path.DirectorySeparatorChar);
            string[] toPaths = toPath.Split(Path.DirectorySeparatorChar);
            int length = Math.Min(fromPaths.Length, toPaths.Length);

            //find common root
            int lastCommonRoot = -1;
            for (int i = 0; i < length; i++)
            {
                if (string.Compare(fromPaths[i], toPaths[i], true) != 0)
                {
                    break; // TODO: might not be correct. Was : Exit For
                }
                lastCommonRoot = i;
            }

            if (lastCommonRoot == -1)
            {
                return toPath;
            }

            //add relative folders in from path
            for (int i = lastCommonRoot + 1; i < fromPaths.Length; i++)
            {
                if (fromPaths[i].Length > 0)
                {
                    relativePath.Add("..");
                }
            }

            //add to folders to path
            for (int i = lastCommonRoot + 1; i < toPaths.Length; i++)
            {
                relativePath.Add(toPaths[i]);
            }

            //create relative path
            string[] relativeParts = new string[relativePath.Count + 1];
            relativePath.CopyTo(relativeParts, 0);

            //create final relative path
            string newPath = string.Join(Path.DirectorySeparatorChar.ToString(), relativeParts);
            return newPath;

        }
        /// <summary>
        /// Constructs an absolute path if <paramref name="absoluteOrRelativePath"/> is a relative path.
        /// Otherwise <paramref name="absoluteOrRelativePath"/> is returned.
        /// </summary>
        /// <param name="fromPath">The path that defines the start of the relative path</param>
        /// <param name="absoluteOrRelativePath">The relative path</param>
        /// <returns>The the absolute path to <paramref name="absoluteOrRelativePath"/> if it is a relative path.
        /// Otherwise <paramref name="absoluteOrRelativePath"/> is returned.</returns>
        public static string AbsolutePathTo(string fromPath, string absoluteOrRelativePath)
        {
            //taken from here:
            //http://vgoncharov.blogspot.com/2010/01/convert-relative-path-to-absolute-in.html
            //Examples:
            //GetAbsolutePath("c:\temp\", "cache/")	                c:/temp/cache/
            //GetAbsolutePath("c:\windows\app.conf", "system32/")	c:/windows/system32/
            //GetAbsolutePath("c:\windows\temp\", "d:\cache\")	    d:/cache/
            if (absoluteOrRelativePath == string.Empty)
                return string.Empty;
            Uri fromUri = new Uri(fromPath);
            Uri newPath = fromUri;
            Uri.TryCreate(fromUri, absoluteOrRelativePath, out newPath);
            return Uri.UnescapeDataString(newPath.AbsolutePath);
        }
    }
}
