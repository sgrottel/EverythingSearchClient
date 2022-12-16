using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EverythingSearchClient.TestProject
{
	internal class DataGenerator
	{

		public string TestDataRootName { get; set; }

		public string TestDataRootDirectory { get; set; }

		public DataGenerator()
		{
			string p = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Environment.CurrentDirectory;
			TestDataRootName = string.Format("ESC{0}_TestData", Process.GetCurrentProcess().Id);
			TestDataRootDirectory = Path.Combine(p, TestDataRootName);

			// If you edit this list of test directories and file, it will affect the expected results of all tests!
			DirectoryInfo root = EnsureDir(TestDataRootDirectory);
			EnsureFile(root, "FileA.txt");
			EnsureFile(root, "FileB.txt");
			EnsureFile(root, "FileC.xml");
			DirectoryInfo sub1 = EnsureDir(root, "SubDir1");
			EnsureFile(sub1, "fileA.jpg");
			EnsureFile(sub1, "FileD.gif");
			DirectoryInfo sub2 = EnsureDir(root, "SubDir2");
			EnsureFile(sub2, "fileA.html");
			EnsureFile(sub2, "FileE.dat");
			DirectoryInfo sub2sub = EnsureDir(sub2, "SubSubDirA");
			EnsureFile(sub2sub, "FileA.json");
			EnsureFile(sub2sub, "FileF.txt");

			// We need to wait for EverythingIndex to be built up
			SearchClient everything = new();
			Result res = new();
			DateTime startWaiting = DateTime.Now;
			while (res.TotalItems == 0)
			{
				if ((DateTime.Now - startWaiting).TotalMinutes > 5.0)
				{
					throw new Exception("Everything file indexing took longer than 5 minutes");
				}
				Thread.Sleep(20);
				try
				{
					res = everything.Search("File " + TestDataRootDirectory, SearchClient.BehaviorWhenBusy.Error);
				}
				catch
				{
					res = new();
				}
			}
		}

		public DateTime TestCreationTime { get; } = new DateTime(2015, 4, 1, 14, 44, 59);

		public DateTime TestLastWriteTime { get; } = new DateTime(2016, 6, 30, 12, 0, 1);

		private void EnsureFile(DirectoryInfo dir, string filename)
		{
			string path = Path.Combine(dir.FullName, filename);
			if (!File.Exists(path))
			{
				File.WriteAllText(path, "Test Data");
				File.SetCreationTime(path, TestCreationTime);
				File.SetLastWriteTime(path, TestLastWriteTime);
			}
		}

		private DirectoryInfo EnsureDir(string testDir)
		{
			if (!Directory.Exists(testDir))
			{
				Directory.CreateDirectory(testDir);
			}
			return new(testDir);
		}

		private DirectoryInfo EnsureDir(DirectoryInfo dir, string testSubDir)
		{
			return EnsureDir(Path.Combine(dir.FullName, testSubDir));
		}

		public bool Contains(Result res, string name)
		{
			string p = Path.GetDirectoryName(Path.Combine(TestDataRootDirectory, name)) ?? string.Empty;
			string n = Path.GetFileName(name);
			foreach (Result.Item i in res.Items)
			{
				if (string.Compare(i.Name, n) != 0) continue;
				if (!i.Path.EndsWith(p)) continue;
				return true;
			}
			return false;
		}

	}
}
