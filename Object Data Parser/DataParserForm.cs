﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;

namespace Object_Data_Parser
{
	public partial class DataParserForm : Form
	{
		private Directory dataDir;

		public DataParserForm()
		{
			InitializeComponent();
		}

		private void CleanNewLines(object sender, EventArgs e)
		{
			if (sender == null || !(sender is TextBox txtBox) || !txtBox.Text.Contains(Environment.NewLine)) return;

			int selStart = txtBox.SelectionStart - Environment.NewLine.Length;

			txtBox.Text = txtBox.Text.Replace(Environment.NewLine, "");

			txtBox.SelectionStart = selStart;
		}

		private void SetDir(string path)
		{
			txtIn.Text = path;

			FileManager file = FileManager.FromPath(path);

			if (file != null && file.FileType == FileType.Directory)
			{
				dataDir = (Directory)file;
			}
			else
			{
				dataDir = null;
			}
		}

		private void SetFile(string path)
		{
			txtOut.Text = path;
		}

		private void SetFile2(string path)
		{
			txtOutCsv.Text = path;
		}

		private void DataParserForm_Load(object sender, EventArgs e)
		{
			MinimumSize = Size;
		}

		private void BtnBrowseIn_Click(object sender, EventArgs e)
		{
			DoDirDialog();
		}

		private void BtnBrowseOut_Click(object sender, EventArgs e)
		{
			DoFileDialog();
		}

		private void BtnBrowseOutCsv_Click(object sender, EventArgs e)
		{
			DoFile2Dialog();
		}

		private void DoDirDialog()
		{
			using (FolderBrowserDialog dialog = new FolderBrowserDialog())
			{
				if (dialog.ShowDialog() == DialogResult.OK)
				{
					SetDir(dialog.SelectedPath);
				}
			}
		}

		private void DoFileDialog()
		{
			using (SaveFileDialog dialog = new SaveFileDialog {Filter = "Text File|*.txt", Title = "Output Text File", FileName = "unknown.txt"})
			{
				if (dialog.ShowDialog() == DialogResult.OK)
				{
					SetFile(dialog.FileName);
				}
			}
		}

		private void DoFile2Dialog()
		{
			using (SaveFileDialog dialog = new SaveFileDialog {Filter = "CSV Table|*.csv", Title = "Output CSV File", FileName = "unknown.csv"})
			{
				if (dialog.ShowDialog() == DialogResult.OK)
				{
					SetFile2(dialog.FileName);
				}
			}
		}

		private void DoFileHover(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
		}

		private void DoDirDrop(object sender, DragEventArgs e)
		{
			string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
			foreach (string file in files) Console.WriteLine(file);

			if (files.Length > 0)
			{
				SetDir(files[0]);
			}
		}

		private void DoFileDrop(object sender, DragEventArgs e)
		{
			string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
			foreach (string file in files) Console.WriteLine(file);

			if (files.Length > 0)
			{
				SetFile(files[0]);
			}
		}

		private void DoFile2Drop(object sender, DragEventArgs e)
		{
			string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
			foreach (string file in files) Console.WriteLine(file);

			if (files.Length > 0)
			{
				SetFile2(files[0]);
			}
		}

		private void BtnProcess_Click(object sender, EventArgs e)
		{
			ProcessFiles();
		}

		private void ProcessFiles()
		{
			SetDir(txtIn.Text);

			if (dataDir != null)
			{
				int curFileCount = 0;
				int maxFileCount = dataDir.GetFiles().Length * ((string.IsNullOrEmpty(txtOut.Text) ? 0 : 1) + (string.IsNullOrEmpty(txtOutCsv.Text) ? 0 : 1)) + (string.IsNullOrEmpty(txtOut.Text) ? 0 : 3);
				progressBar.Value = 0;

				CsvGen csvGen = new CsvGen
				{
					columnNames = new List<string>(new string[] { "Time (Minutes)" })
				};

				/*
				 * Diffs Generation
				 */
				if (!string.IsNullOrEmpty(txtOut.Text))
				{
					Dictionary<string, float> objectsList = new Dictionary<string, float>();

					foreach (File file in dataDir.GetFiles())
					{
						if (file != null && file.Name.EndsWith("Diffs.txt"))
						{
							foreach (string line in System.IO.File.ReadAllLines(file.Path))
							{
								if (string.IsNullOrEmpty(line))
								{
									continue;
								}

								string[] split = line.Split(new string[] {" # "}, StringSplitOptions.None);

								if (split.Length == 2 && !string.IsNullOrEmpty(split[0]) && float.TryParse(split[1], out float parsedCount))
								{
									string parsedName = split[0];

									float value = 0;
									if (objectsList.ContainsKey(parsedName) && objectsList.TryGetValue(parsedName, out value))
									{
										objectsList.Remove(parsedName);
									}

									objectsList.Add(parsedName, value + parsedCount);
								}
							}
						}

						progressBar.Value = ++curFileCount / maxFileCount;
					}

					List<string> list = new List<string>();

					foreach (KeyValuePair<string, float> objectCount in objectsList)
					{
						if (objectCount.Value != 0)
						{
							list.Add(objectCount.Key + " # " + objectCount.Value);
						}
					}

					objectsList.Clear();

					for (int i = 0; i < 1000; i++)
					{
						try
						{
							System.IO.File.WriteAllLines(txtOut.Text, list.ToArray());
							break;
						}
						catch
						{
							// ignored
						}
					}

					list.Clear();
				}

				/*
				 * CSV Generation
				 */
				if (!string.IsNullOrEmpty(txtOutCsv.Text))
				{
					Dictionary<string, Dictionary<int, float>> csvObjectsList = new Dictionary<string, Dictionary<int, float>>();

					int rowCount = 0;

					foreach (File file in dataDir.GetFiles())
					{
						if (file != null && !file.Name.EndsWith("Diffs.txt"))
						{
							foreach (string line in System.IO.File.ReadAllLines(file.Path))
							{
								if (string.IsNullOrEmpty(line)) continue;

								string[] split = line.Split(new string[] {" # "}, StringSplitOptions.None);

								if (split.Length != 2 || string.IsNullOrEmpty(split[0]) || !float.TryParse(split[1], out float parsedCount)) continue;

								string parsedName = split[0];
								string strParsedMinutes = file.Name.Substring("LogNum".Length); // "LogNum2.txt"
								strParsedMinutes = strParsedMinutes.Remove(strParsedMinutes.IndexOf(".txt", StringComparison.Ordinal)); // "2.txt"

								if (!int.TryParse(strParsedMinutes, out int parsedMinutes)) continue;

								if (csvObjectsList.ContainsKey(parsedName) && csvObjectsList.TryGetValue(parsedName, out Dictionary<int, float> value))
								{
									value.Add(parsedMinutes, parsedCount);
								}
								else
								{
									Dictionary<int, float> dict = new Dictionary<int, float>
									{
										{parsedMinutes, parsedCount}
									};

									csvObjectsList.Add(parsedName, dict);
								}
							}

							rowCount++;
						}

						progressBar.Value = ++curFileCount / maxFileCount;
					}

					Dictionary<string, float[]> columns = new Dictionary<string, float[]>();
					List<string>[] rows = new List<string>[rowCount];

					foreach (KeyValuePair<string, Dictionary<int, float>> objectCount in csvObjectsList)
					{
						float[] column = new float[rowCount];

						for (int i = 0; i < column.Length; i++)
						{
							column[i] = (objectCount.Value.TryGetValue(i, out float value) ? value : 0);
						}

						columns.Add(objectCount.Key, column);
					}

					for (int i = 0; i < rows.Length; i++)
					{
						rows[i] = new List<string>(new string[] {i.ToString()});
					}

					for (int i = 0; i < rows.Length; i++)
					{
						foreach (KeyValuePair<string, float[]> column in columns)
						{
							rows[i].Add(column.Value[i].ToString(CultureInfo.InvariantCulture));
						}
					}

					/*
					 * Finally add data to CSV
					 */
					foreach (KeyValuePair<string, float[]> column in columns)
					{
						csvGen.columnNames.Add(column.Key);
					}

					csvGen.rows = new List<List<string>>(rows);

					progressBar.Value = ++curFileCount / maxFileCount;

					for (int i = 0; i < 1000; i++)
					{
						try
						{
							csvGen.WriteTable(txtOutCsv.Text);
							break;
						}
						catch
						{
							// ignored
						}
					}
				}

				progressBar.Value = 100;

				const string message = "Data has successfully been processed!";
				const string caption = "Process Complete!";
				const MessageBoxButtons buttons = MessageBoxButtons.OK;

				// Displays the MessageBox.
				MessageBox.Show(message, caption, buttons);
			}
			else if (dataDir == null)
			{
				const string message = "You did not enter a valid directory. Would you like to choose a new directory?";
				const string caption = "Error: Can't find directory";
				const MessageBoxButtons buttons = MessageBoxButtons.YesNo;

				// Displays the MessageBox.

				DialogResult result = MessageBox.Show(message, caption, buttons);

				if (result == DialogResult.Yes)
				{
					DoDirDialog();
				}
			}
		}
	}
}
