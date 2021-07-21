using ServerLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GodPanel
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void adduser_Click(object sender, EventArgs e)
        {
            Db.StaffUsers.InsertOne(new ServerLib.Models.StaffUser
            {
                HashedPassword = password.Text,
                Email = email.Text,
                Username = username.Text,
                Types = new List<ServerLib.Models.SupportType> { ServerLib.Models.SupportType.Accounts, ServerLib.Models.SupportType.Money},
                Rank = 0,
                LastLogin = DateTime.UtcNow,
            });
            
            password.Text = string.Empty;
            email.Text = string.Empty;
            username.Text = string.Empty;
        }

        private void ema_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
