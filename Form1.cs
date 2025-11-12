using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MinuEpood
{
    public partial class Form1 : Form
    {
        private SqlConnection _conn = new SqlConnection(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\opilane\source\repos\Poldsaar\MinuEpood\Database1.mdf;Integrated Security=True");
        private SqlCommand _command;
        private SqlDataAdapter _adapterToode, _adapterKategooria;
        public string SaveFileName = "";

        public Form1()
        {
            InitializeComponent();
            NaitaAndmed();
            NaitaKategooriad();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var categoryExists = false;
            foreach (var item in Kat_Box.Items)
            {
                if (item.ToString() != Kat_Box.Text)
                    continue;

                categoryExists = true;
                break;
            }

            if (!categoryExists)
            {
                _command = new SqlCommand("INSERT INTO Kategooriad (Nimetus) VALUES (@kat)", _conn);
                _conn.Open();
                _command.Parameters.AddWithValue("@kat", Kat_Box.Text);
                _command.ExecuteNonQuery();
                _conn.Close();
                Kat_Box.Items.Clear();
                NaitaKategooriad();
            }
            else
            {
                MessageBox.Show("Selline kategooriat on juba olemas!");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (Kat_Box.SelectedItem != null)
            {
                _conn.Open();
                var value = Kat_Box.SelectedItem.ToString();
                _command = new SqlCommand("DELETE FROM Kategooriad WHERE Nimetus = @kat", _conn);
                _command.Parameters.AddWithValue("@kat", value);
                _command.ExecuteNonQuery();
                _conn.Close();
                Kat_Box.Items.Clear();
                NaitaKategooriad();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var open = new OpenFileDialog();
            open.InitialDirectory = @"C:\Users\opilane\Pictures";
            open.Multiselect = true;
            open.Filter = "Image Files(*.jpg;*.bmp;*.png;*.jpg)|*.jpged;*.bmp;*.png;*.jpg";

            var info = new FileInfo(@"C:\Users\opilane\Pictures\" + open.FileName);
            if (open.ShowDialog() == DialogResult.OK && Toode_txt.Text != null)
            {
                var save = new SaveFileDialog();
                save.InitialDirectory = Path.GetFullPath(@"..\..\images");
                var extension = Path.GetExtension(open.FileName);
                save.FileName = Toode_txt.Text + extension;
                save.Filter = "Images" + extension + "|" + extension;
                if (save.ShowDialog() == DialogResult.OK && Toode_txt.Text != null)
                {
                    File.Copy(open.FileName, save.FileName);
                    pictureBox1.Image = Image.FromFile(save.FileName);
                }

                SaveFileName = open.FileName;
            }
            else
            {
                MessageBox.Show("Puudub toode nimetus või oli vajutatud Cancel.");
            }
        }

        private void NaitaKategooriad()
        {
            _conn.Open();
            _adapterKategooria = new SqlDataAdapter("SELECT Id, Nimetus FROM Kategooriad", _conn);
            var table = new DataTable();
            _adapterKategooria.Fill(table);
            foreach (DataRow item in table.Rows)
            {
                if (!Kat_Box.Items.Contains(item["Nimetus"]))
                {
                    Kat_Box.Items.Add(item["Nimetus"]);
                }
                else
                {
                    _command = new SqlCommand("DELETE FROM Kategooriad WHERE Id = @id", _conn);
                    _command.Parameters.AddWithValue("@id", item["Id"]);
                    _command.ExecuteNonQuery();
                }
            }
            _conn.Close();
        }

        private void NaitaAndmed()
        {
            _conn.Open();
            var toodeTable = new DataTable();
            _adapterToode = new SqlDataAdapter("SELECT Tooded.Id, Tooded.Nimetus, Tooded.Kogus, " +
                "Tooded.Hind, Tooded.Pilt, Tooded.Bpilt, Kategooriad.Nimetus " +
                "as Kategooria_Nimetus FROM Tooded INNER JOIN Kategooriad on Tooded.KategooriaId = Kategooriad.Id", _conn);
            _adapterToode.Fill(toodeTable);
            dataGridView1.Columns.Clear();
            dataGridView1.DataSource = toodeTable;
            var comboCategory = new DataGridViewComboBoxColumn();
            comboCategory.DataPropertyName = "Kategooria";
            var keys = new HashSet<string>();
            foreach (DataRow row in toodeTable.Rows)
            {
                var name = row["Nimetus"].ToString();
                if (!keys.Contains(name))
                {
                    keys.Add(name);
                    comboCategory.Items.Add(name);
                }
            }
            dataGridView1.Columns.Add(comboCategory);
            pictureBox1.Image = Image.FromFile(Path.Combine(Path.GetFullPath(@"..\..\images"), "peacock.jpg"));
            _conn.Close();
        }

        private void dataGridView1_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 4)
                return;

            var imageData = dataGridView1.Rows[e.RowIndex].Cells["Bpilt"].Value as byte[];
            if (imageData == null)
                return;

            using (MemoryStream stream = new MemoryStream(imageData))
            {
                var image = Image.FromStream(stream);
                LooPilt(image, e.RowIndex);
            }
        }

        private void dataGridView1_CellMouseLeave(object sender, DataGridViewCellEventArgs e)
        {
            if (_popupForm != null && !_popupForm.IsDisposed)
            {
                _popupForm.Close();
            }
        }

        private Form _popupForm;

        private void Lisa_Click(object sender, EventArgs e)
        {
            if (SaveFileName == "")
            {
                MessageBox.Show("Palun vali pilt!");
                return;
            }

            if (Toode_txt.Text.Trim() != string.Empty && Kogus_txt.Text.Trim() != string.Empty && Hind_txt.Text.Trim() != string.Empty && Kat_Box.SelectedItem != null)
            {
                try
                {
                    _conn.Open();
                    _command = new SqlCommand("SELECT Id FROM Kategooriad WHERE Nimetus=@kat", _conn);
                    _command.Parameters.AddWithValue("@kat", Kat_Box.Text);
                    _command.ExecuteNonQuery();
                    var Id = Convert.ToInt32(_command.ExecuteScalar());
                    _command = new SqlCommand("INSERT INTO Tooded (Nimetus,Kogus,Hind,Pilt,Bpilt,KategooriaId) " +
                        " VALUES (@toode,@kogus,@hind,@pilt,@bpilt,@kat)", _conn);
                    _command.Parameters.AddWithValue("@toode", Toode_txt.Text);
                    _command.Parameters.AddWithValue("@kogus", Kogus_txt.Text);
                    _command.Parameters.AddWithValue("@hind", Hind_txt.Text);
                    var extension = Path.GetExtension(SaveFileName);
                    _command.Parameters.AddWithValue("@pilt", Toode_txt.Text + extension);
                    var imageData = File.ReadAllBytes(SaveFileName);
                    _command.Parameters.AddWithValue("@bpilt", imageData);
                    _command.Parameters.AddWithValue("@kat", Id);
                    _command.ExecuteNonQuery();
                    _conn.Close();
                    NaitaAndmed();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    _conn.Close();
                }
            }
        }

        private void LooPilt(Image image, int rowIndex)
        {
            _popupForm = new Form();
            _popupForm.FormBorderStyle = FormBorderStyle.None;
            _popupForm.StartPosition = FormStartPosition.Manual;
            _popupForm.Size = new Size(200, 200);

            var pictureBox = new PictureBox();
            pictureBox.Image = image;
            pictureBox.Dock = DockStyle.Fill;
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;

            _popupForm.Controls.Add(pictureBox);

            var rect = dataGridView1.GetCellDisplayRectangle(4, rowIndex, true);
            var pos = dataGridView1.PointToScreen(rect.Location);
            pos.X -= rect.Width;

            _popupForm.Location = pos;
            _popupForm.Show();
        }
    }
}
