
namespace GodPanel
{
    partial class Main
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.username = new System.Windows.Forms.TextBox();
            this.password = new System.Windows.Forms.TextBox();
            this.adduser = new System.Windows.Forms.Button();
            this.gb1 = new System.Windows.Forms.GroupBox();
            this.email = new System.Windows.Forms.TextBox();
            this.gb1.SuspendLayout();
            this.SuspendLayout();
            // 
            // username
            // 
            this.username.Location = new System.Drawing.Point(6, 26);
            this.username.Name = "username";
            this.username.PlaceholderText = "Username";
            this.username.Size = new System.Drawing.Size(229, 27);
            this.username.TabIndex = 0;
            // 
            // password
            // 
            this.password.Location = new System.Drawing.Point(6, 59);
            this.password.Name = "password";
            this.password.PlaceholderText = "Password";
            this.password.Size = new System.Drawing.Size(229, 27);
            this.password.TabIndex = 1;
            this.password.TextChanged += new System.EventHandler(this.textBox2_TextChanged);
            // 
            // adduser
            // 
            this.adduser.Location = new System.Drawing.Point(6, 125);
            this.adduser.Name = "adduser";
            this.adduser.Size = new System.Drawing.Size(229, 29);
            this.adduser.TabIndex = 2;
            this.adduser.Text = "Add staff";
            this.adduser.UseVisualStyleBackColor = true;
            this.adduser.Click += new System.EventHandler(this.adduser_Click);
            // 
            // gb1
            // 
            this.gb1.Controls.Add(this.email);
            this.gb1.Controls.Add(this.username);
            this.gb1.Controls.Add(this.adduser);
            this.gb1.Controls.Add(this.password);
            this.gb1.Location = new System.Drawing.Point(12, 12);
            this.gb1.Name = "gb1";
            this.gb1.Size = new System.Drawing.Size(241, 163);
            this.gb1.TabIndex = 3;
            this.gb1.TabStop = false;
            this.gb1.Text = "Add Staff User";
            // 
            // email
            // 
            this.email.Location = new System.Drawing.Point(6, 92);
            this.email.Name = "email";
            this.email.PlaceholderText = "Email";
            this.email.Size = new System.Drawing.Size(229, 27);
            this.email.TabIndex = 3;
            this.email.TextChanged += new System.EventHandler(this.ema_TextChanged);
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1285, 595);
            this.Controls.Add(this.gb1);
            this.Name = "Main";
            this.Text = "OTaff God Panel";
            this.Load += new System.EventHandler(this.Main_Load);
            this.gb1.ResumeLayout(false);
            this.gb1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox username;
        private System.Windows.Forms.TextBox password;
        private System.Windows.Forms.Button adduser;
        private System.Windows.Forms.GroupBox gb1;
        private System.Windows.Forms.TextBox ema;
        private System.Windows.Forms.TextBox emi;
        private System.Windows.Forms.TextBox email;
    }
}

