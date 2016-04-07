using System;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
#if DEBUG
            MyShell.AllocConsole();
            MyShell.WriteLine("注意：启动程序...");

            MyShell.WriteLine("/tWritten by Oyi319");
            MyShell.WriteLine("/tBlog: http://blog.csdn.com/oyi319");
            MyShell.WriteLine("{0}：{1}", "警告", "这是一条警告信息。");
            MyShell.WriteLine("{0}：{1}", "错误", "这是一条错误信息！");
            MyShell.WriteLine("{0}：{1}", "注意", "这是一条需要的注意信息。");
            MyShell.WriteLine("");
#endif

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());

#if DEBUG
            //MyShell.WriteLine("注意：2秒后关闭...");
            //Thread.Sleep(2000);
            MyShell.FreeConsole();
#endif  

        }
    }
}
