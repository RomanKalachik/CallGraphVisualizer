using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        bool rangeInitialized = false;
        ObservableCollection<int> timings = new ObservableCollection<int>();
        String[] lines;
        ViewModel viewModel = new ViewModel();

        public MainWindow()
        {
            InitializeComponent();
            var text = File.ReadAllText(@"callGraph.txt");
            lines = text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            DataContext = viewModel;
            Update();
            rangeControl.PreviewMouseUp+= (s, e) => {
                Update();
            };
        }

        private void Update()
        {
            int index = 0;
            int currentChildIndex = -1;
            int currentTime = 0;
            Dictionary<string, int> nodes = new Dictionary<string, int>();
            Dictionary<int, List<int>> connections = new Dictionary<int, List<int>>();
            foreach (var line in lines)
            {
                var lline = line.Trim();
                if (lline.Length < 5) continue;
                if (lline.StartsWith("The ")) continue;
                if (lline.StartsWith("[")) continue;
                var isNew = lline.StartsWith("__");
                if (isNew)
                {
                    var len = "___InvalidateMeasure".Length;
                    string timeString = lline.Substring(len, lline.IndexOf(@"\n") - len);
                    currentTime = int.Parse(timeString);
                    if (!rangeInitialized) timings.Add(currentTime);
                }
                if (rangeInitialized)
                {
                    if (timings[(int)(double)rangeControl.VisibleRangeStart] > currentTime || timings[(int)(double)rangeControl.VisibleRangeEnd] < currentTime) continue;
                }

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
                if (!currentNodeConnections.Contains(currentChildIndex) && currentChildIndex >= 0 && !isNew) currentNodeConnections.Add(currentChildIndex);
                currentChildIndex = storedIndex;
                index++;
            }
            if (!rangeInitialized)
            {
                viewModel.Timings = timings;
                rangeInitialized = true;
            }
            diagramControl.BeginInit();
            viewModel.Connections.Clear();
            viewModel.Items.Clear();
            foreach (var node in nodes.Keys)
            {
                var lindex = nodes[node];
                var connectionList = connections[lindex];
                var name = node.Replace("!", Environment.NewLine);
                int pointIndex = name.LastIndexOf(".");
                if (pointIndex > 0 && pointIndex < name.Length - 1) name = name.Substring(0, pointIndex) + Environment.NewLine + name.Substring(pointIndex + 1);
                viewModel.Items.Add(new Item() { Id = lindex, Name = name });
                foreach (var cindex in connectionList)
                {
                    viewModel.Connections.Add(new Link() { From = lindex, To = cindex });
                }
            }
            diagramControl.ApplyTreeLayout();
            diagramControl.EndInit();
        }
    }
    public class ViewModel
    {
        public ObservableCollection<Item> Items { get; set; }
        public ObservableCollection<Link> Connections { get; set; }
        public ObservableCollection<int> Timings { get; set; }
        public ViewModel()
        {
            Items = new ObservableCollection<Item>();
            Connections = new ObservableCollection<Link>();
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
}
