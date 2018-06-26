using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace GTAPilot.Extensions
{
    public static class UIElementExtensions
    {
        public static T GetTransform<T>(this UIElement element) where T : Transform
        {
            return (T)((TransformGroup)element.RenderTransform).Children.First(tr => tr is T);
        }
    }
}
