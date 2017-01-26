using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace LescoApp.Classes
{
    class UtilityClass
    {
        public static string getTempPath()
        {
            return string.Format(@"{0}\Temp", Directory.GetCurrentDirectory());
        }
        public static void createDirectoryStructure()
        {
            string path = getTempPath();
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            } 
        }
        public static void cleanTemp()
        {
            string path = getTempPath();
            string[] tempFiles = Directory.GetFiles(path);

            if (Directory.Exists(path))
            {
                foreach (string file in tempFiles)
                {
                    File.Delete(file);
                }
            }
        }
        public static void customAnimation(Window win, string animationName, DependencyObject obj)
        {
            Storyboard sb = win.FindResource(animationName) as Storyboard;
            Storyboard.SetTarget(sb, obj);
            sb.Begin();
        }
    }
}
