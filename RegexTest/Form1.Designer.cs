﻿namespace RegexTest
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.button2 = new System.Windows.Forms.Button();
            this.tbContent = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.btnEnum = new System.Windows.Forms.Button();
            this.btnJS = new System.Windows.Forms.Button();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1862, 1017);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.button2);
            this.tabPage1.Controls.Add(this.tbContent);
            this.tabPage1.Controls.Add(this.button1);
            this.tabPage1.Location = new System.Drawing.Point(4, 28);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(1854, 985);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Replace";
            this.tabPage1.UseVisualStyleBackColor = true;
            this.tabPage1.Click += new System.EventHandler(this.tabPage1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(297, 229);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(209, 63);
            this.button2.TabIndex = 2;
            this.button2.Text = "SQL字段末尾加XXX";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // tbContent
            // 
            this.tbContent.Location = new System.Drawing.Point(8, 41);
            this.tbContent.Multiline = true;
            this.tbContent.Name = "tbContent";
            this.tbContent.Size = new System.Drawing.Size(1818, 129);
            this.tbContent.TabIndex = 1;
            this.tbContent.Text = "fffffffffff\r\n\r\nfffffffffff\r\n\r\ndfdfdfdf\r\n\r\nerererere\r\n\r\nferewfdfds";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(30, 229);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(209, 63);
            this.button1.TabIndex = 0;
            this.button1.Text = "行末尾加XXX";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.btnJS);
            this.tabPage2.Controls.Add(this.btnEnum);
            this.tabPage2.Location = new System.Drawing.Point(4, 28);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(1854, 985);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Find";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // btnEnum
            // 
            this.btnEnum.Location = new System.Drawing.Point(40, 167);
            this.btnEnum.Name = "btnEnum";
            this.btnEnum.Size = new System.Drawing.Size(209, 63);
            this.btnEnum.TabIndex = 4;
            this.btnEnum.Text = "提取枚举名称和枚举描述";
            this.btnEnum.UseVisualStyleBackColor = true;
            this.btnEnum.Click += new System.EventHandler(this.btnEnum_Click);
            // 
            // btnJS
            // 
            this.btnJS.Location = new System.Drawing.Point(293, 167);
            this.btnJS.Name = "btnJS";
            this.btnJS.Size = new System.Drawing.Size(209, 63);
            this.btnJS.TabIndex = 5;
            this.btnJS.Text = "提取JS中的中文";
            this.btnJS.UseVisualStyleBackColor = true;
            this.btnJS.Click += new System.EventHandler(this.btnJS_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1874, 1014);
            this.Controls.Add(this.tabControl1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TextBox tbContent;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button btnEnum;
        private System.Windows.Forms.Button btnJS;
    }
}

