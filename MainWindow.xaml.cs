using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace CallGraphVisualizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var text = File.ReadAllText(@"callGraph.txt");
            Dictionary<string, int> nodes = new Dictionary<string, int>();
            Dictionary<int, List<int>> connections = new Dictionary<int, List<int>>();
            var lines = text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            int index = 0;
            int currentChildIndex = -1;
            foreach (var line in lines)
            {
                var lline = line.Trim();
                if (lline.Length < 5) continue;
                if (lline.StartsWith("The ")) continue;
                if (lline.StartsWith("[")) continue;

                if (!nodes.TryGetValue(lline, out int storedIndex))
                {
                    nodes.Add(lline, index);
                    storedIndex = index;
                }                
                if (!connections.TryGetValue(storedIndex, out List<int> currentNodeConnections))
                {
                    currentNodeConnections = new List<int>();
                    connections.Add(storedIndex, currentNodeConnections);
                }
                var isNew = lline.StartsWith("__");
                if (!currentNodeConnections.Contains(currentChildIndex) && currentChildIndex >= 0 && !isNew) currentNodeConnections.Add(currentChildIndex);
                //if (isNew)
                //    currentChildIndex = -1;
                //else
                currentChildIndex = storedIndex;
                index++;
            }
            var viewModel = new ViewModel();
            DataContext = viewModel;
            foreach (var node in nodes.Keys)
            {
                var lindex = nodes[node];
                var connectionList = connections[lindex];
                viewModel.Items.Add(new Item() { Id = lindex, Name = node });
                foreach (var cindex in connectionList)
                {
                    viewModel.Connections.Add(new Link() { From = lindex, To = cindex });
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            diagramControl.ApplyMindMapTreeLayout();
        }
    }
    public class ViewModel
    {
        public List<Item> Items { get; set; }
        public List<Link> Connections { get; set; }
        public ViewModel()
        {
            Items = new List<Item>();
            Connections = new List<Link>();
        }
    }
    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Link
    {
        public object From { get; set; }
        public object To { get; set; }
    }

    public class NameToColorConverter : IValueConverter
    {
        SolidColorBrush dx = new SolidColorBrush(Colors.LightBlue);
        SolidColorBrush ordinal = new SolidColorBrush(Colors.LightGray);

        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            string name = (string)value;
            if (name.StartsWith("DevEx"))
                return dx;
            else return ordinal;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return null;
            // Do the conversion from visibility to bool
        }
    }
}
