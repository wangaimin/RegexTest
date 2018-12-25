using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RegexTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            string v = tbContent.Text;
            tbContent.Text = "";
            tbContent.Text = v;
        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
           //$0 为匹配到的所有；$1为匹配到的第一个分组；$2为匹配到的第二个分组


            string v = @"fffffffffff

                        fffffffffff

                        dfdfdfdf

                        erererere

                        ferewfdfds";
            string val = tbContent.Text;
            string pattern = "^\\w*$";
            Regex regex = new Regex(pattern);
            string result = Regex.Replace(v, pattern, "$0XXX", RegexOptions.Multiline);
            tbContent.Text = result;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string val = @"SELECT TOP 1000 [SysNo]
                          ,[ParentSysNo]
                          ,[OrganizationCode]
                          ,[CategoryCode]
                          ,[CategoryName]
                          ,[CommonStatus]
                          ,[InUserSysNo]
                          ,[InUserName]
                          ,[InDate]
                          ,[EditUserSysNo]
                          ,[EditUserName]
                          ,[EditDate]
                          ,[SystemCategoryType]
                          ,[IconUrl]
                          ,[ImageUrl]
                          ,[ServiceType]
                      FROM [YZ_Operation].[dbo].[SystemCategory]";


            string result = Regex.Replace(val, @"\[\w+\](?=\r\n)", "$0-XXX", RegexOptions.Multiline);
            tbContent.Text = result;
        }

        private void btnEnum_Click(object sender, EventArgs e)
        {
            //(\w+)此处()是把枚举名称加入分组；(.+?)此处?为懒惰匹配，匹配最少项
            Regex regexEnum = new Regex(@"enum\s+(\w+)\s+{(.+?)}", RegexOptions.IgnoreCase | RegexOptions.Singleline);


            Regex regexEnumDescription = new Regex(@"\[Description\(""(\w+)""\)\]\s+(\w+)", RegexOptions.Singleline);


            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string path = baseDirectory.Substring(0, baseDirectory.LastIndexOf("\\bin"));
            var files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                List<string> enumNameList = new List<string>();

                string content = File.ReadAllText(file);
                MatchCollection matchCollection = regexEnum.Matches(content);
                for (int i = 0; i < matchCollection.Count; i++)
                {
                    string enumName = matchCollection[i].Groups[1].Value;
                    string enumContent = matchCollection[i].Groups[2].Value;
                    if (!enumNameList.Contains(enumName))
                    {
                        List<string> enumDescriptionList = new List<string>();

                        enumNameList.Add(enumName);

                        var regexEnumDescriptionCollection = regexEnumDescription.Matches(enumContent);
                        for (int j = 0; j < regexEnumDescriptionCollection.Count; j++)
                        {
                            string description = regexEnumDescriptionCollection[j].Groups[1].Value;
                            enumDescriptionList.Add(description);
                        }



                    }
                }
            }


        }

        /// <summary>
        /// 提取js中的中文
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnJS_Click(object sender, EventArgs e)
        {

            //tag1为别名，后面会引用( 规则(?<name>规则)  )；前面和后面完全匹配，例如前面为  start  ，后面也必须为start  ( "start123start")
            //['"]取'或"；
            //[^\r\n]非换行符或回车   
            //匹配双字节字符(包括汉字在内)：[^\x00-\xff]
            Regex regexEnum = new Regex(@"(?<tag>['""])([^\r\n]*[^\x00-\xff]+[^\r\n]*)\k<tag>", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string path = baseDirectory.Substring(0, baseDirectory.LastIndexOf("\\bin"));
            var files = Directory.GetFiles(path, "*.js", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                List<string> enumNameList = new List<string>();

                string content = File.ReadAllText(file);
                MatchCollection matchCollection = regexEnum.Matches(content);
                for (int i = 0; i < matchCollection.Count; i++)
                {
                    string word = matchCollection[i].Groups[0].Value;
                   
                }
            }


        }

        private void btnService_Click(object sender, EventArgs e)
        {
            //
            Regex regex = new Regex(@"""([^\r\n]*[^\x00-\xff]+[^\r\n]*)""",RegexOptions.Multiline);
        }

        private void btnCsHtml_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            string str = textBox1.Text.Trim();
            string pattern = "(?!^)(?=(.{4})+$)";
            Regex regex = new Regex(pattern, RegexOptions.Multiline);
            textBox1.Text=regex.Replace(str, " ");

        }

        private void button4_Click(object sender, EventArgs e)
        {
            string str = "CSDN_hello-nihao_1/2/3";
            string pattern = @"(?im)(CSDN_).*_(\D*\d+/.*?)(_.*|$)";
            string result = Regex.Replace(str, pattern, "$1$2");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            string result = Regex.Replace("中国：", @"^([^\x00-\xff\s]+)[:：！!]+$", "$1", RegexOptions.Singleline);


        }
    }
}
