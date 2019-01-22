using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CallGraphVisualizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        String[] lines;
        bool rangeInitialized = false;
        ObservableCollection<int> timingCount = new ObservableCollection<int>();
        ObservableCollection<int> timings = new ObservableCollection<int>();
        ViewModel viewModel = new ViewModel();

        public MainWindow()
        {
            InitializeComponent();
            var text = File.ReadAllText(@"callGraph.txt");
            lines = text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            DataContext = viewModel;
            Update();
            rangeControl.MouseUp += (s, e) =>
            {
                Mouse.SetCursor(Cursors.Wait);
                Update();
                Mouse.SetCursor(Cursors.Arrow);
            };
        }

        private void Update()
        {
            int index = 0;
            int currentChildIndex = -1;
            int currentTime = 0;
            int eventsCountAtCurrentTime = 0;
            int start = 0;
            int end = 0;
            if (rangeInitialized)
            {
                start = (int)(double)rangeControl.SelectionRangeStart;
                end = (int)(double)rangeControl.SelectionRangeEnd;
            }
            Dictionary<string, int> nodes = new Dictionary<string, int>();
            Dictionary<int, List<int>> connections = new Dictionary<int, List<int>>();
            Dictionary<int, int> hitCount = new Dictionary<int, int>();

            foreach (var line in lines)
            {
                var lline = line.Replace("\t", string.Empty);
                lline = lline.Trim();
                if (lline.Length < 5) continue;
                if (lline.StartsWith("The ")) continue;
                if (lline.StartsWith("[")) continue;
                var isNew = lline.StartsWith("__");
                eventsCountAtCurrentTime++;
                if (isNew)
                {
                    var len = "___InvalidateMeasure".Length;
                    var endIndex = lline.IndexOf(@"\n");
                    if (endIndex > 0 && len > 0)
                    {
                        string timeString = lline.Substring(len, endIndex - len);
                        currentTime = int.Parse(timeString);
                        if (!rangeInitialized && timings.Count ==0 || currentTime > timings[timings.Count-1])
                        {
                            timings.Add(currentTime);
                            timingCount.Add(eventsCountAtCurrentTime);
                            eventsCountAtCurrentTime = 0;
                        }
                        lline = lline.Substring(lline.IndexOf(@"\n") + 3);
                    }
                }
                if (rangeInitialized)
                {

                    if (timings[start] > currentTime ||
                        timings[end] < currentTime) continue;
                }

                if (!nodes.TryGetValue(lline, out int storedIndex))
                {
                    nodes.Add(lline, index);
                    hitCount.Add(index, 1);
                    storedIndex = index;
                    index++;
                }
                else
                    hitCount[storedIndex] = hitCount[storedIndex] + 1;
                if (!connections.TryGetValue(storedIndex, out List<int> currentNodeConnections))
                {
                    currentNodeConnections = new List<int>();
                    connections.Add(storedIndex, currentNodeConnections);
                }
                if (!currentNodeConnections.Contains(currentChildIndex) && currentChildIndex >= 0 && !isNew) currentNodeConnections.Add(currentChildIndex);
                currentChildIndex = storedIndex;
            }
            if (!rangeInitialized)
            {
                for (int i = 0; i < timingCount.Count; i++)
                    timingCount[i] = (int)(10 * Math.Log(timingCount[i]));

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
                if (pointIndex > 0 && pointIndex < name.Length - 1) name = name.Substring(0, pointIndex) + Environment.NewLine + name.Substring(pointIndex + 1) +
                        Environment.NewLine + hitCount[lindex];
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
        public ViewModel()
        {
            Items = new ObservableCollection<Item>();
            Connections = new ObservableCollection<Link>();
        }

        public ObservableCollection<Link> Connections { get; set; }
        public ObservableCollection<Item> Items { get; set; }
        public ObservableCollection<int> Timings { get; set; }
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
