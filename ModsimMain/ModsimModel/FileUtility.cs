public static class GlobalMembersFileUtility
{
/* get rid of that nasty new line char at the end of it all */

	public static void chop(string strng)
	{
		if (strng[strng.Length - 1] == '\n')
		{
		strng = strng.Substring(0, strng.Length - 1);
		}
	}

/*****************************************************************************
This function sets the file extension of the filename `file' to the extension
`ext'.  If `file' already has an extension, it is replaced with `ext'.  Note
that there should not be more than one period in a file name.

If `ext' is NULL, any file extension is removed from the filename, including
the period.
\*****************************************************************************/

	public static void SetFileExtension(string file, string ext)
	{
		 int len;
		 int i;
		 int j;

	 len = file.Length;

	 /* Find the period in the filename.  If one doesn't exist, `i' will be less
	 ** than zero when this while loop finishes.  `j' counts to five so that only
	 ** the last five characters in the file name are checked.
	 */

	 i = len;
	 j = 0;
	 while ((--i >= 0) && (file[i] != '.'))
	 {
	   if (++j == 8)
		 i = 0;
	 }

	 /* If the above while loop found a file extension, remove it. */

	 if (i >= 0)
	   file = file.Substring(0, i);


	 /* Add the file extension to the file name, or leave no extension if ext is
	 ** a NULL string.
	 */

	 if (ext != null)
	 {
	   file += ".";
	   file += ext;
	 }
	}
}


