using System;
using System.Windows.Forms;

namespace ScriptLauncher
{
    internal static class Program
    {
        /// <summary>
        /// Главная точка входа в приложение.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                // Если при старте (или в любой момент работы) возникнет необработанное
                // исключение, вместо тихого закрытия без единого сообщения покажем текст
                // ошибки — это сильно упрощает диагностику подобных проблем.
                MessageBox.Show($"Программа столкнулась с ошибкой и не может продолжить работу:\n\n{ex}",
                    "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
