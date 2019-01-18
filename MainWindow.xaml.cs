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
        ObservableCollection<int> timingCount = new ObservableCollection<int>();

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
            int currentTimeCount = 0;

            Dictionary<string, int> nodes = new Dictionary<string, int>();
            Dictionary<int, List<int>> connections = new Dictionary<int, List<int>>();
            foreach (var line in lines)
            {
                var lline = line.Trim();
                if (lline.Length < 5) continue;
                if (lline.StartsWith("The ")) continue;
                if (lline.StartsWith("[")) continue;
                var isNew = lline.StartsWith("__");
                currentTimeCount++;
                if (isNew)
                {
                    var len = "___InvalidateMeasure".Length;
                    var endIndex = lline.IndexOf(@"\n");
                    if (endIndex > 0 && len > 0)
                    {
                        string timeString = lline.Substring(len, endIndex - len);
                        currentTime = int.Parse(timeString);
                        if (!rangeInitialized) { timings.Add(currentTime);
                            timingCount.Add(currentTimeCount);
                            currentTimeCount = 0;
                        }
                        lline = lline.Substring(lline.IndexOf(@"\n") + 2);
                    }
                }
                if (rangeInitialized)
                {
                    int start = (int)(double)rangeControl.SelectionRangeStart;
                    int end = (int)(double)rangeControl.SelectionRangeEnd;
                    if (timings[start]  > currentTime || 
                        timings[end]    < currentTime) continue;
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
                viewModel.Timings = timingCount;
                rangeInitialized = true;
            }
            diagramControl.BeginInit();
            behavior.BeginInit();
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
            behavior.EndInit();
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
