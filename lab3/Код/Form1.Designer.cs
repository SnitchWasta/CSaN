namespace chat_ptp
{
    partial class Form1
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
            textBox1 = new TextBox();
            name_button = new Button();
            chat_flow = new FlowLayoutPanel();
            chat_text_box = new TextBox();
            send_button = new Button();
            textBox2 = new TextBox();
            label1 = new Label();
            label2 = new Label();
            SuspendLayout();
            // 
            // textBox1
            // 
            textBox1.Location = new Point(29, 33);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(143, 23);
            textBox1.TabIndex = 3;
            // 
            // name_button
            // 
            name_button.Location = new Point(375, 32);
            name_button.Name = "name_button";
            name_button.Size = new Size(75, 23);
            name_button.TabIndex = 4;
            name_button.Text = "принять";
            name_button.UseVisualStyleBackColor = true;
            name_button.Click += button1_Click;
            // 
            // chat_flow
            // 
            chat_flow.AllowDrop = true;
            chat_flow.AutoScroll = true;
            chat_flow.BackColor = SystemColors.ActiveCaption;
            chat_flow.BorderStyle = BorderStyle.Fixed3D;
            chat_flow.Enabled = false;
            chat_flow.Location = new Point(29, 85);
            chat_flow.Name = "chat_flow";
            chat_flow.Size = new Size(734, 324);
            chat_flow.TabIndex = 5;
            // 
            // chat_text_box
            // 
            chat_text_box.Enabled = false;
            chat_text_box.Location = new Point(29, 415);
            chat_text_box.Name = "chat_text_box";
            chat_text_box.Size = new Size(671, 23);
            chat_text_box.TabIndex = 0;
            // 
            // send_button
            // 
            send_button.Enabled = false;
            send_button.Location = new Point(706, 415);
            send_button.Name = "send_button";
            send_button.Size = new Size(57, 23);
            send_button.TabIndex = 6;
            send_button.Text = ">>";
            send_button.UseVisualStyleBackColor = true;
            send_button.Click += send_button_Click;
            // 
            // textBox2
            // 
            textBox2.Location = new Point(178, 33);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(143, 23);
            textBox2.TabIndex = 7;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(29, 15);
            label1.Name = "label1";
            label1.Size = new Size(31, 15);
            label1.TabIndex = 8;
            label1.Text = "Имя";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(178, 15);
            label2.Name = "label2";
            label2.Size = new Size(51, 15);
            label2.TabIndex = 9;
            label2.Text = "IP aдрес";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(textBox2);
            Controls.Add(send_button);
            Controls.Add(chat_text_box);
            Controls.Add(chat_flow);
            Controls.Add(name_button);
            Controls.Add(textBox1);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private TextBox textBox1;
        private Button name_button;
        private FlowLayoutPanel chat_flow;
        private TextBox chat_text_box;
        private Button send_button;
        private TextBox textBox2;
        private Label label1;
        private Label label2;
    }
}
